# Legacy Master Analysis

> **역할**: "왜 Master Design의 규칙이 이런 값으로 정해졌는가"에 대한 1차 사료. `MASTER_DESIGN.md`의
> 근거 자료일 뿐 Source of Truth가 아니다 — 규칙 자체는 항상 `MASTER_DESIGN.md`를 따른다.

## Master Commit 선정

git 히스토리 전체를 조사해 **`cfcc0e4`(fix(gameplay): 게임 시작 플로우 및 씬 손상 복구,
2026-07-08)** 를 Master Commit으로 선정했다.

**선정 근거**:
- Step 24(`9d6b85b`)에서 1440×3120/포트레이트 타겟팅이 시작됐다 — 그 이전(1200×800 가로 레퍼런스
  시절)은 이 프로젝트의 "1440×3120 기준 디자인"이 아니므로 제외.
- 바로 다음 커밋 `90baf6a`의 메시지 자체에 "Apply SF tactical display styling"이 명시돼 있고,
  실제로 그 시점 `Hud.uss`엔 이미 `corner-cut`/`event-dock` 등 Tactical 실험이 들어 있었다 —
  Tactical Design System 도입이 시작된 지점이라 제외.
- `cfcc0e4`의 `Hud.uss`(113줄)에는 tactical/corner-cut/event-dock 문자열이 전혀 없다 — Tactical
  실험 시작 직전 마지막 순수 상태.
- 이 시점 `MainMenu`/`CountrySelect`/`CountryPopup`/`CountryStatusPanel`/`EndingScreen`/`Hud`/
  `RankingPanel`/`UpgradeTree` 8개 화면이 전부 완성된 형태로 존재 — 초기 프로토타입이 아니라
  실제 플레이 가능한 버전.

## 실측 데이터 (Master Commit 기준, 참조 캔버스 480×1040)

### Typography(Theme.uss 토큰)
`xs=13 sm=14 body=16 md=18 lg=20 xl=24 xxl=26 display=32 hero=40` — 현재와 동일(이 스케일은
프로젝트 시작 이래 한 번도 변경되지 않음).

### Spacing(Theme.uss 토큰)
`xs=4 sm=8 md=12 lg=16 xl=20 xxl=24` — 현재와 동일.

### HUD 구성(Hud.uss/Hud.uxml)
- news-scroll height = 150px(화면 높이의 14.4%)
- bottom-bar(정보+버튼 통합, Header가 별도로 없었음) = 약 142px(13.7%) — padding 14px/`--space-lg`,
  stats-row(그래프+라벨) + tabs-row(버튼) 포함
- stat-graph-item__graph = 104×34px
- tab-button height = `--touch-target-min`(48px)
- Content Area(map-spacer, 잔여) = 748px(71.9%)

### Card/Row(MainMenu.uss)
- pathogen-card: border 2px, radius `--radius-md`(6px), padding 10px/14px
- country-row: border 2px, radius 6px, padding 8px/12px, 국기 아이콘 36×26px
- detail-panel: padding `--space-lg`, min-height 70px

### Panel(CountryStatusPanel.uss/RankingPanel.uss)
- CountryStatusPanel: `top:8.5%; bottom:12.5%; left:5%; right:5%` → 높이 79.0%, 폭 90%
- RankingPanel: `left:9%; right:9%; top:22%`(bottom 지정 없음, 콘텐츠 기반 높이)
- 이 시점 이미 "참조 해상도 480×1040 기준 고정 px 인셋 대신 %로 표현한다"는 명시적 주석 존재 —
  Master Design 자체가 처음부터 비율 우선 설계였다는 직접 증거.

### Popup(CountryPopup.uss)
- popup-root: width 320px, `left:50%`+`margin-left:-160px`(중앙 정렬), radius `--radius-lg`(8px),
  padding `--space-lg`
- popup-close height 36px(주력 버튼 48px보다 작은 별도 티어)

### EndingScreen.uss
- ending-title = `--font-size-hero`(40px)
- ending-detail = `--font-size-lg`(20px), ending-score = `--font-size-xxl`(26px)
- ending-button = 180×56px(주력 버튼 48px보다 큰 별도 티어)

### UpgradeTree.uss
- upgrade-header__close = 80px, ad-bonus = 160px, arrow = 52px
- tree-node: 고정 크기 없음, `padding:4px 6px`로 콘텐츠 기반 자동 크기(현재의 C# 고정
  110×50px 캔버스 노드와는 완전히 다른 구조)

## 존재하지 않았던 컴포넌트

- **Progress Bar**(population-bar) — Legacy에 없음, 완전 신규.
- **Status Bar(독립 Header)** — day/DNA/phase가 Bottom Bar 안에 통합돼 있었고, 화면 최상단
  독립 Status Bar 개념 자체가 없었음.
- **Hero Stat(2×2 그리드)** — CountryStatusPanel은 이 시점 단순 스크롤 리스트였음.

## Legacy vs Current 실측 비교(요약)

| 항목 | Legacy | Current | 변화 |
|---|---|---|---|
| HUD 상단 정보 텍스트(day/dna/phase) | 18px | 14px | **−22.2%**(가장 뚜렷한 실축소) |
| News 높이 | 150px | 158px | +5.3% |
| Graph 높이 | 34px | 52px | +52.9% |
| Button 높이(tab-button) | 48px | 48px | 0% |
| Card border(pathogen-card) | 2px | 1px | −50% |
| CountryStatusPanel 높이 비율 | 79.0% | 84.5% | +7.0%p |
