using System.Collections.Generic;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 48개국 현재 상태를 한 화면 스크롤 리스트로 보여주는 패널. Step 28-2에서 국가 클릭 팝업
    /// (CountryPopupController)을 대체하기 위해 추가했다 — 지도가 화면보다 넓고(Step 28) 국가가
    /// 촘촘히 배치돼 있어(Step 27/28-2) 개별 국가를 정확히 탭하기 어려운 문제를, "지도에서 직접
    /// 클릭" 대신 "버튼 하나로 전체 목록을 훑어보기" 방식으로 우회했다.
    ///
    /// CountryPopupController.Populate()와 항목 구성을 최대한 맞춰서(인구/감염/사망/의료수준/
    /// 항공·항구/국경) 정보 손실 없이 UX만 바꿨다.
    ///
    /// [렉 수정 — Country Dock 리팩터 세션] 원래 구현은 WorldDataManager.OnCountryChanged가 올
    /// 때마다(패널이 열려 있는 동안) ScrollView를 Clear()하고 48개 행 전체를 VisualElement/Label
    /// 째로 새로 생성했다. 이 이벤트는 SimulationManager가 매 틱 "감염 중인 국가마다" 개별 호출하므로
    /// (감염국 20~30개면 틱당 20~30회), 국가 하나 값이 바뀌어도 리스트 전체를 국가 수만큼 반복해서
    /// 다시 그리는 셈이 되어 패널이 열려 있는 동안 심한 프레임 드랍이 발생했다.
    /// 지금은 행을 국가 id로 캐싱해 최초 1회만 생성하고, 이후에는 값이 바뀐 국가의 라벨 텍스트만
    /// 직접 갱신한다(Clear()+재생성 없음) — 갱신 1건당 비용이 O(48)에서 O(1)로 줄었다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountryStatusPanelController : MonoBehaviour
    {
        private class RowRefs
        {
            public VisualElement Row;
            public Label NameLabel;
            public Label StatsLabel;
            public Label FlagsLabel;
        }

        private VisualElement _root;
        private ScrollView _scroll;
        private Button _closeButton;

        /// <summary>국가 id → 이미 생성된 행 참조. 최초 Populate() 때 한 번만 채워지고, 이후에는
        /// Clear()/재생성 없이 이 캐시를 통해 해당 행의 라벨만 갱신한다.</summary>
        private readonly Dictionary<string, RowRefs> _rowsByCountryId = new();

        /// <summary>패널이 열려 있는 동안만 WorldDataManager.OnCountryChanged에 반응해서 다시 그린다
        /// — 닫혀 있을 땐 갱신 비용 자체를 안 쓰기 위함.</summary>
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

        /// <summary>매 틱 국가별로 개별 호출될 수 있는 이벤트 — 여기서는 절대 전체를 다시 그리지
        /// 않고, 바뀐 국가 한 줄만 갱신한다.</summary>
        private void HandleCountryChanged(Country country)
        {
            if (!_isShown) return;

            EnsureRowsBuilt();
            RefreshRow(country);
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

        /// <summary>패널을 열 때 한 번만 호출 — 행이 아직 없으면 생성하고, 전체 국가 값을 최신화한다.
        /// (이 전체 순회는 Show() 시 1회뿐이라 비용 문제가 없다. 매 틱 반복되던 것이 문제였다.)</summary>
        private void Populate()
        {
            if (_scroll == null) return;

            EnsureRowsBuilt();

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            foreach (var country in countries)
                RefreshRow(country);
        }

        /// <summary>행이 이미 만들어져 있으면 아무 것도 하지 않는다 — 국가 목록은 게임 시작 시
        /// 고정되므로(48개), 최초 1회 생성 후에는 재생성이 필요 없다.</summary>
        private void EnsureRowsBuilt()
        {
            if (_rowsByCountryId.Count > 0) return;
            if (_scroll == null) return;

            var countries = WorldDataManager.Instance?.Countries;
            if (countries == null) return;

            _scroll.Clear();
            _rowsByCountryId.Clear();

            foreach (var country in countries)
            {
                var refs = BuildRow(country);
                _rowsByCountryId[country.id] = refs;
                _scroll.Add(refs.Row);
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

        /// <summary>이미 생성된 행의 라벨 텍스트만 갱신한다 — VisualElement/Label을 새로 만들지 않는다.</summary>
        private void RefreshRow(Country country)
        {
            if (!_rowsByCountryId.TryGetValue(country.id, out var refs)) return;

            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;
            float deadRatio = country.population > 0
                ? (float)country.deadCount / country.population
                : 0f;

            var stage = country.GetCollapseStage();

            refs.NameLabel.text = $"{country.name} — {StageLabel(stage)}";

            refs.StatsLabel.text =
                $"인구 {country.population:N0} · 감염 {infectionRatio:P0} · 사망 {deadRatio:P0} · 의료 {DevLabel(country.developmentLevel)}";
            refs.StatsLabel.EnableInClassList("status-row__detail--danger", stage >= CountryCollapseStage.Disorder);

            refs.FlagsLabel.text =
                $"공항 {(country.isAirportOpen ? "개방" : "폐쇄")} · " +
                $"항구 {(country.isPortOpen ? "개방" : "폐쇄")} · " +
                $"국경 {(country.isBorderClosed ? "봉쇄" : "개방")}";
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
