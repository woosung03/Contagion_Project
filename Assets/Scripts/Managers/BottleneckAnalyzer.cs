using System;
using System.Collections.Generic;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 병목 분석기(Phase 1) — "왜 이 연구를 지금 해야 하는가"를 이해시키기 위한 게임 디자인 설계의
    /// 1단계 구현체. WorldState/Pathogen/UpgradeManager 기존 필드만 읽는 순수 파생 계산 계층이며,
    /// 어떤 시뮬레이션 값도 쓰지 않는다 — SimulationManager/HumanResistanceManager/UpgradeManager는
    /// 이 클래스의 존재를 모른다(단방향 의존).
    ///
    /// Phase 1 범위: SpreadStalled_LowInfectivity(A4) / DeathLow_LowLethality(B2) /
    /// DetectionFast_HighSeverity(C1) / CureRace_Urgent(D2) / ResourceStarved(E) 5종만 판정한다.
    /// 정보성 병목(A1/A2/A3/B1/D1/D3/C2)은 Phase 1.5에서 추가 — 지금은 해당 원인으로 확산/사망/
    /// 발각이 정체돼도 "판정 없음"으로 지나간다(거짓으로 A4/B2/C1이라 단정하지 않기 위한 의도적 여백).
    ///
    /// HumanResistanceManager.OnPolicyApplied(그 틱의 봉쇄/연구기여도 갱신이 끝난 뒤 발행)를
    /// 구독한다 — SimulationManager.OnTickCompleted를 직접 구독하면 실행 순서가 보장되지 않아
    /// 국가 상태를 그 틱 갱신 전 값으로 읽을 위험이 있었다(승인된 설계 §1 해결책 B).
    /// </summary>
    public class BottleneckAnalyzer : MonoBehaviour
    {
        public static BottleneckAnalyzer Instance { get; private set; }

        [Header("판정 윈도우")]
        [SerializeField, Tooltip("추세 판단에 사용하는 과거 틱 수(W)")]
        private int historyWindowTicks = 5;

        [Header("A4 — 확산 정체(순수 감염력 부족)")]
        [SerializeField, Tooltip("직전 W틱 대비 최근 W틱 신규 감염 비율이 이 값 미만이면 정체로 판정")]
        private float spreadStallRatio = 0.8f;
        [SerializeField, Tooltip("전 세계 미감염 생존자 비율이 이 값 미만이면(감염 대상이 거의 안 남음) " +
            "A4 판정에서 제외 — 후반 자연 둔화를 병목으로 오인하지 않기 위함")]
        private float minSusceptibleRatioForA4 = 0.05f;

        [Header("B2 — 치사율 부족")]
        [SerializeField] private float lowLethalityThreshold = 0.15f;
        [SerializeField, Tooltip("최근 W틱 (사망 증가량/감염 증가량) 비율이 이 값 미만이면 " +
            "'아직 잘 안 죽이고 있다'로 판정")]
        private float lowDeathToInfectionRatio = 0.05f;

        [Header("C1 — 발각 가속(severity 과다)")]
        [SerializeField, Tooltip("최근 W틱 plagueVisibility 증가량이 이 값을 넘으면 판정")]
        private float visibilitySurgeThreshold = 0.05f;

        [Header("D2 — 치료제 위협")]
        [SerializeField, Tooltip("치료제 완성까지 예상 남은 틱 수가 이 값 이하면 위협으로 판정")]
        private float cureUrgentEtaTicks = 30f;

        private struct Snapshot
        {
            public long infected;
            public long dead;
            public float cureProgress;
            public float visibility;
        }

        // Queue가 아니라 List — Wticks/2Wticks 전 값에 인덱스로 바로 접근해야 해서
        // (RemoveAt(0)은 O(n)이지만 n=2W+1 정도라 틱 주기에서 비용 무시 가능).
        private readonly List<Snapshot> _history = new List<Snapshot>();
        private int HistoryCapacity => historyWindowTicks * 2 + 1;

        public BottleneckReport CurrentReport { get; private set; } = BottleneckReport.None;
        public event Action<BottleneckReport> OnBottleneckChanged;

        private WorldDataManager Data => WorldDataManager.Instance;
        private UpgradeManager Upgrades => UpgradeManager.Instance;

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
            if (HumanResistanceManager.Instance == null) return;
            HumanResistanceManager.Instance.OnPolicyApplied -= HandleTick;
            HumanResistanceManager.Instance.OnPolicyApplied += HandleTick;
        }

        private void OnDisable()
        {
            if (HumanResistanceManager.Instance != null)
                HumanResistanceManager.Instance.OnPolicyApplied -= HandleTick;
        }

        /// <summary>새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출 — 이전 판의 롤링
        /// 히스토리/마지막 판정이 새 판에 새어 들어가지 않도록 초기화한다.</summary>
        public void ResetForNewGame()
        {
            _history.Clear();
            CurrentReport = BottleneckReport.None;
        }

        private void HandleTick(WorldState state)
        {
            PushSnapshot(state);
            if (_history.Count < HistoryCapacity) return; // 판정에 필요한 이력이 아직 안 쌓임

            var report = Evaluate(state);
            bool changed = report.Type != CurrentReport.Type;
            CurrentReport = report;
            if (changed)
                OnBottleneckChanged?.Invoke(report);
        }

        private void PushSnapshot(WorldState state)
        {
            _history.Add(new Snapshot
            {
                infected = state.infectedCount,
                dead = state.deadCount,
                cureProgress = state.cureProgress,
                visibility = state.plagueVisibility
            });
            if (_history.Count > HistoryCapacity)
                _history.RemoveAt(0);
        }

        /// <summary>우선순위: E(자원 고갈) → D2(치료제 임박) → C1(봉쇄 가속) → A4(확산 정체) →
        /// B2(치사율 부족). 여러 조건이 동시에 참이어도 가장 시급한 것 하나만 보고한다 —
        /// 여러 개를 한꺼번에 보여주면 다시 "정보 과잉"이 된다.</summary>
        private BottleneckReport Evaluate(WorldState state)
        {
            if (TryEvaluateResourceStarved(out var e)) return e;
            if (TryEvaluateCureRaceUrgent(state, out var d2)) return d2;
            if (TryEvaluateDetectionFast(out var c1)) return c1;
            if (TryEvaluateSpreadStalled(state, out var a4)) return a4;
            if (TryEvaluateLowLethality(state, out var b2)) return b2;
            return BottleneckReport.None;
        }

        private bool TryEvaluateSpreadStalled(WorldState state, out BottleneckReport report)
        {
            report = BottleneckReport.None;
            int w = historyWindowTicks;
            var oldest = _history[0];
            var mid = _history[w];
            var latest = _history[_history.Count - 1];

            long recentDelta = latest.infected - mid.infected;
            long priorDelta = mid.infected - oldest.infected;
            if (priorDelta <= 0) return false; // 애초에 늘고 있지 않았으면 "정체로의 전환"을 판단할 기준이 없음

            float ratio = (float)recentDelta / priorDelta;
            if (ratio >= spreadStallRatio) return false;

            // 감염 대상 자체가 거의 안 남아 자연히 느려진 후반 국면은 병목이 아니라 정상 현상 — 제외.
            // WorldState 필드만으로 계산(국가 순회 불필요).
            long globalLiving = state.totalPopulation - state.deadCount;
            long globalSusceptible = globalLiving - state.infectedCount;
            float susceptibleRatio = state.totalPopulation > 0 ? (float)globalSusceptible / state.totalPopulation : 0f;
            if (susceptibleRatio < minSusceptibleRatioForA4) return false;

            var severity = SeverityFromRatio(ratio, spreadStallRatio, higherIsWorse: false);
            report = new BottleneckReport(BottleneckType.SpreadStalled_LowInfectivity, severity, actionable: true,
                relevantStat: "infectivity",
                evidence: $"최근 {w}틱 신규 감염 {recentDelta:N0}명(직전 {w}틱 {priorDelta:N0}명 대비 {ratio:P0})");
            return true;
        }

        private bool TryEvaluateLowLethality(WorldState state, out BottleneckReport report)
        {
            report = BottleneckReport.None;
            var pathogen = Data?.CurrentPathogen;
            if (pathogen == null) return false;
            if (pathogen.lethality >= lowLethalityThreshold) return false;
            if (state.infectedCount <= 0) return false;

            int w = historyWindowTicks;
            var mid = _history[_history.Count - 1 - w];
            var latest = _history[_history.Count - 1];

            long dInfected = latest.infected - mid.infected;
            long dDead = latest.dead - mid.dead;
            if (dInfected <= 0) return false; // 확산 자체가 없으면 이 판정은 의미 없음(A4가 이미 다룸)

            float deathRatio = (float)dDead / dInfected;
            if (deathRatio >= lowDeathToInfectionRatio) return false;

            var severity = SeverityFromRatio(pathogen.lethality, lowLethalityThreshold, higherIsWorse: false);
            report = new BottleneckReport(BottleneckType.DeathLow_LowLethality, severity, actionable: true,
                relevantStat: "lethality",
                evidence: $"현재 치사율 {pathogen.lethality:P0}, 최근 {w}틱 사망/감염 비율 {deathRatio:P1}");
            return true;
        }

        private bool TryEvaluateDetectionFast(out BottleneckReport report)
        {
            report = BottleneckReport.None;
            int w = historyWindowTicks;
            var mid = _history[_history.Count - 1 - w];
            var latest = _history[_history.Count - 1];

            float dVisibility = latest.visibility - mid.visibility;
            if (dVisibility < visibilitySurgeThreshold) return false;
            if (latest.visibility >= 0.999f) return false; // 이미 최대치라 경고 의미 없음

            var severity = SeverityFromRatio(dVisibility, visibilitySurgeThreshold, higherIsWorse: true);
            report = new BottleneckReport(BottleneckType.DetectionFast_HighSeverity, severity, actionable: true,
                relevantStat: "severity", // ResearchRecommender가 이 stat의 "음수" delta를 우대 점수화(§6)
                evidence: $"최근 {w}틱 발각도 +{dVisibility:P1} 상승(현재 {latest.visibility:P0})");
            return true;
        }

        private bool TryEvaluateCureRaceUrgent(WorldState state, out BottleneckReport report)
        {
            report = BottleneckReport.None;
            if (!state.cureResearchStarted) return false; // D1(미착수) — Phase 1.5, 지금은 스킵

            int w = historyWindowTicks;
            var mid = _history[_history.Count - 1 - w];
            var latest = _history[_history.Count - 1];

            float dCure = latest.cureProgress - mid.cureProgress;
            if (dCure <= 0f) return false;

            float remaining = 1f - latest.cureProgress;
            float etaTicks = remaining / (dCure / w);
            if (etaTicks > cureUrgentEtaTicks) return false; // D3(안전권) — Phase 1.5, 지금은 스킵

            var severity = SeverityFromRatio(etaTicks, cureUrgentEtaTicks, higherIsWorse: false);
            report = new BottleneckReport(BottleneckType.CureRace_Urgent, severity, actionable: true,
                relevantStat: "drugResistance",
                evidence: $"치료제 완성까지 약 {etaTicks:F0}틱 남음(진행률 {latest.cureProgress:P0})");
            return true;
        }

        private bool TryEvaluateResourceStarved(out BottleneckReport report)
        {
            report = BottleneckReport.None;
            var upgrades = Upgrades;
            var data = Data;
            if (upgrades == null || data == null) return false;

            bool anyPrereqEligible = false;
            bool anyAffordable = false;
            foreach (var node in upgrades.Tree)
            {
                if (node.isUnlocked) continue;

                bool prereqMet = true;
                foreach (var pid in node.prerequisites)
                {
                    if (!upgrades.IsUnlocked(pid)) { prereqMet = false; break; }
                }
                if (!prereqMet) continue;

                anyPrereqEligible = true;
                if (data.State.dnaPoints >= upgrades.GetEffectiveCost(node))
                {
                    anyAffordable = true;
                    break;
                }
            }

            // 선행조건을 만족한 노드가 아예 없는 경우(트리 소진)는 "자원 고갈"이 아니라 다른 상황이라
            // 이번 판정에서 제외 — DNA를 더 모아도 살 게 없는 상태와 혼동하지 않기 위함.
            if (!anyPrereqEligible || anyAffordable) return false;

            report = new BottleneckReport(BottleneckType.ResourceStarved, BottleneckSeverity.High, actionable: false,
                relevantStat: null,
                evidence: $"현재 DNA {data.State.dnaPoints} — 구매 조건을 만족한 노드가 있으나 전부 감당 못 함");
            return true;
        }

        /// <summary>value/threshold 비를 4단계로 매핑. higherIsWorse=false면 비가 작을수록(임계값보다
        /// 훨씬 못 미칠수록) 심각, true면 비가 클수록(임계값을 훨씬 넘을수록) 심각.</summary>
        private static BottleneckSeverity SeverityFromRatio(float value, float threshold, bool higherIsWorse)
        {
            if (threshold <= 0f) return BottleneckSeverity.Medium;
            float r = value / threshold;

            if (higherIsWorse)
            {
                if (r >= 3f) return BottleneckSeverity.Critical;
                if (r >= 2f) return BottleneckSeverity.High;
                if (r >= 1.3f) return BottleneckSeverity.Medium;
                return BottleneckSeverity.Low;
            }

            if (r <= 0.3f) return BottleneckSeverity.Critical;
            if (r <= 0.5f) return BottleneckSeverity.High;
            if (r <= 0.75f) return BottleneckSeverity.Medium;
            return BottleneckSeverity.Low;
        }
    }
}
