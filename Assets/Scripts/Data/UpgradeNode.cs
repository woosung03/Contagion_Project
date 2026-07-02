using System;
using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>UpgradeNode.effects 항목 (stat 이름 -> 변화량). 설계 문서 3.4절.</summary>
    [Serializable]
    public struct UpgradeEffectEntry
    {
        public string statName;   // 예: "infectivity", "severity", "lethality", "drugResistance"
        public float amount;      // 변화량 (가산, 음수면 감소 — 예: 은신 계열은 severity를 낮춤)
    }

    /// <summary>
    /// 업그레이드 트리 노드. 설계 문서 3.4절 / 2절(전파 경로/증상/능력 트리).
    /// </summary>
    [Serializable]
    public class UpgradeNode
    {
        public string id;
        public UpgradeCategory category;
        public int cost;                              // DNA 포인트 기본 비용 (실제 구매 비용은
                                                        // UpgradeManager.GetEffectiveCost()에서 카테고리별
                                                        // 해금 개수에 따라 가산 — 나무위키 "진화 시 다음 특성의
                                                        // 소모 DNA가 늘어난다" 반영, Docs/PlagueIncReference.md)
        public List<string> prerequisites = new List<string>(); // 선행 노드 id
        public bool isUnlocked;
        public List<UpgradeEffectEntry> effects = new List<UpgradeEffectEntry>();

        /// <summary>
        /// 트리 시각화용 좌표 (UpgradeTreeView가 픽셀 단위로 사용). 설계 문서에 노드 좌표 데이터가
        /// 없어 DefaultUpgradeTreeFactory에서 카테고리별 열(column) + 선행조건 깊이(row)로 자동 배치한다.
        /// </summary>
        public Vector2 position;

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
                effects = new List<UpgradeEffectEntry>(effects),
                position = position
            };
        }
    }
}
