using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;

namespace Contagion.UI
{
    /// <summary>
    /// 국가 정보 팝업 — Tactical Modal 공용 프레임(<see cref="TacticalModalController"/>) 위에서
    /// 국가 데이터 매핑만 담당하는 얇은 래퍼. Docs/UI_Design.md 12절(승격 결론)/13절(구현 패턴 b).
    /// UXML/USS는 CountryPopup.uxml/.uss — modal-root(+tactical-panel)/modal-title/modal-close/
    /// modal-rows 계약을 따르도록 승격됨.
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

        protected override void OnEnable()
        {
            base.OnEnable();
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
            }
        }

        private void OnDisable()
        {
            if (WorldMap.Instance != null)
                WorldMap.Instance.OnCountryClicked -= HandleCountryClicked;
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
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

        public override void Hide()
        {
            _shownCountryId = null;
            base.Hide();
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
        ///    severity 색상(개방=info, 폐쇄/봉쇄=danger)을 입혀 다른 행과 표현 문법을 통일했다.</summary>
        private void Populate(Country country)
        {
            ClearRows();
            AddRow("인구", $"{country.population:N0}");
            AddRow("감염자", $"{country.infectedCount:N0}", "data-value--infected");
            AddRow("사망자", $"{country.deadCount:N0}", "data-value--dead");
            AddRow("의료 수준", DevLabel(country.developmentLevel), DevValueClass(country.developmentLevel));
            AddRow("기후", ClimateLabel(country.climate));
            AddRow("공항", country.isAirportOpen ? "개방" : "폐쇄",
                   country.isAirportOpen ? "data-value--info" : "data-value--danger");
            AddRow("항구", country.isPortOpen ? "개방" : "폐쇄",
                   country.isPortOpen ? "data-value--info" : "data-value--danger");
            AddRow("국경", country.isBorderClosed ? "봉쇄" : "개방",
                   country.isBorderClosed ? "data-value--danger" : "data-value--info");
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
