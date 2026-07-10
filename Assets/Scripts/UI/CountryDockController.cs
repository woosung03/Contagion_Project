using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 우측 상단 "Country Dock" — 선택한 국가 정보를 지도 위에 상시 표시한다. HTML 프로토타입
    /// v2(Frostpunk/HOI4 참고)의 country-dock을 실제 UI Toolkit HUD로 이식한 것.
    ///
    /// CountryPopupController(모달 팝업, Step 28-2 이후 클릭 트리거가 제거돼 죽은 코드)와 달리
    /// 이 컨트롤러는 Hide()로 감추지 않는다 — 국가를 선택하지 않았을 때는 안내 문구/플레이스홀더로
    /// 되돌아갈 뿐 패널 자체는 항상 보인다("상시 표시"). HudController/NewsFeedController와 같은
    /// UIDocument(GameObject)에 붙는다 — Hud GameObject에 이 컴포넌트를 Add Component로 추가하는
    /// 작업은 Unity 에디터에서 수동으로 해야 한다(unity-editor-task.md 참고).
    ///
    /// 데이터 바인딩 로직은 CountryPopupController.Populate()와 사실상 동일한 소스(Country.cs 필드)를
    /// 쓴다. 항목: 인구/감염률/사망률/의료수준/국경(기후는 화면 공간 대비 우선순위가 낮아 생략) +
    /// 붕괴단계/항공·항구 상태(국가현황 패널과 중복 정보를 줄이기 위해 이관).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountryDockController : MonoBehaviour
    {
        private Label _nameLabel;
        private Label _populationValue;
        private Label _infectedRateValue;
        private Label _deadRateValue;
        private Label _healthValue;
        private Label _borderValue;
        private Label _stageValue;
        private Label _transportValue;

        private string _shownCountryId;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _nameLabel = root.Q<Label>("country-dock-name");
            _populationValue = root.Q<Label>("country-dock-population");
            _infectedRateValue = root.Q<Label>("country-dock-infected-rate");
            _deadRateValue = root.Q<Label>("country-dock-dead-rate");
            _healthValue = root.Q<Label>("country-dock-health");
            _borderValue = root.Q<Label>("country-dock-border");
            // 국가현황 패널(전체 목록)과 정보 중복을 줄이기 위해 이관 — 붕괴단계/항공·항구 상태.
            _stageValue = root.Q<Label>("country-dock-stage");
            _transportValue = root.Q<Label>("country-dock-transport");

            // 진단용 — Step 63과 같은 종류의 "Q<>() 바인딩 실패"가 재발했는지 조용히 넘어가지 않고
            // 바로 원인을 드러내기 위함(Step 68 재신고 조사). country-dock-stage/-transport는
            // 후속(Step 61) 추가 필드라 구버전 Hud.uxml에서는 없을 수 있어 진단 대상에서 제외.
            if (_nameLabel == null || _populationValue == null || _infectedRateValue == null ||
                _deadRateValue == null || _healthValue == null || _borderValue == null)
            {
                Debug.LogWarning("[CountryDockController] OnEnable — country-dock-* Label을 UXML에서 " +
                    $"찾지 못했습니다(name/population/infected/dead/health/border = " +
                    $"{_nameLabel != null}/{_populationValue != null}/{_infectedRateValue != null}/" +
                    $"{_deadRateValue != null}/{_healthValue != null}/{_borderValue != null}). " +
                    "Hud.uxml의 country-dock-* name 속성 또는 UIDocument Source Asset 배선을 확인하세요.");
            }

            Subscribe();
            ShowPlaceholder();
        }

        private void Start()
        {
            Subscribe();
        }

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
        }

        /// <summary>선택 중인 국가의 값이 매 틱 갱신될 때(다른 국가면 무시) 도킹 패널도 같이 갱신한다.</summary>
        private void HandleCountryChanged(Country country)
        {
            if (country.id == _shownCountryId) Populate(country);
        }

        private void ShowPlaceholder()
        {
            _shownCountryId = null;
            if (_nameLabel == null) return;

            _nameLabel.text = "국가를 선택하세요";
            _populationValue.text = "-";
            _infectedRateValue.text = "-";
            _deadRateValue.text = "-";
            _healthValue.text = "-";
            _borderValue.text = "-";
            if (_stageValue != null) _stageValue.text = "-";
            if (_transportValue != null) _transportValue.text = "-";
        }

        private void Populate(Country country)
        {
            // OnEnable에서 Q<>() 바인딩이 실패했으면(경고 로그 참고) 여기서 NRE로 죽는 대신 조용히
            // 빠져나온다 — ShowPlaceholder()와 동일한 가드 스타일.
            if (_nameLabel == null) return;

            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;
            float deadRatio = country.population > 0
                ? (float)country.deadCount / country.population
                : 0f;

            _nameLabel.text = country.name;
            _populationValue.text = FormatPopulation(country.population);
            _infectedRateValue.text = $"{infectionRatio * 100f:F1}%";
            _deadRateValue.text = $"{deadRatio * 100f:F2}%";
            _healthValue.text = DevLabel(country.developmentLevel);
            _borderValue.text = country.isBorderClosed ? "폐쇄" : "개방";

            if (_stageValue != null)
            {
                var stage = country.GetCollapseStage();
                _stageValue.text = StageLabel(stage);
                _stageValue.EnableInClassList("country-dock__value--danger", stage >= CountryCollapseStage.Disorder);
            }

            if (_transportValue != null)
            {
                _transportValue.text =
                    $"{(country.isAirportOpen ? "공항 개방" : "공항 폐쇄")} · {(country.isPortOpen ? "항구 개방" : "항구 폐쇄")}";
            }
        }

        /// <summary>인구수를 억/만 단위로 축약(예: 1,412,914,089 → "14.1억") — Country Dock은
        /// `.country-dock` 폭이 140px(CountryPopup 340px보다 훨씬 좁은 상시 표시 위젯)이라 10자리
        /// 정수를 그대로 쓰면 넘친다. 상세 수치가 필요하면 CountryPopup(N0 그대로 유지, Docs/DevLog.md
        /// Step 77)을 열어 확인하면 되므로 Dock은 규모 감만 전달하는 축약 표기로 통일한다.</summary>
        private static string FormatPopulation(long population)
        {
            if (population >= 100_000_000) return $"{population / 100_000_000f:0.#}억";
            if (population >= 10_000) return $"{population / 10_000f:0.#}만";
            return population.ToString("N0");
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
            DevelopmentLevel.High => "높음",
            DevelopmentLevel.Mid => "보통",
            DevelopmentLevel.Low => "낮음",
            _ => level.ToString()
        };
    }
}
