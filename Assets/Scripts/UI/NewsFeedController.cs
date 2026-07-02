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
        }

        private void HandleNewsEvent(NewsEvent evt)
        {
            string extraClass = evt.category switch
            {
                NewsEventCategory.Positive => "news-entry--positive",
                NewsEventCategory.Negative => "news-entry--negative",
                NewsEventCategory.Flavor => "news-entry--flavor",
                _ => null
            };
            AddEntry($"[Day {evt.day}] {evt.text}", extraClass);
        }

        private void HandleResistanceStageChanged(ResistanceStage stage)
        {
            AddEntry($"[상황 변화] 인류 저항 단계 — {StageLabel(stage)}", null);
        }

        /// <summary>
        /// 사망률 기준 위험도 단계 변경 알림 — 나무위키 "세계를 위협"/"인류 멸종 임박" 문구 세분화
        /// (Docs/PlagueIncReference.md 2절). 인류 저항 단계(전염 확산 축)와 별개로 뉴스에 추가 노출.
        /// </summary>
        private void HandleMortalityStageChanged(WorldMortalityStage stage)
        {
            // Stable은 "아직 사망자가 없다"는 기본값이라 별도 뉴스로 띄울 필요가 없음(진입 시점에만 스킵).
            if (stage == WorldMortalityStage.Stable) return;
            AddEntry($"[상황 변화] 세계 위험도 — {MortalityStageLabel(stage)}", "news-entry--negative");
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

        private static string MortalityStageLabel(WorldMortalityStage stage) => stage switch
        {
            WorldMortalityStage.Stable => "안정적",
            WorldMortalityStage.EmergingThreat => "위협 시작",
            WorldMortalityStage.WorldThreatened => "세계를 위협",
            WorldMortalityStage.ExtinctionImminent => "인류 멸종 임박",
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
