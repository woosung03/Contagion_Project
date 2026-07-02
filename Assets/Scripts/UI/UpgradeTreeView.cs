using System.Linq;
using System.Text;
using Contagion.Ads;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 업그레이드 트리 화면. 설계 문서 7.2절.
    /// 실제 노드 그래프(선/좌표) 대신 카테고리별 리스트로 단순화했다 —
    /// 노드 좌표 데이터가 설계 문서에 없어, 좌표 기반 그래프 렌더링은 후속 작업(Step 8 확장)으로 남겨둔다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeTreeView : MonoBehaviour
    {
        private VisualElement _upgradeRoot;
        private Label _dnaLabel;
        private ScrollView _nodeScroll;
        private Label _detailTitle;
        private Label _detailDesc;
        private Button _buyButton;
        private Button _closeButton;
        private Button _adBonusButton;

        private string _selectedNodeId;

        [SerializeField, Tooltip("광고 시청 보상 DNA — 설계 문서 13절 표 1행")]
        private int adBonusDna = 10;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _upgradeRoot = root.Q<VisualElement>("upgrade-root");
            _dnaLabel = root.Q<Label>("dna-label");
            _nodeScroll = root.Q<ScrollView>("node-scroll");
            _detailTitle = root.Q<Label>("detail-title");
            _detailDesc = root.Q<Label>("detail-desc");
            _buyButton = root.Q<Button>("buy-button");
            _closeButton = root.Q<Button>("close-button");
            _adBonusButton = root.Q<Button>("ad-bonus-button");

            _closeButton.RegisterCallback<ClickEvent>(_ => Hide());
            _buyButton.RegisterCallback<ClickEvent>(_ => HandleBuyClicked());
            _adBonusButton.RegisterCallback<ClickEvent>(_ => HandleAdBonusClicked());

            Subscribe();
            Hide();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (UpgradeManager.Instance == null) return;
            UpgradeManager.Instance.OnDnaChanged -= HandleDnaChanged;
            UpgradeManager.Instance.OnDnaChanged += HandleDnaChanged;
            UpgradeManager.Instance.OnNodeUnlocked -= HandleNodeUnlocked;
            UpgradeManager.Instance.OnNodeUnlocked += HandleNodeUnlocked;
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance == null) return;
            UpgradeManager.Instance.OnDnaChanged -= HandleDnaChanged;
            UpgradeManager.Instance.OnNodeUnlocked -= HandleNodeUnlocked;
        }

        /// <summary>HUD 탭 버튼에서 호출. focusCategory는 스크롤 위치 참고용(선택 사항).</summary>
        public void Show(UpgradeCategory? focusCategory = null)
        {
            _upgradeRoot.style.display = DisplayStyle.Flex;
            RebuildNodeList();
            RefreshDna();
        }

        public void Hide()
        {
            if (_upgradeRoot != null) _upgradeRoot.style.display = DisplayStyle.None;
        }

        private void RefreshDna()
        {
            int dna = WorldDataManager.Instance?.State.dnaPoints ?? 0;
            _dnaLabel.text = $"DNA: {dna:N0}";
        }

        private void RebuildNodeList()
        {
            _nodeScroll.contentContainer.Clear();
            if (UpgradeManager.Instance == null) return;

            foreach (var category in new[] { UpgradeCategory.Transmission, UpgradeCategory.Symptom, UpgradeCategory.Ability })
            {
                var nodesInCategory = UpgradeManager.Instance.Tree.Where(n => n.category == category).ToList();
                if (nodesInCategory.Count == 0) continue;

                var header = new Label(CategoryLabel(category));
                header.AddToClassList("category-header");
                _nodeScroll.Add(header);

                foreach (var node in nodesInCategory)
                {
                    var row = new VisualElement();
                    row.AddToClassList("node-row");
                    if (node.isUnlocked) row.AddToClassList("node-row--unlocked");
                    if (node.id == _selectedNodeId) row.AddToClassList("node-row--selected");

                    var label = new Label($"{node.id} ({node.cost} DNA){(node.isUnlocked ? " ✓" : "")}");
                    label.AddToClassList("node-row__label");
                    row.Add(label);

                    string capturedId = node.id;
                    row.RegisterCallback<ClickEvent>(_ => SelectNode(capturedId));

                    _nodeScroll.Add(row);
                }
            }
        }

        private void SelectNode(string nodeId)
        {
            _selectedNodeId = nodeId;
            var node = UpgradeManager.Instance?.GetNode(nodeId);
            if (node == null) return;

            _detailTitle.text = node.id;
            _detailDesc.text = BuildDescription(node);

            bool canUnlock = UpgradeManager.Instance.CanUnlock(nodeId);
            _buyButton.SetEnabled(canUnlock);
            _buyButton.text = node.isUnlocked ? "이미 해금됨" : "구매";

            RebuildNodeList();
        }

        private static string BuildDescription(UpgradeNode node)
        {
            var sb = new StringBuilder();
            sb.Append($"비용: {node.cost} DNA\n");
            if (node.prerequisites.Count > 0)
                sb.Append($"선행 노드: {string.Join(", ", node.prerequisites)}\n");
            if (node.effects.Count > 0)
            {
                sb.Append("효과: ");
                sb.Append(string.Join(", ", node.effects.Select(e => $"{e.statName} {(e.amount >= 0 ? "+" : "")}{e.amount:0.##}")));
            }
            return sb.ToString();
        }

        private void HandleBuyClicked()
        {
            if (string.IsNullOrEmpty(_selectedNodeId)) return;
            if (UpgradeManager.Instance.TryUnlock(_selectedNodeId))
                SelectNode(_selectedNodeId);
        }

        /// <summary>설계 문서 13절 표 1행: 업그레이드 화면에서 선택 시청 -> DNA +10.</summary>
        private void HandleAdBonusClicked()
        {
            _adBonusButton.SetEnabled(false); // 중복 클릭 차단 (위키 함정 #10 대응)
            GameAds.Rewarded.Show(
                onSuccess: () =>
                {
                    UpgradeManager.Instance?.AddDna(adBonusDna);
                    _adBonusButton.SetEnabled(true);
                },
                onFailed: () => _adBonusButton.SetEnabled(true));
        }

        private void HandleDnaChanged(int totalDna) => RefreshDna();
        private void HandleNodeUnlocked(UpgradeNode node) => RebuildNodeList();

        private static string CategoryLabel(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "감염 경로",
            UpgradeCategory.Symptom => "증상",
            UpgradeCategory.Ability => "능력",
            _ => category.ToString()
        };
    }
}
