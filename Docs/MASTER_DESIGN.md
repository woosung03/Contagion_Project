# Master Design Specification

> 이 문서는 Contagion Project UI의 **Constitution**이다. 신규 UI 제작, 기존 UI 리팩터링, Theme
> Token, Auto Scale, 반응형 레이아웃은 전부 이 문서를 기준으로 설계한다. Legacy 구현체(Git
> History)는 참고 자료일 뿐이며, 이 문서가 최상위 Source of Truth다.
>
> **범위**: 이 문서는 Scale·Typography·Spacing·Component 크기·Layout Grammar만 다룬다. 색상
> 체계(Color System)는 범위 밖이며 `DESIGN.md` 2절이 계속 그 권위를 가진다.

---

## 1. Philosophy

- **Atomic Grid 우선**: Spacing·Layout 치수는 2px 배수다. 임의 px는 존재하지 않는다.
  ([UI Scale Pass, 2026-07-21] Typography Role은 Base 재보정 이후 반올림 근사치를 허용한다 —
  2절/3절 참고. 이 예외는 폰트 크기에만 적용되며 Spacing/Component 배치 치수는 그대로 엄격하다.)
- **역할 기반 Typography**: 폰트 크기는 "몇 px인가"가 아니라 "무슨 역할인가"로 결정한다.
- **정보 밀도 우선**: 장식보다 판독 가능한 수치·상태를 우선한다.
- **작은 화면에서의 가독성**: 모든 컴포넌트는 최소 터치 타깃과 최소 글자 크기를 만족해야 한다.
- **일관된 Scale**: 컴포넌트 크기는 항상 Base의 배수로 정의하며, 화면마다 다른 임의 값을 만들지
  않는다.
- **Component 재사용**: 새 화면은 새 컴포넌트를 만들기 전에 기존 Component Rule(5절)로 표현
  가능한지 먼저 검토한다.
- **예외는 규칙 안으로 편입한다**: 예외가 필요하면 임시방편으로 남기지 않고 8절에 정식 등재한다.

---

## 2. Design Foundation

### Base

```
Base = 19  (2026-07-21 UI Scale Pass 이전 값: 16)
```

모든 Typography·Spacing·Component 치수는 Base의 배수로 정의한다.

> **[UI Scale Pass, 2026-07-21]** 실플레이 가독성·터치성 개선을 위해 Base를 16→19(+18.75%)로
> 재보정했다. 이 재보정 이후 §3 Typography Role 값은 "Base×배수"의 **반올림 근사치**다 —
> 16이었던 시절에는 배수 공식이 정수로 정확히 맞아떨어졌지만, 완전한 정수 배수를 유지하는
> Base 값이 15~20% 범위 안에 없어(다음 정수 배수 지점은 Base=32, 두 배) 이번부터는 근사치
> 방식으로 전환한다. §4 Spacing System은 Atomic Grid(2px 배수) 규칙을 그대로 엄격히 지킨다 —
> 근사치 전환은 Typography에만 적용된다.

### Atomic Grid

```
Atomic Grid = 2px  (Base × 0.125)
```

어떤 치수든 이 격자 위에 있어야 한다 — 2px로 나누어떨어지지 않는 값은 존재할 수 없다.

### Canvas

```
Canvas = 480 × 1040
```

Canvas는 **설계 좌표계**다. 실제 렌더링 해상도가 아니며, 실기기 대응은 PanelSettings의 Scale
System이 전담한다. 새 레이아웃을 설계할 때는 항상 이 480×1040 좌표계를 기준으로 비율을 계산한다.

---

## 3. Typography System

Typography는 px 값이 아니라 **역할(Role)**로 지정한다. 새 텍스트를 추가할 때는 반드시 아래 6개
역할 중 하나를 선택하고, 그 역할에 대응하는 크기만 사용한다.

| Role | Base 배수(근사) | 크기 | 이전 값(~2026-07-21) | 정의 |
|---|---|---|---|---|
| **Hero** | ≈×2.5 | 48px | 40px(+20%) | 화면을 대표하는 단 하나의 결과 문구(승/패 타이틀 등). 화면당 최대 1회. |
| **Display** | ≈×2.0 | 38px | 32px(+19%) | 화면의 대표 타이틀(Fullscreen 화면 최상단 제목). |
| **Title** | ≈×1.5 | 28px | 24px(+17%) | Section/Dialog 제목. Overlay Panel·Header의 기본 제목 크기. |
| **Subtitle** | ≈×1.125 | 21px | 18px(+17%) | 보조 제목, 리스트 행의 주요 값(라벨·이름). |
| **Body** | ×1.0(Base) | 19px | 16px(+19%) | 기본 읽기 텍스트. 모든 역할의 기준점. |
| **Caption** | ≈×0.875 | 17px | 14px(+21%) | 보조 정보, 부가 설명, 작은 라벨. |

**[UI Scale Pass, 2026-07-21]** 위 표는 재보정 후 값이다 — Base가 더 이상 완전한 정수 배수를
만들지 않아(2절 참고) "Base 배수" 열은 근사치(≈)로 표기한다.

### Panel Title 확장

Bottom Sheet Content Area(6절)처럼 화면의 대부분을 차지하는 **전체 패널의 헤더**는 Title(28px)
대신 한 단계 큰 **Panel Title(≈Base×1.625=31px)** 을 쓴다. Overlay Panel/Dialog급 제목(Title,
28px)과 Fullscreen급 패널 제목(Panel Title, 31px)을 구분하는 것이 목적이다. (이전 값: Title
24px/Panel Title 26px)

### 밀도 전용 축소 캡션

트리 캔버스처럼 반복 요소가 매우 많아 Caption(17px)도 과한 맥락에서는 **≈Base×0.8125(15px)** 를
Caption의 하위 변형("Caption-Dense")으로 허용한다. 단, 이 크기는 반복 밀집 요소(노드 라벨 등)
안에서만 쓰고, 문장형 설명 텍스트에는 쓰지 않는다. (이전 값: 13px)

### 사용 금지

- Body(19px)보다 작은 크기를 문장형 설명 텍스트에 쓰지 않는다(Caption-Dense는 노드/칩 라벨 전용).
- 6개 역할(+Panel Title, +Caption-Dense) 밖의 임의 폰트 크기를 추가하지 않는다.
- 같은 화면 안에서 같은 역할에 서로 다른 크기를 쓰지 않는다.

### Hierarchy

```
Hero(48px) > Display(38px) > Panel Title(31px) > Title(28px) > Subtitle(21px)
> Body(19px) > Caption(17px) > Caption-Dense(15px)
```

---

## 4. Spacing System

### Atomic Grid

```
2px (Base × 0.125)
```

### Spacing Token

| 토큰 | 크기 | 이전 값(~2026-07-21) |
|---|---|---|
| xs | 4px | 4px(변경 없음) |
| sm | 10px | 8px(+25%) |
| md | 16px | 12px(+33%) |
| lg | 20px | 16px(+25%) |
| xl | 24px | 20px(+20%) |
| xxl | 28px | 24px(+17%) |

**[UI Scale Pass, 2026-07-21]** "UI가 빽빽해 보인다"는 문제의 핵심 원인을 여백 부족으로 보고
Spacing Scale을 Typography보다 더 크게 상향했다. xs(4px)만 그대로 뒀다 — Atomic Grid 최소
단위에 가까워 체감 밀도에 미치는 영향이 적고, 2px 배수 안에서 15~20%대 증분을 만들 정수가
마땅치 않았기 때문이다. 이 표는 더 이상 Base의 단순 배수(×0.25/×0.5...)로 표현되지 않는다 —
Atomic Grid(2px 배수) 규칙만 그대로 유지한다(아래 "허용 범위" 참고).

### 사용 규칙

- 새 여백은 항상 위 6개 토큰 중 하나를 먼저 검토한다.
- 6개 토큰으로 표현이 안 되는 중간값이 꼭 필요하면(예: 10px, 14px), **2px Atomic Grid 위에서만**
  정의하고 반드시 새 토큰으로 등록한다 — 리터럴 값으로 흩뿌리지 않는다.

### 허용 범위

- 2px의 정수배(2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24...)만 허용.

### 금지 사항

- 2px로 나누어떨어지지 않는 여백(홀수 px)은 어떤 경우에도 금지한다.
- 토큰 없이 같은 리터럴 px 값을 3곳 이상에서 반복 사용하지 않는다 — 반복되면 즉시 토큰화한다.

---

## 5. Component Rules

### Button

| 항목 | 규칙 |
|---|---|
| 필수 | 높이는 7절 Button Hierarchy의 Tier 값만 사용한다. |
| 권장 | Primary Action Tier(48px)를 기본값으로 우선 검토한다. |
| 예외 | Icon/Confirm Tier가 필요하면 7절에 등재된 것만 사용, 임의 높이 금지. |
| 금지 | Tier 3종 밖의 새 버튼 높이를 만들지 않는다. |

### Card

| 항목 | 규칙 |
|---|---|
| 필수 | Border = Base×0.125(2px), Radius = Base×0.375(6px). |
| 권장 | Padding은 Spacing Token(sm~lg) 중 하나. |
| 예외 | 대량 반복 리스트(수십 행 이상)는 Border 대신 좌측 accent-bar(4px)로 대체 가능. |
| 금지 | Card 안 텍스트에 Typography Role 밖의 크기를 쓰지 않는다(3절). |

### Popup(Overlay Panel)

| 항목 | 규칙 |
|---|---|
| 필수 | Radius = Base×0.5(8px), Padding = Base×1.0(16px, lg). |
| 권장 | 폭은 Base의 정수배(예: Base×20=320px)로 고정하고 화면 중앙에 배치. |
| 예외 | 콘텐츠가 많아 폭 확장이 필요하면 Base×24(384px)까지 허용. |
| 금지 | %기반 가변 폭을 쓰지 않는다(Overlay Panel은 고정폭 원칙, 6절 Panel과 구분). |

### Panel(Bottom Sheet / Content Area)

| 항목 | 규칙 |
|---|---|
| 필수 | Radius = Base×0.5(8px). Padding = Base×1.25~1.5(xl/xxl). |
| 권장 | 대시보드형(콘텐츠 많음)은 화면 높이의 87.5%(상단 시작점 12.5% 지점)까지 확장 가능. |
| 예외 | Panel Composition은 6절 원칙에 따라 화면 목적별 독립 설계 허용(공통 Base 배수 규칙 없음). |
| 금지 | 서로 다른 목적의 두 Panel에 동일한 인셋 값을 강제하지 않는다. |

### Row(List Item)

| 항목 | 규칙 |
|---|---|
| 필수 | Radius = Base×0.375(6px) 또는 좌측 accent-bar(4px, 반복 리스트용). |
| 권장 | Padding은 Spacing Token(sm~md). |
| 금지 | 행 안의 이름/메타 텍스트는 Subtitle/Caption 역할만 사용(3절). |

### Progress Bar

| 항목 | 규칙 |
|---|---|
| 필수 | Master Design에 전례가 없는 신규 컴포넌트 — Atomic Grid만 준수하면 새로 설계 가능. |
| 권장 | 트랙 높이는 Base×0.875(14px) 권장(Caption 크기와 시각적 정렬을 맞추기 위함). |
| 금지 | Border/Radius는 Card 규칙(2px/6px)을 그대로 재사용, 별도 값을 만들지 않는다. |

### Graph

| 항목 | 규칙 |
|---|---|
| 필수 | Height = Base×2.125(34px). |
| 권장 | Width는 고정폭 대신 부모 폭에 반응(Responsive)하도록 구성 — 높이만 Grammar 값 고정. |
| 금지 | Height를 화면마다 다르게 재정의하지 않는다. |

### Icon

| 항목 | 규칙 |
|---|---|
| 필수 | 정사각 아이콘은 Base×2.0(32px) 또는 Base×2.25(36px) 중 하나. |
| 권장 | 국기 등 비율 고정 아이콘은 Base×2.25 × Base×1.625(36×26px) 비율 유지. |
| 금지 | Atomic Grid 밖의 아이콘 크기(홀수 px) 금지. |

### Panel Header

| 항목 | 규칙 |
|---|---|
| 필수 | 제목은 Typography Role "Panel Title"(26px, 3절) 사용. 닫기 버튼이 있다면 Button Hierarchy Tier 2(Icon Button, 36px). |
| 권장 | 본문과의 경계는 헤어라인 구분선(Section Divider 규칙 재사용) 하나로 표현. |
| 예외 | 없음. |
| 금지 | 제목에 Role 밖 크기 사용 금지. |

**발견된 불일치(코드 미변경, 기록만)**: 현재 코드베이스의 `tactical-panel__title`은 13px(Caption-Dense 수준)를 쓰고 있어 이 규칙(26px)과 다르다. Tactical 시대의 의도된 "작고 넓은 자간의 판독 라벨" 스타일일 수도 있고 단순 누락일 수도 있어 판단을 보류한다 — 코드 변경은 이번 문서화 범위 밖이다.

### Section Header

Panel 본문 내부에서 하위 그룹을 나누는 캡션(예: "세계 요약", "정밀 진단").

| 항목 | 규칙 |
|---|---|
| 필수 | Typography Role "Caption"(14px, 3절) 사용. 강조는 색상·자간만으로 표현한다. |
| 권장 | 상단 여백은 Spacing Token 하나(`sm`)로 통일. |
| 예외 | 접기/펼치기 가능한 Section Header(아코디언)는 Icon Button Tier(36px) 이상의 터치 높이를 확보한다. |
| 금지 | Section Header는 UI 크롬이므로 Severity 4색(2.2절, 국가 데이터 전용)을 사용하지 않는다. |

### Section Divider

Section 사이를 구분하는 순수 구조 요소(예: 헤어라인).

| 항목 | 규칙 |
|---|---|
| 필수 | 두께는 Border Scale 최소값(5절 Card Border 참고), 색상은 중립(그리드 라인류)만 사용. |
| 권장 | 상하 여백은 Spacing Token(`xs`~`sm`). |
| 예외 | 없음. |
| 금지 | 브랜드색·상태색을 구분선에 사용하지 않는다 — 순수 구조 요소로 유지. |

### Status Tile(Hero Stat류)

2×N 그리드로 배치되는 핵심 수치 카드(예: 감염자/사망자/치료제/소멸 국가).

| 항목 | 규칙 |
|---|---|
| 필수 | 라벨은 Caption Role(14px), 값은 Title Role(24px) 이상. |
| 권장 | 타일 폭은 `%`로 지정(예: 48%)해 화면 폭에 반응하도록 구성. |
| 예외 | 저반복(4개 이하) 타일은 코너컷 없이 얇은 테두리만 사용 가능. |
| 금지 | 값 텍스트에 Role 밖 크기 사용 금지. |

### Badge

상태(등급)를 표시하는 작은 칩(예: 공항/항구/국경 개방 여부).

| 항목 | 규칙 |
|---|---|
| 필수 | Border는 Border Scale 최소값, Radius는 `--radius-sm`. 배경은 투명 — 테두리+텍스트 색으로만 상태를 표현한다. |
| 권장 | Severity 4색 모디파이어(성공/경고/위험/정보)만 사용, 새 색상 축을 만들지 않는다. |
| 예외 | 없음. |
| 금지 | 배지에 배경색을 채우지 않는다(5절 구조용 발광색과 시각적으로 혼동되지 않도록). |

### Icon Button

| 항목 | 규칙 |
|---|---|
| 필수 | 높이/폭은 Button Hierarchy Tier 2(36px, 7절) 고정, 텍스트 없이 아이콘/기호 1개만 담는다. |
| 권장 | 닫기(✕) 등 보조 액션 전용으로 화면당 1~2개로 제한. |
| 예외 | 7절 Button Hierarchy가 이미 정의한 범위를 그대로 따른다 — 추가 예외 없음. |
| 금지 | Icon Button에 2글자 이상의 텍스트 라벨을 넣지 않는다 — 그런 경우 Primary/Confirm Tier로 전환한다. |

### Detail Panel

Fullscreen 화면 내부에 얹히는 소형 하위 패널(예: MainMenu/CountrySelect의 선택 항목 상세).

| 항목 | 규칙 |
|---|---|
| 필수 | 제목은 Title Role(24px), 본문 설명은 Body Role(16px, 3절). |
| 권장 | `min-height`는 콘텐츠 실측 기반으로 고정해 레이아웃 흔들림을 방지한다. |
| 예외 | Panel(6절, Bottom Sheet)과 달리 Fullscreen 화면 안에 얹히므로 Safe Area 보정은 상위 Fullscreen 컨테이너가 대신 담당한다. |
| 금지 | Detail Panel 안에서 6절 Panel Composition의 인셋 규칙을 적용하지 않는다 — 서로 다른 레이어다. |

### Modal

`TacticalModalController` 계약을 따르는 Overlay Panel의 하위 유형.

| 항목 | 규칙 |
|---|---|
| 필수 | Popup(Overlay Panel) 규칙을 그대로 상속 — 별도 치수 체계를 만들지 않는다. |
| 권장 | 버튼 0~2개는 Modal Footer 컴포넌트(균등폭 배치)로 구성. |
| 예외 | 없음. |
| 금지 | Modal 전용 새 치수 체계를 만들지 않는다 — Popup 규칙 재사용이 원칙. |

### Bottom Navigation

| 항목 | 규칙 |
|---|---|
| 필수 | 버튼은 Button Hierarchy Tier 1(Primary, 48px, 7절). 전체 높이는 `--hud-bottom-nav-height`(70px) 토큰과 반드시 일치. |
| 권장 | 버튼 2~3개, 균등폭 또는 보조 액션만 축소 비율(0.6배)로 구성. |
| 예외 | 없음. |
| 금지 | Bottom Navigation 높이를 화면마다 다르게 재정의하지 않는다(6절 HUD 3단 구조의 고정 하단). |

---

## 6. Layout Rules

### HUD

HUD는 항상 3단 구조를 유지한다: **Status(상단, 고정) → Content Area(가변) → Bottom
Action(하단, 고정)**. Status/Bottom Action의 높이는 5절 Component Rule(Button/Graph 등)로부터
파생된 값이어야 하며, 임의로 지정하지 않는다.

### Popup / Status / Ranking / Panel

- Popup(Overlay)은 고정폭 + 중앙 배치(5절).
- Status/Ranking 등 Bottom Sheet Content Area는 Status Bar 아래·Bottom Action 위 공간만
  사용하며, 그 경계는 HUD 실측값(Component Rule에서 파생)을 참조해 계산한다 — 화면비 기반
  퍼센트를 HUD 실측값과 무관하게 독립적으로 정하지 않는다.
- **Panel Composition은 공통 Grammar가 아니다.** 각 Panel의 상하좌우 인셋은 화면 목적(콘텐츠
  분량, 강조하려는 정보)에 따라 독립적으로 설계한다 — CountryStatusPanel과 RankingPanel이 서로
  다른 인셋을 쓰는 것은 결함이 아니라 의도된 설계 자유도다.

### Safe Area

모든 화면의 최상위 컨테이너는 Safe Area 보정(상하좌우 인셋 자동 추가)을 받아야 한다. 예외 없음.

### Inset / Alignment / Anchor

- 화면 전체를 덮는 화면(Fullscreen)은 `top/left/right/bottom: 0`.
- Bottom Sheet Content Area는 좌우 `0`, 상하는 HUD 구조에서 파생된 spacer로 자동 계산.
- Overlay Panel은 중앙 정렬(`left:50%` + 음수 margin 또는 flex center) 고정폭.

### Layout 우선순위

1. Safe Area 보정
2. HUD 3단 구조(Status/Content/Bottom)
3. 화면 분류(Fullscreen/Overlay/Bottom Sheet/HUD Chrome) 확정
4. Panel Composition(화면별 독립 설계)

---

## 7. Button Hierarchy

| Tier | 역할 | Base 배수(근사) | 크기 | 이전 값 | 사용 위치 |
|---|---|---|---|---|---|
| **Tier 1 — Primary Action** | 기본 액션/내비게이션 | ≈×3.0 | 56px | 48px(+17%) | 탭 버튼, 확인/다음 버튼, 리스트 닫기, 주요 CTA 전반 — **기본값** |
| **Tier 2 — Icon Button** | 아이콘 전용 소형 컨트롤 | ≈×2.25 | 42px | 36px(+17%) | 다이얼로그 닫기(✕) 등 텍스트 없는 단일 아이콘 버튼 |
| **Tier 3 — Confirm/Terminal Action** | 되돌릴 수 없는 최종 결정 | ≈×3.5 | 64px | 56px(+14%) | 재시작/부활 등 화면당 극히 드문 1회성 확정 액션 |

**정의**: 새 버튼을 만들 때는 반드시 이 3개 Tier 중 하나를 먼저 선택한다. 최소 크기는 Tier
2(42px)이며, 이보다 작은 버튼은 존재할 수 없다.

**[UI Scale Pass, 2026-07-21]** 터치성 개선 목표로 3개 Tier 전부 상향(`--touch-target-min`도
Tier 1과 동일하게 48→56px). "헤더 내장 Icon Button 32px" 등 8절 Approved Exception으로 등재된
Tier 밖 버튼(예: `.popup-close` 32px)은 이번 조정 대상이 아니다 — Exception은 그 자체로 Tier
체계 밖에 있다.

---

## 8. Approved Exceptions

### Rule / Approved Exception / Design Debt 분류 기준

이 문서는 세 종류의 항목을 다루며, **절대 서로 혼용하지 않는다**.

| 분류 | 정의 | 구속력 |
|---|---|---|
| **Rule**(1~7·9~11절) | 프로젝트 전체가 따르는 기본 규칙 | 모든 신규 구현에 강제 적용 |
| **Approved Exception**(이 절) | 특정 화면·컴포넌트에서만 허용되는, **이유가 명확히 밝혀진** 의도적 이탈 | 등재된 화면·컴포넌트에만 적용, 다른 곳에 자동 확장되지 않음 |
| **Design Debt**(12절) | 아직 결론이 나지 않은 미결정 항목 — 버그도 예외도 아니다 | 결론이 날 때까지 현재 구현을 잠정 유지, Rule 위반으로 취급하지 않되 "해결된 것"으로도 취급하지 않음 |

**Rule과 Exception의 관계**: Exception은 Rule을 무효화하지 않는다 — Rule은 "기본값"으로 계속
유효하고, Exception은 "이 화면/컴포넌트에 한해 다른 값을 쓴다"는 등재된 예외 사항이다. 새 화면을
만들 때는 항상 Rule을 먼저 적용하고, 이 절에 등재된 항목과 정확히 같은 상황일 때만 해당 Exception
값을 재사용한다 — Exception이 있다고 해서 비슷해 보이는 다른 상황에 임의로 확장하지 않는다.

### 등재된 Exception

| 예외 | 적용 화면/컴포넌트 | 기본 Rule과의 차이 | 이유 | 다른 화면에 적용하지 않는 이유 |
|---|---|---|---|---|
| Panel Layout(Bottom Sheet 인셋) | 모든 Bottom Sheet 화면 | Base 배수가 아니라 %(화면비) 기반 | 6절 — Panel Composition은 애초에 공통 배수 규칙이 없음(Grammar Validation에서 확정) | 해당 없음(전 Bottom Sheet 공통 원칙) |
| **헤더 내장 Icon Button 32px** | CountryPopup(popup-close), CountryStatusPanel(status-close-btn) | Icon Button Tier 2(36px) 대신 32px(Base×2.0) | 헤더 한 줄에 국기/제목/배지 등과 함께 들어가야 해서 36px이면 헤더 높이가 늘어나거나 다른 요소와 부딪힘 — DESIGN.md에 이미 오래전부터 문서화된 관례(28px→32px로 탭 정확도 확보 이력 있음) | 헤더 바깥에 독립적으로 배치되는 신규 Icon Button(예: 향후 추가될 공유 버튼)은 이 예외 대상이 아니며 기본 Rule(36px)을 따른다 |
| Ending Confirm 버튼(재시작/부활) | EndingScreen | Button Hierarchy Tier 3(56px) | 되돌릴 수 없는 최종 결정이라 화면당 극히 드문 1회성 확정 액션에만 허용되는 확대 Tier | 다른 화면은 이런 성격의 액션 자체가 없음 |
| Panel Title | CountryStatusPanel/RankingPanel 등 Bottom Sheet Content Area 헤더 | Title(24px) 대신 Panel Title(26px) | Fullscreen급 패널 헤더는 Overlay Panel/Dialog급 제목보다 한 단계 큰 무게가 필요 | Overlay Panel(Popup)은 계속 Title(24px) 사용 |
| Caption-Dense(13px) | UpgradeTree tree-node 라벨 등 반복 밀집 요소 | Caption(14px) 대신 13px | 좁은 캔버스 노드 안에 많은 텍스트를 밀집 배치해야 함 | 문장형 설명 텍스트에는 절대 사용 금지(3절) |
| Spacing 중간값(예: 10px/14px) | 여러 화면의 개별 margin/padding | 명명된 Spacing Token 대신 중간값 | 2px Atomic Grid 안에서 세밀 조정이 필요했던 기존 관행 | 새 값은 반드시 이 표에 먼저 등재 후 사용(무제한 확장 금지) |
| **Compact Popup Header** | CountryPopup | 표준 Panel Header(제목+선택적 닫기) 대신 국기+이름+대륙+배지+닫기를 한 줄에 담는 자체 구조 | Bottom Sheet **Compact** 분류(DESIGN.md 12.1절)는 Content Area 계열과 정보 밀도 요구가 달라 표준 Panel Header로 표현 불가 | Bottom Sheet **Content Area** 계열(CountryStatusPanel/RankingPanel/UpgradeTree)은 표준 Panel Header를 따라야 함 — Compact 전용 예외 |
| **UpgradeTree 전용 Header 구조** | UpgradeTree(3개 카테고리 페이지) | 표준 Panel Header 대신 2행(탭 3개 + DNA/광고/닫기) 자체 구조 | 카테고리 탭 전환 + DNA 카운터 + 광고 버튼까지 한 헤더에 담아야 하는 유일한 화면 | 다른 화면은 이런 기능적 요구 자체가 없음 |
| **Graph 52px** | Hud(graph-panel) | Graph Rule(Base×2.125=34px) 대신 52px(Base×3.25) | 감염자/사망자/치료제 추세선 판독성 향상을 위해 기준선+격자선과 함께 의도적으로 확대(코드 주석에 이력 명시) | 이 예외는 **현재 존재하는 Hud 그래프에 한정**된다 — 향후 다른 화면에 Graph가 추가되면 34px Rule과 52px 예외 중 무엇을 기본으로 삼을지는 12절 Design Debt로 별도 결정한다 |
| **EndingScreen Hero 중복 사용** | EndingScreen(ending-title + ending-score) | "Hero는 화면당 최대 1회"(3절) 규칙 위반, 2회 사용 | 승/패 타이틀과 최종 점수 모두 "결과"라는 동일한 감정적 무게를 가져 의도적으로 동일 크기로 시각적 동등성을 부여(코드 주석에 이력 명시) | EndingScreen은 게임의 유일한 최종 결산 화면 — 다른 화면에서 여러 수치를 전부 Hero로 키우면 위계 자체가 무너짐. 향후 유사한 "클라이맥스 화면"이 생기면 12절 Design Debt로 재검토 |

이 문서에 등재되지 않은 예외는 **예외로 인정하지 않는다**.

### Exception 추가 절차

1. Compliance Audit 등으로 Rule과의 불일치를 발견한다.
2. 원인이 **명확히 설명 가능**한지 확인한다(코드 주석·설계 이력·기능적 필요성 등 근거 필요).
   원인이 불명확하면 Exception이 아니라 12절 **Design Debt**로 등재한다.
3. 위 표에 "적용 화면/컴포넌트 · 기본 Rule과의 차이 · 이유 · 다른 화면에 적용하지 않는 이유"
   4개 항목을 전부 채워 추가한다 — 하나라도 비어 있으면 등재하지 않는다.
4. 등재 후에만 구현에 반영한다(등재 전에 코드부터 바꾸지 않는다).

---

## 9. Legacy Exclusions

아래 항목은 Master Design의 Source of Truth가 아니다. 참고용으로만 남기며, 신규 구현·리팩터링
시 절대 그대로 가져오지 않는다.

| 화면 | 제외 항목 | 사유 |
|---|---|---|
| MainMenu | Card Text(이름/스탯), Subtitle, Detail Panel 텍스트 | Atomic Grid를 벗어난 홀수 px 하드코딩(19/21/17/22px) |
| CountrySelect | Subtitle, Detail Panel 텍스트, 뒤로 버튼 높이 | MainMenu와 스타일시트를 공유해 발생하는 동일 잔재 |
| UpgradeTree | Description 텍스트, Category Label | 홀수 px 하드코딩(17/19px) — 그 외 구조는 Master Design 채택 |

이 세 화면(과 해당 항목)은 **리팩터링 대상**으로 명시한다 — 향후 작업에서 위 표의 항목을 발견하면
3~5절의 Typography Role/Spacing Token으로 교체한다.

---

## 10. Design Checklist

새 UI를 만들거나 기존 UI를 수정할 때 아래를 전부 확인한다.

- [ ] 모든 텍스트가 3절 Typography Role(Hero/Display/Panel Title/Title/Subtitle/Body/
      Caption/Caption-Dense) 중 하나에 대응하는가
- [ ] 모든 치수가 2px Atomic Grid 위에 있는가
- [ ] 여백이 4절 Spacing Token(xs~xxl) 또는 등록된 예외 토큰만 쓰였는가
- [ ] 버튼이 7절 Button Hierarchy 3개 Tier 중 하나에 속하는가
- [ ] 5절 Component Rule(Button/Card/Popup/Panel/Row/Progress Bar/Graph/Icon/Panel Header/
      Section Header/Section Divider/Status Tile/Badge/Icon Button/Detail Panel/Modal/
      Bottom Navigation)의 필수 항목을 만족하는가
- [ ] 9절 Legacy Exclusions에 등재된 값을 그대로 복사하지 않았는가
- [ ] 임의 px(격자 밖, 역할 밖, Tier 밖)를 새로 추가하지 않았는가
- [ ] 새 Token이 필요하다면 8절 Exception Rules에 먼저 등재하고 근거를 남겼는가
- [ ] Panel을 새로 만든다면 화면 목적에 따라 인셋을 독립적으로 설계했는가(6절 — 공통 배수 강제
      아님)
- [ ] Safe Area 보정이 최상위 컨테이너에 적용됐는가
- [ ] Player-facing 텍스트가 11절 Language Policy(한국어 기본, 영어는 등재된 예외만)를 따르는가

---

## 11. Language Policy

### 기본 원칙

Player가 보는 모든 UI 텍스트는 **기본적으로 한국어**를 사용한다. 새 UI는 영어로 초안을 작성한
뒤 번역하는 방식이 아니라, **처음부터 한국어로 설계**한다.

### 용어집(공식 번역 매핑)

새 UI 텍스트를 작성할 때는 아래 매핑을 그대로 따른다 — 화면마다 다른 번역어를 새로 만들지
않는다.

| English | 한국어 |
|---|---|
| Main Menu | 메인 메뉴 |
| Continue | 이어하기 |
| New Game | 새 게임 |
| Upgrade | 진화 |
| Research | 연구 |
| Severity | 증상 |
| Infectivity | 감염력 |
| Lethality | 치사율 |
| Transmission | 전파 |
| Population | 인구 |
| Dead | 사망자 |
| Healthy | 건강 |
| News | 뉴스 |
| World | 세계 |
| Ranking | 순위 |
| Game Over | 게임 오버 |

**참고**: `업그레이드`(외래어 표기)는 이미 한글 표기이므로 이 정책상 "영어 미번역" 위반은
아니다. `진화`는 언어 정책 준수를 위한 교체가 아니라, 이 게임의 주제(병원체 진화)에 더 맞는
용어로의 **별도 창작 결정**이다 — 실제 UI 문자열 변경 여부는 이번 문서화와 별개로 판단한다.

### 영어 유지 대상(국제 통용 고유 용어)

다음은 번역하지 않고 영어를 그대로 유지한다.

- `DNA`, `RNA` — 과학적 고유 명칭
- `FPS`, `CPU`, `GPU` — 필요 시, 기술 고유 약어
- 그 외 게임 시스템이 고유 명칭으로 채택한 약어(신규 도입 시 아래 "예외 등재 규칙"을 따른다)

### 코드 식별자 정책

코드 식별자(클래스명·변수명·네임스페이스·파일명)는 이 정책의 적용 대상이 **아니다** — 계속
영어를 사용한다(`CountryDatabase`, `CountryData`, `HudController`, `UpgradeTree` 등). 이
정책은 **Player가 실제로 읽는 UI 문자열에만** 적용된다.

### Localization 원칙

UI 문자열은 향후 로컬라이제이션(다국어) 도입 가능성을 고려해 관리한다.

- 하드코딩 문자열은 가능한 한 줄이고, 재사용되는 문구는 한 곳에서 관리한다.
- **현재 이 프로젝트에는 로컬라이제이션 시스템(문자열 테이블 등)이 없다** — 이 원칙은 지금
  당장의 구현 요구가 아니라 향후 도입을 어렵게 만들지 않기 위한 사전 방침이다.

### 예외 등재 규칙

영어가 플레이 경험을 명확히 개선하는 경우에만 예외를 허용하며, **이 표에 명시적으로 등재된
것만** 유효한 예외다. 등재되지 않은 영어 UI 텍스트는 전부 정책 위반으로 간주한다.

| 예외 | 사유 |
|---|---|
| `DNA` | 과학적 고유 명칭, 번역 시 의미 손실 |
| `DNA Point` | DNA와 동일 — 자원 명칭에 고유 명사 유지 |
| `DNA Mutation` | DNA와 동일 |

### 발견된 기존 위반 후보 — 해결됨(2026-07-21, UI Localization Migration Phase 1)

Tactical Console 시각 언어(DEFCON/Frostpunk 참고)의 일부로 아래 영어 대문자 캡션들이 있었으나,
전부 한국어로 교체 완료됐다: `Hud.uxml`의 `WORLD NEWS`(→세계 뉴스), `CountryStatusPanel.uxml`의
`GLOBAL STATUS CENTER`/`GLOBAL STATUS`/`INFECTED`/`DEATHS`/`CURE`/`EXTINCT`/`WORLD OVERVIEW`/
`ADVANCED DIAGNOSTICS`/`SAFE`/`WARNING`/`DANGER`/`COLLAPSE`, `RankingPanel.uxml`의
`LEADERBOARD TERMINAL`(→랭킹), `UpgradeTreeView.cs`(`CategoryEnglishCaption()` + `" LAB"`)의
`TRANSMISSION LAB`/`SYMPTOM LAB`/`ABILITY LAB`(→전파/증상/적응 연구소),
`CountryStatusPanelController.cs`(`GlobalStatusAssessment()`)의 동적 배너 4종
(`WORLD COLLAPSE IMMINENT`/`GLOBAL PANDEMIC`/`OUTBREAK EXPANDING`/`CONTAINMENT SUCCESS`).
예외로 등재하지 않고 (b) 한국어 교체를 선택했다 — 남은 영어 UI 문자열은 승인된 예외
목록(`DNA`/`DNA Point`/`DNA Mutation`)뿐이다.

---

## 12. Design Debt

Design Debt는 **버그도 Approved Exception도 아니다** — Master Design Compliance Audit 등으로
Rule과의 불일치가 발견됐지만, 원인이 명확하지 않거나(미확인) 해결 방향에 대한 판단이 아직
내려지지 않은 항목이다. 결론이 날 때까지 현재 구현을 잠정 유지하며, "위반"으로 다급하게
고치지도 않고 "해결됨"으로 취급하지도 않는다.

| 항목 | 발견 위치 | 현재 구현 | Master Design 규칙 | 충돌 원인 | 권장 해결 방향 | 우선순위 |
|---|---|---|---|---|---|---|
| **Panel Header 13px ↔ 26px** | `Tactical.uss` `.tactical-panel__title` (CountryStatusPanel status-title, RankingPanel ranking-title 소비) | `font-size: var(--font-size-xs)`(13px) | Panel Header 필수 — Panel Title Role(26px, 5절) | 미확인 — Tactical 시대의 의도적 축소 스타일인지 단순 누락인지 Compliance Audit에서도 확정하지 못함 | 실기기/에디터에서 26px로 올려 시각적 완성도를 비교 후 결정. 완성도가 떨어지면 Rule 자체를 재조정(예: Panel Header 전용 축소 Role 신설), 좋아지면 코드 마이그레이션 | 중간(공용 클래스라 파급력 크지만 시각적 판단 선행 필요) |
| **Section Header 11px ↔ 14px** | `Theme.uss` `--font-size-caption`, `Tactical.uss` `.tactical-caption`/`.modal-section-caption` (CountryStatusPanel/CountrySelect/UpgradeTree 소비) | 11px | Section Header 필수 — Caption Role(14px, 5절) | `--font-size-caption`(11px) 자체가 Master Design 8개 Typography Role 밖의 값 — Tactical 시대에 추가된 별도 캡션 체계와 Legacy 기반 Master Design Role 체계가 애초에 다른 스케일이었음 | `--font-size-caption`을 14px로 올려 Role 체계에 편입할지(파급력 큼), 이 축소 캡션 체계를 Master Design과 별개의 "UI Chrome 전용 축소 표기"로 공식 인정할지 결정 | 중간(3개 화면 동시 영향) |
| **MainMenu Detail Panel Typography** | `MainMenu.uss` `.detail-panel__title`/`.detail-panel__desc` | 22px/17px 하드코딩 | Detail Panel 필수 — Title Role(24px)/Body Role(16px, 5절) | Legacy(Boundary Analysis에서 이미 확정된 잔재) | CountrySelect Phase 1에서 이미 같은 문제를 캐스케이드 override로 해결한 방법론을 그대로 MainMenu 본체에 적용(원인·해결법 모두 확정, 실행만 남음) | 낮음(방법론 검증 완료, 실행 대기) |
| **Graph Rule 재정의 여부** | `MASTER_DESIGN.md` 5절 Graph 규칙 | Hud.uss 52px(8절 Approved Exception으로 이미 승인됨) | Base×2.125(34px) | Rule은 Legacy 실측 기반, 실제 값은 이후 가독성 개선으로 확대 — 어느 쪽을 "향후 신규 Graph의 기본값"으로 삼을지 미결정 | 지금은 Graph 소비 화면이 Hud 하나뿐이라 결정을 미룬다 — 향후 다른 화면에 Graph가 추가될 때 재검토 | 낮음(신규 화면 생기기 전까지 유예) |
| **Hero 사용 규칙 재검토** | `MASTER_DESIGN.md` 3절 Typography "Hero: 화면당 최대 1회" | EndingScreen이 2회 사용(8절 Approved Exception으로 이미 승인됨) | 화면당 최대 1회 | "최대 1회" 규칙이 "클라이맥스 화면에서 동일한 감정적 무게를 강조하기 위한 의도적 반복"이라는 정당한 패턴을 고려하지 않음 | 규칙 문구를 "화면당 최대 1회, 단 클라이맥스 화면은 예외 가능"으로 완화할지, 그대로 두고 EndingScreen만 개별 예외로 남길지 결정 — 유사한 클라이맥스 화면이 추가되면 재검토 | 낮음(현재 EndingScreen 하나만 해당) |

### Design Debt 처리 절차

1. Compliance Audit 등에서 Rule과의 불일치를 발견하면, 원인이 명확한지부터 판단한다.
2. 원인이 명확하면 8절 **Approved Exception**으로(4개 필드를 전부 채워) 등재한다 — Design Debt로
   보내지 않는다.
3. 원인이 불명확하거나 해결 방향에 대한 판단이 아직 없으면 위 표에 6개 필드(발견 위치/현재
   구현/Master Design 규칙/충돌 원인/권장 해결 방향/우선순위)를 전부 채워 등재한다.
4. Design Debt는 등재만으로 "미해결" 상태를 공식화할 뿐, 등재 자체가 코드 변경을 요구하지
   않는다 — 결론이 나기 전까지 현재 구현을 그대로 유지한다.
5. 결론이 나면 표에서 제거하고, 결과에 따라 (a) 8절 Approved Exception으로 재등재하거나
   (b) Rule 자체를 개정하거나 (c) 코드를 Rule에 맞게 마이그레이션한다 — 어느 경로든 이 표에는
   더 이상 남지 않는다.
