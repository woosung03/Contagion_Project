using System;
using System.Collections.Generic;
using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 국가 정보 팝업 — Bottom Sheet(화면 하단 고정, 지도를 가리지 않는 5초 확인용 UI)로 전환됨.
    /// <see cref="TacticalModalController"/>(Show/Hide/Footer 계약)를 그대로 상속하되, 이 클래스는
    /// modal-rows(detail-rows) 기반의 동적 행 생성(AddRow/ClearRows)을 더 이상 쓰지 않는다 —
    /// 새 CountryPopup.uxml은 quick-stat-grid 안에 8개 필드가 고정 슬롯으로 이미 존재하므로,
    /// CountryDockController.cs와 동일한 방식(OnEnable에서 이름으로 한 번만 Q&lt;Label&gt;() 캐싱 →
    /// Populate()는 텍스트/클래스만 갱신)으로 바꿨다.
    ///
    /// 도넛 차트(CountryDonutChart)/세계 순위(AddWorldRankRows)/이동 통제 상세(AddTransportRow)/
    /// 치사율·의료 부하·기후 등 기존 상세 정보는 이 화면 책임에서 제외됐다 — 해당 정보는
    /// "상세 보기"(modal-detail) 버튼을 눌러 이동하는 별도 상세 화면(CountryStatusPanel)이
    /// 담당한다. 버튼 클릭 시 <see cref="OnDetailRequested"/>를 발행하고, UIManager가 이를 구독해
    /// AppScreen 상태 머신(TransitionTo)으로 CountryStatusPanel을 연다.
    ///
    /// [주의] Country Dock(CountryDockController.cs)이 아직 폐지되지 않아, 국가를 탭하면 이 팝업과
    /// Country Dock이 여전히 동시에 갱신된다(Docs/DevLog.md Step 67에서 이미 확인된 기존 동작) —
    /// Dock 폐지는 별도 작업(P1)으로 남아 있다.
    /// </summary>
    public class CountryPopupController : TacticalModalController
    {
        private string _shownCountryId;
        private Country _shownCountry;

        /// <summary>modal-detail("상세 보기") 버튼 클릭 시 발행 — UIManager가 구독해 AppScreen
        /// 상태 머신을 통해 CountryStatusPanel을 연다.</summary>
        public event Action<Country> OnDetailRequested;

        private Button _detailButton;

        private Label _flagValue;
        private Label _continentValue;
        private Label _populationValue;
        private Label _infectedValue;
        private Label _deadValue;
        private Label _infectionRateValue;
        private Label _airportValue;
        private Label _seaportValue;
        private Label _medicalValue;
        private Label _borderStatusValue;

        private readonly Dictionary<string, Texture2D> _flagCache = new Dictionary<string, Texture2D>();

        /// <summary>CountryStatusPanelController.ContinentByCountryId와 동일한 데이터의 독립 사본이다
        /// — 그 필드는 private이고 CountryStatusPanelController.cs는 이번 작업 범위에서 수정 금지라
        /// 공유할 수 없다. Country.cs에는 대륙 필드가 없어(원본 컨트롤러의 동일한 코멘트 참고) 이
        /// 파일이 독자적으로 계산식/데이터를 복제해온 기존 관례(CaseFatalityRate 등)를 그대로 따른다.</summary>
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

        private static string ContinentOf(Country country) =>
            ContinentByCountryId.TryGetValue(country.id, out var continent) ? continent : "OTHER";

        protected override void OnEnable()
        {
            base.OnEnable();

            var root = GetComponent<UIDocument>().rootVisualElement;

            _flagValue = root.Q<Label>("popup-flag");
            _continentValue = root.Q<Label>("popup-continent");
            _populationValue = root.Q<Label>("population");
            _infectedValue = root.Q<Label>("infected");
            _deadValue = root.Q<Label>("dead");
            _infectionRateValue = root.Q<Label>("infection-rate");
            _airportValue = root.Q<Label>("airport");
            _seaportValue = root.Q<Label>("seaport");
            _medicalValue = root.Q<Label>("medical");
            _borderStatusValue = root.Q<Label>("border-status");
            _detailButton = root.Q<Button>("modal-detail");

            if (_flagValue == null || _continentValue == null || _populationValue == null ||
                _infectedValue == null || _deadValue == null || _infectionRateValue == null ||
                _airportValue == null || _seaportValue == null || _medicalValue == null ||
                _borderStatusValue == null || _detailButton == null)
            {
                Debug.LogWarning("[CountryPopupController] OnEnable — quick-stat-grid Label 중 일부를 " +
                    "찾지 못했습니다. CountryPopup.uxml의 name 속성(popup-flag/popup-continent/" +
                    "population/infected/dead/infection-rate/airport/seaport/medical/" +
                    "border-status/modal-detail)을 확인하세요.");
            }

            _detailButton?.RegisterCallback<ClickEvent>(_ => OnDetailRequested?.Invoke(_shownCountry));

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

                // 세계 순위 표시는 이 화면에서 제거됐지만(상세 화면으로 이관), 감염률 등 표시 중인
                // 국가 자신의 값은 여전히 매 틱 갱신돼야 하므로 OnCountryChanged 구독은 유지한다.
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

        private void HandleCountryClicked(Country country) => ShowCountry(country);

        /// <summary>국가 상세를 채우고 팝업을 연다 — 지도 클릭(HandleCountryClicked)과
        /// CountryStatusPanelController의 48개국 목록 행 클릭(UIManager 배선) 양쪽에서 재사용하는
        /// 공개 진입점(기존과 동일, 변경 없음).</summary>
        public void ShowCountry(Country country)
        {
            if (country == null) return;
            _shownCountryId = country.id;
            Populate(country);
            Show(country.name);
        }

        private void HandleCountryChanged(Country country)
        {
            if (country.id == _shownCountryId) Populate(country);
        }

        private void HandleWorldStateChanged(WorldState _)
        {
            if (_shownCountryId == null) return;
            var country = WorldDataManager.Instance?.GetCountry(_shownCountryId);
            if (country != null) Populate(country);
        }

        public override void Hide()
        {
            _shownCountryId = null;
            _shownCountry = null;
            base.Hide();
        }

        /// <summary>quick-stat-grid 8개 필드 + 헤더(국기/대륙) 갱신. 기존의 ClearRows()+AddRow() 반복
        /// 생성 방식 대신, OnEnable에서 캐싱해둔 고정 Label들의 텍스트/클래스만 바꾼다.</summary>
        private void Populate(Country country)
        {
            _shownCountry = country;

            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;

            if (_flagValue != null)
                _flagValue.style.backgroundImage = new StyleBackground(GetFlagTexture(country.id));
            if (_continentValue != null)
                _continentValue.text = ContinentOf(country);

            if (_populationValue != null) _populationValue.text = $"{country.population:N0}";
            if (_infectedValue != null) _infectedValue.text = $"{country.infectedCount:N0}";
            if (_deadValue != null) _deadValue.text = $"{country.deadCount:N0}";
            if (_infectionRateValue != null) _infectionRateValue.text = $"{infectionRatio * 100f:F1}%";

            if (_airportValue != null)
            {
                _airportValue.text = country.isAirportOpen ? "개방" : "폐쇄";
                ApplySeverityClass(_airportValue, country.isAirportOpen ? "badge-tag--success" : "badge-tag--warning");
            }

            if (_seaportValue != null)
            {
                _seaportValue.text = country.isPortOpen ? "개방" : "폐쇄";
                ApplySeverityClass(_seaportValue, country.isPortOpen ? "badge-tag--success" : "badge-tag--warning");
            }

            if (_medicalValue != null)
            {
                _medicalValue.text = DevLabel(country.developmentLevel);
                ApplySeverityClass(_medicalValue, DevValueClass(country.developmentLevel));
            }

            if (_borderStatusValue != null)
            {
                _borderStatusValue.text = country.isBorderClosed ? "폐쇄" : "개방";
                ApplySeverityClass(_borderStatusValue, country.isBorderClosed ? "badge-tag--warning" : "badge-tag--success");
            }
        }

        /// <summary>Resources/Flags/{countryId}.png를 로드해 캐시한다 — CountrySelectController.
        /// GetFlagTexture()와 동일한 패턴의 독립 사본(그 메서드는 private이라 공유 불가, 이 파일
        /// 기존 관례상 소규모 헬퍼는 복제해 쓴다). popup-flag가 UXML상 Label 엘리먼트라 텍스트가 아닌
        /// backgroundImage 스타일로 그린다 — Label도 VisualElement를 상속하므로 문제없이 동작하지만,
        /// 의미상으로는 VisualElement가 더 적절할 수 있음(TODO, UXML 변경 필요).</summary>
        private Texture2D GetFlagTexture(string countryId)
        {
            if (string.IsNullOrEmpty(countryId)) return null;
            if (_flagCache.TryGetValue(countryId, out var cached)) return cached;

            var tex = Resources.Load<Texture2D>($"Flags/{countryId}");
            _flagCache[countryId] = tex;
            return tex;
        }

        /// <summary>badge-tag 계열 severity 수정자 클래스를 서로 배타적으로 적용한다 — 이전 값이
        /// 남아있으면 색이 겹쳐 보일 수 있어 항상 전부 지운 뒤 하나만(또는 없음) 붙인다.</summary>
        private static readonly string[] SeverityModifierClasses =
        {
            "badge-tag--success", "badge-tag--warning", "badge-tag--danger", "badge-tag--info"
        };

        private static void ApplySeverityClass(Label label, string cssClass)
        {
            foreach (var cls in SeverityModifierClasses)
                label.RemoveFromClassList(cls);
            if (!string.IsNullOrEmpty(cssClass))
                label.AddToClassList(cssClass);
        }

        /// <summary>CountrySelectController.DevValueClass()와 동일 규약 — 의료 수준을 severity로
        /// 재해석. 반환 클래스명을 이번 UXML의 badge-tag 블록에 맞춰 data-value--* 대신
        /// badge-tag--*로 바꿨다(기존 modal-rows 계열 클래스는 이 화면에 더 이상 없음).</summary>
        private static string DevValueClass(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "badge-tag--success",
            DevelopmentLevel.Low => "badge-tag--danger",
            _ => "badge-tag--warning"
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
