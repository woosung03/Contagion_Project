using System;
using System.Collections.Generic;
using System.Linq;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 국가 데이터 + WorldState 보관소. 설계 문서 12절 Core Managers.
    /// SimulationManager, WorldMap, UpgradeManager 등이 이 매니저를 통해 데이터를 읽고 쓴다.
    /// </summary>
    public class WorldDataManager : MonoBehaviour
    {
        public static WorldDataManager Instance { get; private set; }

        [SerializeField] private List<Country> countries = new List<Country>();
        [SerializeField] private Pathogen currentPathogen = new Pathogen();
        [SerializeField] private WorldState worldState = new WorldState();

        private Dictionary<string, Country> _countryLookup;

        public IReadOnlyList<Country> Countries => countries;
        public Pathogen CurrentPathogen => currentPathogen;
        public WorldState State => worldState;

        /// <summary>국가 상태가 바뀔 때마다 (매 틱) 발행. WorldMap이 색상 갱신에 사용.</summary>
        public event Action<Country> OnCountryChanged;

        /// <summary>WorldState가 바뀔 때마다 발행. HUD 등에서 사용.</summary>
        public event Action<WorldState> OnWorldStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _countryLookup = countries.Where(c => c != null && !string.IsNullOrEmpty(c.id))
                                       .ToDictionary(c => c.id, c => c);
        }

        public Country GetCountry(string id)
        {
            if (_countryLookup == null) RebuildLookup();
            return _countryLookup.TryGetValue(id, out var c) ? c : null;
        }

        /// <summary>
        /// 초기 국가 목록을 주입한다 (테스트/씬 부트스트랩용). Step 9에서 ScriptableObject 로더로 대체 예정.
        ///
        /// id 기준 정렬(서수 비교, 문화권 비의존)을 여기서 한 번만 강제한다 — CountryDatabase 에셋의
        /// 인스펙터 리스트 순서를 그대로 쓰면 "지금은 우연히 항상 같은 순서"일 뿐 보장이 아니라서,
        /// 에셋을 편집하다 리스트가 재배열되면 SimulationManager/EventManager/HumanResistanceManager
        /// 등 이 컬렉션을 순회하는 모든 곳의 실행 순서가 조용히 바뀔 수 있었다(Deterministic Tick
        /// Ordering #1). string.CompareOrdinal은 로케일에 따라 결과가 달라지는 string.Compare와
        /// 달리 항상 코드 유닛 값으로만 비교해 플랫폼/문화권 무관하게 동일한 순서를 보장한다
        /// (TransportManager.PairKey()가 이미 같은 이유로 CompareOrdinal을 쓰고 있음).
        /// </summary>
        public void SetCountries(List<Country> newCountries)
        {
            countries = newCountries ?? new List<Country>();
            countries.Sort((a, b) => string.CompareOrdinal(a?.id, b?.id));
            RebuildLookup();
            RecalculateWorldTotals();
        }

        public void SetPathogen(Pathogen pathogen) => currentPathogen = pathogen;

        /// <summary>
        /// 저장된 WorldState 값을 기존 인스턴스에 덮어쓴다 (참조 교체 대신 필드 복사 — 구독자들이
        /// 들고 있는 State 참조가 계속 유효하도록). SaveManager(Step 13)의 로드 경로에서 사용.
        /// </summary>
        public void LoadState(WorldState loaded)
        {
            if (loaded == null) return;
            worldState.totalPopulation = loaded.totalPopulation;
            worldState.infectedCount = loaded.infectedCount;
            worldState.deadCount = loaded.deadCount;
            worldState.cureProgress = loaded.cureProgress;
            worldState.plagueVisibility = loaded.plagueVisibility;
            worldState.dnaPoints = loaded.dnaPoints;
            worldState.currentDay = loaded.currentDay;
            worldState.cureResearchStarted = loaded.cureResearchStarted;
            // [Step 54] 이 플래그를 안 옮기면 저장된 판을 불러올 때마다 hasEverBeenInfected가 false로
            // 리셋돼, 로드 직후 감염자가 우연히 0인 프레임에 IsPathogenEradicated 게이트가 잘못 열려있는
            // 상태가 된다 — 불러온 판은 이미 감염이 진행 중이었을 것이므로 그대로 복사해야 안전하다.
            worldState.hasEverBeenInfected = loaded.hasEverBeenInfected;
            NotifyWorldStateChanged();
        }

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 이 매니저는 DontDestroyOnLoad라
        /// 씬을 리로드해도 살아남기 때문에, worldState의 누적 필드(cureProgress 등)를 명시적으로
        /// 초기화하지 않으면 이전 판 값이 새 게임에 그대로 이어진다.
        /// </summary>
        public void ResetForNewGame()
        {
            worldState.Reset();
            NotifyWorldStateChanged();
        }

        /// <summary>
        /// OnCountryChanged를 구독자별로 개별 try/catch하며 순회 호출한다. 매 틱 여러 번 발행되는
        /// 이벤트인데, 표준 멀티캐스트 delegate.Invoke()는 구독자 하나가 예외를 던지면 그 뒤에 등록된
        /// 구독자는 전부 호출되지 않고 조용히 스킵된다 — 구독 순서상 뒤에 있는 컨트롤러(예: Country
        /// Dock)만 원인 불명으로 안 움직이는 것처럼 보이는 버그를 유발할 수 있어 방어적으로 격리한다.
        /// </summary>
        public void NotifyCountryChanged(Country country)
        {
            if (OnCountryChanged == null) return;
            foreach (Action<Country> handler in OnCountryChanged.GetInvocationList())
            {
                try
                {
                    handler(country);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public void NotifyWorldStateChanged() => OnWorldStateChanged?.Invoke(worldState);

        /// <summary>국가별 합계로부터 WorldState의 인구/감염/사망 총계를 다시 계산한다.</summary>
        public void RecalculateWorldTotals()
        {
            long pop = 0, infected = 0, dead = 0;
            foreach (var c in countries)
            {
                pop += c.population;
                infected += c.infectedCount;
                dead += c.deadCount;
            }
            worldState.totalPopulation = pop;
            worldState.infectedCount = infected;
            worldState.deadCount = dead;

            // [Step 54] 감염이 실제로 한 번이라도 관측되면 게이트를 켠다 — WorldState.IsPathogenEradicated가
            // "발원 감염이 아직 안 심어진 새 게임 시작 직후"를 "치료제로 박멸됨"으로 착각하지 않도록 하는
            // 플래그. GameDataBootstrapper.SeedStartingInfection()도 이 메서드를 호출하므로, 발원국에 처음
            // 감염을 심는 시점에 자동으로 true가 된다(별도 배선 불필요).
            if (infected > 0) worldState.hasEverBeenInfected = true;

            NotifyWorldStateChanged();
        }
    }
}
