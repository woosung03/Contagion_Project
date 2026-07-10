using System;
using System.Linq;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 승리/패배 엔딩 화면. 설계 문서 1절(승패 조건), 11절(바이오하자드 점수), 14절(난이도 배율).
    ///
    /// 점수 공식은 설계 문서 11절과 14절이 "클리어 속도 보너스"를 중복 정의(11절 바이오하자드 점수
    /// 구성에 이미 날짜 기반 항목이 있는데 14절 최종 Score가 또 곱함)하고 있어, 여기서는
    /// 바이오하자드 점수(날짜+업그레이드 효율+병원체 배율) × 난이도 배율 한 번만 적용하도록 정리했다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class EndingScreenController : MonoBehaviour
    {
        private VisualElement _endingRoot;
        private Label _resultTitle;
        private VisualElement _statsRows;
        private Label _scoreLabel;
        private Button _reviveButton;
        private Button _restartButton;

        public event Action OnReviveRequested;
        public event Action OnRestartRequested;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _endingRoot = root.Q<VisualElement>("ending-root");
            _resultTitle = root.Q<Label>("result-title");
            _statsRows = root.Q<VisualElement>("stats-rows");
            _scoreLabel = root.Q<Label>("score-label");
            _reviveButton = root.Q<Button>("revive-button");
            _restartButton = root.Q<Button>("restart-button");

            _reviveButton.RegisterCallback<ClickEvent>(_ =>
            {
                Debug.Log("[FLOW][EndingScreenController] 부활 버튼 클릭됨.");
                OnReviveRequested?.Invoke();
            });
            _restartButton.RegisterCallback<ClickEvent>(_ =>
            {
                Debug.Log("[FLOW][EndingScreenController] 재시작 버튼 클릭됨.");
                OnRestartRequested?.Invoke();
            });

            Subscribe();
            Hide();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (SimulationManager.Instance == null) return;
            SimulationManager.Instance.OnGameEnded -= HandleGameEnded;
            SimulationManager.Instance.OnGameEnded += HandleGameEnded;
        }

        private void OnDisable()
        {
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnGameEnded -= HandleGameEnded;
        }

        private void HandleGameEnded(bool isVictory)
        {
            Debug.Log($"[FLOW][EndingScreenController] HandleGameEnded — isVictory={isVictory} =====");
            GameManager.Instance?.SetPaused(true);

            int day = WorldDataManager.Instance?.State.currentDay ?? 0;
            float score = ComputeFinalScore(isVictory, day);

            _resultTitle.text = isVictory ? "인류 전멸 — 승리" : "치료제 완성 — 패배";
            // 라벨("바이오하자드 점수")은 UXML의 .ending-score-caption 고정 텍스트로 분리됨 —
            // 숫자만 hero 폰트로 렌더링해 화면 폭(참조 해상도 480px) 안에 들어오게 한다.
            _scoreLabel.text = $"{score:N0}";

            // 문단(N일 경과)뿐이던 결과 요약을 data-row 4줄(감염/사망/붕괴국/경과일)로 확장 —
            // Docs/UI_Design.md 14절. GLOBAL INFECTED/DEATHS는 WorldDataManager.State 스냅샷 그대로,
            // COLLAPSED NATIONS는 국가별 Country.GetCollapseStage()(이미 존재하는 함수)를
            // 종료 시점에 순회 집계하는 표시 전용 계산이라 신규 게임 로직이 아니다.
            long infected = WorldDataManager.Instance?.State.infectedCount ?? 0;
            long dead = WorldDataManager.Instance?.State.deadCount ?? 0;
            int collapsedNations = WorldDataManager.Instance?.Countries
                .Count(c => c.GetCollapseStage() >= CountryCollapseStage.FullCollapse) ?? 0;

            _statsRows?.Clear();
            _statsRows?.Add(MakeStatRow("전 세계 감염자", $"{infected:N0}", "data-value--infected"));
            _statsRows?.Add(MakeStatRow("전 세계 사망자", $"{dead:N0}", "data-value--dead"));
            _statsRows?.Add(MakeStatRow("붕괴 국가 수", $"{collapsedNations}", "data-value--danger"));
            _statsRows?.Add(MakeStatRow("경과일", $"{day}일"));

            // 부활(광고)은 패배(치료제 완성으로 병원체가 진 상황)에서만 의미가 있다 — 설계 문서 13절.
            _reviveButton.style.display = isVictory ? DisplayStyle.None : DisplayStyle.Flex;

            RankingManager.Instance?.SubmitRun((long)Mathf.Round(score));

            Show();
        }

        public void Show()
        {
            if (_endingRoot != null) _endingRoot.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_endingRoot != null) _endingRoot.style.display = DisplayStyle.None;
        }

        private float ComputeFinalScore(bool isVictory, int day)
        {
            float bioHazardScore = ComputeBiohazardScore(isVictory, day);
            float difficultyMultiplier = GetDifficultyMultiplier(GameManager.Instance?.CurrentDifficulty ?? Difficulty.Normal);
            return bioHazardScore * difficultyMultiplier;
        }

        private float ComputeBiohazardScore(bool isVictory, int day)
        {
            // 클리어 날짜: 승리는 빠를수록 고점, 패배(치료제 완성당함)는 오래 버틴 만큼만 부분 점수.
            float clearDateScore = isVictory ? Mathf.Max(10f, 300f - day * 2f) : day * 1.5f;

            var tree = UpgradeManager.Instance?.Tree;
            int totalNodes = tree?.Count ?? 0;
            int usedUpgrades = tree?.Count(n => n.isUnlocked) ?? 0;
            // 사용한 업그레이드 수가 적을수록 효율 보너스 (0.5~1.5배 클램프)
            float efficiencyBonus = totalNodes > 0 ? Mathf.Clamp(1.5f - (float)usedUpgrades / totalNodes, 0.5f, 1.5f) : 1f;

            var pathogenType = WorldDataManager.Instance?.CurrentPathogen?.type ?? PathogenType.Bacteria;
            float pathogenMultiplier = GetPathogenBaseMultiplier(pathogenType);

            return clearDateScore * efficiencyBonus * pathogenMultiplier;
        }

        private static float GetPathogenBaseMultiplier(PathogenType type) => type switch
        {
            PathogenType.Parasite or PathogenType.Nano => 1.2f,
            PathogenType.BioWeapon or PathogenType.Necroa or PathogenType.Neurax => 1.5f,
            _ => 1.0f
        };

        private static float GetDifficultyMultiplier(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Casual => 1.0f,
            Difficulty.Normal => 1.5f,
            Difficulty.Brutal => 2.0f,
            Difficulty.MegaBrutal => 3.0f,
            _ => 1.0f
        };

        /// <summary>data-row 한 줄(라벨+값)을 만든다 — Tactical.uss data-row/data-label/data-value 계약.</summary>
        private static VisualElement MakeStatRow(string label, string value, string valueClass = null)
        {
            var row = new VisualElement();
            row.AddToClassList("data-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("data-label");
            row.Add(labelEl);

            var valueEl = new Label(value);
            valueEl.AddToClassList("data-value");
            if (!string.IsNullOrEmpty(valueClass)) valueEl.AddToClassList(valueClass);
            row.Add(valueEl);

            return row;
        }
    }
}
