using System;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 세계지도 위를 이동하는 비행기/배 한 대. 사용자 제공 "Global Transport Network Design" 문서의
    /// Transport Unit(Current Node/Destination Node/Progress/Speed/Infection Carrier Flag)을 그대로 구현.
    /// 비행기와 배는 완전히 같은 이동/도착 로직을 공유하고(공유 클래스), 속도·색상·감염 확률만
    /// TransportManager가 타입별로 다르게 넘겨준다.
    ///
    /// [Step 37] 원래는 절차적으로 그린 십자형(비행기)/납작한 선체형(배) 실루엣을 썼는데(BuildTriangleSprite/
    /// BuildHullSprite — 여전히 코드에 남아있고, 이모지 에셋 로드가 실패하면 폴백으로 쓰인다), 사용자가
    /// 후보 이모지를 보고 직접 골라서(✈️ 여객기 / 🚢 화물선) 실제 이모지 그래픽으로 교체했다. 이모지 PNG는
    /// Twemoji(emoji-datasource-twitter npm 패키지, 코드는 MIT/그래픽 자체는 CC-BY 4.0 — Docs/DevLog.md
    /// Step 37 참고, 상용 배포 시 저작자 표시 필요 여부 확인할 것) 64px 원본을 오프라인에서 그대로 복사해
    /// `Assets/Resources/TransportIcons/{plane,ship}.png`로 추가했다(Unity는 색 이모지 글리프를 폰트로
    /// 직접 렌더링하지 못해서 — world_base.png A* 계산이나 InfectionDotPoints처럼 "런타임에 못 하는 건
    /// 오프라인에서 미리 만들어 에셋으로 심는다"는 이 프로젝트의 반복된 패턴을 그대로 따름).
    ///
    /// 이모지는 이미 여러 색이 섞여 있어서 기존 방식(스프라이트 색 자체를 carrier/idle 색으로 곱해서
    /// 물들이기)을 그대로 쓰면 탁하게 보인다 — 그래서 색 틴트는 이모지 스프라이트 자체가 아니라 별도
    /// 표시로 옮겼다.
    ///
    /// [Step 38] 처음엔 이모지 뒤에 반투명 원형 halo를 깔았는데, 사용자 피드백("하이라이트 효과가 너무
    /// 큼")을 받아 걷어내고 대신 이모지 실루엣을 그대로 따라가는 "색상 윤곽선"으로 교체했다 — 오프라인에서
    /// 미리 만들어둔 실루엣 PNG(`{plane,ship}_outline.png`, RGB는 흰색 고정 + 원본과 같은 알파채널)를
    /// carrier/idle 색으로 물들인 뒤, 8방향(OutlineOffsetAngles)으로 몇 픽셀씩 어긋나게 겹쳐 그려서
    /// (GetAirOutlineSprite/GetSeaOutlineSprite) 본체 이모지 가장자리 바깥으로 살짝 삐져나온 부분만
    /// 보이게 하는 전통적인 "스프라이트 윤곽선" 트릭이다. 실루엣 PNG의 RGB를 흰색으로 고정해둔 이유는
    /// 이모지 원본 텍스처를 그대로 그 색으로 곱하면(원본 RGB × 틴트) 색이 탁하게 섞이기 때문 — 흰색
    /// × 틴트 = 틴트 그대로라 항상 깨끗한 단색 윤곽선이 나온다. 이 윤곽선 색이 Step 30-3에서 갈라놓은
    /// 항공=파랑/주황, 해운=초록/빨강 구분을 그대로 이어받는다. 이모지 자체 크기도 살짝 줄였다(iconScale).
    ///
    /// [Step 39] 사용자가 Twemoji 🚢 대신 직접 준비한 고해상도 화물선 이미지("Ship 1.png", Multiple
    /// 스프라이트 모드, pixelsPerUnit=2500)를 TransportIcons 폴더에 넣어서 GetSeaSprite()가 이를 최우선으로
    /// 시도하도록 교체(LoadFirstSprite 헬퍼로 LoadAll 후 첫 서브 스프라이트 사용 — 기존 ship.png는 폴백으로
    /// 그대로 남겨둠). 이 작업 중 윤곽선 두께 계산이 "텍스처 픽셀 수 / 스프라이트 pixelsPerUnit" 방식이라
    /// 스프라이트마다 pixelsPerUnit이 다르면(기존 이모지 260 vs Ship 1 2500) 같은 값이 완전히 다른 두께로
    /// 보이는 버그를 발견해 월드 유닛 고정값(outlineThicknessWorldUnits)으로 교체했다. 사용자가 이미
    /// iconScale을 직접 조정해뒀다고 밝혀서 그 값은 건드리지 않았다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TransportUnit : MonoBehaviour
    {
        private static Sprite _airSprite;
        private static Sprite _seaSprite;
        private static Sprite _airOutlineSprite;
        private static Sprite _seaOutlineSprite;
        private static bool _airSpriteLoadAttempted;
        private static bool _seaSpriteLoadAttempted;

        // Step 30-3에서 항공=파랑 계열/해운=초록 계열, carrier=주황/빨강으로 색상(hue) 자체를 확실히
        // 갈라놓았던 배색을 그대로 유지 — [Step 37]에선 halo에, [Step 38]부터는 윤곽선에 적용한다.
        private static readonly Color AirCarrierColor = new Color(1f, 0.5f, 0f, 1f);
        private static readonly Color AirIdleColor = new Color(0.15f, 0.55f, 1f, 0.95f);
        private static readonly Color SeaCarrierColor = new Color(0.85f, 0.12f, 0.12f, 1f);
        private static readonly Color SeaIdleColor = new Color(0.1f, 0.65f, 0.4f, 0.95f);

        // [Step 37] 이모지 원본 그림이 기본적으로 향하는 방향 — FaceTravelDirection의 Atan2 회전
        // 규칙(0도=+X)은 "스프라이트가 그린 그대로 +X를 향한다"고 가정하는데, Twemoji ✈️는 오른쪽 위
        // 대각선(약 45도)을, 🚢는 왼쪽(180도)을 향하고 있어 그 차이만큼 보정해야 실제 이동 방향과
        // 이모지가 향하는 방향이 맞아떨어진다(PCA로 계산한 축 각도 기준, 오프라인에서 확인).
        private const float AirSpriteBaseFacingDeg = 45f;
        private const float SeaSpriteBaseFacingDeg = 180f;

        // [Step 38] 8방향 윤곽선 오프셋 복사본의 회전각(도) — 등간격 45도씩.
        private static readonly float[] OutlineOffsetAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        [SerializeField, Tooltip("이모지 본체 크기 배율. Step 38에서 '비행기/배 이미지가 너무 크다'는 " +
            "피드백을 받아 기본값을 1보다 살짝 작게 잡았다 — 전체 배율이라 아웃라인도 같이 줄어든다.")]
        private float iconScale = 0.8f;

        [SerializeField, Tooltip("색상 윤곽선 두께 — 월드 유닛 기준(고정값). [Step 39] 원래는 " +
            "'텍스처 픽셀 수 / 스프라이트 pixelsPerUnit'으로 계산했는데, 사용자가 새 배 이미지(Ship 1, " +
            "pixelsPerUnit=2500)를 넣으면서 같은 픽셀값이 스프라이트마다 완전히 다른 두께로 보이는 문제가 " +
            "생겼다(예: 기존 이모지는 260, 새 이미지는 2500이라 같은 3px이 거의 10배 차이 나는 두께가 됨) " +
            "— 스프라이트 해상도와 무관하게 항상 같은 두께로 보이도록 월드 유닛 고정값으로 교체했다.")]
        private float outlineThicknessWorldUnits = 0.012f;

        private SpriteRenderer _renderer;
        private SpriteRenderer[] _outlineRenderers;
        private bool _isEmojiSprite;

        /// <summary>도착할 때마다 발행 — TransportManager가 감염 전파 판정 + 다음 구간 배정을 수행.</summary>
        public event Action<TransportUnit> OnArrived;

        public TransportHubType HubType { get; private set; }
        public string CurrentHubId { get; private set; }
        public string DestinationHubId { get; private set; }

        /// <summary>이번 구간 출발 시점에 실제로 감염을 옮길 수 있는 상태였는지 (설계 문서 "Infection Carrier Flag").</summary>
        public bool IsCarrier { get; private set; }

        // Step 30: 직선 2점(from/to) 대신 경유점을 포함한 여러 점을 순서대로 지나가도록 확장 — 해운/일부
        // 장거리 항공 노선이 대륙을 관통하는 문제(DevLog Step 30 참고)를 TransportManager가 계산한 경로
        // (Vector3[] path)를 그대로 따라가게 해서 해결한다. path.Length == 2(경유점 없음)면 기존 동작과
        // 완전히 동일 — 대부분의 근거리 노선은 여전히 직선이다.
        private Vector3[] _path;
        private float[] _cumulativeDistance; // _cumulativeDistance[i] = path[0]부터 path[i]까지 누적 거리
        private float _totalDistance;
        private float _traveled;
        private float _speed;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sortingOrder = 60; // DNA 버블(연출) 아래, 국가 오버레이 위 정도의 어중간한 레이어 — 필요시 조정

            // [Step 38] 이모지 본체 크기를 살짝 줄임 — 자식(윤곽선 복사본)도 이 루트 transform 기준
            // 로컬 좌표라 같이 축소돼 비율이 항상 맞는다.
            transform.localScale = new Vector3(iconScale, iconScale, 1f);

            // [Step 38] carrier/idle 구분을 halo(반투명 원) 대신 이모지 실루엣을 따라가는 색상 윤곽선으로
            // 표현 — 실루엣 스프라이트를 8방향으로 몇 픽셀씩 어긋나게 겹쳐 그려서 본체 가장자리 바깥으로
            // 삐져나온 부분만 보이게 하는 방식(전통적인 "스프라이트 아웃라인" 트릭). 정확한 오프셋 위치는
            // 스프라이트가 정해지는 BeginLeg()에서 계산한다(항공/해운 스프라이트의 pixelsPerUnit이 다를
            // 수 있어서).
            _outlineRenderers = new SpriteRenderer[OutlineOffsetAngles.Length];
            for (int i = 0; i < _outlineRenderers.Length; i++)
            {
                var outlineObject = new GameObject($"Outline_{i}");
                outlineObject.transform.SetParent(transform, false);
                var outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
                outlineRenderer.sortingOrder = 59; // 이모지 본체(60)보다 한 단계 아래
                _outlineRenderers[i] = outlineRenderer;
            }
        }

        /// <summary>
        /// 새 구간 시작. 풀에서 꺼내질 때뿐 아니라 도착 직후 다음 목적지로 이어갈 때도 호출된다.
        /// speed는 월드 유닛/초 — TransportManager가 항공/해운별 기본 속도를 넘겨준다.
        /// path는 출발 허브부터 도착 허브까지(양 끝 포함) 순서대로 지나갈 지점들 — 최소 2개(직선).
        /// </summary>
        public void BeginLeg(string fromHubId, string toHubId, Vector3[] path,
            TransportHubType hubType, float speed, bool isCarrier)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            CurrentHubId = fromHubId;
            DestinationHubId = toHubId;
            _path = path != null && path.Length >= 2 ? path : new[] { transform.position, transform.position };
            HubType = hubType;
            _speed = Mathf.Max(0.05f, speed);
            IsCarrier = isCarrier;

            _cumulativeDistance = new float[_path.Length];
            _cumulativeDistance[0] = 0f;
            for (int i = 1; i < _path.Length; i++)
                _cumulativeDistance[i] = _cumulativeDistance[i - 1] + Vector3.Distance(_path[i - 1], _path[i]);
            _totalDistance = _cumulativeDistance[_cumulativeDistance.Length - 1];
            _traveled = 0f;

            _renderer.sprite = hubType == TransportHubType.Air ? GetAirSprite() : GetSeaSprite();
            // GetAirSprite/GetSeaSprite가 방금 _airIsEmoji/_seaIsEmoji를 확정지었다 — 이모지가 로드됐으면
            // 본체는 원본 색 그대로(흰색 틴트=무변화) 두고 윤곽선으로만 carrier/idle을 표시하지만,
            // 폴백(절차적 도형)이면 예전처럼 본체 자체를 carrier/idle 색으로 물들여야 구분이 된다(폴백
            // 도형엔 애초에 윤곽선용 실루엣 에셋이 따로 없음).
            _isEmojiSprite = hubType == TransportHubType.Air ? _airIsEmoji : _seaIsEmoji;
            Color stateColor = hubType == TransportHubType.Air
                ? (isCarrier ? AirCarrierColor : AirIdleColor)
                : (isCarrier ? SeaCarrierColor : SeaIdleColor);
            _renderer.color = _isEmojiSprite ? Color.white : stateColor;

            // [Step 38] 윤곽선 8개 복사본 갱신 — 폴백일 땐 아예 꺼둔다(본체가 이미 물들어 있어서 필요 없음).
            Sprite outlineSprite = _isEmojiSprite
                ? (hubType == TransportHubType.Air ? GetAirOutlineSprite() : GetSeaOutlineSprite())
                : null;
            bool showOutline = _isEmojiSprite && outlineSprite != null;
            float offsetMagnitude = showOutline ? outlineThicknessWorldUnits : 0f;
            for (int i = 0; i < _outlineRenderers.Length; i++)
            {
                var outlineRenderer = _outlineRenderers[i];
                outlineRenderer.gameObject.SetActive(showOutline);
                if (!showOutline) continue;

                outlineRenderer.sprite = outlineSprite;
                outlineRenderer.color = stateColor;
                float rad = OutlineOffsetAngles[i] * Mathf.Deg2Rad;
                outlineRenderer.transform.localPosition =
                    new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * offsetMagnitude;
            }

            transform.position = _path[0];
            FaceTravelDirection(_path[0], _path[Mathf.Min(1, _path.Length - 1)]);
        }

        private void Update()
        {
            if (_totalDistance <= 0.0001f)
            {
                transform.position = _path[_path.Length - 1];
                OnArrived?.Invoke(this);
                return;
            }

            _traveled += _speed * Time.deltaTime;
            if (_traveled >= _totalDistance)
            {
                _traveled = _totalDistance;
                transform.position = _path[_path.Length - 1];
                OnArrived?.Invoke(this);
                return;
            }

            // 현재 위치가 몇 번째 구간에 있는지 찾는다 — 경유점이 많아야 5~6개라 선형 탐색으로 충분.
            int segment = 0;
            while (segment < _cumulativeDistance.Length - 2 && _cumulativeDistance[segment + 1] < _traveled)
                segment++;

            Vector3 segFrom = _path[segment];
            Vector3 segTo = _path[segment + 1];
            float segLength = _cumulativeDistance[segment + 1] - _cumulativeDistance[segment];
            float segProgress = segLength > 0.0001f ? (_traveled - _cumulativeDistance[segment]) / segLength : 1f;

            transform.position = Vector3.LerpUnclamped(segFrom, segTo, segProgress);
            FaceTravelDirection(segFrom, segTo);
        }

        /// <summary>이동 방향을 향해 스프라이트를 회전 — 원본 게임처럼 비행기/배가 실제로 그 방향으로 "가는" 느낌.
        /// 경유점이 있는 경로에서는 구간이 바뀔 때마다 방향도 같이 꺾인다.
        /// [Step 37] 절차적 스프라이트(BuildTriangleSprite/BuildHullSprite)는 항상 +X를 바라보게 그렸지만,
        /// 이모지 그림은 그렇지 않다(✈️는 오른쪽 위 대각선, 🚢는 왼쪽) — HubType별 기준 각도(AirSpriteBaseFacingDeg/
        /// SeaSpriteBaseFacingDeg)만큼 빼줘서 "그림이 실제로 그려진 방향"과 "0도" 기준을 맞춘다.</summary>
        private void FaceTravelDirection(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) return;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // 폴백(절차적 도형)은 항상 +X를 바라보게 그려서 보정이 필요 없다 — 이모지일 때만 기준각을 뺀다.
            float baseFacingDeg = _isEmojiSprite
                ? (HubType == TransportHubType.Air ? AirSpriteBaseFacingDeg : SeaSpriteBaseFacingDeg)
                : 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg - baseFacingDeg);
        }

        // ---- 스프라이트 로드 (이모지 우선, 실패 시 절차적 도형 폴백) ----

        private static bool _airIsEmoji;
        private static bool _seaIsEmoji;

        /// <summary>Resources/TransportIcons/plane.png(✈️) 로드 시도 — 실패하면 기존 절차적 십자형으로 폴백.</summary>
        private static Sprite GetAirSprite()
        {
            if (_airSprite != null) return _airSprite;
            if (!_airSpriteLoadAttempted)
            {
                _airSpriteLoadAttempted = true;
                _airSprite = Resources.Load<Sprite>("TransportIcons/plane");
                _airIsEmoji = _airSprite != null;
                if (_airSprite == null)
                    Debug.LogWarning("[TransportUnit] TransportIcons/plane.png을 찾지 못해 절차적 비행기 도형으로 폴백합니다.");
            }
            if (_airSprite == null)
                _airSprite = BuildTriangleSprite();
            return _airSprite;
        }

        /// <summary>[Step 39] Resources/TransportIcons/Ship 1.png(사용자 제공 고해상도 화물선) 로드 시도 —
        /// Multiple 스프라이트 모드라 LoadFirstSprite(LoadAll 기반)로 꺼낸다. 실패하면 기존 Twemoji
        /// ship.png(🚢)로, 그것도 실패하면 절차적 선체형으로 순서대로 폴백.</summary>
        private static Sprite GetSeaSprite()
        {
            if (_seaSprite != null) return _seaSprite;
            if (!_seaSpriteLoadAttempted)
            {
                _seaSpriteLoadAttempted = true;
                _seaSprite = LoadFirstSprite("TransportIcons/Ship 1");
                if (_seaSprite == null)
                    _seaSprite = Resources.Load<Sprite>("TransportIcons/ship");
                _seaIsEmoji = _seaSprite != null;
                if (_seaSprite == null)
                    Debug.LogWarning("[TransportUnit] TransportIcons/Ship 1.png, ship.png를 모두 찾지 못해 절차적 선체 도형으로 폴백합니다.");
            }
            if (_seaSprite == null)
                _seaSprite = BuildHullSprite();
            return _seaSprite;
        }

        /// <summary>Resources/TransportIcons/plane_outline.png(RGB 흰색 고정 + ✈️와 동일한 알파채널) 로드.
        /// GetAirSprite()가 먼저 호출돼 _airIsEmoji가 true로 확정된 경우에만 호출부에서 사용한다.</summary>
        private static Sprite GetAirOutlineSprite()
        {
            if (_airOutlineSprite == null)
                _airOutlineSprite = Resources.Load<Sprite>("TransportIcons/plane_outline");
            return _airOutlineSprite;
        }

        /// <summary>[Step 39] Resources/TransportIcons/Ship 1_outline.png(Ship 1과 동일한 크롭+PPU, RGB
        /// 흰색 고정 실루엣) 로드 시도 — Multiple 모드라 LoadFirstSprite로 꺼낸다. 실패하면 기존 Twemoji
        /// ship_outline.png로 폴백.</summary>
        private static Sprite GetSeaOutlineSprite()
        {
            if (_seaOutlineSprite == null)
            {
                _seaOutlineSprite = LoadFirstSprite("TransportIcons/Ship 1_outline");
                if (_seaOutlineSprite == null)
                    _seaOutlineSprite = Resources.Load<Sprite>("TransportIcons/ship_outline");
            }
            return _seaOutlineSprite;
        }

        /// <summary>[Step 39] Multiple 스프라이트 모드 텍스처(예: 사용자가 Unity 에디터에서 직접 슬라이스한
        /// "Ship 1.png")는 Resources.Load&lt;Sprite&gt;로는 못 찾는다 — CountryView.ApplyCountryShape()와
        /// 같은 이유로 LoadAll을 쓰고 첫 번째 서브 스프라이트를 꺼낸다.</summary>
        private static Sprite LoadFirstSprite(string path)
        {
            var sprites = Resources.LoadAll<Sprite>(path);
            return sprites != null && sprites.Length > 0 ? sprites[0] : null;
        }

        /// <summary>
        /// +X 방향을 가리키는 "십자형(dart)" 비행기 실루엣 — Atan2 회전 규칙(0도=+X)과 그대로 맞아떨어진다.
        /// 가느다란 동체(fuselage)가 기수 쪽으로 갈수록 뾰족해지고, 중간에 동체보다 훨씬 넓은 날개(wing)
        /// 밴드가 가로질러 튀어나와 있어(세로로 거의 전체 높이) 실제 비행기 아이콘처럼 읽힌다 — 배(선체)
        /// 모양과는 실루엣 자체가 확실히 다르다.
        /// </summary>
        private static Sprite BuildTriangleSprite()
        {
            const int w = 32, h = 20;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            float fuselageHalf = h * 0.14f; // 얇은 동체
            float noseStart = 0.62f; // 이 지점부터 기수로 갈수록 뾰족해짐
            int wingStart = Mathf.RoundToInt(w * 0.32f);
            int wingEnd = Mathf.RoundToInt(w * 0.5f);
            float wingHalf = h * 0.48f; // 날개는 동체보다 훨씬 넓게(거의 전체 높이)
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = x / (float)(w - 1); // 0(꼬리) -> 1(기수)
                    float fuselageHalfHeight = t <= noseStart
                        ? fuselageHalf
                        : fuselageHalf * (1f - (t - noseStart) / (1f - noseStart));
                    bool inFuselage = Mathf.Abs(y - h / 2f) <= fuselageHalfHeight;
                    bool inWing = x >= wingStart && x <= wingEnd && Mathf.Abs(y - h / 2f) <= wingHalf;
                    bool inside = inFuselage || inWing;
                    pixels[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            // [Step 30-5] 감염 보유(빨간색) 유닛이 눈에 잘 띄어야 하는데 유닛이 너무 크면 지도 전체가
            // 산만해진다는 피드백 — pixelsPerUnit을 70→150으로 올려(텍스처 해상도는 그대로 두고 화면상
            // 실제 크기만) 이전보다 절반 이하로 축소.
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 150f);
        }

        /// <summary>
        /// +X 방향 이물(뱃머리)을 가진 길고 납작한 선체 모양(배) — 비행기보다 훨씬 긴 가로세로 비율(폭:높이
        /// ≈ 4.4:1, 비행기는 1.6:1)이라 실루엣만으로도 "길게 물살을 가르는 배"와 "짧고 뾰족한 비행기"가
        /// 뚜렷이 구분된다.
        /// </summary>
        private static Sprite BuildHullSprite()
        {
            const int w = 40, h = 9;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color32[w * h];
            float bowStart = w * 0.72f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inside;
                    if (x < bowStart)
                    {
                        inside = true; // 선체 몸통 — 꽉 찬 직사각형
                    }
                    else
                    {
                        float t = (x - bowStart) / (w - bowStart); // 0~1, 뱃머리로 갈수록 뾰족해짐
                        float halfHeight = (h / 2f) * (1f - t);
                        inside = Mathf.Abs(y - h / 2f) <= halfHeight;
                    }
                    pixels[y * w + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 150f);
        }
    }
}
