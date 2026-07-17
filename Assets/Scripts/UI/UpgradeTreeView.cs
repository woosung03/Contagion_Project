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
    /// Research Database 화면 (Research Database v2 커밋 6 — "연구 항목 행 클릭 이벤트 배선").
    /// 참고: Docs/ResearchDatabase_V2_ImplementationPlan.md §1 Step 8~14, §4 커밋 5~6.
    ///
    /// 커밋 5까지 더미 데이터 경로를 완전히 제거하고, <see cref="UpgradeManager.Instance"/>.Tree의
    /// 실제 45개 <see cref="UpgradeNode"/>를 <see cref="NodeBranch"/> 기준으로 그룹핑해 그리는
    /// 단계까지 마쳤다. 이번 커밋에서는 연구 항목 행 클릭 시 <see cref="OnResearchItemSelected"/>
    /// 이벤트를 발행하도록 배선했다.
    ///
    /// 이번 커밋 범위 밖(다음 커밋으로 이관): <c>UIManager</c>가 이 이벤트를 구독해
    /// <c>ResearchPopupController.Show()</c>를 호출하는 배선(V2 계획 커밋 7) — 그래서 아직
    /// <c>UIManager</c>가 구독하지 않는 한 연구 항목 행을 클릭해도 화면상 변화는 없다(컴파일만
    /// 통과하는 안전한 중간 상태). "연구 시작" 구매 로직(<see cref="UpgradeManager.TryUnlock"/>)
    /// 연결, 잠금 사유 텍스트 헬퍼(<c>LockReason()</c>, V2 계획 커밋 8)도 이후 커밋 범위.
    /// 하단 상세 패널(<c>detail-panel</c>)은 개별 항목이 아니라 "선택된 브랜치" 요약(진행률 +
    /// 다음 추천 연구)을 보여준다 — V2 계획 §1 Step 12.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeTreeView : MonoBehaviour
    {
        /// <summary>
        /// node.id(영문 내부 식별자) → 한국어 표시명. 이번 커밋부터 <see cref="DisplayName"/>을 통해
        /// 실제 연구 항목 행 렌더링에 쓰인다. 값은 Docs/UpgradeTree_ResearchDatabase_NodeMapping.md의
        /// "신규 이름" 열을 그대로 반영했다 — "유지" 분류 노드는 기존 이름 그대로, "수정"/"대체"
        /// 분류 노드는 신규 이름으로 교체했을 뿐, 효과·비용·선행조건(DefaultUpgradeTreeFactory.cs)은
        /// 1개도 바꾸지 않는다.
        /// </summary>
        private static readonly Dictionary<string, string> NodeDisplayNames = new Dictionary<string, string>
        {
            // 감염 경로 — 공기 계열
            { "trans_air1", "비말 핵 잔류" },
            { "trans_air2", "에어로졸 광역 부유" },
            { "trans_droplet1", "호흡기 상재균 교란" },
            { "trans_droplet2", "실내 공기 재순환 감염" },
            // 감염 경로 — 수인성 계열
            { "trans_water1", "수인성 전파" },
            { "trans_water2", "해상 전파" },
            { "trans_animal1", "인수공통 전파" },
            { "trans_animal2", "숙주 전이 가속" },
            // 감염 경로 — 접촉·동물매개 계열
            { "trans_contact1", "조류 매개 전파" },
            { "trans_insect1", "설치류 매개 확산" },
            { "trans_blood1", "수혈 전파" },
            { "trans_blood2", "오염 혈액 유통망" },
            // 감염 경로 — 통합 연구
            { "trans_advanced1", "교차 매개 네트워크" },
            { "trans_advanced2", "혈액-매개체 융합 전파" },
            { "trans_global", "전지구적 전파망" },
            // 증상 — 표준형(기침 계열)
            { "sym_cough", "기침" },
            { "sym_fever", "발열" },
            { "sym_pneumonia", "폐렴" },
            { "sym_respfailure", "호흡 부전" },
            // 증상 — 은신형(발진 계열)
            { "sym_rash", "발진" },
            { "sym_lesion", "피부 병변" },
            { "sym_dermatitis", "피부염" },
            { "sym_necrosis", "만성 병변" },
            // 증상 — 공격형(구토 계열)
            { "sym_nausea", "구토감" },
            { "sym_vomit", "구토" },
            { "sym_hemorrhage", "출혈" },
            { "sym_sepsis", "패혈증" },
            // 증상 — 통합 연구
            { "sym_multiorgan1", "다발성 장기부전 I" },
            { "sym_multiorgan2", "다발성 장기부전 II" },
            { "sym_organfailure", "전신 장기부전" },
            // 적응(구 "능력") — 변이 계열
            { "abl_mutation1", "항원 변이" },
            { "abl_mutation2", "잠복 변이" },
            { "abl_resist1", "백신 회피 항체 조작" },
            { "abl_resist3", "다중 변종 분화" },
            // 적응 — 은신 계열
            { "abl_stealth1", "면역 회피 단백질" },
            { "abl_stealth2", "검사 회피" },
            { "abl_camouflage1", "숙주 내 잠행" },
            { "abl_camouflage2", "면역계 잠입" },
            // 적응 — 구조 강화 계열
            { "abl_hardening1", "약물 내성" },
            { "abl_hardening2", "세포벽 강화" },
            { "abl_resist2", "격리 내성" },
            { "abl_resist4", "내한성" },
            // 적응 — 통합 연구
            { "abl_superbug1", "다제내성 병원체" },
            { "abl_superbug2", "전천후 적응" },
            { "abl_finalevo", "최종 진화" },
        };

        /// <summary>
        /// node.id → 상세 설명 한 줄. Research Popup(ResearchPopupController.Show())이 연결되는
        /// 이후 커밋(V2 계획 커밋 6~7, 이번 커밋 범위 밖)에서 실제로 참조될 예정 — 이번 커밋의
        /// 하단 상세 패널은 개별 항목 설명이 아니라 브랜치 요약만 보여주므로 이번 커밋에서도 아직
        /// 미사용이다(안전한 중간 지점, 빌드/동작에 영향 없음).
        /// 문구는 ResearchDatabase_MVP_ImplementationPlan.md §0.1 "서술-효과 불일치 방지 원칙"을
        /// 따른다 — "이미 로직이 존재하는" 노드(abl_hardening1 — drugResistanceReduction 공식으로
        /// 치료제 진행을 이미 저해하는 로직이 존재)만 규칙 문장을 포함하고, 나머지(전부 Phase
        /// 2~4 미착수 메커닉 — environmentResistance 소비/medicalBurdenModifier/unlockedFlags
        /// 의존)는 "정체성 서사"까지만 쓰고 "지금 이 연구가 실제로 무엇을 바꾸는지"에 대한 구체적
        /// 규칙 문장은 넣지 않는다. 스탯 변화(infectivity/severity/lethality/drugResistance
        /// 증감)는 이 딕셔너리가 아니라 기존처럼 effects 기반 data-row로 노출한다.
        /// </summary>
        private static readonly Dictionary<string, string> NodeDescriptions = new Dictionary<string, string>
        {
            // 감염 경로 — 공기 계열
            { "trans_air1", "미세 비말이 실내 공기 중에 핵 형태로 남아 잔류 감염을 일으킨다." },
            { "trans_air2", "병원체가 초미세 에어로졸로 변해 더 넓은 공간까지 퍼져나간다." },
            { "trans_droplet1", "호흡기 상주균총을 교란시켜 비말을 통한 근접 감염을 강화한다." },
            { "trans_droplet2", "환기 시설을 타고 실내 공기가 재순환되며 밀집 공간의 감염이 늘어난다." },
            // 감염 경로 — 수인성 계열
            { "trans_water1", "오염된 식수를 통해 병원체가 퍼진다." },
            { "trans_water2", "선박 급수 시설과 해상 물류를 타고 병원체가 항구 사이를 이동한다." },
            { "trans_animal1", "가축·야생동물을 매개로 한 전파 경로가 열린다." },
            { "trans_animal2", "동물 숙주 사이의 전이 속도가 빨라져 재유입 감염이 잦아진다." },
            // 감염 경로 — 접촉·동물매개 계열
            { "trans_contact1", "철새의 계절 이동 경로를 따라 병원체가 퍼진다." },
            { "trans_insect1", "쥐 등 설치류를 매개로 병원체가 은밀히 확산된다." },
            { "trans_blood1", "오염된 혈액이 수혈 과정을 통해 새로운 숙주에게 전달된다." },
            { "trans_blood2", "혈액 제제 유통망이 오염되어 전파 경로가 더 넓어진다." },
            // 감염 경로 — 통합 연구
            { "trans_advanced1", "여러 전파 경로가 교차하며 하나의 통합된 감염망을 이룬다." },
            { "trans_advanced2", "혈액 전파와 매개체 전파가 융합되어 새로운 감염 양상을 만든다." },
            { "trans_global", "국경을 초월한 전지구적 확산 능력을 얻는다." },
            // 증상 — 표준형(기침 계열)
            { "sym_cough", "감염자에게 기침 증상이 나타나 비말 전파의 토대가 된다." },
            { "sym_fever", "체온이 상승하며 병세의 초반 진행이 완만해진다." },
            { "sym_pneumonia", "호흡기 증상이 악화되어 병세가 급격히 진행된다." },
            { "sym_respfailure", "호흡 기능이 저하되어 치사율이 크게 상승한다." },
            // 증상 — 은신형(발진 계열)
            { "sym_rash", "피부에 가벼운 발진이 나타나 초기에는 눈에 띄지 않는다." },
            { "sym_lesion", "발진이 병변으로 진행되며 좀처럼 발각되지 않는다." },
            { "sym_dermatitis", "만성적인 피부염 증상으로 진행되어 대응이 더디다." },
            { "sym_necrosis", "병변이 만성화되어 급성기 없이 서서히 악화된다." },
            // 증상 — 공격형(구토 계열)
            { "sym_nausea", "가벼운 구토감 증상이 나타난다." },
            { "sym_vomit", "실제 구토 증상으로 진행되어 중증도가 상승한다." },
            { "sym_hemorrhage", "내출혈 증상이 동반되어 감염자들이 스스로 몸을 사리게 된다." },
            { "sym_sepsis", "전신 염증 반응으로 치사율이 크게 상승한다." },
            // 증상 — 통합 연구
            { "sym_multiorgan1", "복수 계열의 중증 증상이 겹쳐 장기 기능이 저하된다." },
            { "sym_multiorgan2", "장기부전이 더욱 심화되어 치사율이 큰 폭으로 상승한다." },
            { "sym_organfailure", "전신의 장기 기능이 상실되어 치사율이 최대치로 상승한다." },
            // 적응 — 변이 계열
            { "abl_mutation1", "표면 항원이 끊임없이 변이해 치료제 개발을 근본적으로 어렵게 한다." },
            { "abl_mutation2", "잠복기 동안 변이가 축적되어 발각도가 낮아진다." },
            { "abl_resist1", "항체 반응을 무력화하는 방향으로 표면 구조를 조작한다." },
            { "abl_resist3", "여러 변종으로 분화해 치료제 완성 이후에도 일부가 살아남는다." },
            // 적응 — 은신 계열
            { "abl_stealth1", "면역계의 인식을 피하는 단백질을 발현해 감염자가 스스로 낫기 어렵게 만든다." },
            { "abl_stealth2", "진단 검사를 피해가는 성질을 얻어 발각도를 낮춘다." },
            { "abl_camouflage1", "숙주 체내에 조용히 자리잡아 대응 체계의 경계를 늦춘다." },
            { "abl_camouflage2", "면역계 깊숙이 잠입해 발각도를 최대한 억제한다." },
            // 적응 — 구조 강화 계열
            { "abl_hardening1", "병원체 구조를 조정해 치료제의 효력을 떨어뜨린다." },
            { "abl_hardening2", "세포벽을 강화해 구조적 생존력을 높인다." },
            { "abl_resist2", "격리 상황에서도 버텨내는 구조적 내성을 얻는다." },
            { "abl_resist4", "추운 환경에서도 버텨내는 내한성을 얻는다." },
            // 적응 — 통합 연구
            { "abl_superbug1", "여러 계열의 적응 능력을 통합해 다제내성을 지닌 균주로 진화한다." },
            { "abl_superbug2", "기후에 관계없이 버텨내는 전천후 적응력을 갖춘다." },
            { "abl_finalevo", "병원체가 최종 진화체로 완성된다." },
        };

        /// <summary>
        /// node.id → 소속 갈래(브랜치) 라벨. GetBranch()를 통해 ResearchPopup이 조회하는 데
        /// 쓰인다(트리 캔버스 승격 이후 자체 그룹핑 용도의 소비처는 사라짐). 4브랜치 구조(기반
        /// 갈래 3개 + 통합 갈래 1개)는
        /// DefaultUpgradeTreeFactory.cs의 prerequisites 그래프가 실제로 강제하는 구조다(카테고리당
        /// 15개 = 4+4+4+3, ResearchDatabase_V2_UI_StructureDesign.md §0이 이미 확인). 라벨
        /// 문자열 자체는 기존 더미 데이터가 이미 쓰던 값(공기 계열/수인성 계열/접촉·동물매개
        /// 계열/통합 연구 등)을 그대로 재사용해 화면 문구가 갑자기 바뀌는 위화감이 없게 했다.
        /// </summary>
        private static readonly Dictionary<string, string> NodeBranch = new Dictionary<string, string>
        {
            // 감염 경로
            { "trans_air1", "공기 계열" },
            { "trans_air2", "공기 계열" },
            { "trans_droplet1", "공기 계열" },
            { "trans_droplet2", "공기 계열" },
            { "trans_water1", "수인성 계열" },
            { "trans_water2", "수인성 계열" },
            { "trans_animal1", "수인성 계열" },
            { "trans_animal2", "수인성 계열" },
            { "trans_contact1", "접촉·동물매개 계열" },
            { "trans_insect1", "접촉·동물매개 계열" },
            { "trans_blood1", "접촉·동물매개 계열" },
            { "trans_blood2", "접촉·동물매개 계열" },
            { "trans_advanced1", "통합 연구" },
            { "trans_advanced2", "통합 연구" },
            { "trans_global", "통합 연구" },
            // 증상
            { "sym_cough", "표준형 — 기침 계열" },
            { "sym_fever", "표준형 — 기침 계열" },
            { "sym_pneumonia", "표준형 — 기침 계열" },
            { "sym_respfailure", "표준형 — 기침 계열" },
            { "sym_rash", "은신형 — 발진 계열" },
            { "sym_lesion", "은신형 — 발진 계열" },
            { "sym_dermatitis", "은신형 — 발진 계열" },
            { "sym_necrosis", "은신형 — 발진 계열" },
            { "sym_nausea", "공격형 — 구토 계열" },
            { "sym_vomit", "공격형 — 구토 계열" },
            { "sym_hemorrhage", "공격형 — 구토 계열" },
            { "sym_sepsis", "공격형 — 구토 계열" },
            { "sym_multiorgan1", "통합 연구" },
            { "sym_multiorgan2", "통합 연구" },
            { "sym_organfailure", "통합 연구" },
            // 적응(구 "능력")
            { "abl_mutation1", "변이 계열" },
            { "abl_mutation2", "변이 계열" },
            { "abl_resist1", "변이 계열" },
            { "abl_resist3", "변이 계열" },
            { "abl_stealth1", "은신 계열" },
            { "abl_stealth2", "은신 계열" },
            { "abl_camouflage1", "은신 계열" },
            { "abl_camouflage2", "은신 계열" },
            { "abl_hardening1", "구조 강화 계열" },
            { "abl_hardening2", "구조 강화 계열" },
            { "abl_resist2", "구조 강화 계열" },
            { "abl_resist4", "구조 강화 계열" },
            { "abl_superbug1", "통합 연구" },
            { "abl_superbug2", "통합 연구" },
            { "abl_finalevo", "통합 연구" },
        };

        /// <summary>node.id → 한국어 표시명.</summary>
        private static string DisplayName(string id) =>
            NodeDisplayNames.TryGetValue(id, out var name) ? name : id;

        /// <summary>node.id → 한국어 표시명 (외부 공개용, V2 계획 커밋 7 — UIManager가
        /// ResearchPopupController.Show()를 채우는 데 사용). <see cref="DisplayName"/>의 얇은
        /// 공개 래퍼일 뿐, 조회 로직 자체는 바뀌지 않는다.</summary>
        public static string GetDisplayName(string id) => DisplayName(id);

        /// <summary>node.id → 상세 설명 한 줄 (외부 공개용, V2 계획 커밋 7). 매핑에 없으면 빈 문자열.</summary>
        public static string GetDescription(string id) =>
            NodeDescriptions.TryGetValue(id, out var desc) ? desc : string.Empty;

        /// <summary>node.id → 소속 갈래(브랜치) 라벨 (외부 공개용, V2 계획 커밋 7). 매핑에 없으면 빈 문자열.</summary>
        public static string GetBranch(string id) =>
            NodeBranch.TryGetValue(id, out var branch) ? branch : string.Empty;

        [SerializeField, Tooltip("이 창이 담당할 업그레이드 카테고리 — 이 창은 이 카테고리 노드만 그린다.")]
        private UpgradeCategory category = UpgradeCategory.Transmission;

        /// <summary>UIManager가 탭 클릭 시 어느 카테고리 화면을 보여줘야 할지 판단하는 데 쓴다.</summary>
        public UpgradeCategory Category => category;

        private VisualElement _upgradeRoot;
        private Label _dnaLabel;
        private Label _labCaptionLabel;
        private ScrollView _nodeScroll;
        private Button _closeButton;
        private Button _adBonusButton;
        private Button _tabTransmissionButton;
        private Button _tabSymptomButton;
        private Button _tabAbilityButton;

        // ================================================================
        // 트리 캔버스(Painter2D 절대좌표 트리) — node-scroll의 실제 콘텐츠로 렌더링한다.
        // branch-board(Commit 2)/research-row(Commit 3A)/detail-panel(Commit 3B) 전부 제거되어
        // 이 화면 안에는 캔버스만 존재한다.
        // ================================================================

        private TreePathElement _treePathElement;

        /// <summary>탭 클릭 — 다른 카테고리를 요청하면 발생. 실제 화면 전환(다른 UIDocument
        /// Show/Hide)은 UIManager가 담당한다.</summary>
        public event System.Action<UpgradeCategory> OnCategoryRequested;

        /// <summary>연구 항목 행 클릭 — 해당 <see cref="UpgradeNode"/>와 함께 발생(V2 계획 커밋 6).
        /// 상세 팝업(ResearchPopupController.Show())을 실제로 여는 구독은 UIManager가 담당한다
        /// (V2 계획 커밋 7, 이 클래스는 발행만 하고 구독자가 있는지는 신경 쓰지 않는다).</summary>
        public event System.Action<UpgradeNode> OnResearchItemSelected;

        /// <summary>닫기(X) 버튼 클릭 — 이 화면 스스로 Hide()를 호출하지 않고 요청만 발행한다.
        /// 실제로 화면을 닫고(TransitionTo(AppScreen.Gameplay)) WorldMapInputLock을 해제하는 책임은
        /// UIManager 한 곳에만 있다 — HUD 버튼 경로(TransitionTo)와 X 버튼 경로가 서로 다른 코드를
        /// 타면서 Unlock() 호출이 누락되던 버그(WorldMap Input Lock 영구 유지)의 근본 수정.</summary>
        public event System.Action OnCloseRequested;

        [SerializeField, Tooltip("광고 시청 보상 DNA — 설계 문서 13절 표 1행")]
        private int adBonusDna = 10;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _upgradeRoot = root.Q<VisualElement>("upgrade-root");
            _dnaLabel = root.Q<Label>("dna-label");
            _labCaptionLabel = root.Q<Label>("lab-caption-label");
            _nodeScroll = root.Q<ScrollView>("node-scroll");
            _closeButton = root.Q<Button>("close-button");
            _adBonusButton = root.Q<Button>("ad-bonus-button");
            _tabTransmissionButton = root.Q<Button>("tab-transmission-button");
            _tabSymptomButton = root.Q<Button>("tab-symptom-button");
            _tabAbilityButton = root.Q<Button>("tab-ability-button");

            _nodeScroll.mode = ScrollViewMode.Vertical;

            _closeButton.RegisterCallback<ClickEvent>(_ => OnCloseRequested?.Invoke());
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
            UpgradeManager.Instance.OnNodeUnlocked -= HandleNodeUnlockedForCanvas;
            UpgradeManager.Instance.OnNodeUnlocked += HandleNodeUnlockedForCanvas;
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance == null) return;
            UpgradeManager.Instance.OnDnaChanged -= HandleDnaChanged;
            UpgradeManager.Instance.OnNodeUnlocked -= HandleNodeUnlockedForCanvas;
        }

        /// <summary>구매 직후 캔버스 경로가 즉시 밝아지도록 갱신한다(Commit 1부터 항상 실행).</summary>
        private void HandleNodeUnlockedForCanvas(UpgradeNode node)
        {
            BuildTreeCanvas();
        }

        /// <summary>HUD "업그레이드" 버튼 또는 탭 클릭에서 호출 — 이 창은 인스펙터에 지정된
        /// <see cref="category"/> 하나만 그린다. detail-panel 제거(Commit 3B) 이후로는 캔버스만
        /// 다시 그리면 된다.</summary>
        public void Show()
        {
            _upgradeRoot.style.display = DisplayStyle.Flex;
            UpdateTabHighlight();
            RefreshDna();

            BuildTreeCanvas();
        }

        public void Hide()
        {
            if (_upgradeRoot != null) _upgradeRoot.style.display = DisplayStyle.None;
        }

        // ================================================================
        // 트리 캔버스(Painter2D 절대좌표 트리) — node-scroll.contentContainer를 직접 채운다.
        // research-row 리스트(BuildResearchList 등)는 Commit 3A에서 완전히 제거됨 — 이제 이
        // 컨테이너의 유일한 콘텐츠 소스다.
        // ================================================================

        /// <summary>node.position(월드 좌표, DefaultUpgradeTreeFactory.cs)을 이 카테고리 노드들의
        /// 최소값 기준으로 정규화해 캔버스 로컬 좌표로 그린다 — 카테고리별 x 오프셋(예: 증상=640)을
        /// 하드코딩하지 않고 런타임에 계산해서, factory 데이터가 나중에 바뀌어도 매직넘버가 깨지지
        /// 않게 한다. LAB 캡션 갱신도 여기서 겸한다 — 기존에 BuildResearchList()가 맡던 역할인데,
        /// 그 메서드가 제거되면서 카테고리별 콘텐츠를 그리는 이 메서드로 옮겼다(캡션 로직 자체는
        /// 무변경).</summary>
        private void BuildTreeCanvas()
        {
            if (_nodeScroll == null || UpgradeManager.Instance == null) return;

            if (_labCaptionLabel != null)
                _labCaptionLabel.text = $"{CategoryEnglishCaption(category)} LAB";

            var categoryNodes = UpgradeManager.Instance.Tree
                .Where(n => n != null && n.category == category)
                .ToList();
            if (categoryNodes.Count == 0) return;

            const float NodeWidth = 110f;
            const float NodeHeight = 50f;
            const float Margin = 60f;

            float minX = categoryNodes.Min(n => n.position.x);
            float minY = categoryNodes.Min(n => n.position.y);
            float maxX = categoryNodes.Max(n => n.position.x);
            float maxY = categoryNodes.Max(n => n.position.y);

            float canvasWidth = (maxX - minX) + NodeWidth + Margin * 2f;
            float canvasHeight = (maxY - minY) + NodeHeight + Margin * 2f;

            Vector2 ToLocal(UpgradeNode n) => new Vector2(
                n.position.x - minX + Margin,
                n.position.y - minY + Margin);

            var container = _nodeScroll.contentContainer;
            container.Clear();
            container.style.position = Position.Relative;
            container.style.width = canvasWidth;
            container.style.height = canvasHeight;

            _treePathElement = new TreePathElement
            {
                style = { width = canvasWidth, height = canvasHeight }
            };
            container.Add(_treePathElement);
            _treePathElement.SetSegments(BuildLineSegments(categoryNodes, ToLocal));

            foreach (var node in categoryNodes)
                container.Add(CreateTreeNode(node, ToLocal(node), NodeWidth, NodeHeight));
        }

        /// <summary>prerequisites 그래프를 그대로 선분으로 변환한다 — 일반 체인이든 합류 지점(선행
        /// 2개)이든 "선행 -> 자신" 한 쌍이 곧 선분 하나이므로 별도 분기 없이 전 노드를 한 번만
        /// 순회한다. 목적지 노드가 해금됐으면 밝고 굵게, 아니면 흐리고 얇게(Round 1~4 프로토타입과
        /// 동일한 시각 규칙).</summary>
        private List<TreePathElement.LineSegment> BuildLineSegments(
            List<UpgradeNode> categoryNodes, System.Func<UpgradeNode, Vector2> toLocal)
        {
            var segments = new List<TreePathElement.LineSegment>();
            var lookup = categoryNodes.ToDictionary(n => n.id, n => n);

            var brightColor = new Color(0.588f, 1f, 0.725f, 0.9f); // --color-accent-glow
            var dimColor = new Color(0.588f, 1f, 0.725f, 0.28f);

            foreach (var node in categoryNodes)
            {
                foreach (var prereqId in node.prerequisites)
                {
                    if (!lookup.TryGetValue(prereqId, out var prereq)) continue;

                    bool bright = node.isUnlocked;
                    segments.Add(new TreePathElement.LineSegment(
                        toLocal(prereq), toLocal(node),
                        bright ? brightColor : dimColor,
                        bright ? 6f : 3f));
                }
            }

            return segments;
        }

        /// <summary>캔버스 노드 하나 — 즉석 조립한 VisualElement를 반환한다(별도 노드 클래스
        /// 없음). 상태 판정은 기존 DetermineState()를 그대로 재사용(읽기 전용 호출, 수정 없음).</summary>
        private VisualElement CreateTreeNode(UpgradeNode node, Vector2 localPos, float width, float height)
        {
            string state = DetermineState(node);

            var el = new VisualElement();
            el.AddToClassList("tree-node");
            el.AddToClassList($"tree-node--{state}");
            if (IsMergeNode(node)) el.AddToClassList("tree-node--merge");
            if (IsFinalNode(node)) el.AddToClassList("tree-node--final");

            el.style.left = localPos.x - width / 2f;
            el.style.top = localPos.y - height / 2f;
            el.style.width = width;
            el.style.height = height;

            var label = new Label(DisplayName(node.id));
            label.AddToClassList("tree-node__label");
            el.Add(label);

            el.RegisterCallback<ClickEvent>(_ => OnResearchItemSelected?.Invoke(node));

            return el;
        }

        /// <summary>합류 노드 = 선행이 2개 이상이고 리프가 아님(리프까지 포함하면 sym_organfailure류
        /// 최종 노드도 선행 2개라 합류로 오분류된다 — 반드시 IsFinalNode와 배타적으로 판정).</summary>
        private static bool IsMergeNode(UpgradeNode node) =>
            node.prerequisites.Count > 1 && !IsFinalNode(node);

        /// <summary>최종 진화 = 이 노드를 선행으로 삼는 다른 노드가 하나도 없음(리프).
        /// DetermineState()가 이미 같은 계산을 하고 있지만(로컬 변수), 이번 범위는 기존 메서드를
        /// 수정하지 않고 순수 추가만 하기로 했으므로 별도 헬퍼로 중복 정의한다.</summary>
        private static bool IsFinalNode(UpgradeNode node) =>
            !UpgradeManager.Instance.Tree.Any(n => n.prerequisites.Contains(node.id));

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

        /// <summary>UpgradeNode 기반 상태 판정. isUnlocked==true면 이 노드를 선행조건으로 삼는
        /// 다른 노드가 있는지로 active(중간 티어)/maxed(말단) 구분, isUnlocked==false면 선행조건
        /// 충족 여부로 available/locked 구분.</summary>
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

        /// <summary>node.effects의 statName -> 상세 패널 표시 라벨. 개별 항목 상세(Research Popup)
        /// 연결 시 재사용 예정 — 이번 커밋(브랜치 요약 단계)에서는 미사용.</summary>
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

        /// <summary>탭 라벨(짧은 한글) — 전파 탭만 축약형("감염 경로" 대신 "전파")이고 나머지는
        /// 그대로.</summary>
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
    }
}
