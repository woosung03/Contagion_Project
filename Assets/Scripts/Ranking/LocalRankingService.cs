using System;
using UnityEngine;

namespace Contagion.Ranking
{
    /// <summary>비 앱인토스 환경(에디터/네이티브)용 로컬 전용 랭킹 서비스. PlayerPrefs 기반 개인 최고 기록만 관리.</summary>
    public class LocalRankingService : IRankingService
    {
        private const string PersonalBestKey = "contagion_personal_best";

        public bool HasExternalLeaderboard => false;

        public long GetPersonalBest() => (long)PlayerPrefs.GetFloat(PersonalBestKey, 0f);

        public void SubmitRun(long score, Action<bool> onCompleted = null)
        {
            if (score > GetPersonalBest())
            {
                PlayerPrefs.SetFloat(PersonalBestKey, score);
                PlayerPrefs.Save();
            }
            onCompleted?.Invoke(true);
        }

        public void OpenExternalLeaderboard(Action<bool> onCompleted = null) => onCompleted?.Invoke(false);
    }
}
