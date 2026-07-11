using System.Collections.Generic;
using System.Linq;
using Contagion.Ads;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// Research Database 화면 (Commit 1 — "화면 전환 + UI 구조 변경"만 포함하는 UI Shell).
    /// 참고: Docs/ResearchDatabase_MVP_ImplementationPlan.md, Docs/UpgradeTree_ResearchDatabase_ScreenDesign.md.
    ///
    /// 예전엔 이 화면이 UpgradeManager.Tree의 실제 45개 노드를 절대좌표 캔버스(RebuildTree()) +
    /// 선행조건 연결선(DrawConnections(), Painter2D)으로 그렸다. 이번 커밋은 그 캔버스를 세로
    /// 스크롤 리스트(갈래 섹션 헤더 + 연구 항목 행)로 교체하되, 목록 내용 자체는 아직 더미 데이터
    /// (<see cref="BuildDummyBranches"/>)다 — 실제 UpgradeNode 연결(코드/상태/DNA 비용을
    /// UpgradeManager에서 읽어오는 것)은 다음 커밋(Commit 2)에서 수행한다. 그래서 상세 패널의
    /// "연구 시작" 버튼도 이번 커밋에서는 항상 비활성화 상태다 — 아직 아무 노드에도 연결돼 있지
    /// 않은데 눌리면 안 되기 때문.
    ///
    /// DetermineState()/StateCaption()/CategoryLabel() 등 상태 판정·표시 텍스트 헬퍼와
    /// NodeDisplayNames/_codeByNodeId 필드는 Commit 2에서 그대로 재사용할 예정이라 이번 커밋에서
    /// 지우지 않고 남겨뒀다(현재는 미사용 — ResearchDatabase_MVP_ImplementationPlan.md §5 재사용
    /// 표 참고). 씬 GameObject 3개(TransmissionTreeUI/SymptomTreeUI/AbilityTreeUI, 카테고리별
    /// 독립 UIDocument) 구조는 그대로 유지한다 — 탭 클릭 시 UIManager가 세 UIDocument의 표시
    /// 여부만 토글하므로(플랜 §6-3), 씬 배선을 다시 할 필요가 없다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeTreeView : MonoBehaviour
    {
        /// <summary>
        /// node.id(영문 내부 식별자) → 한국어 표시명. Commit 2에서 실제 UpgradeNode 연결 시
        /// 그대로 재사용할 딕셔너리 — 이번 커밋(더미 데이터 단계)에서는 미사용.
        /// </summary>
        private static readonly Dictionary<string, string> NodeDisplayNames = new Dictionary<string, string>
        {
            // 감염 경로
            { "trans_air1", "공기 전파 I" },
            { "trans_water1", "수인성 전파 I" },
            { "trans_contact1", "접촉 전파 I" },
            { "trans_air2", "공기 전파 II" },
            { "trans_water2", "수인성 전파 II" },
            { "trans_insect1", "곤충 매개 전파" },
            { "trans_droplet1", "비말 전파" },
            { "trans_animal1", "인수공통 전파" },
            { "trans_blood1", "혈액 전파" },
            { "trans_droplet2", "비말 전파 강화" },
            { "trans_animal2", "인수공통 전파 강화" },
            { "trans_blood2", "혈액 전파 강화" },
            { "trans_advanced1", "광역 전파 I" },
            { "trans_advanced2", "광역 전파 II" },
            { "trans_global", "전지구적 전파" },
            // 증상
            { "sym_cough", "기침" },
            { "sym_rash", "발진" },
            { "sym_nausea", "구토감" },
            { "sym_fever", "발열" },
            { "sym_lesion", "피부 병변" },
            { "sym_vomit", "구토" },
            { "sym_pneumonia", "폐렴" },
            { "sym_dermatitis", "피부염" },
            { "sym_hemorrhage", "출혈" },
            { "sym_respfailure", "호흡 부전" },
            { "sym_necrosis", "만성 병변" },
            { "sym_sepsis", "패혈증" },
            { "sym_multiorgan1", "다발성 장기부전 I" },
            { "sym_multiorgan2", "다발성 장기부전 II" },
            { "sym_organfailure", "전신 장기부전" },
            // 적응 (구 "능력")
            { "abl_mutation1", "유전자 변이 I" },
            { "abl_stealth1", "은신 I" },
            { "abl_hardening1", "구조 강화 I" },
            { "abl_mutation2", "유전자 변이 II" },
            { "abl_stealth2", "은신 II" },
            { "abl_hardening2", "구조 강화 II" },
            { "abl_resist1", "약물 저항 I" },
            { "abl_camouflage1", "위장 I" },
            { "abl_resist2", "구조적 내성 I" },
            { "abl_resist3", "약물 저항 II" },
            { "abl_camouflage2", "위장 II" },
            { "abl_resist4", "구조적 내성 II" },
            { "abl_superbug1", "슈퍼균주 I" },
            { "abl_superbug2", "슈퍼균주 II" },
            { "abl_finalevo", "최종 진화체" },
        };

        /// <summary>node.id → 한국어 표시명. Commit 2 재사용 예정, 현재 미사용.</summary>
        private static string DisplayName(string id) =>
            NodeDisplayNames.TryGetValue(id, out var name) ? name : id;

        [SerializeField, Tooltip("이 창이 담당할 업그레이드 카테고리 — 이 창은 이 카테고리 노드만 그린다.")]
        private UpgradeCategory category = UpgradeCategory.Transmission;

        /// <summary>UIManager가 탭 클릭 시 어느 카테고리 화면을 보여줘야 할지 판단하는 데 쓴다.</summary>
        public UpgradeCategory Category => category;

        private VisualElement _upgradeRoot;
        private Label _dnaLabel;
        private Label _labCaptionLabel;
        private ScrollView _nodeScroll;
        private Label _detailTitle;
        private VisualElement _detailRows;
        private Button _buyButton;
        private Button _closeButton;
        private Button _adBonusButton;
        private Button _tabTransmissionButton;
        private Button _tabSymptomButton;
        private Button _tabAbilityButton;

        /// <summary>node.id → 표시용 코드(예: "TRANS-001"). Commit 2에서 실제 노드 순서 기준으로
        /// 채울 예정 — 이번 커밋에서는 더미 항목의 코드를 <see cref="BuildResearchList"/>가 직접
        /// 생성하므로 미사용.</summary>
        private readonly Dictionary<string, string> _codeByNodeId = new Dictionary<string, string>();

        /// <summary>탭 클릭 — 다른 카테고리를 요청하면 발생. 실제 화면 전환(다른 UIDocument
        /// Show/Hide)은 UIManager가 담당한다.</summary>
        public event System.Action<UpgradeCategory> OnCategoryRequested;

        private DummyResearchItem _selectedDummyItem;

        [SerializeField, Tooltip("광고 시청 보상 DNA — 설계 문서 13절 표 1행")]
        private int adBonusDna = 10;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _upgradeRoot = root.Q<VisualElement>("upgrade-root");
            _dnaLabel = root.Q<Label>("dna-label");
            _labCaptionLabel = root.Q<Label>("lab-caption-label");
            _nodeScroll = root.Q<ScrollView>("node-scroll");
            _detailTitle = root.Q<Label>("detail-title");
            _detailRows = root.Q<VisualElement>("detail-rows");
            _buyButton = root.Q<Button>("buy-button");
            _closeButton = root.Q<Button>("close-button");
            _adBonusButton = root.Q<Button>("ad-bonus-button");
            _tabTransmissionButton = root.Q<Button>("tab-transmission-button");
            _tabSymptomButton = root.Q<Button>("tab-symptom-button");
            _tabAbilityButton = root.Q<Button>("tab-ability-button");

            _nodeScroll.mode = ScrollViewMode.Vertical;

            _closeButton.RegisterCallback<ClickEvent>(_ => Hide());
            _adBonusButton.RegisterCallback<ClickEvent>(_ => HandleAdBonusClicked());
            _tabTransmissionButton?.RegisterCallback<ClickEvent>(_ => RequestCategory(UpgradeCategory.Transmission));
            _tabSymptomButton?.RegisterCallback<ClickEvent>(_ => RequestCategory(UpgradeCategory.Symptom));
            _tabAbilityButton?.RegisterCallback<ClickEvent>(_ => RequestCategory(UpgradeCategory.Ability));
            UpdateTabHighlight();

            Subscribe();
            Hide();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (UpgradeManager.Instance == null) return;
            UpgradeManager.Instance.OnDnaChanged -= HandleDnaChanged;
            UpgradeManager.Instance.OnDnaChanged += HandleDnaChanged;
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance == null) return;
            UpgradeManager.Instance.OnDnaChanged -= HandleDnaChanged;
        }

        /// <summary>HUD "업그레이드" 버튼 또는 탭 클릭에서 호출 — 이 창은 인스펙터에 지정된
        /// <see cref="category"/> 하나만 그린다.</summary>
        public void Show()
        {
            _upgradeRoot.style.display = DisplayStyle.Flex;
            UpdateTabHighlight();
            BuildResearchList();
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

        /// <summary>이 화면 자신의 탭에 강조 클래스를 준다 — 화면 하나가 카테고리 하나만 담당하므로
        /// 항상 자기 카테고리 탭만 활성 표시하면 된다.</summary>
        private void UpdateTabHighlight()
        {
            _tabTransmissionButton?.RemoveFromClassList("tab-button--active");
            _tabSymptomButton?.RemoveFromClassList("tab-button--active");
            _tabAbilityButton?.RemoveFromClassList("tab-button--active");

            var activeTab = category switch
            {
                UpgradeCategory.Transmission => _tabTransmissionButton,
                UpgradeCategory.Symptom => _tabSymptomButton,
                UpgradeCategory.Ability => _tabAbilityButton,
                _ => null
            };
            activeTab?.AddToClassList("tab-button--active");
        }

        private void RequestCategory(UpgradeCategory target)
        {
            if (target == category) return; // 이미 이 화면이 보여주는 카테고리 — 아무 일도 안 함
            OnCategoryRequested?.Invoke(target);
        }

        /// <summary>
        /// 갈래 섹션 헤더 + 연구 항목 행을 순서대로 Add() — 예전 RebuildTree()의 절대좌표 계산
        /// (_xOffset, canvasWidth/Height, node.position 기반 style.left/top 대입)과 DrawConnections()의
        /// Painter2D 꺾은선 계산을 전부 대체한다. UI Toolkit 기본 Flex 세로 흐름만으로 배치되므로
        /// 좌표 계산 코드가 필요 없다(ScreenDesign.md §10).
        /// 이번 커밋은 더미 데이터만 그린다 — 실제 UpgradeManager.Tree 연결은 Commit 2.
        /// </summary>
        private void BuildResearchList()
        {
            var container = _nodeScroll.contentContainer;
            container.Clear();

            if (_labCaptionLabel != null)
                _labCaptionLabel.text = $"{CategoryEnglishCaption(category)} LAB";

            int order = 0;
            foreach (var branch in BuildDummyBranches(category))
            {
                container.Add(CreateBranchSectionHeader(branch.Label));
                foreach (var item in branch.Items)
                {
                    order++;
                    item.Code = $"{CategoryPrefix(category)}-{order:000}";
                    container.Add(CreateResearchRow(item));
                }
            }
        }

        private VisualElement CreateBranchSectionHeader(string branchLabel)
        {
            var caption = new Label(branchLabel);
            caption.AddToClassList("section-caption");
            caption.AddToClassList($"section-caption--{CategoryClass(category)}");
            return caption;
        }

        private VisualElement CreateResearchRow(DummyResearchItem item)
        {
            var row = new VisualElement();
            row.AddToClassList("research-row");
            row.AddToClassList($"research-row--{CategoryClass(category)}");
            row.AddToClassList($"research-row--{item.State}");
            if (item == _selectedDummyItem) row.AddToClassList("research-row--selected");

            var codeLabel = new Label(item.Code);
            codeLabel.AddToClassList("research-row__code");
            row.Add(codeLabel);

            var nameLabel = new Label(item.Name);
            nameLabel.AddToClassList("research-row__name");
            row.Add(nameLabel);

            string statusText = StateCaption(item.State);
            if (item.State == "locked" && !string.IsNullOrEmpty(item.LockReason))
                statusText += $" · 선행: {item.LockReason}";
            var statusLabel = new Label(statusText);
            statusLabel.AddToClassList("research-row__status");
            row.Add(statusLabel);

            var costLabel = new Label(item.CostText);
            costLabel.AddToClassList("research-row__cost");
            row.Add(costLabel);

            row.RegisterCallback<ClickEvent>(_ => SelectDummyItem(item));
            return row;
        }

        /// <summary>LOCKED/AVAILABLE/ACTIVE/MAXED 4단계를 UpgradeNode 없이 문자열 그대로 조회하는
        /// 더미 버전. Commit 2에서 <c>DetermineState(UpgradeNode)</c>가 반환하는 값과 동일한 4개
        /// 문자열("locked"/"available"/"active"/"maxed")을 쓰므로, 실제 데이터 연결 시 이 캡션
        /// 매핑을 그대로 재사용할 수 있다.</summary>
        private static string StateCaption(string state) => state switch
        {
            "locked" => "잠김",
            "available" => "연구 가능",
            "active" => "진행 중",
            "maxed" => "완료",
            _ => state.ToUpperInvariant()
        };

        /// <summary>UpgradeNode 기반 상태 판정 — Commit 2에서 재사용 예정, 이번 커밋(더미 데이터
        /// 단계)에서는 호출되지 않는다. isUnlocked/prerequisites 등 읽기 전용 조회만 하므로 게임
        /// 로직에는 영향이 없다.</summary>
        private static string DetermineState(UpgradeNode node)
        {
            if (node.isUnlocked)
            {
                bool isLeaf = !UpgradeManager.Instance.Tree.Any(n => n.prerequisites.Contains(node.id));
                return isLeaf ? "maxed" : "active";
            }

            bool prereqsMet = node.prerequisites.All(pid => UpgradeManager.Instance.IsUnlocked(pid));
            return prereqsMet ? "available" : "locked";
        }

        private void SelectDummyItem(DummyResearchItem item)
        {
            _selectedDummyItem = item;

            _detailTitle.text = item.Name;
            _detailRows?.Clear();
            AddDetailRow("이름", item.Name);
            AddDetailRow("설명", item.Description);
            AddDetailRow("비용", item.CostText);
            AddDetailRow("상태", StateCaption(item.State), $"data-value--{item.State}");

            // Commit 1은 UI 구조 변경만 포함 — 실제 UpgradeManager.TryUnlock() 연결은 Commit 2.
            // 그래서 버튼은 항상 비활성화 상태로 둔다(아무 노드에도 연결돼 있지 않은데 눌리면 안 됨).
            _buyButton.SetEnabled(false);
            _buyButton.text = item.State == "active" || item.State == "maxed"
                ? "연구 완료"
                : "연구 시작 (연결 예정)";

            BuildResearchList(); // 선택 강조(research-row--selected) 반영
        }

        /// <summary>data-row 한 줄을 만들어 detail-rows 컨테이너에 추가한다(country-dock__row와
        /// 동일 계약 — UI_Design.md 3절/11.4).</summary>
        private void AddDetailRow(string label, string value, string valueStateClass = null)
        {
            if (_detailRows == null) return;

            var row = new VisualElement();
            row.AddToClassList("data-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("data-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("data-value");
            if (!string.IsNullOrEmpty(valueStateClass)) valueEl.AddToClassList(valueStateClass);
            row.Add(valueEl);

            _detailRows.Add(row);
        }

        /// <summary>node.effects의 statName -> 상세 패널 표시 라벨. Commit 2 재사용 예정,
        /// 이번 커밋에서는 미사용.</summary>
        private static string EffectStatLabel(string statName) => statName switch
        {
            "infectivity" => "전파력",
            "severity" => "중증도",
            "lethality" => "치사율",
            "drugResistance" => "약물 내성",
            _ => statName.ToUpperInvariant()
        };

        /// <summary>설계 문서 13절 표 1행: 업그레이드 화면에서 선택 시청 -> DNA +10. 화면 구조와
        /// 무관한 기존 DNA 재화 로직이라 이번 커밋에서도 그대로 유지한다.</summary>
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

        private static string CategoryLabel(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "감염 경로",
            UpgradeCategory.Symptom => "증상",
            UpgradeCategory.Ability => "적응", // ResearchDatabase_Design.md 권장안 — "능력"에서 개명
            _ => category.ToString()
        };

        private static string CategoryClass(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "transmission",
            UpgradeCategory.Symptom => "symptom",
            UpgradeCategory.Ability => "ability",
            _ => "unknown"
        };

        /// <summary>탭 라벨(짧은 한글) — CategoryLabel()보다 더 축약된 표기가 필요한 전파 탭만
        /// 다르고("감염 경로" 대신 "전파"), 나머지는 CategoryLabel()과 동일.</summary>
        private static string CategoryTabLabel(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "전파",
            UpgradeCategory.Symptom => "증상",
            UpgradeCategory.Ability => "적응",
            _ => category.ToString()
        };

        /// <summary>목록 영역 상단 영문 LAB 캡션(예: "TRANSMISSION LAB").</summary>
        private static string CategoryEnglishCaption(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "TRANSMISSION",
            UpgradeCategory.Symptom => "SYMPTOM",
            UpgradeCategory.Ability => "ADAPTATION",
            _ => category.ToString().ToUpperInvariant()
        };

        /// <summary>노드 표시코드(TRANS-001 등) 접두어. "능력"→"적응" 개명에 맞춰 접두어도 갱신.</summary>
        private static string CategoryPrefix(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Transmission => "감염",
            UpgradeCategory.Symptom => "증상",
            UpgradeCategory.Ability => "적응",
            _ => "노드"
        };

        // ================================================================
        // 더미 연구 데이터 (Commit 1 전용) — 실제 UpgradeNode/UpgradeManager.Tree를 전혀 읽지 않는다.
        // 브랜치·항목명은 NodeDisplayNames/UpgradeTree_ResearchDatabase_ScreenDesign.md §3 예시를
        // 그대로 따서 Commit 2 전환 시 위화감이 없게 했다. Commit 2에서는 이 메서드 전체가
        // DefaultUpgradeTreeFactory.cs의 prerequisites/position 기반 실제 그룹핑으로 교체된다.
        // ================================================================

        private class DummyResearchItem
        {
            public string Code;
            public readonly string Name;
            public readonly string State; // "locked" | "available" | "active" | "maxed"
            public readonly string LockReason;
            public readonly string CostText;
            public readonly string Description;

            public DummyResearchItem(string name, string state, string lockReason, string costText, string description)
            {
                Name = name;
                State = state;
                LockReason = lockReason;
                CostText = costText;
                Description = description;
            }
        }

        private class DummyBranch
        {
            public readonly string Label;
            public readonly List<DummyResearchItem> Items;

            public DummyBranch(string label, List<DummyResearchItem> items)
            {
                Label = label;
                Items = items;
            }
        }

        private static List<DummyBranch> BuildDummyBranches(UpgradeCategory targetCategory) => targetCategory switch
        {
            UpgradeCategory.Transmission => new List<DummyBranch>
            {
                new DummyBranch("공기 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("공기 전파 I", "maxed", null, "완료", "비말 형태로 병원체가 실내 공기 중에 부유해 확산된다."),
                    new DummyResearchItem("공기 전파 II", "active", null, "18 DNA", "공기 전파 범위가 확장되어 전파력이 추가로 상승한다."),
                    new DummyResearchItem("비말 전파", "available", null, "22 DNA", "기침·재채기로 배출된 비말을 통해 근접 감염이 강화된다."),
                    new DummyResearchItem("비말 전파 강화", "locked", "비말 전파", "—", "비말 입자의 생존 시간을 늘려 전파 반경을 넓힌다."),
                }),
                new DummyBranch("수인성 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("수인성 전파 I", "available", null, "16 DNA", "오염된 식수를 통해 병원체가 퍼진다."),
                    new DummyResearchItem("수인성 전파 II", "locked", "수인성 전파 I", "—", "정수 시설이 미비한 지역에서 전파력이 추가로 상승한다."),
                    new DummyResearchItem("곤충 매개 전파", "locked", "수인성 전파 II", "—", "모기 등 매개체를 통한 간접 전파 경로가 열린다."),
                }),
                new DummyBranch("접촉·동물매개 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("접촉 전파 I", "available", null, "16 DNA", "직접 접촉을 통한 전파력이 상승한다."),
                    new DummyResearchItem("인수공통 전파", "locked", "접촉 전파 I", "—", "가축·야생동물을 매개로 한 전파 경로가 열린다."),
                    new DummyResearchItem("혈액 전파", "locked", "접촉 전파 I", "—", "혈액 접촉을 통한 전파 경로가 열린다."),
                }),
                new DummyBranch("통합 연구", new List<DummyResearchItem>
                {
                    new DummyResearchItem("광역 전파 I", "locked", "3개 항목", "—", "복수 계열의 전파 경로를 통합해 광역 확산 능력을 얻는다."),
                    new DummyResearchItem("전지구적 전파", "locked", "광역 전파 I", "—", "국경을 초월한 전지구적 확산 능력을 얻는다."),
                }),
            },
            UpgradeCategory.Symptom => new List<DummyBranch>
            {
                new DummyBranch("표준형 — 기침 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("기침", "maxed", null, "완료", "감염자에게 기침 증상이 나타나 비말 전파의 토대가 된다."),
                    new DummyResearchItem("발열", "active", null, "14 DNA", "체온 상승으로 중증도가 소폭 상승한다."),
                    new DummyResearchItem("폐렴", "available", null, "20 DNA", "호흡기 증상이 악화되어 중증도·치사율이 함께 상승한다."),
                    new DummyResearchItem("호흡 부전", "locked", "폐렴", "—", "호흡 기능이 저하되어 치사율이 크게 상승한다."),
                }),
                new DummyBranch("은신형 — 발진 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("발진", "available", null, "14 DNA", "피부에 가벼운 발진이 나타난다."),
                    new DummyResearchItem("피부 병변", "locked", "발진", "—", "발진이 병변으로 진행되어 중증도가 상승한다."),
                    new DummyResearchItem("피부염", "locked", "피부 병변", "—", "만성적인 피부염 증상으로 진행된다."),
                    new DummyResearchItem("만성 병변", "locked", "피부염", "—", "병변이 만성화되어 치사율이 소폭 상승한다."),
                }),
                new DummyBranch("공격형 — 구토 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("구토감", "available", null, "14 DNA", "가벼운 구토감 증상이 나타난다."),
                    new DummyResearchItem("구토", "locked", "구토감", "—", "실제 구토 증상으로 진행되어 중증도가 상승한다."),
                    new DummyResearchItem("출혈", "locked", "구토", "—", "내출혈 증상이 동반되어 치사율이 상승한다."),
                    new DummyResearchItem("패혈증", "locked", "출혈", "—", "전신 염증 반응으로 치사율이 크게 상승한다."),
                }),
                new DummyBranch("통합 연구", new List<DummyResearchItem>
                {
                    new DummyResearchItem("다발성 장기부전 I", "locked", "3개 항목", "—", "복수 계열의 중증 증상이 겹쳐 장기 기능이 저하된다."),
                    new DummyResearchItem("전신 장기부전", "locked", "다발성 장기부전 I", "—", "전신의 장기 기능이 상실되어 치사율이 최대치로 상승한다."),
                }),
            },
            UpgradeCategory.Ability => new List<DummyBranch>
            {
                new DummyBranch("변이 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("유전자 변이 I", "maxed", null, "완료", "병원체의 유전자가 변이해 기초 능력치가 상승한다."),
                    new DummyResearchItem("유전자 변이 II", "active", null, "18 DNA", "추가 변이로 기초 능력치가 더 상승한다."),
                    new DummyResearchItem("약물 저항 I", "available", null, "20 DNA", "치료제 개발 속도를 늦추는 약물 저항력을 얻는다."),
                    new DummyResearchItem("약물 저항 II", "locked", "약물 저항 I", "—", "약물 저항력이 강화되어 치료제 진행이 더 느려진다."),
                }),
                new DummyBranch("은신 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("은신 I", "available", null, "16 DNA", "증상을 억제해 발견 확률을 낮춘다."),
                    new DummyResearchItem("은신 II", "locked", "은신 I", "—", "은신 능력이 강화되어 발견 확률이 더 낮아진다."),
                    new DummyResearchItem("위장 I", "locked", "은신 II", "—", "검사 회피 능력을 얻는다."),
                    new DummyResearchItem("위장 II", "locked", "위장 I", "—", "위장 능력이 강화된다."),
                }),
                new DummyBranch("구조 강화 계열", new List<DummyResearchItem>
                {
                    new DummyResearchItem("구조 강화 I", "available", null, "16 DNA", "병원체 구조를 강화해 생존력을 높인다."),
                    new DummyResearchItem("구조 강화 II", "locked", "구조 강화 I", "—", "구조 강화가 추가로 진행된다."),
                    new DummyResearchItem("구조적 내성 I", "locked", "구조 강화 II", "—", "구조적 내성을 얻어 약물 저항력이 상승한다."),
                    new DummyResearchItem("구조적 내성 II", "locked", "구조적 내성 I", "—", "구조적 내성이 강화된다."),
                }),
                new DummyBranch("통합 연구", new List<DummyResearchItem>
                {
                    new DummyResearchItem("슈퍼균주 I", "locked", "3개 항목", "—", "세 계열의 적응 능력을 통합해 슈퍼균주로 진화한다."),
                    new DummyResearchItem("최종 진화체", "locked", "슈퍼균주 I", "—", "병원체가 최종 진화체로 완성된다."),
                }),
            },
            _ => new List<DummyBranch>(),
        };
    }
}
