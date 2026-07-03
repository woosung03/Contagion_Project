using System;
using System.Collections.Generic;
using Contagion.Data;
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
        private Label _detailDesc;
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
            _detailDesc = root.Q<Label>("detail-desc");
            _backButton = root.Q<Button>("back-button");
            _startButton = root.Q<Button>("start-button");

            // 디버깅용 — 이 중 하나라도 NULL이면 UXML의 name 속성과 여기서 쿼리하는 이름이
            // 어긋났거나 UIDocument의 Source Asset이 제대로 로드되지 않은 것. NULL인 채로
            // RegisterCallback을 호출하면 예외가 나서 이 메서드 전체(버튼 클릭 핸들러 등록 포함)가
            // 중간에 멈춰버리는데, 그 예외가 씬 로드 시점에 한 번만 조용히 찍히고 지나가면
            // 나중에 버튼을 눌러도 "아무 반응 없음"으로만 보여서 원인 파악이 어려웠다 — 그래서
            // 각 참조를 개별적으로 확인하고 로그로 남긴다.
            Debug.Log($"[FLOW][CountrySelectController] OnEnable (instanceId={GetInstanceID()}, " +
                $"time={Time.realtimeSinceStartup:F2}) — " +
                $"root={(_root != null ? "OK" : "NULL")}, " +
                $"countryList={(_countryList != null ? "OK" : "NULL")}, " +
                $"detailTitle={(_detailTitle != null ? "OK" : "NULL")}, " +
                $"detailDesc={(_detailDesc != null ? "OK" : "NULL")}, " +
                $"backButton={(_backButton != null ? "OK" : "NULL")}, " +
                $"startButton={(_startButton != null ? "OK" : "NULL")}");

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
                Debug.Log($"[FLOW][CountrySelectController] 시작 버튼 클릭됨 (instanceId={GetInstanceID()}, " +
                    $"selectedCountryId={_selectedCountryId}).");
                HandleStartClicked();
            });
        }

        public void Show()
        {
            Debug.Log($"[FLOW][CountrySelectController] Show() 호출됨 (instanceId={GetInstanceID()}, " +
                $"root={(_root != null ? "OK" : "NULL")}).");
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            RebuildList();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
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
            _detailDesc.text = "";

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

            Debug.Log($"[FLOW][CountrySelectController] 국가 목록 {countries.Count}개 렌더링 완료.");
        }

        private VisualElement CreateCountryRow(Country country)
        {
            var row = new VisualElement();
            row.AddToClassList("country-row");

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

            var meta = new Label($"인구 {country.population:N0} · {DevLabel(country.developmentLevel)}");
            meta.AddToClassList("country-row__meta");
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
            Debug.Log($"[FLOW][CountrySelectController] 국가 행 클릭됨 — {country.id} (instanceId={GetInstanceID()}).");
            _selectedCountryId = country.id;

            foreach (var child in _countryList.Children())
                child.RemoveFromClassList("country-row--selected");
            row.AddToClassList("country-row--selected");

            _detailTitle.text = country.name;
            _detailDesc.text = $"인구: {country.population:N0}\n" +
                                $"기후: {ClimateLabel(country.climate)}\n" +
                                $"의료 수준: {DevLabel(country.developmentLevel)}";

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
