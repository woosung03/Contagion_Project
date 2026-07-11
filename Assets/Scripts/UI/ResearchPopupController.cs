using Contagion.Gameplay;
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
    /// [이번 커밋 범위] Popup 표시 / 숨김 / 텍스트 갱신 / Close 버튼 처리만 구현한다.
    /// modal-footer의 research-popup-confirm-button("연구 시작")/research-popup-cancel-button
    /// ("취소")은 ResearchPopup.uxml에 이미 존재하지만 이 커밋에서는 클릭 콜백을 연결하지 않는다 —
    /// 노드 선택 이벤트 연결, 연구 구매(UpgradeManager.TryUnlock) 로직, Popup 자동 호출
    /// (UpgradeTreeView/UIManager 연동), 씬 배선(GameObject에 UIDocument 연결)은 모두 이후
    /// 커밋/에디터 작업의 범위이며 이 커밋에서는 손대지 않는다.
    /// </summary>
    public class ResearchPopupController : TacticalModalController
    {
        private Label _popupDescription;

        protected override void OnEnable()
        {
            // modal-root/modal-title/modal-close/modal-rows/modal-footer 바인딩,
            // modal-close 클릭 -> Hide() 등록, 초기 Hide() 호출까지 여기서 모두 끝난다.
            base.OnEnable();

            var root = GetComponent<UIDocument>().rootVisualElement;
            _popupDescription = root.Q<Label>("popup-description");
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
        public void Show(string title, string branch, string description)
        {
            // [WorldMap Input Lock System] ResearchPopup은 요구사항상 잠금 사유 목록에 명시적으로
            // 포함된 화면이다(CountryPopup과 달리) — 지금은 Research 화면(AppScreen.Research) 위에서만
            // 열리므로 UIManager의 Research 잠금과 사실상 중복이지만, 이 팝업이 나중에 다른 화면 위에서도
            // 열리게 되더라도 스스로 잠그고 풀도록 방어적으로 별도 사유를 둔다.
            WorldMapInputLock.Lock(WorldMapLockReason.ResearchPopup);
            base.Show(title);

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
