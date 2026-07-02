using System;
using System.Collections.Generic;
using System.Linq;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>뉴스 이벤트가 병원체/인류 중 누구에게 유리한지 구분. 설계 문서 8절.</summary>
    public enum NewsEventCategory
    {
        Positive, // 병원체 유리
        Negative  // 인류 유리
    }

    /// <summary>NewsFeed UI(Step 8)가 구독해 화면에 표시할 뉴스 한 건.</summary>
    [Serializable]
    public struct NewsEvent
    {
        public NewsEventCategory category;
        public string text;
        public int day;

        public NewsEvent(NewsEventCategory category, string text, int day)
        {
            this.category = category;
            this.text = text;
            this.day = day;
        }
    }

    /// <summary>이벤트 하나의 발동 확률/쿨다운 설정. 인스펙터에서 밸런싱.</summary>
    [Serializable]
    public class NewsEventSettings
    {
        public bool enabled = true;
        [Range(0f, 1f)] public float triggerChance = 0.15f;
        public int cooldownDays = 10;
    }

    /// <summary>
    /// 뉴스 피드 + 랜덤/조건부 이벤트. 설계 문서 8절.
    /// 설계 문서가 정확한 확률/수치를 정의하지 않아, 6개 이벤트 각각에 합리적인 트리거 임계값과
    /// 확률/쿨다운을 부여했다 — 인스펙터에서 조정 가능.
    /// SimulationManager.OnTickCompleted를 구독해 매 틱마다 발동 조건을 검사한다.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [Header("긍정 이벤트 (병원체 유리)")]
        [SerializeField] private NewsEventSettings naturalDisaster = new NewsEventSettings { triggerChance = 0.12f, cooldownDays = 8 };
        [SerializeField, Range(0f, 1f)] private float naturalDisasterMinInfectionRatio = 0.05f;
        [SerializeField, Range(0f, 1f)] private float naturalDisasterSurgeRatio = 0.2f; // 대상국 감염 가능 인구 중 급증 비율

        [SerializeField] private NewsEventSettings politicalInstability = new NewsEventSettings { triggerChance = 0.1f, cooldownDays = 12 };
        [SerializeField, Range(0f, 1f)] private float politicalInstabilityMinVisibility = 0.3f;
        [SerializeField, Range(0f, 1f)] private float politicalInstabilityStabilityPenalty = 0.2f;

        [SerializeField] private NewsEventSettings medicalStrike = new NewsEventSettings { triggerChance = 0.1f, cooldownDays = 12 };
        [SerializeField, Range(0f, 1f)] private float medicalStrikeMinCureProgress = 0.2f;
        [SerializeField, Range(0f, 1f)] private float medicalStrikeCureProgressPenalty = 0.05f;

        [Header("부정 이벤트 (인류 유리)")]
        [SerializeField] private NewsEventSettings whoEmergencyMeeting = new NewsEventSettings { triggerChance = 0.15f, cooldownDays = 10 };
        [SerializeField] private long whoMeetingMinDeathCount = 50_000;
        [SerializeField, Range(0f, 1f)] private float whoMeetingCureProgressBonus = 0.03f;

        [SerializeField] private NewsEventSettings internationalCooperation = new NewsEventSettings { triggerChance = 0.12f, cooldownDays = 14 };
        [SerializeField, Range(0f, 1f)] private float internationalCooperationMinVisibility = 0.5f;

        [SerializeField] private NewsEventSettings vaccineTrialSuccess = new NewsEventSettings { triggerChance = 0.2f, cooldownDays = 999 };
        [SerializeField, Range(0f, 1f)] private float vaccineTrialMinCureProgress = 0.6f;
        [SerializeField, Range(0f, 1f)] private float vaccineTrialCureProgressJump = 0.1f;

        [Header("처형/폭격 (인류 유리, 나무위키 기준 Docs/PlagueIncReference.md 4절)")]
        [SerializeField, Tooltip("감염 초기 국가를 강제로 감염자 삭감 + 봉쇄시키는 강한 역풍 이벤트")]
        private NewsEventSettings executionOrBombing = new NewsEventSettings { triggerChance = 0.1f, cooldownDays = 15 };
        [SerializeField, Range(0f, 1f)] private float executionMinVisibility = 0.35f;
        [SerializeField, Range(0f, 1f), Tooltip("대상국의 감염 비율이 이 값 이하일 때만 발동 — '아직 초기인 국가'만 노림")]
        private float executionMaxInfectionRatioOfCountry = 0.05f;
        [SerializeField, Range(0f, 1f)] private float executionMinReductionRatio = 0.3f;
        [SerializeField, Range(0f, 1f)] private float executionMaxReductionRatio = 1.0f;

        private readonly Dictionary<string, int> _lastTriggeredDay = new Dictionary<string, int>();
        private readonly HashSet<string> _firedOnce = new HashSet<string>();

        /// <summary>새 뉴스가 발생할 때마다 발행 — NewsFeed UI(Step 8)가 구독할 지점.</summary>
        public event Action<NewsEvent> OnNewsEvent;

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
            var data = WorldDataManager.Instance;
            if (data == null) return;

            TryTrigger("natural_disaster", naturalDisaster, state,
                state.totalPopulation > 0 && (float)state.infectedCount / state.totalPopulation >= naturalDisasterMinInfectionRatio,
                () => ApplyNaturalDisaster(data));

            TryTrigger("political_instability", politicalInstability, state,
                state.plagueVisibility >= politicalInstabilityMinVisibility,
                () => ApplyPoliticalInstability(data));

            TryTrigger("medical_strike", medicalStrike, state,
                state.cureProgress >= medicalStrikeMinCureProgress,
                () => ApplyMedicalStrike(state));

            TryTrigger("who_emergency_meeting", whoEmergencyMeeting, state,
                state.deadCount >= whoMeetingMinDeathCount,
                () => ApplyWhoEmergencyMeeting(state));

            TryTrigger("international_cooperation", internationalCooperation, state,
                state.plagueVisibility >= internationalCooperationMinVisibility,
                () => ApplyInternationalCooperation(data));

            TryTrigger("vaccine_trial_success", vaccineTrialSuccess, state,
                state.cureProgress >= vaccineTrialMinCureProgress,
                () => ApplyVaccineTrialSuccess(state), oneShot: true);

            TryTrigger("execution_or_bombing", executionOrBombing, state,
                state.plagueVisibility >= executionMinVisibility,
                () => ApplyExecutionOrBombing(data));
        }

        private void TryTrigger(string id, NewsEventSettings settings, WorldState state, bool conditionMet,
            Func<bool> applyEffect, bool oneShot = false)
        {
            if (!settings.enabled || !conditionMet) return;
            if (oneShot && _firedOnce.Contains(id)) return;

            _lastTriggeredDay.TryGetValue(id, out int lastDay);
            if (state.currentDay - lastDay < settings.cooldownDays) return;
            if (UnityEngine.Random.value > settings.triggerChance) return;

            bool applied = applyEffect();
            if (!applied) return; // 조건은 맞았지만 대상(국가 등)이 없어 실제로는 발동 못한 경우 - 쿨다운/1회성 기록 안 함

            _lastTriggeredDay[id] = state.currentDay;
            if (oneShot) _firedOnce.Add(id);
        }

        // --- 긍정 이벤트 (병원체 유리) ---

        private bool ApplyNaturalDisaster(WorldDataManager data)
        {
            var candidates = data.Countries.Where(c => c.infectedCount > 0 && c.SusceptibleCount > 0).ToList();
            if (candidates.Count == 0) return false;

            var country = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            long surge = (long)(country.SusceptibleCount * naturalDisasterSurgeRatio);
            surge = Math.Clamp(surge, 1, country.SusceptibleCount);
            country.infectedCount += surge;
            data.NotifyCountryChanged(country);

            RaiseNews(NewsEventCategory.Positive, $"[속보] {country.name}에서 대규모 자연재해 발생 — 감염이 급격히 확산되고 있습니다.");
            return true;
        }

        private bool ApplyPoliticalInstability(WorldDataManager data)
        {
            var countries = data.Countries;
            if (countries.Count == 0) return false;

            var country = countries[UnityEngine.Random.Range(0, countries.Count)];
            country.isBorderClosed = false;
            country.isAirportOpen = true;
            country.isPortOpen = true;
            country.governmentStability = Mathf.Max(0f, country.governmentStability - politicalInstabilityStabilityPenalty);
            data.NotifyCountryChanged(country);

            RaiseNews(NewsEventCategory.Positive, $"[속보] {country.name} 정치 불안 심화 — 방역 조치가 무력화되고 있습니다.");
            return true;
        }

        private bool ApplyMedicalStrike(WorldState state)
        {
            state.cureProgress = Mathf.Max(0f, state.cureProgress - medicalStrikeCureProgressPenalty);
            RaiseNews(NewsEventCategory.Positive, "[속보] 전 세계 의료진 파업 발생 — 치료제 연구가 지연되고 있습니다.");
            return true;
        }

        // --- 부정 이벤트 (인류 유리) ---

        private bool ApplyWhoEmergencyMeeting(WorldState state)
        {
            state.cureProgress = Mathf.Clamp01(state.cureProgress + whoMeetingCureProgressBonus);
            RaiseNews(NewsEventCategory.Negative, "[속보] WHO 긴급회의 개최 — 국제 공조로 치료제 연구가 가속화됩니다.");
            return true;
        }

        private bool ApplyInternationalCooperation(WorldDataManager data)
        {
            bool anyChanged = false;
            foreach (var country in data.Countries)
            {
                if (country.developmentLevel == DevelopmentLevel.Low) continue;
                if (country.isBorderClosed) continue;

                country.isBorderClosed = true;
                country.isAirportOpen = false;
                country.isPortOpen = false;
                data.NotifyCountryChanged(country);
                anyChanged = true;
            }

            if (anyChanged)
                RaiseNews(NewsEventCategory.Negative, "[속보] 국제 공조 강화 — 각국이 일제히 국경을 봉쇄합니다.");
            return anyChanged;
        }

        private bool ApplyVaccineTrialSuccess(WorldState state)
        {
            state.cureProgress = Mathf.Clamp01(state.cureProgress + vaccineTrialCureProgressJump);
            RaiseNews(NewsEventCategory.Negative, "[속보] 백신 임상시험 성공 — 치료제 개발이 크게 진전되었습니다.");
            return true;
        }

        /// <summary>
        /// 원본 게임 특유의 강한 역풍 이벤트 — 아직 감염 초기인 국가를 노려 감염자를 대폭 삭감하고
        /// 국경/공항/항구를 강제로 봉쇄한다. 섬나라에서 뜨면 사실상 그 나라는 재감염이 거의 불가능해져
        /// 뉴스피드에 극적인 순간을 만들어준다. (Docs/PlagueIncReference.md 4절)
        /// </summary>
        private bool ApplyExecutionOrBombing(WorldDataManager data)
        {
            var candidates = data.Countries.Where(c =>
                c.infectedCount > 0 && !c.isBorderClosed && c.LivingPopulation > 0 &&
                (float)c.infectedCount / c.LivingPopulation <= executionMaxInfectionRatioOfCountry).ToList();
            if (candidates.Count == 0) return false;

            var country = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            float reductionRatio = UnityEngine.Random.Range(executionMinReductionRatio, executionMaxReductionRatio);
            long reduced = (long)(country.infectedCount * reductionRatio);
            reduced = Math.Clamp(reduced, 1, country.infectedCount);

            country.infectedCount -= reduced;
            country.isBorderClosed = true;
            country.isAirportOpen = false;
            country.isPortOpen = false;
            data.NotifyCountryChanged(country);

            RaiseNews(NewsEventCategory.Negative,
                $"[속보] {country.name} 정부, 감염자 처형 및 지역 폭격 단행 — 감염자 수가 급감하고 국경이 전면 봉쇄되었습니다.");
            return true;
        }

        private void RaiseNews(NewsEventCategory category, string text)
        {
            int day = WorldDataManager.Instance?.State.currentDay ?? 0;
            OnNewsEvent?.Invoke(new NewsEvent(category, text, day));
        }
    }
}
