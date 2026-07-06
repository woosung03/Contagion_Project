using System;
using System.Collections.Generic;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 국가 데이터 바인딩 + 색상 표시 + 클릭 처리. 설계 문서 7.1 세계 지도, 7.3 국가 정보 팝업.
    /// CountryView들이 Start()에서 스스로 등록하므로, 이 매니저는 씬에 하나만 있으면 된다.
    /// </summary>
    public class WorldMap : MonoBehaviour
    {
        public static WorldMap Instance { get; private set; }

        private readonly Dictionary<string, CountryView> _views = new Dictionary<string, CountryView>();

        /// <summary>국가 스프라이트 클릭 시 발행 — 7.3 국가 정보 팝업 UI(Step 8)가 구독할 이벤트.</summary>
        public event Action<Country> OnCountryClicked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.OnCountryChanged += HandleCountryChanged;
        }

        private void OnDisable()
        {
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
        }

        private void Start()
        {
            // WorldDataManager가 이 컴포넌트보다 늦게 Awake될 수도 있으므로 재구독 + 초기 전체 갱신.
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnCountryChanged -= HandleCountryChanged;
                WorldDataManager.Instance.OnCountryChanged += HandleCountryChanged;
                RefreshAll();
            }
        }

        public void RegisterCountryView(CountryView view)
        {
            if (view == null || string.IsNullOrEmpty(view.CountryId)) return;
            _views[view.CountryId] = view;

            var country = WorldDataManager.Instance?.GetCountry(view.CountryId);
            if (country != null) view.UpdateVisual(country);
        }

        /// <summary>BubbleSpawner 등이 국가의 스크린/월드 위치를 구할 때 사용.</summary>
        public CountryView GetView(string countryId) =>
            _views.TryGetValue(countryId, out var view) ? view : null;

        /// <summary>
        /// 특정 국가에 속하지 않는 임의의 지점(예: 해상 항로 경유점)을 CountryView.DnaSpawnWorldPosition과
        /// 같은 좌표계로 변환한다. CountryView는 항상 이 GameObject(WorldMap) 기준 로컬 (0,0,0)에 고정돼
        /// 있으므로(dnaSpawnLocalOffset만 다름) transform.TransformPoint 결과가 CountryView 쪽과 동일하게
        /// 나온다 — TransportManager가 말라카 해협/수에즈 운하 같은 경유점을 국가 앵커 없이 배치할 때 사용.
        /// </summary>
        public Vector3 ToWorldPosition(Vector2 localOffset) => transform.TransformPoint(localOffset);

        public void UnregisterCountryView(CountryView view)
        {
            if (view == null || string.IsNullOrEmpty(view.CountryId)) return;
            if (_views.TryGetValue(view.CountryId, out var existing) && existing == view)
                _views.Remove(view.CountryId);
        }

        public void RefreshAll()
        {
            if (WorldDataManager.Instance == null) return;
            foreach (var country in WorldDataManager.Instance.Countries)
                HandleCountryChanged(country);
        }

        private void HandleCountryChanged(Country country)
        {
            if (country == null) return;
            if (_views.TryGetValue(country.id, out var view))
                view.UpdateVisual(country);
        }

        public void HandleCountryClicked(string countryId)
        {
            var country = WorldDataManager.Instance?.GetCountry(countryId);
            if (country != null) OnCountryClicked?.Invoke(country);
        }
    }
}
