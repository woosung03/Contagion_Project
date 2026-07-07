using System;

namespace Contagion.Data
{
    /// <summary>
    /// 전역 세계 상태. 설계 문서 3.1절.
    /// </summary>
    [Serializable]
    public class WorldState
    {
        public long totalPopulation;      // 전 세계 총 인구
        public long infectedCount;        // 현재 감염자 수
        public long deadCount;            // 누적 사망자 수
        [UnityEngine.Range(0f, 1f)] public float cureProgress;       // 치료제 개발 진행률 (0~1)
        [UnityEngine.Range(0f, 1f)] public float plagueVisibility;   // 전염병 노출도 (0~1)
        public int dnaPoints;             // 플레이어 보유 DNA 포인트
        public int currentDay;            // 경과 일수

        /// <summary>
        /// 치료제 연구가 실제로 "시작"됐는지 — SimulationManager가 매 틱 감염자/사망자 수 기반 확률로
        /// 판정해 한 번 true가 되면 그때부터 cureProgress가 오르기 시작한다(그 전엔 0에 고정).
        /// 실제 전염병처럼 초반엔 아무도 모르다가 피해가 커질수록 "발견되어 보도될" 확률이 높아지는
        /// 방식을 표현하기 위한 플래그.
        /// </summary>
        public bool cureResearchStarted;

        /// <summary>
        /// [Step 54] "게임 시작 버튼을 누르면 즉시 패배(광고 보고 부활/다시 시작) 화면이 뜬다" 버그 수정용
        /// 플래그. 문제의 원인: <see cref="IsPathogenEradicated"/>가 "infectedCount &lt;= 0"만으로
        /// 패배를 판정하는데, 이 조건은 "치료제 완성으로 병원체가 박멸된 상태"와 "아직 발원 감염이
        /// 심어지기 전(=새 게임 막 시작 직후)"을 구분하지 못한다 — 둘 다 infectedCount가 0이기 때문.
        /// 감염이 실제로 한 번이라도 기록된 적이 있어야만(RecalculateWorldTotals()가 감지) 그 뒤로
        /// infectedCount가 0이 되는 걸 "박멸"로 인정하도록 게이트를 건다(cureResearchStarted와 같은
        /// 패턴 — 절대값만으로는 "아직 시작 전"과 "끝난 후"를 구분 못 하는 문제에 항상 이 방식을 씀).
        /// </summary>
        public bool hasEverBeenInfected;

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 호출. totalPopulation/infectedCount/deadCount는 어차피
        /// WorldDataManager.SetCountries() 직후 RecalculateWorldTotals()가 다시 계산하지만,
        /// cureProgress/plagueVisibility/dnaPoints/currentDay는 그 계산에 포함되지 않아 별도로
        /// 초기화하지 않으면 이전 판 값이 그대로 남는다(예: 치료제 100%로 이겼던 판 다음에 새 게임을
        /// 시작해도 cureProgress가 1로 시작해 몇 틱 만에 즉시 종료돼버리는 버그).
        /// </summary>
        public void Reset()
        {
            totalPopulation = 0;
            infectedCount = 0;
            deadCount = 0;
            cureProgress = 0f;
            plagueVisibility = 0f;
            dnaPoints = 0;
            currentDay = 0;
            cureResearchStarted = false;
            hasEverBeenInfected = false;
        }

        public void AddDna(int amount) => dnaPoints = Math.Max(0, dnaPoints + amount);

        public bool SpendDna(int amount)
        {
            if (dnaPoints < amount) return false;
            dnaPoints -= amount;
            return true;
        }

        /// <summary>승리 조건: 전 세계 인구 전멸. 설계 문서 1절.</summary>
        public bool IsHumanityExtinct => deadCount >= totalPopulation && totalPopulation > 0;

        /// <summary>
        /// 패배 조건: 감염자 0명 + 생존자 존재. 설계 문서 1절 원문은 "(치료제 완성 후 박멸)"이라고
        /// 부연하지만, 이는 보통 그렇게 된다는 서술이지 조건은 아니다 — cureProgress와 무관하게
        /// 감염자가 0이 되고 생존자가 있으면 패배로 처리한다. cureProgress < 1인 상태에서 병원체가
        /// 스스로 소멸(전파력 부족으로 자연 소멸)해도 플레이어 입장에선 동일한 패배이기 때문.
        /// (예전엔 cureProgress >= 1f도 같이 요구해서, 마지막 생존자 1명이 남고 병원체가 이미
        /// 자연 소멸한 상태에서 cureProgress가 100%가 아니면 영원히 게임이 안 끝나는 버그가 있었음.)
        ///
        /// [Step 54] hasEverBeenInfected 게이트 추가 — "감염자 0명"은 발원 감염이 아직 심어지기 전
        /// (새 게임 막 시작한 직후)에도 참이라, 게이트 없이는 게임 시작 버튼을 누르자마자 이 조건이
        /// 바로 만족돼 즉시 패배 화면(광고 보고 부활/다시 시작)이 뜨는 버그가 있었다. 감염이 실제로
        /// 한 번이라도 있었던 뒤에만(RecalculateWorldTotals가 감지) 그 이후 0이 되는 걸 "박멸"로 본다.
        /// </summary>
        public bool IsPathogenEradicated => hasEverBeenInfected && infectedCount <= 0 && deadCount < totalPopulation;

        /// <summary>plagueVisibility 값을 5개 구간으로 분류. 설계 문서 5절.</summary>
        public ResistanceStage GetResistanceStage()
        {
            if (plagueVisibility < 0.2f) return ResistanceStage.NoAwareness;
            if (plagueVisibility < 0.4f) return ResistanceStage.DiseaseReported;
            if (plagueVisibility < 0.6f) return ResistanceStage.PublicHealthEmergency;
            if (plagueVisibility < 0.8f) return ResistanceStage.NationalEmergency;
            return ResistanceStage.WorldCollapse;
        }

        /// <summary>
        /// 전 세계 사망률(deadCount / totalPopulation) 기준 위험도 단계. 나무위키 "세계를 위협"/
        /// "인류 멸종 임박" 문구 반영 (Docs/PlagueIncReference.md 2절). GetResistanceStage()와 별개 축.
        /// </summary>
        public WorldMortalityStage GetMortalityStage()
        {
            if (totalPopulation <= 0 || deadCount <= 0) return WorldMortalityStage.Stable;
            float deathRatio = (float)deadCount / totalPopulation;
            if (deathRatio >= 0.2f) return WorldMortalityStage.ExtinctionImminent;
            if (deathRatio >= 0.01f) return WorldMortalityStage.WorldThreatened;
            return WorldMortalityStage.EmergingThreat;
        }
    }
}
