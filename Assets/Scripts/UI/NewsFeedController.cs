using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 뉴스 피드 스크롤 표시. 설계 문서 7.1절(상단 뉴스 피드), 8절(이벤트 텍스트).
    /// EventManager.OnNewsEvent + HumanResistanceManager.OnResistanceStageChanged를 구독해
    /// Hud.uxml의 news-scroll에 항목을 쌓는다. HudController와 같은 GameObject(UIDocument)에 붙인다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class NewsFeedController : MonoBehaviour
    {
        private const int MaxEntries = 30;

        [SerializeField] private string newsScrollElementName = "news-scroll";

        private ScrollView _newsScroll;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _newsScroll = root.Q<ScrollView>(newsScrollElementName);
            Subscribe();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnNewsEvent -= HandleNewsEvent;
                EventManager.Instance.OnNewsEvent += HandleNewsEvent;
            }

            if (HumanResistanceManager.Instance != null)
            {
                HumanResistanceManager.Instance.OnResistanceStageChanged -= HandleResistanceStageChanged;
                HumanResistanceManager.Instance.OnResistanceStageChanged += HandleResistanceStageChanged;
                HumanResistanceManager.Instance.OnMortalityStageChanged -= HandleMortalityStageChanged;
                HumanResistanceManager.Instance.OnMortalityStageChanged += HandleMortalityStageChanged;
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnCureResearchStarted -= HandleCureResearchStarted;
                SimulationManager.Instance.OnCureResearchStarted += HandleCureResearchStarted;
            }
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
                EventManager.Instance.OnNewsEvent -= HandleNewsEvent;
            if (HumanResistanceManager.Instance != null)
            {
                HumanResistanceManager.Instance.OnResistanceStageChanged -= HandleResistanceStageChanged;
                HumanResistanceManager.Instance.OnMortalityStageChanged -= HandleMortalityStageChanged;
            }
            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnCureResearchStarted -= HandleCureResearchStarted;
        }

        /// <summary>
        /// SimulationManager가 감염자/사망자 수 기반 확률 판정으로 질병 발견을 확정지은 순간 딱 1회 호출됨.
        /// 그 전까진 cureProgress가 0에 고정돼 있다가 이 시점부터 실제로 오르기 시작한다.
        /// </summary>
        private void HandleCureResearchStarted()
        {
            AddEntry("[상황 변화] 정체불명의 질병이 세계 보건당국에 의해 공식 확인됨 — 치료제 연구가 시작됩니다.", CurrentDay(), "news-row__dot--negative", "news-row__title--negative");
        }

        private void HandleNewsEvent(NewsEvent evt)
        {
            string dotClass = evt.category switch
            {
                NewsEventCategory.Positive => "news-row__dot--positive",
                NewsEventCategory.Negative => "news-row__dot--negative",
                NewsEventCategory.Flavor => "news-row__dot--flavor",
                _ => null
            };
            string titleClass = evt.category switch
            {
                NewsEventCategory.Positive => "news-row__title--positive",
                NewsEventCategory.Negative => "news-row__title--negative",
                NewsEventCategory.Flavor => "news-row__title--flavor",
                _ => null
            };
            AddEntry(evt.text, evt.day, dotClass, titleClass);
        }

        private void HandleResistanceStageChanged(ResistanceStage stage)
        {
            AddEntry($"[상황 변화] 인류 저항 단계 — {StageLabel(stage)}", CurrentDay(), null, null);
        }

        /// <summary>
        /// 사망률 기준 위험도 단계 변경 알림 — 나무위키 "세계를 위협"/"인류 멸종 임박" 문구 세분화
        /// (Docs/PlagueIncReference.md 2절). 인류 저항 단계(전염 확산 축)와 별개로 뉴스에 추가 노출.
        /// </summary>
        private void HandleMortalityStageChanged(WorldMortalityStage stage)
        {
            // Stable은 "아직 사망자가 없다"는 기본값이라 별도 뉴스로 띄울 필요가 없음(진입 시점에만 스킵).
            if (stage == WorldMortalityStage.Stable) return;
            AddEntry($"[상황 변화] 세계 위험도 — {MortalityStageLabel(stage)}", CurrentDay(), "news-row__dot--negative", "news-row__title--negative");
        }

        private static int CurrentDay() => WorldDataManager.Instance?.State.currentDay ?? 0;

        private static string StageLabel(ResistanceStage stage) => stage switch
        {
            ResistanceStage.NoAwareness => "인식 없음 (정상 생활)",
            ResistanceStage.DiseaseReported => "질병 보도 (마스크·손씻기 캠페인)",
            ResistanceStage.PublicHealthEmergency => "공중보건 비상사태 (격리 시작, 연구 가속)",
            ResistanceStage.NationalEmergency => "국가 비상사태 (국경 봉쇄, 공항/항구 폐쇄)",
            ResistanceStage.WorldCollapse => "세계 붕괴 (무정부 상태)",
            _ => stage.ToString()
        };

        private static string MortalityStageLabel(WorldMortalityStage stage) => stage switch
        {
            WorldMortalityStage.Stable => "안정적",
            WorldMortalityStage.EmergingThreat => "위협 시작",
            WorldMortalityStage.WorldThreatened => "세계를 위협",
            WorldMortalityStage.ExtinctionImminent => "인류 멸종 임박",
            _ => stage.ToString()
        };

        /// <summary>
        /// [WORLD NEWS 심각도 표시] 한 줄 = 심각도 점(news-row__dot) + 제목(news-row__title) +
        /// DAY(news-row__day). 최신 뉴스가 항상 맨 위에 오도록 index 0에 삽입하고, 초과분은
        /// 맨 아래(가장 오래된 항목)부터 제거한다. 자동 스크롤은 하지 않는다 — 플레이어가
        /// 필요할 때만 직접 스크롤해 과거 기록을 본다(News Feed는 "기록", Important Event
        /// Popup이 "즉시 알림" 역할을 담당).
        /// </summary>
        private void AddEntry(string title, int day, string dotClass, string titleClass)
        {
            if (_newsScroll == null) return;

            var row = new VisualElement();
            row.AddToClassList("news-row");

            var dot = new VisualElement();
            dot.AddToClassList("news-row__dot");
            dot.AddToClassList(dotClass ?? "news-row__dot--flavor");
            row.Add(dot);

            var body = new VisualElement();
            body.AddToClassList("news-row__body");

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("news-row__title");
            if (titleClass != null) titleLabel.AddToClassList(titleClass);
            body.Add(titleLabel);

            var dayLabel = new Label($"DAY {day}");
            dayLabel.AddToClassList("news-row__day");
            body.Add(dayLabel);

            row.Add(body);

            var content = _newsScroll.contentContainer;
            content.Insert(0, row);

            while (content.childCount > MaxEntries)
                content.RemoveAt(content.childCount - 1);
        }
    }
}
