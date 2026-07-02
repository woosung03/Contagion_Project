using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using AppsInToss;
#endif

namespace Contagion.Ads
{
    /// <summary>
    /// 앱인토스 전면/보상형 광고 1개(adGroupId 1개)의 로드·표시 수명주기 컨트롤러.
    /// 위키 wiki/apps-in-toss-unity/snippet-toss-fullscreen-ad.md 드롭인을 그대로 이식.
    /// 사용 규칙:
    ///   - 같은 adGroupId당 앱 전역 인스턴스 1개만 생성 (동시 선로드 1개 제약)
    ///   - 생성 시 자동 선로드. Show 후 dismissed/failedToShow 시점에 자동 재로드 (load→show→load)
    ///   - Show(onSuccess, onFailed): 광고 패널이 닫힌 시점에 둘 중 정확히 1개 호출 보장
    ///   - 성공 기준: Rewarded = userEarnedReward 수신 / Interstitial = show·impression 후 dismissed
    /// 비 AIT 환경(에디터·네이티브)에서는 IsReady=false, Show 즉시 onFailed.
    /// </summary>
    public sealed class TossFullScreenAd
    {
        /// <summary>콘솔 광고 그룹 타입과 반드시 일치시킬 것.</summary>
        public enum AdKind { Interstitial, Rewarded }

        public string AdGroupId { get; }
        public AdKind Kind { get; }
        public bool IsReady { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        Action loadUnsubscribe;
        Action showUnsubscribe;
        bool showing;
#endif

        public TossFullScreenAd(string adGroupId, AdKind kind, bool loadImmediately = true)
        {
            AdGroupId = adGroupId;
            Kind = kind;
            if (loadImmediately) Load();
        }

        /// <summary>광고 선로드. 표시 후엔 자동 재로드되므로 보통 직접 호출할 일 없음.</summary>
        public void Load()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (string.IsNullOrEmpty(AdGroupId)) { Debug.LogError("[TossFullScreenAd] adGroupId 미설정 — 로드 스킵."); return; }
            IsReady = false;
            loadUnsubscribe?.Invoke();
            loadUnsubscribe = AIT.LoadFullScreenAd(
                AdGroupId,
                onEvent: e =>
                {
                    if (e != null && e.Type == "loaded")
                    {
                        IsReady = true;
                        Debug.Log($"[TossFullScreenAd] {AdGroupId} 로드 완료.");
                    }
                },
                onError: err =>
                {
                    IsReady = false;
                    Debug.LogWarning($"[TossFullScreenAd] {AdGroupId} 로드 실패: {err?.Message}");
                });
#endif
        }

        /// <summary>광고 표시. 콜백은 광고가 닫힌 시점에 정확히 1개만 호출.</summary>
        public void Show(Action onSuccess, Action onFailed)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (showing) { onFailed?.Invoke(); return; }
            if (!IsReady)
            {
                Debug.LogWarning($"[TossFullScreenAd] {AdGroupId} 미준비 — onFailed + 재로드.");
                onFailed?.Invoke();
                Load();
                return;
            }

            bool rewarded = false, shown = false, finished = false;
            showing = true;
            IsReady = false; // 1회 표시 후 재로드 필요

            void Finish(bool success)
            {
                if (finished) return;
                finished = true;
                showing = false;
                showUnsubscribe?.Invoke();
                showUnsubscribe = null;
                Load(); // 다음 광고 재고 확보 (공식 권장: dismissed 직후 재로드)
                if (success) onSuccess?.Invoke();
                else onFailed?.Invoke();
            }

            showUnsubscribe = AIT.ShowFullScreenAd(
                AdGroupId,
                onEvent: e =>
                {
                    switch (e?.Type)
                    {
                        case "show":
                        case "impression":
                            shown = true;
                            break;
                        case "userEarnedReward": // 보상형 한정. 지급 근거는 오직 이 이벤트
                            rewarded = true;
                            Debug.Log($"[TossFullScreenAd] {AdGroupId} 보상 획득 type={e.Data?.UnitType} amount={e.Data?.UnitAmount} (닫힘 대기).");
                            break;
                        case "dismissed":
                            Finish(Kind == AdKind.Rewarded ? rewarded : shown);
                            break;
                        case "failedToShow":
                            Debug.LogWarning($"[TossFullScreenAd] {AdGroupId} 표시 실패(failedToShow).");
                            Finish(false);
                            break;
                    }
                },
                onError: err =>
                {
                    Debug.LogWarning($"[TossFullScreenAd] {AdGroupId} 표시 실패: {err?.Message}");
                    Finish(false);
                });
#else
            Debug.Log($"[TossFullScreenAd] (비 AIT 환경) Show({AdGroupId}) — onFailed 폴백.");
            onFailed?.Invoke();
#endif
        }

        /// <summary>구독 해제. 앱 종료/소유자 파괴 시 호출 권장 (앱 전역 인스턴스면 생략 가능).</summary>
        public void Dispose()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            loadUnsubscribe?.Invoke(); loadUnsubscribe = null;
            showUnsubscribe?.Invoke(); showUnsubscribe = null;
            showing = false;
#endif
            IsReady = false;
        }
    }
}
