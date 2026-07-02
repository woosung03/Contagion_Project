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

        /// <summary>초기 국가 목록을 주입한다 (테스트/씬 부트스트랩용). Step 9에서 ScriptableObject 로더로 대체 예정.</summary>
        public void SetCountries(List<Country> newCountries)
        {
            countries = newCountries ?? new List<Country>();
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
            NotifyWorldStateChanged();
        }

        public void NotifyCountryChanged(Country country) => OnCountryChanged?.Invoke(country);

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
            NotifyWorldStateChanged();
        }
    }
}
