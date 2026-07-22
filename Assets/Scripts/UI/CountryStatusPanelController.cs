using System;
using System.Collections.Generic;
using System.Linq;
using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// HUD "국가현황" 버튼으로 여는 "GLOBAL STATUS CENTER" — 세계 감염 현황 센터.
    ///
    /// [Performance Dashboard v4, 2026-07-15 사용자 승인] 정체성 최종 확정: 이 화면은 "전파
    /// 전략 콘솔"이 아니라 "성과 대시보드"다. WorldMap=전략 판단, CountryPopup=국가 상세
    /// 브리핑, UpgradeTree=업그레이드 의사결정, CountryStatusPanel=성과/진행 상황 확인으로
    /// 역할을 분리했다. 화면 구성(위→아래): GLOBAL STATUS 배너 → WORLD OVERVIEW(세계 요약+
    /// 감염 국가 현황+국가 상태 분포+의료 시스템 현황을 캡션 하나로 통합) → 감염자 TOP10 →
    /// 사망자 TOP10(신규, 승리 조건과 직접 연결) → 48개국 목록(핵심 콘텐츠가 아닌 상세 조회
    /// 도구라 최하단). "전략 정보(공항/항구/국경 등)는 향후 WorldMap으로 이관"이라는 정체성
    /// 재정의 원칙에 따라 신규 전략 오버레이는 이번 범위에서 추가하지 않았다 — 단, 48개국
    /// 목록 각 행의 기존 공항/항구/국경 표시는 "목록의 기존 상세 정보"로 판단해 유지했다
    /// (사용자 확인, 2026-07-15). 종합 위협도 TOP10은 "전략 콘솔" 시절 설계라 제거.
    /// [Contagion UI Language 리디자인] 문장형(라벨:값 미분리, 상태 신호 중복) status-row를
    /// data-row(감염률/사망률)+badge(공항/항구/국경)+중립 상태캡션으로 재구성 — 공항/항구/국경
    /// 표시(위 문단의 FlagsLabel)는 이제 badge-tag 3개(BuildRow/RefreshRow 참고)로 형태만
    /// 바뀌었을 뿐 "유지" 결정 자체는 그대로다. 인구/의료수준은 CountryPopup으로 위임(중복
    /// 노출 아님, 이미 그 화면의 quick-stat-grid에 있음).
    ///
    /// [역할 재정의, 2026-07-10 사용자 승인] 이 컨트롤러는 원래(Step 28-2) CountryPopupController를
    /// 대체하는 "48개국 목록 화면"이었으나, CountryPopupController가 이미 개별 국가 상세 브리핑
    /// 역할로 확장 완료된 지금은 이 화면을 "국가 하나가 아니라 세계 전체 상황"을 보여주는 Tactical
    /// Dashboard로 재설계했다. 조사 근거는 Docs/CountryStatus_Dashboard_Investigation.md(CountryPopup
    /// 확장 조사 — 데이터 소스/재사용 가능 계산식은 동일하게 적용) + 세션 내 사용자 승인 설계.
    /// CountryPopupController/CountryPopup.uxml/.uss는 이번 작업에서 손대지 않는다 — 아래 치사율
    /// (근사)/의료 부하 공식은 그 클래스의 공식과 "동일 규약"으로 이 클래스에 독립적으로 복제했다
    /// (CountrySelectController.DevValueClass()가 CountryPopupController와 동일 규약을 별도로
    /// 구현해둔 것과 같은 방식 — 두 화면이 서로 의존하지 않게 하기 위함).
    ///
    /// [UX Reorder, 2026-07-15] 화면 구성(위→아래): GLOBAL STATUS 배너(세계 상황 한 줄 평가) →
    /// 48개국 목록(대륙별 접기/펼치기, CountryPopup 진입점 — 최상단 그룹) → 종합 위협도 TOP10
    /// (플레이어의 다음 행동 결정에 가장 실행 가능성이 높은 정보) → 감염자 TOP10(기존 유지) →
    /// 세계 요약(population-bar 재사용 + data-row 6줄) → 국가 상태 분포(GetCollapseStage() 6단계를
    /// SAFE/WARNING/DANGER/COLLAPSE 4버킷으로 집계) → 의료 시스템 현황(정상/주의/과부하/붕괴 4버킷,
    /// 국가 상태 분포와 동일 UI 재사용) → 감염 국가 현황(감염/무감염/소멸 국가 수 — 가장 파생적인
    /// 집계라 최하단). "세계 통계 리포트"에서 "플레이어 의사결정 콘솔"로 UX Audit 결과에 따라
    /// 섹션 순서만 재배치했다 — UXML 자식 순서만 바꿨고 요소 이름/클래스/데이터 바인딩(아래
    /// OnEnable()의 root.Q&lt;T&gt;() 조회는 이름 기반이라 DOM 순서 무관)·계산식·기능은 전부
    /// 그대로다. 신규 데이터 모델 없음 — Country/WorldState/WorldDataManager의 기존 필드와
    /// 계산식만 사용한다.
    ///
    /// [2차 확장, 2026-07-11 사용자 승인] GLOBAL STATUS/감염 국가 현황/의료 시스템 현황/종합
    /// 위협도(THREAT INDEX)를 추가하고, 기존 감염률/치사율(추정)/의료 부하 TOP10 랭킹 3개는
    /// 종합 위협도 TOP10 하나로 통합했다(랭킹 섹션 4개→2개, "정보량을 늘리지 말고 판단 데이터를
    /// 추가하라"는 요구 반영). GLOBAL HOTSPOT 카드와 교통 허브 위험도 집계는 검토 후 이번 범위에서
    /// 제외(사용자 선택).
    ///
    /// [갱신 빈도 설계] 세계 요약/분포/랭킹은 48개국 전체를 다시 훑어야 하는 "집계" 값이라
    /// WorldDataManager.OnWorldStateChanged(틱당 정확히 1회)에만 반응해 다시 그린다. 반면 하단
    /// 48개국 목록은 국가 하나의 값만 바뀌면 되므로 기존 방식 그대로 OnCountryChanged(국가별,
    /// 틱당 최대 20~30회)로 해당 행만 갱신한다 — 이 목록에 아직도 Clear()+전체 재생성을 쓰면
    /// 원래 있던 "매 틱 48개국 반복 재생성" 성능 버그가 재발하기 때문에(주석 하단 참고) 캐싱
    /// 패턴을 그대로 유지했다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountryStatusPanelController : MonoBehaviour
    {
        private enum StatusBucket { Safe, Warning, Danger, Collapse }

        private class RowRefs
        {
            public VisualElement Row;
            public Label NameLabel;
            public Label StageCaption;
            public Label StatsSummary;
            public Label AirportBadge;
            public Label PortBadge;
            public Label BorderBadge;
        }

        /// <summary>[대륙별 접기/펼치기, 2026-07-14 사용자 요청] 48개국 단일 리스트를 대륙 단위로
        /// 묶는다. `Country`에는 대륙 필드가 없어(Assets/Scripts/Data/Country.cs 확인 완료) 데이터
        /// 모델은 건드리지 않고 이 컨트롤러 전용 id→대륙 매핑을 새로 둔다 — MedicalLoad()/
        /// CaseFatalityRate() 등 다른 계산식을 CountryPopupController에서 독립적으로 복제해온
        /// 이 파일의 기존 관례와 동일한 방식이다. 러시아(유럽/아시아 걸침)·튀르키예(아시아 대부분,
        /// 유럽 취급도 흔함)·이집트(아프리카, 시나이반도만 아시아)처럼 대륙에 걸친 나라는 게임의
        /// 6대륙 단순화 관례를 따른 판단값이다 — 실제 플레이 확인 후 조정 가능.</summary>
        private static readonly Dictionary<string, string> ContinentByCountryId = new()
        {
            // 아시아(중동·중앙아시아 포함, 19개국)
            ["CHI"] = "ASIA", ["IND"] = "ASIA", ["KOR"] = "ASIA", ["JPN"] = "ASIA", ["IDN"] = "ASIA",
            ["SAU"] = "ASIA", ["PAK"] = "ASIA", ["BAN"] = "ASIA", ["PHI"] = "ASIA", ["VIE"] = "ASIA",
            ["IRN"] = "ASIA", ["TUR"] = "ASIA", ["THA"] = "ASIA", ["MYA"] = "ASIA", ["IRQ"] = "ASIA",
            ["AFG"] = "ASIA", ["UZB"] = "ASIA", ["MAS"] = "ASIA", ["YEM"] = "ASIA",
            // 유럽(7개국)
            ["GER"] = "EUROPE", ["UK"] = "EUROPE", ["FRA"] = "EUROPE", ["RUS"] = "EUROPE",
            ["ITA"] = "EUROPE", ["ESP"] = "EUROPE", ["POL"] = "EUROPE",
            // 북아메리카(3개국)
            ["USA"] = "NORTH AMERICA", ["MEX"] = "NORTH AMERICA", ["CAN"] = "NORTH AMERICA",
            // 남아메리카(4개국)
            ["BRA"] = "SOUTH AMERICA", ["COL"] = "SOUTH AMERICA", ["ARG"] = "SOUTH AMERICA",
            ["PER"] = "SOUTH AMERICA",
            // 아프리카(14개국)
            ["NGA"] = "AFRICA", ["EGY"] = "AFRICA", ["RSA"] = "AFRICA", ["DRC"] = "AFRICA",
            ["ETH"] = "AFRICA", ["TAN"] = "AFRICA", ["KEN"] = "AFRICA", ["SDN"] = "AFRICA",
            ["ALG"] = "AFRICA", ["UGA"] = "AFRICA", ["ANG"] = "AFRICA", ["MAR"] = "AFRICA",
            ["MOZ"] = "AFRICA", ["GHA"] = "AFRICA",
            // 오세아니아(1개국)
            ["AUS"] = "OCEANIA",
        };

        /// <summary>섹션이 그려지는 고정 순서. 매핑에 없는(향후 추가된) 국가는 ContinentOf()가
        /// "OTHER"로 떨어뜨리고, EnsureRowsBuilt()가 이 배열 뒤에 자동으로 이어붙인다.</summary>
        private static readonly string[] ContinentOrder =
            { "ASIA", "EUROPE", "NORTH AMERICA", "SOUTH AMERICA", "AFRICA", "OCEANIA" };

        /// <summary>초기 상태에서 펼쳐져 있을 대륙 — 요구사항: "ASIA만 펼침, 나머지는 접힘".</summary>
        private const string DefaultExpandedContinent = "ASIA";

        private static string ContinentOf(Country country) =>
            ContinentByCountryId.TryGetValue(country.id, out var continent) ? continent : "OTHER";

        private VisualElement _root;
        private Button _closeButton;

        private Label _globalStatusBanner;

        /// <summary>[Hero Stats, 2026-07-15 사용자 승인] GLOBAL STATUS 바로 아래 2x2 KPI —
        /// INFECTED/DEATHS/CURE/EXTINCT. 신규 데이터 없음: WorldState.infectedCount/deadCount/
        /// cureProgress + ExtinctCountryCount() 헬퍼(기존 소멸 국가 집계 추출)만 사용한다.</summary>
        private Label _heroInfectedValue;
        private Label _heroDeathsValue;
        private Label _heroCureValue;
        private Label _heroExtinctValue;

        private VisualElement _populationHealthySegment;
        private VisualElement _populationInfectedSegment;
        private VisualElement _populationDeadSegment;
        private Label _populationSummaryLabel;
        private VisualElement _worldSummaryRows;

        private VisualElement _infectionSummaryRows;

        private Label _distributionSafeCount;
        private Label _distributionWarningCount;
        private Label _distributionDangerCount;
        private Label _distributionCollapseCount;

        private Label _medicalNormalCount;
        private Label _medicalCautionCount;
        private Label _medicalOverloadCount;
        private Label _medicalCollapseCount;

        private VisualElement _rankingInfected;
        private VisualElement _rankingDead;

        /// <summary>[Performance Dashboard v5, 2026-07-15 사용자 승인] 국가 상태 분포/의료
        /// 시스템 현황을 삭제하지 않고 강등한 "ADVANCED DIAGNOSTICS" 접기/펼치기 섹션.
        /// 48개국 목록의 대륙 아코디언(_continentExpanded 등)과는 별개의 단일 bool로 관리한다
        /// — 대륙별 Dictionary를 재사용하면 이 섹션 하나만을 위해 불필요한 키 관리가 생긴다.
        /// 기본값 false(접힘)는 요구사항 그대로.</summary>
        private VisualElement _advancedDiagnosticsHeader;
        private Label _advancedDiagnosticsArrow;
        private VisualElement _advancedDiagnosticsBody;
        private bool _advancedDiagnosticsExpanded;

        private VisualElement _statusList;

        /// <summary>PopulateGlobalStatusBanner()가 매번 4개 modifier 클래스를 순회하며 하나만
        /// 켠다 — EnableInClassList를 4번 호출하는 것보다 배열로 순회하는 편이 실수(끄는 걸
        /// 빠뜨리는 것) 여지가 적다.</summary>
        private static readonly string[] GlobalStatusModifierClasses =
        {
            "global-status-banner--info",
            "global-status-banner--infected",
            "global-status-banner--danger",
            "global-status-banner--dead",
        };

        /// <summary>국가 id → 이미 생성된 행 참조. 최초 Populate() 때 한 번만 채워지고, 이후에는
        /// Clear()/재생성 없이 이 캐시를 통해 해당 행의 라벨만 갱신한다(기존 성능 최적화 유지).</summary>
        private readonly Dictionary<string, RowRefs> _rowsByCountryId = new();

        /// <summary>대륙 라벨("ASIA" 등) → 해당 섹션의 펼침 여부. 컨트롤러(=GameObject)가
        /// Show()/Hide()로만 토글되고 파괴되지 않는 한 이 필드가 그대로 유지되므로, 요구사항
        /// "CountryPopup을 열고 닫아도 대륙 펼침 상태 유지"가 별도 저장 로직 없이 충족된다
        /// (EnsureRowsBuilt()가 최초 1회만 섹션을 만드는 기존 가드와 같은 원리).</summary>
        private readonly Dictionary<string, bool> _continentExpanded = new();

        /// <summary>대륙 라벨 → 국가 행들을 담는 컨테이너. 펼침 토글 시 display만 바꾼다.</summary>
        private readonly Dictionary<string, VisualElement> _continentBodies = new();

        /// <summary>대륙 라벨 → 화살표(▼/▶) Label. 토글 시 텍스트만 바꾼다.</summary>
        private readonly Dictionary<string, Label> _continentArrows = new();

        /// <summary>패널이 열려 있는 동안만 이벤트에 반응해서 다시 그린다 — 닫혀 있을 땐 갱신
        /// 비용 자체를 안 쓰기 위함(기존과 동일한 가드).</summary>
        private bool _isShown;

        /// <summary>닫기(X) 버튼 클릭 — 이 컨트롤러 스스로 Hide()를 호출하지 않고 요청만 발행한다.
        /// 실제로 화면을 닫고 WorldMapInputLock을 해제하는 책임은 UIManager 한 곳에만 있다
        /// (UpgradeTreeView.OnCloseRequested와 동일 패턴 — WorldMap Input Lock 영구 유지 버그 수정).</summary>
        public event Action OnCloseRequested;

        /// <summary>48개국 목록 행 클릭 — Research Database의 "리스트 화면(AppScreen.Research) →
        /// 항목 클릭 → 상세 드릴다운(ResearchPopupController)" 구조를 이 화면에도 그대로 적용한다
        /// (2026-07-14 결정: 새 Country Database 화면을 만들지 않고 기존 CountryStatusPanel을
        /// 리스트→상세 팝업 구조로 확장, CountryPopup은 지도 클릭과 이 리스트 양쪽에서 재사용).
        /// 실제로 팝업을 여는 구독(CountryPopupController.ShowCountry 호출)은 UIManager가 담당한다
        /// (UpgradeTreeView.OnResearchItemSelected와 동일 패턴 — 이 클래스는 발행만 한다).</summary>
        public event Action<Country> OnCountryRowSelected;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _root = root.Q<VisualElement>("status-root");
            _closeButton = root.Q<Button>("close-button");

            _globalStatusBanner = root.Q<Label>("global-status-banner");

            _heroInfectedValue = root.Q<Label>("hero-infected-value");
            _heroDeathsValue = root.Q<Label>("hero-deaths-value");
            _heroCureValue = root.Q<Label>("hero-cure-value");
            _heroExtinctValue = root.Q<Label>("hero-extinct-value");

            _populationHealthySegment = root.Q<VisualElement>("population-bar-healthy");
            _populationInfectedSegment = root.Q<VisualElement>("population-bar-infected");
            _populationDeadSegment = root.Q<VisualElement>("population-bar-dead");
            _populationSummaryLabel = root.Q<Label>("population-bar-summary");
            _worldSummaryRows = root.Q<VisualElement>("world-summary-rows");

            _infectionSummaryRows = root.Q<VisualElement>("infection-summary-rows");

            _distributionSafeCount = root.Q<Label>("distribution-safe-count");
            _distributionWarningCount = root.Q<Label>("distribution-warning-count");
            _distributionDangerCount = root.Q<Label>("distribution-danger-count");
            _distributionCollapseCount = root.Q<Label>("distribution-collapse-count");

            _medicalNormalCount = root.Q<Label>("medical-normal-count");
            _medicalCautionCount = root.Q<Label>("medical-caution-count");
            _medicalOverloadCount = root.Q<Label>("medical-overload-count");
            _medicalCollapseCount = root.Q<Label>("medical-collapse-count");

            _rankingInfected = root.Q<VisualElement>("ranking-infected");
            _rankingDead = root.Q<VisualElement>("ranking-dead");

            _advancedDiagnosticsHeader = root.Q<VisualElement>("advanced-diagnostics-header");
            _advancedDiagnosticsArrow = root.Q<Label>("advanced-diagnostics-arrow");
            _advancedDiagnosticsBody = root.Q<VisualElement>("advanced-diagnostics-body");
            _advancedDiagnosticsHeader?.RegisterCallback<ClickEvent>(_ => ToggleAdvancedDiagnostics());

            _statusList = root.Q<VisualElement>("status-list");

            _closeButton.RegisterCallback<ClickEvent>(_ => OnCloseRequested?.Invoke());

            Subscribe();
            Hide();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
                WorldDataManager.Instance.OnCountryChanged += HandleCountryChanged;

                WorldDataManager.Instance.OnWorldStateChanged -= HandleWorldStateChanged;
                WorldDataManager.Instance.OnWorldStateChanged += HandleWorldStateChanged;
            }

            if (WorldMap.Instance != null)
            {
                WorldMap.Instance.OnCountryClicked -= HandleWorldMapCountryClicked;
                WorldMap.Instance.OnCountryClicked += HandleWorldMapCountryClicked;
            }
        }

        private void OnDisable()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
                WorldDataManager.Instance.OnWorldStateChanged -= HandleWorldStateChanged;
            }

            if (WorldMap.Instance != null)
                WorldMap.Instance.OnCountryClicked -= HandleWorldMapCountryClicked;
        }

        /// <summary>[패널 충돌 수정, 2026-07-11] 지도에서 국가를 클릭하면 `CountryPopupController`가
        /// 자신의 `WorldMap.OnCountryClicked` 구독으로 독립적으로(이 클래스와 무관하게) 팝업을 띄운다
        /// — `CountryPopupController`/`UIManager`는 이번 작업 수정 대상이 아니라서(사용자 지정),
        /// 그 쪽에서 "Global Status Center가 열려 있으면 먼저 닫아라" 같은 조정을 걸 수 없다. 대신
        /// 이 클래스가 스스로 지도 클릭 이벤트를 구독해 "국가를 클릭하면 이 대시보드는 스스로 닫힌다"로
        /// 해결했다 — CountryStatusPanel(세계 요약, top:3%~bottom:12.5% 거의 풀스크린)과 CountryPopup이
        /// 동시에 열려 있으면 둘 다 같은 `sortingOrder`(씬 확인, 둘 다 1)를 써서 어느 쪽이 위에 오는지
        /// 불명확한 데다, CountryStatusPanel의 (투명하게 보여도 UI Toolkit 히트테스트는 여전히 막는)
        /// 전체화면 루트가 CountryPopup의 닫기(✕) 버튼 클릭을 가로채 "눌러도 반응 없고 콘솔 로그도
        /// 안 뜨는" 증상으로 나타났다. 국가를 클릭하는 순간 이 대시보드를 닫아버리면 그 뒤로는
        /// CountryPopup 단독으로만 화면에 남아 이 충돌 자체가 발생하지 않는다(UX적으로도 "세계
        /// 요약 보다가 국가 하나를 짚으면 그 나라 상세로 전환"이 자연스럽다).</summary>
        private void HandleWorldMapCountryClicked(Country _)
        {
            if (_isShown) Hide();
        }

        /// <summary>매 틱 국가별로 개별 호출될 수 있는 이벤트 — 여기서는 절대 전체를 다시 그리지
        /// 않고, 바뀐 국가 한 줄만 갱신한다(기존 동작 그대로).</summary>
        private void HandleCountryChanged(Country country)
        {
            if (!_isShown) return;

            EnsureRowsBuilt();
            RefreshRow(country);
        }

        /// <summary>틱당 1회만 발행 — 48개국 전체를 다시 훑어야 하는 세계 요약/분포/랭킹은
        /// 이 이벤트에서만 재계산한다(Docs/CountryStatus_Dashboard_Investigation.md 2.4절과 동일한
        /// 근거 — 48개국 정렬 4회 정도는 틱당 1회 수준이면 성능 영향 없음).</summary>
        private void HandleWorldStateChanged(WorldState state)
        {
            if (!_isShown || state == null) return;

            PopulateGlobalStatusBanner(state);
            PopulateWorldSummary(state);

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            PopulateHeroStats(state, countries);
            PopulateInfectionSummary(countries);
            PopulateDistribution(countries);
            PopulateMedicalDistribution(countries);
            PopulateRankings(countries);
        }

        public void Show()
        {
            _isShown = true;
            if (_root != null) _root.style.display = DisplayStyle.Flex;

            if (WorldDataManager.Instance != null)
            {
                HandleWorldStateChanged(WorldDataManager.Instance.State);
            }

            PopulateList();
        }

        public void Hide()
        {
            _isShown = false;
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        /// <summary>CountryPopup(Bottom Sheet)의 "상세 보기" 버튼 진입점 — Show()로 패널을 연 뒤,
        /// 해당 국가가 속한 대륙 아코디언만 펼친다(48개국 목록 재구성/자동 스크롤/행 강조는 하지
        /// 않음 — 이번 작업 범위 밖). 기존 대륙 펼침 재료(ContinentOf/_continentExpanded/
        /// ApplyContinentExpandedState)를 그대로 재사용한다.</summary>
        public void FocusCountry(Country country)
        {
            Show();

            if (country == null) return;

            string continent = ContinentOf(country);
            if (!_continentExpanded.ContainsKey(continent)) return;

            _continentExpanded[continent] = true;
            ApplyContinentExpandedState(continent);
        }

        // ------------------------------------------------------------
        // GLOBAL STATUS — 세계 상황 한 줄 평가
        // ------------------------------------------------------------

        /// <summary>WorldState.GetMortalityStage()(기존, 사망률 축)와 세계 감염률(WorldInfectionRate,
        /// 세계 요약과 동일 계산을 공유)을 조합해 4단계 한 줄 평가로 압축한다. 신규 데이터/계산식
        /// 없음 — 기존 값 두 개를 조합만 한다. 임계값(감염률 5%/50%)은 제안값이며 플레이테스트로
        /// 조정 필요(Docs/QA_Checklist.md 참고).</summary>
        private void PopulateGlobalStatusBanner(WorldState state)
        {
            if (_globalStatusBanner == null) return;

            var (text, modifierClass) = GlobalStatusAssessment(state);
            _globalStatusBanner.text = text;

            foreach (var cls in GlobalStatusModifierClasses)
                _globalStatusBanner.EnableInClassList(cls, cls == modifierClass);
        }

        private static (string text, string modifierClass) GlobalStatusAssessment(WorldState state)
        {
            var mortalityStage = state.GetMortalityStage();
            float infectionRate = WorldInfectionRate(state);

            if (mortalityStage == WorldMortalityStage.ExtinctionImminent)
                return ("세계 붕괴 임박", "global-status-banner--dead");
            if (infectionRate >= 0.5f)
                return ("세계적 대유행", "global-status-banner--danger");
            if (infectionRate >= 0.05f || mortalityStage == WorldMortalityStage.WorldThreatened)
                return ("확산 진행 중", "global-status-banner--infected");
            return ("봉쇄 성공", "global-status-banner--info");
        }

        private static float WorldInfectionRate(WorldState state)
        {
            long living = Math.Max(0L, state.totalPopulation - state.deadCount);
            return living > 0 ? (float)state.infectedCount / living : 0f;
        }

        // ------------------------------------------------------------
        // Hero Stats — INFECTED / DEATHS / CURE / EXTINCT 2x2 KPI
        // ------------------------------------------------------------

        /// <summary>신규 계산식 없음 — WorldState.infectedCount/deadCount/cureProgress(HudController와
        /// 동일한 %표기 규약)와 ExtinctCountryCount() 헬퍼만 사용한다.</summary>
        private void PopulateHeroStats(WorldState state, IReadOnlyList<Country> countries)
        {
            if (_heroInfectedValue != null) _heroInfectedValue.text = NumberFormatter.FormatSummary(state.infectedCount);
            if (_heroDeathsValue != null) _heroDeathsValue.text = NumberFormatter.FormatSummary(state.deadCount);
            if (_heroCureValue != null) _heroCureValue.text = $"{state.cureProgress * 100f:F1}%";
            if (_heroExtinctValue != null) _heroExtinctValue.text = ExtinctCountryCount(countries).ToString();
        }

        /// <summary>[헬퍼 추출, 2026-07-15] 기존 PopulateInfectionSummary()의 "소멸 국가" 집계식을
        /// 그대로 뽑아냈다 — Hero Stats(EXTINCT)와 WORLD OVERVIEW(소멸 국가) 둘 다 같은 값을
        /// 단일 소스에서 읽도록 하기 위함(신규 계산식 아님, 기존 식 재사용).</summary>
        private static int ExtinctCountryCount(IReadOnlyList<Country> countries) =>
            countries.Count(c => c.GetCollapseStage() == CountryCollapseStage.Extinct);

        // ------------------------------------------------------------
        // 세계 요약 — population-bar(Hud.uss 재사용) + data-row 6줄
        // ------------------------------------------------------------

        /// <summary>HudController.UpdatePopulationBar()와 동일 계산(healthy/infected/dead 비율을
        /// flexGrow에 그대로 대입) — 이 화면 전용 population-bar 인스턴스(다른 UIDocument)에도
        /// 같은 방식을 그대로 적용한다.</summary>
        private void PopulateWorldSummary(WorldState state)
        {
            long total = state.totalPopulation;
            long dead = state.deadCount;
            long infected = state.infectedCount;

            if (_populationHealthySegment != null)
            {
                if (total <= 0)
                {
                    _populationHealthySegment.style.flexGrow = 1f;
                    _populationInfectedSegment.style.flexGrow = 0f;
                    _populationDeadSegment.style.flexGrow = 0f;
                    if (_populationSummaryLabel != null)
                        _populationSummaryLabel.text = "감염 0.0% · 사망 0.00%";
                }
                else
                {
                    long healthy = Math.Max(0L, total - infected - dead);
                    _populationHealthySegment.style.flexGrow = healthy;
                    _populationInfectedSegment.style.flexGrow = infected;
                    _populationDeadSegment.style.flexGrow = dead;

                    if (_populationSummaryLabel != null)
                    {
                        float infectedPct = (float)infected / total * 100f;
                        float deadPct = (float)dead / total * 100f;
                        _populationSummaryLabel.text = $"감염 {infectedPct:F1}% · 사망 {deadPct:F2}%";
                    }
                }
            }

            if (_worldSummaryRows == null) return;
            _worldSummaryRows.Clear();

            // GLOBAL STATUS 배너와 동일한 WorldInfectionRate()를 재사용(값 불일치 방지).
            float worldInfectionRate = WorldInfectionRate(state);
            // 세계 치사율(추정) — CountryPopupController.CaseFatalityRate()와 동일 규약(사망/(사망+감염)
            // 근사치)을 세계 스케일에 그대로 적용. 정확한 누적 CFR은 현재 데이터로 계산 불가능하다는
            // 결론이 국가 단위와 동일하게 적용된다(Docs/CountryStatus_Dashboard_Investigation.md 2.2절).
            float worldCfr = (dead + infected) > 0 ? (float)dead / (dead + infected) : 0f;

            // [Performance Dashboard v5, 2026-07-15 사용자 승인] "총 감염자"/"총 사망자" 행은
            // 위 Hero Stats(INFECTED/DEATHS)와 중복돼 제거했다 — 계산식은 그대로, 표시 위치만
            // Hero Stats로 옮겨졌다(신규 계산식 없음).
            AddDataRow(_worldSummaryRows, "세계 인구", $"{total:N0}");
            AddDataRow(_worldSummaryRows, "세계 감염률", $"{worldInfectionRate * 100f:F1}%",
                       worldInfectionRate >= 0.5f ? "data-value--danger" : "data-value--infected");
            AddDataRow(_worldSummaryRows, "치사율(추정)", $"{worldCfr * 100f:F1}%",
                       worldCfr >= 0.3f ? "data-value--dead" : null);
            AddDataRow(_worldSummaryRows, "경과 일수", $"{state.currentDay}일차");
        }

        // ------------------------------------------------------------
        // 감염 국가 현황 — 감염/무감염/소멸 국가 수
        // ------------------------------------------------------------

        /// <summary>"국가 상태 분포"(사망률 기준 4버킷)와 별개 축 — 이건 "감염이 실제로 시작됐는지"
        /// (infectedCount&gt;0) 기준이라 아직 침투되지 않은 국가를 따로 보여준다. 소멸(Extinct,
        /// 사망률 100%)은 기존 COLLAPSE 버킷에 이미 포함돼 있지만 거기선 안 보이므로 여기서 별도
        /// 카운트로 뽑아준다. 신규 계산식 없음 — Country.infectedCount/GetCollapseStage() 재사용.</summary>
        private void PopulateInfectionSummary(IReadOnlyList<Country> countries)
        {
            if (_infectionSummaryRows == null) return;
            _infectionSummaryRows.Clear();

            int total = countries.Count;
            int infectedCountries = countries.Count(c => c.infectedCount > 0);
            int extinctCountries = ExtinctCountryCount(countries);

            // [Performance Dashboard v5, 2026-07-15 사용자 승인] "무감염 국가 수" 행은
            // Hero Stats(EXTINCT)·감염 국가 행과 함께 두면 정보가 중복돼 제거했다.
            AddDataRow(_infectionSummaryRows, "감염 국가", $"{infectedCountries} / {total}",
                       infectedCountries > 0 ? "data-value--infected" : "data-value--info");
            AddDataRow(_infectionSummaryRows, "소멸 국가", $"{extinctCountries}",
                       extinctCountries > 0 ? "data-value--dead" : null);
        }

        // ------------------------------------------------------------
        // 국가 상태 분포 — GetCollapseStage() 6단계 → 4버킷 집계
        // ------------------------------------------------------------

        private void PopulateDistribution(IReadOnlyList<Country> countries)
        {
            int safe = 0, warning = 0, danger = 0, collapse = 0;
            foreach (var country in countries)
            {
                switch (BucketOf(country.GetCollapseStage()))
                {
                    case StatusBucket.Safe: safe++; break;
                    case StatusBucket.Warning: warning++; break;
                    case StatusBucket.Danger: danger++; break;
                    default: collapse++; break;
                }
            }

            if (_distributionSafeCount != null) _distributionSafeCount.text = safe.ToString();
            if (_distributionWarningCount != null) _distributionWarningCount.text = warning.ToString();
            if (_distributionDangerCount != null) _distributionDangerCount.text = danger.ToString();
            if (_distributionCollapseCount != null) _distributionCollapseCount.text = collapse.ToString();
        }

        /// <summary>사망률 기준 6단계(CountryCollapseStage)를 플레이어가 한눈에 읽을 4단계로
        /// 묶는다. Normal=평시(SAFE), FullCollapse=초기 붕괴 조짐(WARNING), Disorder=명백한 위험
        /// (DANGER), NearAnarchy/FullAnarchy/Extinct=사실상 회생 불가(COLLAPSE)로 그룹화 —
        /// 임계값 자체는 Country.GetCollapseStage()를 그대로 재사용(신규 계산식 없음).</summary>
        private static StatusBucket BucketOf(CountryCollapseStage stage) => stage switch
        {
            CountryCollapseStage.Normal => StatusBucket.Safe,
            CountryCollapseStage.FullCollapse => StatusBucket.Warning,
            CountryCollapseStage.Disorder => StatusBucket.Danger,
            _ => StatusBucket.Collapse, // NearAnarchy / FullAnarchy / Extinct
        };

        // ------------------------------------------------------------
        // 의료 시스템 현황 — MedicalLoad() 4버킷(정상/주의/과부하/붕괴) 국가 수 집계
        // ------------------------------------------------------------

        /// <summary>"국가 상태 분포"와 동일한 distribution-row/stat-chip UI를 재사용해 의료 부하
        /// 축(감염 비율×(1-의료수준))도 같은 방식으로 집계한다. 버킷 판정은 MedicalLoadStatus()의
        /// 라벨을 그대로 재사용해 임계값이 어긋날 여지를 없앴다(단일 소스).</summary>
        private void PopulateMedicalDistribution(IReadOnlyList<Country> countries)
        {
            int normal = 0, caution = 0, overload = 0, collapse = 0;
            foreach (var country in countries)
            {
                switch (MedicalLoadStatus(MedicalLoad(country)).label)
                {
                    case "정상": normal++; break;
                    case "주의": caution++; break;
                    case "과부하": overload++; break;
                    default: collapse++; break; // "붕괴"
                }
            }

            if (_medicalNormalCount != null) _medicalNormalCount.text = normal.ToString();
            if (_medicalCautionCount != null) _medicalCautionCount.text = caution.ToString();
            if (_medicalOverloadCount != null) _medicalOverloadCount.text = overload.ToString();
            if (_medicalCollapseCount != null) _medicalCollapseCount.text = collapse.ToString();
        }

        // ------------------------------------------------------------
        // 랭킹 — 감염자 / 사망자 각 TOP 10
        // ------------------------------------------------------------

        /// <summary>[Performance Dashboard v4, 2026-07-15 사용자 승인] 종합 위협도 TOP10("전략
        /// 콘솔" 시절 설계, ThreatIndex() 가중합)은 성과 대시보드 정체성과 맞지 않아 제거했다.
        /// 대신 승리 조건과 직접 연결되는 사망자 TOP10을 추가 — 감염자 TOP10과 동일하게
        /// RebuildTopRows()에 기존 Country.deadCount만 넘긴다(신규 계산식 없음). ThreatIndex()가
        /// 유일하게 쓰던 CaseFatalityRate()(치사율 근사 계산)도 이제 다른 곳에서 참조되지 않아
        /// 함께 제거했다 — 세계 요약의 치사율(추정)은 별도 인라인 계산(worldCfr)이라 영향 없다.</summary>
        private void PopulateRankings(IReadOnlyList<Country> countries)
        {
            RebuildTopRows(_rankingInfected, countries,
                c => (double)c.infectedCount,
                c => $"{c.infectedCount:N0}",
                _ => "data-value--infected");

            RebuildTopRows(_rankingDead, countries,
                c => (double)c.deadCount,
                c => $"{c.deadCount:N0}",
                _ => "data-value--dead");
        }

        private static void RebuildTopRows(
            VisualElement container,
            IReadOnlyList<Country> countries,
            Func<Country, double> keySelector,
            Func<Country, string> valueFormatter,
            Func<Country, string> valueClassSelector,
            int topN = 10)
        {
            if (container == null) return;
            container.Clear();

            var sorted = countries.OrderByDescending(keySelector).Take(topN).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var country = sorted[i];
                AddDataRow(container, $"{i + 1}. {country.name}", valueFormatter(country), valueClassSelector(country));
            }
        }

        private static float InfectionRatio(Country country) =>
            country.LivingPopulation > 0 ? (float)country.infectedCount / country.LivingPopulation : 0f;

        /// <summary>CountryPopupController.MedicalLoadStatus()와 동일 공식(감염 비율 × (1-의료수준))을
        /// 독립적으로 복제.</summary>
        private static float MedicalLoad(Country country) => InfectionRatio(country) * (1f - country.HealthLevel);

        private static (string label, string valueClass) MedicalLoadStatus(float load)
        {
            if (load < 0.1f) return ("정상", "data-value--info");
            if (load < 0.3f) return ("주의", "data-value--infected");
            if (load < 0.6f) return ("과부하", "data-value--danger");
            return ("붕괴", "data-value--dead");
        }

        /// <summary>Tactical.uss data-row/data-label/data-value 계약 — TacticalModalController.AddRow()와
        /// 동일 패턴을 이 컨트롤러 전용으로 복제(여러 컨테이너에 붙여야 해서 컨테이너를 인자로 받는
        /// 점만 다르다).</summary>
        private static void AddDataRow(VisualElement container, string label, string value, string valueClass = null)
        {
            if (container == null) return;

            var row = new VisualElement();
            row.AddToClassList("data-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("data-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("data-value");
            if (!string.IsNullOrEmpty(valueClass)) valueEl.AddToClassList(valueClass);
            row.Add(valueEl);

            container.Add(row);
        }

        // ------------------------------------------------------------
        // 48개국 목록 — 기존 O(1) 행 캐싱 구조 그대로 유지, 시각만 버킷 accent bar로 갱신
        // ------------------------------------------------------------

        /// <summary>패널을 열 때 한 번만 호출 — 행이 아직 없으면 생성하고, 전체 국가 값을 최신화한다.
        /// (이 전체 순회는 Show() 시 1회뿐이라 비용 문제가 없다.)</summary>
        private void PopulateList()
        {
            if (_statusList == null) return;

            EnsureRowsBuilt();

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            foreach (var country in countries)
                RefreshRow(country);
        }

        /// <summary>행이 이미 만들어져 있으면 아무 것도 하지 않는다 — 국가 목록은 게임 시작 시
        /// 고정되므로(48개), 최초 1회 생성 후에는 재생성이 필요 없다(기존 동작 그대로 유지).
        /// [대륙별 접기/펼치기, 2026-07-14] 이제 국가 행을 `_statusList`에 바로 붙이지 않고
        /// 대륙 섹션(`ContinentOrder` 순서, 매핑 밖 국가는 뒤에 자동으로 이어붙임) → 섹션 바디
        /// 순으로 붙인다. 섹션/펼침 상태는 최초 1회만 만들어지므로 이후 Show()/Hide()나
        /// CountryPopup 열고 닫기로는 재생성되지 않는다(펼침 상태 유지 요구사항의 근거).</summary>
        private void EnsureRowsBuilt()
        {
            if (_rowsByCountryId.Count > 0) return;
            if (_statusList == null) return;

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            _statusList.Clear();
            _rowsByCountryId.Clear();
            _continentBodies.Clear();
            _continentArrows.Clear();
            _continentExpanded.Clear();

            var byContinent = countries.GroupBy(ContinentOf).ToDictionary(g => g.Key, g => g.ToList());

            // 고정 순서(ContinentOrder) 먼저, 매핑에 없는 대륙("OTHER")은 뒤에 이어붙인다.
            var continentKeys = ContinentOrder.Where(byContinent.ContainsKey)
                .Concat(byContinent.Keys.Where(k => !ContinentOrder.Contains(k)))
                .ToList();

            foreach (var continentLabel in continentKeys)
            {
                var members = byContinent[continentLabel];
                _continentExpanded[continentLabel] = continentLabel == DefaultExpandedContinent;

                var (section, arrow, body) = BuildContinentSection(continentLabel, members.Count);
                _continentArrows[continentLabel] = arrow;
                _continentBodies[continentLabel] = body;
                ApplyContinentExpandedState(continentLabel);

                foreach (var country in members)
                {
                    var refs = BuildRow(country);
                    _rowsByCountryId[country.id] = refs;
                    body.Add(refs.Row);
                }

                _statusList.Add(section);
            }
        }

        /// <summary>대륙 헤더(화살표+"ASIA (12)") + 바디(국가 행 컨테이너)로 구성된 섹션 하나를
        /// 만든다. AddDataRow()와 같은 패턴(컨트롤러가 런타임에 VisualElement를 직접 조립)을
        /// 따른다. 헤더 클릭은 이 대륙 하나만 토글한다(요구사항: 대륙 헤더 클릭 시 접힘↔펼침).</summary>
        private (VisualElement section, Label arrow, VisualElement body) BuildContinentSection(
            string continentLabel, int countryCount)
        {
            var section = new VisualElement();
            section.AddToClassList("continent-section");

            var header = new VisualElement();
            header.AddToClassList("continent-header");
            header.AddToClassList("accent-bar-row");

            var arrow = new Label();
            arrow.AddToClassList("continent-header__arrow");
            header.Add(arrow);

            var titleLabel = new Label($"{continentLabel} ({countryCount})");
            titleLabel.AddToClassList("continent-header__label");
            header.Add(titleLabel);

            header.RegisterCallback<ClickEvent>(_ => ToggleContinent(continentLabel));

            var body = new VisualElement();
            body.AddToClassList("continent-body");

            section.Add(header);
            section.Add(body);

            return (section, arrow, body);
        }

        private void ToggleContinent(string continentLabel)
        {
            if (!_continentExpanded.ContainsKey(continentLabel)) return;
            _continentExpanded[continentLabel] = !_continentExpanded[continentLabel];
            ApplyContinentExpandedState(continentLabel);
        }

        /// <summary>펼침 상태 → 바디 display + 화살표 텍스트(▼ 펼침 / ▶ 접힘) 반영. 상태를
        /// 바꾸는 쪽(ToggleContinent)과 최초 반영하는 쪽(EnsureRowsBuilt) 둘 다 이 메서드 하나로
        /// 모아 두 곳의 표시 로직이 어긋나지 않게 했다.</summary>
        private void ApplyContinentExpandedState(string continentLabel)
        {
            bool expanded = _continentExpanded.TryGetValue(continentLabel, out var value) && value;

            if (_continentBodies.TryGetValue(continentLabel, out var body))
                body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (_continentArrows.TryGetValue(continentLabel, out var arrow))
                arrow.text = expanded ? "▼" : "▶";
        }

        /// <summary>ADVANCED DIAGNOSTICS 헤더 클릭 — ToggleContinent()/ApplyContinentExpandedState()와
        /// 동일한 토글+표시 로직이지만, 이 섹션은 대륙별 Dictionary에 속하지 않는 단일 섹션이라
        /// 별도 bool 필드로 관리한다(48개국 목록 아코디언과 무관, 서로 영향 없음).</summary>
        private void ToggleAdvancedDiagnostics()
        {
            _advancedDiagnosticsExpanded = !_advancedDiagnosticsExpanded;

            if (_advancedDiagnosticsBody != null)
                _advancedDiagnosticsBody.style.display =
                    _advancedDiagnosticsExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            if (_advancedDiagnosticsArrow != null)
                _advancedDiagnosticsArrow.text = _advancedDiagnosticsExpanded ? "▼" : "▶";
        }

        /// <summary>Contagion UI Language 리디자인(설계 검증 완료) — 라벨:값이 붙은 문장 3줄 대신
        /// 이름/상태캡션/data-row 2개/badge 3개로 분해한다. accent-bar(EnableInClassList 아래)가
        /// 유일한 상태 신호이고, StageCaption은 중립색 텍스트로 accent-bar 4버킷이 뭉개는
        /// NearAnarchy/FullAnarchy/Extinct 구분만 보완한다(원칙 3 — 신호는 하나, 단 다른 차원의
        /// 정보를 신호 없이 추가 노출하는 것은 중복이 아니다).</summary>
        private RowRefs BuildRow(Country country)
        {
            var row = new VisualElement();
            row.AddToClassList("status-row");

            var nameLabel = new Label();
            nameLabel.AddToClassList("status-row__name");
            row.Add(nameLabel);

            var stageCaption = new Label();
            stageCaption.AddToClassList("status-row__stage");
            row.Add(stageCaption);

            // [Direction C Phase 5B] 감염률/사망률을 각자 data-row(2줄)로 쌓지 않고
            // population-bar__summary와 동일한 "감염 N% · 사망 N%" 한 줄 문법을 재사용한다
            // (HudController/CountryStatusPanelController가 이미 쓰고 있는 관례, RefreshRow 참고).
            var statsSummary = new Label();
            statsSummary.AddToClassList("status-row__stats-summary");
            row.Add(statsSummary);

            var badges = new VisualElement();
            badges.AddToClassList("status-row__badges");
            var airportBadge = new Label();
            airportBadge.AddToClassList("badge-tag");
            badges.Add(airportBadge);
            var portBadge = new Label();
            portBadge.AddToClassList("badge-tag");
            badges.Add(portBadge);
            var borderBadge = new Label();
            borderBadge.AddToClassList("badge-tag");
            badges.Add(borderBadge);
            row.Add(badges);

            // research-row와 동일한 규약 — 행 자체가 클릭 대상이며, 상태(LOCKED 포함)와 무관하게
            // 항상 상세를 열 수 있다(research-row도 LOCKED 상태에서 선행조건 확인을 위해 클릭 가능).
            // country는 이 메서드 호출 시점의 참조를 클로저로 캡처하지만 WorldDataManager.Countries의
            // Country 인스턴스는 게임 시작 시 고정되어 필드만 매 틱 갱신되므로(RefreshRow 참고),
            // 나중에 클릭해도 항상 최신 데이터를 가리킨다.
            row.RegisterCallback<ClickEvent>(_ => OnCountryRowSelected?.Invoke(country));

            return new RowRefs
            {
                Row = row,
                NameLabel = nameLabel,
                StageCaption = stageCaption,
                StatsSummary = statsSummary,
                AirportBadge = airportBadge,
                PortBadge = portBadge,
                BorderBadge = borderBadge
            };
        }

        /// <summary>이미 생성된 행의 라벨 텍스트만 갱신한다 — VisualElement/Label을 새로 만들지 않는다
        /// (기존 성능 최적화 그대로). 좌측 accent bar 색상만 버킷이 바뀔 때 클래스 스위칭으로 갱신한다.</summary>
        private void RefreshRow(Country country)
        {
            if (!_rowsByCountryId.TryGetValue(country.id, out var refs)) return;

            float infectionRatio = InfectionRatio(country);
            float deadRatio = country.population > 0 ? (float)country.deadCount / country.population : 0f;

            var stage = country.GetCollapseStage();
            var bucket = BucketOf(stage);

            refs.NameLabel.text = country.name;
            refs.StageCaption.text = StageLabel(stage);

            // [Direction C Phase 5B] population-bar__summary와 동일 포맷("감염 N% · 사망 N%")
            // — 새 계산식 없이 기존 infectionRatio/deadRatio를 한 줄로 결합만 한다.
            refs.StatsSummary.text = $"감염 {infectionRatio:P0} · 사망 {deadRatio:P0}";

            refs.AirportBadge.text = $"공항 {(country.isAirportOpen ? "개방" : "폐쇄")}";
            refs.PortBadge.text = $"항구 {(country.isPortOpen ? "개방" : "폐쇄")}";
            refs.BorderBadge.text = $"국경 {(country.isBorderClosed ? "봉쇄" : "개방")}";

            // [Direction C Color Pass 5A] CountryPopupController.ApplySeverityClass()와 동일 패턴 —
            // 텍스트만 갱신하고 severity 클래스를 부여하지 않아 개방/폐쇄가 항상 같은 회색 테두리로
            // 보이던 문제를 해결. 신규 CSS/색상 없음(badge-tag--success/warning은 Tactical.uss에
            // 이미 존재), 기존 isAirportOpen/isPortOpen/isBorderClosed 데이터만 사용.
            ApplySeverityClass(refs.AirportBadge, country.isAirportOpen ? "badge-tag--success" : "badge-tag--warning");
            ApplySeverityClass(refs.PortBadge, country.isPortOpen ? "badge-tag--success" : "badge-tag--warning");
            ApplySeverityClass(refs.BorderBadge, country.isBorderClosed ? "badge-tag--warning" : "badge-tag--success");

            refs.Row.EnableInClassList("status-row--safe", bucket == StatusBucket.Safe);
            refs.Row.EnableInClassList("status-row--warning", bucket == StatusBucket.Warning);
            refs.Row.EnableInClassList("status-row--danger", bucket == StatusBucket.Danger);
            refs.Row.EnableInClassList("status-row--collapse", bucket == StatusBucket.Collapse);
        }

        /// <summary>badge-tag 계열 severity 수정자 클래스를 서로 배타적으로 적용한다 —
        /// CountryPopupController.ApplySeverityClass()와 동일 규약(이전 값이 남아있으면 색이
        /// 겹쳐 보일 수 있어 항상 전부 지운 뒤 하나만 붙인다).</summary>
        private static readonly string[] BadgeSeverityClasses =
        {
            "badge-tag--success", "badge-tag--warning", "badge-tag--danger", "badge-tag--info"
        };

        private static void ApplySeverityClass(Label label, string cssClass)
        {
            foreach (var cls in BadgeSeverityClasses)
                label.RemoveFromClassList(cls);
            if (!string.IsNullOrEmpty(cssClass))
                label.AddToClassList(cssClass);
        }

        private static string StageLabel(CountryCollapseStage stage) => stage switch
        {
            CountryCollapseStage.Normal => "평시",
            CountryCollapseStage.FullCollapse => "붕괴 시작",
            CountryCollapseStage.Disorder => "무질서",
            CountryCollapseStage.NearAnarchy => "무정부 근접",
            CountryCollapseStage.FullAnarchy => "완전 무정부",
            CountryCollapseStage.Extinct => "소멸",
            _ => stage.ToString()
        };
    }
}
