namespace Contagion.Data
{
    /// <summary>병원체 종류. 설계 문서 3.3 / 6절.</summary>
    public enum PathogenType
    {
        Bacteria,
        Virus,
        Fungus,
        Parasite,
        Nano,
        Prion,
        BioWeapon,
        Necroa,
        Neurax
    }

    /// <summary>국가 기후. 설계 문서 3.2절.</summary>
    public enum ClimateType
    {
        Arid,
        Temperate,
        Cold,
        Humid
    }

    /// <summary>국가 의료/개발 수준. 설계 문서 3.2 / 5절.</summary>
    public enum DevelopmentLevel
    {
        Low,
        Mid,
        High
    }

    /// <summary>감염 경로. 설계 문서 2절 트리 업그레이드 항목.</summary>
    public enum TransmissionRoute
    {
        Air,
        Water,
        Contact,
        Animal,
        Insect,
        Blood
    }

    /// <summary>업그레이드 노드 카테고리. 설계 문서 3.4절.</summary>
    public enum UpgradeCategory
    {
        Transmission,
        Symptom,
        Ability
    }

    /// <summary>게임 진행 페이즈. 설계 문서 10절.</summary>
    public enum GamePhase
    {
        Incubation,   // Phase 1 - 잠복기
        Spread,       // Phase 2 - 확산기
        Endgame       // Phase 3 - 결정기
    }

    /// <summary>난이도. 설계 문서 9 / 14절.</summary>
    public enum Difficulty
    {
        Casual,
        Normal,
        Brutal,
        MegaBrutal
    }

    /// <summary>
    /// 인류 저항 단계. plagueVisibility 구간에 따라 결정된다. 설계 문서 5절.
    /// 0.0~0.2 / 0.2~0.4 / 0.4~0.6 / 0.6~0.8 / 0.8~1.0
    /// </summary>
    public enum ResistanceStage
    {
        NoAwareness,            // 인식 없음
        DiseaseReported,        // 질병 보도 (마스크, 손씻기)
        PublicHealthEmergency,  // 공중보건 비상사태 (격리, 연구 가속)
        NationalEmergency,      // 국가 비상사태 (국경 봉쇄, 항공/항구 폐쇄)
        WorldCollapse           // 세계 붕괴 (무정부, 연구 감속)
    }
}
