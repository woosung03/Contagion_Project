using Contagion.Ads;
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
        [SerializeField] private UpgradeTreeView upgradeTreeView;
        [SerializeField] private CountryPopupController countryPopupController;
        [SerializeField] private EndingScreenController endingScreenController;
        [SerializeField] private RankingPanelController rankingPanelController;

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
        }

        private void HandleUpgradeTabClicked(Contagion.Data.UpgradeCategory category)
        {
            countryPopupController?.Hide();
            upgradeTreeView?.Show(category);
        }

        private void HandleRankingClicked()
        {
            upgradeTreeView?.Hide();
            rankingPanelController?.Show();
        }

        private void HandleRestartRequested()
        {
            GameManager.Instance?.SetPaused(false);
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
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
