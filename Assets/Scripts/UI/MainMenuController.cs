using System;
using System.Collections.Generic;
using Contagion.Data;
using Contagion.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 병원체 선택 화면 (UI/UX 폴리싱 — MainMenu). 설계 문서 2절 "병원체 선택".
    /// GameDataBootstrapper.AvailablePathogens 목록을 카드 리스트로 그리고, 선택 후 "다음" 누르면
    /// CountrySelectController로 이어진다 (조정은 UIManager 담당 — 다른 컨트롤러들과 동일한 패턴).
    /// 게임이 시작되기 전 화면이라 GameManager.isPaused는 기본 true 상태를 유지한다.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private VisualElement _root;
        private ScrollView _pathogenList;
        private Label _detailTitle;
        private Label _detailDesc;
        private Button _nextButton;

        private PathogenDefinition _selected;

        /// <summary>플레이어가 병원체를 확정(다음 버튼)했을 때 발행 — UIManager가 CountrySelect로 넘긴다.</summary>
        public event Action<PathogenDefinition> OnPathogenConfirmed;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _root = root.Q<VisualElement>("mainmenu-root");
            _pathogenList = root.Q<ScrollView>("pathogen-list");
            _detailTitle = root.Q<Label>("detail-title");
            _detailDesc = root.Q<Label>("detail-desc");
            _nextButton = root.Q<Button>("next-button");

            _nextButton.RegisterCallback<ClickEvent>(_ => HandleNextClicked());
        }

        public void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            RebuildList();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        private void RebuildList()
        {
            _pathogenList.Clear();
            _selected = null;
            _nextButton.SetEnabled(false);
            _detailTitle.text = "";
            _detailDesc.text = "";

            IReadOnlyList<PathogenDefinition> pathogens = GameDataBootstrapper.Instance?.AvailablePathogens;
            if (pathogens == null || pathogens.Count == 0)
            {
                Debug.LogWarning("[MainMenuController] 사용 가능한 병원체가 없습니다 — GameDataBootstrapper의 " +
                    "Available Pathogens 배열을 확인하세요.");
                return;
            }

            foreach (var pathogen in pathogens)
            {
                if (pathogen == null) continue;
                _pathogenList.Add(CreatePathogenCard(pathogen));
            }

            Debug.Log($"[MainMenuController] 병원체 목록 {pathogens.Count}개 렌더링 완료.");
        }

        private VisualElement CreatePathogenCard(PathogenDefinition pathogen)
        {
            var card = new VisualElement();
            card.AddToClassList("pathogen-card");

            var name = new Label(pathogen.DisplayName);
            name.AddToClassList("pathogen-card__name");
            card.Add(name);

            var stats = new Label(
                $"전염력 {StatBar(pathogen.Infectivity)}  중증도 {StatBar(pathogen.Severity)}  " +
                $"치사율 {StatBar(pathogen.Lethality)}  내성 {StatBar(pathogen.DrugResistance)}");
            stats.AddToClassList("pathogen-card__stats");
            card.Add(stats);

            card.RegisterCallback<ClickEvent>(_ => SelectPathogen(pathogen, card));
            return card;
        }

        /// <summary>0~1 값을 5칸짜리 텍스트 막대로 대충 시각화 — 실제 그래픽 바는 비주얼/연출 단계에서.</summary>
        private static string StatBar(float value01)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(value01 * 5f), 0, 5);
            return new string('■', filled) + new string('□', 5 - filled);
        }

        private void SelectPathogen(PathogenDefinition pathogen, VisualElement card)
        {
            _selected = pathogen;

            foreach (var child in _pathogenList.Children())
                child.RemoveFromClassList("pathogen-card--selected");
            card.AddToClassList("pathogen-card--selected");

            _detailTitle.text = pathogen.DisplayName;
            _detailDesc.text = string.IsNullOrEmpty(pathogen.FlavorText)
                ? "설명 없음"
                : pathogen.FlavorText;

            _nextButton.SetEnabled(true);
        }

        private void HandleNextClicked()
        {
            if (_selected == null) return;
            OnPathogenConfirmed?.Invoke(_selected);
        }
    }
}
