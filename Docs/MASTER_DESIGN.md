# Master Design Specification

> 이 문서는 Contagion Project UI의 **Constitution**이다. 신규 UI 제작, 기존 UI 리팩터링, Theme
> Token, Auto Scale, 반응형 레이아웃은 전부 이 문서를 기준으로 설계한다. Legacy 구현체(Git
> History)는 참고 자료일 뿐이며, 이 문서가 최상위 Source of Truth다.
>
> **범위**: 이 문서는 Scale·Typography·Spacing·Component 크기·Layout Grammar만 다룬다. 색상
> 체계(Color System)는 범위 밖이며 `DESIGN.md` 2절이 계속 그 권위를 가진다.

---

## 1. Philosophy

- **Atomic Grid 우선**: 모든 치수는 2px 배수다. 임의 px는 존재하지 않는다.
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
Base = 16
```

모든 Typography·Spacing·Component 치수는 Base의 배수로 정의한다.

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

| Role | Base 배수 | 크기 | 정의 |
|---|---|---|---|
| **Hero** | ×2.5 | 40px | 화면을 대표하는 단 하나의 결과 문구(승/패 타이틀 등). 화면당 최대 1회. |
| **Display** | ×2.0 | 32px | 화면의 대표 타이틀(Fullscreen 화면 최상단 제목). |
| **Title** | ×1.5 | 24px | Section/Dialog 제목. Overlay Panel·Header의 기본 제목 크기. |
| **Subtitle** | ×1.125 | 18px | 보조 제목, 리스트 행의 주요 값(라벨·이름). |
| **Body** | ×1.0(Base) | 16px | 기본 읽기 텍스트. 모든 역할의 기준점. |
| **Caption** | ×0.875 | 14px | 보조 정보, 부가 설명, 작은 라벨. |

### Panel Title 확장

Bottom Sheet Content Area(6절)처럼 화면의 대부분을 차지하는 **전체 패널의 헤더**는 Title(24px)
대신 한 단계 큰 **Panel Title(Base×1.625=26px)** 을 쓴다. Overlay Panel/Dialog급 제목(Title,
24px)과 Fullscreen급 패널 제목(Panel Title, 26px)을 구분하는 것이 목적이다.

### 밀도 전용 축소 캡션

트리 캔버스처럼 반복 요소가 매우 많아 Caption(14px)도 과한 맥락에서는 **Base×0.8125(13px)** 를
Caption의 하위 변형("Caption-Dense")으로 허용한다. 단, 이 크기는 반복 밀집 요소(노드 라벨 등)
안에서만 쓰고, 문장형 설명 텍스트에는 쓰지 않는다.

### 사용 금지

- Body(16px)보다 작은 크기를 문장형 설명 텍스트에 쓰지 않는다(Caption-Dense는 노드/칩 라벨 전용).
- 6개 역할(+Panel Title, +Caption-Dense) 밖의 임의 폰트 크기를 추가하지 않는다.
- 같은 화면 안에서 같은 역할에 서로 다른 크기를 쓰지 않는다.

### Hierarchy

```
Hero(2.5) > Display(2.0) > Panel Title(1.625) > Title(1.5) > Subtitle(1.125)
> Body(1.0) > Caption(0.875) > Caption-Dense(0.8125)
```

---

## 4. Spacing System

### Atomic Grid

```
2px (Base × 0.125)
```

### Spacing Token

| 토큰 | Base 배수 | 크기 |
|---|---|---|
| xs | ×0.25 | 4px |
| sm | ×0.5 | 8px |
| md | ×0.75 | 12px |
| lg | ×1.0 | 16px |
| xl | ×1.25 | 20px |
| xxl | ×1.5 | 24px |

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

| Tier | 역할 | Base 배수 | 크기 | 사용 위치 |
|---|---|---|---|---|
| **Tier 1 — Primary Action** | 기본 액션/내비게이션 | ×3.0 | 48px | 탭 버튼, 확인/다음 버튼, 리스트 닫기, 주요 CTA 전반 — **기본값** |
| **Tier 2 — Icon Button** | 아이콘 전용 소형 컨트롤 | ×2.25 | 36px | 다이얼로그 닫기(✕) 등 텍스트 없는 단일 아이콘 버튼 |
| **Tier 3 — Confirm/Terminal Action** | 되돌릴 수 없는 최종 결정 | ×3.5 | 56px | 재시작/부활 등 화면당 극히 드문 1회성 확정 액션 |

**정의**: 새 버튼을 만들 때는 반드시 이 3개 Tier 중 하나를 먼저 선택한다. 최소 크기는 Tier
2(36px)이며, 이보다 작은 버튼은 존재할 수 없다.

---

## 8. Exception Rules

| 예외 | 적용 규칙 |
|---|---|
| Panel Layout(Bottom Sheet 인셋) | Base 배수가 아니라 화면 목적 중심으로 독립 설계(6절). |
| Popup Close 버튼 | Button Hierarchy Tier 2(Icon Button, 36px) 적용. |
| Ending Confirm 버튼(재시작/부활) | Button Hierarchy Tier 3(Confirm/Terminal Action, 56px) 적용. |
| Panel Title | Typography Title(24px)이 아니라 3절의 Panel Title 확장(26px) 적용. |
| Caption-Dense(13px) | 반복 밀집 요소(트리 노드 라벨 등) 내부에서만 Caption의 하위 변형으로 허용. |
| Spacing 중간값(예: 10px/14px) | 2px Atomic Grid를 지키는 한 신규 토큰으로 등록해 허용(4절). |

이 문서에 등재되지 않은 예외는 **예외로 인정하지 않는다** — 새로운 예외가 필요하면 이 표에 먼저
추가한 뒤 구현한다.

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
- [ ] 5절 Component Rule(Card/Popup/Panel/Row/Progress/Graph/Icon)의 필수 항목을 만족하는가
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

### 발견된 기존 위반 후보(결정 보류)

현재 코드베이스를 조사한 결과, Tactical Console 시각 언어(DEFCON/Frostpunk 참고)의 일부로
아래 영어 대문자 캡션들이 이미 존재한다 — 이 문서의 예외 표에 아직 등재되지 않았으므로 현재는
**정책 위반 상태**다. 판단은 보류하며, 다음 중 하나로 결정해야 한다: (a) Tactical Console
미학의 의도된 요소로 보고 예외 표에 추가, (b) 한국어로 교체.

| 위치 | 문자열 |
|---|---|
| `Hud.uxml` | `WORLD NEWS` |
| `CountryStatusPanel.uxml` | `GLOBAL STATUS CENTER`, `GLOBAL STATUS`, `INFECTED`, `DEATHS`, `CURE`, `EXTINCT`, `WORLD OVERVIEW`, `ADVANCED DIAGNOSTICS`, `SAFE`, `WARNING`, `DANGER`, `COLLAPSE` |
| `RankingPanel.uxml` | `LEADERBOARD TERMINAL` |
| `UpgradeTreeView.cs`(`CategoryEnglishCaption()` + `" LAB"`) | `TRANSMISSION LAB`, `SYMPTOM LAB`, `ABILITY LAB` |

이 목록은 이번 문서화 작업 중 발견된 것으로, **코드/UXML은 이번 작업에서 수정하지 않았다.**
