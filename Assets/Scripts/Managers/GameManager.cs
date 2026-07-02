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
        [SerializeField] private bool isPaused;

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
    }
}
