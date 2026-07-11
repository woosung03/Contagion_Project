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
        /// node.id → 소속 갈래(브랜치) 라벨. 이번 커밋부터 <see cref="BuildBranches"/>가 실제
        /// 그룹핑 기준으로 사용한다. 4브랜치 구조(기반 갈래 3개 + 통합 갈래 1개)는
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

        /// <summary>
        /// 카테고리별 브랜치 표시 순서(브랜치 보드 4행 + 리스트 섹션 순서). NodeBranch 딕셔너리의
        /// 값 자체는 순서를 보장하지 않으므로(Dictionary 순회 순서에 의존하지 않기 위해) 별도로
        /// 명시한다. 값은 기존 더미 데이터(BuildDummyBranches, 이번 커밋에서 제거)가 쓰던 순서와
        /// 동일 — 계열 3개(각 4개 노드) + 통합 연구(3개 노드) 순.
        /// </summary>
        private static readonly Dictionary<UpgradeCategory, string[]> BranchOrder = new Dictionary<UpgradeCategory, string[]>
        {
            { UpgradeCategory.Transmission, new[] { "공기 계열", "수인성 계열", "접촉·동물매개 계열", "통합 연구" } },
            { UpgradeCategory.Symptom, new[] { "표준형 — 기침 계열", "은신형 — 발진 계열", "공격형 — 구토 계열", "통합 연구" } },
            { UpgradeCategory.Ability, new[] { "변이 계열", "은신 계열", "구조 강화 계열", "통합 연구" } },
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
        private VisualElement _branchBoardRows;
        private Label _detailTitle;
        private VisualElement _detailRows;
        private Button _buyButton;
        private Button _closeButton;
        private Button _adBonusButton;
        private Button _tabTransmissionButton;
        private Button _tabSymptomButton;
        private Button _tabAbilityButton;

        /// <summary>node.id → 표시용 코드(예: "TRANS-001"). <see cref="AssignCodes"/>가 카테고리
        /// 전체(선택된 브랜치와 무관하게)를 브랜치 순서대로 훑으며 채운다 — 브랜치를 옮겨 다녀도
        /// 코드가 흔들리지 않도록 하기 위함.</summary>
        private readonly Dictionary<string, string> _codeByNodeId = new Dictionary<string, string>();

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

        /// <summary>이 카테고리의 4개 브랜치(계열 3 + 통합 연구 1). Show()/RequestCategory 시점마다
        /// UpgradeManager.Tree로부터 다시 계산한다.</summary>
        private List<ResearchBranch> _branches = new List<ResearchBranch>();

        /// <summary>현재 브랜치 보드/리스트/하단 요약이 가리키는 브랜치.</summary>
        private ResearchBranch _selectedBranch;

        [SerializeField, Tooltip("광고 시청 보상 DNA — 설계 문서 13절 표 1행")]
        private int adBonusDna = 10;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _upgradeRoot = root.Q<VisualElement>("upgrade-root");
            _dnaLabel = root.Q<Label>("dna-label");
            _labCaptionLabel = root.Q<Label>("lab-caption-label");
            _nodeScroll = root.Q<ScrollView>("node-scroll");
            _branchBoardRows = root.Q<VisualElement>("branch-board__rows");
            _detailTitle = root.Q<Label>("detail-title");
            _detailRows = root.Q<VisualElement>("detail-rows");
            _buyButton = root.Q<Button>("buy-button");
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
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance == null) return;
            UpgradeManager.Instance.OnDnaChanged -= HandleDnaChanged;
        }

        /// <summary>HUD "업그레이드" 버튼 또는 탭 클릭에서 호출 — 이 창은 인스펙터에 지정된
        /// <see cref="category"/> 하나만 그린다. 매번 브랜치 목록을 다시 계산하고 "미완료 항목이
        /// 있는 첫 브랜치"로 선택을 초기화한다(V2 계획 §5 기본값 — 마지막 선택 기억은 이번 범위
        /// 밖).</summary>
        public void Show()
        {
            _upgradeRoot.style.display = DisplayStyle.Flex;
            UpdateTabHighlight();
            _branches = BuildBranches(category);
            AssignCodes(_branches);
            SelectBranch(FirstIncompleteBranch(_branches));
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

        // ================================================================
        // 브랜치 데이터 — UpgradeManager.Tree(실제 45개 UpgradeNode)를 NodeBranch 기준으로
        // 그룹핑한다. Docs/ResearchDatabase_V2_ImplementationPlan.md §1 Step 8.
        // ================================================================

        private class ResearchBranch
        {
            public readonly string Label;
            public readonly List<UpgradeNode> Items;

            public ResearchBranch(string label, List<UpgradeNode> items)
            {
                Label = label;
                Items = items;
            }
        }

        /// <summary>UpgradeManager.Tree에서 이 카테고리 노드만 추려 <see cref="NodeBranch"/> 기준으로
        /// <see cref="BranchOrder"/> 순서대로 4개 브랜치를 만든다. 각 브랜치 내부는 position.y(티어)
        /// → position.x(열) 순으로 정렬 — DefaultUpgradeTreeFactory.cs가 이미 이 좌표로 티어/열을
        /// 인코딩해뒀으므로 별도 깊이 계산이 필요 없다.</summary>
        private static List<ResearchBranch> BuildBranches(UpgradeCategory targetCategory)
        {
            var branches = new List<ResearchBranch>();
            if (UpgradeManager.Instance == null) return branches;

            var labels = BranchOrder.TryGetValue(targetCategory, out var order) ? order : System.Array.Empty<string>();
            var categoryNodes = UpgradeManager.Instance.Tree.Where(n => n != null && n.category == targetCategory).ToList();

            foreach (var label in labels)
            {
                var items = categoryNodes
                    .Where(n => NodeBranch.TryGetValue(n.id, out var branchLabel) && branchLabel == label)
                    .OrderBy(n => n.position.y)
                    .ThenBy(n => n.position.x)
                    .ToList();
                branches.Add(new ResearchBranch(label, items));
            }
            return branches;
        }

        /// <summary>브랜치 순서대로 전체 카테고리를 훑으며 _codeByNodeId를 채운다 — 어느 브랜치가
        /// 선택돼 있는지와 무관하게 코드가 고정되도록 브랜치를 옮길 때마다 다시 계산하지 않고
        /// Show()/RequestCategory 시점(카테고리 전체 재계산 시점)에만 갱신한다.</summary>
        private void AssignCodes(List<ResearchBranch> branches)
        {
            _codeByNodeId.Clear();
            int order = 0;
            foreach (var branch in branches)
            {
                foreach (var node in branch.Items)
                {
                    order++;
                    _codeByNodeId[node.id] = $"{CategoryPrefix(category)}-{order:000}";
                }
            }
        }

        /// <summary>기본 선택 브랜치 — "미완료 항목이 있는 첫 브랜치", 전부 완료됐으면 첫 브랜치.
        /// V2 계획 §1 Step 10.</summary>
        private static ResearchBranch FirstIncompleteBranch(List<ResearchBranch> branches)
        {
            if (branches.Count == 0) return null;
            return branches.FirstOrDefault(b => b.Items.Any(n => !n.isUnlocked)) ?? branches[0];
        }

        /// <summary>브랜치 보드/리스트/하단 요약을 모두 선택된 브랜치 기준으로 다시 그린다.</summary>
        private void SelectBranch(ResearchBranch branch)
        {
            _selectedBranch = branch;
            BuildBranchBoard();
            BuildResearchList();
            BuildBranchSummary();
        }

        // ================================================================
        // 브랜치 보드 — branch-board__rows(UpgradeTree.uxml, 커밋 1에서 이미 추가된 빈 컨테이너)를
        // 채운다. V2 계획 §1 Step 9.
        // ================================================================

        private void BuildBranchBoard()
        {
            if (_branchBoardRows == null) return;
            _branchBoardRows.Clear();
            foreach (var branch in _branches)
                _branchBoardRows.Add(CreateBranchRow(branch));
        }

        private VisualElement CreateBranchRow(ResearchBranch branch)
        {
            var row = new VisualElement();
            row.AddToClassList("branch-row");

            int total = branch.Items.Count;
            int unlocked = branch.Items.Count(n => n.isUnlocked);
            // "통합 연구" 브랜치처럼 진입 노드의 선행조건이 다른 브랜치에 걸쳐 있는 경우, 아직
            // 아무것도 해금하지 못했고 진입 노드 자체가 잠겨 있으면 브랜치 전체를 잠김으로 표시한다
            // (V2 계획 §1 Step 2 — 실제 해금 가능 여부 판정은 UpgradeManager.CanUnlock()이 담당,
            // 여기서는 표시 문구만 결정).
            bool locked = unlocked == 0 && total > 0 && DetermineState(branch.Items[0]) == "locked";
            if (locked) row.AddToClassList("branch-row--locked");
            if (branch == _selectedBranch) row.AddToClassList("branch-row--selected");

            var label = new Label($"{branch.Label} ({unlocked}/{total})");
            label.AddToClassList("branch-row__label");
            row.Add(label);

            var track = new VisualElement();
            track.AddToClassList("branch-row__progress");

            var fill = new VisualElement();
            fill.AddToClassList("branch-row__progress-fill");
            fill.style.flexGrow = unlocked;
            track.Add(fill);

            // 잔여(미해금) 구간 — population-bar류와 달리 이 트랙은 세그먼트가 2개(해금/잔여)뿐이고
            // 이번 커밋은 UpgradeTreeView.cs만 수정 범위라 UpgradeTree.uss에 새 클래스를 추가하지
            // 않는다. 트랙 자체 배경(.branch-row__progress의 --color-surface-soft)이 빈 구간처럼
            // 보이도록 인라인 스타일로만 비율을 잡는다.
            var remainder = new VisualElement();
            remainder.style.flexBasis = 0;
            remainder.style.flexGrow = Mathf.Max(total - unlocked, 0);
            remainder.style.flexShrink = 0;
            track.Add(remainder);

            row.Add(track);

            row.RegisterCallback<ClickEvent>(_ => SelectBranch(branch));
            return row;
        }

        // ================================================================
        // 연구 목록 — node-scroll 안에 "선택된 브랜치 하나"만 그린다(V2 계획 §1 Step 11, 이전
        // 커밋까지는 카테고리 전체 브랜치를 순서대로 다 그렸다).
        // ================================================================

        private void BuildResearchList()
        {
            var container = _nodeScroll.contentContainer;
            container.Clear();

            if (_labCaptionLabel != null)
                _labCaptionLabel.text = $"{CategoryEnglishCaption(category)} LAB";

            if (_selectedBranch == null) return;

            container.Add(CreateBranchSectionHeader(_selectedBranch.Label));
            foreach (var node in _selectedBranch.Items)
                container.Add(CreateResearchRow(node));
        }

        private VisualElement CreateBranchSectionHeader(string branchLabel)
        {
            var caption = new Label(branchLabel);
            caption.AddToClassList("section-caption");
            caption.AddToClassList($"section-caption--{CategoryClass(category)}");
            return caption;
        }

        /// <summary>연구 항목 행 — 실제 UpgradeNode 기반. 클릭 시 <see cref="OnResearchItemSelected"/>를
        /// 발행한다(V2 계획 커밋 6). LOCKED 상태를 포함해 모든 상태에서 발행한다 — 잠긴 항목도
        /// 상세 팝업에서 선행조건("잠긴 이유")을 확인할 수 있어야 하고, 그 열람 자체를 막을 이유가
        /// 없다(팝업의 "연구 시작" 버튼 활성화 여부만 상태에 따라 갈리며, 이는 팝업 쪽 책임이다).
        /// UIManager가 아직 이 이벤트를 구독하지 않으므로(V2 계획 커밋 7) 지금 탭해도 화면상
        /// 변화는 없다(안전한 중간 상태).</summary>
        private VisualElement CreateResearchRow(UpgradeNode node)
        {
            var row = new VisualElement();
            row.AddToClassList("research-row");
            row.AddToClassList($"research-row--{CategoryClass(category)}");

            string state = DetermineState(node);
            row.AddToClassList($"research-row--{state}");

            var codeLabel = new Label(_codeByNodeId.TryGetValue(node.id, out var code) ? code : node.id);
            codeLabel.AddToClassList("research-row__code");
            row.Add(codeLabel);

            var nameLabel = new Label(DisplayName(node.id));
            nameLabel.AddToClassList("research-row__name");
            row.Add(nameLabel);

            var statusLabel = new Label(StateCaption(state));
            statusLabel.AddToClassList("research-row__status");
            row.Add(statusLabel);

            var costLabel = new Label(CostText(node, state));
            costLabel.AddToClassList("research-row__cost");
            row.Add(costLabel);

            row.RegisterCallback<ClickEvent>(_ => OnResearchItemSelected?.Invoke(node));

            return row;
        }

        /// <summary>LOCKED/AVAILABLE/ACTIVE/MAXED 4단계 캡션.</summary>
        private static string StateCaption(string state) => state switch
        {
            "locked" => "잠김",
            "available" => "연구 가능",
            "active" => "진행 중",
            "maxed" => "완료",
            _ => state.ToUpperInvariant()
        };

        /// <summary>비용 열 텍스트 — locked는 "—", available은 실제 구매 비용(GetEffectiveCost,
        /// 읽기 전용 조회라 비용 로직 자체는 건드리지 않는다), active/maxed(둘 다 이미 해금됨)는
        /// "완료".</summary>
        private static string CostText(UpgradeNode node, string state)
        {
            if (node == null || UpgradeManager.Instance == null) return "—";
            return state switch
            {
                "available" => $"{UpgradeManager.Instance.GetEffectiveCost(node)} DNA",
                "active" => "완료",
                "maxed" => "완료",
                _ => "—"
            };
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

        // ================================================================
        // 하단 상세 패널 — 개별 항목이 아니라 "선택된 브랜치" 요약(진행률 + 다음 추천 연구) 2행을
        // 보여준다(V2 계획 §1 Step 12). 개별 항목 상세는 Research Popup(커밋 6~7)의 역할이다.
        // ================================================================

        private void BuildBranchSummary()
        {
            if (_detailRows == null) return;

            if (_selectedBranch == null)
            {
                if (_detailTitle != null) _detailTitle.text = "연구를 선택하세요";
                _detailRows.Clear();
                return;
            }

            int total = _selectedBranch.Items.Count;
            int unlocked = _selectedBranch.Items.Count(n => n.isUnlocked);

            if (_detailTitle != null) _detailTitle.text = _selectedBranch.Label;
            _detailRows.Clear();
            AddDetailRow("진행률", $"{unlocked}/{total}");

            var next = _selectedBranch.Items.FirstOrDefault(n => DetermineState(n) == "available");
            if (next != null)
                AddDetailRow("다음 추천 연구", DisplayName(next.id));
            else if (total > 0 && unlocked == total)
                AddDetailRow("다음 추천 연구", "브랜치 완료");
            else
                AddDetailRow("다음 추천 연구", "선행 조건 필요");
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
    }
}
