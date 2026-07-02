using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using AppsInToss;
#endif

namespace Contagion.Ranking
{
    /// <summary>
    /// 앱인토스 게임센터 리더보드 정적 래퍼. 위키 wiki/apps-in-toss-unity/snippet-toss-leaderboard.md 그대로 이식.
    ///   - SubmitScore: 점수 제출. 게임 종료/클리어 시점에만 호출 (진입 직후 금지 — 프로필 생성 전 오류)
    ///   - OpenLeaderboard: 토스 리더보드 WebView 열기
    /// 비 AIT 환경(에디터·네이티브 빌드)에서는 로그 후 onCompleted(false) 즉시 폴백.
    /// 전제: 게임 카테고리 미니앱(아니면 40000), 토스 앱 5.221.0+(미만이면 null 응답 = 정상 무시).
    ///
    /// 설계 문서(14절)는 랭킹 데이터를 Firebase/Supabase에 별도 저장하는 것으로 가정했지만,
    /// 앱인토스가 게임 카테고리 미니앱에 게임센터 리더보드를 기본 제공하므로 별도 백엔드 없이
    /// 이 드롭인만으로 점수 제출/리더보드 열기가 가능하다 (자세한 사유는 RankingManager 주석 참고).
    /// </summary>
    public static class TossLeaderboard
    {
        /// <summary>AIT 경로가 활성인 빌드인지. 랭킹 버튼을 WebView로 위임할지 UI 분기에 사용.</summary>
        public static bool IsAvailable
#if UNITY_WEBGL && !UNITY_EDITOR
            => true;
#else
            => false;
#endif

        /// <summary>점수 제출. onCompleted: true=제출 완료(구버전 null 응답 포함), false=예외 실패.</summary>
        public static void SubmitScore(long score, Action<bool> onCompleted = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SubmitInternalAsync(score, onCompleted);
#else
            Debug.Log($"[TossLeaderboard] (비 AIT 환경) SubmitScore({score}) — 폴백.");
            onCompleted?.Invoke(false);
#endif
        }

        /// <summary>토스 게임센터 리더보드 WebView 열기.</summary>
        public static void OpenLeaderboard(Action<bool> onCompleted = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            OpenInternalAsync(onCompleted);
#else
            Debug.Log("[TossLeaderboard] (비 AIT 환경) OpenLeaderboard — 폴백.");
            onCompleted?.Invoke(false);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static async void SubmitInternalAsync(long score, Action<bool> onCompleted)
        {
            try
            {
                var response = await AIT.SubmitGameCenterLeaderBoardScore(
                    new SubmitGameCenterLeaderBoardScoreParams { Score = score.ToString() }); // Score는 string
                Debug.Log($"[TossLeaderboard] 점수 제출 완료: {score}, statusCode={response?.StatusCode ?? "(구버전 미지원)"}");
                onCompleted?.Invoke(true);
            }
            catch (Exception e)
            {
                // 40000 = 게임 카테고리 미등록. 그 외 네트워크/플랫폼 오류.
                Debug.LogWarning($"[TossLeaderboard] 점수 제출 실패: {e.Message}");
                onCompleted?.Invoke(false);
            }
        }

        static async void OpenInternalAsync(Action<bool> onCompleted)
        {
            try
            {
                await AIT.OpenGameCenterLeaderboard();
                Debug.Log("[TossLeaderboard] 리더보드 WebView 열기 완료.");
                onCompleted?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TossLeaderboard] 리더보드 열기 실패: {e.Message}");
                onCompleted?.Invoke(false);
            }
        }
#endif
    }
}
