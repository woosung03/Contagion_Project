using UnityEngine;

namespace Contagion.Data
{
    /// <summary>
    /// 병원체 하나를 데이터 자산으로 만든 것. 설계 문서 Step 9 / 6절(전염병 종류).
    /// 인스펙터에서 값을 채운 뒤, 게임 시작 시 CreateRuntimeInstance()로 복제해서 사용한다
    /// (원본 자산은 절대 직접 수정하지 않는다 — 업그레이드 효과는 복제본에만 적용됨).
    /// </summary>
    [CreateAssetMenu(fileName = "NewPathogen", menuName = "Contagion/Pathogen Definition")]
    public class PathogenDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "새 병원체";
        [SerializeField] private string flavorText;
        [SerializeField] private Pathogen data = new Pathogen();

        public string DisplayName => displayName;
        public string FlavorText => flavorText;
        public PathogenType Type => data.type;

        // MainMenu 병원체 선택 화면 표시용 — CreateRuntimeInstance()로 매번 복제하지 않고
        // 원본 자산 값을 그대로 읽기 전용으로 노출한다 (선택 미리보기는 복제할 필요가 없음).
        public float Infectivity => data.infectivity;
        public float Severity => data.severity;
        public float Lethality => data.lethality;
        public float DrugResistance => data.drugResistance;

        /// <summary>런타임에서 사용할 복제본. 원본 자산(data)은 절대 직접 넘기지 않는다.</summary>
        public Pathogen CreateRuntimeInstance() => data.Clone();
    }
}
