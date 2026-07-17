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

            var root = GetComponent<UIDocument>().rootVisualElement;
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

            _currentNodeId = nodeId;
            if (_popupDescription != null) _popupDescription.text = description;
            if (_advancedFoldout != null) _advancedFoldout.value = false; // 항상 접힘 상태로 시작

            RefreshDetails();
        }

        public override void Hide()
        {
            WorldMapInputLock.Unlock(WorldMapLockReason.ResearchPopup);
            base.Hide();
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

        /// <summary>버튼 상태 4갈래 — 이미 해금(연구 완료, 비활성) > 선행조건 미충족(선행 연구
        /// 필요, 비활성) > DNA 부족(비활성) > 구매 가능(N DNA · 연구하기, 활성). 선행조건 판정은
        /// UpgradeTreeView.DetermineState()와 동일 로직이지만, 이번 커밋 범위(호출부 무변경)상
        /// 그 메서드를 공개로 승격하지 않고 여기서 동일한 조건을 다시 계산한다.</summary>
        private void RefreshButton(UpgradeNode node, bool owned, int cost, int dna)
        {
            if (_confirmButton == null) return;

            if (owned)
            {
                _confirmButton.text = "연구 완료";
                _confirmButton.SetEnabled(false);
                return;
            }

            bool prereqsMet = node.prerequisites.All(pid => UpgradeManager.Instance.IsUnlocked(pid));
            if (!prereqsMet)
            {
                _confirmButton.text = "선행 연구 필요";
                _confirmButton.SetEnabled(false);
                return;
            }

            if (dna < cost)
            {
                _confirmButton.text = "DNA 부족";
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
