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

        [Header("감염 점(dot) 오버레이 (Step 35/36/41~43, Step 50 복원)")]
        [SerializeField, Tooltip("[Step 50] '레이어로 나눠서 구분하는 거 너무 힘든데 이전 버전(하이라이트 " +
            "없는 거)으로 돌아갈까'(Step 49) 이후, 사용자가 다시 '감염 점 안 보이는데'라고 확인해준 " +
            "결과 — 원했던 건 국가 색상 얼룩만 남기는 게 아니라, Step 44의 큰 '핫스팟' 원(레이어링 " +
            "문제의 원인) 없이 Step 41~43 시절의 '국가당 여러 개의 작은 점' 방식으로 돌아가는 것이었다. " +
            "그래서 Step 44에서 대체됐던 개별 점 GameObject 생성 로직(SetupInfectionDots/UpdateInfectionDots)을 " +
            "복원했다 — 좌표 데이터(InfectionDotPoints.json)는 Step 43 이후 그대로 재사용했었는데, Step 51에서 " +
            "개수 자체를 2배로 재생성했다(아래 maxInfectionDots 툴팁 참고). 공유 점 " +
            "스프라이트도 Step 47의 부드러운 그라디언트 버전을 그대로 재사용(하드엣지 원판으로 되돌리지 " +
            "않음 — 순수 개선이라 되돌릴 이유 없음), sortingOrder만 Step 46 레이어 체계(지도 0 < 이 값 " +
            "10 < 국가 오버레이 20 < 교통 30~40)에 맞춰 예전 값(50)에서 재조정했다.")]
        private bool dotsEnabled = true;
        [SerializeField, Tooltip("[Step 35~43 이력] 국가당 만들 개별 점(GameObject) 개수의 안전 상한 — " +
            "실제 개수는 InfectionDotPoints.json에 국가별로 미리 계산돼 있는 값을 그대로 쓰고, 이 값은 " +
            "그보다 커도 상관없게 넉넉히 잡아둔 상한일 뿐이다. " +
            "[Step 51] '개수 2배로 늘려줘' 요청으로 JSON 자체의 국가별 점 개수를 2배로 재생성(한국 " +
            "64→128개, 러시아 412→824개, 총합 6380→12760개)했는데, 새 최대치(러시아 824)가 이 상한(450)에 " +
            "걸려 잘려나가고 있었으므로 상한도 450→900으로 같이 올렸다.")]
        private int maxInfectionDots = 900;
        [SerializeField] private Color infectionDotColor = new Color(0.85f, 0.05f, 0.05f, 0.95f);
        [SerializeField, Tooltip("InfectionDotDatabase가 국가별로 계산해둔 점 지름(전체 활성화 시 국가 " +
            "면적의 약 70%를 덮도록 역산한 값)에 곱하는 배율. [Step 42] 1→0.35(Step41의 1→0.5를 거쳐 " +
            "재조정한 최종값). [Step 51] '크기 2배로 늘려줘' 요청으로 0.35→0.7. " +
            "[Step 53] '최소 크기 0.5로 고정, 한국보다 큰 나라는 크기를 확대'로 역할이 바뀌었다 — 이제 " +
            "이 값은 전 국가 공통 배율이 아니라 '가장 작은 나라(현재 한국)에 적용되는 최소 배율(바닥값)'이다. " +
            "0.7→0.5로 낮추고, 실제 배율은 SetupInfectionDots()에서 InfectionDotDatabase.MinDiameter 대비 " +
            "자국 diameter 비율만큼 이 값 위로 키운 뒤 적용한다(아래 참고) — 한국은 정확히 이 값(0.5)을 " +
            "그대로 쓰고, 더 큰 나라는 비례해서 더 커진다.")]
        private float infectionDotDiameterScale = 0.5f;
        [SerializeField, Tooltip("점이 나타나고 사라지는(스케일 0↔1) 전환 속도.")]
        private float dotTransitionSpeed = 6f;
        [SerializeField, Tooltip("[Step 50] 감염 점 렌더링 정렬 순서 — 예전(Step 41~43) 값은 50이었지만, " +
            "그 이후(Step 46) 확립된 레이어 체계(지도 0 < 감염 오버레이 10 < 국가 오버레이 20 < 교통 " +
            "30~40)에 맞춰 핫스팟이 쓰던 자리(10)를 그대로 물려받았다 — 지도 바로 위, 국가 색상 " +
            "얼룩/교통수단보다는 아래.")]
        private int dotSortingOrder = 10;

        [Header("감염 핫스팟 오버레이 (Step 44)")]
        [SerializeField, Tooltip("[Step 49] '레이어로 나눠서 구분하는 게 너무 힘들다, 하이라이트 없는 " +
            "이전 버전으로 돌아가자'는 요청 반영 — 핫스팟이 다른 요소를 가리는 문제를 Step 45~48에 " +
            "걸쳐 네 번 손봤는데도(상한 추가/레이어 재정비/그라디언트 텍스처/색상 대비 조정) 계속 " +
            "레이어링 문제가 반복돼서, 아예 핫스팟 오버레이 자체를 끄고 Step 35 이전처럼 국가 색상 " +
            "얼룩(healthyColor→infectedColor→deadColor)만으로 감염 상태를 표시하는 쪽으로 되돌렸다. " +
            "핫스팟 관련 코드/필드는 전부 그대로 남겨뒀다 — 이 값을 다시 true로 켜기만 하면 언제든 " +
            "복원 가능하다(코드 삭제가 아니라 비활성화).")]
        private bool hotspotsEnabled = false;
        [SerializeField, Tooltip("[Step 35~43 이력] 감염률에 따른 색상 얼룩만으로는 시각적 피드백이 " +
            "약해서 국가당 32~412개의 개별 점(GameObject)을 미리 만들어두고 감염률에 비례해 하나씩 " +
            "켜는 방식을 세 번(Step 35/36/41/42/43) 반복 튜닝했는데, 국가당 최대 412개 × 48개국 = " +
            "6380개 GameObject가 Awake 시점에 전부 생성되고 매 프레임 순회하는 구조라 비효율적이라는 " +
            "지적을 받았다. " +
            "[Step 44] 개별 점 GameObject를 만드는 대신, 국가당 소수(3~10개)의 큰 '핫스팟' 원만 만들고 " +
            "감염률에 비례해 개수뿐 아니라 크기/밝기도 연속적으로 커지게 바꿨다 — 국가당 GameObject가 " +
            "412개 → 최대 10개로 줄어(48개국 합계 6380개 → 대략 300개 안팎) 훨씬 가볍다. 좌표/지름은 " +
            "여전히 InfectionDotPoints.json(Resources/InfectionDotDatabase)의 기존 데이터를 그대로 " +
            "재사용한다 — 새로 오프라인 스크립트를 돌리지 않고, '전체 점 개수 대비 실제 쓰는 핫스팟 " +
            "개수' 비율만큼 지름을 수학적으로 키워서(면적 공식 재적용과 동일한 결과) 쓴다. 이 값은 " +
            "그 핫스팟 개수의 상한이다(국가 데이터가 이보다 적으면 있는 만큼만 생성).")]
        private int maxHotspotCount = 10;
        [SerializeField, Tooltip("핫스팟 개수 하한 — 아주 작은 나라도 최소 이 개수만큼은 핫스팟을 " +
            "갖는다(면적 비례 공식이 3 미만을 뽑을 수 있어서 하한을 둠).")]
        private int minHotspotCount = 3;
        [SerializeField, Tooltip("핫스팟 개수를 정하는 공식의 나눔값 — count = sqrt(전체 점 개수)/이 값, " +
            "min~maxHotspotCount로 클램프. 값을 낮추면 국가별 핫스팟 개수 차이가 더 커지고(큰 나라에 " +
            "더 많이), 높이면 다들 비슷하게 적은 개수로 수렴한다.")]
        private float hotspotCountDivisor = 1.5f;
        [SerializeField, Tooltip("핫스팟 지름 배율 — InfectionDotDatabase 원본 지름(원래 개수 기준 '면적 " +
            "70% 커버' 공식값)을 sqrt(원래 개수/핫스팟 개수)만큼 키운 값에 추가로 곱한다. 1보다 크게 " +
            "잡으면 핫스팟끼리 겹쳐서 '뭉친 붉은 영역'처럼 보이고(의도된 모습), 1에 가까우면 개별 원이 " +
            "더 뚜렷하게 분리돼 보인다. " +
            "[Step 45] '원이 국가 밖으로/이웃 나라로 침범한다' 피드백으로 1.5→0.9로 낮춤 — 아래 " +
            "maxHotspotDiameterFraction과 함께 이중으로 과도한 크기를 억제한다. " +
            "[Step 46] '최대 점 크기를 지금에서 절반으로' 요청에 맞춰 0.9→0.45로 다시 낮춤 — " +
            "maxHotspotDiameterFraction도 동시에 정확히 절반으로 낮춰서(두 값이 min()으로 묶여 있어 " +
            "어느 쪽이 실제 상한으로 작동하든) 48개국 전부 최종 지름이 예외 없이 정확히 절반이 되도록 함. " +
            "[Step 47] '아직도 다른 오브젝트를 가림' 피드백으로 0.45→0.225로 한 번 더 절반.")]
        private float hotspotSizeBoost = 0.225f;
        [SerializeField, Tooltip("[Step 45] 핫스팟 지름이 이 국가의 실제 폭/높이(아래 참고)를 넘지 못하도록 " +
            "거는 상한 — '좁은 쪽' 치수(bounding box의 min(width,height))에 곱하는 비율이다. sizeBoost나 " +
            "면적 공식 계산이 특정 국가에서 과하게 커지는 경우(예: 점 개수가 적은데 원래 지름 자체가 큰 " +
            "국가)를 대비한 안전장치 — 이게 없으면 이웃 나라 국경까지 침범해 가시성이 떨어지는 문제가 " +
            "있었다. 국가의 폭/높이는 InfectionDotDatabase 좌표(_allDotPoints, 전부 실루엣 내부가 " +
            "보장됨)의 바운딩박스로 런타임에 근사한다 — 새 오프라인 데이터 불필요. " +
            "[Step 46] hotspotSizeBoost와 함께 0.35→0.175로 절반 조정(위 참고). " +
            "[Step 47] 0.175→0.0875로 한 번 더 절반(위 hotspotSizeBoost와 항상 같은 비율로 맞출 것).")]
        private float maxHotspotDiameterFraction = 0.0875f;
        [SerializeField, Tooltip("[Step 45] '원이 국가 영역을 가려서 안 보인다' 피드백으로 알파(불투명도)를 " +
            "0.85→0.5로 낮춤 — 핫스팟이 커져도 그 아래 국가 색상/실루엣이 비쳐 보이도록.")]
        private Color hotspotColor = new Color(0.85f, 0.1f, 0.1f, 0.5f);
        [SerializeField, Tooltip("핫스팟이 나타나고(스케일 0→목표치) 커지는 전환 속도. colorTransitionSpeed와 " +
            "같은 방식(Exp 감쇠).")]
        private float hotspotTransitionSpeed = 4f;
        [SerializeField, Tooltip("[Step 46] 레이어 순서 재정비 — '비행기/배+경로를 가장 위, 국가 오버레이를 " +
            "그 다음, 감염 핫스팟은 지도 바로 위, 바다(지도)는 맨 아래'로 가시성을 개선해달라는 요청 반영. " +
            "전체 순서(아래→위): 지도(WorldMapBackgroundLoader, 0) → 감염 핫스팟(10, 이 필드) → 국가 " +
            "오버레이(20, countrySortingOrder) → 교통 노선(30, TransportManager) → 교통 유닛(39~40, " +
            "TransportUnit). 예전엔 50(교통 노선 40보다 위, 국가 오버레이 암묵적 0보다 위)이었는데, 이제 " +
            "국가 오버레이가 핫스팟보다 위로 가면서 정반대로 재배치됐다.")]
        private int hotspotSortingOrder = 10;
        [SerializeField, Tooltip("[Step 46] 국가 색상 오버레이(이 스크립트의 SpriteRenderer 본체 — 사용자가 " +
            "'국가 경계선' 레이어로 지칭한 것과 동일한 요소, 실제 테두리 선은 아직 없음)의 정렬 순서. " +
            "지도(0)·감염 핫스팟(10)보다 위, 교통 노선/유닛(30~40)보다 아래.")]
        private int countrySortingOrder = 20;
        [SerializeField, Range(0f, 1f), Tooltip("핫스팟이 막 드러났을 때의 최소 크기 비율(1=꽉 찬 크기). " +
            "감염률이 올라갈수록 이 값과 1 사이를 보간해 커진다 — 낮게 잡을수록 '처음엔 작다가 점점 " +
            "커지는' 느낌이 강해진다.")]
        private float minRevealedScaleFraction = 0.3f;

        private SpriteRenderer _renderer;
        private BoxCollider2D _collider;
        private Color _targetColor;
        private bool _hasTarget;
        private float _targetInfectionRatio;
        private float _lastLoggedInfectionBand = -1f;
        private float _lastLoggedDeadBand = -1f;

        private static Sprite _sharedInfectionDotSprite;
        private Vector2[] _allDotPoints; // InfectionDotDatabase 원본 좌표 전체(핫스팟 앵커 + DNA 버블 스폰 후보로 재사용)
        private float _baseDotDiameter; // InfectionDotDatabase 원본 지름(핫스팟 지름 역산의 기준값)
        private Transform[] _hotspotTransforms;
        private float[] _hotspotCurrentScale;
        private float _hotspotDiameter;
        private Transform[] _dotTransforms;
        private float[] _dotCurrentScale;
        private int _targetActiveDotCount;
        private float _resolvedDotDiameter;

        public string CountryId => countryId;

        /// <summary>DNA 버블 스폰 기준 월드 좌표(국가 중심 근사치). world.svg 폴리곤 중심에서 뽑은
        /// 값이라 항공/해운 허브 앵커링처럼 "대략 이 나라 어딘가"면 충분한 용도에는 적합하지만, 아래
        /// GetRandomDnaSpawnWorldPosition()처럼 실루엣 내부가 보장되지는 않는다 — 국가별 감염 점 데이터가
        /// 없을 때의 폴백으로만 쓸 것(Step 42 참고).</summary>
        public Vector3 DnaSpawnWorldPosition => transform.TransformPoint(dnaSpawnLocalOffset);

        /// <summary>
        /// [Step 42] DNA 버블이 실제로 스폰될 정확한 지점 하나를 무작위로 반환한다. InfectionDotDatabase
        /// 좌표(오프라인에서 국가 실루엣 알파마스크로 실측 검증된, 항상 국가 내부인 좌표)를 그대로
        /// 재사용하면 국가 크기에 관계없이 항상 실루엣 안에서 스폰된다는 게 보장된다.
        /// [Step 44] 예전엔 화면에 실제로 만들어둔 점 GameObject(_dotTransforms) 중에서 골랐는데,
        /// 이제 핫스팟 GameObject는 몇 개 안 되므로(3~10개) 그 대신 원본 좌표 배열(_allDotPoints, 32~412개)
        /// 에서 직접 뽑는다 — 시각적으로 보이는 핫스팟 개수와 DNA 버블이 스폰될 수 있는 지점의 다양성은
        /// 서로 무관해도 된다(오히려 버블은 핫스팟보다 더 다양한 위치에서 나오는 게 자연스럽다).
        /// </summary>
        public Vector3 GetRandomDnaSpawnWorldPosition()
        {
            if (_allDotPoints != null && _allDotPoints.Length > 0)
            {
                Vector2 p = _allDotPoints[UnityEngine.Random.Range(0, _allDotPoints.Length)];
                return transform.TransformPoint(p);
            }
            return DnaSpawnWorldPosition;
        }

        /// <summary>DNA 버블이 스폰 지점 주변에 흩어질 반경 — 감염 점 지름(_baseDotDiameter, 국가
        /// 면적에 비례해 계산된 값)에 맞춰 국가마다 다르게 잡는다. 감염 점 데이터가 없는 국가는 아주
        /// 작은 고정값으로 폴백(예전처럼 큰 고정 반경을 쓰면 같은 버그가 재발한다).</summary>
        public float DnaSpawnScatterRadius => _baseDotDiameter > 0f ? _baseDotDiameter * 0.6f : 0.03f;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<BoxCollider2D>();
            _renderer.color = healthyColor; // 알파 0 — 첫 UpdateVisual 전까지 아무것도 안 보임(의도된 동작)
            _renderer.sortingOrder = countrySortingOrder; // [Step 46] 레이어 순서 재정비, 위 필드 툴팁 참고
            ApplyCountryShape();
            // [Step 49] 좌표/지름 로딩은 hotspotsEnabled와 무관하게 항상 실행 — DNA 버블 스폰
            // (GetRandomDnaSpawnWorldPosition/DnaSpawnScatterRadius)이 이 데이터를 재사용하기 때문에,
            // 핫스팟을 꺼도 DNA 버블은 여전히 정확한 국가 실루엣 내부 좌표에서 스폰돼야 한다(Step 42에서
            // 고친 국경 이탈 버그가 다시 나면 안 됨).
            LoadDotData();
            if (dotsEnabled) SetupInfectionDots(); // [Step 50] 기본값 true — 위 dotsEnabled 툴팁 참고
            if (hotspotsEnabled) SetupHotspots(); // 기본값 false — 아래 hotspotsEnabled 툴팁 참고
        }

        /// <summary>[Step 49] SetupHotspots()에서 좌표/지름 로딩 부분만 분리 — hotspotsEnabled가 꺼져
        /// 있어도 DNA 버블 스폰용 데이터(_allDotPoints/_baseDotDiameter)는 항상 필요하다.</summary>
        private void LoadDotData()
        {
            var layout = InfectionDotDatabase.GetLayout(countryId);
            _allDotPoints = layout.Points;
            _baseDotDiameter = layout.Diameter;

            if (_allDotPoints.Length == 0)
                Debug.LogWarning($"[CountryView] {countryId} — InfectionDotPoints.json에 좌표가 없어 DNA 버블/감염 핫스팟이 폴백 위치를 사용합니다.");
        }

        /// <summary>
        /// [Step 35/36/41~43, Step 50 복원] 국가 실루엣 내부에 미리 계산해둔 좌표(InfectionDotDatabase)에
        /// 맞춰 점 오브젝트를 최대 maxInfectionDots개 만들어둔다. 처음엔 전부 scale 0(안 보임) 상태로
        /// 대기하다가 UpdateVisual()이 감염률에 비례해 몇 개를 "활성"으로 표시할지 정하면 Update()에서
        /// 그 개수만큼만 스케일을 0→1로 부드럽게 키운다(UpdateInfectionDots() 참고). 좌표/지름 로딩은
        /// Awake()에서 LoadDotData()가 먼저 해뒀으므로 여기선 이미 채워진 _allDotPoints/_baseDotDiameter를
        /// 그대로 쓴다(InfectionDotDatabase.GetLayout()을 중복 호출하지 않음).
        /// </summary>
        private void SetupInfectionDots()
        {
            int count = Mathf.Min(maxInfectionDots, _allDotPoints.Length);

            // [Step 53] "최소 크기 0.5 고정, 한국보다 큰 나라는 확대" — infectionDotDiameterScale(기본
            // 0.5)은 이제 가장 작은 나라(InfectionDotDatabase.MinDiameter, 현재 한국)에 적용되는 바닥값이다.
            // 자국 diameter가 그 최소값보다 클수록(=한국보다 국토가 클수록) 배율을 같은 비율로 키운다 —
            // 한국 자신은 ratio=1이라 정확히 바닥값(0.5)을 쓰고, 예를 들어 러시아는 원본 diameter가
            // 한국의 약 5.66배라 배율도 0.5*5.66≈2.83까지 커진다(Mathf.Max로 바닥 아래로는 안 내려가게 보호).
            float sizeRatio = _baseDotDiameter / InfectionDotDatabase.MinDiameter;
            float resolvedScale = Mathf.Max(infectionDotDiameterScale, infectionDotDiameterScale * sizeRatio);
            _resolvedDotDiameter = _baseDotDiameter * resolvedScale;

            _dotTransforms = new Transform[count];
            _dotCurrentScale = new float[count];

            if (count == 0) return; // 좌표 없음 — LoadDotData()에서 이미 경고 로그를 남겼으니 조용히 스킵.

            Sprite dotSprite = GetSharedInfectionDotSprite();

            for (int i = 0; i < count; i++)
            {
                var dotObject = new GameObject($"InfectionDot_{i}");
                dotObject.transform.SetParent(transform, false);
                dotObject.transform.localPosition = new Vector3(_allDotPoints[i].x, _allDotPoints[i].y, 0f);
                dotObject.transform.localScale = Vector3.zero; // 처음엔 안 보임

                var dotRenderer = dotObject.AddComponent<SpriteRenderer>();
                dotRenderer.sprite = dotSprite;
                dotRenderer.color = infectionDotColor;
                dotRenderer.sortingOrder = dotSortingOrder;

                _dotTransforms[i] = dotObject.transform;
                _dotCurrentScale[i] = 0f;
            }
        }

        /// <summary>
        /// [Step 44] InfectionDotDatabase(Resources/InfectionDotPoints.json)의 기존 좌표를 재사용해
        /// 국가당 소수(min~maxHotspotCount)의 큰 "핫스팟" 원만 만든다. 좌표 배열은 Step 36의 가중
        /// 라운드로빈 방식대로 앞쪽일수록 도시 위주로 정렬돼 있어서, 배열 앞에서부터 hotspotCount개만
        /// 골라 쓰면 자연스럽게 "이 나라에서 가장 중요한 지점들"이 앵커가 된다.
        ///
        /// 지름은 원본 데이터(전체 점 개수 기준 "면적의 약 70% 커버" 공식으로 미리 계산된 값)를
        /// sqrt(전체 개수/핫스팟 개수)만큼 키워서 쓴다 — 같은 면적 공식을 핫스팟 개수에 맞게 재적용한
        /// 것과 수학적으로 동일한 결과라 오프라인 스크립트를 다시 돌릴 필요가 없다.
        ///
        /// [Step 45] 이 계산값이 특정 국가에서 실제 국가 폭/높이보다 커져 이웃 나라 국경까지 침범하는
        /// 문제가 있었다 — _allDotPoints(전부 실루엣 내부 보장)의 바운딩박스로 이 국가의 실제 "좁은 쪽"
        /// 치수를 근사해, 지름이 그 치수의 maxHotspotDiameterFraction 비율을 넘지 못하도록 상한을 건다.
        /// </summary>
        private void SetupHotspots()
        {
            // [Step 49] 좌표/지름 로딩은 DNA 버블 스폰(GetRandomDnaSpawnWorldPosition)도 같이 쓰는 데이터라
            // hotspotsEnabled=false여도 항상 필요 — Awake()에서 LoadDotData()로 미리 분리해뒀으니 여기선
            // 이미 채워진 _allDotPoints/_baseDotDiameter를 그대로 쓴다.
            if (_allDotPoints.Length == 0)
            {
                _hotspotTransforms = System.Array.Empty<Transform>();
                _hotspotCurrentScale = System.Array.Empty<float>();
                return;
            }

            int count = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Sqrt(_allDotPoints.Length) / hotspotCountDivisor),
                minHotspotCount, maxHotspotCount);
            count = Mathf.Min(count, _allDotPoints.Length);

            float rawDiameter = _baseDotDiameter * Mathf.Sqrt((float)_allDotPoints.Length / count) * hotspotSizeBoost;

            // [Step 45] 국가 바운딩박스의 "좁은 쪽" 기준 상한 — 이웃 나라 침범 방지.
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < _allDotPoints.Length; i++)
            {
                Vector2 p = _allDotPoints[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            float minExtent = Mathf.Min(maxX - minX, maxY - minY);
            float diameterCap = minExtent > 0f ? minExtent * maxHotspotDiameterFraction : rawDiameter;

            _hotspotDiameter = Mathf.Min(rawDiameter, diameterCap);

            _hotspotTransforms = new Transform[count];
            _hotspotCurrentScale = new float[count];

            Sprite dotSprite = GetSharedInfectionDotSprite();

            for (int i = 0; i < count; i++)
            {
                var dotObject = new GameObject($"Hotspot_{i}");
                dotObject.transform.SetParent(transform, false);
                dotObject.transform.localPosition = new Vector3(_allDotPoints[i].x, _allDotPoints[i].y, 0f);
                dotObject.transform.localScale = Vector3.zero; // 처음엔 안 보임

                var dotRenderer = dotObject.AddComponent<SpriteRenderer>();
                dotRenderer.sprite = dotSprite;
                dotRenderer.color = hotspotColor;
                dotRenderer.sortingOrder = hotspotSortingOrder;

                _hotspotTransforms[i] = dotObject.transform;
                _hotspotCurrentScale[i] = 0f;
            }
        }

        /// <summary>
        /// 지름 1유닛짜리 부드러운 원형 스프라이트를 코드로 한 번만 생성해 모든 CountryView가 공유한다
        /// (국가마다 텍스처를 새로 만들면 48개 * 텍스처가 낭비 — 모바일 메모리 절약을 위해 static
        /// 캐시). 실제 표시 크기는 각 핫스팟 GameObject의 localScale로 조절한다.
        ///
        /// [Step 47] "감염 점이 아직도 다른 오브젝트를 가려서 안 보인다"는 반복된 피드백에 대응해 — 지금까지는
        /// 크기(hotspotSizeBoost)/상한(maxHotspotDiameterFraction)/알파(hotspotColor.a)만 계속 낮춰왔는데,
        /// 이 세 값을 아무리 낮춰도 스프라이트 자체가 가장자리 1px만 페이드되는 "거의 꽉 찬 단색 원판"이라
        /// 원 안쪽은 항상 균일하게 불투명했다 — 즉 "얼마나 크게 보이느냐"만 줄였지 "원 안에서는 무조건
        /// 아래가 안 보인다"는 근본 성질은 그대로였다. 셰이더/블렌드모드(예: 가산 블렌딩)를 바꾸는 방법도
        /// 있지만, 이 프로젝트는 URP + 에디터 미접속 상태라 커스텀 셰이더가 실제로 컴파일/렌더링되는지
        /// 확인할 방법이 없어(Step 44/45에서 같은 이유로 이미 두 번 배제한 선택) 블라인드로 셰이더를
        /// 바꾸는 리스크를 지지 않기로 했다. 대신 기존과 똑같이 검증된 방식(SpriteRenderer + 표준 알파
        /// 블렌드, 텍스처 생성 로직만 변경)으로 "중심만 진하고 가장자리로 갈수록 완전히 투명해지는" 부드러운
        /// 방사형 그라디언트(glow)를 만들었다 — 원 면적의 상당 부분이 항상 부분 투명 이하이므로, 크기를
        /// 더 줄이지 않아도 "다른 오브젝트를 완전히 가리는" 면적 자체가 구조적으로 줄어든다.
        ///
        /// [Step 52] Step 50에서 이 스프라이트를 개별 "점"(핫스팟보다 훨씬 작음)에 재사용하게 되면서,
        /// "점이 또렷하지 않고 부드러운 글로우/백광처럼 번져 보인다"는 피드백을 받았다 — 반지름 전체에
        /// 걸쳐 페이드되는 게 큰 핫스팟 원에는 맞는 절충이었지만 작은 점에는 과했던 것. 중심 60%는 완전
        /// 불투명한 코어로 유지하고 바깥 40%에서만 페이드하도록 바꿔 "또렷한 점 + 안티에일리어싱된 가장자리"
        /// 느낌으로 조정했다(아래 코드 참고) — 완전 하드엣지(Step47 이전)로 되돌아가지는 않는다.
        /// </summary>
        private static Sprite GetSharedInfectionDotSprite()
        {
            if (_sharedInfectionDotSprite != null) return _sharedInfectionDotSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float radius = size / 2f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(dist / radius); // 0=중심, 1=가장자리
                    // [Step 47] 이전엔 alpha = Clamp01(radius - dist)로 거의 전체가 불투명한 단색 원판이었다.
                    // SmoothStep(1,0,t)를 반지름 전체에 걸어서(중심부터 바로 옅어짐) "다른 오브젝트를 완전히
                    // 가리는" 영역을 구조적으로 줄였는데, [Step 52] 그 결과가 개별 점(Step 50 복원) 입장에서는
                    // "또렷한 점"이 아니라 "부드럽게 번지는 글로우/백광"처럼 보인다는 피드백을 받았다 — 핫스팟
                    // (큰 원, 지금은 꺼져 있음)엔 맞는 절충이었지만 작은 점에는 과했던 것. 중심 coreFraction
                    // (60%) 반경까지는 완전 불투명한 코어를 유지해 "점"처럼 또렷하게 보이게 하고, 남은 바깥
                    // 40% 구간에서만 SmoothStep으로 페이드해 Step47 이전의 완전 하드엣지(가장자리 1px만
                    // 안티에일리어싱)로 되돌아가지는 않도록 했다. 이 스프라이트는 핫스팟과 공유(static
                    // 캐시)라, 나중에 hotspotsEnabled를 다시 켜면 핫스팟도 이 더 또렷한 코어를 같이 쓰게 된다
                    // — 핫스팟을 재활성화할 일이 생기면 "가리는 문제"가 살짝 다시 커질 수 있음을 감안할 것.
                    const float coreFraction = 0.6f;
                    float alpha;
                    if (t <= coreFraction)
                    {
                        alpha = 1f;
                    }
                    else
                    {
                        float edgeT = (t - coreFraction) / (1f - coreFraction); // 0=코어 끝, 1=가장자리
                        alpha = Mathf.SmoothStep(1f, 0f, edgeT);
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();

            // pixelsPerUnit = size → 기본 스프라이트 지름이 1유닛. localScale로 실제 지름을 맞춘다.
            _sharedInfectionDotSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _sharedInfectionDotSprite;
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
        }

        private void Start()
        {
            if (WorldMap.Instance != null)
            {
                WorldMap.Instance.RegisterCountryView(this);
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
            _targetInfectionRatio = Mathf.Clamp01(infectionRatio);

            // [Step 41~43, Step 50 복원] 감염 점 개수 갱신 — CeilToInt라 감염자가 단 1명이라도 있으면
            // 즉시 점이 하나 나타난다. 색상 얼룩은 비율에 선형 비례라 초반엔 거의 안 보이는 문제를
            // dot 쪽에서 보완한다.
            if (_dotTransforms != null && _dotTransforms.Length > 0)
            {
                _targetActiveDotCount = infectionRatio <= 0f
                    ? 0
                    : Mathf.Clamp(Mathf.CeilToInt(infectionRatio * _dotTransforms.Length), 1, _dotTransforms.Length);
            }

            // [Step 54] "이 부류 디버그 로그 지워줘" 요청으로 국가별 목표색 갱신 로그를 제거했다 — 48개국이
            // 감염률/사망률 10%p 밴드가 바뀔 때마다(사실상 거의 매 틱, 국가 수만큼) 콘솔에 로그를 남겨
            // 스팸이 심했다. 밴드 추적 필드(_lastLoggedInfectionBand/_lastLoggedDeadBand)는 당장 다른
            // 용도가 없어 로그와 함께 정리해도 되지만, 나중에 디버깅용으로 다시 켤 수도 있어 필드 자체는
            // 남겨두고 갱신 로직만 비활성화했다.
        }

        private void Update()
        {
            if (!_hasTarget) return;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            _renderer.color = Color.Lerp(_renderer.color, _targetColor, 1f - Mathf.Exp(-colorTransitionSpeed * Time.deltaTime));

            if (dotsEnabled) UpdateInfectionDots(); // [Step 50] 기본값 true — 위 dotsEnabled 툴팁 참고
            if (hotspotsEnabled) UpdateHotspots(); // [Step 49] 기본값 false — 위 hotspotsEnabled 툴팁 참고
        }

        /// <summary>[Step 41~43, Step 50 복원] 인덱스 i &lt; _targetActiveDotCount인 점만 scale을 0→1로,
        /// 나머지는 1→0으로 부드럽게 전환한다. colorTransitionSpeed와 같은 Exp 감쇠 lerp라 프레임레이트에
        /// 무관하게 일정한 속도로 수렴한다.</summary>
        private void UpdateInfectionDots()
        {
            if (_dotTransforms == null || _dotTransforms.Length == 0) return;

            float t = 1f - Mathf.Exp(-dotTransitionSpeed * Time.deltaTime);
            for (int i = 0; i < _dotTransforms.Length; i++)
            {
                float target = i < _targetActiveDotCount ? 1f : 0f;
                if (Mathf.Approximately(_dotCurrentScale[i], target)) continue;

                float next = Mathf.Lerp(_dotCurrentScale[i], target, t);
                if (Mathf.Abs(next - target) < 0.01f) next = target; // 눈에 안 띄는 잔여값 스냅
                _dotCurrentScale[i] = next;

                float worldScale = next * _resolvedDotDiameter;
                _dotTransforms[i].localScale = new Vector3(worldScale, worldScale, 1f);
            }
        }

        /// <summary>
        /// [Step 44] 핫스팟 i는 감염률이 (i / 개수) 임계값을 넘어야 드러나기 시작한다(예전 점 시스템과
        /// 같은 "감염이 퍼질수록 하나씩 추가로 드러남" 느낌 유지) — 다만 드러난 뒤에는 크기가 0→1로
        /// 한 번만 켜지는 게 아니라, 감염률 자체에 비례해 minRevealedScaleFraction~1 사이를 계속
        /// 오간다. 즉 이미 드러난 핫스팟도 감염이 심해질수록 계속 커진다 — "새 지역으로 퍼짐"과
        /// "기존 지역이 더 심해짐"을 핫스팟 몇 개만으로 동시에 표현한다.
        /// </summary>
        private void UpdateHotspots()
        {
            if (_hotspotTransforms == null || _hotspotTransforms.Length == 0) return;

            int n = _hotspotTransforms.Length;
            float t = 1f - Mathf.Exp(-hotspotTransitionSpeed * Time.deltaTime);

            for (int i = 0; i < n; i++)
            {
                float revealThreshold = (float)i / n;
                float target = _targetInfectionRatio > 0f && _targetInfectionRatio >= revealThreshold
                    ? Mathf.Lerp(minRevealedScaleFraction, 1f, _targetInfectionRatio)
                    : 0f;

                if (Mathf.Approximately(_hotspotCurrentScale[i], target)) continue;

                float next = Mathf.Lerp(_hotspotCurrentScale[i], target, t);
                if (Mathf.Abs(next - target) < 0.01f) next = target; // 눈에 안 띄는 잔여값 스냅
                _hotspotCurrentScale[i] = next;

                float worldScale = next * _hotspotDiameter;
                _hotspotTransforms[i].localScale = new Vector3(worldScale, worldScale, 1f);
            }
        }

        // Step 28-2에서 국가를 탭해서 개별 팝업을 여는 인터랙션을 제거했었다(지도 위 실제 위치·크기
        // 그대로라 국가가 촘촘/작아서 탭 정확도 문제가 있었기 때문 — DevLog 참고). HUD 리디자인에서
        // HTML 프로토타입의 "Country Dock"(상시 표시 국가 정보 패널)을 실제 UI Toolkit HUD로
        // 이식하면서 트리거를 되살렸다 — CountryPopupController(모달 팝업)를 다시 쓰는 게 아니라
        // 신규 CountryDockController.cs(Hud.uxml의 country-dock 요소를 상시 갱신)가 이 이벤트를
        // 구독한다. WorldMapCameraController.WasDragging으로 지도 드래그 중 스친 클릭은 걸러내지만,
        // Step 28-2가 지적한 "국가가 촘촘해 탭 정확도가 낮다"는 근본 문제 자체는 그대로이므로 실기기
        // 플레이테스트로 오탭 빈도를 반드시 확인할 것(unity-editor-task.md 참고).
        private void OnMouseUpAsButton()
        {
            if (WorldMapCameraController.Instance != null && WorldMapCameraController.Instance.WasDragging)
                return;

            WorldMap.Instance?.HandleCountryClicked(countryId);
        }
    }
}
