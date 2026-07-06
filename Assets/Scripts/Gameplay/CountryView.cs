using Contagion.Data;
using Contagion.Managers;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 국가 하나에 대응하는 시각 요소. Step 29에서 "국가별로 따로 만들지 말고 세계지도 하나 위에
    /// 국가별 위치만 표시" 방향으로 아키텍처를 바꿨다 — Resources/CountryShapes/{countryId}.png는
    /// 더 이상 독립된(다른 크기/위치의) 국가 실루엣이 아니라, 세계지도 배경(Resources/WorldMap/world_base.png)과
    /// 완전히 동일한 캔버스(4000x1714px, Sprite Pixels Per Unit 500)에서 그 국가 하나만 흰색으로 채워
    /// 래스터화한 "오버레이" 이미지다(생성 스크립트가 좌표 정렬까지 전부 처리 — DevLog Step 29 참고).
    /// 그래서 이 컴포넌트가 붙은 GameObject는 항상 부모(WorldMap) 기준 로컬 (0,0,0)에 고정돼 있으면
    /// 스프라이트 픽셀 자체가 알아서 지도 위 정확한 위치에 그려진다 — Step 27/28에서 겪었던 "국가별
    /// 위치·크기를 일일이 맞추고 겹침을 피해야 하는" 문제가 구조적으로 사라진다.
    ///
    /// 색상도 불투명 도형이 아니라 "감염이 퍼질수록 진해지는 반투명 얼룩" 방식으로 바꿨다 —
    /// healthyColor의 알파를 0으로 둬서 평시엔 안 보이고(그 아래 세계지도 배경만 보임), 감염률/사망률이
    /// 오르면서 infectedColor/deadColor로 Lerp되며 알파도 같이 올라가 흐릿하게 스며드는 것처럼 보인다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CountryView : MonoBehaviour
    {
        [SerializeField] private string countryId;
        [SerializeField] private Color healthyColor = new Color(0.35f, 0.75f, 0.35f, 0f);
        [SerializeField] private Color infectedColor = new Color(0.95f, 0.65f, 0.1f, 0.65f);
        [SerializeField] private Color deadColor = new Color(0.55f, 0.05f, 0.05f, 0.85f);
        [SerializeField, Tooltip("색이 목표값으로 전환되는 속도. 값이 클수록 빠르게(즉시에 가깝게) 바뀐다. " +
            "틱마다 색이 뚝뚝 끊겨 바뀌는 대신 부드럽게 전환되도록 추가 — 기본 플레이 퀄리티 개선 항목.")]
        private float colorTransitionSpeed = 3f;

        [SerializeField, Tooltip("이 국가의 DNA 버블이 스폰될 위치 — 부모(WorldMap) 기준 로컬 오프셋. " +
            "world.svg에서 이 국가 폴리곤의 중심(가장 큰 파츠 기준 무게중심)을 추출해 계산한 값 " +
            "(DevLog Step 29 참고). CountryView 자체는 항상 (0,0,0)에 있으므로 이 필드가 없으면 " +
            "모든 국가의 버블이 지도 중심 한 점에서 스폰된다.")]
        private Vector2 dnaSpawnLocalOffset;

        private SpriteRenderer _renderer;
        private BoxCollider2D _collider;
        private Color _targetColor;
        private bool _hasTarget;
        private float _lastLoggedInfectionBand = -1f;
        private float _lastLoggedDeadBand = -1f;

        public string CountryId => countryId;

        /// <summary>DNA 버블 스폰 기준 월드 좌표. BubbleSpawner가 사용.</summary>
        public Vector3 DnaSpawnWorldPosition => transform.TransformPoint(dnaSpawnLocalOffset);

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            _renderer.color = healthyColor; // 알파 0 — 첫 UpdateVisual 전까지 아무것도 안 보임(의도된 동작)
            ApplyCountryShape();
        }

        /// <summary>
        /// Resources/CountryShapes/{countryId}.png(세계지도와 동일 캔버스 기준 오버레이)를 로드해 적용한다.
        /// 예전엔 국가마다 원본 이미지 크기가 제각각이라 shapeTargetSize 기준으로 localScale을 재계산했는데,
        /// Step 29부터는 모든 오버레이가 같은 캔버스/같은 Pixels Per Unit(500)로 생성되므로 그럴 필요가
        /// 없다 — sprite를 그대로 붙이기만 하면 이미 올바른 크기·위치로 그려진다.
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
            Debug.Log($"[CountryView] {countryId} 세계지도 오버레이 스프라이트 적용 완료.");
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

            // healthyColor→infectedColor→deadColor Lerp에 RGB뿐 아니라 알파도 같이 포함돼 있어서
            // (각 색상의 a값이 0 → 0.65 → 0.85로 다름) 별도 알파 계산 없이 이 Lerp만으로 "안 보임 →
            // 옅은 얼룩 → 진한 얼룩" 전환이 자동으로 된다.
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

        // Step 28-2: 국가를 탭해서 개별 팝업을 여는 인터랙션을 제거했다. 지금은 국가들이 지도 위 실제
        // 위치·크기 그대로라 예전보다 더 촘촘/작아서 탭 정확도 문제가 여전하다. 대신 HUD의 "국가현황"
        // 버튼으로 18개국 상태를 한 번에 보는 CountryStatusPanel(Step 28-2 신규)을 쓴다 — DevLog 참고.
        // CountryPopupController/CountryPopup.uxml은 코드는 남아있지만 더 이상 아무도 호출하지 않는다.
        // BoxCollider2D도 같은 이유로 더 이상 클릭 판정에 쓰이지 않지만(RequireComponent만 유지),
        // 제거하는 리스크보다 그대로 두는 게 안전하다고 판단해 남겨뒀다.
    }
}
