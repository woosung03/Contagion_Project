using System.Collections.Generic;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// ScriptableObject 데이터 자산을 런타임 인스턴스로 복제해 각 매니저에 주입한다. 설계 문서 Step 9.
    /// GamePlay 씬의 Bootstrap 오브젝트에 다른 매니저들과 함께 배치하고, 인스펙터에서
    /// countryDatabase / selectedPathogen / upgradeTreeDatabase를 연결한다.
    ///
    /// UI/UX 폴리싱(MainMenu/CountrySelect 화면) 추가로 국가/트리 데이터는 여전히 Start()에서
    /// 즉시 로드하지만(뒤에 지도가 보이는 게 자연스러움), 병원체 선택 + 발원 감염 시딩은 더 이상
    /// 자동으로 하지 않는다 — MainMenuController → CountrySelectController → UIManager가 플레이어의
    /// 선택을 모아 <see cref="BeginGame"/>을 호출해야 실제로 게임이 시작된다. 그 전까지
    /// GameManager.isPaused가 기본 true라 SimulationManager 틱이 돌지 않는다(GameManager.cs 참고).
    /// </summary>
    public class GameDataBootstrapper : MonoBehaviour
    {
        public static GameDataBootstrapper Instance { get; private set; }

        [SerializeField] private CountryDatabase countryDatabase;
        [SerializeField] private PathogenDefinition selectedPathogen;
        [SerializeField] private UpgradeTreeDatabase upgradeTreeDatabase;
        [SerializeField] private string startingCountryId;
        [SerializeField] private long startingInfectedCount = 100;

        [Header("MainMenu 병원체 선택 화면에 노출할 목록")]
        [SerializeField] private PathogenDefinition[] availablePathogens;

        [Header("개발/테스트용")]
        [SerializeField, Tooltip("켜면 MainMenu/CountrySelect를 거치지 않고 selectedPathogen/startingCountryId로 즉시 시작 " +
            "(Step 9 시절 동작 — 빠른 플레이테스트용).")]
        private bool skipMainMenu = false;

        public IReadOnlyList<PathogenDefinition> AvailablePathogens => availablePathogens;

        /// <summary>CountrySelect 화면이 목록을 그릴 때 쓰는 국가 템플릿 목록 (읽기 전용 — 런타임 인스턴스 아님).</summary>
        public IReadOnlyList<Country> AvailableCountries =>
            countryDatabase != null ? countryDatabase.Countries : System.Array.Empty<Country>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (WorldDataManager.Instance == null || UpgradeManager.Instance == null)
            {
                Debug.LogError("[FLOW][GameDataBootstrapper] WorldDataManager/UpgradeManager가 씬에 없습니다.");
                return;
            }

            ResetPersistentManagersForNewGame();

            if (countryDatabase != null)
            {
                var runtimeCountries = countryDatabase.CreateRuntimeInstances();
                WorldDataManager.Instance.SetCountries(runtimeCountries);
            }
            else
            {
                Debug.LogWarning("[GameDataBootstrapper] countryDatabase 미지정 — 빈 국가 목록으로 시작합니다.");
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
                UpgradeManager.Instance.SetTree(DefaultUpgradeTreeFactory.BuildDefaultDetailedTree());
            }

            if (skipMainMenu)
            {
                Debug.Log("[FLOW][GameDataBootstrapper] skipMainMenu 켜짐 — MainMenu 없이 즉시 시작합니다.");
                BeginGame(selectedPathogen, startingCountryId);
            }
        }

        /// <summary>
        /// GameManager/WorldDataManager/SimulationManager/HumanResistanceManager/EventManager/SaveManager는
        /// 전부 DontDestroyOnLoad라 "재시작"(씬 리로드)해도 죽지 않고 이전 판 상태를 그대로 들고 살아남는다.
        /// countryDatabase.CreateRuntimeInstances()로 국가만 새로 주입해서는 이 매니저들 내부의 누적
        /// 값(cureProgress, _gameEnded, 이벤트 쿨다운/1회성 플래그, 저항 단계 캐시 등)이 초기화되지
        /// 않는다 — 그중 SimulationManager._gameEnded는 특히 치명적이라(리셋 안 하면 재시작한 새 게임의
        /// 틱이 영원히 멈춘 채로 시작됨), Start()가 실행될 때마다(첫 시작이든 재시작이든) 항상 먼저
        /// 호출해서 전부 깨끗한 상태로 되돌린다.
        /// </summary>
        private void ResetPersistentManagersForNewGame()
        {
            GameManager.Instance?.ResetForNewGame();
            WorldDataManager.Instance?.ResetForNewGame();
            SimulationManager.Instance?.ResetForNewGame();
            HumanResistanceManager.Instance?.ResetForNewGame();
            EventManager.Instance?.ResetForNewGame();
            SaveManager.Instance?.ResetForNewGame();
            TransportManager.Instance?.ResetForNewGame();
        }

        /// <summary>
        /// MainMenu(병원체 선택) + CountrySelect(발원국 선택) 완료 시 UIManager가 호출한다.
        /// 병원체를 실제로 주입하고 발원 감염을 심은 뒤 시뮬레이션 일시정지를 해제한다.
        /// </summary>
        public void BeginGame(PathogenDefinition pathogen, string countryId)
        {
            var effectivePathogen = pathogen != null ? pathogen : selectedPathogen;
            if (effectivePathogen != null)
            {
                WorldDataManager.Instance.SetPathogen(effectivePathogen.CreateRuntimeInstance());
            }
            else
            {
                Debug.LogWarning("[FLOW][GameDataBootstrapper] BeginGame — 병원체가 지정되지 않아 기본 Pathogen()으로 시작합니다.");
            }

            startingCountryId = countryId;
            SeedStartingInfection();
            GameManager.Instance?.SetPaused(false);
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
