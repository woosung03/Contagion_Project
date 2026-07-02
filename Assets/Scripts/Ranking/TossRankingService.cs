using System;

namespace Contagion.Ranking
{
    /// <summary>
    /// 앱인토스 환경용 랭킹 서비스. 위키 권장 원칙 "로컬 기록은 항상 병행" 반영 —
    /// SubmitRun에서 로컬 저장을 먼저 하고, 토스 제출은 fire-and-forget으로 병행한다.
    /// </summary>
    public class TossRankingService : IRankingService
    {
        private readonly LocalRankingService _local = new LocalRankingService();

        public bool HasExternalLeaderboard => TossLeaderboard.IsAvailable;

        public long GetPersonalBest() => _local.GetPersonalBest();

        public void SubmitRun(long score, Action<bool> onCompleted = null)
        {
            _local.SubmitRun(score); // 로컬 먼저 — 네트워크 실패와 무관하게 PB 유지
            TossLeaderboard.SubmitScore(score, onCompleted);
        }

        public void OpenExternalLeaderboard(Action<bool> onCompleted = null) =>
            TossLeaderboard.OpenLeaderboard(onCompleted);
    }
}
