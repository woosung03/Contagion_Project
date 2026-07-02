using System;
using Contagion.Ranking;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 랭킹 진입점. 설계 문서 Step 11 / 14절.
    ///
    /// 설계 문서 14절은 "랭킹 데이터는 서버(Firebase Firestore 또는 Supabase) 저장"이라고 가정했지만,
    /// 앱인토스 SDK가 게임 카테고리 미니앱에 게임센터 리더보드(점수 제출 + 순위 WebView)를 기본 제공하므로
    /// 별도 백엔드를 구축하지 않고 이 기능을 그대로 사용하도록 변경했다. 이유:
    ///   1. 별도 서버/DB 없이 즉시 사용 가능 (Firebase/Supabase 프로젝트 생성·과금·운영 불필요)
    ///   2. userHashKey 기반 사용자 식별을 앱인토스가 이미 처리 (AIT.GetUserKeyForGame())
    ///   3. 단, "글로벌 Top N 조회" API는 없다 — 자체 UI에 순위 리스트를 그릴 수 없고,
    ///      순위 화면은 토스 WebView에 위임해야 한다 (Step 12 랭킹 UI가 이 제약을 반영해 축소됨).
    /// 만약 병원체별/주간 랭킹처럼 세분화된 자체 UI가 꼭 필요해지면, 그때는 정말로 별도
    /// 백엔드(Firebase/Supabase)가 필요하다 — 이 클래스의 IRankingService를 새 구현으로 교체하면 된다.
    /// </summary>
    public class RankingManager : MonoBehaviour
    {
        public static RankingManager Instance { get; private set; }

        private IRankingService _service;

        public bool HasExternalLeaderboard => _service?.HasExternalLeaderboard ?? false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _service = RankingServiceFactory.Create();
        }

        public long GetPersonalBest() => _service?.GetPersonalBest() ?? 0;

        public void SubmitRun(long score, Action<bool> onCompleted = null) => _service?.SubmitRun(score, onCompleted);

        public void OpenExternalLeaderboard(Action<bool> onCompleted = null) => _service?.OpenExternalLeaderboard(onCompleted);
    }
}
