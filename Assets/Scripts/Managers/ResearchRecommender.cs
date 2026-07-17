using System;
using System.Collections.Generic;
using System.Linq;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// BottleneckAnalyzer의 판정 결과를 UpgradeNode 추천 순위로 변환한다. 시뮬레이션 상태를 직접
    /// 읽지 않고 BottleneckReport + UpgradeManager.Tree만 소비하는 순수 매칭기 — BottleneckAnalyzer에만
    /// 의존(단방향), UpgradeManager는 이 클래스의 존재를 모른다.
    ///
    /// 핵심 원칙: report.Actionable == false(정보성 병목, ResourceStarved 포함)면 추천 리스트를
    /// 항상 비운다 — 연구로 못 푸는 문제에 억지로 노드를 추천하면 "이해시키기"가 아니라 다시
    /// "거짓 정답 주기"가 되어버리기 때문(설계 §5 핵심 원칙).
    /// </summary>
    public class ResearchRecommender : MonoBehaviour
    {
        public static ResearchRecommender Instance { get; private set; }

        [SerializeField, Tooltip("추천 리스트 상위 몇 개까지 유지할지")]
        private int maxRecommendations = 5;

        public IReadOnlyList<RecommendationEntry> CurrentRecommendations { get; private set; } = Array.Empty<RecommendationEntry>();

        /// <summary>가장 최근에 반영한 병목 유형 — 추천이 비어 있을 때 "왜 비어 있는지"(어떤 병목
        /// 때문인지)를 향후 UI(Phase 2)가 설명할 수 있도록 남겨둔다.</summary>
        public BottleneckType LastReasonType { get; private set; } = BottleneckType.None;

        public event Action<IReadOnlyList<RecommendationEntry>> OnRecommendationsChanged;

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
            if (BottleneckAnalyzer.Instance == null) return;
            BottleneckAnalyzer.Instance.OnBottleneckChanged -= HandleBottleneckChanged;
            BottleneckAnalyzer.Instance.OnBottleneckChanged += HandleBottleneckChanged;
        }

        private void OnDisable()
        {
            if (BottleneckAnalyzer.Instance != null)
                BottleneckAnalyzer.Instance.OnBottleneckChanged -= HandleBottleneckChanged;
        }

        /// <summary>새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출.</summary>
        public void ResetForNewGame()
        {
            CurrentRecommendations = Array.Empty<RecommendationEntry>();
            LastReasonType = BottleneckType.None;
        }

        private void HandleBottleneckChanged(BottleneckReport report)
        {
            LastReasonType = report.Type;

            if (!report.Actionable || Upgrades == null)
            {
                SetRecommendations(Array.Empty<RecommendationEntry>());
                return;
            }

            // DetectionFast_HighSeverity(C1)만 유일하게 "음수 delta"가 좋은 효과다(은신 계열이
            // severity를 깎는 노드) — 그 경우에만 부호를 반전해서 점수화한다(설계 §6).
            bool invert = report.Type == BottleneckType.DetectionFast_HighSeverity;

            var scored = Upgrades.Tree
                .Where(n => Upgrades.CanUnlock(n.id))
                .Select(n => new RecommendationEntry(n, Score(n, report.RelevantStat, invert)))
                .Where(r => r.Score != 0f) // 이 병목과 무관한(effect가 0인) 노드는 추천 후보에서 제외
                .OrderByDescending(r => r.Score)
                .Take(maxRecommendations)
                .ToList();

            SetRecommendations(scored);
        }

        /// <summary>score(node) = 관련 stat delta / GetEffectiveCost(node) — 비용은 카테고리별
        /// 해금 개수에 따른 가산(GetEffectiveCost)을 이미 반영하므로, 몰아찍기 카테고리는 자동으로
        /// 점수가 낮아진다(설계 §6, 별도 페널티 로직 불필요).</summary>
        private float Score(UpgradeNode node, string statName, bool invert)
        {
            float delta = node.GetEffect(statName);
            if (invert) delta = -delta;

            int cost = Upgrades.GetEffectiveCost(node);
            if (cost <= 0) return 0f;
            return delta / cost;
        }

        private void SetRecommendations(IReadOnlyList<RecommendationEntry> list)
        {
            CurrentRecommendations = list;
            OnRecommendationsChanged?.Invoke(list);
        }
    }
}
