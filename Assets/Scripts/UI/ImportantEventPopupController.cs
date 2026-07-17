using System;
using System.Collections.Generic;
using Contagion.Data;
using Contagion.Gameplay;
using Contagion.Managers;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// Important Event Popup System — 국면 전환급 이벤트(첫 국가 붕괴/첫 완전 무정부/치료제 개발 시작)를
    /// 뉴스 피드와 별개로 화면 중앙 모달로 알린다. <see cref="TacticalModalController"/>를 상속하되
    /// modal-rows/modal-close는 쓰지 않는다 — 제목/설명/확인 버튼 1개뿐인 최소 구조(ResearchPopup
    /// 콘텐츠 구조 미복제).
    ///
    /// Show()를 직접 호출하지 않는다 — 모든 이벤트는 Enqueue()를 거쳐 큐에 쌓이고, 현재 표시 중인
    /// 팝업이 없을 때만 ShowNext()가 실제로 연다. 같은 틱에 여러 "최초" 이벤트가 동시에 발생해도
    /// (예: 첫 국가 붕괴 + 첫 완전 무정부가 한 국가에서 동시에) 확인 버튼을 누를 때마다 다음 이벤트가
    /// 이어서 뜬다.
    /// </summary>
    public class ImportantEventPopupController : TacticalModalController
    {
        private struct ImportantEventData
        {
            public string title;
            public string description;
        }

        private Label _description;
        private Button _confirmButton;

        private readonly Queue<ImportantEventData> _eventQueue = new Queue<ImportantEventData>();
        private bool _isShowing;

        protected override void OnEnable()
        {
            base.OnEnable();

            // 씬 리로드(재시작) 시 이 GameObject는 파괴 후 재생성되므로 필드 자체가 이미 빈 상태로
            // 시작하지만, "항상 빈 상태로 시작한다"는 의도를 명시적으로 남긴다(방어적, 비용 없음).
            _eventQueue.Clear();
            _isShowing = false;

            var root = GetComponent<UIDocument>().rootVisualElement;
            _description = root.Q<Label>("popup-description");
            _confirmButton = root.Q<Button>("important-event-confirm-button");

            _confirmButton?.RegisterCallback<ClickEvent>(_ => Hide());

            Subscribe();
        }

        private void Start() => Subscribe();

        private void Subscribe()
        {
            if (HumanResistanceManager.Instance != null)
            {
                HumanResistanceManager.Instance.OnFirstCountryCollapse -= HandleFirstCollapse;
                HumanResistanceManager.Instance.OnFirstCountryCollapse += HandleFirstCollapse;
                HumanResistanceManager.Instance.OnFirstFullAnarchy -= HandleFirstFullAnarchy;
                HumanResistanceManager.Instance.OnFirstFullAnarchy += HandleFirstFullAnarchy;
            }

            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.OnCureResearchStarted -= HandleCureResearchStarted;
                SimulationManager.Instance.OnCureResearchStarted += HandleCureResearchStarted;
            }
        }

        private void OnDisable()
        {
            if (HumanResistanceManager.Instance != null)
            {
                HumanResistanceManager.Instance.OnFirstCountryCollapse -= HandleFirstCollapse;
                HumanResistanceManager.Instance.OnFirstFullAnarchy -= HandleFirstFullAnarchy;
            }

            if (SimulationManager.Instance != null)
                SimulationManager.Instance.OnCureResearchStarted -= HandleCureResearchStarted;
        }

        private void HandleFirstCollapse(Country country) =>
            Enqueue("국가 붕괴", $"{country.name}이(가) 최초로 붕괴 상태에 진입했습니다.");

        private void HandleFirstFullAnarchy(Country country) =>
            Enqueue("완전 무정부", $"{country.name}이(가) 최초로 완전 무정부 상태에 진입했습니다.");

        private void HandleCureResearchStarted() =>
            Enqueue("치료제 개발 시작", "인류가 치료제 개발을 시작했습니다.");

        /// <summary>Show()를 직접 부르지 않는다 — 큐에 넣고, 현재 표시 중인 팝업이 없을 때만 이어서 연다.</summary>
        private void Enqueue(string title, string description)
        {
            _eventQueue.Enqueue(new ImportantEventData { title = title, description = description });
            if (!_isShowing) ShowNext();
        }

        private void ShowNext()
        {
            if (_eventQueue.Count == 0)
            {
                _isShowing = false;
                return;
            }

            var evt = _eventQueue.Dequeue();
            _isShowing = true;

            WorldMapInputLock.Lock(WorldMapLockReason.ImportantEvent);
            base.Show(evt.title);
            if (_description != null) _description.text = evt.description;
        }

        /// <summary>확인 버튼 클릭 시 호출 — 잠금 해제 후 큐에 남은 다음 이벤트를 곧바로 이어서 연다.</summary>
        public override void Hide()
        {
            WorldMapInputLock.Unlock(WorldMapLockReason.ImportantEvent);
            base.Hide();
            ShowNext();
        }
    }
}
