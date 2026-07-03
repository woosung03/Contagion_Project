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
    /// 국기 아이콘은 아직 실제 에셋이 없어 자리만 비워둠(추후 UI/UX 폴리싱 후속 작업에서 채울 슬롯 —
    /// flag-icon 클래스가 붙은 빈 VisualElement로 만들어 나중에 배경 이미지만 USS로 지정하면 됨).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CountrySelectController : MonoBehaviour
    {
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

            // 국기 아이콘 슬롯 — 지금은 빈 사각형(placeholder), 나중에 국기 에셋 들어오면
            // USS에서 이 클래스에 backgroundImage만 지정하면 된다. countryId별 개별 배경은
            // 국가 수만큼 USS 클래스를 늘리기보단 실제 에셋 준비 시 스프라이트 아틀라스로 재작업 예정.
            var flagSlot = new VisualElement();
            flagSlot.AddToClassList("country-row__flag");
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
