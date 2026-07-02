using System;
using System.Collections.Generic;

namespace Contagion.Data
{
    /// <summary>
    /// 국가 데이터. 설계 문서 3.2절.
    /// airRouteCountryIds / seaRouteCountryIds / neighborCountryIds 는 설계 문서 4.1 "국가 간 전파"
    /// 계산에 필요한 연결 그래프로, Step 9(ScriptableObject 데이터화) 전까지는 수동으로 채운다.
    /// </summary>
    [Serializable]
    public class Country
    {
        public string id;
        public string name;

        public long population;
        public long infectedCount;
        public long deadCount;

        public ClimateType climate;
        public DevelopmentLevel developmentLevel;

        public bool isAirportOpen = true;
        public bool isPortOpen = true;
        public bool isBorderClosed = false;

        [UnityEngine.Range(0f, 1f)] public float healthFunding;        // 치료제 기여도
        [UnityEngine.Range(0f, 1f)] public float governmentStability = 1f; // 0=무정부, 1=안정

        /// <summary>
        /// 치료 자금 투자 한계치 (0~1). 기본 1(제한 없음) — 나무위키 "자연재해로 사망자가 나면 그 나라의
        /// 치료 자금 투자 한계치 자체가 낮아진다"를 반영 (Docs/PlagueIncReference.md 4절). 자연재해 이벤트가
        /// 뜬 국가는 이 값이 영구적으로 깎이고, HumanResistanceManager.ApplyPolicy()가 healthFunding을
        /// 계산한 뒤 이 상한선으로 다시 한 번 클램프한다.
        /// </summary>
        [UnityEngine.Range(0f, 1f)] public float healthFundingCap = 1f;

        // 국가 간 전파용 연결 그래프 (국가 id 목록)
        public List<string> neighborCountryIds = new List<string>();   // 육상 국경 인접국
        public List<string> airRouteCountryIds = new List<string>();   // 항공 노선 연결국
        public List<string> seaRouteCountryIds = new List<string>();   // 해운 노선 연결국

        /// <summary>사망자를 제외한 생존 인구.</summary>
        public long LivingPopulation => Math.Max(0, population - deadCount);

        /// <summary>아직 감염되지 않은 생존 인구 (감염 가능 대상).</summary>
        public long SusceptibleCount => Math.Max(0, LivingPopulation - infectedCount);

        /// <summary>
        /// 국가 의료 수준 (0~1). 설계 문서 4.1의 countryHealthLevel / 4.2의 healthcareCapacity에 사용.
        /// developmentLevel로부터 유도 — 설계 문서에 명시적 공식이 없어 합리적으로 매핑한 값.
        /// </summary>
        public float HealthLevel => developmentLevel switch
        {
            DevelopmentLevel.High => 0.8f,
            DevelopmentLevel.Mid => 0.5f,
            DevelopmentLevel.Low => 0.2f,
            _ => 0.3f
        };

        /// <summary>
        /// 치료제 연구 기여 배율. 설계 문서 4.3 researchMultiplier / 5절 "선진국: 높은 연구 기여도".
        /// </summary>
        public float ResearchMultiplier => developmentLevel switch
        {
            DevelopmentLevel.High => 1.5f,
            DevelopmentLevel.Mid => 0.8f,
            DevelopmentLevel.Low => 0.2f,
            _ => 0.5f
        };

        /// <summary>
        /// 국가별 개별 붕괴 단계. 나무위키 Plague Inc./상태 문서 기준 사망률 임계값
        /// (Docs/PlagueIncReference.md 1절). 전 세계 공통 ResistanceStage와 별개로,
        /// "국가 A는 이미 무정부 상태인데 국가 B는 멀쩡하다"를 개별적으로 표현하기 위함.
        /// </summary>
        public CountryCollapseStage GetCollapseStage()
        {
            if (population <= 0) return CountryCollapseStage.Normal;
            float deathRatio = (float)deadCount / population;

            if (deathRatio >= 1f) return CountryCollapseStage.Extinct;
            if (deathRatio >= 0.95f) return CountryCollapseStage.FullAnarchy;
            if (deathRatio >= 0.7f) return CountryCollapseStage.NearAnarchy;
            if (deathRatio >= 0.5f) return CountryCollapseStage.Disorder;
            if (deathRatio >= 0.2f) return CountryCollapseStage.FullCollapse;
            return CountryCollapseStage.Normal;
        }

        /// <summary>
        /// 깊은 복사본 생성. Step 9: CountryDatabase(ScriptableObject)는 "템플릿"이므로
        /// 매 게임 시작 시 Clone해서 런타임 인스턴스를 만들어야 원본 자산이 오염되지 않는다.
        /// </summary>
        public Country Clone()
        {
            return new Country
            {
                id = id,
                name = name,
                population = population,
                infectedCount = infectedCount,
                deadCount = deadCount,
                climate = climate,
                developmentLevel = developmentLevel,
                isAirportOpen = isAirportOpen,
                isPortOpen = isPortOpen,
                isBorderClosed = isBorderClosed,
                healthFunding = healthFunding,
                healthFundingCap = healthFundingCap,
                governmentStability = governmentStability,
                neighborCountryIds = new List<string>(neighborCountryIds),
                airRouteCountryIds = new List<string>(airRouteCountryIds),
                seaRouteCountryIds = new List<string>(seaRouteCountryIds)
            };
        }
    }
}
