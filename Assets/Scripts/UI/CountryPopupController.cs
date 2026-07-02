using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>국가 정보 팝업. 설계 문서 7.3절.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountryPopupController : MonoBehaviour
    {
        private VisualElement _popupRoot;
        private Label _countryName;
        private Label _populationLabel;
        private Label _infectedLabel;
        private Label _deadLabel;
        private Label _healthLabel;
        private Label _climateLabel;
        private Label _statusLabel;
        private Button _closeButton;

        private string _shownCountryId;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _popupRoot = root.Q<VisualElement>("popup-root");
            _countryName = root.Q<Label>("country-name");
            _populationLabel = root.Q<Label>("population-label");
            _infectedLabel = root.Q<Label>("infected-label");
            _deadLabel = root.Q<Label>("dead-label");
            _healthLabel = root.Q<Label>("health-label");
            _climateLabel = root.Q<Label>("climate-label");
            _statusLabel = root.Q<Label>("status-label");
            _closeButton = root.Q<Button>("close-button");

            _closeButton.RegisterCallback<ClickEvent>(_ => Hide());

            Subscribe();
            Hide();
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
            _popupRoot.style.display = DisplayStyle.Flex;
            Populate(country);
        }

        private void HandleCountryChanged(Country country)
        {
            if (country.id == _shownCountryId) Populate(country);
        }

        public void Hide()
        {
            _shownCountryId = null;
            if (_popupRoot != null) _popupRoot.style.display = DisplayStyle.None;
        }

        private void Populate(Country country)
        {
            _countryName.text = country.name;
            _populationLabel.text = $"인구: {country.population:N0}";
            _infectedLabel.text = $"감염자: {country.infectedCount:N0}";
            _deadLabel.text = $"사망자: {country.deadCount:N0}";
            _healthLabel.text = $"의료 수준: {DevLabel(country.developmentLevel)}";
            _climateLabel.text = $"기후: {ClimateLabel(country.climate)}";
            _statusLabel.text = $"항공 {(country.isAirportOpen ? "개방" : "폐쇄")} · " +
                                 $"항구 {(country.isPortOpen ? "개방" : "폐쇄")} · " +
                                 $"국경 {(country.isBorderClosed ? "봉쇄" : "개방")}";
        }

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
