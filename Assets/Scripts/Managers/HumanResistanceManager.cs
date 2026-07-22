using System;
using System.Collections.Generic;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 인류 저항 AI. 설계 문서 5절.
    /// plagueVisibility 구간(ResistanceStage)에 따라 국가별 방역 조치(국경/항공/항구 봉쇄)와
    /// 치료제 연구 기여도(healthFunding)를 자동으로 갱신한다.
    /// SimulationManager.RunTick()이 매 틱 이후 ProcessTick()을 직접, 고정된 순서로 호출한다
    /// (Deterministic Tick Ordering #1 — 예전엔 OnTickCompleted 구독 방식이라 EventManager 등
    /// 다른 구독자와의 상대 순서가 보장되지 않았다).
    ///
    /// 국가 등급별 봉쇄 임계값(plagueVisibility 기준)과 세계 붕괴 시 안정성 감소율은
    /// 설계 문서에 정확한 수치가 없어 5절의 서술(선진국 빠른 봉쇄 / 개도국 느린 봉쇄 / 저개발국 봉쇄 없음)을
    /// 근거로 합리적으로 정의한 값이며, 인스펙터에서 조정 가능하다.
    /// </summary>
    public class HumanResistanceManager : MonoBehaviour
    {
        public static HumanResistanceManager Instance { get; private set; }

        [Header("국가 등급별 봉쇄 트리거 (plagueVisibility 임계값 — '국경' 폐쇄 기준값)")]
        [SerializeField, Tooltip("선진국은 빠르게 봉쇄")] private float highDevLockdownThreshold = 0.5f;
        [SerializeField, Tooltip("개발도상국은 느리게 봉쇄")] private float midDevLockdownThreshold = 0.7f;
        // 저개발국(Low)은 설계 문서 5절에 따라 봉쇄 트리거 없음

        [Header("국경 폐쇄 순차화 (나무위키 기준: 공항 > 국경 > 항구, Docs/PlagueIncReference.md 5절)")]
        [SerializeField, Tooltip("위 임계값 기준으로 공항은 이만큼 일찍, 항구는 이만큼 늦게 닫힘")]
        private float sequentialClosureMargin = 0.1f;

        [Header("QA 지원 — Animal Route(TransmissionRoute) 검증용. 기본값 false, 실제 밸런스 무관")]
        [SerializeField, Tooltip("켜면 High/Mid 개발국의 봉쇄 기준 임계값이 아래 qaClosureThresholdOverride로 " +
            "강제 대체된다 — 공항(-margin)/국경(그대로)/항구(+margin) 순차 구조는 그대로 유지되고 기준점만 " +
            "낮아져 봉쇄가 훨씬 빨리 발생한다. plagueVisibility 증가율(SimulationManager.visibilityGainRate)이나 " +
            "다른 어떤 확산 공식도 건드리지 않는다 — 순수하게 이 매니저의 봉쇄 판정 임계값만 바뀐다. " +
            "Animal(국경 폐쇄 우회) 검증용으로만 켜고, 실제 플레이/밸런스 검증 시에는 반드시 꺼둘 것.")]
        private bool qaFastClosureMode = false;
        [SerializeField, Range(0.01f, 1f), Tooltip("qaFastClosureMode가 켜졌을 때 High/Mid 개발국 봉쇄 기준 " +
            "임계값을 이 값으로 강제 대체한다. 기본 0.05면 국경이 visibility=0.05에서 닫힌다(원래 High 0.5/" +
            "Mid 0.7 대비 10~14배 빠름).")]
        private float qaClosureThresholdOverride = 0.05f;

        [Header("국가 등급별 최대 연구 기여도 (healthFunding)")]
        [SerializeField] private float highDevMaxFunding = 1.0f;
        [SerializeField] private float midDevMaxFunding = 0.5f;
        [SerializeField] private float lowDevMaxFunding = 0.05f;

        [Header("세계 붕괴 단계 (0.8~1.0) 페널티")]
        [SerializeField] private float worldCollapseFundingMultiplier = 0.3f;
        [SerializeField] private float worldCollapseStabilityDecayPerTick = 0.01f;

        [Header("국가별 개별 붕괴 단계 페널티 (나무위키 기준, Docs/PlagueIncReference.md 1절)")]
        [SerializeField, Tooltip("무질서 팽배(사망률 50%+) 구간 연구 가동률")]
        private float disorderFundingMultiplier = 0.6f;
        [SerializeField, Tooltip("무정부상태 근접(사망률 70%+) 구간 연구 가동률")]
        private float nearAnarchyFundingMultiplier = 0.25f;
        [SerializeField, Tooltip("완전한 무정부 상태(사망률 95%+)에서 감염자가 없어도 매 틱 발생하는 치안붕괴 사망 비율")]
        private float fullAnarchyUnrestDeathRate = 0.01f;

        private WorldDataManager Data => WorldDataManager.Instance;
        private ResistanceStage _lastStage = ResistanceStage.NoAwareness;
        private WorldMortalityStage _lastMortalityStage = WorldMortalityStage.Stable;
        private readonly Dictionary<string, CountryCollapseStage> _lastCollapseStage = new Dictionary<string, CountryCollapseStage>();

        /// <summary>Important Event Popup System — 게임 전체 최초 1회 판정용 플래그. 국가별 상태를
        /// 담는 _lastCollapseStage와 달리 이건 "어떤 국가든 이 단계에 처음 도달했는지"를 전역으로
        /// 추적한다. SaveData 연동 없음(세션 한정, ResetForNewGame()에서만 초기화).</summary>
        private bool _firstCollapseShown;
        private bool _firstFullAnarchyShown;

        /// <summary>게임 전체에서 어떤 국가든 최초로 Normal 상태를 벗어났을 때 딱 1회 발행 —
        /// ImportantEventPopupController가 구독.</summary>
        public event Action<Country> OnFirstCountryCollapse;

        /// <summary>게임 전체에서 어떤 국가든 최초로 FullAnarchy에 도달했을 때 딱 1회 발행 —
        /// ImportantEventPopupController가 구독.</summary>
        public event Action<Country> OnFirstFullAnarchy;

        /// <summary>저항 단계가 바뀔 때만 발행 — 뉴스 피드(Step 7)에서 활용 예정.</summary>
        public event Action<ResistanceStage> OnResistanceStageChanged;

        /// <summary>
        /// 전 세계 사망률 기준 위험도 단계가 바뀔 때만 발행. 나무위키 "세계를 위협"/"인류 멸종 임박" 문구
        /// 세분화 (Docs/PlagueIncReference.md 2절) — OnResistanceStageChanged(전염 확산 축)와 별개 축이다.
        /// </summary>
        public event Action<WorldMortalityStage> OnMortalityStageChanged;

        /// <summary>그 틱의 국가별 봉쇄/연구기여도 갱신(ApplyPolicy)이 전부 끝난 뒤 발행 —
        /// BottleneckAnalyzer가 구독한다. 이 이벤트는 구독자가 BottleneckAnalyzer 하나뿐이라
        /// 순서 문제가 없다(ProcessTick() 안에서 동기적으로 발행되므로 항상 이 시점에 딱 맞춰 실행됨).</summary>
        public event Action<WorldState> OnPolicyApplied;

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

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 이 매니저는 DontDestroyOnLoad라
        /// _lastStage/_lastCollapseStage 등이 이전 판(예: WorldCollapse로 끝난 판) 값을 그대로 들고
        /// 있는다 — 기능적으로 치명적이진 않지만(다음 틱에 바로 올바른 값으로 갱신됨), 리셋 안 하면
        /// 새 게임 시작 직후 "WorldCollapse -> NoAwareness" 같은 혼란스러운 역행 로그/이벤트가 뜬다.
        /// </summary>
        public void ResetForNewGame()
        {
            _lastStage = ResistanceStage.NoAwareness;
            _lastMortalityStage = WorldMortalityStage.Stable;
            _lastCollapseStage.Clear();
            _firstCollapseShown = false;
            _firstFullAnarchyShown = false;
        }

        /// <summary>SimulationManager.RunTick()이 매 틱 이후 직접 호출한다(고정 순서, §1번째).</summary>
        public void ProcessTick(WorldState state)
        {
            var stage = state.GetResistanceStage();
            if (stage != _lastStage)
            {
                Debug.Log($"[HumanResistanceManager] 세계 저항 단계 변경: {_lastStage} -> {stage} (visibility={state.plagueVisibility:F2})");
                _lastStage = stage;
                OnResistanceStageChanged?.Invoke(stage);
            }

            var mortalityStage = state.GetMortalityStage();
            if (mortalityStage != _lastMortalityStage)
            {
                _lastMortalityStage = mortalityStage;
                OnMortalityStageChanged?.Invoke(mortalityStage);
            }

            if (Data != null)
            {
                foreach (var country in Data.Countries)
                    ApplyPolicy(country, state, stage);
            }

            OnPolicyApplied?.Invoke(state);
        }

        private void ApplyPolicy(Country country, WorldState state, ResistanceStage stage)
        {
            float visibility = state.plagueVisibility;

            switch (country.developmentLevel)
            {
                case DevelopmentLevel.High:
                    ApplySequentialClosure(country, visibility,
                        qaFastClosureMode ? qaClosureThresholdOverride : highDevLockdownThreshold);
                    break;
                case DevelopmentLevel.Mid:
                    ApplySequentialClosure(country, visibility,
                        qaFastClosureMode ? qaClosureThresholdOverride : midDevLockdownThreshold);
                    break;
                case DevelopmentLevel.Low:
                    // 저개발국: 봉쇄 없음 (설계 문서 5절) — QA 모드에서도 건드리지 않는다(저개발국
                    // 봉쇄 없음은 밸런스 설계 그 자체이지 "느려서 못 보는" 문제가 아니므로 QA 대상이 아님).
                    break;
            }

            float baseFunding = country.developmentLevel switch
            {
                DevelopmentLevel.High => highDevMaxFunding,
                DevelopmentLevel.Mid => midDevMaxFunding,
                DevelopmentLevel.Low => lowDevMaxFunding,
                _ => 0f
            };

            // PublicHealthEmergency(0.4~) 이상부터 "격리 시작, 연구 가속" — 그 이전엔 기여도 낮음
            bool researchAccelerated = stage >= ResistanceStage.PublicHealthEmergency;
            float funding = researchAccelerated ? baseFunding : baseFunding * 0.2f;

            if (stage == ResistanceStage.WorldCollapse)
            {
                // 세계 붕괴: 무정부 상태 -> 치료제 연구 감속 + 정부 안정성 저하
                funding *= worldCollapseFundingMultiplier;
                country.governmentStability = Mathf.Max(0f, country.governmentStability - worldCollapseStabilityDecayPerTick);
            }

            // 국가별 개별 붕괴 단계 — 특정 국가만 이미 궤멸했어도 그 나라의 연구 기여도가 따로 줄어들도록.
            // (전 세계 공통 ResistanceStage와 별개로 국가 자체 사망률만 본다. Docs/PlagueIncReference.md 1절)
            var collapseStage = country.GetCollapseStage();

            // [Step 55] "이 부류 디버그 로그 지워줘" 요청으로 국가별 붕괴 단계 변경 로그를 제거했다 — 48개국이
            // 각자 붕괴 단계가 바뀔 때마다 콘솔에 로그를 남겨 스팸이 심했다(CountryView의 목표색 갱신 로그를
            // 지웠던 Step 54와 같은 종류의 요청). _lastCollapseStage 갱신 자체는 funding 배율 계산에 필요한
            // 로직이라 그대로 유지했다.
            bool hadPrevious = _lastCollapseStage.TryGetValue(country.id, out var lastCollapseStage);
            if (hadPrevious && lastCollapseStage != collapseStage)
            {
                // Important Event Popup System — 게임 전체 최초 1회만. 안전 조건(이전 단계가 정확히
                // Normal이었는지)을 명시적으로 확인해 hadPrevious==false인 초기화 프레임과 확실히
                // 구분한다(사망률 단조 비감소라 Normal 재진입은 불가능 — 이 조건은 평생 최대 1번만 참).
                if (lastCollapseStage == CountryCollapseStage.Normal && collapseStage != CountryCollapseStage.Normal
                    && !_firstCollapseShown)
                {
                    _firstCollapseShown = true;
                    OnFirstCountryCollapse?.Invoke(country);
                }

                if (lastCollapseStage != CountryCollapseStage.FullAnarchy && collapseStage == CountryCollapseStage.FullAnarchy
                    && !_firstFullAnarchyShown)
                {
                    _firstFullAnarchyShown = true;
                    OnFirstFullAnarchy?.Invoke(country);
                }
            }
            _lastCollapseStage[country.id] = collapseStage;

            switch (collapseStage)
            {
                case CountryCollapseStage.Disorder:
                    funding *= disorderFundingMultiplier;
                    break;
                case CountryCollapseStage.NearAnarchy:
                    funding *= nearAnarchyFundingMultiplier;
                    break;
                case CountryCollapseStage.FullAnarchy:
                case CountryCollapseStage.Extinct:
                    funding = 0f;
                    break;
            }

            // 국가별 치료 자금 투자 한계치 — 자연재해 등으로 깎였을 수 있음 (Docs/PlagueIncReference.md 4절,
            // EventManager.ApplyNaturalDisaster). 매 틱 계산된 funding을 이 상한선으로 다시 한 번 클램프한다.
            country.healthFunding = Mathf.Min(funding, country.healthFundingCap);

            // 완전한 무정부 상태: 감염자가 없어도 치안 붕괴로 소량 추가 사망 발생 (원본 게임 특유의 룰).
            if (collapseStage == CountryCollapseStage.FullAnarchy && country.LivingPopulation > 0)
            {
                long unrestDeaths = (long)(country.LivingPopulation * fullAnarchyUnrestDeathRate);
                if (unrestDeaths > 0)
                {
                    unrestDeaths = Math.Min(unrestDeaths, country.LivingPopulation);
                    country.deadCount += unrestDeaths;

                    // deadCount가 늘면 LivingPopulation(=population-deadCount)이 줄어드는데, infectedCount는
                    // 여기서 손대지 않았으니 그대로 두면 "감염자 수 > 생존 인구"가 되어 감염률이 100%를 넘는
                    // 버그가 생긴다(치안붕괴로 죽은 사람 중 일부는 원래 감염자였다고 보는 게 합리적이므로
                    // infectedCount도 새 LivingPopulation을 넘지 않게 같이 깎아준다).
                    country.infectedCount = Math.Min(country.infectedCount, country.LivingPopulation);

                    Debug.Log($"[HumanResistanceManager] {country.name} 완전 무정부 상태 — 치안붕괴 사망 +{unrestDeaths}");
                    Data?.NotifyCountryChanged(country);
                }
            }
        }

        /// <summary>
        /// 나무위키 기준 국경 폐쇄 우선순위 — 항상 공항 먼저, 그다음 국경, 마지막에 항구 순으로 닫힌다
        /// (Docs/PlagueIncReference.md 5절). 기존엔 하나의 임계값을 넘으면 세 개를 동시에 닫았는데,
        /// baseThreshold를 기준으로 공항은 sequentialClosureMargin만큼 일찍, 항구는 그만큼 늦게 닫히도록
        /// 3단계로 쪼갰다 — "항구는 아직 열려 있으니 배로 파고들 시간이 있다" 같은 원본 특유의 긴장감 재현.
        /// </summary>
        private void ApplySequentialClosure(Country country, float visibility, float baseThreshold)
        {
            float airportThreshold = Mathf.Max(0f, baseThreshold - sequentialClosureMargin);
            float borderThreshold = baseThreshold;
            float portThreshold = Mathf.Min(1f, baseThreshold + sequentialClosureMargin);

            if (country.isAirportOpen && visibility >= airportThreshold)
            {
                country.isAirportOpen = false;
                Debug.Log($"[HumanResistanceManager] {country.name}({country.id}) 공항 폐쇄 (1순위, visibility={visibility:F2} >= {airportThreshold:F2})");
            }

            if (!country.isBorderClosed && visibility >= borderThreshold)
            {
                country.isBorderClosed = true;
                Debug.Log($"[HumanResistanceManager] {country.name}({country.id}) 국경 폐쇄 (2순위, visibility={visibility:F2} >= {borderThreshold:F2})");
            }

            if (country.isPortOpen && visibility >= portThreshold)
            {
                country.isPortOpen = false;
                Debug.Log($"[HumanResistanceManager] {country.name}({country.id}) 항구 폐쇄 (3순위, visibility={visibility:F2} >= {portThreshold:F2})");
            }
        }
    }
}
