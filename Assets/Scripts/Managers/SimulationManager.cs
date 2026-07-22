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

        [Header("치료제 연구 '시작' 판정 (절대 임계값 대신 매 틱 확률)")]
        [SerializeField, Tooltip("아직 연구가 시작되지 않은 상태에서, 전 세계 감염자 수(명 단위 아님 — population과 " +
            "동일 스케일) 1당 이 틱에 '발견되어 보도될' 확률이 이만큼 늘어난다. 감염자/사망자가 많아질수록 " +
            "누적 확률이 아니라 매 틱 새로 굴리는 확률 자체가 커지는 방식이라, 초반엔 몇 틱을 버텨도 안 걸릴 수 " +
            "있고 반대로 운 나쁘면(=피해가 커지기도 전에) 일찍 걸릴 수도 있다 — 현실의 '조기 발견/뒤늦은 발견' " +
            "변동성을 재현.")]
        private float cureStartChancePerInfected = 0.0000005f;
        [SerializeField, Tooltip("사망자는 감염자보다 훨씬 눈에 띄는 사건이라(뉴스/부검/장례 등으로 은폐가 어려움) " +
            "1당 확률 기여도를 감염자보다 크게 잡았다.")]
        private float cureStartChancePerDeath = 0.0000025f;

        [Header("국가 간 전파 (설계 문서 4.1 - 확률적 이동)")]
        [SerializeField, Range(0f, 1f), Tooltip("[미사용] TransportManager가 항공 전파를 실제 이동체 도착 판정으로 " +
            "대체하면서 더 이상 여기서 읽지 않는다 — 값은 남겨뒀지만(다른 용도로 재활용 가능) SpreadBetweenCountries()가" +
            " 이 필드를 참조하지 않는다.")]
        private float airRouteSpreadChance = 0.05f;
        [SerializeField, Range(0f, 1f), Tooltip("[미사용] TransportManager가 해운 전파를 실제 이동체 도착 판정으로 " +
            "대체하면서 더 이상 여기서 읽지 않는다.")]
        private float seaRouteSpreadChance = 0.03f;
        [SerializeField, Range(0f, 1f)] private float landBorderSpreadChance = 0.08f;
        [SerializeField] private long seedInfectedAmount = 10;

        [Header("TransmissionRoute Phase 2 — 전문화 경로")]
        [SerializeField, Range(0f, 1f), Tooltip("Animal 경로 해금 시, 국경이 폐쇄된 인접국에도 이 배율만큼 " +
            "낮은 확률로 육상 전파가 계속된다(철새·야생동물은 국경 통제로 못 막는다는 설정). 기존 " +
            "landBorderSpreadChance * sourceRatio 호출과는 별개의 추가 호출이라, 이 경로를 안 켠 병원체의 " +
            "기존 확산에는 전혀 영향을 주지 않는다.")]
        private float animalBypassFactor = 0.15f;
        [SerializeField, Tooltip("Insect 경로 해금 시, Humid 기후 국가의 국내 확산 공식에서 infectivity에 " +
            "가산되는 보너스(곱연산이 아니라 가산 — pathogen.environmentResistance와 축을 분리하기 위함, " +
            "TransmissionRoute Design Phase 2 §5 참고).")]
        private float insectHumidBonus = 0.05f;
        [SerializeField, Tooltip("Blood 경로 해금 시, 국내 확산 공식에 곱해지는 배율 — " +
            "1 + 이 값 * (1 - country.HealthLevel). 의료 수준이 낮은 국가일수록 보너스가 커진다.")]
        private float bloodLowHealthBonusScale = 0.5f;

        [Header("DNA 마일스톤 (설계 문서 4.4 — 원본 게임 방식: 국가별 최초 감염 1회 + 국가별 인구 대비 퍼센트 단위)")]
        [SerializeField, Tooltip("국가에 감염자가 최초로 발생하는 순간 무조건 1회 지급되는 DNA 보상 — 절대값/퍼센트와" +
            " 무관하게 항상 발동. 원본 게임의 '신규 감염 국가 발견' 보상.")]
        private bool grantDnaOnFirstInfection = true;
        [SerializeField, Range(0.01f, 1f), Tooltip("국가 인구 대비 감염자 비율이 이 값만큼 늘어날 때마다 DNA 지급 " +
            "(예: 0.25 = 25%p마다). 이전엔 국가별 절대 감염자 수(예: 100,000명)를 기준으로 했는데, 국가마다 인구" +
            " 규모 차이가 커서(최소 약 2,700만~최대 약 14억) 절대값 기준으로는 작은 나라는 마일스톤이 거의 안 뜨고" +
            " 큰 나라만 계속 뜨는 문제가 있었다. 인구 대비 퍼센트로 바꾸면 국가 크기와 무관하게 동일한 빈도로 발동한다." +
            " (0.1→0.25로 완화: 국가 48개 × 10%p 간격이라 버블이 너무 자주 뜬다는 피드백 반영 — 국가당 최대 발동" +
            " 횟수가 10회→4회로 줄어든다.)")]
        private float infectionMilestonePercentStep = 0.25f;
        [SerializeField, Range(0.01f, 1f), Tooltip("국가 인구 대비 사망자 비율이 이 값만큼 늘어날 때마다 DNA 지급. 사망은" +
            " 감염보다 드물고 무거운 사건이라(dnaPerDeathBubble 보상도 더 큼) 기본 간격을 감염보다 촘촘하게 잡되," +
            " (0.05→0.15로 완화: 마찬가지로 버블 빈도 완화 — 국가당 최대 발동 횟수가 20회→약 6~7회로 줄어든다.)")]
        private float deathMilestonePercentStep = 0.15f;

        /// <summary>국가별로 "최초 감염 DNA"를 이미 지급했는지 — 게임당 국가마다 정확히 1회만 발동해야 한다.</summary>
        private readonly HashSet<string> _firstInfectionGranted = new HashSet<string>();
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

        /// <summary>치료제 연구가 (확률 판정을 통과해) 실제로 시작되는 순간 딱 1회 발행 — NewsFeedController가 구독.</summary>
        public event Action OnCureResearchStarted;

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

            if (GameManager.Instance == null)
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
            _firstInfectionGranted.Clear();
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

            // --- Event Queue 일괄 적용 (World Simulation Engine #2) ---
            // 직전 틱에 EventManager.ProcessTick()이 큐에 쌓아둔 Country/WorldState 변경(자연재해
            // 감염 급증, 국경 강제 개방/봉쇄, cureProgress 가감 등)을 국가 루프가 시작되기 "전"에
            // FIFO 순서로 전부 반영한다 — 그래야 이번 틱의 사망/신규감염 계산이 "이번 틱에 새로
            // 발동한 이벤트"가 아니라 "직전 틱까지 확정된 상태"만을 입력으로 사용한다(Docs/Archive/
            // WorldSimulationSystem_Design.md §9.3, §10).
            EventManager.Instance?.ApplyQueuedChanges();

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

                // TransmissionRoute Phase 2 — Insect(가산)/Blood(곱연산) 전문화 보너스. Insect는
                // pathogen.GetEnvironmentResistance()(병원체 고유 기후 생존력, climateModifier)와
                // 축을 분리하기 위해 곱연산이 아니라 infectivity에 직접 가산한다(Design Phase 2 §5).
                float insectBonus = pathogen.HasTransmissionRoute(TransmissionRoute.Insect)
                    && country.climate == ClimateType.Humid ? insectHumidBonus : 0f;
                float bloodMultiplier = pathogen.HasTransmissionRoute(TransmissionRoute.Blood)
                    ? 1f + bloodLowHealthBonusScale * (1f - country.HealthLevel) : 1f;

                // National Infection Dynamics Design Phase — Population Density(최소 구현안).
                // 국가별 population 5분위 등급에 따른 국내 확산 배율(0.70~1.30) — 같은 병원체라도
                // 인구가 많은 나라(중국/인도 등)는 더 빨리, 적은 나라는 더 느리게 퍼진다.
                float densityMultiplier = country.DensityMultiplier;

                long newInfected = StochasticRound(preTickInfected * (pathogen.infectivity + insectBonus) * globalSpreadFactor
                                           * difficultySpreadMultiplier * (1f - effectiveHealthLevel) * climateModifier
                                           * bloodMultiplier * densityMultiplier);
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
            // 원본 게임/현실의 실제 전염병처럼, 아직 발견되지 않은 질병을 미리 연구할 수는 없다. 이전엔
            // plagueVisibility(=고정 임계값 0.2) 도달 여부로 한 번에 딱 끊어 판정했는데, 그러면 "감염자/사망자가
            // 늘어날수록 발견 확률이 자연스럽게 올라간다"는 느낌이 없이 항상 같은 타이밍에 발견돼버린다.
            // 대신 아직 연구가 안 시작된 동안은 매 틱 "이번에 발견될 확률"을 전 세계 감염자·사망자 수로부터
            // 직접 계산해서 굴린다 — 피해 규모가 클수록 이번 틱에 걸릴 확률 자체가 커지는 방식.
            if (!state.cureResearchStarted)
            {
                float discoveryChance = Mathf.Clamp01(
                    state.infectedCount * cureStartChancePerInfected +
                    state.deadCount * cureStartChancePerDeath);
                if (UnityEngine.Random.value < discoveryChance)
                {
                    state.cureResearchStarted = true;
                    OnCureResearchStarted?.Invoke();
                }
            }

            if (state.cureResearchStarted)
            {
                float cureIncrease = 0f;
                foreach (var country in _data.Countries)
                    cureIncrease += country.healthFunding * country.ResearchMultiplier;
                cureIncrease *= (1f + state.plagueVisibility * 0.5f);
                cureIncrease *= GameManager.Instance != null ? GameManager.Instance.GetDifficultyResearchMultiplier() : 1f;
                float drugResistanceReduction = pathogen.drugResistance * drugResistanceCoefficient;
                state.cureProgress = Mathf.Clamp01(state.cureProgress + cureIncrease * cureProgressCoefficient - drugResistanceReduction);
            }

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

            // --- 결정론적 후처리 파이프라인 (Deterministic Tick Ordering #1) ---
            // 예전엔 HumanResistanceManager/EventManager/TransportManager/SaveManager 네 매니저가
            // 전부 독립적으로 OnTickCompleted를 구독했다. Unity는 서로 다른 컴포넌트의 OnEnable/Start
            // 호출 순서를 보장하지 않으므로, 같은 틱 안에서 "봉쇄 판정이 먼저냐 뉴스 이벤트가
            // 먼저냐"가 실행마다 달라질 수 있었다(Docs/Archive/WorldSimulationSystem_Design.md §2,
            // §5, §9.1 — 이미 분석되고 승인된 결함). 예를 들어 EventManager.ApplyPoliticalInstability가
            // 국경을 강제로 다시 여는 효과가 HumanResistanceManager.ApplyPolicy의 이번 틱 봉쇄
            // 판정보다 먼저 실행되는지 나중에 실행되는지에 따라 그 틱의 최종 국경 상태가 달라졌다.
            // SimulationManager가 이미 소유한 틱 루프 안에서 이 네 매니저를 직접, 고정된 순서로
            // 호출하는 것으로 대체한다 — 새 이벤트/큐 인프라를 만들지 않고 GameManager.EvaluatePhase()
            // 호출과 동일한 기존 "직접 호출" 패턴을 그대로 확장한 것뿐이다.
            HumanResistanceManager.Instance?.ProcessTick(state); // Layer 4: 봉쇄/연구기여도 갱신 (내부에서 OnPolicyApplied 발행 → BottleneckAnalyzer)
            EventManager.Instance?.ProcessTick(state);           // Layer 6: 뉴스 이벤트 판정 (봉쇄 갱신 이후 상태를 읽음)
            TransportManager.Instance?.ProcessTick(state);       // 교통 유닛 스폰/도착 (이벤트로 바뀐 국경 상태를 반영)
            SaveManager.Instance?.ProcessTick(state);             // 오토세이브 (그 틱의 최종 상태를 저장)

            EvaluateEndConditions(state);
        }

        /// <summary>
        /// 4.1 "국가 간 전파" - 육상 국경을 통한 확률적 감염자 이동.
        /// 항공/해운 전파는 더 이상 여기서 추상적으로 굴리지 않는다 — TransportManager가 실제로 지도 위를
        /// 이동하는 비행기/배(TransportUnit)로 시각화해서 그 도착 시점에 직접 감염을 옮긴다(사용자 제공
        /// "Global Transport Network Design" 문서 반영). 두 시스템을 동시에 돌리면 항공/해운 전파가
        /// 이중으로 적용되므로, airRouteSpreadChance/seaRouteSpreadChance 필드와 Country의
        /// airRouteCountryIds/seaRouteCountryIds 데이터는 남아있지만(다른 용도로 재활용될 수 있어 삭제하지
        /// 않음) 더 이상 여기서 읽지 않는다.
        /// </summary>
        private void SpreadBetweenCountries(Pathogen pathogen)
        {
            foreach (var source in _data.Countries)
            {
                if (source.infectedCount <= 0) continue;
                float sourceRatio = source.LivingPopulation > 0 ? (float)source.infectedCount / source.LivingPopulation : 0f;

                // TransmissionRoute Phase 2 — Contact 효율(기본 0.7, trans_contact1 연구 후 1.0)을
                // 기존 확률에 곱한다. 봉쇄(isBorderClosed) 판정은 무변경.
                TrySpreadRoute(source, source.neighborCountryIds,
                    landBorderSpreadChance * sourceRatio * pathogen.contactRouteEfficiency,
                    target => !source.isBorderClosed && !target.isBorderClosed);

                // Animal 경로 — 국경 폐쇄를 낮은 확률로 우회하는 추가 호출. 기존 호출과 완전히
                // 별개라 이 경로를 안 켠 병원체의 확산에는 영향이 없다.
                if (pathogen.HasTransmissionRoute(TransmissionRoute.Animal))
                {
                    TrySpreadRoute(source, source.neighborCountryIds,
                        landBorderSpreadChance * sourceRatio * animalBypassFactor,
                        target => true);
                }
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
            // 최초 감염 — preTickInfected가 아니라 "이 국가에서 지금까지 한 번도 지급 안 했는지"로 판정한다.
            // (국내 자연 확산으로 0→양수가 되는 경우뿐 아니라, SpreadBetweenCountries()로 항공/해운/육상
            // 경로를 통해 다른 나라에서 막 전파돼 들어온 경우도 다음 틱의 이 루프에서 정확히 한 번 잡힌다.)
            if (grantDnaOnFirstInfection && country.infectedCount > 0 && _firstInfectionGranted.Add(country.id))
            {
                OnInfectionMilestone?.Invoke(country);
            }
            else if (country.population > 0)
            {
                long stepIndex = (long)((double)country.infectedCount / country.population / infectionMilestonePercentStep);
                _lastInfectionMilestone.TryGetValue(country.id, out long lastStep);
                if (stepIndex > lastStep)
                {
                    _lastInfectionMilestone[country.id] = stepIndex;
                    OnInfectionMilestone?.Invoke(country);
                }
            }

            if (country.population > 0)
            {
                long deathStepIndex = (long)((double)country.deadCount / country.population / deathMilestonePercentStep);
                _lastDeathMilestone.TryGetValue(country.id, out long lastDeathStep);
                if (deathStepIndex > lastDeathStep)
                {
                    _lastDeathMilestone[country.id] = deathStepIndex;
                    OnDeathMilestone?.Invoke(country);
                }
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
