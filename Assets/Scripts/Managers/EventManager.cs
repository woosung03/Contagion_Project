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
        Negative, // 인류 유리

        /// <summary>
        /// 게임 효과 없는 플레이버 텍스트 전용 이벤트 (올림픽/월드컵 등). 나무위키 "게임성에 큰 영향은
        /// 없고 텍스트 다양성용" (Docs/PlagueIncReference.md 4절) — 뉴스 피드 다양성 목적으로만 존재.
        /// </summary>
        Flavor
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
    /// SimulationManager.RunTick()이 매 틱 이후 ProcessTick()을 직접, 고정된 순서로(
    /// HumanResistanceManager 다음) 호출한다 — Deterministic Tick Ordering #1. 예전엔 이 매니저도
    /// SimulationManager.OnTickCompleted를 독립적으로 구독해서, 같은 틱 안에서 봉쇄 판정
    /// (HumanResistanceManager)과 이벤트 판정 중 어느 쪽이 먼저 실행되는지가 Unity의 컴포넌트
    /// 초기화 순서에 따라 달라질 수 있었다(Docs/Archive/WorldSimulationSystem_Design.md §9.1).
    ///
    /// Event Queue(World Simulation Engine #2, Docs/Archive/WorldSimulationSystem_Design.md §9.3):
    /// ProcessTick()에서 이벤트가 발동해도 Country/WorldState를 그 자리에서 바로 고치지 않는다 —
    /// "무슨 일이 일어났는가"(대상 국가, 굴린 배율)만 확정해 <see cref="_pendingChanges"/>에 쌓아두고,
    /// 실제 필드 대입은 SimulationManager.RunTick()이 다음 틱 시작 시점에 <see cref="ApplyQueuedChanges"/>
    /// 로 FIFO 순서 그대로 일괄 반영한다. 이벤트가 "이번 틱 결과에 끼어들지" 않고 "다음 틱의 입력을
    /// 바꾸는" 규칙이 되어, 그 틱에 이미 끝난 국가별 계산에 어느 이벤트가 먼저/나중에 반영됐는지가
    /// 실행마다 달라지는 문제가 원천적으로 사라진다. 게임 효과가 없는 <see cref="ApplyFlavorEvent"/>는
    /// 큐에 쌓을 State 변경이 아예 없어 그대로 즉시 실행한다.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [Header("긍정 이벤트 (병원체 유리)")]
        [SerializeField] private NewsEventSettings naturalDisaster = new NewsEventSettings { triggerChance = 0.12f, cooldownDays = 8 };
        [SerializeField, Range(0f, 1f)] private float naturalDisasterMinInfectionRatio = 0.05f;
        [SerializeField, Range(0f, 1f)] private float naturalDisasterSurgeRatio = 0.2f; // 대상국 감염 가능 인구 중 급증 비율
        [SerializeField, Range(0f, 1f), Tooltip("나무위키: 재해로 사망자가 나면 그 나라의 치료 자금 투자 " +
            "한계치 자체가 영구적으로 낮아진다 (Docs/PlagueIncReference.md 4절)")]
        private float naturalDisasterFundingCapPenalty = 0.15f;

        [SerializeField] private NewsEventSettings politicalInstability = new NewsEventSettings { triggerChance = 0.1f, cooldownDays = 12 };
        [SerializeField, Range(0f, 1f)] private float politicalInstabilityMinVisibility = 0.3f;
        [SerializeField, Range(0f, 1f)] private float politicalInstabilityStabilityPenalty = 0.2f;

        [SerializeField] private NewsEventSettings medicalStrike = new NewsEventSettings { triggerChance = 0.1f, cooldownDays = 12 };
        [SerializeField, Range(0f, 1f)] private float medicalStrikeMinCureProgress = 0.2f;
        [SerializeField, Range(0f, 1f)] private float medicalStrikeCureProgressPenalty = 0.05f;

        [Header("부정 이벤트 (인류 유리)")]
        [SerializeField] private NewsEventSettings whoEmergencyMeeting = new NewsEventSettings { triggerChance = 0.15f, cooldownDays = 10 };
        [SerializeField] private long whoMeetingMinDeathCount = 50_000_000; // 인구 스케일을 실제 인구 수 그대로(스케일 제거)로 바꾼 데 맞춰 1000배 상향
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

        [Header("플레이버 이벤트 (게임 효과 없음, 텍스트 다양성용, Docs/PlagueIncReference.md 4절)")]
        [SerializeField] private NewsEventSettings flavorEvent = new NewsEventSettings { triggerChance = 0.08f, cooldownDays = 20 };
        [SerializeField] private string[] flavorEventTexts =
        {
            "[속보] 전 세계인의 이목이 하계 올림픽 개막식으로 쏠렸습니다.",
            "[속보] 월드컵 예선 경기 결과로 전 세계가 떠들썩합니다.",
            "[속보] 유명 팝스타의 월드투어 콘서트가 각국에서 매진 행렬을 이어가고 있습니다.",
            "[속보] 국제 우주정거장에서 새로운 실험 결과가 발표되었습니다.",
        };

        private readonly Dictionary<string, int> _lastTriggeredDay = new Dictionary<string, int>();
        private readonly HashSet<string> _firedOnce = new HashSet<string>();

        /// <summary>
        /// Event Queue에 쌓이는 변경의 종류. 값 이름 자체가 "어느 이벤트가 무슨 효과를 냈는가"를
        /// 그대로 드러내도록 이벤트 단위로 하나씩 정의한다(범용 "AddInfected" 같은 이름 대신 —
        /// 그러면 로그나 디버거에서 어느 ApplyXXX가 만든 항목인지 알 수 없다).
        /// </summary>
        private enum ChangeType
        {
            /// <summary>자연재해 — Country 대상. Amount=감염 급증 비율, ExtraAmount=healthFundingCap 감소량.</summary>
            NaturalDisasterSurge,
            /// <summary>정치 불안 — Country 대상. Amount=governmentStability 감소량(국경/공항/항구는 전부 재개방).</summary>
            PoliticalInstabilityReopen,
            /// <summary>의료진 파업 — WorldState 대상(Country=null). Amount=cureProgress 감소량.</summary>
            MedicalStrikeCurePenalty,
            /// <summary>WHO 긴급회의 — WorldState 대상. Amount=cureProgress 증가량.</summary>
            WhoEmergencyCureBonus,
            /// <summary>국제 공조 봉쇄 — Country 대상(대상국마다 개별 항목). Amount 미사용.</summary>
            InternationalCooperationClose,
            /// <summary>백신 임상 성공 — WorldState 대상. Amount=cureProgress 증가량.</summary>
            VaccineTrialCureJump,
            /// <summary>처형/폭격 — Country 대상. Amount=감염자 삭감 비율(국경/공항/항구도 함께 봉쇄).</summary>
            ExecutionOrBombingStrike,
        }

        /// <summary>
        /// Event Queue 항목 하나 — "변경 종류 + 대상(Country 또는 WorldState) + 적용에 필요한 값"만
        /// 담는 순수 데이터. Country가 null이면 WorldState(전역) 대상이라는 뜻. 다형성/가상 디스패치
        /// 없이 <see cref="ApplyQueuedChanges"/>의 switch 하나로 전부 처리한다 — 이벤트마다 별도
        /// Command 클래스를 만들지 않기 위한 최소 구조.
        /// </summary>
        private readonly struct PendingChange
        {
            public readonly ChangeType Type;
            public readonly Country Country; // null = WorldState 대상
            public readonly float Amount;
            public readonly float ExtraAmount; // NaturalDisasterSurge 전용 2번째 값(healthFundingCap 감소량)

            public PendingChange(ChangeType type, Country country, float amount = 0f, float extraAmount = 0f)
            {
                Type = type;
                Country = country;
                Amount = amount;
                ExtraAmount = extraAmount;
            }

            public override string ToString() =>
                Country != null
                    ? $"{Type}(country={Country.id}, amount={Amount:F3}, extra={ExtraAmount:F3})"
                    : $"{Type}(worldState, amount={Amount:F3})";
        }

        /// <summary>
        /// Event Queue(World Simulation Engine #2) — Docs/Archive/WorldSimulationSystem_Design.md
        /// §9.3 "간접 수정 큐"의 최소 구현. ProcessTick()에서 이벤트가 발동하면 Country/WorldState를
        /// 그 자리에서 바로 고치지 않고 <see cref="PendingChange"/> 형태로 여기 FIFO로 쌓아두기만
        /// 한다 — 그래야 그 틱에 이미 끝난 국가별 계산(SimulationManager 국가 루프)에 끼어들지 않고,
        /// "다음 틱의 입력"으로만 반영된다. Queue&lt;PendingChange&gt;가 FIFO를 그대로 보장한다.
        /// </summary>
        private readonly Queue<PendingChange> _pendingChanges = new Queue<PendingChange>();

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

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 이 매니저는 DontDestroyOnLoad라
        /// _lastTriggeredDay(쿨다운 기준일)가 이전 판의 큰 day 값을 그대로 들고 있으면, 새 게임은
        /// currentDay가 0부터 시작하므로 "currentDay - lastDay"가 한참 음수가 되어 사실상 모든 뉴스
        /// 이벤트가 오랫동안 발동 안 하는 버그가 있었다. _firedOnce(백신 임상 성공 등 1회성 이벤트)도
        /// 안 비우면 두 번째 판부터는 영원히 발동하지 않는다.
        /// </summary>
        public void ResetForNewGame()
        {
            _lastTriggeredDay.Clear();
            _firedOnce.Clear();
            _pendingChanges.Clear();
        }

        /// <summary>
        /// SimulationManager.RunTick()이 매 틱 "시작 시점"(국가 루프보다 먼저) 직접 호출한다 —
        /// 직전 틱에 쌓인 Event Queue를 FIFO 순서 그대로 전부 비운다(Docs/Archive/
        /// WorldSimulationSystem_Design.md §9.3, §10 "다음 Tick(적용 큐 반영이 여기서 실제로
        /// State에 반영됨)"). 같은 틱에서 여러 이벤트가 발동했다면 그 발동 순서(=Enqueue 순서)
        /// 그대로 적용된다 — Queue&lt;T&gt;는 FIFO를 보장하므로 별도 정렬이 필요 없다.
        /// </summary>
        public void ApplyQueuedChanges()
        {
            if (_pendingChanges.Count == 0) return;

            var data = WorldDataManager.Instance;
            var state = data?.State;
            while (_pendingChanges.Count > 0)
                ApplyChange(_pendingChanges.Dequeue(), data, state);
        }

        /// <summary>
        /// PendingChange 하나를 실제 필드에 반영한다. 어느 이벤트가 무슨 변경을 냈는지 switch만
        /// 읽으면 바로 보이도록, 이벤트 단위 ChangeType 하나당 case 하나 — 다형성/가상 디스패치 없음.
        /// 대상 값(SusceptibleCount/infectedCount)이 트리거 시점 이후 바뀌었을 수 있어(같은 틱에서
        /// 이 항목보다 나중에 실행되는 TransportManager 도착 판정 등) 비율(Amount)만 큐에 담아두고
        /// 실제 증감량은 여기서 "지금" 값 기준으로 계산한다 — 원본 동기 코드도 항상 그 순간의
        /// 현재 값으로 계산했고, 큐 방식에서 그 순간이 트리거가 아니라 적용으로 옮겨간 것뿐.
        /// </summary>
        private static void ApplyChange(PendingChange change, WorldDataManager data, WorldState state)
        {
            switch (change.Type)
            {
                case ChangeType.NaturalDisasterSurge:
                {
                    var country = change.Country;
                    if (country == null || country.SusceptibleCount <= 0) return; // 적용 시점엔 이미 감염 가능 인구가 없을 수 있음

                    long surge = (long)(country.SusceptibleCount * change.Amount);
                    surge = Math.Clamp(surge, 1, country.SusceptibleCount);
                    country.infectedCount += surge;

                    // 나무위키: 재해로 사망자가 나면 그 나라의 치료 자금 투자 한계치가 영구적으로 낮아진다
                    // (Docs/PlagueIncReference.md 4절) — HumanResistanceManager.ApplyPolicy가 이 상한선으로
                    // healthFunding을 매 틱 다시 클램프하므로, 여기서는 캡 자체만 낮추면 자연스럽게 반영된다.
                    country.healthFundingCap = Mathf.Max(0f, country.healthFundingCap - change.ExtraAmount);
                    data?.NotifyCountryChanged(country);
                    return;
                }
                case ChangeType.PoliticalInstabilityReopen:
                {
                    var country = change.Country;
                    if (country == null) return;

                    country.isBorderClosed = false;
                    country.isAirportOpen = true;
                    country.isPortOpen = true;
                    country.governmentStability = Mathf.Max(0f, country.governmentStability - change.Amount);
                    data?.NotifyCountryChanged(country);
                    return;
                }
                case ChangeType.MedicalStrikeCurePenalty:
                    if (state != null) state.cureProgress = Mathf.Max(0f, state.cureProgress - change.Amount);
                    return;
                case ChangeType.WhoEmergencyCureBonus:
                    if (state != null) state.cureProgress = Mathf.Clamp01(state.cureProgress + change.Amount);
                    return;
                case ChangeType.InternationalCooperationClose:
                {
                    var country = change.Country;
                    if (country == null || country.isBorderClosed) return; // 그사이 이미 봉쇄됐으면 중복 대입 생략

                    country.isBorderClosed = true;
                    country.isAirportOpen = false;
                    country.isPortOpen = false;
                    data?.NotifyCountryChanged(country);
                    return;
                }
                case ChangeType.VaccineTrialCureJump:
                    if (state != null) state.cureProgress = Mathf.Clamp01(state.cureProgress + change.Amount);
                    return;
                case ChangeType.ExecutionOrBombingStrike:
                {
                    var country = change.Country;
                    if (country == null || country.infectedCount <= 0) return; // 적용 시점엔 이미 감염자가 없을 수 있음

                    long reduced = (long)(country.infectedCount * change.Amount);
                    reduced = Math.Clamp(reduced, 1, country.infectedCount);
                    country.infectedCount -= reduced;
                    country.isBorderClosed = true;
                    country.isAirportOpen = false;
                    country.isPortOpen = false;
                    data?.NotifyCountryChanged(country);
                    return;
                }
            }
        }

        /// <summary>SimulationManager.RunTick()이 매 틱 이후 직접 호출한다(고정 순서, HumanResistanceManager 다음).</summary>
        public void ProcessTick(WorldState state)
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

            TryTrigger("flavor_event", flavorEvent, state, true,
                () => ApplyFlavorEvent());
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

        /// <summary>
        /// 대상 국가와 굴린 비율(Amount/ExtraAmount)은 지금(트리거 시점) 확정해 큐에 담지만,
        /// 실제 증가량(surge)은 적용 시점(<see cref="ApplyChange"/>)에서 그 순간의 살아있는
        /// SusceptibleCount로 다시 계산한다 — 같은 틱 안에서 이 이벤트보다 나중에 실행되는
        /// TransportManager 도착 판정이 이 국가의 infectedCount를 먼저 바꿀 수 있기 때문.
        /// </summary>
        private bool ApplyNaturalDisaster(WorldDataManager data)
        {
            var candidates = data.Countries.Where(c => c.infectedCount > 0 && c.SusceptibleCount > 0).ToList();
            if (candidates.Count == 0) return false;

            var country = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            _pendingChanges.Enqueue(new PendingChange(ChangeType.NaturalDisasterSurge, country,
                naturalDisasterSurgeRatio, naturalDisasterFundingCapPenalty));

            RaiseNews(NewsEventCategory.Positive,
                $"[속보] {country.name}에서 대규모 자연재해 발생 — 감염이 급격히 확산되고 의료 인프라 투자 여력이 줄어들고 있습니다.");
            return true;
        }

        private bool ApplyPoliticalInstability(WorldDataManager data)
        {
            var countries = data.Countries;
            if (countries.Count == 0) return false;

            var country = countries[UnityEngine.Random.Range(0, countries.Count)];
            _pendingChanges.Enqueue(new PendingChange(ChangeType.PoliticalInstabilityReopen, country,
                politicalInstabilityStabilityPenalty));

            RaiseNews(NewsEventCategory.Positive, $"[속보] {country.name} 정치 불안 심화 — 방역 조치가 무력화되고 있습니다.");
            return true;
        }

        private bool ApplyMedicalStrike(WorldState state)
        {
            _pendingChanges.Enqueue(new PendingChange(ChangeType.MedicalStrikeCurePenalty, null,
                medicalStrikeCureProgressPenalty));

            RaiseNews(NewsEventCategory.Positive, "[속보] 전 세계 의료진 파업 발생 — 치료제 연구가 지연되고 있습니다.");
            return true;
        }

        // --- 부정 이벤트 (인류 유리) ---

        private bool ApplyWhoEmergencyMeeting(WorldState state)
        {
            _pendingChanges.Enqueue(new PendingChange(ChangeType.WhoEmergencyCureBonus, null,
                whoMeetingCureProgressBonus));

            RaiseNews(NewsEventCategory.Negative, "[속보] WHO 긴급회의 개최 — 국제 공조로 치료제 연구가 가속화됩니다.");
            return true;
        }

        /// <summary>
        /// 대상국 목록(developmentLevel/isBorderClosed 필터)은 트리거 시점 스냅샷으로 확정한다 —
        /// TryTrigger()가 이 반환값(적용 대상이 있었는지)으로 쿨다운 기록 여부를 즉시 판단해야 하기
        /// 때문. 국가마다 개별 PendingChange를 하나씩 큐에 담아, 적용 시점에도(그사이 이미 봉쇄된
        /// 국가는 <see cref="ApplyChange"/>가 다시 한번 걸러 중복 대입을 피한다) 원래 foreach와
        /// 동일한 순서로 하나씩 반영된다.
        /// </summary>
        private bool ApplyInternationalCooperation(WorldDataManager data)
        {
            var targets = data.Countries
                .Where(c => c.developmentLevel != DevelopmentLevel.Low && !c.isBorderClosed)
                .ToList();
            if (targets.Count == 0) return false;

            foreach (var country in targets)
                _pendingChanges.Enqueue(new PendingChange(ChangeType.InternationalCooperationClose, country));

            RaiseNews(NewsEventCategory.Negative, "[속보] 국제 공조 강화 — 각국이 일제히 국경을 봉쇄합니다.");
            return true;
        }

        private bool ApplyVaccineTrialSuccess(WorldState state)
        {
            _pendingChanges.Enqueue(new PendingChange(ChangeType.VaccineTrialCureJump, null,
                vaccineTrialCureProgressJump));

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
            _pendingChanges.Enqueue(new PendingChange(ChangeType.ExecutionOrBombingStrike, country, reductionRatio));

            RaiseNews(NewsEventCategory.Negative,
                $"[속보] {country.name} 정부, 감염자 처형 및 지역 폭격 단행 — 감염자 수가 급감하고 국경이 전면 봉쇄되었습니다.");
            return true;
        }

        /// <summary>
        /// 게임 효과가 전혀 없는 플레이버 텍스트 이벤트 — 올림픽/월드컵 등 뉴스 피드 다양성용.
        /// 조건 없이(항상 true) 확률/쿨다운만으로 발동한다. (Docs/PlagueIncReference.md 4절)
        /// </summary>
        private bool ApplyFlavorEvent()
        {
            if (flavorEventTexts == null || flavorEventTexts.Length == 0) return false;

            string text = flavorEventTexts[UnityEngine.Random.Range(0, flavorEventTexts.Length)];
            RaiseNews(NewsEventCategory.Flavor, text);
            return true;
        }

        private void RaiseNews(NewsEventCategory category, string text)
        {
            // 콘솔에 뉴스 이벤트마다 Debug.Log를 남기던 것을 제거 — 인게임 뉴스피드(NewsFeedController가
            // 구독하는 OnNewsEvent)에는 영향 없음, 콘솔 스팸만 없앤 것.
            int day = WorldDataManager.Instance?.State.currentDay ?? 0;
            OnNewsEvent?.Invoke(new NewsEvent(category, text, day));
        }
    }
}
