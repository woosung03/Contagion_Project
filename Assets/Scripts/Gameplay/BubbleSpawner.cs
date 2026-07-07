using Contagion.Data;
using Contagion.Managers;
using Contagion.Utils;
using UnityEngine;

namespace Contagion.Gameplay
{
    /// <summary>
    /// DNA 수집 버블 생성기. 설계 문서 4.4절 + 15절 "DNA 버블은 오브젝트 풀링 필수".
    /// SimulationManager의 감염/사망 마일스톤 이벤트를 구독해 해당 국가 위치에 버블을 스폰한다.
    /// </summary>
    public class BubbleSpawner : MonoBehaviour
    {
        [SerializeField] private DnaBubble bubblePrefab;
        [SerializeField] private Transform poolParent;
        [SerializeField] private int prewarmCount = 20;

        [Header("보상량")]
        [SerializeField] private int dnaPerInfectionBubble = 1;
        [SerializeField] private int dnaPerDeathBubble = 3;

        [Header("버블 수명/스폰")]
        [SerializeField] private float bubbleLifetime = 6f;
        [SerializeField, Tooltip("[Step 42] 더 이상 실제 스폰 반경으로 쓰이지 않는다 — 국가마다 크기가 " +
            "달라 고정값 하나로는 작은 나라에서 국경 밖으로 버블이 벗어나는 버그가 있었다(CountryView." +
            "DnaSpawnScatterRadius가 국가별 반경을 대신 계산). CountryView 등록이 안 된 국가(이론상 " +
            "발생 안 함)에 대한 최후 폴백으로만 남겨둠.")]
        private float spawnRadius = 0.03f;

        private ObjectPool<DnaBubble> _pool;

        private void Awake()
        {
            if (bubblePrefab != null)
                _pool = new ObjectPool<DnaBubble>(bubblePrefab, poolParent != null ? poolParent : transform, prewarmCount);
            else
                Debug.LogWarning("[BubbleSpawner] bubblePrefab이 지정되지 않았습니다.");
        }

        private void OnEnable() => Subscribe();
        private void Start() => Subscribe(); // SimulationManager.Instance가 OnEnable 시점엔 아직 없을 수 있어 재시도

        private void Subscribe()
        {
            if (SimulationManager.Instance == null) return;
            SimulationManager.Instance.OnInfectionMilestone -= HandleInfectionMilestone;
            SimulationManager.Instance.OnInfectionMilestone += HandleInfectionMilestone;
            SimulationManager.Instance.OnDeathMilestone -= HandleDeathMilestone;
            SimulationManager.Instance.OnDeathMilestone += HandleDeathMilestone;
        }

        private void OnDisable()
        {
            if (SimulationManager.Instance == null) return;
            SimulationManager.Instance.OnInfectionMilestone -= HandleInfectionMilestone;
            SimulationManager.Instance.OnDeathMilestone -= HandleDeathMilestone;
        }

        private void HandleInfectionMilestone(Country country) => SpawnBubble(country, dnaPerInfectionBubble);
        private void HandleDeathMilestone(Country country) => SpawnBubble(country, dnaPerDeathBubble);

        private void SpawnBubble(Country country, int dnaValue)
        {
            if (_pool == null) return;

            var view = WorldMap.Instance != null ? WorldMap.Instance.GetView(country.id) : null;
            // Step 29: 모든 CountryView가 (0,0,0)에 겹쳐 있으므로(세계지도 오버레이 방식) 더 이상
            // view.transform.position을 쓰면 안 되고, 국가별로 미리 계산해둔 스폰 앵커를 써야 한다.
            // Step 42: 국가 중심 근사치(DnaSpawnWorldPosition) 주변에 고정 반경으로 흩뿌리던 방식은
            // 국가 크기 차이를 반영하지 못해 작은 나라에서 버블이 국경 밖에 스폰되는 버그가 있었다.
            // GetRandomDnaSpawnWorldPosition()이 실루엣 내부가 보장된 감염 점 좌표를 재사용하고,
            // DnaSpawnScatterRadius가 국가별로 알맞은(작은 나라는 작게) 산개 반경을 제공한다.
            Vector3 basePos = view != null ? view.GetRandomDnaSpawnWorldPosition() : Vector3.zero;
            float radius = view != null ? view.DnaSpawnScatterRadius : spawnRadius;
            Vector3 offset = (Vector3)(UnityEngine.Random.insideUnitCircle * radius);

            var bubble = _pool.Get();
            bubble.transform.position = basePos + offset;

            bubble.OnCollected -= HandleBubbleCollected;
            bubble.OnCollected += HandleBubbleCollected;
            bubble.OnExpired -= HandleBubbleExpired;
            bubble.OnExpired += HandleBubbleExpired;

            bubble.Activate(dnaValue, bubbleLifetime);
        }

        private void HandleBubbleCollected(DnaBubble bubble)
        {
            UpgradeManager.Instance?.AddDna(bubble.DnaValue);
            Release(bubble);
        }

        private void HandleBubbleExpired(DnaBubble bubble) => Release(bubble);

        private void Release(DnaBubble bubble)
        {
            bubble.OnCollected -= HandleBubbleCollected;
            bubble.OnExpired -= HandleBubbleExpired;
            _pool.Release(bubble);
        }
    }
}
