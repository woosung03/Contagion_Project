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

        /// <summary>선행 노드가 모두 해금됐고 DNA가 충분한지 확인.</summary>
        public bool CanUnlock(string id)
        {
            var node = GetNode(id);
            if (node == null || node.isUnlocked) return false;
            if (Data == null || Data.State.dnaPoints < node.cost) return false;
            return node.prerequisites.All(IsUnlocked);
        }

        /// <summary>노드를 해금하고 DNA를 소모, 병원체 스탯에 효과를 적용한다.</summary>
        public bool TryUnlock(string id)
        {
            if (!CanUnlock(id)) return false;
            var node = GetNode(id);

            if (!Data.State.SpendDna(node.cost)) return false;
            node.isUnlocked = true;

            ApplyEffectsToPathogen(node);

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
        /// 지원 stat: infectivity / severity / lethality / drugResistance (모두 0~1 클램프).
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
                    default:
                        Debug.LogWarning($"[UpgradeManager] 알 수 없는 stat '{effect.statName}' (node={node.id}) — 무시됨");
                        break;
                }
            }
        }
    }
}
