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
    /// 발원 국가 선택 화면 (UI/UX 폴리싱 — CountrySelect). 설계 문서 2절 "발원 국가 선택".
    /// GameDataBootstrapper.AvailableCountries(18개 템플릿 목록)를 리스트로 그리고, 선택 후
    /// "시작" 누르면 UIManager가 GameDataBootstrapper.BeginGame()을 호출해 실제 플레이가 시작된다.
    /// 국기 아이콘: <c>Assets/Resources/Flags/{countryId}.png</c>를 <see cref="Resources.Load"/>로
    /// 런타임에 불러와 country-row__flag 슬롯의 배경으로 지정한다(씬/에셋 GUID를 직접 참조하지 않아도
    /// 되는 방식이라 코드만으로 배선 가능). 파일이 없는 국가는 기존처럼 빈 사각형(placeholder)으로 남는다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountrySelectController : MonoBehaviour
    {
        // country.id(CHI/IND/USA...) -> Resources/Flags/{id}.png. 캐시해서 국가 목록을 다시 그릴 때마다
        // (SelectCountry 등에서 RebuildList를 부르진 않지만, 재시작 시 새 인스턴스가 다시 로드하므로)
        // 매번 Resources.Load를 반복 호출하지 않도록 한다.
        private readonly Dictionary<string, Texture2D> _flagCache = new Dictionary<string, Texture2D>();

        private VisualElement _root;
        private ScrollView _countryList;
        private Label _detailTitle;
        private VisualElement _detailRows;
        private Button _backButton;
        private Button _startButton;

        private string _selectedCountryId;

        /// <summary>발원 국가 확정(시작 버튼) — UIManager가 GameDataBootstrapper.BeginGame()을 호출한다.</summary>
        public event Action<string> OnCountryConfirmed;

        /// <summary>뒤로 버튼 — UIManager가 MainMenu로 되돌린다.</summary>
        public event Action OnBackRequested;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _root = root.Q<VisualElement>("countryselect-root");
            _countryList = root.Q<ScrollView>("country-list");
            _detailTitle = root.Q<Label>("detail-title");
            _detailRows = root.Q<VisualElement>("detail-rows");
            _backButton = root.Q<Button>("back-button");
            _startButton = root.Q<Button>("start-button");

            if (_backButton == null || _startButton == null)
            {
                Debug.LogError("[FLOW][CountrySelectController] back-button/start-button을 UXML에서 찾지 못해 " +
                    "클릭 이벤트를 등록할 수 없습니다 — CountrySelect.uxml의 이름 속성을 확인하세요.");
                return;
            }

            _backButton.RegisterCallback<ClickEvent>(_ =>
            {
                int subs = OnBackRequested?.GetInvocationList().Length ?? 0;
                Debug.Log($"[FLOW][CountrySelectController] 뒤로 버튼 클릭됨 (instanceId={GetInstanceID()}, " +
                    $"OnBackRequested 구독자 수={subs}).");
                OnBackRequested?.Invoke();
            });
            _startButton.RegisterCallback<ClickEvent>(_ =>
            {
                HandleStartClicked();
            });
        }

        public void Show()
        {
            // [WorldMap Input Lock System] CountrySelect가 떠 있는 동안은 WorldMap 클릭/드래그를 차단한다.
            WorldMapInputLock.Lock(WorldMapLockReason.CountrySelect);
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            RebuildList();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
            WorldMapInputLock.Unlock(WorldMapLockReason.CountrySelect);
        }

        private void RebuildList()
        {
            if (_countryList == null)
            {
                Debug.LogError("[FLOW][CountrySelectController] RebuildList — countryList(ScrollView)가 NULL이라 " +
                    "국가 목록을 그릴 수 없습니다 (OnEnable의 요소 바인딩 로그를 확인하세요).");
                return;
            }

            _countryList.Clear();
            _selectedCountryId = null;
            _startButton.SetEnabled(false);
            _detailTitle.text = "";
            _detailRows?.Clear();

            IReadOnlyList<Country> countries = GameDataBootstrapper.Instance?.AvailableCountries;
            if (countries == null || countries.Count == 0)
            {
                Debug.LogWarning("[FLOW][CountrySelectController] 사용 가능한 국가가 없습니다 — GameDataBootstrapper의 " +
                    "Country Database 연결을 확인하세요.");
                return;
            }

            foreach (var country in countries)
            {
                if (country == null) continue;
                _countryList.Add(CreateCountryRow(country));
            }

        }

        private VisualElement CreateCountryRow(Country country)
        {
            var row = new VisualElement();
            row.AddToClassList("country-row");
            row.AddToClassList("accent-bar-row"); // Docs/UI_Design.md 13절 — 48행 리스트라 코너컷 대신 좌측 accent bar
            row.AddToClassList(DevAccentClass(country.developmentLevel));

            // 국기 아이콘 슬롯 — Resources/Flags/{countryId}.png 로드해서 배경으로 지정.
            // 국가마다 다른 이미지라 USS 클래스 하나로는 표현 불가능해서 코드에서 개별로 채운다.
            var flagSlot = new VisualElement();
            flagSlot.AddToClassList("country-row__flag");
            var flagTex = GetFlagTexture(country.id);
            if (flagTex != null)
                flagSlot.style.backgroundImage = new StyleBackground(flagTex);
            else
                Debug.LogWarning($"[CountrySelectController] 국기 텍스처를 찾지 못했습니다: Resources/Flags/{country.id}.png — 빈 슬롯으로 표시됩니다.");
            row.Add(flagSlot);

            var name = new Label(country.name);
            name.AddToClassList("country-row__name");
            row.Add(name);

            // country-row__meta — Tactical.uss data-row/data-label/data-value 판독행 문법 재사용
            // (Docs/UI_Design.md 9절 "Country Dock 시각 언어 재사용"). 48행 스크롤 높이 제약 때문에
            // data-row를 세로로 쌓지 않고 이 한 줄 안에서만 label:value로 구조화한다(정보량 동일,
            // 표현 문법만 판독행으로 전환 — country-row__meta CSS가 기본 data-row 하단 헤어라인은 무효화).
            var meta = new VisualElement();
            meta.AddToClassList("country-row__meta");
            meta.AddToClassList("data-row");

            var metaLabel = new Label("인구");
            metaLabel.AddToClassList("data-label");
            meta.Add(metaLabel);

            var metaValue = new Label($"{country.population:N0} · {DevLabel(country.developmentLevel)}");
            metaValue.AddToClassList("data-value");
            meta.Add(metaValue);

            row.Add(meta);

            row.RegisterCallback<ClickEvent>(_ => SelectCountry(country, row));
            return row;
        }

        /// <summary>
        /// Resources/Flags/{countryId}.png를 로드해 캐시한다. Resources.Load는 경로 문자열로 찾기 때문에
        /// 씬/에셋 파일에 GUID를 직접 박아넣을 필요가 없다 — Unity 에디터가 없는 세션에서도 텍스처
        /// 파일만 그 경로에 두면(및 프로젝트를 에디터로 한 번 열어 임포트되면) 바로 동작한다.
        /// </summary>
        private Texture2D GetFlagTexture(string countryId)
        {
            if (string.IsNullOrEmpty(countryId)) return null;
            if (_flagCache.TryGetValue(countryId, out var cached)) return cached;

            var tex = Resources.Load<Texture2D>($"Flags/{countryId}");
            _flagCache[countryId] = tex; // 못 찾아도 null로 캐시해서 매 프레임 재조회하지 않음
            return tex;
        }

        private void SelectCountry(Country country, VisualElement row)
        {
            _selectedCountryId = country.id;

            foreach (var child in _countryList.Children())
                child.RemoveFromClassList("country-row--selected");
            row.AddToClassList("country-row--selected");

            _detailTitle.text = country.name;

            // 문단 한 덩어리(인구/기후/의료 수준)를 data-row 3줄로 분해 — Docs/UI_Design.md 13절.
            // Country Dock(country-dock__row)과 동일한 문법이라 게임 시작 전/후 정보 표현이 통일된다.
            _detailRows?.Clear();
            _detailRows?.Add(MakeDetailRow("인구", $"{country.population:N0}"));
            _detailRows?.Add(MakeDetailRow("기후", ClimateLabel(country.climate)));
            _detailRows?.Add(MakeDetailRow("의료 수준", DevLabel(country.developmentLevel), DevValueClass(country.developmentLevel)));

            _startButton.SetEnabled(true);
        }

        private void HandleStartClicked()
        {
            if (string.IsNullOrEmpty(_selectedCountryId)) return;
            OnCountryConfirmed?.Invoke(_selectedCountryId);
        }

        private static string DevLabel(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "선진국",
            DevelopmentLevel.Mid => "개발도상국",
            DevelopmentLevel.Low => "저개발국",
            _ => level.ToString()
        };

        /// <summary>country-row 좌측 accent bar 색상 클래스 — 의료 수준을 severity로 재해석(13절).</summary>
        private static string DevAccentClass(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "country-row--dev-high",
            DevelopmentLevel.Mid => "country-row--dev-mid",
            DevelopmentLevel.Low => "country-row--dev-low",
            _ => "country-row--dev-mid"
        };

        /// <summary>상세 패널의 "의료 수준" data-value 색상 클래스(Tactical.uss severity variant 재사용).</summary>
        private static string DevValueClass(DevelopmentLevel level) => level switch
        {
            DevelopmentLevel.High => "data-value--info",
            DevelopmentLevel.Low => "data-value--danger",
            _ => null
        };

        /// <summary>data-row 한 줄(라벨+값)을 만든다 — Tactical.uss data-row/data-label/data-value 계약.</summary>
        private static VisualElement MakeDetailRow(string label, string value, string valueClass = null)
        {
            var row = new VisualElement();
            row.AddToClassList("data-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("data-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("data-value");
            if (!string.IsNullOrEmpty(valueClass)) valueEl.AddToClassList(valueClass);
            row.Add(valueEl);

            return row;
        }

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
