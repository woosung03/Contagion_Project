namespace Contagion.Data
{
    /// <summary>ResearchRecommender가 산출하는 추천 항목 1개 — 노드와 그 노드의 효율 점수(§6:
    /// 관련 stat delta 합 / GetEffectiveCost)만 담는다. 새 데이터가 아니라 UpgradeNode를 그대로 참조.</summary>
    public class RecommendationEntry
    {
        public UpgradeNode Node { get; }
        public float Score { get; }

        public RecommendationEntry(UpgradeNode node, float score)
        {
            Node = node;
            Score = score;
        }
    }
}
