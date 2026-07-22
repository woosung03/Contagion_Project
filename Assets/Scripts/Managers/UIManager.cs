using System;
using System.Collections.Generic;
using Contagion.Ads;
using Contagion.Data;
using Contagion.Gameplay;
using Contagion.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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

        /// <summary>[UpgradeTree Architecture Refactor, 2026-07-22] CountryStatusPanel/RankingPanel과
        /// 동일한 아키텍처(UIDocument 1개)로 통일 — 카테고리별 별도 창 3개(전파/증상/능력)를 Show/Hide로
        /// 전환하던 예전 구조를 없앴다. 카테고리 전환은 이제 UpgradeTreeView 내부에서만 처리한다.
        /// FormerlySerializedAs로 기존 인스펙터 배선(예전 "Transmission Tree View" 슬롯)을 그대로
        /// 이어받는다 — 남은 SymptomTreeUI/AbilityTreeUI GameObject 삭제는 Unity 에디터에서 수동으로
        /// 해야 한다(Docs/Archive/unity-editor-task.md 참고).</summary>
        [Header("업그레이드 트리 — CountryStatusPanel과 동일하게 UIDocument 1개")]
        [SerializeField, FormerlySerializedAs("transmissionTreeView")]
        private UpgradeTreeView upgradeTreeView;

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
                // [HUD Visibility, 2026-07-23] MainMenu(병원체 선택)/CountrySelect가 떠 있는 동안 HUD
                // (resource-strip/action-strip)는 완전히 숨긴다 — Country 선택 완료(HandleCountryConfirmed)
                // 시점에 다시 켠다.
                hudController?.SetHudVisible(false);
                countrySelectController?.Hide();
                mainMenuController.Show();
            }
            else
            {
                Debug.LogWarning("[FLOW][UIManager] mainMenuController가 NULL이라 MainMenu를 표시하지 않습니다 — " +
                    "UIManager 인스펙터의 Main Menu Controller 슬롯 연결을 확인하세요.");
            }

            SubscribeGameEnded();
        }

        private void OnEnable()
        {
            BuildScreenMap();
            SubscribeGameEnded();

            if (hudController != null)
            {
                hudController.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;
                hudController.OnUpgradeButtonClicked += HandleUpgradeButtonClicked;
                hudController.OnCountryStatusClicked -= HandleCountryStatusClicked;
                hudController.OnCountryStatusClicked += HandleCountryStatusClicked;
                hudController.OnRankingClicked -= HandleRankingClicked;
                hudController.OnRankingClicked += HandleRankingClicked;
            }

            if (upgradeTreeView != null)
            {
                upgradeTreeView.OnResearchItemSelected -= HandleResearchItemSelected;
                upgradeTreeView.OnResearchItemSelected += HandleResearchItemSelected;
                upgradeTreeView.OnCloseRequested -= HandleScreenCloseRequested;
                upgradeTreeView.OnCloseRequested += HandleScreenCloseRequested;
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
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnGameEnded -= HandleGameEndedForHud;

            if (hudController != null)
            {
                hudController.OnUpgradeButtonClicked -= HandleUpgradeButtonClicked;
                hudController.OnCountryStatusClicked -= HandleCountryStatusClicked;
                hudController.OnRankingClicked -= HandleRankingClicked;
            }

            if (upgradeTreeView != null)
            {
                upgradeTreeView.OnResearchItemSelected -= HandleResearchItemSelected;
                upgradeTreeView.OnCloseRequested -= HandleScreenCloseRequested;
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
            // [HUD Visibility, 2026-07-23] 발원 국가 선택 완료 = 실제 게임 시작 시점 — 이때부터 HUD를 켠다.
            hudController?.SetHudVisible(true);
            GameDataBootstrapper.Instance?.BeginGame(_pendingPathogen, countryId);
        }

        /// <summary>
        /// [HUD Visibility, 2026-07-23] EndingScreenController와 동일하게 SimulationManager.OnGameEnded를
        /// 구독해 HUD를 숨긴다 — Instance가 OnEnable 시점엔 아직 없을 수 있어(DontDestroyOnLoad 매니저
        /// 초기화 순서) Start()에서도 한 번 더 호출한다(EndingScreenController.Subscribe()와 동일 패턴).
        /// </summary>
        private void SubscribeGameEnded()
        {
            if (SimulationManager.Instance == null) return;
            SimulationManager.Instance.OnGameEnded -= HandleGameEndedForHud;
            SimulationManager.Instance.OnGameEnded += HandleGameEndedForHud;
        }

        private void HandleGameEndedForHud(bool isVictory) => hudController?.SetHudVisible(false);

        /// <summary>
        /// AppScreen → 실제 화면(들)을 여닫는 IScreenController 매핑. OnEnable에서 1회 구성한다.
        /// [UpgradeTree Architecture Refactor, 2026-07-22] Research는 이제 GlobalStatus/Leaderboard와
        /// 동일한 패턴 — 컨트롤러 1개의 Show()/Hide()만 호출한다(예전엔 카테고리별 창 3개를 페이징했음).
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
                        upgradeTreeView?.Show();
                    },
                    hide: () =>
                    {
                        upgradeTreeView?.Hide();
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

            // [HUD Full Screen Panel 개편] Research/GlobalStatus/Leaderboard로 전환되면 메인 플레이
            // 화면(WORLD NEWS/지도/막대 그래프/선 그래프)을 통째로 숨기고, Gameplay로 돌아오면 다시
            // 보인다 — resource-strip/action-strip은 HudController.SetGameplayContentVisible()이
            // 건드리는 gameplay-content 바깥(형제)이라 항상 유지된다.
            hudController?.SetGameplayContentVisible(next == AppScreen.Gameplay);

            nextScreen.Show();
            _currentScreen = next;
        }

        /// <summary>
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

        /// <summary>연구 항목 행 클릭 — UpgradeTreeView.OnResearchItemSelected 구독(V2 계획 커밋 7).
        /// node.id로 표시명/브랜치/설명을 조회해 ResearchPopupController.Show()를 호출한다.
        /// 실제 구매(TryUnlock)는 ResearchPopupController의 확인 버튼이 담당한다 — 이 메서드는
        /// nodeId를 팝업에 전달만 할 뿐 구매 로직/상태 변경에는 관여하지 않는다.</summary>
        private void HandleResearchItemSelected(UpgradeNode node)
        {
            if (node == null || researchPopupController == null) return;

            string displayName = UpgradeTreeView.GetDisplayName(node.id);
            string branch = UpgradeTreeView.GetBranch(node.id);
            string description = UpgradeTreeView.GetDescription(node.id);

            researchPopupController.Show(displayName, branch, description, node.id);
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
                    // [HUD Visibility, 2026-07-23] 부활 = Gameplay 재개 — HandleGameEndedForHud로 숨겼던 HUD를 되돌린다.
                    hudController?.SetHudVisible(true);
                },
                onFailed: () =>
                {
                    // 광고 실패/미준비 — 부활 불가, 엔딩 화면 유지
                });
        }
    }
}
