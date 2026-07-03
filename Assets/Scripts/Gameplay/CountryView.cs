using Contagion.Data;
using Contagion.Managers;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 국가 하나에 대응하는 시각 요소. 설계 문서 15절 "국가 지도는 2D 폴리곤 또는 스프라이트 방식 권장"
    /// 에 따라 SpriteRenderer + Collider2D(클릭 감지) 조합으로 구현한다.
    /// 씬에서 국가 개수만큼 배치하고 countryId만 채워주면 WorldMap이 자동으로 등록/갱신한다.
    /// </summary>
    // OnMouseDown()이 동작하려면 Collider2D가 필요하다. 국가 모양이 제각각이므로 기본은
    // BoxCollider2D를 강제하되, 실제 국경선 클릭 정확도가 필요하면 PolygonCollider2D로 교체할 것.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CountryView : MonoBehaviour
    {
        [SerializeField] private string countryId;
        [SerializeField] private Color healthyColor = new Color(0.35f, 0.75f, 0.35f);
        [SerializeField] private Color infectedColor = new Color(0.9f, 0.75f, 0.15f);
        [SerializeField] private Color deadColor = new Color(0.55f, 0.1f, 0.1f);
        [SerializeField, Tooltip("색이 목표값으로 전환되는 속도. 값이 클수록 빠르게(즉시에 가깝게) 바뀐다. " +
            "틱마다 색이 뚝뚝 끊겨 바뀌는 대신 부드럽게 전환되도록 추가 — 기본 플레이 퀄리티 개선 항목.")]
        private float colorTransitionSpeed = 3f;

        [SerializeField, Tooltip("Resources/CountryShapes/{countryId} 실루엣 스프라이트를 로드했을 때 최종 " +
            "표시 크기(월드 유닛, 긴 변 기준). 국가마다 원본 경위도 종횡비가 제각각이라 이 값 기준으로 " +
            "균일 스케일을 계산한다 — 기존 플레이스홀더 사각형 크기(약 0.48)와 비슷하게 맞춰 지도 배치가 " +
            "안 깨지도록 함.")]
        private float shapeTargetSize = 0.45f;

        private SpriteRenderer _renderer;
        private BoxCollider2D _collider;
        private Color _targetColor;
        private bool _hasTarget;
        private float _lastLoggedInfectionBand = -1f;
        private float _lastLoggedDeadBand = -1f;

        public string CountryId => countryId;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            _renderer.color = healthyColor; // 첫 UpdateVisual 전까지 흰색 스프라이트가 잠깐 보이는 걸 방지
            ApplyCountryShape();
        }

        /// <summary>
        /// 비주얼 폴리싱 — 씬에 배치된 플레이스홀더 사각형 스프라이트를 Resources/CountryShapes/{countryId}.png
        /// 실제 국가 실루엣으로 런타임 교체한다. Flags(Step 22)와 같은 방식으로 씬/에셋 파일에 스프라이트
        /// GUID를 하드코딩하지 않고 경로 문자열로 로드 — Unity 에디터 없이도 텍스트(스크립트+이미지 파일
        /// 추가)만으로 배선 가능. 이미지가 없으면 기존 플레이스홀더가 그대로 남는다.
        ///
        /// 프로젝트 기본 텍스처 임포트 프리셋이 Sprite(Multiple) 모드라 서브에셋 이름이 "{id}_0"이 되므로
        /// (Step 22 국기 로딩 때 확인된 동작) Resources.Load&lt;Sprite&gt;(경로)는 실패할 수 있다 —
        /// LoadAll로 서브에셋을 전부 가져와 첫 번째를 쓰는 방식이 spriteMode(Single/Multiple)에 관계없이 안전하다.
        /// </summary>
        private void ApplyCountryShape()
        {
            string path = $"CountryShapes/{countryId}";
            var sprites = Resources.LoadAll<Sprite>(path);
            Sprite shape = sprites != null && sprites.Length > 0 ? sprites[0] : Resources.Load<Sprite>(path);

            if (shape == null)
            {
                Debug.LogWarning($"[CountryView] {countryId} — Resources/{path}.png를 찾지 못해 기존 플레이스홀더 스프라이트를 유지합니다.");
                return;
            }

            _renderer.sprite = shape;

            float longestLocal = Mathf.Max(shape.bounds.size.x, shape.bounds.size.y);
            if (longestLocal > 0.0001f)
            {
                float scale = shapeTargetSize / longestLocal;
                transform.localScale = new Vector3(scale, scale, 1f);
            }

            if (_collider != null)
                _collider.size = shape.bounds.size; // 실제 실루엣 크기에 맞춰 클릭 판정 영역도 갱신

            Debug.Log($"[CountryView] {countryId} 실제 국가 실루엣 스프라이트 적용 완료 (scale={transform.localScale.x:F2}).");
        }

        private void Start()
        {
            if (WorldMap.Instance != null)
            {
                WorldMap.Instance.RegisterCountryView(this);
                Debug.Log($"[CountryView] {countryId} WorldMap에 등록됨");
            }
            else
            {
                Debug.LogWarning($"[CountryView] {countryId} — WorldMap.Instance가 없어 등록 실패. WorldMap 오브젝트가 씬에 있는지 확인.");
            }
        }

        private void OnDestroy()
        {
            if (WorldMap.Instance != null)
                WorldMap.Instance.UnregisterCountryView(this);
        }

        /// <summary>WorldMap이 국가 상태 갱신 시 호출 — 목표 색만 갱신하고 실제 적용은 Update()에서 보간한다.</summary>
        public void UpdateVisual(Country country)
        {
            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;
            float deadRatio = country.population > 0
                ? (float)country.deadCount / country.population
                : 0f;

            Color color = Color.Lerp(healthyColor, infectedColor, Mathf.Clamp01(infectionRatio));
            color = Color.Lerp(color, deadColor, Mathf.Clamp01(deadRatio));

            _targetColor = color;
            _hasTarget = true;

            // 매 틱 스팸 방지 — 감염률/사망률이 10%p 단위로 바뀔 때만 로그
            float infectionBand = Mathf.Round(infectionRatio * 10f) / 10f;
            float deadBand = Mathf.Round(deadRatio * 10f) / 10f;
            if (!Mathf.Approximately(infectionBand, _lastLoggedInfectionBand) || !Mathf.Approximately(deadBand, _lastLoggedDeadBand))
            {
                _lastLoggedInfectionBand = infectionBand;
                _lastLoggedDeadBand = deadBand;
                Debug.Log($"[CountryView] {countryId} 목표색 갱신 — 감염률={infectionRatio:P0}, 사망률={deadRatio:P0}, 목표색={color}");
            }
        }

        private void Update()
        {
            if (!_hasTarget) return;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            _renderer.color = Color.Lerp(_renderer.color, _targetColor, 1f - Mathf.Exp(-colorTransitionSpeed * Time.deltaTime));
        }

        /// <summary>
        /// 예전엔 OnMouseDown()에서 바로 클릭 처리했는데, Step 28에서 지도 좌우 드래그 스크롤을
        /// 추가하면서 문제가 생겼다 — 지도를 스와이프하다가 손가락이 국가 위를 지나가면 누르는 순간
        /// 바로 팝업이 열려버림. OnMouseUpAsButton(누른 채 이 콜라이더 위에서 뗄 때만 호출)으로
        /// 바꾸고, WorldMapCameraController가 이번 누름 사이클에서 드래그 임계값을 넘었으면
        /// WasDragging이 true가 되므로 그 경우엔 클릭을 무시한다.
        /// </summary>
        private void OnMouseUpAsButton()
        {
            if (WorldMapCameraController.Instance != null && WorldMapCameraController.Instance.WasDragging)
                return;

            WorldMap.Instance?.HandleCountryClicked(countryId);
        }
    }
}
