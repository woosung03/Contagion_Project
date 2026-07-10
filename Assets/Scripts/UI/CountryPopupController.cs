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
    /// 국가 정보 팝업 — Tactical Modal 공용 프레임(<see cref="TacticalModalController"/>) 위에서
    /// 국가 데이터 매핑만 담당하는 얇은 래퍼. Docs/UI_Design.md 12절(승격 결론)/13절(구현 패턴 b).
    /// UXML/USS는 CountryPopup.uxml/.uss — modal-root(+tactical-panel)/modal-title/modal-close/
    /// modal-rows 계약을 따르도록 승격됨.
    ///
    /// [국가 대시보드 1차 확장] Docs/CountryStatus_Dashboard_Investigation.md 조사 결과 중 "즉시
    /// 구현 가능" 4항목(감염 상태 도넛 차트/감염 통계/의료 시스템 부하/세계 순위)을 반영했다.
    /// "최근 국가 이벤트"는 조사 문서 2.5절에 정리된 대로 EventManager/HumanResistanceManager
    /// 확장이 선행돼야 해서 이번 범위에서 제외했다.
    ///
    /// 주의(발견 사항, Docs/DevLog.md Step 67 참고): CountryDockController.cs 주석은 이 컨트롤러를
    /// "Step 28-2 이후 클릭 트리거가 제거된 죽은 코드"로 서술하지만, 실제로는 HUD 리디자인 때
    /// CountryView.OnMouseUpAsButton()이 WorldMap.HandleCountryClicked()를 다시 호출하도록
    /// 되살아났고(의도한 소비자는 CountryDockController였음) 이 클래스의 WorldMap.OnCountryClicked
    /// 구독은 지워지지 않은 채로 남아 있었다 — 씬의 CountryPopupUI GameObject도 활성 상태(m_IsActive: 1)
    /// 라 국가를 탭하면 Country Dock과 이 팝업이 동시에 뜬다. 이번 작업은 승격(스타일/구조 전환)
    /// 범위이므로 이 동작 자체는 그대로 유지했다 — 자동 팝업을 유지할지/끌지는 별도 결정 필요.
    /// </summary>
    public class CountryPopupController : TacticalModalController
    {
        private string _shownCountryId;

        private CountryDonutChart _donutChart;
        private Label _legendHealthyValue;
        private Label _legendInfectedValue;
        private Label _legendDeadValue;

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildDonutAndLegend();
            Subscribe();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (WorldMap.Instance != null)
            {
                WorldMap.Instance.OnCountryClicked -= HandleCountryClicked;
                WorldMap.Instance.OnCountryClicked += HandleCountryClicked;
            }

            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
                WorldDataManager.Instance.OnCountryChanged += HandleCountryChanged;

                // 세계 순위(4개 신규 항목 중 하나)는 "표시 중인 국가 자신의 값"이 안 바뀌어도 다른
                // 국가들의 값 변화만으로 뒤바뀔 수 있다 — OnCountryChanged(국가별, 틱당 최대 20~30회)만
                // 구독하면 다른 나라가 앞질러도 순위가 갱신 안 되고 고정돼 보인다. OnWorldStateChanged는
                // 틱당 정확히 1회만 발행되므로(WorldDataManager.RecalculateWorldTotals) 이 이벤트에도
                // 반응해 팝업이 열려 있는 동안 전체를 다시 그린다 — 48개국 정렬 3회 정도를 틱당 1회
                // 수준으로만 계산하면 되므로 성능 영향 없음(Docs/CountryStatus_Dashboard_Investigation.md
                // 2.4절).
                WorldDataManager.Instance.OnWorldStateChanged -= HandleWorldStateChanged;
                WorldDataManager.Instance.OnWorldStateChanged += HandleWorldStateChanged;
            }
        }

        private void OnDisable()
        {
            if (WorldMap.Instance != null)
                WorldMap.Instance.OnCountryClicked -= HandleCountryClicked;
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
                WorldDataManager.Instance.OnWorldStateChanged -= HandleWorldStateChanged;
            }
        }

        private void HandleCountryClicked(Country country)
        {
            _shownCountryId = country.id;
            Populate(country);
            Show(country.name);
        }

        private void HandleCountryChanged(Country country)
        {
            if (country.id == _shownCountryId) Populate(country);
        }

        /// <summary>틱당 1회 발행 — 팝업이 열려 있을 때만 세계 순위 갱신 목적으로 전체를 다시 그린다.</summary>
        private void HandleWorldStateChanged(WorldState _)
        {
            if (_shownCountryId == null) return;
            var country = WorldDataManager.Instance?.GetCountry(_shownCountryId);
            if (country != null) Populate(country);
        }

        public override void Hide()
        {
            _shownCountryId = null;
            base.Hide();
        }

        /// <summary>도넛 차트 엘리먼트를 찾아 <see cref="CountryDonutChart"/>를 붙이고, 범례 3줄
        /// (건강/감염/사망)을 최초 1회만 생성한다(이후에는 UpdateDonutAndLegend()가 텍스트만 갱신 —
        /// CountryStatusPanelController의 "행 캐싱" 패턴과 동일한 이유로, 매 갱신마다 VisualElement를
        /// 새로 만들지 않기 위함).</summary>
        private void BuildDonutAndLegend()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var donutTarget = root.Q<VisualElement>("modal-donut");
            var legendContainer = root.Q<VisualElement>("modal-donut-legend");

            if (donutTarget == null || legendContainer == null)
            {
                Debug.LogWarning("[CountryPopupController] modal-donut/modal-donut-legend를 UXML에서 " +
                    "찾지 못했습니다 — CountryPopup.uxml의 popup-donut-row 배선을 확인하세요.");
                return;
            }

            // Theme.uss --color-status-info/--color-status-infected/--color-status-dead와 동일한
            // RGB(하드코딩) — Painter2D는 USS 커스텀 프로퍼티를 직접 읽지 못해 C# 쪽에 값을 복제해야
            // 한다(HudController가 HudSparkline에 Color를 직접 넘기는 것과 동일한 방식).
            _donutChart = new CountryDonutChart(
                donutTarget,
                healthyColor: new Color(140f / 255f, 210f / 255f, 255f / 255f),
                infectedColor: new Color(255f / 255f, 170f / 255f, 90f / 255f),
                deadColor: new Color(220f / 255f, 90f / 255f, 90f / 255f));

            legendContainer.Clear();
            _legendHealthyValue = BuildLegendRow(legendContainer, "건강", "popup-donut-legend__dot--healthy");
            _legendInfectedValue = BuildLegendRow(legendContainer, "감염", "popup-donut-legend__dot--infected");
            _legendDeadValue = BuildLegendRow(legendContainer, "사망", "popup-donut-legend__dot--dead");
        }

        private static Label BuildLegendRow(VisualElement container, string label, string dotClass)
        {
            var row = new VisualElement();
            row.AddToClassList("popup-donut-legend__row");

            var labelGroup = new VisualElement();
            labelGroup.AddToClassList("popup-donut-legend__label-group");

            var dot = new VisualElement();
            dot.AddToClassList("popup-donut-legend__dot");
            dot.AddToClassList(dotClass);
            labelGroup.Add(dot);

            var labelEl = new Label(label);
            labelEl.AddToClassList("popup-donut-legend__label");
            labelGroup.Add(labelEl);

            row.Add(labelGroup);

            var valueEl = new Label();
            valueEl.AddToClassList("popup-donut-legend__value");
            row.Add(valueEl);

            container.Add(row);
            return valueEl;
        }

        /// <summary>기존 6개 popup-row(Label 개별 대입)를 data-row 8줄로 전환 — 정보 항목은 동일,
        /// 표현만 판독행으로 통일(Country Dock/CountrySelect와 동일 severity 색상 규약 재사용).
        ///
        /// 오버플로우 조사 결과(Docs/QA_Checklist.md) 반영 — 두 가지를 고쳤다:
        /// 1) 인구는 정확한 전체 자릿수(N0)를 유지한다 — CountryPopup은 상세 정보 창이라 축약 없이
        ///    전체 숫자를 보여주는 게 목적에 맞다(억/만 축약 표기는 Country Dock 쪽으로 이관됨,
        ///    Docs/DevLog.md Step 77). 오버플로우 자체는 Tactical.uss의 wrap/shrink 공용 수정(Step 76)
        ///    으로 이미 해결돼 있어 N0로 되돌려도 320px보다 넓어진 340px 폭 안에서 문제없이 줄바꿈된다.
        /// 2) "공항/항구/국경" 한 줄로 이어붙였던 값을 감염자/사망자처럼 개별 data-row 3개로 분리하고
        ///    severity 색상(개방=info, 폐쇄/봉쇄=danger)을 입혀 다른 행과 표현 문법을 통일했다.
        ///
        /// [국가 대시보드 1차 확장] 기존 "감염자"/"사망자" 절대값 data-row 2줄은 위쪽 도넛 범례가
        /// 정확한 수치+비율을 이미 보여주므로 제거하고, 그 자리에 감염 통계(감염률/치사율 추정/
        /// 생존자 수)·의료 시스템 부하·세계 순위를 새로 추가했다.</summary>
        private void Populate(Country country)
        {
            UpdateDonutAndLegend(country);

            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;

            ClearRows();
            AddRow("인구", $"{country.population:N0}");
            AddRow("생존자 수", $"{country.LivingPopulation:N0}");
            AddRow("감염률", $"{infectionRatio * 100f:F1}%",
                   infectionRatio >= 0.5f ? "data-value--danger" : "data-value--infected");
            float caseFatalityRate = CaseFatalityRate(country);
            AddRow("치사율(추정)", $"{caseFatalityRate * 100f:F1}%",
                   caseFatalityRate >= 0.3f ? "data-value--dead" : null);

            var (loadLabel, loadClass) = MedicalLoadStatus(country, infectionRatio);
            AddRow("의료 시스템 상태", loadLabel, loadClass);

            AddRow("의료 수준", DevLabel(country.developmentLevel), DevValueClass(country.developmentLevel));
            AddRow("기후", ClimateLabel(country.climate));

            AddWorldRankRows(country);

            AddSectionCaption("이동 통제");
            AddTransportRow("공항", country.isAirportOpen, "개방", "폐쇄");
            AddTransportRow("항구", country.isPortOpen, "개방", "폐쇄");
            AddTransportRow("국경", !country.isBorderClosed, "개방", "봉쇄");
        }

        /// <summary>개방/폐쇄(또는 봉쇄) 이분법 상태 한 줄 — data-value 색(info/danger)뿐 아니라
        /// data-row 좌측 accent bar(data-row--open/--closed)까지 같이 입혀서, 스크롤 없이 훑어봐도
        /// "이 나라가 지금 봉쇄 중인지"가 한눈에 들어오게 한다(2차 다듬기 — 시인성 개선 요청 반영,
        /// 아이콘 폰트 글리프 대신 텍스트+accent bar 조합을 택한 이유는 AddSectionCaption() 주석 참고).</summary>
        private void AddTransportRow(string label, bool isOpen, string openText, string closedText)
        {
            AddRow(label, isOpen ? openText : closedText,
                   isOpen ? "data-value--info" : "data-value--danger",
                   isOpen ? "data-row--open" : "data-row--closed");
        }

        /// <summary>도넛 차트 값 갱신 + 범례 3줄(건강/감염/사망) 텍스트 갱신 — Label 인스턴스는
        /// BuildDonutAndLegend()에서 이미 만들어져 있으므로 여기서는 텍스트만 바꾼다.</summary>
        private void UpdateDonutAndLegend(Country country)
        {
            long healthy = country.SusceptibleCount;
            long infected = country.infectedCount;
            long dead = country.deadCount;

            _donutChart?.SetValues(healthy, infected, dead);

            long total = Math.Max(1, country.population); // 인구 0인 국가는 사실상 없지만 0 나누기 방지
            if (_legendHealthyValue != null)
                _legendHealthyValue.text = $"{healthy:N0} ({healthy * 100f / total:F0}%)";
            if (_legendInfectedValue != null)
                _legendInfectedValue.text = $"{infected:N0} ({infected * 100f / total:F0}%)";
            if (_legendDeadValue != null)
                _legendDeadValue.text = $"{dead:N0} ({dead * 100f / total:F0}%)";
        }

        /// <summary>치사율(추정) — 이 게임엔 "감염 후 자연 회복" 개념이 없어(치료제 100% 완성 시
        /// 세계 단위로 일괄 박멸, SimulationManager.EradicatePathogen 참고) 누적 감염 경험 인구를
        /// 따로 세지 않는다. 정확한 역학적 치사율(누적 사망 / 누적 감염)은 현재 데이터로 계산할 수
        /// 없어(Docs/CountryStatus_Dashboard_Investigation.md 2.2절), 현재 스냅샷 기준 근사치
        /// (사망 / (사망+현재 감염))로 대체하고 라벨에 "(추정)"을 명시한다.</summary>
        private static float CaseFatalityRate(Country country)
        {
            long denominator = country.deadCount + country.infectedCount;
            return denominator > 0 ? (float)country.deadCount / denominator : 0f;
        }

        /// <summary>감염 비율 × (1 - 의료 수준)으로 "의료 체계가 지금 이 순간 버티고 있는지"를
        /// 계산한다. 사망률 기준 CountryCollapseStage(이미 있음, 사회 전체 붕괴 축)와는 다른 축 —
        /// 이건 "의료 시스템 부하" 축이라 라벨/이름을 겹치지 않게 분리했다(DESIGN.md의 severity 축과
        /// 노드 상태 축을 분리하라는 원칙을 그대로 적용). 임계값은 조사 문서의 제안값이며 플레이테스트로
        /// 조정 필요.</summary>
        private static (string label, string valueClass) MedicalLoadStatus(Country country, float infectionRatio)
        {
            float load = infectionRatio * (1f - country.HealthLevel);

            if (load < 0.1f) return ("정상", "data-value--info");
            if (load < 0.3f) return ("주의", "data-value--infected");
            if (load < 0.6f) return ("과부하", "data-value--danger");
            return ("붕괴", "data-value--dead");
        }

        /// <summary>48개국 기준 감염자/사망자/감염률 3개 지표 순위를 각각 별도 data-row로 추가한다.
        /// (2차 다듬기 — 애초 "감염자 N위 · 사망자 N위 · 감염률 N위" 한 줄 압축이 다른 data-row와
        /// 표현 문법이 달라 오히려 읽기 불편하다는 피드백으로 분리) WorldDataManager.Countries는
        /// 이미 국가현황 패널이 순회하는 것과 같은 리스트라 접근 비용이 없고, 48개 정렬 3회는 팝업이
        /// 열려 있을 때만(그것도 틱당 1회, HandleWorldStateChanged 참고) 계산되므로 성능 영향이
        /// 없다(Docs/CountryStatus_Dashboard_Investigation.md 2.4절).</summary>
        private void AddWorldRankRows(Country country)
        {
            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null || countries.Count == 0)
            {
                AddRow("감염자 순위", "-");
                AddRow("사망자 순위", "-");
                AddRow("감염률 순위", "-");
                return;
            }

            int total = countries.Count;
            int infectedRank = RankOf(countries, country.id, c => c.infectedCount);
            int deadRank = RankOf(countries, country.id, c => c.deadCount);
            int rateRank = RankOf(countries, country.id,
                c => c.LivingPopulation > 0 ? (double)c.infectedCount / c.LivingPopulation : 0d);

            AddRow("감염자 순위", $"{infectedRank}위 / {total}개국");
            AddRow("사망자 순위", $"{deadRank}위 / {total}개국");
            AddRow("감염률 순위", $"{rateRank}위 / {total}개국");
        }

        private static int RankOf<TKey>(IReadOnlyList<Country> countries, string id, Func<Country, TKey> keySelector)
        {
            var sorted = countries.OrderByDescending(keySelector).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].id == id) return i + 1;
            }
            return sorted.Count;
        }

        /// <summary>CountrySelectController.DevValueClass()와 동일 규약 — 의료 수준을 severity로 재해석.</summary>
        private static string DevValueClass(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "data-value--info",
            DevelopmentLevel.Low => "data-value--danger",
            _ => null
        };

        private static string DevLabel(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "선진국",
            DevelopmentLevel.Mid => "개발도상국",
            DevelopmentLevel.Low => "저개발국",
            _ => level.ToString()
        };

        private static string ClimateLabel(ClimateType climate) => climate switch
        {
            ClimateType.Arid => "건조",
            ClimateType.Temperate => "온대",
            ClimateType.Cold => "한대",
            ClimateType.Humid => "습윤",
            _ => climate.ToString()
        };
    }
}
