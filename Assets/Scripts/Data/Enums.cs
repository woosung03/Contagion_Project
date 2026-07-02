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

    /// <summary>
    /// 국가별 개별 붕괴 단계. 사망률(deadCount / population) 기준.
    /// 나무위키 Plague Inc./상태 문서 참고 — 원본은 사망률 20/50/70/95/100%로 6단계를 나누고
    /// 국가별로 개별 판정한다 (전 세계 공통 ResistanceStage와 별개). Docs/PlagueIncReference.md 1절 참고.
    /// </summary>
    public enum CountryCollapseStage
    {
        Normal,          // 평시 (사망률 20% 미만) — 연구 100% 가동
        FullCollapse,    // 전면적 붕괴 (20% 이상) — 아직 연구 100% 가동, 혼란 시작
        Disorder,        // 무질서 팽배 (50% 이상) — 연구 가동률 50~70%
        NearAnarchy,     // 무정부상태 근접 (70% 이상) — 연구 가동률 20~30%
        FullAnarchy,     // 완전한 무정부 상태 (95% 이상) — 정부 몰락, 연구 중단, 치안 붕괴로 추가 사망
        Extinct          // 국가 소멸 (100%)
    }

    /// <summary>
    /// 세계 전체 사망률 기준 위험도 단계. 나무위키 Plague Inc./상태 문서의 "세계를 위협(사망 1~20%)" /
    /// "인류 멸종 임박(사망 20%+)" 문구를 반영 (Docs/PlagueIncReference.md 2절).
    /// 기존 ResistanceStage(plagueVisibility 기준, 인류의 대응 단계)와는 별개 축이다 — 이쪽은
    /// "얼마나 퍼졌는지가 아니라 얼마나 많이 죽었는지"만 본다. 둘을 조합해 UI에 노출한다.
    /// </summary>
    public enum WorldMortalityStage
    {
        Stable,             // 사망률 0% — 안정적
        EmergingThreat,     // 사망률 0% 초과 ~ 1% 미만 — 위협 시작
        WorldThreatened,    // 사망률 1% 이상 ~ 20% 미만 — 세계를 위협
        ExtinctionImminent  // 사망률 20% 이상 — 인류 멸종 임박
    }
}
