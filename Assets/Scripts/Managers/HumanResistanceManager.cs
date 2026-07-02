using System;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 인류 저항 AI. 설계 문서 5절.
    /// plagueVisibility 구간(ResistanceStage)에 따라 국가별 방역 조치(국경/항공/항구 봉쇄)와
    /// 치료제 연구 기여도(healthFunding)를 자동으로 갱신한다.
    /// SimulationManager.OnTickCompleted를 구독해 매 틱 이후 실행된다.
    ///
    /// 국가 등급별 봉쇄 임계값(plagueVisibility 기준)과 세계 붕괴 시 안정성 감소율은
    /// 설계 문서에 정확한 수치가 없어 5절의 서술(선진국 빠른 봉쇄 / 개도국 느린 봉쇄 / 저개발국 봉쇄 없음)을
    /// 근거로 합리적으로 정의한 값이며, 인스펙터에서 조정 가능하다.
    /// </summary>
    public class HumanResistanceManager : MonoBehaviour
    {
        public static HumanResistanceManager Instance { get; private set; }

        [Header("국가 등급별 봉쇄 트리거 (plagueVisibility 임계값)")]
        [SerializeField, Tooltip("선진국은 빠르게 봉쇄")] private float highDevLockdownThreshold = 0.5f;
        [SerializeField, Tooltip("개발도상국은 느리게 봉쇄")] private float midDevLockdownThreshold = 0.7f;
        // 저개발국(Low)은 설계 문서 5절에 따라 봉쇄 트리거 없음

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

        /// <summary>저항 단계가 바뀔 때만 발행 — 뉴스 피드(Step 7)에서 활용 예정.</summary>
        public event Action<ResistanceStage> OnResistanceStageChanged;

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

        private void HandleTick(WorldState state)
        {
            var stage = state.GetResistanceStage();
            if (stage != _lastStage)
            {
                _lastStage = stage;
                OnResistanceStageChanged?.Invoke(stage);
            }

            if (Data == null) return;
            foreach (var country in Data.Countries)
                ApplyPolicy(country, state, stage);
        }

        private void ApplyPolicy(Country country, WorldState state, ResistanceStage stage)
        {
            float visibility = state.plagueVisibility;

            switch (country.developmentLevel)
            {
                case DevelopmentLevel.High:
                    if (visibility >= highDevLockdownThreshold) CloseBorders(country);
                    break;
                case DevelopmentLevel.Mid:
                    if (visibility >= midDevLockdownThreshold) CloseBorders(country);
                    break;
                case DevelopmentLevel.Low:
                    // 저개발국: 봉쇄 없음 (설계 문서 5절) — 국경 상태를 건드리지 않는다.
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
            switch (country.GetCollapseStage())
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

            country.healthFunding = funding;

            // 완전한 무정부 상태: 감염자가 없어도 치안 붕괴로 소량 추가 사망 발생 (원본 게임 특유의 룰).
            if (country.GetCollapseStage() == CountryCollapseStage.FullAnarchy && country.LivingPopulation > 0)
            {
                long unrestDeaths = (long)(country.LivingPopulation * fullAnarchyUnrestDeathRate);
                if (unrestDeaths > 0)
                {
                    unrestDeaths = Math.Min(unrestDeaths, country.LivingPopulation);
                    country.deadCount += unrestDeaths;
                    Data?.NotifyCountryChanged(country);
                }
            }
        }

        private void CloseBorders(Country country)
        {
            country.isBorderClosed = true;
            country.isAirportOpen = false;
            country.isPortOpen = false;
        }
    }
}
