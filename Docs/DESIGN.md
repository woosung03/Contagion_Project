---
version: 1
name: Contagion-Project-Tactical-Design-System
description: >
  Contagion Project(전염병 주식회사 클론)의 디자인 시스템 문서. "감염병 통제센터 콘솔" —
  DEFCON/Frostpunk류 군사·전술 HUD를 참고한 다크 캔버스 + 발광 헤어라인 + 코너컷 브래킷
  시스템이다. HashiCorp DESIGN.md의 surface-lift-not-shadow 철학과 hairline-first 테두리
  규칙을 뼈대로 삼고, Binance의 dense data-row 패턴과 status color semantics, ClickHouse의
  key:value 스펙 테이블 패턴을 선택적으로 얹었다. 새로운 철학이 아니라 이미 HUD(Hud.uss)와
  UpgradeTree(UpgradeTree.uss)에서 검증되어 Tactical.uss로 승격된 토큰·컴포넌트를 그대로
  문서화한 것이다.

source_of_truth:
  - Assets/UI/Theme.uss   # 색상·간격·폰트·트랜지션 토큰(:root)
  - Assets/UI/Tactical.uss  # 화면 공용으로 승격된 tactical-panel/corner-cut/data-row
  - Assets/UI/Hud.uss     # resource-strip/stat-chip/population-bar/graph-panel 등 HUD 구현
  - Assets/UI/UpgradeTree.uss  # tree-node 4상태/detail-panel(연구 분석 콘솔) 구현

colors:
  canvas: "rgba(8, 8, 16, 0.97)"            # --color-bg-root
  panel: "rgba(15, 15, 25, 0.95)"            # --color-bg-panel
  panel-strong: "rgba(15, 15, 25, 0.97)"     # --color-bg-panel-strong
  panel-alt: "rgba(10, 10, 20, 0.92)"        # --color-bg-panel-alt
  scrim-strong: "rgba(0, 0, 0, 0.75)"        # --color-bg-scrim-strong
  scrim-soft: "rgba(0, 0, 0, 0.55)"          # --color-bg-scrim-soft
  surface: "rgba(255, 255, 255, 0.08)"       # --color-surface
  surface-soft: "rgba(255, 255, 255, 0.06)"  # --color-surface-soft
  surface-selected: "rgba(255, 210, 90, 0.12)" # --color-surface-selected
  border: "rgba(255, 255, 255, 0.15)"        # --color-border
  border-selected: "rgb(255, 210, 90)"       # --color-border-selected
  text-primary: "rgb(255, 255, 255)"
  text-secondary: "rgb(210, 210, 210)"
  text-tertiary: "rgb(180, 180, 180)"
  text-info: "rgb(180, 220, 255)"
  brand-dna: "rgb(120, 220, 140)"            # --color-brand-dna
  brand-gold: "rgb(255, 210, 90)"            # --color-brand-gold
  brand-premium: "rgb(120, 80, 200)"         # --color-brand-premium
  status-infected: "rgb(255, 170, 90)"
  status-dead: "rgb(220, 90, 90)"
  status-danger: "rgb(255, 140, 120)"
  status-info: "rgb(140, 210, 255)"
  node-transmission: "rgba(120, 190, 255, 0.4)"
  node-symptom: "rgba(255, 140, 100, 0.4)"
  node-ability: "rgba(200, 140, 255, 0.4)"
  accent-glow: "rgb(150, 255, 185)"          # --color-accent-glow (구조용 발광색)
  accent-glow-soft: "rgba(150, 255, 185, 0.35)"
  grid-line: "rgba(150, 255, 185, 0.12)"

typography:
  scale: [13, 14, 16, 18, 20, 24, 26, 32, 40]  # --font-size-xs ~ --font-size-hero
  tracking-caption: 1px
  tracking-label: 0.5px
  family: "프로젝트 기본 폰트(시스템/한글 지원 폰트) 단일 사용 — 별도 display/mono 서체 없음"

spacing:
  scale: [4, 8, 12, 16, 20, 24]   # --space-xs ~ --space-xxl
  radius: [0, 3, 6, 8]            # tactical-panel(0) / --radius-sm(3) / --radius-md(6) / --radius-lg(8)
  touch-target-min: 48px

components:
  tactical-panel: { border: 1px accent-glow-soft, radius: 0 }
  corner-cut: { size: "13x2px", color: accent-glow, variants: [tl, tr, bl, br] }
  data-row: { border-bottom: 1px grid-line, layout: "label(left) : value(right)" }
  stat-chip: { size: "6x6px", color: accent-glow }
  world-status-frame: { border: 1px accent-glow-soft, radius: radius-sm }
  population-bar: { track-border: 1px accent-glow-soft, segments: [healthy, infected, dead] }
  graph-frame: { size: "104x52px", border: 1px accent-glow-soft, baseline: accent-glow-soft }
  tree-node: { border-left: 4px category-color, state-border: locked/available/active/maxed }
---

> **문서 역할 안내**: 이 문서는 **디자인 시스템 문서**다. 화면별 배치·와이어프레임·구현
> 우선순위는 `Docs/UI_Design.md`(화면 설계 문서)에 있다. 이 문서는 오직 (1) 디자인 토큰,
> (2) 컴포넌트 규칙, (3) Do/Don't만 다루며, MainMenu·CountrySelect·EndingScreen 같은
> 화면 단위 설계는 여기 쓰지 않는다. 두 문서가 충돌하면 **토큰 값은 이 문서, 화면 배치는
> UI_Design.md**가 우선한다.

## Design Principles

1. **판독 우선(Readout-first)** — 장식보다 데이터 가독성이 항상 위에 있다. `corner-cut`은
   배경 도형에 영향을 주지 않는 순수 오버레이 장식이고(`position: absolute`, 자식 요소일
   뿐), 레이아웃의 실제 최소 단위는 언제나 `data-row`(라벨:값 판독 행)다. 새 화면·새 패널을
   설계할 때 "이 정보가 몇 개의 data-row/stat-chip으로 표현되는가"를 먼저 정하고, 장식은
   그 위에 얹는다.
2. **항상 다크 캔버스(Always-dark canvas)** — HashiCorp·ClickHouse처럼 라이트모드 파생이
   없다. `--color-bg-root`(rgba(8,8,16,0.97))가 모든 화면의 바닥이며, 어떤 화면도 밝은
   캔버스로 전환되지 않는다.
3. **Hairline-first, No shadow** — UI Toolkit(USS)은 `box-shadow`를 지원하지 않는다(코드베이스
   위키 `css-to-uss-support.md` 확인 사항). 이는 제약이 아니라 시스템의 근간이다 —
   위계는 **테두리 유무·굵기·발광색**과 **배경 alpha 단계**로만 표현한다
   (HashiCorp의 "surface lift, never shadow" 그대로 채택).
4. **색상 축 분리(Color-axis separation)** — 브랜드 악센트(DNA그린/골드/프리미엄퍼플),
   severity(감염/사망/위험/정보), 업그레이드 카테고리(전파/증상/능력), 구조용 발광색
   (accent-glow)은 서로 다른 4개의 축이다. 한 축의 색을 다른 축의 의미로 재사용하지 않는다.
5. **각짐(Angularity)** — `tactical-panel`은 `border-radius: 0`으로 고정, `corner-cut`은
   45°/-45° 대각선 틱이다. pill 모양·둥근 카드는 이 시스템에 없다(브리핑룸 톤의
   `country-row`/`pathogen-card` 같은 메뉴류 예외는 `UI_Design.md` 화면 설계 재량이며,
   HUD/UpgradeTree 계열 "전술 디스플레이"에는 적용하지 않는다).
6. **밀도와 절제(Density with restraint)** — 좁은 세로 모바일 폭(1440×3120 기준)에서
   격자선·코너컷은 항상 얇게(1~2px) 유지하고, 판독 텍스트 크기는 화면 성격에 따라
   차등한다(HUD는 압축된 `font-size-xs`, 메뉴류는 기존 크기 유지). 화면 전체를 HUD처럼
   극단적으로 압축하지 않는다.

## Visual Identity

정체성은 한 문장으로 "**감염병 통제센터 콘솔**"이다 — DEFCON의 전술 지도, Frostpunk의
생존 관리 HUD 계열 참고. 시그니처 요소 5가지:

- **코너컷(`corner-cut`)** — 패널 네 귀퉁이에 발광색 대각선 틱을 얹어 "각진 모서리"
  인상을 만드는 장치. clip-path/mask 미지원의 대체재이자 이 시스템의 가장 눈에 띄는
  지문(fingerprint)이다.
- **발광 헤어라인(`accent-glow` / `accent-glow-soft`)** — 모든 패널·구분선·판독 프레임의
  테두리 색. ClickHouse의 "단일 브랜드 전압" 개념과 같되, 콘텐츠색이 아니라 **구조 전용
  색**이라는 점이 다르다.
- **좌측 accent-bar(4px)** — 카테고리 식별을 전체 테두리 대신 얇은 좌측 바로 압축한 밀도
  절약형 장치(`tree-node`, 향후 `country-row` 등에서 재사용).
- **색상 사각 칩(`stat-chip`)** — 아이콘 폰트 글리프가 실기기에서 검증되지 않은 리스크를
  피하기 위한 대체 언어. 6×6px 색상 사각형 + 텍스트 라벨 조합.
- **영문 대문자 캡션(`tactical-caption` / `*__caption`)** — `TRANS-001`, `STATUS: ACTIVE`
  류의 판독 코드 톤. 별도 모노스페이스 서체 없이 자간(`tracking-caption`)과 굵기만으로
  "기술적" 인상을 만든다.

**참고 계보**: HashiCorp(surface ladder + hairline-only elevation) · Binance(색상으로
2분화된 dense data row) · ClickHouse(key:value 스펙 테이블 밀도) — 세 시스템의 철학을
선택적으로 흡수했을 뿐, 브랜드 악센트 자체는 늘리지 않는다. HashiCorp가 제품마다 새
악센트 색을 추가하는 것과 달리, 이 시스템은 **3개의 브랜드 색(DNA그린/골드/프리미엄퍼플)
으로 고정**하고 그 이상 늘리지 않는다.

## Color System

### 1. Canvas & Panel — 표면 계단 (배경)

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-bg-root` | rgba(8,8,16,0.97) | MainMenu/CountrySelect 등 전체화면 캔버스 |
| `--color-bg-panel-alt` | rgba(10,10,20,0.92) | UpgradeTree 같은 전체화면 "패널형" 화면 |
| `--color-bg-panel` | rgba(15,15,25,0.95) | 팝업/랭킹/Event Dock/Country Dock |
| `--color-bg-panel-strong` | rgba(15,15,25,0.97) | 국가현황처럼 더 불투명해야 하는 패널 |
| `--color-bg-scrim-strong` | rgba(0,0,0,0.75) | HUD 하단 바(population-bar/graph-panel/action-strip) |
| `--color-bg-scrim-soft` | rgba(0,0,0,0.55) | HUD 상단 바(resource-strip)/뉴스피드 |

### 2. Surface & Border — 카드/행 표면

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-surface` | rgba(255,255,255,0.08) | tree-node, detail-panel 기본 배경 |
| `--color-surface-soft` | rgba(255,255,255,0.06) | graph-frame, population-bar__track 배경 |
| `--color-surface-selected` | rgba(255,210,90,0.12) | 선택된 카드/행 배경(금색 계열) |
| `--color-border` | rgba(255,255,255,0.15) | 중립 테두리(비활성/기본) |
| `--color-border-selected` | rgb(255,210,90) | 선택 강조(화면 전체에서 이 색 하나로 통일) |
| `--color-surface-hover` / `--color-border-hover` | rgba(255,255,255,0.12) / 0.35 | 카드류 hover 트랜지션 타겟 |

### 3. Text

`--color-text-primary`(백색, 헤드라인/강조) · `--color-text-secondary`(값 텍스트 기본) ·
`--color-text-tertiary`(라벨/캡션/잠금 상태) · `--color-text-info`(청색 계열, 정보성 텍스트).

### 4. 브랜드 악센트 — 자원/강조 의미 (3색 고정)

| 토큰 | 색 | 의미 |
|---|---|---|
| `--color-brand-dna` | rgb(120,220,140) 초록 | 병원체 DNA/치료제 — 플레이어 자원 |
| `--color-brand-gold` | rgb(255,210,90) 금색 | 강조/선택/화폐성 텍스트 |
| `--color-brand-premium` | rgb(120,80,200) 보라 | 프리미엄(광고 보상) 액션 전용 |

이 3색은 화면 전체에서 의미가 고정된다. HashiCorp가 제품마다 색을 추가하는 패턴을
그대로 따르지 않는다 — 새 기능이 생겨도 이 3색 중 하나에 배정하거나, 배정할 수 없으면
severity/카테고리 축을 쓴다.

### 5. Severity — 국가 데이터 의미 (Binance 패턴 차용)

| 토큰 | 색 | 의미 |
|---|---|---|
| `--color-status-infected` | rgb(255,170,90) | 감염 |
| `--color-status-dead` | rgb(220,90,90) | 사망 |
| `--color-status-danger` | rgb(255,140,120) | 위험/경고 |
| `--color-status-info` | rgb(140,210,255) | 정보/건강 |

Binance의 `trading-up`(녹)/`trading-down`(적) 2색 상태 코딩을 국가 데이터 도메인에 맞게
4색으로 확장한 것. **이 4색은 국가 데이터 전용 의미로 고정**되어 있으며(`population-bar`,
`country-dock`, News 이벤트 dot 등), 노드 상태나 UI 크롬에는 쓰지 않는다.

### 6. 업그레이드 카테고리 — 노드 정체성 (HashiCorp per-product 패턴 차용)

| 토큰 | 색(40% 알파) | 의미 |
|---|---|---|
| `--color-node-transmission` | rgba(120,190,255,0.4) 청 | 전파 계열 노드 |
| `--color-node-symptom` | rgba(255,140,100,0.4) 주황 | 증상 계열 노드 |
| `--color-node-ability` | rgba(200,140,255,0.4) 보라 | 능력 계열 노드 |

HashiCorp가 Terraform/Vault/Waypoint마다 고유색을 쓰는 것과 같은 사고방식이나, 색은
좌측 4px `accent-bar`에만 쓰고 카드 전체 배경에는 절대 칠하지 않는다 — 노드 **상태**
색상(아래 7번)과 시각적으로 겹치지 않아야 하기 때문이다.

### 7. 노드 상태 — 4단계 (severity와 별개 축)

| 토큰(클래스) | 색 | 의미 |
|---|---|---|
| `.tree-node--locked` | border: `--color-border`, opacity 0.7 | 잠김 |
| `.tree-node--available` | border: `accent-glow-soft` | 연구 가능 |
| `.tree-node--active` | border: `accent-glow`(2px), bg: rgba(120,220,140,0.12) | 연구 중/활성 |
| `.tree-node--maxed` | border: `brand-gold`(2px), bg: rgba(255,210,90,0.10) | 완료/최대 |

카테고리색(6번)은 **좌측 바**, 상태색(7번)은 **테두리 전체+배경**으로 표현 위치를
분리해 같은 카드 안에서 두 축이 시각적으로 충돌하지 않게 한다.

### 8. 구조용 발광색 (Tactical Chrome)

`--color-accent-glow`(rgb(150,255,185)) / `--color-accent-glow-soft`(35% 알파) /
`--color-grid-line`(12% 알파) — 패널 테두리, 코너컷, 캡션, 격자선 전용. **콘텐츠 색이
아니다** — 이 색이 데이터 값에 쓰이면 severity 축과 혼동되므로 금지.

## Typography

이 시스템은 별도의 display 서체나 모노스페이스 서체를 쓰지 않는다(한글 표시가 필수라
모노스페이스 폰트를 도입하면 자간 계산이 깨지고 실기기 검증 리스크가 커진다 — ClickHouse의
JetBrains Mono 스펙 테이블 패턴은 **서체가 아니라 레이아웃 구조**만 차용했다).

| 토큰 | 크기 | 용도 |
|---|---|---|
| `--font-size-xs` | 13px | data-label/data-value, tree-node 캡션, stat-chip 라벨 |
| `--font-size-sm` | 14px | stat-label, country-dock__name |
| `--font-size-body` | 16px | graph 라벨 |
| `--font-size-md` | 18px | 일반 본문 |
| `--font-size-lg` | 20px | category-header, arrow 버튼 |
| `--font-size-xl` | 24px | upgrade-header 타이틀/DNA 표시 |
| `--font-size-xxl` | 26px | (예약) |
| `--font-size-display` | 32px | (예약) |
| `--font-size-hero` | 40px | 엔딩 등 최종 판정 강조 |

**"기술적" 톤을 만드는 실제 수단**은 서체가 아니라 다음 3가지 조합이다:
1. `letter-spacing: var(--tracking-caption)`(1px) — 영문 대문자 캡션/코드(`TRANS-001`).
2. `letter-spacing: var(--tracking-label)`(0.5px) — data-label/data-value.
3. `-unity-font-style: bold` + 작은 크기(`font-size-xs`) — 라벨과 값 모두 볼드로 통일해
   서체 대비 대신 굵기로 판독 텍스트임을 표시.

## Spacing System

| 토큰 | 값 |
|---|---|
| `--space-xs` | 4px |
| `--space-sm` | 8px |
| `--space-md` | 12px |
| `--space-lg` | 16px |
| `--space-xl` | 20px |
| `--space-xxl` | 24px |
| `--touch-target-min` | 48px (모바일 터치 최소 높이) |

**모서리 반경** — `--radius-sm`(3px)/`--radius-md`(6px)/`--radius-lg`(8px) 스케일이
존재하지만, `tactical-panel`/`corner-cut`이 붙는 모든 "전술 디스플레이" 패널은
**항상 `border-radius: 0`**으로 이 스케일을 오버라이드한다 — 각짐(Angularity) 원칙이
반경 스케일보다 우선한다. 반경 스케일은 `population-bar__track`, `graph-frame`,
`world-status-frame`처럼 코너컷이 없는 "판독창" 계열에만 적용된다.

## Stroke System

**"두께는 굵기가 아니라 의미다."** 같은 두께가 서로 다른 의미를 표현하면 안 된다 — 테두리를
그릴 때는 항상 아래 5단계 중 하나를 선택하고, 임의의 중간값(예: 2.5px)을 만들지 않는다.

| 토큰 | 값 | 의미 | 용도 예시 |
|---|---|---|---|
| `--stroke-hairline` | 1px | 구조(기본 테두리) | `tactical-panel`, `data-row` 하단 구분선, `tree-node` 기본 테두리 |
| `--stroke-active` | 2px | 진행 상태 | `tree-node--active`(연구 중) |
| `--stroke-selected` | 3px | 선택 상태 | `country-row--selected`, `branch-row--selected` |
| `--stroke-accent-bar` | 4px | 카테고리 강조(좌측 바) | `accent-bar-row`, `tree-node` 좌측 바 |
| `--stroke-sub-accent` | 2px | 보조 강조(좌측 바, 격하) | `data-row--open`/`--closed` 좌측 바 |

**주의**: `--stroke-active`(2px, 전체 테두리)와 `--stroke-sub-accent`(2px, 좌측 바)는 값이
같지만 **적용 위치가 다르다**. 전체 테두리(`border-width`) 자리에 2px를 쓸 때는 반드시
"진행 상태"를 의미해야 하고, 좌측 바(`border-left-width`) 자리에 2px를 쓸 때는 "격하된
accent bar"를 의미해야 한다 — 같은 숫자·다른 CSS 프로퍼티(전체 vs 좌측)로 두 의미를
분리하며, 두 의미를 같은 프로퍼티 자리에서 혼용하지 않는다.

### Stroke Semantic Table

| 컴포넌트/상태 | 두께 | 역할 | 비고 |
|---|---|---|---|
| `tactical-panel` | 1px hairline | 구조 | |
| `data-row` 하단 구분선 | 1px hairline | 구조 | |
| `tree-node`(locked/available) | 1px hairline | 구조 | |
| `tree-node--active` | 2px active | 진행 상태 | |
| `tree-node--maxed` | 2px active(색: gold) | 진행 상태(완료) | |
| `country-row--selected` | 3px selected | 선택 상태 | |
| `branch-row--selected` | 3px selected | 선택 상태 | 기존 2px는 active와 혼동되어 수정 |
| `accent-bar-row` / `tree-node` 좌측 바 | 4px accent-bar | 카테고리 강조 | |
| `data-row--open`/`--closed` 좌측 바 | 2px sub-accent | 보조 이분 상태 | 기존 3px는 selected와 혼동되어 수정 |
| `country-row`/`pathogen-card` 기본 테두리 | 2px(예외) | 메뉴카드 예외 | "브리핑룸 톤 메뉴류"(Design Principles 5번)로 이미 각짐 규칙에서 면제된 카드군. Stroke System 5단계에 억지로 맞추지 않는다 — 변경 시 MainMenu 6장+CountrySelect 48행 전체 시각 밀도가 바뀌므로 별도 검토 필요 |

## Surface Hierarchy

HashiCorp의 "surface lift, never shadow"를 그대로 채택하되, 회색 계단(charcoal ladder)
대신 **알파값 계단 + 발광 테두리**로 구현한다. 5단계:

| 단계 | 배경 | 테두리 | 사용처 |
|---|---|---|---|
| 0. Canvas | `bg-root` / `bg-panel-alt` | 없음 | 전체화면 바닥 |
| 1. Scrim bar | `bg-scrim-soft` / `bg-scrim-strong` | 상/하 1px `accent-glow-soft` | resource-strip, population-bar, graph-panel, action-strip — "떠있는 패널"이 아니라 화면에 고정된 크롬 바 |
| 2. Docked panel | `bg-panel` / `bg-panel-strong` | 1px `accent-glow-soft`(전체) | event-dock, country-dock, tactical-panel, detail-panel |
| 3. Card/row | `surface` / `surface-soft` | 1px `border`(중립) 또는 상태색 | tree-node, graph-frame, world-status-frame, population-bar__track |
| 4. Selected/active emphasis | `surface-selected` 또는 상태별 rgba | 상태색 2px | tree-node--active/--maxed, border-selected 카드 |

규칙: **단계가 올라갈수록 배경 alpha가 진해지거나 테두리가 굵어질 뿐, 그림자는 절대
추가하지 않는다.** 새 패널을 만들 때는 이 5단계 중 하나를 선택하고, 중간값을 임의로
만들지 않는다.

## Component Library

화면 전용이 아닌, **재사용 가능한 시스템 컴포넌트**만 기록한다(화면 배치는
`UI_Design.md`). 이미 `Tactical.uss`(공용) 또는 `Hud.uss`/`UpgradeTree.uss`(화면 로컬,
2회 이상 재사용 시 `Tactical.uss`로 승격 예정)에 구현되어 있다.

### tactical-panel
기본 패널 셸. `border: 1px accent-glow-soft`, `radius: 0`. `.tactical-panel__header`
(하단 헤어라인 `grid-line`) + `.tactical-panel__title`(`accent-glow`, `tracking-caption`)
조합으로 헤더를 구성한다.

### corner-cut
`position: absolute` 대각선 틱 4종(`--tl/--tr/--bl/--br`, 13×2px, `accent-glow`,
±45° 회전). `position: relative`인 부모(패널) 안에 자식으로 배치한다. 배경 도형에는
영향을 주지 않는 순수 오버레이 — `picking-mode: Ignore`로 클릭을 통과시킨다.

### data-row / data-label / data-value — 판독 행 (Binance dense-row 차용)
가장 기본이 되는 "라벨:값" 단위. `flex-direction: row; justify-content: space-between`,
하단 1px `grid-line` 구분선. `data-label`은 `text-tertiary` + `tracking-label`,
`data-value`는 `text-secondary` + bold + `tracking-label`. severity 수정자
(`--infected`/`--dead`/`--danger`/`--info`)로 값 색상만 교체한다. Binance의
`markets-row`+`price-up-cell`/`price-down-cell` 구조를 국가/노드 도메인에 맞게 옮긴
것 — **새로운 수치 판독 UI가 필요하면 항상 이 컴포넌트부터 검토**한다.

### detail-rows — 스펙 테이블 (ClickHouse 패턴 차용)
`data-row`를 세로로 쌓는 빈 컨테이너 계약(`margin: space-sm 0`). 컨트롤러가 런타임에
`data-row`를 Add한다. UpgradeTree의 `detail-panel`("연구 분석 콘솔" — 업그레이드
비용/효과/상태를 key:value로 나열)과 Country Dock이 이 패턴이다. ClickHouse의
`code-window-card`(SQL key:value 스펙)와 동일한 정보 구조를, 모노스페이스 서체 없이
`data-row` 반복으로 구현한 것 — **엔티티 상세 정보(업그레이드 노드, 국가, 향후 병원체
등)는 항상 `detail-rows` + `data-row` 반복으로 표현**하고, 문단형 설명 텍스트로
되돌리지 않는다.

### stat-chip / stat-chip-row / stat-label
아이콘 폰트 대체 언어. `stat-chip`(6×6px 색상 사각형) + `stat-label`(볼드, 말줄임
처리). `stat-label--day`/`--dna`/`--phase` 등 고정폭 변형으로 자릿수 변화 시 레이아웃
흔들림을 방지한다.

### world-status-frame
상시 표시되는 상태 판독창. 평시엔 라벨 폭이 0에 가깝게 줄고, 위험 상태일 때만
(`--mortality--active`) 배경/테두리가 danger 색으로 강조된다 — "값이 있을 때만 크롬이
등장"하는 패턴의 표준 예시.

### population-bar
3세그먼트(healthy/infected/dead) 스택형 막대. `flex-basis: 0` + `flex-grow` 비율만으로
폭이 정해지는 순수 비율 컴포넌트(Painter2D 없음). 세그먼트 색은 severity 축(5번)을
그대로 사용.

### graph-panel / stat-graph-item (+ graph-frame/baseline/gridline)
고정 프레임(104×52px) 안에 절대배치 스파크라인을 얹는 구조. `baseline`(50%, `accent-glow-soft`)
+ `gridline--upper/--lower`(25%/75%, `grid-line`, 더 옅음) 2단계 밝기로 "기준선은
밝게, 보조 격자는 옅게" 위계를 준다.

### tree-node
`background: surface`, `border-left: 4px 카테고리색`, 상태 클래스(`--locked/--available/
--active/--maxed`)로 테두리·배경·opacity 결정. 내부는 `__code`(캡션) → `__label`
(본문, `white-space: normal`로 한글 줄바꿈 허용) → `__status` → `__cost` 4줄 세로 스택.

### tab-button / tab-button--secondary
주 액션(발광 테두리, `accent-glow`) vs 보조 액션(중립 테두리, `border`)을 테두리 색
하나로 구분하는 하단 액션 바 버튼.

## Usage Rules

Component Library 각 항목의 "언제 이렇게, 언제 이렇게 하지 않는지"를 명문화한다. 원래
`UI_Design.md`(화면 설계 문서) 여러 절에 흩어져 있던 컴포넌트 사용 규칙 — 코너컷 배치/밀도
기준, severity와 노드 상태 색상을 분리한 이유 — 를 디자인 시스템 규칙으로 이곳에 통합했다.

### Corner Cut

**Do**
- 카드/패널(개별 컴포넌트) 단위에만 부착한다. `position: relative`인 부모 안에 자식으로
  4개(또는 2개)를 절대배치한다.
- 화면당 인스턴스 수가 적은 패널(1~6개 내외 — 예: pathogen-card 6장, 화면별 detail-panel/
  모달/엔딩 스코어 패널)에는 **4개 전부** 사용한다.
- 강조 정도를 낮추고 싶으면 2개(tl/br)만 쓰는 축소형도 허용한다.
- 항상 `picking-mode: Ignore`로 설정해 아래 실제 콘텐츠의 클릭/터치를 막지 않는다.

**Don't**
- 화면 루트(전체화면 컨테이너)에는 절대 붙이지 않는다 — 모바일 SafeArea 인셋과 겹칠 수
  있다. 코너컷은 항상 화면 안의 개별 패널에만 붙인다.
- 수십 개가 반복되는 리스트 행이나 노드(예: 48행 리스트, 카테고리당 9노드)에는 코너컷을
  쓰지 않는다 — 대신 좌측 4px `accent-bar-row`로 대체해 밀도를 확보한다.

### Severity Colors

**Do**
- `--color-status-infected/dead/danger/info` 4색은 **국가 데이터**(감염/사망/위험/정보)에만
  사용한다 — population-bar, Country Dock, 국가 리스트 행, 뉴스 이벤트 dot 등.
- 새로운 국가 관련 수치 UI를 추가할 때는 이 4색을 그대로 재사용하고 새 색을 만들지 않는다.

**Don't**
- 노드 상태(잠김/가능/활성/완료), UI 크롬(테두리/헤더/코너컷), 브랜드 강조(자원/프리미엄)에
  이 4색을 가져다 쓰지 않는다. **이유**: severity 색은 "국가가 처한 상황"이라는 의미로 화면
  전체에 고정되어 있어, 다른 맥락(예: 업그레이드 노드)에 재사용하면 "이 노드가 위험하다"는
  잘못된 신호를 줄 수 있다.

### Node State Colors

**Do**
- LOCKED/AVAILABLE/ACTIVE/MAXED 4단계는 **브랜드 색상**(`--color-brand-dna` 초록 = 진행 중,
  `--color-brand-gold` 금색 = 완료)과 중립색(`--color-text-tertiary`/`--color-border` = 잠김·
  대기)으로 표현한다.
- 카테고리 정체성(전파/증상/능력)은 좌측 4px accent-bar, 상태는 테두리 전체+배경으로 위치를
  분리해 한 카드 안에서 두 축이 겹치지 않게 한다.

**Don't**
- Severity 색상을 노드 상태에 재사용하지 않는다. **이유**: severity는 "국가 데이터" 축,
  노드 상태는 "플레이어 진행도" 축으로 의미가 완전히 다르다. 이미 업그레이드 화면 전용으로
  쓰이는 DNA초록/금색 두 브랜드색을 "진행 중/완료" 축으로 재사용하는 편이 두 축을 혼동하지
  않으면서 의미상으로도 더 맞다.

### Tactical Panel

**Do**
- 테두리는 항상 1px `--color-accent-glow-soft`, `border-radius: 0`. 배경은 화면 맥락에 맞는
  Surface Hierarchy 단계(`--color-bg-panel` 등)를 그대로 쓴다.
- `tactical-panel`은 **테두리 + 헤더 규약만 제공**하고 배경색을 강제하지 않는다 — 카드 배경이
  화면마다 다를 수 있음을 허용한다(예: `pathogen-card`는 `--color-surface`를 유지한 채
  `tactical-panel` 테두리만 병기해도 된다).

**Don't**
- `box-shadow`, 그라디언트, 둥근 모서리를 추가하지 않는다.
- 인스턴스 수가 많은 리스트 행에는 전체 패널이 아니라 `data-row`/`accent-bar-row`만
  적용한다 — `tactical-panel`은 "화면당 개수가 적은 핵심 패널"에만 쓴다(Corner Cut 규칙과
  같은 기준).

## Button System

Unity 기본 런타임 버튼(둥근 회색)이 방치되면 Tactical Design System과 바로 충돌한다(과거
`EndingScreen.uss .ending-button`이 이 버그였다가 수정된 전례가 있음, 파일 내 주석 참고).
새 버튼을 만들 때는 항상 아래 3종 중 하나로 분류하고, `background-color`/`border`/
`border-radius`를 **반드시 명시적으로 지정**한다 — width/height/padding/margin만 지정하고
끝내지 않는다.

### Primary Button
확정/진행 액션(다음 단계, 연구 시작, 부활 등). `background-color: --color-accent-glow-soft`
(또는 문맥에 맞는 브랜드색 — 프리미엄 액션은 `--color-brand-premium`), `border: 1px
--color-accent-glow`(또는 브랜드색과 동일), `border-radius: 0`, `color: --color-text-primary`.
예: `popup-footer-button--confirm`, `ending-button--revive`, `mainmenu-next`.

### Secondary Button
중립/취소/닫기/뒤로가기 액션. `background-color: --color-bg-panel`, `border: 1px
--color-accent-glow`, `border-radius: 0`, `color: --color-text-primary`. 예:
`popup-footer-button`(기본), `ending-button`(기본), `popup-close`, `status-close-btn`,
`mainmenu-back`.

### Danger Button
파괴적 확인 액션(예: 진행 중 게임 포기, 저장 데이터 삭제 — 현재 코드베이스에 아직 인스턴스
없음, 예약 정의). `background-color: --color-bg-panel`, `border: 1px --color-status-danger`,
`color: --color-status-danger`, `border-radius: 0`. Severity 색(`--color-status-danger`)을
버튼 크롬에 쓰는 유일한 예외다 — "파괴적 액션 경고"라는 의미가 국가 데이터 severity와
자연스럽게 겹치기 때문에 허용한다(Usage Rules > Severity Colors의 예외 조항으로 취급).

### Action Color 규칙 (Amber — 예약 토큰, 미적용)
신규 Action Color로 **Amber**를 지정한다. 이번 작업에서는 토큰을 문서에 예약만 해두고
코드(Theme.uss)에는 적용하지 않는다 — 전면 적용은 별도 작업으로 남긴다. 향후 Primary
Button의 CTA 강조색으로 `--color-accent-glow` 대신(또는 함께) 도입할 후보이며, 실제 도입
시 아래 Semantic Color 규칙과 마찬가지로 Status Semantics 표에 "Action" 축을 추가해야
한다 — 임의로 기존 4축 중 하나에 끼워 넣지 않는다.

### Semantic Color 규칙 (Red/Blue/Orange/Gray — 신규 축, 미적용)
"전염병 vs 인류" 대립 구도를 색으로 표현하는 신규 개념 축이다: **Red = 전염병에게 유리**,
**Blue = 인류에게 유리**, **Orange = 주의**, **Gray = 중립**. **기존 Severity 축(Color
System 5번 — 감염/사망/위험/정보, 국가 데이터 전용)과는 다른 축**이며, 현재 코드베이스에
아직 와이어링되지 않은 상태다. 이후 게임플레이 밸런스 UI(예: 특정 업그레이드가 전염병에
유리한지/인류 저항에 유리한지 표시)에 적용할 때는 기존 `--color-status-*` 토큰과 이름이
겹치지 않는 새 토큰(`--color-semantic-red` 등)을 Theme.uss에 만들고, 이 문서의 Status
Semantics 표에 5번째 축으로 추가한다. 이번 작업 범위에서는 문서화만 하고 실제 컴포넌트에
적용하지 않는다.

## Interaction Rules

- **버튼**: `:active` 시 `scale: 0.96 0.96`만 적용, 배경색은 절대 건드리지 않는다(색
  버튼 위에서도 자연스럽게 동작). `:disabled`는 `opacity: 0.5`.
- **선택 가능 카드/행** (`pathogen-card`, `country-row`, `tree-node`, `status-row`):
  hover 시 `border-color`/`background-color`가 `--transition-normal`(0.18s ease-out)로
  부드럽게 전환된다. 새 선택형 컴포넌트를 추가할 때 이 트랜지션 계약을 그대로 재사용하고,
  임의의 hover 스타일을 새로 만들지 않는다.
- **USS 기술적 제약(모든 규칙의 전제)**: `box-shadow`, `clip-path`/`mask`,
  `background-repeat`, `:last-child` 미지원. 따라서 그림자 대신 헤어라인, 코너 절삭
  대신 코너컷 오버레이, 반복 배경 대신 정적 라인 배치, 마지막 행 예외 처리 대신
  "옅은 색이라 안 보여도 무방"으로 설계한다. 새 컴포넌트 설계 시 이 4가지 미지원 기능을
  요구하는 방식을 먼저 배제한다.

## Status Semantics

이 시스템은 **서로 다른 4개의 색상 축**을 유지한다(5번 항목 색상 표 참고). 요약:

| 축 | 토큰 그룹 | 의미 | 재정의 금지 범위 |
|---|---|---|---|
| 브랜드 악센트 | brand-dna/gold/premium | 자원/강조/프리미엄 | 3색 고정, 신규 추가 금지 |
| Severity | status-infected/dead/danger/info | 국가 데이터(감염/사망/위험/정보) | 노드 상태·UI 크롬에 사용 금지 |
| 노드 카테고리 | node-transmission/symptom/ability | 업그레이드 계열 정체성 | 카드 전체 배경에 칠하지 않음(좌측 바만) |
| 구조용 발광색 | accent-glow(-soft), grid-line | 테두리/캡션/격자 | 데이터 값에 사용 금지 |

새 기능이 생겼을 때 "이게 4축 중 어디에 속하는가"를 먼저 판정한다. 어느 축에도 속하지
않는다면 새 축을 만들기 전에 이 문서를 먼저 갱신한다(임의로 5번째 색을 끼워 넣지 않는다).

## Data Visualization Rules

- **비율형 데이터(population-bar)**: 항상 3세그먼트 고정 순서(healthy→infected→dead),
  `flex-basis: 0` + `flex-grow` 비율로만 표현. 절대 픽셀 폭을 계산해 대입하지 않는다.
- **추세형 데이터(graph-frame/sparkline)**: 프레임 크기 고정(104×52px), 기준선 1개
  (밝은 `accent-glow-soft`) + 보조 격자 2개(옅은 `grid-line`, 25%/75%) — 격자는
  많을수록 좋은 게 아니라 "기준선만 밝고 나머지는 옅다"는 밝기 위계가 핵심이다.
- **수치형 데이터(data-row/stat-chip)**: 아이콘 글리프를 실기기 검증 없이 먼저 쓰지
  않는다 — 항상 색상 칩(`stat-chip`) 또는 라벨:값(`data-row`) 조합이 기본값이고,
  아이콘은 검증 후에만 대체재로 승격한다.
- **신규 판독 UI 추가 규칙**: 위 3가지(비율/추세/수치) 중 하나로 분류되지 않는 새로운
  시각화가 필요하면, 이 문서의 Component Library를 먼저 확장하고 화면에 바로 구현하지
  않는다.

## Do / Don't

### Do
- 새 패널은 항상 5단계 Surface Hierarchy 중 하나를 선택하고 `accent-glow-soft` 헤어라인으로
  시작한다.
- 새 수치 판독은 `data-row` 또는 `stat-chip` 패턴을 재사용한다.
- 카테고리색은 좌측 `accent-bar`(4px), 상태색은 테두리+배경 — 위치를 분리해 같은 카드
  안에서 두 축이 겹치지 않게 한다.
- 업그레이드/국가 등 엔티티 상세 정보는 `detail-rows` + `data-row` 반복(스펙 테이블)으로
  표현한다.
- 위계는 배경 alpha 단계와 테두리 굵기/색으로만 표현한다.

### Don't
- `box-shadow`, 그라디언트, 둥근 pill 버튼을 도입하지 않는다 — `tactical-panel`은 항상
  `radius: 0`.
- 실기기에서 검증되지 않은 아이콘 폰트 글리프를 먼저 쓰지 않는다 — `stat-chip`이 검증된
  대체재다.
- 모노스페이스 서체를 도입하지 않는다 — 한글 줄바꿈/자간 문제가 있고, 기술적 톤은
  자간(tracking) + 굵기로 이미 표현되고 있다.
- 브랜드 악센트 색을 3개(DNA그린/골드/프리미엄퍼플) 이상으로 늘리지 않는다 — HashiCorp식
  "기능마다 새 색"을 이 시스템은 채택하지 않는다.
- Severity 4색·노드 상태 4색·카테고리 3색·구조용 발광색을 서로의 의미로 교차 사용하지
  않는다.
- 라이트모드 파생을 만들지 않는다 — 캔버스는 항상 다크.
- 화면 전용 레이아웃(배치, 화면 흐름, 우선순위)은 이 문서에 쓰지 않는다 — `UI_Design.md`
  역할이다.
