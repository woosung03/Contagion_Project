using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>
    /// 감염경로/증상/능력 3개 카테고리에 걸친 세분화된 기본 업그레이드 트리를 코드로 생성한다.
    /// 설계 문서 2절/3.4절은 트리 "카테고리"만 정의하고 실제 노드·좌표·선행조건 데이터는 없었으므로,
    /// UpgradeTreeDatabase(ScriptableObject) 에셋을 Unity 에디터에서 직접 만들어 노드를 하나하나
    /// 입력하는 대신, 여기서 27개 노드(카테고리당 9개, 3단계 티어 + 최종 노드)를 프로그래밍적으로
    /// 정의해 둔다. GameDataBootstrapper는 upgradeTreeDatabase 에셋이 지정되지 않았을 때 이 팩토리를
    /// 폴백으로 사용한다 — 즉 에셋을 굳이 만들지 않아도 바로 플레이 가능하다.
    ///
    /// 좌표(position)는 UpgradeTreeView가 절대 좌표 배치 + 선행조건 연결선을 그리는 데 사용한다.
    /// 카테고리별로 x 구간을 나누고(감염경로 40~540 / 증상 640~1140 / 능력 1240~1740 — 노드 폭 140px보다
    /// 넉넉한 100px 여백을 카테고리 사이에 둬서 겹치지 않게 함), y는 선행조건 깊이(티어)에 따라
    /// 4단계(40/150/260/370)로 배치했다.
    ///
    /// 비용 밸런싱: 여기 적힌 cost는 "기본 비용"이며 실제 구매 비용은
    /// UpgradeManager.GetEffectiveCost()에서 같은 카테고리 해금 개수에 따라 가산된다
    /// (Docs/PlagueIncReference.md — 나무위키 "진화 시 다음 특성 비용 증가" 반영).
    /// 노드 효과는 전부 Pathogen의 4개 지원 스탯(infectivity/severity/lethality/drugResistance)만
    /// 사용한다 — UpgradeManager.ApplyEffectsToPathogen이 이 4개 외엔 경고 로그만 남기고 무시하기 때문.
    /// </summary>
    public static class DefaultUpgradeTreeFactory
    {
        public static List<UpgradeNode> BuildDefaultDetailedTree()
        {
            var nodes = new List<UpgradeNode>();

            // ================= 감염 경로 (Transmission) — x: 40~460 =================
            nodes.Add(Node("trans_air1", UpgradeCategory.Transmission, 2, 40, 40, null,
                ("infectivity", 0.05f)));
            nodes.Add(Node("trans_water1", UpgradeCategory.Transmission, 2, 220, 40, null,
                ("infectivity", 0.05f)));
            nodes.Add(Node("trans_contact1", UpgradeCategory.Transmission, 2, 400, 40, null,
                ("infectivity", 0.05f)));

            nodes.Add(Node("trans_air2", UpgradeCategory.Transmission, 4, 40, 150, new[] { "trans_air1" },
                ("infectivity", 0.08f)));
            nodes.Add(Node("trans_water2", UpgradeCategory.Transmission, 4, 220, 150, new[] { "trans_water1" },
                ("infectivity", 0.08f)));
            nodes.Add(Node("trans_insect1", UpgradeCategory.Transmission, 4, 400, 150, new[] { "trans_contact1" },
                ("infectivity", 0.07f)));

            nodes.Add(Node("trans_animal1", UpgradeCategory.Transmission, 5, 130, 260, new[] { "trans_water2" },
                ("infectivity", 0.06f), ("severity", 0.02f)));
            nodes.Add(Node("trans_blood1", UpgradeCategory.Transmission, 5, 400, 260, new[] { "trans_insect1" },
                ("infectivity", 0.07f)));

            nodes.Add(Node("trans_global", UpgradeCategory.Transmission, 10, 220, 370,
                new[] { "trans_air2", "trans_animal1", "trans_blood1" },
                ("infectivity", 0.15f)));

            // ================= 증상 (Symptom) — x: 640~1140 =================
            // (예전엔 520~940이라 감염경로 마지막 칸(400, 우측 끝 540)과 40px밖에 안 떨어져 있었고
            // 노드 폭이 140이라 실제로는 겹쳤다 — 플레이 중 발견되어 카테고리 사이 간격을 100px 이상으로 벌림)
            nodes.Add(Node("sym_cough", UpgradeCategory.Symptom, 2, 640, 40, null,
                ("severity", 0.03f)));
            nodes.Add(Node("sym_rash", UpgradeCategory.Symptom, 2, 820, 40, null,
                ("severity", 0.04f)));
            nodes.Add(Node("sym_nausea", UpgradeCategory.Symptom, 2, 1000, 40, null,
                ("severity", 0.03f)));

            nodes.Add(Node("sym_fever", UpgradeCategory.Symptom, 3, 640, 150, new[] { "sym_cough" },
                ("severity", 0.05f), ("lethality", 0.01f)));
            nodes.Add(Node("sym_lesion", UpgradeCategory.Symptom, 3, 820, 150, new[] { "sym_rash" },
                ("severity", 0.05f)));
            nodes.Add(Node("sym_vomit", UpgradeCategory.Symptom, 3, 1000, 150, new[] { "sym_nausea" },
                ("severity", 0.05f)));

            nodes.Add(Node("sym_pneumonia", UpgradeCategory.Symptom, 5, 640, 260, new[] { "sym_fever" },
                ("severity", 0.06f), ("lethality", 0.03f)));
            nodes.Add(Node("sym_hemorrhage", UpgradeCategory.Symptom, 5, 1000, 260, new[] { "sym_vomit" },
                ("severity", 0.06f), ("lethality", 0.03f)));

            nodes.Add(Node("sym_organfailure", UpgradeCategory.Symptom, 12, 820, 370,
                new[] { "sym_pneumonia", "sym_hemorrhage", "sym_lesion" },
                ("lethality", 0.15f), ("severity", 0.05f)));

            // ================= 능력 (Ability) — x: 1240~1740 =================
            nodes.Add(Node("abl_mutation1", UpgradeCategory.Ability, 2, 1240, 40, null,
                ("drugResistance", 0.04f)));
            nodes.Add(Node("abl_stealth1", UpgradeCategory.Ability, 2, 1420, 40, null,
                ("severity", -0.03f)));
            nodes.Add(Node("abl_hardening1", UpgradeCategory.Ability, 2, 1600, 40, null,
                ("drugResistance", 0.05f)));

            nodes.Add(Node("abl_mutation2", UpgradeCategory.Ability, 4, 1240, 150, new[] { "abl_mutation1" },
                ("drugResistance", 0.06f)));
            nodes.Add(Node("abl_stealth2", UpgradeCategory.Ability, 4, 1420, 150, new[] { "abl_stealth1" },
                ("severity", -0.05f)));
            nodes.Add(Node("abl_hardening2", UpgradeCategory.Ability, 4, 1600, 150, new[] { "abl_hardening1" },
                ("drugResistance", 0.07f)));

            nodes.Add(Node("abl_resist1", UpgradeCategory.Ability, 5, 1240, 260, new[] { "abl_mutation2" },
                ("drugResistance", 0.08f)));
            nodes.Add(Node("abl_resist2", UpgradeCategory.Ability, 5, 1600, 260, new[] { "abl_hardening2" },
                ("drugResistance", 0.08f)));

            nodes.Add(Node("abl_finalevo", UpgradeCategory.Ability, 15, 1420, 370,
                new[] { "abl_resist1", "abl_stealth2", "abl_resist2" },
                ("drugResistance", 0.15f), ("infectivity", 0.05f)));

            return nodes;
        }

        private static UpgradeNode Node(string id, UpgradeCategory category, int cost, float x, float y,
            string[] prerequisites, params (string stat, float amount)[] effects)
        {
            var node = new UpgradeNode
            {
                id = id,
                category = category,
                cost = cost,
                position = new Vector2(x, y),
                prerequisites = prerequisites != null ? new List<string>(prerequisites) : new List<string>()
            };

            foreach (var (stat, amount) in effects)
                node.effects.Add(new UpgradeEffectEntry { statName = stat, amount = amount });

            return node;
        }
    }
}
