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
            }
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
                EventManager.Instance.OnNewsEvent -= HandleNewsEvent;
            if (HumanResistanceManager.Instance != null)
                HumanResistanceManager.Instance.OnResistanceStageChanged -= HandleResistanceStageChanged;
        }

        private void HandleNewsEvent(NewsEvent evt)
        {
            string extraClass = evt.category == NewsEventCategory.Positive ? "news-entry--positive" : "news-entry--negative";
            AddEntry($"[Day {evt.day}] {evt.text}", extraClass);
        }

        private void HandleResistanceStageChanged(ResistanceStage stage)
        {
            AddEntry($"[상황 변화] 인류 저항 단계 — {StageLabel(stage)}", null);
        }

        private static string StageLabel(ResistanceStage stage) => stage switch
        {
            ResistanceStage.NoAwareness => "인식 없음 (정상 생활)",
            ResistanceStage.DiseaseReported => "질병 보도 (마스크·손씻기 캠페인)",
            ResistanceStage.PublicHealthEmergency => "공중보건 비상사태 (격리 시작, 연구 가속)",
            ResistanceStage.NationalEmergency => "국가 비상사태 (국경 봉쇄, 항공/항구 폐쇄)",
            ResistanceStage.WorldCollapse => "세계 붕괴 (무정부 상태)",
            _ => stage.ToString()
        };

        private void AddEntry(string text, string extraClass)
        {
            if (_newsScroll == null) return;

            var label = new Label(text);
            label.AddToClassList("news-entry");
            if (extraClass != null) label.AddToClassList(extraClass);

            _newsScroll.Add(label);

            var content = _newsScroll.contentContainer;
            while (content.childCount > MaxEntries)
                content.RemoveAt(0);

            _newsScroll.ScrollTo(label);
        }
    }
}
