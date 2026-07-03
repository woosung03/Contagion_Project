using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 18개국 현재 상태를 한 화면 스크롤 리스트로 보여주는 패널. Step 28-2에서 국가 클릭 팝업
    /// (CountryPopupController)을 대체하기 위해 추가했다 — 지도가 화면보다 넓고(Step 28) 국가가
    /// 촘촘히 배치돼 있어(Step 27/28-2) 개별 국가를 정확히 탭하기 어려운 문제를, "지도에서 직접
    /// 클릭" 대신 "버튼 하나로 전체 목록을 훑어보기" 방식으로 우회했다.
    ///
    /// CountryPopupController.Populate()와 항목 구성을 최대한 맞춰서(인구/감염/사망/의료수준/
    /// 항공·항구·국경) 정보 손실 없이 UX만 바꿨다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountryStatusPanelController : MonoBehaviour
    {
        private VisualElement _root;
        private ScrollView _scroll;
        private Button _closeButton;

        /// <summary>패널이 열려 있는 동안만 WorldDataManager.OnCountryChanged에 반응해서 다시 그린다
        /// — 매 틱마다 18개 로우를 갈아엎는 비용을 닫혀 있을 땐 안 쓰기 위함.</summary>
        private bool _isShown;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _root = root.Q<VisualElement>("status-root");
            _scroll = root.Q<ScrollView>("status-scroll");
            _closeButton = root.Q<Button>("close-button");

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
            }
        }

        private void OnDisable()
        {
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
        }

        private void HandleCountryChanged(Country country)
        {
            if (_isShown) Populate();
        }

        public void Show()
        {
            _isShown = true;
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            Populate();
        }

        public void Hide()
        {
            _isShown = false;
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        private void Populate()
        {
            if (_scroll == null) return;
            _scroll.Clear();

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            foreach (var country in countries)
                _scroll.Add(BuildRow(country));
        }

        private VisualElement BuildRow(Country country)
        {
            var row = new VisualElement();
            row.AddToClassList("status-row");

            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;
            float deadRatio = country.population > 0
                ? (float)country.deadCount / country.population
                : 0f;

            var stage = country.GetCollapseStage();

            var nameLabel = new Label($"{country.name} — {StageLabel(stage)}");
            nameLabel.AddToClassList("status-row__name");
            row.Add(nameLabel);

            var statsLabel = new Label(
                $"인구 {country.population:N0} · 감염 {infectionRatio:P0} · 사망 {deadRatio:P0} · 의료 {DevLabel(country.developmentLevel)}");
            statsLabel.AddToClassList("status-row__detail");
            if (stage >= CountryCollapseStage.Disorder)
                statsLabel.AddToClassList("status-row__detail--danger");
            row.Add(statsLabel);

            var flagsLabel = new Label(
                $"항공 {(country.isAirportOpen ? "개방" : "폐쇄")} · " +
                $"항구 {(country.isPortOpen ? "개방" : "폐쇄")} · " +
                $"국경 {(country.isBorderClosed ? "봉쇄" : "개방")}");
            flagsLabel.AddToClassList("status-row__detail");
            row.Add(flagsLabel);

            return row;
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
