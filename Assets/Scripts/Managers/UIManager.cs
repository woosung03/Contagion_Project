using Contagion.Ads;
using Contagion.Data;
using Contagion.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Contagion.Managers
{
    /// <summary>
    /// UI 패널 간 조정 담당. 설계 문서 12절 Core Manager.
    /// 각 컨트롤러(HudController, CountryPopupController, EndingScreenController 등)는
    /// 자기 데이터 소스(WorldDataManager, WorldMap, SimulationManager 등)에 직접 구독하고,
    /// UIManager는 "여러 컨트롤러에 걸친" 조정(패널 열기/재시작/광고 연동)만 담당한다.
    ///
    /// GamePlay 씬의 Bootstrap(또는 UI) 오브젝트에 다른 UI 컨트롤러들과 함께 배치하고
    /// 인스펙터에서 4개 참조를 연결한다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private HudController hudController;

        [Header("업그레이드 트리 — 카테고리별 완전히 별개의 창 3개 (UI/UX 폴리싱)")]
        [SerializeField] private UpgradeTreeView transmissionTreeView;
        [SerializeField] private UpgradeTreeView symptomTreeView;
        [SerializeField] private UpgradeTreeView abilityTreeView;

        [SerializeField] private CountryPopupController countryPopupController;
        [SerializeField] private EndingScreenController endingScreenController;
        [SerializeField] private RankingPanelController rankingPanelController;

        [Header("UI/UX 폴리싱 — 게임 시작 전 플로우 (MainMenu → CountrySelect → BeginGame)")]
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private CountrySelectController countrySelectController;

        private PathogenDefinition _pendingPathogen;

        private void Start()
        {
            // 게임 시작 전 화면부터 노출. GameManager.isPaused 기본값은 true지만, GameManager는
            // DontDestroyOnLoad라 "재시작"(씬 리로드) 시에는 직전 판의 인스턴스가 그대로 이어져
            // isPaused=false 상태로 넘어올 수 있다 — MainMenu를 띄우는 시점에 명시적으로 다시 멈춘다.
            Debug.Log($"[FLOW][UIManager] Start() 실행 (instanceId={GetInstanceID()}, time={Time.realtimeSinceStartup:F2}) — " +
                $"mainMenuController={(mainMenuController != null ? "OK" : "NULL")}, " +
                $"countrySelectController={(countrySelectController != null ? "OK" : "NULL")}, " +
                $"endingScreenController={(endingScreenController != null ? "OK" : "NULL")}");

            if (mainMenuController != null)
            {
                GameManager.Instance?.SetPaused(true);
                countrySelectController?.Hide();
                mainMenuController.Show();
            }
            else
            {
                Debug.LogWarning("[FLOW][UIManager] mainMenuController가 NULL이라 MainMenu를 표시하지 않습니다 — " +
                    "UIManager 인스펙터의 Main Menu Controller 슬롯 연결을 확인하세요.");
            }
        }

        private void OnEnable()
        {
            if (hudController != null)
            {
                hudController.OnUpgradeTabClicked -= HandleUpgradeTabClicked;
                hudController.OnUpgradeTabClicked += HandleUpgradeTabClicked;
                hudController.OnRankingClicked -= HandleRankingClicked;
                hudController.OnRankingClicked += HandleRankingClicked;
            }

            if (endingScreenController != null)
            {
                endingScreenController.OnRestartRequested -= HandleRestartRequested;
                endingScreenController.OnRestartRequested += HandleRestartRequested;
                endingScreenController.OnReviveRequested -= HandleReviveRequested;
                endingScreenController.OnReviveRequested += HandleReviveRequested;
            }

            if (mainMenuController != null)
            {
                mainMenuController.OnPathogenConfirmed -= HandlePathogenConfirmed;
                mainMenuController.OnPathogenConfirmed += HandlePathogenConfirmed;
            }

            if (countrySelectController != null)
            {
                countrySelectController.OnCountryConfirmed -= HandleCountryConfirmed;
                countrySelectController.OnCountryConfirmed += HandleCountryConfirmed;
                countrySelectController.OnBackRequested -= HandleCountrySelectBackRequested;
                countrySelectController.OnBackRequested += HandleCountrySelectBackRequested;
            }
        }

        private void OnDisable()
        {
            if (hudController != null)
            {
                hudController.OnUpgradeTabClicked -= HandleUpgradeTabClicked;
                hudController.OnRankingClicked -= HandleRankingClicked;
            }

            if (endingScreenController != null)
            {
                endingScreenController.OnRestartRequested -= HandleRestartRequested;
                endingScreenController.OnReviveRequested -= HandleReviveRequested;
            }

            if (mainMenuController != null)
                mainMenuController.OnPathogenConfirmed -= HandlePathogenConfirmed;

            if (countrySelectController != null)
            {
                countrySelectController.OnCountryConfirmed -= HandleCountryConfirmed;
                countrySelectController.OnBackRequested -= HandleCountrySelectBackRequested;
            }
        }

        private void HandlePathogenConfirmed(PathogenDefinition pathogen)
        {
            Debug.Log($"[FLOW][UIManager] HandlePathogenConfirmed — pathogen={pathogen?.DisplayName}, " +
                $"countrySelectController={(countrySelectController != null ? "OK" : "NULL")}");
            _pendingPathogen = pathogen;
            mainMenuController.Hide();
            countrySelectController?.Show();
        }

        private void HandleCountrySelectBackRequested()
        {
            Debug.Log("[FLOW][UIManager] HandleCountrySelectBackRequested — CountrySelect 숨기고 MainMenu로 복귀");
            countrySelectController.Hide();
            mainMenuController?.Show();
        }

        private void HandleCountryConfirmed(string countryId)
        {
            Debug.Log($"[FLOW][UIManager] HandleCountryConfirmed — countryId={countryId}, " +
                $"GameDataBootstrapper.Instance={(GameDataBootstrapper.Instance != null ? "OK" : "NULL")}");
            countrySelectController.Hide();
            GameDataBootstrapper.Instance?.BeginGame(_pendingPathogen, countryId);
        }

        /// <summary>
        /// 전파/증상/능력 탭 3개가 전부 같은 창을 열어서 카테고리 구분이 안 된다는 피드백으로,
        /// 카테고리별로 완전히 별개인 UpgradeTreeView 3개 중 해당하는 것만 열고 나머지 둘은 닫는다.
        /// </summary>
        private void HandleUpgradeTabClicked(Contagion.Data.UpgradeCategory category)
        {
            countryPopupController?.Hide();

            transmissionTreeView?.Hide();
            symptomTreeView?.Hide();
            abilityTreeView?.Hide();

            switch (category)
            {
                case Contagion.Data.UpgradeCategory.Transmission:
                    transmissionTreeView?.Show();
                    break;
                case Contagion.Data.UpgradeCategory.Symptom:
                    symptomTreeView?.Show();
                    break;
                case Contagion.Data.UpgradeCategory.Ability:
                    abilityTreeView?.Show();
                    break;
            }
        }

        private void HandleRankingClicked()
        {
            transmissionTreeView?.Hide();
            symptomTreeView?.Hide();
            abilityTreeView?.Hide();
            rankingPanelController?.Show();
        }

        private void HandleRestartRequested()
        {
            // 씬을 리로드하면 GameDataBootstrapper.Start()가 다시 실행되면서
            // ResetPersistentManagersForNewGame()으로 DontDestroyOnLoad 매니저들의 이전 판 상태를
            // 전부 초기화하고, 새 UIManager.Start()가 GameManager를 다시 일시정지시키며 MainMenu를
            // 띄운다 — 여기서 따로 일시정지를 풀 필요가 없다(오히려 풀면 리로드 직전 한 프레임 동안
            // 이미 끝난 판의 틱이 다시 돌 여지만 생긴다).
            var activeScene = SceneManager.GetActiveScene();
            Debug.Log($"[FLOW][UIManager] HandleRestartRequested (instanceId={GetInstanceID()}, " +
                $"time={Time.realtimeSinceStartup:F2}) — 씬 리로드 시작 (scene='{activeScene.name}', " +
                $"buildIndex={activeScene.buildIndex})");
            SceneManager.LoadScene(activeScene.buildIndex);
            Debug.Log($"[FLOW][UIManager] HandleRestartRequested — SceneManager.LoadScene() 호출 반환됨 " +
                $"(time={Time.realtimeSinceStartup:F2})");
        }

        /// <summary>
        /// 게임 오버 후 부활. 설계 문서 13절 표 2행 — 보상형 광고 시청 완료 시 진행 상태를 되돌려 재개.
        /// 치료제가 이미 완성된 패배 상황이므로, cureProgress를 일부 되돌려 "재도전 기회"를 준다
        /// (설계 문서에 정확한 롤백 수치가 없어 합리적으로 정한 값 — 밸런싱 대상).
        /// </summary>
        [SerializeField, Tooltip("부활 시 cureProgress를 얼마나 되돌릴지")]
        private float reviveCureProgressRollback = 0.3f;

        private void HandleReviveRequested()
        {
            GameAds.Rewarded.Show(
                onSuccess: () =>
                {
                    var state = WorldDataManager.Instance?.State;
                    if (state != null)
                        state.cureProgress = Mathf.Max(0f, state.cureProgress - reviveCureProgressRollback);

                    WorldDataManager.Instance?.NotifyWorldStateChanged();
                    // SimulationManager._gameEnded를 안 풀면 TickLoop()이 계속 "if (_gameEnded) continue"에
                    // 걸려서, 아래 SetPaused(false)로 일시정지를 풀어도 시뮬레이션이 실제로는 재개되지 않는다.
                    SimulationManager.Instance?.ClearGameEndedFlag();
                    GameManager.Instance?.SetPaused(false);
                    endingScreenController?.Hide();
                },
                onFailed: () =>
                {
                    // 광고 실패/미준비 — 부활 불가, 엔딩 화면 유지
                });
        }
    }
}
