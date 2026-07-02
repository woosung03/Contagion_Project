using System;
using System.Collections.Generic;

namespace Contagion.Data
{
    /// <summary>UpgradeNode.effects 항목 (stat 이름 -> 변화량). 설계 문서 3.4절.</summary>
    [Serializable]
    public struct UpgradeEffectEntry
    {
        public string statName;   // 예: "infectivity", "severity", "lethality", "drugResistance"
        public float amount;      // 변화량 (가산)
    }

    /// <summary>
    /// 업그레이드 트리 노드. 설계 문서 3.4절 / 2절(전파 경로/증상/능력 트리).
    /// </summary>
    [Serializable]
    public class UpgradeNode
    {
        public string id;
        public UpgradeCategory category;
        public int cost;                              // DNA 포인트 비용
        public List<string> prerequisites = new List<string>(); // 선행 노드 id
        public bool isUnlocked;
        public List<UpgradeEffectEntry> effects = new List<UpgradeEffectEntry>();

        public float GetEffect(string statName)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].statName == statName)
                    return effects[i].amount;
            }
            return 0f;
        }

        /// <summary>
        /// 깊은 복사본 생성. isUnlocked는 항상 false로 초기화 — Step 9: UpgradeTreeDatabase(SO)는
        /// "템플릿"이므로 게임 시작 시 매번 잠금 상태로 새로 시작해야 한다.
        /// </summary>
        public UpgradeNode Clone()
        {
            return new UpgradeNode
            {
                id = id,
                category = category,
                cost = cost,
                prerequisites = new List<string>(prerequisites),
                isUnlocked = false,
                effects = new List<UpgradeEffectEntry>(effects)
            };
        }
    }
}
