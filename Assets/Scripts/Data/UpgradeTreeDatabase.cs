using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>
    /// 업그레이드 트리 전체를 하나의 자산으로 관리. 설계 문서 Step 9 / 2절, 3.4절.
    /// UpgradeManager는 이 자산이 아니라 CreateRuntimeInstances()가 반환하는 복제 리스트를 사용해야 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeTreeDatabase", menuName = "Contagion/Upgrade Tree Database")]
    public class UpgradeTreeDatabase : ScriptableObject
    {
        [SerializeField] private List<UpgradeNode> nodes = new List<UpgradeNode>();

        public IReadOnlyList<UpgradeNode> Nodes => nodes;

        /// <summary>런타임에서 사용할 복제 리스트 (모두 isUnlocked=false 상태로 시작).</summary>
        public List<UpgradeNode> CreateRuntimeInstances() => nodes.Select(n => n.Clone()).ToList();
    }
}
