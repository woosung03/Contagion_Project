using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Contagion.Data
{
    /// <summary>
    /// 전체 국가 데이터를 하나의 자산으로 관리. 설계 문서 Step 9.
    /// WorldDataManager는 이 자산이 아니라 CreateRuntimeInstances()가 반환하는 복제 리스트를 사용해야 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "CountryDatabase", menuName = "Contagion/Country Database")]
    public class CountryDatabase : ScriptableObject
    {
        [SerializeField] private List<Country> countries = new List<Country>();

        public IReadOnlyList<Country> Countries => countries;

        /// <summary>런타임에서 사용할 복제 리스트 (원본 자산은 수정하지 않음).</summary>
        public List<Country> CreateRuntimeInstances() => countries.Select(c => c.Clone()).ToList();
    }
}
