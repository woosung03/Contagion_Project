using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// Tactical Modal 공용 프레임 컨트롤러. Docs/UI_Design.md 12절/13절(Tactical Modal System 설계)의
    /// C# 측 결론 — "CountryPopupController를 억지로 범용화하는 대신, 신규 TacticalModalController를
    /// 만들고 CountryPopupController는 그 위의 얇은 래퍼로 남긴다"를 구현한 것.
    ///
    /// UXML이 modal-root(+tactical-panel)/modal-title/modal-close/modal-rows(detail-rows)/
    /// modal-footer 계약(§13 템플릿)을 따르는 화면이면 이 클래스를 그대로 붙이거나 상속해서
    /// 재사용한다. 이 프로젝트는 UXML 템플릿(Template instancing)을 채택하지 않으므로(§15
    /// Historical Notes) 공유되는 것은 이 C# 클래스와 Tactical.uss의 tactical-panel/corner-cut/
    /// data-row 뿐이다 — 각 소비자는 이 계약을 따르는 자기 UXML을 따로 두고 UIDocument로 연결한다.
    /// 지금은 CountryPopupController(국가 상세)가 유일한 소비자이고, 이벤트 상세/설정/확인창 등은
    /// §13이 언급한 향후 후보다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TacticalModalController : MonoBehaviour
    {
        private VisualElement _modalRoot;
        private Label _modalTitle;
        private Button _modalClose;
        private VisualElement _modalRows;
        private VisualElement _modalFooter;

        /// <summary>확인/취소 등 액션 버튼을 붙일 수 있게 footer 컨테이너를 노출한다(비어있으면 안 보임).</summary>
        public VisualElement Footer => _modalFooter;

        protected virtual void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _modalRoot = root.Q<VisualElement>("modal-root");
            _modalTitle = root.Q<Label>("modal-title");
            _modalClose = root.Q<Button>("modal-close");
            _modalRows = root.Q<VisualElement>("modal-rows");
            _modalFooter = root.Q<VisualElement>("modal-footer");

            _modalClose?.RegisterCallback<ClickEvent>(_ => Hide());

            Hide();
        }

        public virtual void Show(string title)
        {
            if (_modalTitle != null) _modalTitle.text = title;
            if (_modalRoot != null) _modalRoot.style.display = DisplayStyle.Flex;
        }

        public virtual void Hide()
        {
            if (_modalRoot != null) _modalRoot.style.display = DisplayStyle.None;
        }

        /// <summary>modal-rows(detail-rows) 컨테이너를 비운다 — 재호출 시 이전 내용 제거용.</summary>
        public void ClearRows() => _modalRows?.Clear();

        /// <summary>data-row 한 줄(라벨+값) 추가 — Tactical.uss data-row/data-label/data-value 계약
        /// (UpgradeTree/CountrySelect/EndingScreen과 동일한 헬퍼 패턴).</summary>
        public void AddRow(string label, string value, string valueClass = null)
        {
            if (_modalRows == null) return;

            var row = new VisualElement();
            row.AddToClassList("data-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("data-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("data-value");
            if (!string.IsNullOrEmpty(valueClass)) valueEl.AddToClassList(valueClass);
            row.Add(valueEl);

            _modalRows.Add(row);
        }
    }
}
