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

        [Header("허브/유닛 규모 (설계 문서 목표: 허브 15+15, 동시 활성 유닛 50~200)")]
        [SerializeField, Tooltip("전염병이 전혀 퍼지지 않은 평시에도 떠 있는 최소 교통량 — 지도가 죽어보이지 않도록.")]
        private int minActiveUnits = 24;
        [SerializeField] private int maxActiveUnits = 180;
        [SerializeField, Tooltip("전 세계 감염률(0~1)에 이 배율을 곱해 목표 활성 유닛 수를 정한다 — 세계 인구 대비 " +
            "감염 비율은 대유행 중에도 대개 몇 % 수준이라, 그대로 쓰면 활성 유닛 목표가 거의 항상 최소치에 " +
            "머문다. 배율을 곱해 '어느 정도만 퍼져도 하늘길이 눈에 띄게 붐빈다'는 체감을 만든다.")]
        private float infectionVisibilityScale = 25f;
        [SerializeField] private int maxSpawnPerTick = 14;
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

        [Header("경로선 시각화")]
        [SerializeField] private Color airRouteColor = new Color(0.55f, 0.85f, 1f, 0.18f);
        [SerializeField] private Color seaRouteColor = new Color(0.55f, 0.8f, 0.6f, 0.18f);
        [SerializeField] private float routeLineWidth = 0.02f;

        private List<TransportHub> _hubs;
        private Dictionary<string, TransportHub> _hubLookup;
        private Dictionary<string, Vector3> _hubWorldPositions;
        private bool _hubPositionsResolved;
        private bool _routesDrawn;

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
                var view = WorldMap.Instance.GetView(hub.countryId);
                if (view == null) return false; // 아직 등록 안 된 국가가 있음 — 전부 준비될 때까지 대기
                resolved[hub.id] = view.DnaSpawnWorldPosition + (Vector3)hub.localOffset;
            }

            _hubWorldPositions = resolved;
            _hubPositionsResolved = true;
            Debug.Log($"[TransportManager] 허브 {_hubs.Count}개 좌표 해석 완료 — 교통망 가동 시작.");
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

                    string pairKey = string.CompareOrdinal(hub.id, target.id) < 0
                        ? $"{hub.id}|{target.id}"
                        : $"{target.id}|{hub.id}";
                    if (!drawnPairs.Add(pairKey)) continue;

                    CreateRouteLine(hub.type, fromPos, toPos);
                }
            }
        }

        /// <summary>
        /// URP 프로젝트에서 "Sprites/Default"는 CountryView/DnaBubble의 SpriteRenderer가 이미 문제없이
        /// 쓰고 있는 셰이더라 LineRenderer에도 그대로 재사용했다. 다만 이 프로젝트는 실제 Unity 에디터에서
        /// 시각 확인을 못 한 채(텍스트 툴로만) 작업 중이라, 만약 선이 화면에 안 보이면(FloatingTextEffect.cs의
        /// 레거시 Font 셰이더 사례처럼 URP 파이프라인에서 이 셰이더가 걸러지는 경우) Shader.Find 대상을
        /// "Universal Render Pipeline/2D/Sprite-Unlit-Default"로 바꿔볼 것.
        /// </summary>
        private void CreateRouteLine(TransportHubType type, Vector3 from, Vector3 to)
        {
            var go = new GameObject($"Route_{type}");
            go.transform.SetParent(_routesParent);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = routeLineWidth;
            line.endWidth = routeLineWidth;
            line.sortingOrder = 40;
            line.numCapVertices = 2;

            var color = type == TransportHubType.Air ? airRouteColor : seaRouteColor;
            var mat = new Material(Shader.Find("Sprites/Default"));
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

            bool isCarrier = IsCountryInfected(source.countryId);
            float speed = source.type == TransportHubType.Air ? airSpeed : seaSpeed;

            var unit = _pool.Get();
            unit.transform.SetParent(_unitsParent);
            unit.OnArrived -= HandleUnitArrived;
            unit.OnArrived += HandleUnitArrived;
            unit.BeginLeg(source.id, destination.id, fromPos, toPos, source.type, speed, isCarrier);

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

            bool isCarrier = IsCountryInfected(currentHub.countryId);
            float speed = currentHub.type == TransportHubType.Air ? airSpeed : seaSpeed;
            unit.BeginLeg(currentHub.id, next.id, fromPos, toPos, currentHub.type, speed, isCarrier);
        }

        /// <summary>
        /// 도착지 국가로 실제 감염을 옮긴다. SimulationManager.TrySpreadRoute와 같은 불변식을 따른다 —
        /// 이미 감염된 국가는 건드리지 않는다(국가 내부 확산은 SimulationManager의 매틱 공식이 전담).
        /// </summary>
        private void TryTransferInfection(TransportUnit unit)
        {
            var destination = WorldDataManager.Instance?.GetCountry(_hubLookup.TryGetValue(unit.DestinationHubId, out var h) ? h.countryId : null);
            if (destination == null || destination.infectedCount > 0) return;

            bool isAir = unit.HubType == TransportHubType.Air;
            float chance = isAir ? airArrivalInfectionChance : seaArrivalInfectionChance;
            if (Random.value > chance) return;

            long seedAmount = isAir ? airSeedAmount : seaSeedAmount;
            long seed = System.Math.Min(seedAmount, destination.SusceptibleCount);
            if (seed <= 0) return;

            destination.infectedCount += seed;
            WorldDataManager.Instance.NotifyCountryChanged(destination);
            Debug.Log($"[TransportManager] {(isAir ? "항공편" : "선박")}이 {destination.name}에 감염 유입 (+{seed}).");
        }

        private bool IsCountryInfected(string countryId)
        {
            var country = WorldDataManager.Instance?.GetCountry(countryId);
            return country != null && country.infectedCount > 0;
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
