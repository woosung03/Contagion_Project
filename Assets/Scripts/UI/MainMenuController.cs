using System;
using System.Collections.Generic;
using Contagion.Data;
using Contagion.Gameplay;
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
            // [WorldMap Input Lock System] MainMenu가 떠 있는 동안은 WorldMap 클릭/드래그를 차단한다.
            WorldMapInputLock.Lock(WorldMapLockReason.MainMenu);
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            RebuildList();
        }

        public void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
            WorldMapInputLock.Unlock(WorldMapLockReason.MainMenu);
        }

        private void RebuildList()
        {
            _pathogenList.Clear();
            _selected = null;
            _nextButton.SetEnabled(false);
            _detailTitle.text = "선택 대기";
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

        }

        private VisualElement CreatePathogenCard(PathogenDefinition pathogen)
        {
            var card = new VisualElement();
            card.AddToClassList("pathogen-card");
            card.AddToClassList("tactical-panel"); // Docs/UI_Design.md 12절 — 카드 6개뿐이라 코너컷 4개 전부 적용

            card.Add(MakeCornerCut("tl"));
            card.Add(MakeCornerCut("tr"));
            card.Add(MakeCornerCut("bl"));
            card.Add(MakeCornerCut("br"));

            var name = new Label(pathogen.DisplayName);
            name.AddToClassList("pathogen-card__name");
            card.Add(name);

            // 스탯 4종 — Contagion UI Language 리디자인: 유니코드 블록 문자 대신 이 앱의 기존
            // CSS 비율 막대 기법(population-bar/branch-row__progress와 동일 기법)을 재사용한다.
            var statsRows = new VisualElement();
            statsRows.AddToClassList("pathogen-card__stats-rows");
            statsRows.Add(MakeStatRow("전염력", pathogen.Infectivity));
            statsRows.Add(MakeStatRow("중증도", pathogen.Severity));
            statsRows.Add(MakeStatRow("치사율", pathogen.Lethality));
            statsRows.Add(MakeStatRow("내성", pathogen.DrugResistance));
            card.Add(statsRows);

            card.RegisterCallback<ClickEvent>(_ => SelectPathogen(pathogen, card));
            return card;
        }

        /// <summary>corner-cut 4방향 중 하나를 만든다(picking-mode Ignore로 클릭 통과, 장식 전용).</summary>
        private static VisualElement MakeCornerCut(string direction)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.AddToClassList("corner-cut");
            el.AddToClassList($"corner-cut--{direction}");
            return el;
        }

        /// <summary>스탯 한 줄(라벨+비율 막대)을 만든다 — population-bar__track/__segment,
        /// branch-row__progress/__progress-fill과 동일한 flex-basis:0 + flex-grow 비율 기법.</summary>
        private static VisualElement MakeStatRow(string label, float value01)
        {
            var row = new VisualElement();
            row.AddToClassList("pathogen-card__stat-row");

            var labelEl = new Label(label);
            labelEl.AddToClassList("pathogen-card__stat-label");
            row.Add(labelEl);

            var track = new VisualElement();
            track.AddToClassList("pathogen-card__stat-track");

            float filled = Mathf.Clamp01(value01);

            var fill = new VisualElement();
            fill.AddToClassList("pathogen-card__stat-fill");
            fill.style.flexGrow = filled;
            track.Add(fill);

            var remainder = new VisualElement();
            remainder.style.flexBasis = 0;
            remainder.style.flexGrow = 1f - filled;
            remainder.style.flexShrink = 0;
            track.Add(remainder);

            row.Add(track);
            return row;
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
