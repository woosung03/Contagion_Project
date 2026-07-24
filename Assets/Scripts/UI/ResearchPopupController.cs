using System;
using System.Linq;
using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 연구 항목 상세 팝업 컨트롤러 — Commit 4(Round 4 IA 반영): 노드명 → DNA 비용 → 효과 →
    /// 주의 → 연구하기 버튼 → 고급 정보(Foldout) 순서로 재구성했다.
    ///
    /// <see cref="TacticalModalController"/>(모달 공용 프레임: modal-root/modal-title/modal-close/
    /// modal-rows/modal-footer 계약)를 상속한다. base.OnEnable()이 modal-root/modal-title/
    /// modal-close/modal-rows/modal-footer를 바인딩하고 modal-close 클릭 -> Hide()를 등록한 뒤
    /// 초기 Hide()까지 호출해준다. 이 클래스는 ResearchPopup.uxml 고유 요소(설명/비용/효과/주의/
    /// 고급 정보) 바인딩과 Show() 오버로드, 구매 흐름을 담당한다.
    ///
    /// Show()가 받는 nodeId로 <see cref="UpgradeManager.GetNode"/>를 직접 조회해 비용/효과/주의/
    /// 버튼 상태/고급 정보 막대를 전부 계산한다 — Show() 시그니처(title/branch/description/nodeId)는
    /// 호출부(UpgradeTreeView/UIManager) 무변경 원칙에 따라 그대로 유지했다. branch 인자는 더 이상
    /// 화면에 쓰이지 않지만(구 "브랜치" data-row 제거) 시그니처 유지를 위해 파라미터는 남겨둔다.
    /// </summary>
    public class ResearchPopupController : TacticalModalController
    {
        /// <summary>[Modal Exclusivity, 2026-07-24] ImportantEventPopupController가 "지금 ResearchPopup이
        /// 열려 있는가"를 확인하고 닫힘 시점을 구독하기 위한 최소 노출 — 이 GameObject는 항상
        /// SetActive(true) 상태를 유지하고 Show()/Hide()만 display를 토글하므로(TacticalModalController
        /// 참고) OnEnable에서 인스턴스를 등록하면 게임 전체에서 안전하게 1회만 설정된다.</summary>
        public static ResearchPopupController Instance { get; private set; }

        /// <summary>지금 이 팝업이 화면에 표시 중인지. Show()/Hide()에서만 갱신.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Hide() 완료 시점에 발행 — 다른 모달(ImportantEventPopup 등)이 이 팝업이 닫히길
        /// 기다렸다가 자기 표시를 이어가는 데 쓴다.</summary>
        public event Action OnClosed;

        /// <summary>[UI Review Pass 1, 2026-07-22] 팝업 뒤 화면을 어둡게 눌러주는 Scrim.
        /// TacticalModalController가 modal-root만 토글하므로(공용 베이스 클래스는 수정하지 않음),
        /// 이 화면 전용으로 Show()/Hide() 오버라이드에서 함께 토글한다.</summary>
        private VisualElement _modalScrim;
        private ScrollView _popupScroll;

        private Label _popupDescription;
        private Label _popupCost;
        private VisualElement _effectSection;
        private VisualElement _effectList;
        private VisualElement _warnSection;
        private VisualElement _warnList;
        private Button _confirmButton;
        private Button _cancelButton;
        private Foldout _advancedFoldout;
        private VisualElement _advancedBars;

        /// <summary>Show()가 대입한, 지금 팝업이 보여주고 있는 연구 노드 id — 확인 버튼 클릭 시
        /// TryUnlock()에 넘길 대상을 기억해둔다.</summary>
        private string _currentNodeId;

        protected override void OnEnable()
        {
            // modal-root/modal-title/modal-close/modal-rows/modal-footer 바인딩,
            // modal-close 클릭 -> Hide() 등록, 초기 Hide() 호출까지 여기서 모두 끝난다.
            base.OnEnable();
            Instance = this;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _modalScrim = root.Q<VisualElement>("modal-scrim");
            _popupScroll = root.Q<ScrollView>("popup-scroll");
            _popupDescription = root.Q<Label>("popup-description");
            _popupCost = root.Q<Label>("popup-cost");
            _effectSection = root.Q<VisualElement>("popup-effect-section");
            _effectList = root.Q<VisualElement>("popup-effect-list");
            _warnSection = root.Q<VisualElement>("popup-warn-section");
            _warnList = root.Q<VisualElement>("popup-warn-list");
            _confirmButton = root.Q<Button>("research-popup-confirm-button");
            _cancelButton = root.Q<Button>("research-popup-cancel-button");
            _advancedFoldout = root.Q<Foldout>("popup-advanced");
            _advancedBars = root.Q<VisualElement>("popup-advanced-bars");

            _confirmButton?.RegisterCallback<ClickEvent>(_ => HandleConfirmClicked());
            _cancelButton?.RegisterCallback<ClickEvent>(_ => Hide());
        }

        private void HandleConfirmClicked()
        {
            bool success = UpgradeManager.Instance != null && UpgradeManager.Instance.TryUnlock(_currentNodeId);
            if (success) Hide();
        }

        /// <summary>
        /// 연구 항목 상세를 채우고 팝업을 표시한다. 시그니처는 호출부(UpgradeTreeView/UIManager)
        /// 무변경을 위해 그대로 유지 — branch는 더 이상 화면에 쓰이지 않는다(구 "브랜치" data-row
        /// 제거, Commit 4).
        /// </summary>
        /// <param name="title">modal-title(Label)에 표시할 연구 항목명.</param>
        /// <param name="branch">Commit 4부터 미사용(하위 호환을 위해 시그니처만 유지).</param>
        /// <param name="description">popup-description(Label)에 표시할 설명 문단.</param>
        /// <param name="nodeId">비용/효과/주의/버튼 상태 계산 및 TryUnlock() 대상 노드 id.</param>
        public void Show(string title, string branch, string description, string nodeId)
        {
            // [WorldMap Input Lock System] ResearchPopup은 요구사항상 잠금 사유 목록에 명시적으로
            // 포함된 화면이다(CountryPopup과 달리) — 지금은 Research 화면(AppScreen.Research) 위에서만
            // 열리므로 UIManager의 Research 잠금과 사실상 중복이지만, 이 팝업이 나중에 다른 화면 위에서도
            // 열리게 되더라도 스스로 잠그고 풀도록 방어적으로 별도 사유를 둔다.
            WorldMapInputLock.Lock(WorldMapLockReason.ResearchPopup);
            base.Show(title);
            IsOpen = true;
            if (_modalScrim != null) _modalScrim.style.display = DisplayStyle.Flex;
            if (_popupScroll != null) _popupScroll.scrollOffset = Vector2.zero; // [P0 UI Fix, 2026-07-23] 이전 노드에서 스크롤한 위치가 남아있지 않도록 항상 최상단에서 시작

            _currentNodeId = nodeId;
            if (_popupDescription != null) _popupDescription.text = description;
            if (_advancedFoldout != null) _advancedFoldout.value = false; // 항상 접힘 상태로 시작

            RefreshDetails();
        }

        public override void Hide()
        {
            WorldMapInputLock.Unlock(WorldMapLockReason.ResearchPopup);
            if (_modalScrim != null) _modalScrim.style.display = DisplayStyle.None;
            base.Hide();
            IsOpen = false;
            OnClosed?.Invoke();
        }

        /// <summary>비용/효과/주의/버튼 상태/고급 정보를 전부 다시 계산해서 채운다.</summary>
        private void RefreshDetails()
        {
            var upgrades = UpgradeManager.Instance;
            var node = upgrades?.GetNode(_currentNodeId);
            if (node == null) return;

            bool owned = node.isUnlocked;
            int cost = upgrades.GetEffectiveCost(node);
            int dna = WorldDataManager.Instance?.State.dnaPoints ?? 0;

            if (_popupCost != null)
                _popupCost.text = owned ? "연구 완료" : $"비용 {cost} DNA";

            BuildEffectList(node);
            BuildWarnList(node);
            RefreshButton(node, owned, cost, dna);
            BuildAdvancedBars(node);
        }

        /// <summary>효과 — node.effects 중 값이 0이 아닌 항목만 "스탯명 ±N%p" 불릿으로 나열한다.
        /// 부호와 무관하게(음수 효과 포함) 실제 값을 그대로 보여준다 — 이 섹션은 순수 수치 사실만
        /// 전달하고, 위험 해석("발각 위험 증가" 등)은 아래 주의 섹션이 별도로 담당한다.</summary>
        private void BuildEffectList(UpgradeNode node)
        {
            if (_effectList == null) return;
            _effectList.Clear();

            bool any = false;
            foreach (var fx in node.effects)
            {
                if (Mathf.Approximately(fx.amount, 0f)) continue;
                any = true;

                var item = new Label($"• {StatLabel(fx.statName)} {FormatDelta(fx.amount)}");
                item.AddToClassList("popup-bullet");
                _effectList.Add(item);
            }

            if (_effectSection != null)
                _effectSection.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>주의 — severity 증가는 "발각 위험 증가", lethality 증가는 "치사율 상승"으로
        /// 텍스트 태그만 붙인다(수치 반복 없음, 색상도 severity 축을 재사용하지 않고 브랜드 골드로
        /// 통일 — DESIGN.md 2.2절 Don't: severity 4색을 노드 상태 UI에 재사용하지 않는다). 두 조건
        /// 다 아니면(즉 severity/lethality가 증가하지 않으면) 섹션 자체를 숨긴다. 두 스탯 다 음수
        /// 방향(감소)인 경우는 오히려 이득이므로 경고 대상이 아니다 — "증가"만 감지한다.</summary>
        private void BuildWarnList(UpgradeNode node)
        {
            if (_warnList == null) return;
            _warnList.Clear();

            bool any = false;
            if (node.GetEffect("severity") > 0f)
            {
                any = true;
                AddWarnBullet("발각 위험 증가");
            }
            if (node.GetEffect("lethality") > 0f)
            {
                any = true;
                AddWarnBullet("치사율 상승");
            }

            if (_warnSection != null)
                _warnSection.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddWarnBullet(string text)
        {
            var item = new Label($"! {text}");
            item.AddToClassList("popup-bullet");
            item.AddToClassList("popup-bullet--warn");
            _warnList.Add(item);
        }

        /// <summary>버튼 상태 4갈래 — 이미 해금(연구 완료, 비활성) > 선행조건 미충족(구체적으로
        /// 부족한 선행 연구 이름 나열, 비활성) > DNA 부족(부족한 양 명시, 비활성) > 구매 가능
        /// (N DNA · 연구하기, 활성). 선행조건 판정은 UpgradeManager.GetMissingPrerequisites()가
        /// CanUnlock()과 동일한 IsUnlocked 기준으로 계산해준다 — 이 메서드가 그 결과를
        /// UpgradeTreeView.GetDisplayName()으로 한국어 이름으로 바꿔 문장만 조립한다
        /// (LockReason System: 사유 계산은 UpgradeManager, 문장 생성은 UI 계층).</summary>
        private void RefreshButton(UpgradeNode node, bool owned, int cost, int dna)
        {
            if (_confirmButton == null) return;

            if (owned)
            {
                _confirmButton.text = "연구 완료";
                _confirmButton.SetEnabled(false);
                return;
            }

            var missingPrereqs = UpgradeManager.Instance.GetMissingPrerequisites(_currentNodeId);
            if (missingPrereqs.Count > 0)
            {
                string names = string.Join(", ", missingPrereqs.Select(UpgradeTreeView.GetDisplayName));
                _confirmButton.text = $"다음 필요 연구: {names}";
                _confirmButton.SetEnabled(false);
                return;
            }

            if (dna < cost)
            {
                _confirmButton.text = $"DNA {cost - dna} 부족";
                _confirmButton.SetEnabled(false);
                return;
            }

            _confirmButton.text = $"{cost} DNA · 연구하기";
            _confirmButton.SetEnabled(true);
        }

        /// <summary>고급 정보(Foldout, 기본 접힘) — 감염성/심각성/치사율/약물저항 4종을 이 노드가
        /// 실제로 건드리는지와 무관하게 항상 전부 보여준다(현재 값 vs 이 노드 적용 시 값). "현재
        /// 값"은 데모 기준선이 아니라 WorldDataManager.CurrentPathogen의 실제 진행 중인 병원체
        /// 스탯을 그대로 읽는다.</summary>
        private void BuildAdvancedBars(UpgradeNode node)
        {
            if (_advancedBars == null) return;
            _advancedBars.Clear();

            var pathogen = WorldDataManager.Instance?.CurrentPathogen;
            if (pathogen == null) return;

            AddBarRow("감염성", pathogen.infectivity, node.GetEffect("infectivity"));
            AddBarRow("심각성", pathogen.severity, node.GetEffect("severity"));
            AddBarRow("치사율", pathogen.lethality, node.GetEffect("lethality"));
            AddBarRow("약물저항", pathogen.drugResistance, node.GetEffect("drugResistance"));
        }

        private void AddBarRow(string label, float before, float delta)
        {
            float after = Mathf.Clamp01(before + delta);

            var row = new VisualElement();
            row.AddToClassList("popup-bar-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("popup-bar-row__label");
            row.Add(labelEl);

            row.Add(CreateBar(before, "popup-bar--before"));
            row.Add(CreateBar(after, "popup-bar--after"));

            _advancedBars.Add(row);
        }

        private static VisualElement CreateBar(float value01, string fillClass)
        {
            var track = new VisualElement();
            track.AddToClassList("popup-bar-track");

            var fill = new VisualElement();
            fill.AddToClassList("popup-bar-fill");
            fill.AddToClassList(fillClass);
            fill.style.width = Length.Percent(Mathf.Clamp01(value01) * 100f);
            track.Add(fill);

            return track;
        }

        private static string StatLabel(string statName) => statName switch
        {
            "infectivity" => "감염성",
            "severity" => "심각성",
            "lethality" => "치사율",
            "drugResistance" => "약물저항",
            _ => statName
        };

        private static string FormatDelta(float amount)
        {
            int pct = Mathf.RoundToInt(amount * 100f);
            return (pct >= 0 ? "+" : "") + pct + "%p";
        }
    }
}
