namespace Contagion.Data
{
    /// <summary>
    /// 병목 유형. Phase 1은 SpreadStalled_LowInfectivity(A4) / DeathLow_LowLethality(B2) /
    /// DetectionFast_HighSeverity(C1) / CureRace_Urgent(D2) / ResourceStarved(E) 5종만
    /// BottleneckAnalyzer가 실제로 판정한다. 나머지(정보성 병목, A1/A2/A3/B1/D1/D3/C2)는
    /// Phase 1.5에서 추가할 자리만 미리 등재해둔다 — 지금은 어떤 코드도 이 값들을 반환하지 않는다.
    /// </summary>
    public enum BottleneckType
    {
        None,
        SpreadStalled_Lockdown,        // A1 — Phase 1.5
        SpreadStalled_Climate,         // A2 — Phase 1.5
        SpreadStalled_Isolation,       // A3 — Phase 1.5
        SpreadStalled_LowInfectivity,  // A4 — Phase 1
        DeathLow_HealthAbsorption,     // B1 — Phase 1.5
        DeathLow_LowLethality,         // B2 — Phase 1
        DetectionFast_HighSeverity,    // C1 — Phase 1
        DetectionFast_ScaleDriven,     // C2 — Phase 1.5
        CureRace_NotStarted,           // D1 — Phase 1.5
        CureRace_Urgent,               // D2 — Phase 1
        CureRace_Safe,                 // D3 — Phase 1.5
        ResourceStarved                // E  — Phase 1
    }

    /// <summary>병목의 긴급도. ResearchRecommender의 점수 가중치, 향후 UI 강조 수준에 사용.</summary>
    public enum BottleneckSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}
