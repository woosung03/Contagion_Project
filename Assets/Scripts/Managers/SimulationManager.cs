using System;
using System.Collections;
using System.Collections.Generic;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 틱 기반 감염/사망/치료제 계산. 설계 문서 4절(시뮬레이션 로직), 15절
    /// "시뮬레이션은 실시간이지만 틱 기반 (Update 매프레임 처리 X, 코루틴/InvokeRepeating)" 요구사항 반영.
    ///
    /// 설계 문서에 정의되지 않은 계수(spreadFactor, visibility 증가율, drugResistanceReduction 계수 등)는
    /// 인스펙터에서 조절 가능한 값으로 노출해 두었다 — 실제 밸런싱은 플레이테스트로 조정 필요.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        [Header("틱 설정")]
        [SerializeField, Tooltip("1틱(=게임 내 1일)에 걸리는 실제 초")]
        private float tickIntervalSeconds = 1f;
        [SerializeField] private bool autoStart = true;

        [Header("확산 계수 (설계 문서 미정의 값 - 밸런싱용)")]
        [SerializeField, Tooltip("4.1 newInfected 공식의 spreadFactor")]
        private float globalSpreadFactor = 0.15f;
        [SerializeField, Tooltip("plagueVisibility 증가 속도")]
        private float visibilityGainRate = 0.02f;
        [SerializeField, Tooltip("4.3 drugResistanceReduction 계산 계수")]
        private float drugResistanceCoefficient = 0.02f;
        [SerializeField, Tooltip("4.3 cureIncreasePerTick 전체에 곱하는 감쇠 계수 — 국가 수만큼 healthFunding을 " +
            "그냥 합산(Σ)하면 국가가 몇 개만 있어도 첫 틱에 치료제가 100%를 찍어버려서 추가함. 이 값을 낮출수록 " +
            "치료제 완성까지 걸리는 일수(틱 수)가 길어진다 — 원하는 게임 길이에 맞춰 플레이테스트로 조정할 것.")]
        private float cureProgressCoefficient = 0.002f;

        [Header("국가 간 전파 (설계 문서 4.1 - 확률적 이동)")]
        [SerializeField, Range(0f, 1f)] private float airRouteSpreadChance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float seaRouteSpreadChance = 0.03f;
        [SerializeField, Range(0f, 1f)] private float landBorderSpreadChance = 0.08f;
        [SerializeField] private long seedInfectedAmount = 10;

        [Header("DNA 마일스톤 (설계 문서 4.4)")]
        [SerializeField] private long infectionMilestoneStep = 100_000;
        [SerializeField] private long deathMilestoneStep = 10_000;

        private readonly Dictionary<string, long> _lastInfectionMilestone = new Dictionary<string, long>();
        private readonly Dictionary<string, long> _lastDeathMilestone = new Dictionary<string, long>();

        private Coroutine _tickRoutine;
        private WorldDataManager _data;

        /// <summary>감염 마일스톤 돌파 시 발행 (country, 새로 돌파한 감염자 수). BubbleSpawner가 구독.</summary>
        public event Action<Country> OnInfectionMilestone;
        /// <summary>사망 마일스톤 돌파 시 발행. BubbleSpawner가 구독.</summary>
        public event Action<Country> OnDeathMilestone;
        /// <summary>승리/패배 판정 시 1회 발행.</summary>
        public event Action<bool /*isVictory*/> OnGameEnded;

        /// <summary>매 틱 계산이 끝난 뒤 발행. HumanResistanceManager 등 후처리 로직이 구독.</summary>
        public event Action<WorldState> OnTickCompleted;

        private bool _gameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _data = WorldDataManager.Instance;

            if (GameManager.Instance != null)
            {
                Debug.Log($"[SimulationManager] 난이도={GameManager.Instance.CurrentDifficulty}, " +
                    $"확산배율={GameManager.Instance.GetDifficultySpreadMultiplier():F2}, " +
                    $"치료속도배율={GameManager.Instance.GetDifficultyResearchMultiplier():F2}");
            }
            else
            {
                Debug.LogWarning("[SimulationManager] GameManager.Instance가 없음 — 난이도 배율이 전부 1.0으로 적용됨. " +
                    "씬에 GameManager 오브젝트가 있는지 확인.");
            }

            if (autoStart) StartSimulation();
        }

        public void StartSimulation()
        {
            if (_tickRoutine != null) return;
            _tickRoutine = StartCoroutine(TickLoop());
        }

        public void StopSimulation()
        {
            if (_tickRoutine == null) return;
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 이 매니저는 DontDestroyOnLoad라
        /// 씬을 리로드해도 살아남는데, _gameEnded가 true로 남아있으면 TickLoop()과
        /// EvaluateEndConditions()가 매 프레임 조용히 스킵돼서 재시작한 새 게임의 틱이 영원히 멈춰있는
        /// 치명적인 버그가 있었다. 마일스톤 기록(_lastInfectionMilestone/_lastDeathMilestone)도 이전 판
        /// 수치가 남아있으면 새 게임에서 DNA 버블이 한참 동안 안 뜨는 문제가 있어 같이 비운다.
        /// </summary>
        public void ResetForNewGame()
        {
            _gameEnded = false;
            _lastInfectionMilestone.Clear();
            _lastDeathMilestone.Clear();
        }

        /// <summary>
        /// 부활(패배 후 보상형 광고 시청 → 같은 판 이어가기, 설계 문서 13절)에서 호출. 완전히 새 게임을
        /// 시작하는 게 아니라 "이 판을 계속 진행"하는 것이므로 마일스톤/국가 데이터는 건드리지 않고
        /// EvaluateEndConditions()가 다시 틱을 스킵하지 않도록 종료 플래그만 푼다. 이걸 안 하면
        /// UIManager.HandleReviveRequested가 GameManager.isPaused는 풀어도 TickLoop()의
        /// "if (_gameEnded) continue;"에 계속 걸려 시뮬레이션이 영원히 멈춰있는 버그가 있었다.
        /// </summary>
        public void ClearGameEndedFlag() => _gameEnded = false;

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(tickIntervalSeconds);
            while (true)
            {
                yield return wait;
                if (_gameEnded) continue;
                if (GameManager.Instance != null && GameManager.Instance.IsPaused) continue;
                RunTick();
            }
        }

        /// <summary>테스트/디버깅용으로 외부에서 즉시 1틱을 강제 실행할 때 사용.</summary>
        public void RunTick()
        {
            if (_data == null) _data = WorldDataManager.Instance;
            if (_data == null || _data.CurrentPathogen == null) return;

            var pathogen = _data.CurrentPathogen;
            var state = _data.State;

            float totalInfectionRatio = 0f;
            int countryCount = 0;

            foreach (var country in _data.Countries)
            {
                long preTickInfected = country.infectedCount;
                if (preTickInfected <= 0 && country.SusceptibleCount <= 0)
                {
                    countryCount++;
                    continue;
                }

                // --- 4.2 사망자 계산 (감염 갱신 전 스냅샷 기준) ---
                float severityFactor = pathogen.severity;
                float healthcareCapacity = country.HealthLevel;
                long newDeaths = StochasticRound(preTickInfected * pathogen.lethality * severityFactor * (1f - healthcareCapacity));
                newDeaths = Math.Clamp(newDeaths, 0, preTickInfected);

                // --- 4.1 국내 전파 ---
                float climateModifier = pathogen.GetEnvironmentResistance(country.climate);
                float effectiveHealthLevel = country.HealthLevel * Mathf.Lerp(1f, country.governmentStability, 0.5f);
                // 난이도별 확산 배율 (나무위키 기준, Docs/PlagueIncReference.md 3절) — 쉬움은 퍼지기 쉽고
                // 어려움부터는 퍼지기 어려워 신중한 플레이가 요구된다. 이전엔 난이도가 치료 속도만 바꿔서
                // 고난이도의 긴장감이 약했음.
                float difficultySpreadMultiplier = GameManager.Instance != null
                    ? GameManager.Instance.GetDifficultySpreadMultiplier() : 1f;
                long newInfected = StochasticRound(preTickInfected * pathogen.infectivity * globalSpreadFactor
                                           * difficultySpreadMultiplier * (1f - effectiveHealthLevel) * climateModifier);
                newInfected = Math.Clamp(newInfected, 0, country.SusceptibleCount);

                country.deadCount += newDeaths;
                country.infectedCount = Math.Max(0, preTickInfected - newDeaths + newInfected);

                CheckMilestones(country);

                if (country.LivingPopulation > 0)
                    totalInfectionRatio += (float)country.infectedCount / country.LivingPopulation;
                countryCount++;

                _data.NotifyCountryChanged(country);
            }

            SpreadBetweenCountries(pathogen);

            // --- 4.3 치료제 개발 속도 ---
            float cureIncrease = 0f;
            foreach (var country in _data.Countries)
                cureIncrease += country.healthFunding * country.ResearchMultiplier;
            cureIncrease *= (1f + state.plagueVisibility * 0.5f);
            cureIncrease *= GameManager.Instance != null ? GameManager.Instance.GetDifficultyResearchMultiplier() : 1f;
            float drugResistanceReduction = pathogen.drugResistance * drugResistanceCoefficient;
            state.cureProgress = Mathf.Clamp01(state.cureProgress + cureIncrease * cureProgressCoefficient - drugResistanceReduction);

            // 설계 문서 1절 패배 조건 "감염자 0명 + 생존자 존재 (치료제 완성 후 박멸)" —
            // 치료제가 100% 완성되는 순간 전 세계 감염자를 즉시 박멸한다. 문서에 별도 회복 공식이
            // 없어서, 감염자가 우연히 자연 소멸하길 기다리는 대신 치료제 완성을 명확한 트리거로 삼았다.
            if (state.cureProgress >= 1f)
                EradicatePathogen();

            // --- plagueVisibility 갱신 (설계 문서 5절 저항 단계 산정 근거) ---
            float infectedRatio = state.totalPopulation > 0 ? (float)state.infectedCount / state.totalPopulation : 0f;
            state.plagueVisibility = Mathf.Clamp01(state.plagueVisibility + infectedRatio * pathogen.severity * visibilityGainRate);

            state.currentDay++;
            _data.RecalculateWorldTotals();

            float averageInfectionRatio = countryCount > 0 ? totalInfectionRatio / countryCount : 0f;
            GameManager.Instance?.EvaluatePhase(state, averageInfectionRatio);

            OnTickCompleted?.Invoke(state);

            EvaluateEndConditions(state);
        }

        /// <summary>4.1 "국가 간 전파" - 항공/해운/육상 경로를 통한 확률적 감염자 이동.</summary>
        private void SpreadBetweenCountries(Pathogen pathogen)
        {
            foreach (var source in _data.Countries)
            {
                if (source.infectedCount <= 0) continue;
                float sourceRatio = source.LivingPopulation > 0 ? (float)source.infectedCount / source.LivingPopulation : 0f;

                TrySpreadRoute(source, source.airRouteCountryIds, airRouteSpreadChance * sourceRatio,
                    target => source.isAirportOpen && target.isAirportOpen);

                TrySpreadRoute(source, source.seaRouteCountryIds, seaRouteSpreadChance * sourceRatio,
                    target => source.isPortOpen && target.isPortOpen);

                TrySpreadRoute(source, source.neighborCountryIds, landBorderSpreadChance * sourceRatio,
                    target => !source.isBorderClosed && !target.isBorderClosed);
            }
        }

        private void TrySpreadRoute(Country source, List<string> targetIds, float chance, Func<Country, bool> routeOpen)
        {
            if (targetIds == null) return;
            foreach (var targetId in targetIds)
            {
                var target = _data.GetCountry(targetId);
                if (target == null || target.infectedCount > 0) continue;
                if (!routeOpen(target)) continue;
                if (UnityEngine.Random.value > chance) continue;

                long seed = Math.Min(seedInfectedAmount, target.SusceptibleCount);
                if (seed <= 0) continue;
                target.infectedCount += seed;
                _data.NotifyCountryChanged(target);
            }
        }

        /// <summary>
        /// (long) 캐스트는 소수부를 그냥 버려서, 인구가 작아지면(예: 감염자 500명 × 치사율 0.002 = 1.0 미만)
        /// newDeaths/newInfected가 매 틱 0으로 굳어버려 시뮬레이션이 특정 숫자에서 영구히 멈추는 버그가 있었다.
        /// 확률적 반올림으로 대체 — 예: 2.7이면 70% 확률로 3, 30% 확률로 2를 반환해 장기적으로 평균이 맞고,
        /// 값이 1 미만이어도(예: 0.3) 30% 확률로 최소 1은 진행되어 멈추지 않는다.
        /// </summary>
        private static long StochasticRound(float value)
        {
            if (value <= 0f) return 0;
            long floor = (long)value;
            float frac = value - floor;
            if (UnityEngine.Random.value < frac) floor += 1;
            return floor;
        }

        private void CheckMilestones(Country country)
        {
            _lastInfectionMilestone.TryGetValue(country.id, out long lastInfected);
            if (country.infectedCount / infectionMilestoneStep > lastInfected / infectionMilestoneStep)
            {
                _lastInfectionMilestone[country.id] = country.infectedCount;
                OnInfectionMilestone?.Invoke(country);
            }

            _lastDeathMilestone.TryGetValue(country.id, out long lastDead);
            if (country.deadCount / deathMilestoneStep > lastDead / deathMilestoneStep)
            {
                _lastDeathMilestone[country.id] = country.deadCount;
                OnDeathMilestone?.Invoke(country);
            }
        }

        /// <summary>치료제 완성 시점에 전 세계 감염자를 0으로 만든다 (사망자는 그대로 유지 — 이미 죽은 사람은 안 살아남).</summary>
        private void EradicatePathogen()
        {
            foreach (var country in _data.Countries)
            {
                if (country.infectedCount <= 0) continue;
                country.infectedCount = 0;
                _data.NotifyCountryChanged(country);
            }
        }

        private void EvaluateEndConditions(WorldState state)
        {
            if (_gameEnded) return;
            if (state.IsHumanityExtinct)
            {
                _gameEnded = true;
                OnGameEnded?.Invoke(true);
            }
            else if (state.IsPathogenEradicated)
            {
                _gameEnded = true;
                OnGameEnded?.Invoke(false);
            }
        }
    }
}
