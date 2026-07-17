using System.Collections.Generic;
using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Utils;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 사용자 제공 "Global Transport Network Design" 문서 구현 — 항공 15개 + 해운 15개 허브로 구성된
    /// 글로벌 교통망. 비행기/배(TransportUnit)가 허브 사이를 실제로 이동하는 모습을 보여주고, 도착할
    /// 때마다 감염 전파를 굴린다.
    ///
    /// 이 매니저가 SimulationManager의 기존 SpreadBetweenCountries() 중 "국가 간 항공/해운 전파"
    /// 부분(추상적인 매틱 확률 판정)을 대체한다 — 같은 개념을 눈에 보이는 실제 이동체로 바꾼 것이므로
    /// 두 시스템을 동시에 돌리면 항공/해운 전파가 이중으로 적용돼 밸런스가 깨진다. SimulationManager는
    /// 육상 국경(landBorderSpreadChance)만 계속 담당한다.
    ///
    /// 다른 DontDestroyOnLoad 매니저들과 같은 패턴 — 씬 리로드(재시작)에도 살아남고,
    /// GameDataBootstrapper가 ResetForNewGame()을 호출해 이전 판에 떠 있던 유닛들을 정리한다.
    /// </summary>
    public class TransportManager : MonoBehaviour
    {
        public static TransportManager Instance { get; private set; }

        // [Step 30-5] 감염 보유 유닛(빨간색)이 도착해야 전파가 일어나는 걸 눈으로 확인할 수 있어야
        // 하는데, 기존 목표치(50~200)로는 지도 위에 배/비행기가 너무 많이 떠 있어서 어떤 게 빨간색인지
        // 눈에 잘 안 들어온다는 피드백을 받음. 최소/최대/틱당 스폰 수를 전부 절반 이하로 줄여
        // "빨간 유닛 하나가 눈에 띄는" 밀도로 낮췄다.
        // [Step 33] "여전히 비행기/배가 많은 듯" 피드백으로 한 번 더 절반가량 하향(10~70→5~35,
        // 틱당 스폰 6→3). 틱 간격은 SimulationManager 기준 1초 고정이라, 이 값들이 그대로 "초당 최대
        // 생성 수"이자 "정상 상태 유지 개체 수"가 된다.
        [Header("허브/유닛 규모 (Step 33: 5~35, 원래 목표 50~200→10~70→5~35)")]
        [SerializeField, Tooltip("전염병이 전혀 퍼지지 않은 평시에도 떠 있는 최소 교통량 — 지도가 죽어보이지 않도록.")]
        private int minActiveUnits = 5;
        [SerializeField] private int maxActiveUnits = 35;
        [SerializeField, Tooltip("전 세계 감염률(0~1)에 이 배율을 곱해 목표 활성 유닛 수를 정한다 — 세계 인구 대비 " +
            "감염 비율은 대유행 중에도 대개 몇 % 수준이라, 그대로 쓰면 활성 유닛 목표가 거의 항상 최소치에 " +
            "머문다. 배율을 곱해 '어느 정도만 퍼져도 하늘길이 눈에 띄게 붐빈다'는 체감을 만든다.")]
        private float infectionVisibilityScale = 25f;
        [SerializeField] private int maxSpawnPerTick = 3;
        [SerializeField] private int minHopsPerUnit = 3;
        [SerializeField] private int maxHopsPerUnit = 6;

        [Header("속도 (월드 유닛/초 — 설계 문서: 항공은 해운의 3~5배)")]
        [SerializeField] private float airSpeed = 2.4f;
        [SerializeField] private float seaSpeed = 0.6f;

        [Header("도착 시 감염 전파 확률/전파량 (설계 문서: 항공=고빈도/저물량, 해운=저빈도/고물량)")]
        [SerializeField, Range(0f, 1f)] private float airArrivalInfectionChance = 0.35f;
        [SerializeField, Range(0f, 1f)] private float seaArrivalInfectionChance = 0.15f;
        [SerializeField] private long airSeedAmount = 10;
        [SerializeField] private long seaSeedAmount = 20;

        // [Step 33] 사용자 피드백: "감염자가 있는 국가면 무조건 감염된 비행기/배가 나온다" — 기존
        // IsCountryInfected()는 출발 허브의 대표 국가 infectedCount가 1명이라도 있으면 항상 true였다
        // (버그는 아니고 설계상 "감염 여부"만 boolean으로 봤던 것). 인구 수천만~수억 국가에서 1명 감염
        // 됐다고 모든 항공/해운편이 매번 carrier가 되는 건 비현실적이라는 지적 — 국가 감염 "비율"에
        // 비례한 확률로 바꿔서, 초기/미미한 감염 단계에서는 carrier가 드물게만 나오고 감염이 널리
        // 퍼질수록 점점 더 자주 나오도록 완화했다.
        [SerializeField, Tooltip("출발 허브 대표 국가의 감염 비율(감염자/생존인구)에 이 배율을 곱해 해당 " +
            "유닛이 carrier가 될 확률로 쓴다(1로 클램프). 예: 배율 25면 감염 비율 4%부터 사실상 항상 " +
            "carrier, 0.1%면 2.5% 확률로만 carrier.")]
        private float carrierChanceScale = 25f;

        // [Step 40] 원래 TransportUnit 자신의 인스펙터 필드였는데, TransportUnit은 프리팹 없이 코드로만
        // 생성돼(AddComponent) 인스펙터에서 값을 바꿔도 Play 종료 시 사라졌다 — "이미지 크기를 어디서
        // 조정하냐"는 질문에 답이 될, 실제로 저장되는 위치가 필요해서 TransportManager(씬에 실제로
        // 존재하는 컴포넌트)로 옮겼다. 비행기/배 이모지 본체 크기 배율 — 1이 원본 텍스처 크기.
        [SerializeField, Tooltip("비행기/배 이모지 본체 크기 배율(1=원본 텍스처 크기). 여기서 조정하면 " +
            "다음 구간부터 즉시 반영되고 Play 종료 후에도 씬에 저장된다 — 이미지 파일 자체의 픽셀 크기나 " +
            "Pixels Per Unit을 바꿀 필요 없음.")]
        private float iconScale = 0.8f;

        [Header("경로선 시각화")]
        // [Step 48] "감염 점에 가려서 노선이 안 보인다" 재신고 — sortingOrder 자체는 재검증 결과
        // 이미 올바르게 노선(30) > 핫스팟(10)이었다(레이어 순서 버그 아님). 실제 원인은 대비 문제였음:
        // 알파 0.18짜리 얇은(0.02) 선이 alpha-blend되면 아래에 있는 크고 채도 높은 빨간 핫스팟이
        // 82% 그대로 비쳐 보여서, 그리는 순서는 맞아도 육안으로는 "핫스팟한테 먹힌 것"처럼 보였다.
        // 알파/두께를 올려 겹쳐도 선이 뚜렷하게 남도록 함.
        [SerializeField] private Color airRouteColor = new Color(0.55f, 0.85f, 1f, 0.55f);
        [SerializeField] private Color seaRouteColor = new Color(0.55f, 0.8f, 0.6f, 0.55f);
        [SerializeField] private float routeLineWidth = 0.032f;

        private List<TransportHub> _hubs;
        private Dictionary<string, TransportHub> _hubLookup;
        private Dictionary<string, Vector3> _hubWorldPositions;
        private bool _hubPositionsResolved;
        private bool _routesDrawn;

        // Step 30: 직선 대신 경유점을 거치는 노선용 테이블 — key는 PairKey()(사전순 작은 쪽 id가 앞)
        // 형식이고 값은 "작은 쪽 → 큰 쪽" 방향 경유점(로컬 오프셋, WorldMap.ToWorldPosition으로 변환).
        // 자세한 설계 근거는 DefaultTransportHubFactory.BuildSeaWaypoints()/BuildAirWaypoints() 참고.
        private Dictionary<string, Vector2[]> _seaWaypoints;
        private Dictionary<string, Vector2[]> _airWaypoints;

        private Transform _routesParent;
        private Transform _unitsParent;
        private ObjectPool<TransportUnit> _pool;

        private readonly List<TransportUnit> _active = new List<TransportUnit>();
        private readonly Dictionary<TransportUnit, int> _remainingHops = new Dictionary<TransportUnit, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _hubs = DefaultTransportHubFactory.BuildDefaultHubs();
            _hubLookup = new Dictionary<string, TransportHub>();
            foreach (var hub in _hubs)
                _hubLookup[hub.id] = hub;
            _hubWorldPositions = new Dictionary<string, Vector3>();
            _seaWaypoints = DefaultTransportHubFactory.BuildSeaWaypoints();
            _airWaypoints = DefaultTransportHubFactory.BuildAirWaypoints();

            _routesParent = new GameObject("TransportRoutes").transform;
            _routesParent.SetParent(transform);
            _unitsParent = new GameObject("TransportUnits").transform;
            _unitsParent.SetParent(transform);

            var template = new GameObject("TransportUnit_Template");
            template.SetActive(false);
            template.transform.SetParent(transform);
            var templateUnit = template.AddComponent<TransportUnit>();
            _pool = new ObjectPool<TransportUnit>(templateUnit, _unitsParent, prewarmCount: 30);
        }

        private void OnEnable() => Subscribe();
        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (SimulationManager.Instance == null) return;
            SimulationManager.Instance.OnTickCompleted -= HandleTick;
            SimulationManager.Instance.OnTickCompleted += HandleTick;
        }

        private void OnDisable()
        {
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnTickCompleted -= HandleTick;
        }

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 씬은 리로드되지만 이 매니저는
        /// DontDestroyOnLoad라 이전 판에 떠 있던 비행기/배가 그대로 남아있게 된다 — 전부 풀로 되돌린다.
        /// 허브 그래프/좌표 자체는 고정 데이터라 다시 만들 필요 없음(_hubPositionsResolved는 그대로 둔다 —
        /// 씬이 리로드돼도 국가 배치는 항상 동일해서 좌표가 달라지지 않는다).
        /// </summary>
        public void ResetForNewGame()
        {
            foreach (var unit in new List<TransportUnit>(_active))
                ReleaseUnit(unit);
            _active.Clear();
            _remainingHops.Clear();
        }

        private void HandleTick(WorldState state)
        {
            if (!_hubPositionsResolved && !TryResolveHubPositions())
                return; // WorldMap/CountryView가 아직 준비 안 됨 — 다음 틱에 재시도

            if (!_routesDrawn)
            {
                DrawRouteLines();
                _routesDrawn = true;
            }

            float infectionRatio = state.totalPopulation > 0 ? (float)state.infectedCount / state.totalPopulation : 0f;
            float t = Mathf.Clamp01(infectionRatio * infectionVisibilityScale);
            int target = Mathf.RoundToInt(Mathf.Lerp(minActiveUnits, maxActiveUnits, t));

            int spawnCount = Mathf.Min(maxSpawnPerTick, Mathf.Max(0, target - _active.Count));
            for (int i = 0; i < spawnCount; i++)
                TrySpawnUnit();
        }

        private bool TryResolveHubPositions()
        {
            if (WorldMap.Instance == null || WorldDataManager.Instance == null) return false;

            var resolved = new Dictionary<string, Vector3>();
            foreach (var hub in _hubs)
            {
                // [Step 32] 항공 허브는 국가 앵커를 거치지 않고 WorldMap 절대 좌표를 바로 쓴다(실제 공항
                // 위경도 기반 — DefaultTransportHubFactory.Air() 참고). 국가 View 준비 여부와 무관하게
                // 좌표를 계산할 수 있지만, 그래도 대응 국가가 아직 등록되지 않았으면(다른 허브 대기 사유와
                // 동일하게) 한 틱 더 기다린다 — isAirportOpen 등 게임 로직이 country를 참조하기 때문.
                if (hub.useAbsoluteWorldOffset)
                {
                    if (WorldMap.Instance.GetView(hub.countryId) == null) return false;
                    resolved[hub.id] = WorldMap.Instance.ToWorldPosition(hub.localOffset);
                    continue;
                }

                var view = WorldMap.Instance.GetView(hub.countryId);
                if (view == null) return false; // 아직 등록 안 된 국가가 있음 — 전부 준비될 때까지 대기
                resolved[hub.id] = view.DnaSpawnWorldPosition + (Vector3)hub.localOffset;
            }

            _hubWorldPositions = resolved;
            _hubPositionsResolved = true;
            return true;
        }

        /// <summary>허브 쌍마다 정확히 한 번만 선을 그린다(양방향 연결 데이터가 중복이어도 겹쳐 그리지 않음).</summary>
        private void DrawRouteLines()
        {
            var drawnPairs = new HashSet<string>();
            foreach (var hub in _hubs)
            {
                if (!_hubWorldPositions.TryGetValue(hub.id, out var fromPos)) continue;

                foreach (var link in hub.connections)
                {
                    if (!_hubLookup.TryGetValue(link.targetHubId, out var target)) continue;
                    if (!_hubWorldPositions.TryGetValue(target.id, out var toPos)) continue;

                    string pairKey = PairKey(hub.id, target.id);
                    if (!drawnPairs.Add(pairKey)) continue;

                    var path = BuildPathPoints(hub, target, fromPos, toPos);
                    CreateRouteLine(hub.type, path);
                }
            }
        }

        /// <summary>사전순으로 정렬한 "작은ID|큰ID" 형식의 canonical 페어 키 — 경유점 테이블 조회와 중복 선
        /// 방지에 공통으로 쓴다.</summary>
        private static string PairKey(string a, string b) =>
            string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

        /// <summary>
        /// from → to로 실제 이동(및 경로선 렌더링)할 전체 경유 지점 배열을 만든다. 경유점 테이블(canonical
        /// "작은ID → 큰ID" 방향)에 이 페어가 있으면 필요시 뒤집어서 from → to 방향으로 끼워 넣고, 없으면
        /// 기존과 동일하게 직선(양 끝점만) 반환.
        /// </summary>
        private Vector3[] BuildPathPoints(TransportHub from, TransportHub to, Vector3 fromPos, Vector3 toPos)
        {
            var table = from.type == TransportHubType.Sea ? _seaWaypoints : _airWaypoints;
            var points = new List<Vector3> { fromPos };

            if (table != null && table.TryGetValue(PairKey(from.id, to.id), out var localWaypoints)
                && localWaypoints != null && localWaypoints.Length > 0)
            {
                bool reversed = string.CompareOrdinal(from.id, to.id) > 0; // from이 canonical상 "큰 쪽"이면 뒤집는다
                if (!reversed)
                {
                    foreach (var wp in localWaypoints)
                        points.Add(WorldMap.Instance.ToWorldPosition(wp));
                }
                else
                {
                    for (int i = localWaypoints.Length - 1; i >= 0; i--)
                        points.Add(WorldMap.Instance.ToWorldPosition(localWaypoints[i]));
                }
            }

            points.Add(toPos);
            return points.ToArray();
        }

        /// <summary>
        /// LineRenderer는 SpriteRenderer가 아니라서 URP 2D 렌더러의 "레거시 스프라이트 셰이더 자동 호환" 특례를
        /// 받지 못한다(CountryView/DnaBubble의 SpriteRenderer가 "Sprites/Default"로 잘 보이는 것과는 다른 경우 —
        /// FloatingTextEffect.cs가 겪었던 "레거시 Font 셰이더가 URP에서 안 보임" 사례와 동일한 함정). 그래서
        /// URP 2D 전용 셰이더("Universal Render Pipeline/2D/Sprite-Unlit-Default")를 우선 사용하고, 혹시 프로젝트
        /// 설정상 그 셰이더를 못 찾으면 "Sprites/Default"로 폴백한다.
        /// </summary>
        private static Shader _routeLineShader;

        private void CreateRouteLine(TransportHubType type, Vector3[] path)
        {
            var go = new GameObject($"Route_{type}");
            go.transform.SetParent(_routesParent);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = path.Length;
            line.SetPositions(path);
            line.startWidth = routeLineWidth;
            line.endWidth = routeLineWidth;
            // [Step 46] 레이어 순서 재정비 — 지도(0) < 감염 핫스팟(10) < 국가 오버레이(20) < 교통 노선(이 값,
            // 30) < 교통 유닛(39~40). "비행기/배와 경로를 가장 위 그룹으로" 요청 반영, 예전 40에서 조정.
            line.sortingOrder = 30;
            line.numCapVertices = 2;

            var color = type == TransportHubType.Air ? airRouteColor : seaRouteColor;
            if (_routeLineShader == null)
                _routeLineShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                    ?? Shader.Find("Sprites/Default");
            var mat = new Material(_routeLineShader);
            line.material = mat;
            line.startColor = color;
            line.endColor = color;
        }

        private void TrySpawnUnit()
        {
            if (_hubs.Count == 0) return;

            var source = _hubs[Random.Range(0, _hubs.Count)];
            var destination = PickWeightedDestination(source);
            if (destination == null) return;
            if (!_hubWorldPositions.TryGetValue(source.id, out var fromPos)) return;
            if (!_hubWorldPositions.TryGetValue(destination.id, out var toPos)) return;

            bool isCarrier = RollIsCarrier(source.countryId);
            float speed = source.type == TransportHubType.Air ? airSpeed : seaSpeed;
            var path = BuildPathPoints(source, destination, fromPos, toPos);

            var unit = _pool.Get();
            unit.transform.SetParent(_unitsParent);
            unit.OnArrived -= HandleUnitArrived;
            unit.OnArrived += HandleUnitArrived;
            unit.BeginLeg(source.id, destination.id, path, source.type, speed, isCarrier, iconScale);

            _active.Add(unit);
            _remainingHops[unit] = Random.Range(minHopsPerUnit, maxHopsPerUnit + 1);
        }

        private void HandleUnitArrived(TransportUnit unit)
        {
            if (unit.IsCarrier)
                TryTransferInfection(unit);

            int hopsLeft = _remainingHops.TryGetValue(unit, out var h) ? h - 1 : 0;
            _remainingHops[unit] = hopsLeft;

            if (hopsLeft <= 0)
            {
                ReleaseUnit(unit);
                return;
            }

            if (!_hubLookup.TryGetValue(unit.DestinationHubId, out var currentHub))
            {
                ReleaseUnit(unit);
                return;
            }

            var next = PickWeightedDestination(currentHub);
            if (next == null || !_hubWorldPositions.TryGetValue(currentHub.id, out var fromPos)
                             || !_hubWorldPositions.TryGetValue(next.id, out var toPos))
            {
                ReleaseUnit(unit);
                return;
            }

            bool isCarrier = RollIsCarrier(currentHub.countryId);
            float speed = currentHub.type == TransportHubType.Air ? airSpeed : seaSpeed;
            var path = BuildPathPoints(currentHub, next, fromPos, toPos);
            unit.BeginLeg(currentHub.id, next.id, path, currentHub.type, speed, isCarrier, iconScale);
        }

        /// <summary>
        /// 도착지 국가로 실제 감염을 옮긴다. SimulationManager.TrySpreadRoute와 같은 불변식을 따른다 —
        /// 이미 감염된 국가는 건드리지 않는다(국가 내부 확산은 SimulationManager의 매틱 공식이 전담).
        /// </summary>
        private void TryTransferInfection(TransportUnit unit)
        {
            var destination = WorldDataManager.Instance?.GetCountry(_hubLookup.TryGetValue(unit.DestinationHubId, out var h) ? h.countryId : null);
            if (destination == null || destination.infectedCount > 0) return;

            // TransmissionRoute Phase 2 — 항공/해운 효율(기본 0.5, trans_air1/trans_water1 연구 후
            // 1.0)을 도착 성공률에 곱한다. isAirportOpen/isPortOpen 봉쇄 판정(IsRouteOpen)은 무변경 —
            // 봉쇄되면 유닛 자체가 그 항로를 선택하지 않으므로 여기까지 오지 않는다.
            var pathogen = WorldDataManager.Instance?.CurrentPathogen;
            if (pathogen == null) return;

            bool isAir = unit.HubType == TransportHubType.Air;
            float routeEfficiency = isAir ? pathogen.airRouteEfficiency : pathogen.waterRouteEfficiency;
            float chance = (isAir ? airArrivalInfectionChance : seaArrivalInfectionChance) * routeEfficiency;
            if (Random.value > chance) return;

            long seedAmount = isAir ? airSeedAmount : seaSeedAmount;
            long seed = System.Math.Min(seedAmount, destination.SusceptibleCount);
            if (seed <= 0) return;

            destination.infectedCount += seed;
            WorldDataManager.Instance.NotifyCountryChanged(destination);
        }

        /// <summary>
        /// [Step 33] 예전엔 "감염자가 1명이라도 있으면 무조건 carrier"였다 — 인구 수천만 국가에 극초반
        /// 몇 명만 감염돼도 그 나라를 오가는 모든 항공/해운편이 매번 빨간 carrier로 표시돼 비현실적으로
        /// 보인다는 지적을 받아, 감염 "비율"에 비례한 확률로 바꿨다. 비율이 아주 낮으면 carrier가 드물게만
        /// 나오고, 감염이 국가 전체로 퍼질수록 점점 더 자주(결국 거의 항상) carrier가 나온다.
        /// </summary>
        private bool RollIsCarrier(string countryId)
        {
            var country = WorldDataManager.Instance?.GetCountry(countryId);
            if (country == null || country.infectedCount <= 0) return false;

            long livingPopulation = country.LivingPopulation;
            if (livingPopulation <= 0) return false;

            float infectionRatio = (float)country.infectedCount / livingPopulation;
            float chance = Mathf.Clamp01(infectionRatio * carrierChanceScale);
            return Random.value < chance;
        }

        /// <summary>같은 타입 허브 중 항로/뱃길이 열려 있는 곳만 가중치 기반으로 랜덤 선택.</summary>
        private TransportHub PickWeightedDestination(TransportHub from)
        {
            float total = 0f;
            var validLinks = new List<TransportRouteLink>();
            foreach (var link in from.connections)
            {
                if (!_hubLookup.TryGetValue(link.targetHubId, out var target)) continue;
                if (!IsRouteOpen(from, target)) continue;
                validLinks.Add(link);
                total += Mathf.Max(0.01f, link.weight);
            }
            if (validLinks.Count == 0) return null;

            float roll = Random.value * total;
            float acc = 0f;
            foreach (var link in validLinks)
            {
                acc += Mathf.Max(0.01f, link.weight);
                if (roll <= acc) return _hubLookup[link.targetHubId];
            }
            return _hubLookup[validLinks[validLinks.Count - 1].targetHubId];
        }

        private bool IsRouteOpen(TransportHub from, TransportHub to)
        {
            var countryA = WorldDataManager.Instance?.GetCountry(from.countryId);
            var countryB = WorldDataManager.Instance?.GetCountry(to.countryId);
            if (countryA == null || countryB == null) return false;

            return from.type == TransportHubType.Air
                ? countryA.isAirportOpen && countryB.isAirportOpen
                : countryA.isPortOpen && countryB.isPortOpen;
        }

        private void ReleaseUnit(TransportUnit unit)
        {
            unit.OnArrived -= HandleUnitArrived;
            _active.Remove(unit);
            _remainingHops.Remove(unit);
            _pool.Release(unit);
        }
    }
}
