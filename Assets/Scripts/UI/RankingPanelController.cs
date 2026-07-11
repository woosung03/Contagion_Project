using System;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 랭킹 패널. 설계 문서 Step 12 — 단, 앱인토스에 글로벌 Top N 조회 API가 없어(RankingManager 주석 참고)
    /// "전체/병원체별/주간 랭킹 리스트" 대신 개인 최고 기록 표시 + 토스 리더보드 WebView 열기로 축소했다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class RankingPanelController : MonoBehaviour
    {
        private VisualElement _root;
        private Label _pbLabel;
        private Label _noteLabel;
        private Button _openButton;
        private Button _closeButton;

        /// <summary>닫기(X) 버튼 클릭 — 이 컨트롤러 스스로 Hide()를 호출하지 않고 요청만 발행한다.
        /// 실제로 화면을 닫고 WorldMapInputLock을 해제하는 책임은 UIManager 한 곳에만 있다
        /// (UpgradeTreeView.OnCloseRequested와 동일 패턴 — WorldMap Input Lock 영구 유지 버그 수정).</summary>
        public event Action OnCloseRequested;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _root = root.Q<VisualElement>("ranking-root");
            _pbLabel = root.Q<Label>("pb-label");
            _noteLabel = root.Q<Label>("note-label");
            _openButton = root.Q<Button>("open-leaderboard-button");
            _closeButton = root.Q<Button>("close-button");

            _openButton.RegisterCallback<ClickEvent>(_ => RankingManager.Instance?.OpenExternalLeaderboard());
            _closeButton.RegisterCallback<ClickEvent>(_ => OnCloseRequested?.Invoke());

            Hide();
        }

        public void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;

            long pb = RankingManager.Instance?.GetPersonalBest() ?? 0;
            _pbLabel.text = $"개인 최고 기록: {pb:N0}";

            bool hasExternal = RankingManager.Instance?.HasExternalLeaderboard ?? false;
            _openButton.SetEnabled(hasExternal);
            _noteLabel.text = hasExternal
                ? ""
                : "에디터/비 앱인토스 환경에서는 토스 리더보드를 열 수 없습니다 (로컬 기록만 표시).";
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }
    }
}
