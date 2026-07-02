using System;

namespace Contagion.Ranking
{
    /// <summary>
    /// 랭킹 백엔드 추상화. 위키의 "멀티스토어 통합 패턴" 권장 아키텍처를 따른다 —
    /// 호출부(RankingManager/UI)가 앱인토스인지 아닌지 몰라도 되게 감싼다.
    /// </summary>
    public interface IRankingService
    {
        /// <summary>점수 제출. 내부에서 로컬 기록 저장 + (지원 시) 외부 리더보드 제출을 병행한다.</summary>
        void SubmitRun(long score, Action<bool> onCompleted = null);

        /// <summary>로컬 개인 최고 기록 (플랫폼 무관 — 글로벌 Top N 조회 API가 없어 이것만 게임 내에 표시 가능).</summary>
        long GetPersonalBest();

        /// <summary>true면 UI가 OpenExternalLeaderboard로 위임 (앱인토스 WebView).</summary>
        bool HasExternalLeaderboard { get; }

        void OpenExternalLeaderboard(Action<bool> onCompleted = null);
    }
}
