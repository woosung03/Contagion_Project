using System;
using System.Collections.Generic;
using Contagion.Ads;
using Contagion.Data;
using Contagion.Gameplay;
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

        [Header("업그레이드 트리 — 카테고리별로 별개인 창 3개를 배열 순서(전파→증상→능력)로 페이징")]
        [SerializeField] private UpgradeTreeView transmissionTreeView;
        [SerializeField] private UpgradeTreeView symptomTreeView;
        [SerializeField] private UpgradeTreeView abilityTreeView;

        /// <summary>인덱스 0=전파, 1=증상, 2=능력 — HUD "업그레이드" 버튼 하나로 열고 좌우 화살표로 순환.</summary>
        private UpgradeTreeView[] _upgradePages;
        private int _currentUpgradePageIndex;

        /// <summary>
        /// AppScreen 상태 머신 — HUD 버튼 3개(업그레이드/국가현황/랭킹)가 여는 화면을
        /// "다른 패널 Hide 수동 호출" 대신 Dictionary 매핑 + TransitionTo() 한 곳으로 통합한다.
        /// 화면이 늘어나도 _screens에 항목 하나만 추가하면 된다(HandleXxxClicked 핸들러들을
        /// 서로 건드릴 필요 없음).
        /// </summary>
        private Dictionary<AppScreen, IScreenController> _screens;
        private AppScreen _currentScreen = AppScreen.Gameplay;

        /// <summary>
        /// IScreenController 어댑터 — 기존 컨트롤러(CountryStatusPanelController 등)의 클래스
        /// 선언은 건드리지 않고, Show/Hide 델리게이트만 받아 인터페이스 계약을 만족시킨다.
        /// </summary>
        private sealed class ActionScreenController : IScreenController
        {
            private readonly Action _show;
            private readonly Action _hide;

            public ActionScreenController(Action show, Action hide)
            {
                _show = show;
                _hide = hide;
            }

            public void Show() => _show?.Invoke();
            public void Hide() => _hide?.Invoke();
        }

        [SerializeField] private CountryPopupController countryPopupController;
        [SerializeField] private CountryStatusPanelController countryStatusPanelController;
        [SerializeField] private EndingScreenController endingScreenController;
        [SerializeField] private RankingPanelController rankingPanelController;

        [Header("Research Popup — 연구 항목 행 클릭 시 표시 (V2 계획 커밋 7)")]
        [SerializeField] private ResearchPopupController researchPopupController;

        [Header("UI/UX 폴리싱 — 게임 시작 전 플로우 (MainMenu → CountrySelect → BeginGame)")]
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private CountrySelectController countrySelectController;

        private PathogenDefinition _pendingPathogen;

        private void Start()
        {
            // 게임 시작 전 화면부터 노출. GameManager.isPaused 기본값은 true지만, GameManager는
            // DontDestroyOnLoad라 "재시작"(씬 리로드) 시에는 직전 판의 인스턴스가 그대로 이어져
            // isPaused=false 상태로 넘어올 수 있다 — MainMenu를 띄우는 시점에 명시적으로 다시 멈춘다.
            if (mainMenuController != null)
            {
                // [WorldMap Input Lock System] 정적 클래스(WorldMapInputLock)는 씬 리로드(재시작)에도
                // 상태가 이어진다 — 직전 판이 EndingScreen 등 잠금 사유를 못 풀고 넘어왔을 가능성을 대비해
                // 새 판을 시작하는 이 시점에 한 번 전부 해제하고 시작한다(WorldMapInputLock.ClearAll() 참고).
                WorldMapInputLock.ClearAll();
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
            _upgradePages = new[] { transmissionTreeView, symptomTreeView, abilityTreeView };
            BuildScreenMap();

            if (hudController != null)
            {
                hudController.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;
                hudController.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;
                hudController.OnCountryStatusClicked -= HandleCountryStatusClicked;
                hudController.OnCountryStatusClicked += HandleCountryStatusClicked;
                hudController.OnRankingClicked -= HandleRankingClicked;
                hudController.OnRankingClicked += HandleRankingClicked;
            }

            foreach (var page in _upgradePages)
            {
                if (page == null) continue;
                page.OnCategoryRequested -= HandleCategoryRequested;
                page.OnCategoryRequested += HandleCategoryRequested;
                page.OnResearchItemSelected -= HandleResearchItemSelected;
                page.OnResearchItemSelected += HandleResearchItemSelected;
                page.OnCloseRequested -= HandleScreenCloseRequested;
                page.OnCloseRequested += HandleScreenCloseRequested;
            }

            if (countryStatusPanelController != null)
            {
                countryStatusPanelController.OnCloseRequested -= HandleScreenCloseRequested;
                countryStatusPanelController.OnCloseRequested += HandleScreenCloseRequested;
                countryStatusPanelController.OnCountryRowSelected -= HandleCountryRowSelected;
                countryStatusPanelController.OnCountryRowSelected += HandleCountryRowSelected;
            }

            if (countryPopupController != null)
            {
                countryPopupController.OnDetailRequested -= HandleCountryDetailRequested;
                countryPopupController.OnDetailRequested += HandleCountryDetailRequested;
            }

            if (rankingPanelController != null)
            {
                rankingPanelController.OnCloseRequested -= HandleScreenCloseRequested;
                rankingPanelController.OnCloseRequested += HandleScreenCloseRequested;
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
                hudController.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;
                hudController.OnCountryStatusClicked -= HandleCountryStatusClicked;
                hudController.OnRankingClicked -= HandleRankingClicked;
            }

            if (_upgradePages != null)
            {
                foreach (var page in _upgradePages)
                {
                    if (page == null) continue;
                    page.OnCategoryRequested -= HandleCategoryRequested;
                    page.OnResearchItemSelected -= HandleResearchItemSelected;
                    page.OnCloseRequested -= HandleScreenCloseRequested;
                }
            }

            if (countryStatusPanelController != null)
            {
                countryStatusPanelController.OnCloseRequested -= HandleScreenCloseRequested;
                countryStatusPanelController.OnCountryRowSelected -= HandleCountryRowSelected;
            }

            if (countryPopupController != null)
                countryPopupController.OnDetailRequested -= HandleCountryDetailRequested;

            if (rankingPanelController != null)
                rankingPanelController.OnCloseRequested -= HandleScreenCloseRequested;

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
            countrySelectController.Hide();
            GameDataBootstrapper.Instance?.BeginGame(_pendingPathogen, countryId);
        }

        /// <summary>
        /// AppScreen → 실제 화면(들)을 여닫는 IScreenController 매핑. OnEnable에서 _upgradePages가
        /// 만들어진 뒤 1회 구성한다. Research는 컨트롤러 3개(전파/증상/능력)를 하나의 화면으로
        /// 묶어야 해서, 진입 시 ShowUpgradePage(마지막 페이지)를 호출하고 퇴장 시 3개를 모두 닫는다.
        /// Gameplay는 "패널이 아무 것도 없는 기본 지도 화면"이라 Show/Hide 둘 다 아무 일도 하지 않는다.
        /// </summary>
        private void BuildScreenMap()
        {
            // [WorldMap Input Lock System] Gameplay는 "패널이 없는 기본 지도 화면"이라 잠금 사유가 없다
            // (Show/Hide 둘 다 null). 나머지 세 화면은 Show에서 Lock, Hide에서 Unlock을 같이 호출해 —
            // TransitionTo()가 항상 "이전 화면 Hide → 다음 화면 Show" 순서로 부르므로, AppScreen끼리
            // 전환될 때도 사유가 한 프레임도 빠짐없이 정확한 값으로 유지된다.
            _screens = new Dictionary<AppScreen, IScreenController>
            {
                { AppScreen.Gameplay, new ActionScreenController(show: null, hide: null) },
                { AppScreen.Research, new ActionScreenController(
                    show: () =>
                    {
                        WorldMapInputLock.Lock(WorldMapLockReason.Research);
                        ShowUpgradePage(_currentUpgradePageIndex);
                    },
                    hide: () =>
                    {
                        HideAllUpgradePages();
                        WorldMapInputLock.Unlock(WorldMapLockReason.Research);
                    }) },
                { AppScreen.GlobalStatus, new ActionScreenController(
                    show: () =>
                    {
                        WorldMapInputLock.Lock(WorldMapLockReason.GlobalStatus);
                        countryStatusPanelController?.Show();
                    },
                    hide: () =>
                    {
                        countryStatusPanelController?.Hide();
                        WorldMapInputLock.Unlock(WorldMapLockReason.GlobalStatus);
                    }) },
                { AppScreen.Leaderboard, new ActionScreenController(
                    show: () =>
                    {
                        WorldMapInputLock.Lock(WorldMapLockReason.Leaderboard);
                        rankingPanelController?.Show();
                    },
                    hide: () =>
                    {
                        rankingPanelController?.Hide();
                        WorldMapInputLock.Unlock(WorldMapLockReason.Leaderboard);
                    }) },
            };
        }

        /// <summary>
        /// 현재 화면을 닫고 다음 화면을 연다 — 기존에 각 HandleXxxClicked가 "다른 패널들 Hide"를
        /// 일일이 나열하던 부분을 대체한다. 화면이 늘어나면 BuildScreenMap()에 항목 하나만
        /// 추가하면 되고, 이 메서드는 그대로 둔다.
        /// </summary>
        private void TransitionTo(AppScreen next)
        {
            if (_screens == null || !_screens.TryGetValue(next, out var nextScreen))
            {
                Debug.LogWarning($"[UIManager] AppScreen.{next}에 매핑된 IScreenController가 없습니다.");
                return;
            }

            if (_currentScreen != next && _screens.TryGetValue(_currentScreen, out var currentScreen))
                currentScreen.Hide();

            nextScreen.Show();
            _currentScreen = next;
        }

        private void HideAllUpgradePages()
        {
            if (_upgradePages == null) return;
            foreach (var page in _upgradePages)
                page?.Hide();
        }

        /// <summary>
        /// 전파/증상/능력 탭 3개가 각각 다른 창을 열던 걸 버튼 하나로 통합 — 마지막으로 보고 있던
        /// 페이지(_currentUpgradePageIndex)를 그대로 열어준다(항상 전파부터 시작하지 않고, 능력 탭을
        /// 보다가 닫았으면 다음에 열 때도 능력 탭부터 보이는 게 자연스러움).
        ///
        /// countryPopupController는 HUD 버튼으로 여는 AppScreen 상태 머신 밖의 모달(지도 클릭으로
        /// 열림)이라 _screens 매핑에 넣지 않고, 기존과 동일하게 여기서만 명시적으로 닫는다.
        /// </summary>
        private void HandleUpgradeButtonClicked()
        {
            countryPopupController?.Hide();
            TransitionTo(AppScreen.Research);
        }

        /// <summary>
        /// HUD "국가현황" 버튼 — Step 28-2에서 국가 클릭 팝업을 대체해 추가. 18개국 상태를
        /// 스크롤 리스트 하나로 보여준다(CountryStatusPanelController 참고). AppScreen 전환이
        /// 업그레이드/랭킹과의 배타적 표시를 대신 보장한다.
        /// </summary>
        private void HandleCountryStatusClicked()
        {
            TransitionTo(AppScreen.GlobalStatus);
        }

        /// <summary>GlobalStatus 화면(48개국 목록) 행 클릭 — UpgradeTreeView.OnResearchItemSelected
        /// → ResearchPopupController.Show()와 동일한 패턴으로 CountryPopupController.ShowCountry()를
        /// 재사용한다(2026-07-14 결정 — 새 Country Database 화면을 만들지 않고, CountryPopup을
        /// 지도 클릭과 CountryStatusPanel 리스트 양쪽에서 재사용). CountryPopupController는
        /// AppScreen/WorldMapInputLock 밖에 있는 기존 동작을 그대로 유지하므로 이 핸들러는 별도
        /// 잠금 처리 없이 표시만 위임한다.</summary>
        private void HandleCountryRowSelected(Country country)
        {
            countryPopupController?.ShowCountry(country);
        }

        /// <summary>CountryPopup(Bottom Sheet)의 "상세 보기" 버튼 — AppScreen 상태 머신
        /// (TransitionTo)을 그대로 통해 CountryStatusPanel을 열고, 그 국가로 포커스한다.
        /// WorldMapInputLock 등 화면 상태 관리는 TransitionTo/BuildScreenMap이 전담하므로
        /// countryStatusPanelController.Show()를 직접 호출하지 않는다.</summary>
        private void HandleCountryDetailRequested(Country country)
        {
            TransitionTo(AppScreen.GlobalStatus);
            countryStatusPanelController.FocusCountry(country);
        }

        /// <summary>탭 클릭 — Research Database UI Shell(Commit 1)로 좌우 화살표를 대체한 상단 탭
        /// 3개 중 하나가 눌리면 호출된다. 요청된 카테고리를 담당하는 UpgradeTreeView를 찾아 그
        /// 화면만 보이고 나머지 둘은 닫는다(3-GameObject 구조는 그대로 유지 — 화면 표시 여부만
        /// 토글, ResearchDatabase_MVP_ImplementationPlan.md §6-3).</summary>
        private void HandleCategoryRequested(UpgradeCategory targetCategory)
        {
            int index = System.Array.FindIndex(_upgradePages, p => p != null && p.Category == targetCategory);
            if (index < 0)
            {
                Debug.LogWarning($"[UIManager] {targetCategory} 카테고리를 담당하는 UpgradeTreeView를 찾지 못했습니다.");
                return;
            }
            ShowUpgradePage(index);
        }

        /// <summary>연구 항목 행 클릭 — UpgradeTreeView.OnResearchItemSelected 구독(V2 계획 커밋 7).
        /// node.id로 표시명/브랜치/설명을 조회해 ResearchPopupController.Show()를 호출한다. 구매
        /// 로직(TryUnlock)이나 상태 변경은 전혀 건드리지 않는다 — 이번 커밋은 팝업 표시까지만.</summary>
        private void HandleResearchItemSelected(UpgradeNode node)
        {
            if (node == null || researchPopupController == null) return;

            string displayName = UpgradeTreeView.GetDisplayName(node.id);
            string branch = UpgradeTreeView.GetBranch(node.id);
            string description = UpgradeTreeView.GetDescription(node.id);

            researchPopupController.Show(displayName, branch, description);
        }

        /// <summary>지정한 인덱스의 UpgradeTreeView만 보이고 나머지 둘은 닫는다.</summary>
        private void ShowUpgradePage(int index)
        {
            _currentUpgradePageIndex = index;
            for (int i = 0; i < _upgradePages.Length; i++)
            {
                if (_upgradePages[i] == null) continue;
                if (i == index) _upgradePages[i].Show();
                else _upgradePages[i].Hide();
            }
        }

        private void HandleRankingClicked()
        {
            TransitionTo(AppScreen.Leaderboard);
        }

        /// <summary>
        /// Research/GlobalStatus/Leaderboard 세 화면의 X(닫기) 버튼 공통 핸들러 — 버그 수정
        /// ("X 버튼으로 닫으면 WorldMapInputLock이 영구 유지되는 문제"). 세 컨트롤러가 각자
        /// 내부에서 Hide()를 직접 부르던 것을 OnCloseRequested 이벤트 발행으로 바꾸고, 실제
        /// 화면 전환은 HUD 버튼 경로와 동일하게 TransitionTo()로만 수행한다 — 그래야
        /// BuildScreenMap()의 hide 델리게이트(WorldMapInputLock.Unlock 포함)가 항상 호출되고
        /// _currentScreen도 Gameplay로 정확히 되돌아간다. 어느 화면에서 닫기를 눌렀는지는
        /// TransitionTo() 내부에서 _currentScreen 기준으로 알아서 판단하므로 이 핸들러는
        /// 발신자를 구분할 필요가 없다.
        /// </summary>
        private void HandleScreenCloseRequested()
        {
            TransitionTo(AppScreen.Gameplay);
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
