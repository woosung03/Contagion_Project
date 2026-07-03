using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_WEBGL && !UNITY_EDITOR
using AppsInToss;
#endif

namespace Contagion.UI
{
    /// <summary>
    /// UIDocument 루트(또는 지정 USS 클래스 자식)에 Safe Area + 앱인토스 내비게이션 바(X·더보기 버튼)
    /// 회피 여백을 padding으로 적용한다. 참조 전용 위키(C:\Game\codebase, apps-in-toss-safearea-guide.md)의
    /// 드롭인 컴포넌트를 이 프로젝트 네임스페이스로 옮겨온 것 — "모바일 최적화 UI 폴리싱" 요청으로 추가.
    ///
    /// 앱인토스 SDK는 아직 설치 전이라(CLAUDE.md "씬/에셋 배선 필요" 참고, 게임 마지막 단계로 연기됨)
    /// AIT.* 참조는 전부 #if UNITY_WEBGL && !UNITY_EDITOR 안에 있어 지금은 항상 폴백 경로
    /// (Screen.safeArea)로 동작한다 — Step 10~13의 AIT.* 연동과 동일한 패턴(에디터/비AIT 빌드에서
    /// 컴파일은 되지만 실제 AIT 값은 SDK 설치 후에만 온다). 나중에 SDK를 설치해도 코드 변경 없이
    /// 그대로 AIT 경로가 활성화된다.
    ///
    /// 상단 absolute 앵커 요소(예: 좌상단에 별도로 떠 있는 배지·버튼)가 이 프로젝트 UI엔 없어서
    /// (전부 flex 흐름 자식 — 확인 완료) safe-area-top 마커 클래스는 이번엔 안 써도 되지만,
    /// 나중에 그런 요소를 추가하면 위키 문서 §6-2 패턴대로 클래스만 붙이면 된다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SafeAreaApplier : MonoBehaviour
    {
        [Tooltip("inset을 적용할 root VisualElement의 USS 클래스. 비우면 rootVisualElement에 직접 적용.")]
        [SerializeField] private string targetClass = "";

        [Header("앱인토스 내비게이션 바 회피")]
        [Tooltip("safe-area.top 위에 추가로 비울 상단 여백(논리 px). X·더보기 버튼 높이를 덮을 만큼(≈40~56) 확보. " +
                 "안드로이드를 제외한 모든 환경에서 적용.")]
        [SerializeField] private float navBarReserveTop = 48f;

        [Header("디버그 오버레이 (실기기 검증용)")]
        [Tooltip("켜면 화면 좌하단에 적용 상태(src·인셋·padding·dpr·재시도)를 표시. 검증 후 끄거나 제거.")]
        [SerializeField] private bool showDebugOverlay = false;

#if UNITY_EDITOR
        [Header("에디터 미리보기 (Play Mode, 논리 px)")]
        [Tooltip("켜면 에디터 Game 뷰에서 가짜 인셋 + navBarReserveTop을 적용해 실기 레이아웃을 미리 확인. 빌드 영향 없음.")]
        [SerializeField] private bool previewInEditor = false;
        [SerializeField] private float previewSafeTop = 44f;
        [SerializeField] private float previewSafeBottom = 24f;
        [SerializeField] private float previewSafeLeft = 0f;
        [SerializeField] private float previewSafeRight = 0f;
#endif

        private UIDocument document;
        private VisualElement target;
        private Vector2Int lastResolution = new Vector2Int(-1, -1);
        private Rect lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private bool applied;
        private bool loggedFallback, loggedAit;

        private const string SAFE_TOP_CLASS = "safe-area-top"; // 상단 absolute 앵커 요소 마커 — padding에 안 밀려 translate로 인셋 적용

        private Label debugLabel;
        private string lastOverlaySrc = "(미적용)";
        private string overlayStatus = "-";

#if UNITY_WEBGL && !UNITY_EDITOR
        private float tossLeft, tossRight, tossTop, tossBottom;
        private float tossDpr = 1f;
        private bool hasTossInsets;
        private bool fetchingToss;
        private bool loggedTossWait;
        private const int TOSS_MAX_ATTEMPTS = 40;    // 1회 폴링당 최대 재시도 (≈ 12초)
        private const float TOSS_RETRY_DELAY = 0.3f; // 재시도 간격(초)
#endif

        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
            ResolveTarget();
            applied = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            _ = FetchTossInsetsAsync();
#endif
            TryApply();
        }

        private void OnDisable()
        {
            if (debugLabel != null) { debugLabel.RemoveFromHierarchy(); debugLabel = null; }
        }

#if UNITY_EDITOR
        private void OnValidate() => applied = false;
#endif

        private void Update()
        {
            // target이 없거나 패널에서 분리(panel==null)됐으면(UIDocument 비주얼 트리 재빌드 등) 재해결 + 재적용.
            if (target == null || target.panel == null) { ResolveTarget(); applied = false; }
            if (target == null) return;

            if (showDebugOverlay) RefreshDebugOverlay();
            else HideDebugOverlay();

#if UNITY_EDITOR
            if (previewInEditor) { ApplyPreview(); return; }
#endif
            var resolution = new Vector2Int(Screen.width, Screen.height);
            bool resolutionChanged = resolution != lastResolution;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Subscribe API가 이 SDK 버전에서 동작하지 않아(위키 문서 §4-1) 회전/해상도 변경 시 폴링 재조회.
            if (resolutionChanged && !fetchingToss)
            {
                applied = false;
                _ = FetchTossInsetsAsync();
            }
#endif
            if (!applied || resolutionChanged || Screen.safeArea != lastSafeArea)
                TryApply();
        }

        private void ResolveTarget()
        {
            if (document == null || document.rootVisualElement == null) return;
            target = string.IsNullOrEmpty(targetClass)
                ? document.rootVisualElement
                : document.rootVisualElement.Q(className: targetClass);
        }

#if UNITY_EDITOR
        private void ApplyPreview()
        {
            if (target == null) return;
            target.style.paddingTop = Mathf.Max(0f, previewSafeTop + navBarReserveTop);
            target.style.paddingBottom = Mathf.Max(0f, previewSafeBottom);
            target.style.paddingLeft = Mathf.Max(0f, previewSafeLeft);
            target.style.paddingRight = Mathf.Max(0f, previewSafeRight);
            ApplyTopAnchorTranslate(Mathf.Max(0f, previewSafeTop + navBarReserveTop));
            applied = true;
            lastOverlaySrc = "editor preview";
        }
#endif

        private void TryApply()
        {
            if (target == null) return;

            float reserveTop = 0f;
#if !UNITY_ANDROID
            reserveTop = navBarReserveTop;
#endif
            string src = "Screen.safeArea";
#if UNITY_WEBGL && !UNITY_EDITOR
            if (hasTossInsets)
            {
                if (ApplyDevicePx(tossLeft * tossDpr, tossRight * tossDpr, tossTop * tossDpr, tossBottom * tossDpr, reserveTop))
                {
                    MarkApplied();
                    LogAppliedOnce("AIT");
                    lastOverlaySrc = "AIT";
                }
                return;
            }
            src = "Screen.safeArea(+reserve, AIT 대기/폴백)";
#endif
            var area = Screen.safeArea;
            float left = area.xMin;
            float right = Screen.width - area.xMax;
            float top = Screen.height - area.yMax;
            float bottom = area.yMin;
            if (ApplyDevicePx(left, right, top, bottom, reserveTop))
            {
                MarkApplied();
                LogAppliedOnce(src);
                lastOverlaySrc = src;
            }
        }

        private void MarkApplied()
        {
            applied = true;
            lastResolution = new Vector2Int(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
        }

        private void LogAppliedOnce(string source)
        {
            bool isAit = source == "AIT";
            if (isAit ? loggedAit : loggedFallback) return;
            if (isAit) loggedAit = true; else loggedFallback = true;
            var root = document != null ? document.rootVisualElement : null;
            Debug.Log($"[Verify-SafeArea] applied src={source} targetClass='{targetClass}' " +
                      $"screen={Screen.width}x{Screen.height} logical={(root != null ? root.resolvedStyle.width : -1f)}x{(root != null ? root.resolvedStyle.height : -1f)} " +
                      $"reserveTop={navBarReserveTop} padTop={target.resolvedStyle.paddingTop} padBottom={target.resolvedStyle.paddingBottom}");
        }

        // ── 디버그 오버레이 ──────────────────────────────────────────────
        private void RefreshDebugOverlay()
        {
            var root = document != null ? document.rootVisualElement : null;
            if (root == null) return;
            if (debugLabel == null)
            {
                debugLabel = new Label { pickingMode = PickingMode.Ignore };
                var s = debugLabel.style;
                s.position = Position.Absolute;
                s.left = 8f; s.bottom = 8f;
                s.maxWidth = Length.Percent(92f);
                s.paddingLeft = 6f; s.paddingRight = 6f; s.paddingTop = 4f; s.paddingBottom = 4f;
                s.backgroundColor = new Color(0f, 0f, 0f, 0.78f);
                s.color = Color.white;
                s.fontSize = 12f;
                s.whiteSpace = WhiteSpace.Normal;
                s.borderTopLeftRadius = 4f; s.borderTopRightRadius = 4f;
                s.borderBottomLeftRadius = 4f; s.borderBottomRightRadius = 4f;
                root.Add(debugLabel);
            }
            debugLabel.style.display = DisplayStyle.Flex;
            debugLabel.BringToFront();
            debugLabel.text = BuildOverlayText(root);
        }

        private void HideDebugOverlay()
        {
            if (debugLabel != null) debugLabel.style.display = DisplayStyle.None;
        }

        private string BuildOverlayText(VisualElement root)
        {
            float lw = root.resolvedStyle.width;
            float lh = root.resolvedStyle.height;
            bool isRoot = document != null && target == document.rootVisualElement;
            bool attached = target.panel != null;
            float setPadT = target.style.paddingTop.value.value;
            string tossLine = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            tossLine = hasTossInsets
                ? $"toss(css) T={tossTop:F0} B={tossBottom:F0} L={tossLeft:F0} R={tossRight:F0} dpr={tossDpr:F2}\n"
                : "toss insets: 대기/미수신\n";
#endif
            return $"[SafeArea] src={lastOverlaySrc}\n" +
                   $"screen={Screen.width}x{Screen.height} logical={lw:F0}x{lh:F0}\n" +
                   $"tgt isRoot={isRoot} attached={attached} setPadT={setPadT:F1}\n" +
                   $"pad T={target.resolvedStyle.paddingTop:F1} B={target.resolvedStyle.paddingBottom:F1} " +
                   $"L={target.resolvedStyle.paddingLeft:F1} R={target.resolvedStyle.paddingRight:F1}\n" +
                   tossLine +
                   $"reserveTop={navBarReserveTop} status={overlayStatus}";
        }

        private static string Short(string s, int max = 64)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");

        /// <summary>device px 인셋을 패널 논리 px로 환산해 padding 적용 + 상단 앵커 요소 translate. NaN/Inf 가드 포함.</summary>
        private bool ApplyDevicePx(float leftPx, float rightPx, float topPx, float bottomPx, float extraTopLogical)
        {
            var root = document != null ? document.rootVisualElement : null;
            if (root == null) return false;

            float logicalW = root.resolvedStyle.width;
            float logicalH = root.resolvedStyle.height;
            int sw = Screen.width, sh = Screen.height;
            // pre-layout resolvedStyle은 NaN일 수 있고 "NaN <= 0"은 항상 false라 흔한 가드를 통과해버린다 — Finite로 명시 차단.
            if (!Finite(logicalW) || !Finite(logicalH) ||
                logicalW <= 0f || logicalH <= 0f || sw <= 0 || sh <= 0) return false;

            float scaleX = sw / logicalW;
            float scaleY = sh / logicalH;

            float padL = Mathf.Max(0f, leftPx / scaleX);
            float padR = Mathf.Max(0f, rightPx / scaleX);
            float padT = Mathf.Max(0f, topPx / scaleY + extraTopLogical);
            float padB = Mathf.Max(0f, bottomPx / scaleY);
            if (!Finite(padL) || !Finite(padR) || !Finite(padT) || !Finite(padB)) return false;

            target.style.paddingLeft = padL;
            target.style.paddingRight = padR;
            target.style.paddingTop = padT;
            target.style.paddingBottom = padB;
            ApplyTopAnchorTranslate(padT);
            return true;
        }

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        /// <summary>.safe-area-top 요소는 padding에 안 밀리므로(absolute) translate로 동일 top 인셋만큼 내린다.</summary>
        private void ApplyTopAnchorTranslate(float topLogical)
        {
            var root = document != null ? document.rootVisualElement : null;
            if (root == null) return;
            root.Query<VisualElement>(className: SAFE_TOP_CLASS)
                .ForEach(el => el.style.translate = new Translate(0f, topLogical));
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>AIT 인셋을 폴링으로 조회. 실패는 재시도(영구 비활성 금지). 유효 인셋 받으면 종료.</summary>
        private async Awaitable FetchTossInsetsAsync()
        {
            if (fetchingToss) return;
            fetchingToss = true;
            overlayStatus = "AIT fetch 시작";
            try
            {
                for (int attempt = 1; attempt <= TOSS_MAX_ATTEMPTS; attempt++)
                {
                    try
                    {
                        var insets = await AIT.SafeAreaInsetsGet();
                        if (this == null) return;
                        if (insets != null && string.IsNullOrEmpty(insets.error))
                        {
                            tossLeft = (float)insets.Left;
                            tossRight = (float)insets.Right;
                            tossTop = (float)insets.Top;
                            tossBottom = (float)insets.Bottom;
                            double dpr = AIT.GetDevicePixelRatio();
                            tossDpr = dpr > 0.0 ? (float)dpr : 1f;
                            hasTossInsets = true;
                            applied = false;
                            overlayStatus = $"AIT 수신 (attempt={attempt})";
                            Debug.Log($"[Verify-SafeArea] AIT insets(css) T={insets.Top} B={insets.Bottom} L={insets.Left} R={insets.Right} dpr={tossDpr} attempt={attempt} navBarReserveTop={navBarReserveTop}");
                            return;
                        }
                        overlayStatus = $"대기 attempt={attempt}: {Short(insets?.error)}";
                        LogTossWaitOnce(insets?.error, attempt);
                    }
                    catch (System.Exception ex)
                    {
                        if (this == null) return;
                        overlayStatus = $"대기 attempt={attempt}: {Short(ex.Message)}";
                        LogTossWaitOnce(ex.Message, attempt);
                    }
                    await Awaitable.WaitForSecondsAsync(TOSS_RETRY_DELAY);
                    if (this == null) return;
                }
                overlayStatus = $"{TOSS_MAX_ATTEMPTS}회 미도착 → 폴백 유지";
                Debug.Log($"[SafeAreaApplier] AIT Safe Area {TOSS_MAX_ATTEMPTS}회 시도 후 미도착 — Screen.safeArea + navBarReserveTop({navBarReserveTop}) 폴백 유지. 네이티브 인셋은 실제 토스 앱에서만 제공됨.");
            }
            finally { fetchingToss = false; }
        }

        private void LogTossWaitOnce(string reason, int attempt)
        {
            if (loggedTossWait) return;
            loggedTossWait = true;
            Debug.Log($"[SafeAreaApplier] AIT insets 대기 중 — 재시도(attempt={attempt}, 간격 {TOSS_RETRY_DELAY}s). 사유: {reason}");
        }
#endif
    }
}
