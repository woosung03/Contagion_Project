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
#if UNITY_EDITOR
        /// <summary>
        /// Debug Auto Unlock — 밸런스 반복 테스트용 개발 전용 목록. UNITY_EDITOR 빌드에만 존재하는
        /// 필드라(클래스 자체가 #if UNITY_EDITOR 블록 안) Android/Release 빌드에는 이 필드도,
        /// 이걸 읽는 <see cref="ApplyDebugAutoUnlock"/>도 컴파일 결과물에 전혀 포함되지 않는다.
        /// 새 노드를 추가/제거하고 싶으면 이 배열만 수정하면 된다(다른 파일에 흩어져 있지 않음).
        /// 값은 UpgradeNode.id(DefaultUpgradeTreeFactory.cs 참고, 예: "trans_air1") — 선행 연구가
        /// 있는 노드는 그 선행 노드를 먼저 나열해야 한다(ApplyDebugAutoUnlock이 순서대로 처리하며,
        /// 기존 UpgradeManager.TryUnlock()이 선행조건 미충족 시 그대로 실패시키기 때문).
        /// </summary>
        // Debug Auto Unlock
        // 선행 연구 → 후속 연구 순으로 작성한다.
        // UpgradeNode.id 값을 사용한다(DefaultUpgradeTreeFactory.cs 참고).
        // UNITY_EDITOR에서만 적용된다 — 테스트할 노드만 이 배열에 추가/제거하면 된다.
        private static readonly string[] DebugAutoUnlockNodeIds =
        {
            "trans_air1",
            "trans_air2",
            "trans_droplet1",
            "sym_cough",
            "abl_mutation1",
        };
#endif

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
            BottleneckAnalyzer.Instance?.ResetForNewGame();
            ResearchRecommender.Instance?.ResetForNewGame();
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
#if UNITY_EDITOR
            ApplyDebugAutoUnlock();
#endif
            GameManager.Instance?.SetPaused(false);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Debug Auto Unlock — 반복적인 밸런스 테스트에서 매번 같은 초반 연구를 수동으로 찍는 수고를
        /// 덜기 위한 에디터 전용 기능. 새 치트/내부 데이터 조작이 아니라 기존
        /// UpgradeManager.AddDna()/TryUnlock()을 순서대로 호출할 뿐이다 — TryUnlock()이 요구하는
        /// DNA를 AddDna()로 그때그때 정확히 지급한 뒤 TryUnlock()을 부르므로, 선행조건/비용 판정
        /// (CanUnlock) 등 기존 해금 로직이 실제 플레이와 동일하게 그대로 적용된다(node.isUnlocked를
        /// 직접 대입하지 않음). SetPaused(false) 직전, 즉 발원 감염을 심은 직후·플레이어가 실제로
        /// 조작을 시작하기 전에 호출해 자동 해금 화면이 보이지 않는다.
        /// </summary>
        private void ApplyDebugAutoUnlock()
        {
            var upgrades = UpgradeManager.Instance;
            if (upgrades == null || DebugAutoUnlockNodeIds.Length == 0) return;

            foreach (var nodeId in DebugAutoUnlockNodeIds)
            {
                if (upgrades.IsUnlocked(nodeId)) continue;

                int cost = upgrades.GetEffectiveCost(nodeId);
                upgrades.AddDna(cost);

                if (!upgrades.TryUnlock(nodeId))
                {
                    // TryUnlock()이 실패하면(CanUnlock()이 막아 SpendDna() 자체가 호출되지 않는
                    // 경로 — id 오타/선행 연구 미해금) 방금 지급한 DNA가 그대로 남는다. TryUnlock()/
                    // CanUnlock()은 건드리지 않고, 이미 쓰던 AddDna()로 같은 양만큼 되돌려서 테스트용
                    // DNA가 플레이어에게 남지 않도록 한다.
                    upgrades.AddDna(-cost);
                    Debug.LogWarning($"[GameDataBootstrapper] Debug Auto Unlock 실패: '{nodeId}' " +
                        "(id 오타 또는 선행 연구가 목록에서 먼저 나오지 않음 — DebugAutoUnlockNodeIds 순서 확인). " +
                        "지급했던 DNA는 환수함.");
                }
            }
        }
#endif

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
