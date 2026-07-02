using System;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 메인 게임 화면 HUD. 설계 문서 7.1절 (하단 스탯 바 + 전파/증상/능력 탭 버튼).
    /// 뉴스 피드 스크롤은 별도 NewsFeedController가 같은 UIDocument를 공유해 담당한다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        private Label _infectedLabel;
        private Label _deadLabel;
        private Label _cureLabel;
        private Label _dnaLabel;
        private Label _dayLabel;
        private Label _phaseLabel;
        private Button _tabTransmission;
        private Button _tabSymptom;
        private Button _tabAbility;
        private Button _rankingButton;

        public event Action<UpgradeCategory> OnUpgradeTabClicked;
        public event Action OnRankingClicked;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _infectedLabel = root.Q<Label>("infected-label");
            _deadLabel = root.Q<Label>("dead-label");
            _cureLabel = root.Q<Label>("cure-label");
            _dnaLabel = root.Q<Label>("dna-label");
            _dayLabel = root.Q<Label>("day-label");
            _phaseLabel = root.Q<Label>("phase-label");

            _tabTransmission = root.Q<Button>("tab-transmission");
            _tabSymptom = root.Q<Button>("tab-symptom");
            _tabAbility = root.Q<Button>("tab-ability");
            _rankingButton = root.Q<Button>("ranking-button");

            _tabTransmission.RegisterCallback<ClickEvent>(_ => OnUpgradeTabClicked?.Invoke(UpgradeCategory.Transmission));
            _tabSymptom.RegisterCallback<ClickEvent>(_ => OnUpgradeTabClicked?.Invoke(UpgradeCategory.Symptom));
            _tabAbility.RegisterCallback<ClickEvent>(_ => OnUpgradeTabClicked?.Invoke(UpgradeCategory.Ability));
            _rankingButton.RegisterCallback<ClickEvent>(_ => OnRankingClicked?.Invoke());

            Subscribe();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnWorldStateChanged -= HandleWorldStateChanged;
                WorldDataManager.Instance.OnWorldStateChanged += HandleWorldStateChanged;
                HandleWorldStateChanged(WorldDataManager.Instance.State);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
                GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                HandlePhaseChanged(GameManager.Instance.CurrentPhase);
            }
        }

        private void OnDisable()
        {
            if (WorldDataManager.Instance != null)
                WorldDataManager.Instance.OnWorldStateChanged -= HandleWorldStateChanged;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandleWorldStateChanged(WorldState state)
        {
            if (state == null) return;
            _infectedLabel.text = $"감염자: {state.infectedCount:N0}";
            _deadLabel.text = $"사망자: {state.deadCount:N0}";
            _cureLabel.text = $"치료제: {state.cureProgress * 100f:F1}%";
            _dnaLabel.text = $"DNA: {state.dnaPoints:N0}";
            _dayLabel.text = $"Day {state.currentDay}";
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            _phaseLabel.text = phase switch
            {
                GamePhase.Incubation => "잠복기",
                GamePhase.Spread => "확산기",
                GamePhase.Endgame => "결정기",
                _ => ""
            };
        }
    }
}
