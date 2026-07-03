using System;
using Contagion.Data;
using UnityEngine;

namespace Contagion.Managers
{
    /// <summary>
    /// 게임 상태 / 페이즈 관리. 설계 문서 10절, 12절 Core Managers.
    /// Step 1-6 범위에서는 페이즈 판정과 일시정지 제어만 담당한다.
    /// (EventManager/UIManager/SaveManager는 Step 7 이후 범위)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private Difficulty difficulty = Difficulty.Normal;
        [SerializeField] private GamePhase currentPhase = GamePhase.Incubation;
        [SerializeField, Tooltip("MainMenu/CountrySelect에서 병원체·발원국 선택을 마치고 GameDataBootstrapper.BeginGame()이 " +
            "호출되기 전까지는 시뮬레이션이 돌아가면 안 되므로 기본값을 true로 시작한다.")]
        private bool isPaused = true;

        public Difficulty CurrentDifficulty => difficulty;
        public GamePhase CurrentPhase => currentPhase;
        public bool IsPaused => isPaused;

        public event Action<GamePhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetPaused(bool paused) => isPaused = paused;

        public void SetDifficulty(Difficulty newDifficulty) => difficulty = newDifficulty;

        /// <summary>
        /// 새 게임 시작(재시작 포함) 시 GameDataBootstrapper가 호출. 이 매니저는 DontDestroyOnLoad라
        /// currentPhase가 Endgame으로 끝난 상태로 살아남는다 — 리셋 안 하면 재시작한 새 게임이
        /// 처음부터 Endgame 페이즈로 시작해버린다. 일시정지는 MainMenu 표시 로직(UIManager.Start)이
        /// 이미 별도로 처리하므로 여기서는 페이즈만 되돌린다.
        /// </summary>
        public void ResetForNewGame()
        {
            currentPhase = GamePhase.Incubation;
        }

        /// <summary>
        /// SimulationManager의 매 틱 이후 호출된다. 설계 문서 10절의 서술적 기준을
        /// 수치화한 전이 규칙 (문서에 명시된 정확한 임계값은 없어 합리적으로 정의):
        /// Incubation -> Spread : plagueVisibility가 0.2 이상 (질병 보도 시작)
        /// Spread -> Endgame    : cureProgress 0.5 이상 또는 감염 비율이 전 국가 평균 60% 이상
        /// </summary>
        public void EvaluatePhase(WorldState state, float averageInfectionRatio)
        {
            GamePhase next = currentPhase;

            if (currentPhase == GamePhase.Incubation && state.plagueVisibility >= 0.2f)
            {
                next = GamePhase.Spread;
            }
            else if (currentPhase == GamePhase.Spread &&
                     (state.cureProgress >= 0.5f || averageInfectionRatio >= 0.6f))
            {
                next = GamePhase.Endgame;
            }

            if (next != currentPhase)
            {
                currentPhase = next;
                OnPhaseChanged?.Invoke(currentPhase);
            }
        }

        /// <summary>난이도별 방역/치료제 속도 배율. 설계 문서 9절.</summary>
        public float GetDifficultyResearchMultiplier() => difficulty switch
        {
            Difficulty.Casual => 0.7f,
            Difficulty.Normal => 1.0f,
            Difficulty.Brutal => 1.4f,
            Difficulty.MegaBrutal => 1.8f,
            _ => 1.0f
        };

        /// <summary>
        /// 난이도별 전염성(확산 속도) 배율. 나무위키 Plague Inc./시스템 문서 기준
        /// (Docs/PlagueIncReference.md 3절) — 원본은 치료 속도뿐 아니라 확산 속도도 난이도별로 다르다.
        /// 쉬움=퍼지기 쉬움(+보정), 어려움부터는 퍼지기 어려움(-보정)이라 신중한 플레이가 요구된다.
        /// SimulationManager.RunTick()의 newInfected 계산에 globalSpreadFactor와 곱해서 사용.
        /// </summary>
        public float GetDifficultySpreadMultiplier() => difficulty switch
        {
            Difficulty.Casual => 1.3f,
            Difficulty.Normal => 1.0f,
            Difficulty.Brutal => 0.8f,
            Difficulty.MegaBrutal => 0.6f,
            _ => 1.0f
        };
    }
}
