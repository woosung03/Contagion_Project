namespace Contagion.Data
{
    /// <summary>
    /// BottleneckAnalyzer의 산출물 — 읽기 전용 스냅샷. 새 시뮬레이션 데이터가 아니라 WorldState/
    /// Country/Pathogen 기존 필드를 매틱 파생 계산한 결과물이라 SaveManager 저장 대상이 아니다
    /// (게임 로드 시 히스토리가 다시 쌓이면서 자연스럽게 재계산됨).
    /// </summary>
    public class BottleneckReport
    {
        public BottleneckType Type { get; }
        public BottleneckSeverity Severity { get; }

        /// <summary>true면 연구로 실제로 해결 가능한 병목 — ResearchRecommender가 추천을 생성한다.
        /// false면 정보성 병목이라 추천 리스트는 항상 비어 있다(§ 설계 원칙: 거짓 희망 금지).</summary>
        public bool Actionable { get; }

        /// <summary>Actionable일 때만 의미 있음: "infectivity"/"severity"/"lethality"/"drugResistance"
        /// 중 하나 — UpgradeNode.GetEffect()에 그대로 넘겨 점수를 계산하는 키.</summary>
        public string RelevantStat { get; }

        /// <summary>판정 근거가 된 원시 수치(문장 생성/디버그 로그용, 이번 라운드는 UI 미연결이라
        /// Debug.Log 확인 용도로만 쓰인다).</summary>
        public string Evidence { get; }

        public BottleneckReport(BottleneckType type, BottleneckSeverity severity, bool actionable,
            string relevantStat, string evidence)
        {
            Type = type;
            Severity = severity;
            Actionable = actionable;
            RelevantStat = relevantStat;
            Evidence = evidence;
        }

        public static readonly BottleneckReport None =
            new BottleneckReport(BottleneckType.None, BottleneckSeverity.Low, false, null, string.Empty);
    }
}
