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
    /// 별도 프리팹/스프라이트 에셋 없이 동작하도록, 첫 사용 시 아주 작은 십자형(비행기)/납작한 선체형(배)
    /// 텍스처를 코드로 직접 그려서 씀(Step 30-3에서 삼각형/타원형이 서로 구분 안 된다는 피드백을 받아
    /// 형태·색상·크기를 전부 갈라놓았다 — BuildTriangleSprite/BuildHullSprite 참고) — 이 프로젝트의
    /// FloatingTextEffect.cs(내장 폰트만 사용)와 같은 "에셋 임포트/씬 배선 없이 바로 동작" 철학을 따른다.
    /// 나중에 실제 아이콘으로 바꾸고 싶다면 TransportManager 인스펙터에 스프라이트 필드를 추가해
    /// 덮어쓰면 된다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class TransportUnit : MonoBehaviour
    {
        private static Sprite _airSprite;
        private static Sprite _seaSprite;

        // [Step 30-3] 사용자 피드백: "지금 움직이는 도형이 배인지 비행기인지 눈에 안 들어와" — 기존에는
        // 두 모양(삼각형/선체)의 가로세로 비율이 비슷하고(28x16 vs 30x12), 색도 옅은 하늘색 vs 옅은
        // 연두색으로 채도·명도가 비슷해 지도가 축소된 상태에서는 거의 구분이 안 됐다. 그래서
        // (1) 비행기는 동체+날개가 있는 "십자형(dart)" 실루엣으로 바꿔 형태 자체를 다르게 하고,
        // (2) 배는 훨씬 더 길고 납작한 비율로 바꿔 "짧고 뾰족한 비행기" vs "길고 평평한 배"가 실루엣만
        // 봐도 구분되게 했다. (3) 색도 항공=파랑 계열/해운=초록 계열로 색상 자체(hue)를 확실히 갈라
        // 놓고 채도·불투명도를 높였다. (4) Sprite.Create의 pixelsPerUnit을 100→70으로 낮춰 화면상
        // 크기도 키웠다(BuildTriangleSprite/BuildHullSprite 참고).
        private static readonly Color AirCarrierColor = new Color(1f, 0.5f, 0f, 1f);
        private static readonly Color AirIdleColor = new Color(0.15f, 0.55f, 1f, 0.95f);
        private static readonly Color SeaCarrierColor = new Color(0.85f, 0.12f, 0.12f, 1f);
        private static readonly Color SeaIdleColor = new Color(0.1f, 0.65f, 0.4f, 0.95f);

        private SpriteRenderer _renderer;

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
            _renderer.color = hubType == TransportHubType.Air
                ? (isCarrier ? AirCarrierColor : AirIdleColor)
                : (isCarrier ? SeaCarrierColor : SeaIdleColor);

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
        /// 경유점이 있는 경로에서는 구간이 바뀔 때마다 방향도 같이 꺾인다.</summary>
        private void FaceTravelDirection(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            if (dir.sqrMagnitude < 0.0001f) return;
            float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        // ---- 절차적 스프라이트 생성 (에셋 임포트 불필요) ----

        private static Sprite GetAirSprite()
        {
            if (_airSprite == null)
                _airSprite = BuildTriangleSprite();
            return _airSprite;
        }

        private static Sprite GetSeaSprite()
        {
            if (_seaSprite == null)
                _seaSprite = BuildHullSprite();
            return _seaSprite;
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
