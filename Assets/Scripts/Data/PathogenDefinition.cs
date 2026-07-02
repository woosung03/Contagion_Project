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

        /// <summary>런타임에서 사용할 복제본. 원본 자산(data)은 절대 직접 넘기지 않는다.</summary>
        public Pathogen CreateRuntimeInstance() => data.Clone();
    }
}
