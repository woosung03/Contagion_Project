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

        // [HUD Full Screen Panel 개편] resource-strip/action-strip 사이의 "메인 플레이 화면"
        // (WORLD NEWS/지도/막대 그래프/선 그래프) 전체를 담는 래퍼 — UIManager.TransitionTo()가
        // 관리 화면(Research/GlobalStatus/Leaderboard) 전환 시 SetGameplayContentVisible()로
        // 이것 하나만 토글한다(Hud.uxml gameplay-content 참고).
        private VisualElement _gameplayContent;

        // 감염자/사망자/치료제 — 텍스트 라벨만으론 가시성이 떨어진다는 피드백으로 추가한 인라인
        // 스파크라인 그래프 (HudSparkline.cs 참고).
        private HudSparkline _infectedGraph;
        private HudSparkline _deadGraph;
        private HudSparkline _cureGraph;

        // [통합 인구 상태 막대] 정상인/감염자/사망자를 하나의 스택형 막대로 표현 — Painter2D 없이
        // VisualElement 3개의 style.flexGrow 비율만으로 그린다(population-bar, Hud.uxml/Hud.uss 참고).
        private VisualElement _populationHealthySegment;
        private VisualElement _populationInfectedSegment;
        private VisualElement _populationDeadSegment;
        private Label _populationSummaryLabel;

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

            _gameplayContent = root.Q<VisualElement>("gameplay-content");

            _infectedGraph = new HudSparkline(root.Q<VisualElement>("infected-graph"), new Color(1f, 0.67f, 0.35f));
            _deadGraph = new HudSparkline(root.Q<VisualElement>("dead-graph"), new Color(0.86f, 0.35f, 0.35f));
            _cureGraph = new HudSparkline(root.Q<VisualElement>("cure-graph"), new Color(0.47f, 0.86f, 0.55f));

            _populationHealthySegment = root.Q<VisualElement>("population-bar-healthy");
            _populationInfectedSegment = root.Q<VisualElement>("population-bar-infected");
            _populationDeadSegment = root.Q<VisualElement>("population-bar-dead");
            _populationSummaryLabel = root.Q<Label>("population-bar-summary");

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
            _dayLabel.text = $"{state.currentDay}일차";

            _infectedGraph?.AddSample(state.infectedCount);
            _deadGraph?.AddSample(state.deadCount);
            _cureGraph?.AddSample(state.cureProgress * 100f);

            UpdatePopulationBar(state);

            // 사망률 기준 위험도 텍스트 세분화 (Docs/PlagueIncReference.md 2절) — 사망자가 아예 없는
            // Stable 상태에서는 굳이 표시하지 않고(평시엔 빈 문자열), 위협 단계에 들어서야 노출한다.
            var mortalityStage = state.GetMortalityStage();
            _worldStatusLabel.text = mortalityStage == WorldMortalityStage.Stable ? "" : MortalityStageLabel(mortalityStage);

            // [Hud 리디자인] resource-strip에서 world-status-label은 평시(Stable)엔 텍스트가 없어
            // 고정폭을 없앤 CSS 덕에 폭이 거의 0으로 줄어든다. 텍스트가 실제로 있을 때만 강조
            // 배경/테두리(stat-label--mortality--active, Hud.uss 참고)를 입혀 위협 단계를 눈에
            // 띄게 한다.
            bool isThreatActive = mortalityStage != WorldMortalityStage.Stable;
            _worldStatusLabel.EnableInClassList("stat-label--mortality--active", isThreatActive);
        }

        /// <summary>
        /// [통합 인구 상태 막대] 정상인/감염자/사망자 비율을 population-bar의 세그먼트 3개
        /// (healthy/infected/dead) style.flexGrow에 그대로 대입한다. Painter2D를 쓰지 않고 Yoga
        /// 플렉스박스의 flex-grow 분배만으로 폭을 정하므로, 값 자체는 정규화(합이 1이 되게)할
        /// 필요 없이 population/infectedCount/deadCount 원본 비율만 맞으면 된다.
        ///
        /// WorldState.totalPopulation은 WorldDataManager.RecalculateWorldTotals()가 매 틱
        /// 48개국 population 합계로 이미 갱신해두므로 여기서 따로 합산하지 않는다 —
        /// 감염자/사망자 수도 기존 infectedCount/deadCount를 그대로 재사용(신규 데이터 없음).
        /// </summary>
        private void UpdatePopulationBar(WorldState state)
        {
            if (_populationHealthySegment == null) return;

            long total = state.totalPopulation;
            if (total <= 0)
            {
                // 국가 데이터가 아직 로드되기 전(게임 시작 직후 한두 프레임) — USS 기본값과
                // 동일하게 healthy 100% 상태를 유지한다.
                _populationHealthySegment.style.flexGrow = 1f;
                _populationInfectedSegment.style.flexGrow = 0f;
                _populationDeadSegment.style.flexGrow = 0f;
                _populationSummaryLabel.text = "감염 0.0% · 사망 0.00%";
                return;
            }

            long dead = state.deadCount;
            long infected = state.infectedCount;
            long healthy = Math.Max(0L, total - infected - dead);

            _populationHealthySegment.style.flexGrow = (float)healthy;
            _populationInfectedSegment.style.flexGrow = (float)infected;
            _populationDeadSegment.style.flexGrow = (float)dead;

            float infectedPct = (float)infected / total * 100f;
            float deadPct = (float)dead / total * 100f;
            _populationSummaryLabel.text = $"감염 {infectedPct:F1}% · 사망 {deadPct:F2}%";
        }

        private static string MortalityStageLabel(WorldMortalityStage stage) => stage switch
        {
            WorldMortalityStage.Stable => "",
            WorldMortalityStage.EmergingThreat => "위협 시작",
            WorldMortalityStage.WorldThreatened => "세계를 위협",
            WorldMortalityStage.ExtinctionImminent => "인류 멸종 임박",
            _ => stage.ToString()
        };

        /// <summary>
        /// [HUD Full Screen Panel 개편] 메인 플레이 화면(WORLD NEWS/지도/막대 그래프/선 그래프)
        /// 전체를 한 번에 표시/숨김. resource-strip/action-strip은 이 래퍼 바깥(형제)이라 영향
        /// 받지 않는다 — UIManager.TransitionTo()가 AppScreen.Gameplay 여부로 호출한다.
        /// </summary>
        public void SetGameplayContentVisible(bool visible)
        {
            if (_gameplayContent != null)
                _gameplayContent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
