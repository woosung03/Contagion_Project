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
    /// 예전엔 노드 좌표 데이터가 없어 카테고리별 리스트로 단순화했었는데, DefaultUpgradeTreeFactory가
    /// 각 노드에 좌표(position)를 부여하면서 실제 좌표 배치 + 선행조건 연결선(Painter2D)으로 교체했다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeTreeView : MonoBehaviour
    {
        private const float NodeWidth = 140f;
        private const float NodeHeight = 60f;
        private const float CanvasPadding = 80f;
        private const float TopOffset = 34f; // 카테고리 라벨을 위한 상단 여백

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

            // 트리 캔버스가 화면보다 넓다(27노드 3열 배치) — 세로만 스크롤되던 기본값으론 가로쪽
            // 노드들이 잘려서 안 보이므로 양방향 스크롤로 전환 (모바일 좁은 화면에서 특히 중요).
            _nodeScroll.mode = ScrollViewMode.VerticalAndHorizontal;

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
            RebuildTree();
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

        /// <summary>
        /// node.position 절대 좌표로 노드를 배치하고, 선행조건 간 연결선을 그린다.
        /// 기본 플레이 퀄리티/세분화 개선 항목.
        /// </summary>
        private void RebuildTree()
        {
            var canvas = _nodeScroll.contentContainer;
            canvas.Clear();

            if (UpgradeManager.Instance == null)
            {
                Debug.LogWarning("[UpgradeTreeView] UpgradeManager.Instance가 없어 트리를 그릴 수 없습니다.");
                return;
            }

            var nodes = UpgradeManager.Instance.Tree;
            if (nodes.Count == 0)
            {
                Debug.LogWarning("[UpgradeTreeView] 업그레이드 트리가 비어있습니다 — GameDataBootstrapper의 " +
                    "SetTree 호출 여부 확인 (DefaultUpgradeTreeFactory 폴백이 정상이면 27개가 있어야 함).");
                return;
            }

            float maxX = 0f, maxY = 0f;
            foreach (var n in nodes)
            {
                maxX = Mathf.Max(maxX, n.position.x + NodeWidth);
                maxY = Mathf.Max(maxY, n.position.y + NodeHeight);
            }
            float canvasWidth = maxX + CanvasPadding;
            float canvasHeight = maxY + TopOffset + CanvasPadding;

            canvas.style.position = Position.Relative;
            canvas.style.width = canvasWidth;
            canvas.style.height = canvasHeight;

            var connectionsLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            connectionsLayer.style.position = Position.Absolute;
            connectionsLayer.style.left = 0;
            connectionsLayer.style.top = 0;
            connectionsLayer.style.width = canvasWidth;
            connectionsLayer.style.height = canvasHeight;
            connectionsLayer.generateVisualContent += DrawConnections;
            canvas.Add(connectionsLayer);

            foreach (var category in new[] { UpgradeCategory.Transmission, UpgradeCategory.Symptom, UpgradeCategory.Ability })
            {
                var firstOfCategory = nodes.Where(n => n.category == category).OrderBy(n => n.position.x).FirstOrDefault();
                if (firstOfCategory == null) continue;

                var header = new Label(CategoryLabel(category));
                header.AddToClassList("tree-category-label");
                header.style.position = Position.Absolute;
                header.style.left = firstOfCategory.position.x;
                header.style.top = 4f;
                canvas.Add(header);
            }

            foreach (var node in nodes)
                canvas.Add(CreateNodeElement(node));

            Debug.Log($"[UpgradeTreeView] 트리 렌더링 완료 — 노드 {nodes.Count}개, 캔버스 크기 {canvasWidth}x{canvasHeight}");
        }

        private VisualElement CreateNodeElement(UpgradeNode node)
        {
            var box = new VisualElement();
            box.AddToClassList("tree-node");
            box.AddToClassList($"tree-node--{CategoryClass(node.category)}");
            if (node.isUnlocked) box.AddToClassList("tree-node--unlocked");
            if (node.id == _selectedNodeId) box.AddToClassList("tree-node--selected");

            box.style.position = Position.Absolute;
            box.style.left = node.position.x;
            box.style.top = node.position.y + TopOffset;
            box.style.width = NodeWidth;
            box.style.height = NodeHeight;

            var label = new Label(node.id);
            label.AddToClassList("tree-node__label");
            box.Add(label);

            int effectiveCost = UpgradeManager.Instance.GetEffectiveCost(node);
            var costLabel = new Label(node.isUnlocked ? "해금됨" : $"{effectiveCost} DNA");
            costLabel.AddToClassList("tree-node__cost");
            box.Add(costLabel);

            string capturedId = node.id;
            box.RegisterCallback<ClickEvent>(_ => SelectNode(capturedId));

            return box;
        }

        /// <summary>선행조건 -> 노드 연결선. 선행 노드가 이미 해금됐으면 초록색 굵은 선, 아니면 흐린 회색 선.</summary>
        private void DrawConnections(MeshGenerationContext mgc)
        {
            if (UpgradeManager.Instance == null) return;

            var painter = mgc.painter2D;
            foreach (var node in UpgradeManager.Instance.Tree)
            {
                if (node.prerequisites == null) continue;
                foreach (var prereqId in node.prerequisites)
                {
                    var prereq = UpgradeManager.Instance.GetNode(prereqId);
                    if (prereq == null) continue;

                    bool active = prereq.isUnlocked;
                    painter.strokeColor = active
                        ? new Color(0.4f, 0.9f, 0.5f, 0.85f)
                        : new Color(1f, 1f, 1f, 0.2f);
                    painter.lineWidth = active ? 3f : 2f;

                    Vector2 from = prereq.position + new Vector2(NodeWidth / 2f, NodeHeight / 2f + TopOffset);
                    Vector2 to = node.position + new Vector2(NodeWidth / 2f, NodeHeight / 2f + TopOffset);

                    painter.BeginPath();
                    painter.MoveTo(from);
                    painter.LineTo(to);
                    painter.Stroke();
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
            _buyButton.text = node.isUnlocked ? "이미 해금됨" : $"구매 ({UpgradeManager.Instance.GetEffectiveCost(node)} DNA)";

            RebuildTree();
        }

        private static string BuildDescription(UpgradeNode node)
        {
            var sb = new StringBuilder();
            sb.Append($"기본 비용: {node.cost} DNA (실제 비용은 같은 카테고리 해금 수에 따라 증가)\n");
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
        private void HandleNodeUnlocked(UpgradeNode node) => RebuildTree();

        private static string CategoryLabel(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "감염 경로",
            UpgradeCategory.Symptom => "증상",
            UpgradeCategory.Ability => "능력",
            _ => category.ToString()
        };

        private static string CategoryClass(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "transmission",
            UpgradeCategory.Symptom => "symptom",
            UpgradeCategory.Ability => "ability",
            _ => "unknown"
        };
    }
}
