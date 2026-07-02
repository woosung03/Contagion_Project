namespace Contagion.Ranking
{
    /// <summary>빌드 타겟에 따라 올바른 IRankingService 구현을 생성한다.</summary>
    public static class RankingServiceFactory
    {
        public static IRankingService Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new TossRankingService();
#else
            return new LocalRankingService();
#endif
        }
    }
}
