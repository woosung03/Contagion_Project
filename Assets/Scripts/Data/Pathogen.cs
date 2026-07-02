using System;
using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>기후별 환경 내성 항목. Pathogen.environmentResistance 참고.</summary>
    [Serializable]
    public struct ClimateResistanceEntry
    {
        public ClimateType climate;
        [Range(0f, 1f)] public float resistance;
    }

    /// <summary>
    /// 병원체 데이터. 설계 문서 3.3절.
    /// MonoBehaviour가 아닌 순수 데이터 클래스 — SimulationManager/UpgradeManager가 참조/수정한다.
    /// Step 9에서 ScriptableObject로 감쌀 예정.
    /// </summary>
    [Serializable]
    public class Pathogen
    {
        public PathogenType type = PathogenType.Bacteria;

        [Range(0f, 1f)] public float infectivity = 0.1f;      // 감염력
        [Range(0f, 1f)] public float severity = 0.1f;          // 중증도 (높을수록 가시성↑)
        [Range(0f, 1f)] public float lethality = 0.05f;        // 치사율
        [Range(0f, 1f)] public float drugResistance = 0f;      // 약물 내성

        // 원 설계 문서는 float[] {Cold, Hot, Humid, Arid} 로 정의했으나,
        // Country.climate(ClimateType: Arid/Temperate/Cold/Humid)와 순서가 어긋나는 것을 막기 위해
        // climate -> resistance 매핑 리스트로 구현한다 (Inspector에서 직접 편집 가능).
        public List<ClimateResistanceEntry> environmentResistance = new List<ClimateResistanceEntry>();

        public List<TransmissionRoute> transmissionRoutes = new List<TransmissionRoute>();

        /// <summary>
        /// 해당 기후에서의 환경 내성 값(0~1). 등록되지 않은 기후는 1(중립, 영향 없음)을 반환한다.
        /// 0을 기본값으로 두면 데이터 미설정 시 전파가 완전히 막혀버리므로 중립값 1을 기본으로 한다.
        /// </summary>
        public float GetEnvironmentResistance(ClimateType climate)
        {
            for (int i = 0; i < environmentResistance.Count; i++)
            {
                if (environmentResistance[i].climate == climate)
                    return environmentResistance[i].resistance;
            }
            return 1f;
        }

        public bool HasTransmissionRoute(TransmissionRoute route) => transmissionRoutes.Contains(route);

        /// <summary>
        /// 깊은 복사본 생성. Step 9: PathogenDefinition(ScriptableObject)은 "템플릿"이므로
        /// 플레이 중 업그레이드로 원본 자산이 오염되지 않도록 런타임 시작 시 반드시 Clone해서 사용한다.
        /// </summary>
        public Pathogen Clone()
        {
            return new Pathogen
            {
                type = type,
                infectivity = infectivity,
                severity = severity,
                lethality = lethality,
                drugResistance = drugResistance,
                environmentResistance = new List<ClimateResistanceEntry>(environmentResistance),
                transmissionRoutes = new List<TransmissionRoute>(transmissionRoutes)
            };
        }
    }
}
