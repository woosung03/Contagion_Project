using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 연구 항목 상세 팝업 컨트롤러 — Research Database v2 커밋 4.
    ///
    /// <see cref="TacticalModalController"/>(모달 공용 프레임: modal-root/modal-title/modal-close/
    /// modal-rows/modal-footer 계약)를 상속한다. base.OnEnable()이 modal-root/modal-title/
    /// modal-close/modal-rows/modal-footer를 바인딩하고 modal-close 클릭 -> Hide()를 등록한 뒤
    /// 초기 Hide()까지 호출해주므로("Popup은 기본적으로 숨김 상태" 요구사항과 "Close 버튼: 클릭 시
    /// Hide()" 요구사항을 그대로 충족), 이 클래스가 추가로 담당하는 것은 ResearchPopup.uxml 고유
    /// 요소(popup-description) 바인딩과 Show(title, branch, description) 오버로드뿐이다.
    ///
    /// [구매 플로우 복구] modal-footer의 research-popup-confirm-button("연구 시작")/
    /// research-popup-cancel-button("취소")이 이번에 UpgradeManager.TryUnlock()에 연결됐다.
    /// TryUnlock()의 bool 반환값으로 성공/실패를 분기한다 — 성공 시에만 Hide(), 실패 시 팝업을
    /// 유지한다(실패 사유를 알려주는 UI는 이번 범위 밖, CLAUDE.md TODO 커밋 8 LockReason()/CTA
    /// 문구 작업 참고). UpgradeManager 내부 로직/DNA 계산은 전혀 건드리지 않았다.
    /// </summary>
    public class ResearchPopupController : TacticalModalController
    {
        private Label _popupDescription;
        private Button _confirmButton;
        private Button _cancelButton;

        /// <summary>Show()가 대입한, 지금 팝업이 보여주고 있는 연구 노드 id — 확인 버튼 클릭 시
        /// TryUnlock()에 넘길 대상을 기억해둔다.</summary>
        private string _currentNodeId;

        protected override void OnEnable()
        {
            // modal-root/modal-title/modal-close/modal-rows/modal-footer 바인딩,
            // modal-close 클릭 -> Hide() 등록, 초기 Hide() 호출까지 여기서 모두 끝난다.
            base.OnEnable();

            var root = GetComponent<UIDocument>().rootVisualElement;
            _popupDescription = root.Q<Label>("popup-description");
            _confirmButton = root.Q<Button>("research-popup-confirm-button");
            _cancelButton = root.Q<Button>("research-popup-cancel-button");

            _confirmButton?.RegisterCallback<ClickEvent>(_ => HandleConfirmClicked());
            _cancelButton?.RegisterCallback<ClickEvent>(_ => Hide());
        }

        private void HandleConfirmClicked()
        {
            bool success = UpgradeManager.Instance != null && UpgradeManager.Instance.TryUnlock(_currentNodeId);
            if (success) Hide();
        }

        /// <summary>
        /// 연구 항목 상세를 채우고 팝업을 표시한다.
        /// </summary>
        /// <param name="title">modal-title(Label)에 표시할 연구 항목명.</param>
        /// <param name="branch">소속 갈래(예: 전파/증상/적응). ResearchPopup.uxml에는 브랜치 전용
        /// 요소가 없어, TacticalModalController.AddRow()로 modal-rows(detail-rows)에
        /// "브랜치" data-row를 추가하는 방식으로 표시한다(CountryPopupController 등 기존 화면과
        /// 동일한 data-row 재사용 패턴).</param>
        /// <param name="description">popup-description(Label)에 표시할 설명 문단.</param>
        /// <param name="nodeId">확인 버튼 클릭 시 UpgradeManager.TryUnlock()에 넘길 노드 id.</param>
        public void Show(string title, string branch, string description, string nodeId)
        {
            // [WorldMap Input Lock System] ResearchPopup은 요구사항상 잠금 사유 목록에 명시적으로
            // 포함된 화면이다(CountryPopup과 달리) — 지금은 Research 화면(AppScreen.Research) 위에서만
            // 열리므로 UIManager의 Research 잠금과 사실상 중복이지만, 이 팝업이 나중에 다른 화면 위에서도
            // 열리게 되더라도 스스로 잠그고 풀도록 방어적으로 별도 사유를 둔다.
            WorldMapInputLock.Lock(WorldMapLockReason.ResearchPopup);
            base.Show(title);

            _currentNodeId = nodeId;
            if (_popupDescription != null) _popupDescription.text = description;

            // 재호출 시 이전 브랜치 행이 중복 누적되지 않도록 비우고 다시 추가한다.
            ClearRows();
            AddRow("브랜치", branch);
        }

        public override void Hide()
        {
            WorldMapInputLock.Unlock(WorldMapLockReason.ResearchPopup);
            base.Hide();
        }
    }
}
