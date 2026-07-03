using System;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 메인 게임 화면 HUD. 설계 문서 7.1절 (하단 스탯 바 + 업그레이드/랭킹 탭 버튼).
    /// 뉴스 피드 스크롤은 별도 NewsFeedController가 같은 UIDocument를 공유해 담당한다.
    ///
    /// UI/UX 폴리싱 — 원래 전파/증상/능력 탭 버튼 3개가 각각 별개의 창을 열었는데, 버튼 하나
    /// ("업그레이드")로 통합하고 창 안에서 좌우 화살표로 카테고리를 넘기는 방식으로 바꿨다
    /// (UpgradeTreeView의 prev/next 버튼 + UIManager의 페이징 로직 참고). 그래서 카테고리를
    /// 지정하던 <c>OnUpgradeTabClicked(UpgradeCategory)</c> 이벤트가 인자 없는
    /// <see cref="OnUpgradeButtonClicked"/>로 바뀌었다 — 어느 카테고리를 여는지는 UIManager가
    /// "마지막으로 보던 페이지"를 기억해서 결정한다.
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
        private Label _worldStatusLabel;
        private Button _tabUpgrade;
        private Button _countryStatusButton;
        private Button _rankingButton;

        // 감염자/사망자/치료제 — 텍스트 라벨만으론 가시성이 떨어진다는 피드백으로 추가한 인라인
        // 스파크라인 그래프 (HudSparkline.cs 참고).
        private HudSparkline _infectedGraph;
        private HudSparkline _deadGraph;
        private HudSparkline _cureGraph;

        public event Action OnUpgradeButtonClicked;
        public event Action OnCountryStatusClicked;
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
            _worldStatusLabel = root.Q<Label>("world-status-label");

            _tabUpgrade = root.Q<Button>("tab-upgrade");
            _countryStatusButton = root.Q<Button>("tab-country-status");
            _rankingButton = root.Q<Button>("ranking-button");

            _infectedGraph = new HudSparkline(root.Q<VisualElement>("infected-graph"), new Color(1f, 0.67f, 0.35f));
            _deadGraph = new HudSparkline(root.Q<VisualElement>("dead-graph"), new Color(0.86f, 0.35f, 0.35f));
            _cureGraph = new HudSparkline(root.Q<VisualElement>("cure-graph"), new Color(0.47f, 0.86f, 0.55f));

            _tabUpgrade.RegisterCallback<ClickEvent>(_ => OnUpgradeButtonClicked?.Invoke());
            _countryStatusButton.RegisterCallback<ClickEvent>(_ => OnCountryStatusClicked?.Invoke());
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

            _infectedGraph?.AddSample(state.infectedCount);
            _deadGraph?.AddSample(state.deadCount);
            _cureGraph?.AddSample(state.cureProgress * 100f);

            // 사망률 기준 위험도 텍스트 세분화 (Docs/PlagueIncReference.md 2절) — 사망자가 아예 없는
            // Stable 상태에서는 굳이 표시하지 않고(평시엔 빈 문자열), 위협 단계에 들어서야 노출한다.
            var mortalityStage = state.GetMortalityStage();
            _worldStatusLabel.text = mortalityStage == WorldMortalityStage.Stable ? "" : MortalityStageLabel(mortalityStage);
        }

        private static string MortalityStageLabel(WorldMortalityStage stage) => stage switch
        {
            WorldMortalityStage.Stable => "",
            WorldMortalityStage.EmergingThreat => "위협 시작",
            WorldMortalityStage.WorldThreatened => "세계를 위협",
            WorldMortalityStage.ExtinctionImminent => "인류 멸종 임박",
            _ => stage.ToString()
        };

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
