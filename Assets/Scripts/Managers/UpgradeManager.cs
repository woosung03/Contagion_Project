using System;
using System.Collections.Generic;
using System.Linq;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// DNA 포인트 + 업그레이드 트리 상태 관리. 설계 문서 2절(트리 업그레이드), 3.4절.
    /// 노드 목록은 Step 4 범위에서는 인스펙터에 직접 채우고, Step 9에서 ScriptableObject 데이터로 이전한다.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<UpgradeNode> tree = new List<UpgradeNode>();

        [SerializeField, Range(0f, 1f), Tooltip("같은 카테고리에서 노드를 해금할 때마다 다음 노드 비용에 곱해지는 " +
            "가산율. 나무위키 Plague Inc./시스템 문서 \"진화 시 다음 특성의 소모 DNA가 늘어난다\" 반영 " +
            "(Docs/PlagueIncReference.md). 예: 0.15면 같은 카테고리에서 3개 해금 후 4번째 노드는 " +
            "기본 비용의 1.45배(1 + 3*0.15)가 된다.")]
        private float categoryCostEscalationPerUnlock = 0.15f;

        private Dictionary<string, UpgradeNode> _nodeLookup;

        /// <summary>전체 노드 목록 (읽기 전용) — UpgradeTreeView(Step 8)가 트리 렌더링에 사용.</summary>
        public IReadOnlyList<UpgradeNode> Tree => tree;

        public event Action<UpgradeNode> OnNodeUnlocked;
        public event Action<int> OnDnaChanged; // 변경 후 총 DNA

        private WorldDataManager Data => WorldDataManager.Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            _nodeLookup = tree.Where(n => n != null && !string.IsNullOrEmpty(n.id))
                               .ToDictionary(n => n.id, n => n);
        }

        public void SetTree(List<UpgradeNode> newTree)
        {
            tree = newTree ?? new List<UpgradeNode>();
            RebuildLookup();
        }

        public UpgradeNode GetNode(string id)
        {
            if (_nodeLookup == null) RebuildLookup();
            return _nodeLookup.TryGetValue(id, out var node) ? node : null;
        }

        public bool IsUnlocked(string id) => GetNode(id)?.isUnlocked ?? false;

        /// <summary>
        /// 실제 구매 비용 — node.cost에 "같은 카테고리에서 이미 해금한 노드 수"만큼 가산율을 곱한다.
        /// 나무위키 원본 게임 특유의 룰(Docs/PlagueIncReference.md 참고): 같은 계열을 계속 진화시킬수록
        /// 다음 진화 비용이 비싸진다 — 한 카테고리만 몰아 찍기보다 세 카테고리를 골고루 찍도록 유도한다.
        /// </summary>
        public int GetEffectiveCost(UpgradeNode node)
        {
            if (node == null) return 0;
            int unlockedInCategory = tree.Count(n => n.category == node.category && n.isUnlocked);
            float multiplier = 1f + unlockedInCategory * categoryCostEscalationPerUnlock;
            return Mathf.CeilToInt(node.cost * multiplier);
        }

        public int GetEffectiveCost(string id) => GetEffectiveCost(GetNode(id));

        /// <summary>선행 노드가 모두 해금됐고 DNA가 충분한지 확인.</summary>
        public bool CanUnlock(string id)
        {
            var node = GetNode(id);
            if (node == null || node.isUnlocked) return false;
            if (Data == null || Data.State.dnaPoints < GetEffectiveCost(node)) return false;
            return node.prerequisites.All(IsUnlocked);
        }

        /// <summary>노드를 해금하고 DNA를 소모, 병원체 스탯에 효과를 적용한다.</summary>
        public bool TryUnlock(string id)
        {
            if (!CanUnlock(id)) return false;
            var node = GetNode(id);
            int effectiveCost = GetEffectiveCost(node);

            if (!Data.State.SpendDna(effectiveCost)) return false;
            node.isUnlocked = true;

            ApplyEffectsToPathogen(node);

            Debug.Log($"[UpgradeManager] {node.id} 해금 (실비용={effectiveCost}, 기본비용={node.cost})");

            Data.NotifyWorldStateChanged();
            OnNodeUnlocked?.Invoke(node);
            OnDnaChanged?.Invoke(Data.State.dnaPoints);
            return true;
        }

        /// <summary>DNA 포인트 획득 (버블 수집 등). 설계 문서 4.4절.</summary>
        public void AddDna(int amount)
        {
            if (Data == null) return;
            Data.State.AddDna(amount);
            Data.NotifyWorldStateChanged();
            OnDnaChanged?.Invoke(Data.State.dnaPoints);
        }

        /// <summary>
        /// node.effects의 statName을 Pathogen 필드에 매핑해 가산 적용한다.
        /// 지원 stat: infectivity / severity / lethality / drugResistance (모두 0~1 클램프),
        /// airRouteEfficiency / waterRouteEfficiency / contactRouteEfficiency(TransmissionRoute
        /// Phase 2 — 연속값, 동일한 가산+클램프 패턴), unlockAnimal / unlockInsect / unlockBlood
        /// (TransmissionRoute Phase 2 — 이진, transmissionRoutes 리스트에 추가).
        /// </summary>
        private void ApplyEffectsToPathogen(UpgradeNode node)
        {
            var pathogen = Data?.CurrentPathogen;
            if (pathogen == null) return;

            foreach (var effect in node.effects)
            {
                switch (effect.statName)
                {
                    case "infectivity":
                        pathogen.infectivity = Mathf.Clamp01(pathogen.infectivity + effect.amount);
                        break;
                    case "severity":
                        pathogen.severity = Mathf.Clamp01(pathogen.severity + effect.amount);
                        break;
                    case "lethality":
                        pathogen.lethality = Mathf.Clamp01(pathogen.lethality + effect.amount);
                        break;
                    case "drugResistance":
                        pathogen.drugResistance = Mathf.Clamp01(pathogen.drugResistance + effect.amount);
                        break;
                    // TransmissionRoute Phase 2 — 효율 강화(연속값, 기존 4개 스탯과 동일한
                    // 가산+클램프 패턴) / 전문화 경로 해금(이진, transmissionRoutes 리스트에 추가).
                    case "airRouteEfficiency":
                        pathogen.airRouteEfficiency = Mathf.Clamp01(pathogen.airRouteEfficiency + effect.amount);
                        break;
                    case "waterRouteEfficiency":
                        pathogen.waterRouteEfficiency = Mathf.Clamp01(pathogen.waterRouteEfficiency + effect.amount);
                        break;
                    case "contactRouteEfficiency":
                        pathogen.contactRouteEfficiency = Mathf.Clamp01(pathogen.contactRouteEfficiency + effect.amount);
                        break;
                    case "unlockAnimal":
                        if (!pathogen.transmissionRoutes.Contains(TransmissionRoute.Animal))
                            pathogen.transmissionRoutes.Add(TransmissionRoute.Animal);
                        break;
                    case "unlockInsect":
                        if (!pathogen.transmissionRoutes.Contains(TransmissionRoute.Insect))
                            pathogen.transmissionRoutes.Add(TransmissionRoute.Insect);
                        break;
                    case "unlockBlood":
                        if (!pathogen.transmissionRoutes.Contains(TransmissionRoute.Blood))
                            pathogen.transmissionRoutes.Add(TransmissionRoute.Blood);
                        break;
                    default:
                        Debug.LogWarning($"[UpgradeManager] 알 수 없는 stat '{effect.statName}' (node={node.id}) — 무시됨");
                        break;
                }
            }
        }
    }
}
