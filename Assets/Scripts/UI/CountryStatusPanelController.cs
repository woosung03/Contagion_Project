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
    /// 화면 구성(위→아래): GLOBAL STATUS 배너(세계 상황 한 줄 평가) → 세계 요약(population-bar
    /// 재사용 + data-row 6줄) → 감염 국가 현황(감염/무감염/소멸 국가 수) → 국가 상태 분포
    /// (GetCollapseStage() 6단계를 SAFE/WARNING/DANGER/COLLAPSE 4버킷으로 집계) → 의료 시스템 현황
    /// (정상/주의/과부하/붕괴 4버킷, 국가 상태 분포와 동일 UI 재사용) → 랭킹 2종(종합 위협도 TOP10
    /// +감염자 TOP10) → 48개국 목록(기존 유지, 좌측 accent bar로 버킷 색상 표시). 신규 데이터
    /// 모델 없음 — Country/WorldState/WorldDataManager의 기존 필드와 계산식만 사용한다.
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
            public Label StatsLabel;
            public Label FlagsLabel;
        }

        private VisualElement _root;
        private Button _closeButton;

        private Label _globalStatusBanner;

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

        private VisualElement _rankingThreat;
        private VisualElement _rankingInfected;

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

        /// <summary>패널이 열려 있는 동안만 이벤트에 반응해서 다시 그린다 — 닫혀 있을 땐 갱신
        /// 비용 자체를 안 쓰기 위함(기존과 동일한 가드).</summary>
        private bool _isShown;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _root = root.Q<VisualElement>("status-root");
            _closeButton = root.Q<Button>("close-button");

            _globalStatusBanner = root.Q<Label>("global-status-banner");

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

            _rankingThreat = root.Q<VisualElement>("ranking-threat");
            _rankingInfected = root.Q<VisualElement>("ranking-infected");

            _statusList = root.Q<VisualElement>("status-list");

            _closeButton.RegisterCallback<ClickEvent>(_ => Hide());

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
                return ("WORLD COLLAPSE IMMINENT", "global-status-banner--dead");
            if (infectionRate >= 0.5f)
                return ("GLOBAL PANDEMIC", "global-status-banner--danger");
            if (infectionRate >= 0.05f || mortalityStage == WorldMortalityStage.WorldThreatened)
                return ("OUTBREAK EXPANDING", "global-status-banner--infected");
            return ("CONTAINMENT SUCCESS", "global-status-banner--info");
        }

        private static float WorldInfectionRate(WorldState state)
        {
            long living = Math.Max(0L, state.totalPopulation - state.deadCount);
            return living > 0 ? (float)state.infectedCount / living : 0f;
        }

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

            AddDataRow(_worldSummaryRows, "세계 인구", $"{total:N0}");
            AddDataRow(_worldSummaryRows, "총 감염자", $"{infected:N0}", "data-value--infected");
            AddDataRow(_worldSummaryRows, "총 사망자", $"{dead:N0}", "data-value--dead");
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
            int extinctCountries = countries.Count(c => c.GetCollapseStage() == CountryCollapseStage.Extinct);

            AddDataRow(_infectionSummaryRows, "감염 국가", $"{infectedCountries} / {total}",
                       infectedCountries > 0 ? "data-value--infected" : "data-value--info");
            AddDataRow(_infectionSummaryRows, "무감염 국가", $"{total - infectedCountries}", "data-value--info");
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
        // 랭킹 — 종합 위협도 / 감염자 각 TOP 10
        // ------------------------------------------------------------

        /// <summary>[통합, 2026-07-11] 기존엔 감염자/감염률/치사율/의료부하 4개 랭킹(40행)이
        /// 각각 독립 리스트였다 — 정보량만 늘어나고 "이 나라가 종합적으로 얼마나 위험한지"는
        /// 플레이어가 4개 리스트를 직접 대조해야 알 수 있었다. 감염률/치사율/의료부하 3개를
        /// ThreatIndex() 가중합 하나로 통합하고, 감염자 TOP10(원시 수치, 규모 파악용)만 별도로
        /// 남겨 랭킹 섹션을 4개→2개로 줄였다.</summary>
        private void PopulateRankings(IReadOnlyList<Country> countries)
        {
            RebuildTopRows(_rankingThreat, countries,
                c => (double)ThreatIndex(c),
                c => $"{ThreatIndex(c) * 100f:F0}",
                c => ThreatIndex(c) >= 0.5f ? "data-value--danger" : "data-value--infected");

            RebuildTopRows(_rankingInfected, countries,
                c => (double)c.infectedCount,
                c => $"{c.infectedCount:N0}",
                _ => "data-value--infected");
        }

        /// <summary>종합 위협도(0~1) — 감염률 40% + 치사율(추정) 30% + 의료 부하 30% 가중합.
        /// 세 값 모두 기존 계산식(InfectionRatio/CaseFatalityRate/MedicalLoad) 재사용, 신규 데이터
        /// 없음. 가중치는 "감염 확산이 1차 위협, 중증도·의료 붕괴가 2차 위협"이라는 설계 의도로
        /// 잡은 제안값 — 플레이테스트로 조정 필요(Docs/QA_Checklist.md 참고).</summary>
        private static float ThreatIndex(Country country) =>
            InfectionRatio(country) * 0.4f + CaseFatalityRate(country) * 0.3f + MedicalLoad(country) * 0.3f;

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

        /// <summary>CountryPopupController.CaseFatalityRate()와 동일 공식(사망/(사망+감염) 근사치)을
        /// 독립적으로 복제 — CountryPopupController는 이번 작업에서 수정 대상이 아니라 공유 헬퍼로
        /// 추출하지 않고 그대로 복제했다(두 화면이 서로 의존하지 않도록).</summary>
        private static float CaseFatalityRate(Country country)
        {
            long denominator = country.deadCount + country.infectedCount;
            return denominator > 0 ? (float)country.deadCount / denominator : 0f;
        }

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
        /// 고정되므로(48개), 최초 1회 생성 후에는 재생성이 필요 없다(기존 동작 그대로 유지).</summary>
        private void EnsureRowsBuilt()
        {
            if (_rowsByCountryId.Count > 0) return;
            if (_statusList == null) return;

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            _statusList.Clear();
            _rowsByCountryId.Clear();

            foreach (var country in countries)
            {
                var refs = BuildRow(country);
                _rowsByCountryId[country.id] = refs;
                _statusList.Add(refs.Row);
            }
        }

        private RowRefs BuildRow(Country country)
        {
            var row = new VisualElement();
            row.AddToClassList("status-row");

            var nameLabel = new Label();
            nameLabel.AddToClassList("status-row__name");
            row.Add(nameLabel);

            var statsLabel = new Label();
            statsLabel.AddToClassList("status-row__detail");
            row.Add(statsLabel);

            var flagsLabel = new Label();
            flagsLabel.AddToClassList("status-row__detail");
            row.Add(flagsLabel);

            return new RowRefs { Row = row, NameLabel = nameLabel, StatsLabel = statsLabel, FlagsLabel = flagsLabel };
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

            refs.NameLabel.text = $"{country.name} — {StageLabel(stage)}";

            refs.StatsLabel.text =
                $"인구 {country.population:N0} · 감염 {infectionRatio:P0} · 사망 {deadRatio:P0} · 의료 {DevLabel(country.developmentLevel)}";
            refs.StatsLabel.EnableInClassList("status-row__detail--danger", stage >= CountryCollapseStage.Disorder);

            refs.FlagsLabel.text =
                $"공항 {(country.isAirportOpen ? "개방" : "폐쇄")} · " +
                $"항구 {(country.isPortOpen ? "개방" : "폐쇄")} · " +
                $"국경 {(country.isBorderClosed ? "봉쇄" : "개방")}";

            refs.Row.EnableInClassList("status-row--safe", bucket == StatusBucket.Safe);
            refs.Row.EnableInClassList("status-row--warning", bucket == StatusBucket.Warning);
            refs.Row.EnableInClassList("status-row--danger", bucket == StatusBucket.Danger);
            refs.Row.EnableInClassList("status-row--collapse", bucket == StatusBucket.Collapse);
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

        private static string DevLabel(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "선진국",
            DevelopmentLevel.Mid => "개발도상국",
            DevelopmentLevel.Low => "저개발국",
            _ => level.ToString()
        };
    }
}
