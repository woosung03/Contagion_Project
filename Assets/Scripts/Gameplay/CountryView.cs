using Contagion.Data;
using Contagion.Managers;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// 국가 하나에 대응하는 시각 요소. 설계 문서 15절 "국가 지도는 2D 폴리곤 또는 스프라이트 방식 권장"
    /// 에 따라 SpriteRenderer + Collider2D(클릭 감지) 조합으로 구현한다.
    /// 씬에서 국가 개수만큼 배치하고 countryId만 채워주면 WorldMap이 자동으로 등록/갱신한다.
    /// </summary>
    // OnMouseDown()이 동작하려면 Collider2D가 필요하다. 국가 모양이 제각각이므로 기본은
    // BoxCollider2D를 강제하되, 실제 국경선 클릭 정확도가 필요하면 PolygonCollider2D로 교체할 것.
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CountryView : MonoBehaviour
    {
        [SerializeField] private string countryId;
        [SerializeField] private Color healthyColor = new Color(0.35f, 0.75f, 0.35f);
        [SerializeField] private Color infectedColor = new Color(0.9f, 0.75f, 0.15f);
        [SerializeField] private Color deadColor = new Color(0.55f, 0.1f, 0.1f);

        private SpriteRenderer _renderer;

        public string CountryId => countryId;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (WorldMap.Instance != null)
                WorldMap.Instance.RegisterCountryView(this);
        }

        private void OnDestroy()
        {
            if (WorldMap.Instance != null)
                WorldMap.Instance.UnregisterCountryView(this);
        }

        /// <summary>WorldMap이 국가 상태 갱신 시 호출.</summary>
        public void UpdateVisual(Country country)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            float infectionRatio = country.LivingPopulation > 0
                ? (float)country.infectedCount / country.LivingPopulation
                : 0f;
            float deadRatio = country.population > 0
                ? (float)country.deadCount / country.population
                : 0f;

            Color color = Color.Lerp(healthyColor, infectedColor, Mathf.Clamp01(infectionRatio));
            color = Color.Lerp(color, deadColor, Mathf.Clamp01(deadRatio));
            _renderer.color = color;
        }

        private void OnMouseDown()
        {
            WorldMap.Instance?.HandleCountryClicked(countryId);
        }
    }
}
