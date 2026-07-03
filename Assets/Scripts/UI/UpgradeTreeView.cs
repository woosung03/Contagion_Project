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
    ///
    /// UI/UX 폴리싱 — 전파/증상/능력 탭 3개가 전부 같은 창(27노드가 한 캔버스에 다 있는 창)을 열어서
    /// 카테고리 구분이 안 된다는 피드백으로, 이 컴포넌트를 "카테고리 하나만 담당" 하도록 바꿨다.
    /// 씬에 이 스크립트를 붙인 GameObject 3개(TransmissionTreeUI/SymptomTreeUI/AbilityTreeUI)를 만들고
    /// 각각 인스펙터에서 <see cref="category"/>만 다르게 지정한다 — UXML/USS는 동일한 UpgradeTree.uxml을
    /// 재사용(카테고리 제목만 코드에서 동적으로 채움). 전체 트리(27개)가 아니라 그 카테고리(9개)만
    /// 필터링해서 그리므로, DefaultUpgradeTreeFactory가 부여한 절대 좌표(카테고리별로 x 40~1600까지
    /// 넓게 퍼져있음)를 그대로 쓰면 창 하나에 카테고리 하나만 있는데도 왼쪽에 큰 빈 여백이 생긴다 —
    /// RebuildTree()에서 그 카테고리의 최소 x값을 빼서 항상 0부터 시작하도록 보정한다.
    ///
    /// UI/UX 폴리싱 추가 — 창 3개가 버튼 3개로 따로 열리던 걸 "버튼 하나 + 좌우 화살표로 카테고리
    /// 전환"하는 방식으로 바꿨다(HUD 탭이 3개→1개). 이 클래스 자체는 여전히 카테고리 하나만 담당하는
    /// 그대로고(3개 GameObject 구조 유지 — 실제로 화면을 완전히 하나로 합치려면 UIDocument 3개를
    /// 병합해야 하는데 리스크가 커서 보류), 대신 헤더에 이전/다음 버튼을 추가해 클릭 시
    /// <see cref="OnPrevRequested"/>/<see cref="OnNextRequested"/> 이벤트만 쏜다. 실제 페이지 전환(현재
    /// 보이는 창을 Hide()하고 다음 창을 Show())은 <see cref="Contagion.Managers.UIManager"/>가 담당 —
    /// 같은 버튼 클릭 콜백 안에서 동시에 일어나므로 사용자 입장에서는 창이 하나이고 그 안에서
    /// 좌우로 넘어가는 것처럼 보인다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeTreeView : MonoBehaviour
    {
        // 열 간격을 180→140으로 줄인 DefaultUpgradeTreeFactory에 맞춰 노드 박스와 캔버스 여백도 축소
        // (트리를 좀 더 작게 해달라는 피드백 + 참조 해상도 480px 폭 안에 가로로 다 들어가게 하기 위함).
        private const float NodeWidth = 110f;
        private const float NodeHeight = 50f;
        private const float CanvasPadding = 48f;
        private const float TopOffset = 34f; // 카테고리 제목을 위한 상단 여백

        [SerializeField, Tooltip("이 창이 담당할 업그레이드 카테고리 — 이 창은 이 카테고리 노드만 그린다.")]
        private UpgradeCategory category = UpgradeCategory.Transmission;

        private VisualElement _upgradeRoot;
        private Label _dnaLabel;
        private Label _categoryTitleLabel;
        private ScrollView _nodeScroll;
        private Label _detailTitle;
        private Label _detailDesc;
        private Button _buyButton;
        private Button _closeButton;
        private Button _adBonusButton;
        private Button _prevButton;
        private Button _nextButton;

        /// <summary>헤더의 ◀ 버튼 클릭 — 실제 페이지 전환은 UIManager가 담당.</summary>
        public event System.Action OnPrevRequested;
        /// <summary>헤더의 ▶ 버튼 클릭 — 실제 페이지 전환은 UIManager가 담당.</summary>
        public event System.Action OnNextRequested;

        private string _selectedNodeId;

        [SerializeField, Tooltip("광고 시청 보상 DNA — 설계 문서 13절 표 1행")]
        private int adBonusDna = 10;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _upgradeRoot = root.Q<VisualElement>("upgrade-root");
            _dnaLabel = root.Q<Label>("dna-label");
            _categoryTitleLabel = root.Q<Label>("category-title-label");
            _nodeScroll = root.Q<ScrollView>("node-scroll");
            _detailTitle = root.Q<Label>("detail-title");
            _detailDesc = root.Q<Label>("detail-desc");
            _buyButton = root.Q<Button>("buy-button");
            _closeButton = root.Q<Button>("close-button");
            _adBonusButton = root.Q<Button>("ad-bonus-button");
            _prevButton = root.Q<Button>("prev-category-button");
            _nextButton = root.Q<Button>("next-category-button");

            if (_categoryTitleLabel != null)
                _categoryTitleLabel.text = CategoryLabel(category);

            // 카테고리 하나(9노드, 3열×3티어)만 그리므로 이제 세로 스크롤만으로 충분 — 예전엔 27노드가
            // 한 캔버스에 다 있어서(카테고리 3개를 가로로 나열) 양방향 스크롤이 필요했다.
            _nodeScroll.mode = ScrollViewMode.Vertical;

            _closeButton.RegisterCallback<ClickEvent>(_ => Hide());
            _buyButton.RegisterCallback<ClickEvent>(_ => HandleBuyClicked());
            _adBonusButton.RegisterCallback<ClickEvent>(_ => HandleAdBonusClicked());
            _prevButton?.RegisterCallback<ClickEvent>(_ => OnPrevRequested?.Invoke());
            _nextButton?.RegisterCallback<ClickEvent>(_ => OnNextRequested?.Invoke());

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

        /// <summary>HUD 탭 버튼에서 호출 — 이 창은 인스펙터에 지정된 <see cref="category"/> 하나만 그린다.</summary>
        public void Show()
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

        /// <summary>이번 RebuildTree() 호출에서 이 카테고리 노드들의 x 최소값 — 0부터 시작하도록 빼주는 보정값.</summary>
        private float _xOffset;

        /// <summary>
        /// node.position 절대 좌표로 노드를 배치하고, 선행조건 간 연결선을 그린다. 이 카테고리(9개)만
        /// 필터링해서 그리며, DefaultUpgradeTreeFactory의 절대 좌표(카테고리별로 40/640/1240부터 시작)를
        /// 그대로 쓰면 창 하나에 한 카테고리만 있는데도 왼쪽에 큰 빈 여백이 생기므로 _xOffset을 빼서
        /// 항상 x=0부터 시작하도록 보정한다.
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

            var nodes = UpgradeManager.Instance.Tree.Where(n => n.category == category).ToList();
            if (nodes.Count == 0)
            {
                Debug.LogWarning($"[UpgradeTreeView] {category} 카테고리 노드가 없습니다 — GameDataBootstrapper의 " +
                    "SetTree 호출 여부 확인 (DefaultUpgradeTreeFactory 폴백이 정상이면 카테고리당 9개가 있어야 함).");
                return;
            }

            _xOffset = nodes.Min(n => n.position.x);

            float maxX = 0f, maxY = 0f;
            foreach (var n in nodes)
            {
                maxX = Mathf.Max(maxX, n.position.x - _xOffset + NodeWidth);
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

            foreach (var node in nodes)
                canvas.Add(CreateNodeElement(node));

            Debug.Log($"[UpgradeTreeView] {category} 트리 렌더링 완료 — 노드 {nodes.Count}개, 캔버스 크기 {canvasWidth}x{canvasHeight}");
        }

        private VisualElement CreateNodeElement(UpgradeNode node)
        {
            var box = new VisualElement();
            box.AddToClassList("tree-node");
            box.AddToClassList($"tree-node--{CategoryClass(node.category)}");
            if (node.isUnlocked) box.AddToClassList("tree-node--unlocked");
            if (node.id == _selectedNodeId) box.AddToClassList("tree-node--selected");

            box.style.position = Position.Absolute;
            box.style.left = node.position.x - _xOffset;
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

        /// <summary>선행조건 -> 노드 연결선. 선행 노드가 이미 해금됐으면 초록색 굵은 선, 아니면 흐린 회색 선.
        /// 이 카테고리는 자체 완결형(선행조건이 전부 같은 카테고리 안에서만 걸림 — DefaultUpgradeTreeFactory
        /// 참고)이라 카테고리로 필터링해도 연결선이 끊길 일은 없다.</summary>
        private void DrawConnections(MeshGenerationContext mgc)
        {
            if (UpgradeManager.Instance == null) return;

            var painter = mgc.painter2D;
            foreach (var node in UpgradeManager.Instance.Tree.Where(n => n.category == category))
            {
                if (node.prerequisites == null) continue;
                foreach (var prereqId in node.prerequisites)
                {
                    var prereq = UpgradeManager.Instance.GetNode(prereqId);
                    if (prereq == null || prereq.category != category) continue;

                    bool active = prereq.isUnlocked;
                    painter.strokeColor = active
                        ? new Color(0.4f, 0.9f, 0.5f, 0.85f)
                        : new Color(1f, 1f, 1f, 0.2f);
                    painter.lineWidth = active ? 3f : 2f;

                    Vector2 from = new Vector2(prereq.position.x - _xOffset, prereq.position.y)
                        + new Vector2(NodeWidth / 2f, NodeHeight / 2f + TopOffset);
                    Vector2 to = new Vector2(node.position.x - _xOffset, node.position.y)
                        + new Vector2(NodeWidth / 2f, NodeHeight / 2f + TopOffset);

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
