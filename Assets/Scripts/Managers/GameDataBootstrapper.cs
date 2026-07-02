using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// ScriptableObject 데이터 자산을 런타임 인스턴스로 복제해 각 매니저에 주입한다. 설계 문서 Step 9.
    /// GamePlay 씬의 Bootstrap 오브젝트에 다른 매니저들과 함께 배치하고, 인스펙터에서
    /// countryDatabase / selectedPathogen / upgradeTreeDatabase를 연결한다.
    ///
    /// 병원체/발원 국가 선택(MainMenu, CountrySelect 씬)은 아직 구현되지 않았으므로,
    /// 현재는 인스펙터에 지정한 selectedPathogen과 startingCountryId로 고정 시작한다.
    /// 씬 전환이 붙으면 이 필드들을 정적 GameSessionConfig 등으로 전달받도록 교체할 것.
    /// </summary>
    public class GameDataBootstrapper : MonoBehaviour
    {
        [SerializeField] private CountryDatabase countryDatabase;
        [SerializeField] private PathogenDefinition selectedPathogen;
        [SerializeField] private UpgradeTreeDatabase upgradeTreeDatabase;
        [SerializeField] private string startingCountryId;
        [SerializeField] private long startingInfectedCount = 100;

        private void Start()
        {
            if (WorldDataManager.Instance == null || UpgradeManager.Instance == null)
            {
                Debug.LogError("[GameDataBootstrapper] WorldDataManager/UpgradeManager가 씬에 없습니다.");
                return;
            }

            if (countryDatabase != null)
            {
                var runtimeCountries = countryDatabase.CreateRuntimeInstances();
                WorldDataManager.Instance.SetCountries(runtimeCountries);
            }
            else
            {
                Debug.LogWarning("[GameDataBootstrapper] countryDatabase 미지정 — 빈 국가 목록으로 시작합니다.");
            }

            if (selectedPathogen != null)
            {
                WorldDataManager.Instance.SetPathogen(selectedPathogen.CreateRuntimeInstance());
            }
            else
            {
                Debug.LogWarning("[GameDataBootstrapper] selectedPathogen 미지정 — 기본 Pathogen()으로 시작합니다.");
            }

            if (upgradeTreeDatabase != null)
            {
                UpgradeManager.Instance.SetTree(upgradeTreeDatabase.CreateRuntimeInstances());
            }
            else
            {
                // UpgradeTreeDatabase 에셋을 아직 만들지 않았어도 바로 플레이할 수 있도록,
                // 코드로 정의된 27노드 세분화 트리(감염경로/증상/능력 각 9개)를 폴백으로 사용한다.
                // (Docs/PlagueIncReference.md 참고, DefaultUpgradeTreeFactory 참고)
                Debug.Log("[GameDataBootstrapper] upgradeTreeDatabase 미지정 — DefaultUpgradeTreeFactory의 " +
                    "27노드 세분화 트리로 시작합니다.");
                UpgradeManager.Instance.SetTree(DefaultUpgradeTreeFactory.BuildDefaultDetailedTree());
            }

            SeedStartingInfection();
        }

        /// <summary>발원 국가에 초기 감염자를 심는다. 설계 문서 2절 "발원 국가 선택".</summary>
        private void SeedStartingInfection()
        {
            if (string.IsNullOrEmpty(startingCountryId)) return;

            var country = WorldDataManager.Instance.GetCountry(startingCountryId);
            if (country == null)
            {
                Debug.LogWarning($"[GameDataBootstrapper] startingCountryId '{startingCountryId}'를 찾을 수 없습니다.");
                return;
            }

            country.infectedCount = System.Math.Min(startingInfectedCount, country.SusceptibleCount);
            WorldDataManager.Instance.NotifyCountryChanged(country);
            WorldDataManager.Instance.RecalculateWorldTotals();
        }
    }
}
