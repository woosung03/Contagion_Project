# Contagion Project — Color System v2 분석·설계 보고서

> 상태: **분석/설계 초안 — 코드·DESIGN.md 미수정.** 이 문서 자체가 검토용 산출물이다.
> 근거: `Docs/DESIGN.md`(현행), `Assets/UI/Theme.uss`, `Assets/UI/Tactical.uss`,
> `Assets/UI/Hud.uss`, `Assets/UI/UpgradeTree.uss`, `Assets/UI/MainMenu.uss`,
> `Assets/UI/CountryPopup.uss`, `Assets/UI/ResearchPopup.uss`, `Assets/UI/CountryStatusPanel.uss`,
> `Assets/UI/RankingPanel.uss`, `Assets/UI/EndingScreen.uss` 실제 코드 전수 확인.

---

## 0. 결론 요약

DESIGN.md의 Surface 계층·Severity·Brand·Accent Glow 구조는 실패하지 않았다 — 실제 코드가
그 구조를 대체로 잘 따르고 있다. 문제는 **Button System이 아예 존재하지 않는다**는 것,
그리고 그 결과로 **Accent 색(`accent-glow`)이 "구조용 테두리"에만 갇혀 있고 정작 사용자
행동 유도(버튼)에는 쓰이지 못하고 있다**는 것이다. 이번 v2는 새 색을 추가하지 않고,
이미 존재하지만 죽어 있던 토큰 2개(`--color-button-secondary`, MainMenu.uss의 하드코딩
텍스트색)를 되살려 Button System을 완성한다.

---

## 1. 현재 색상 체계가 70:20:10을 얼마나 만족하는가

70:20:10 정의(이번 브리핑 기준): 70% Base(캔버스), 20% Secondary(패널/카드), 10% Accent(버튼/선택/행동유도).

### 1-1. Base(70%) — 양호

`--color-bg-root`(rgba(8,8,16,.97)), `--color-bg-panel-alt`(전체화면 패널), `--color-bg-scrim-soft/strong`
(HUD 상하단 바)이 화면의 절대다수 면적을 차지한다. MainMenu/CountrySelect/UpgradeTree 등
풀스크린 화면은 전부 이 축 위에 그려진다. **의도대로 작동 중.**

### 1-2. Secondary(20%) — 양호

`--color-bg-panel`/`--color-bg-panel-strong`(팝업/도크), `--color-surface`/`--color-surface-soft`
(카드·행)가 Base 위에 얹힌 패널·카드 면적을 담당한다. `pathogen-card`, `country-row`,
`tree-node`, `status-row`, `CountryPopup`/`RankingPanel`/`ResearchPopup` 전부 이 축을
정확히 쓴다. **의도대로 작동 중** — 사용자가 "회색 패널/회색 카드"라고 느낀 부분은
사실 실패가 아니라 이 축이 정확히 설계된 대로(짙은 네이비 계단) 작동한 결과다.

### 1-3. Accent(10%) — 사실상 미달, "행동 유도"에 전혀 안 쓰임

`--color-accent-glow`(민트그린)는 DESIGN.md 8번 항목에서 **"구조 전용 색 — 콘텐츠 색이
아니다"**로 명시적으로 못 박혀 있다. 실제 쓰임도 그렇다: `tactical-panel` 테두리 1px,
`corner-cut` 13×2px 틱, `tactical-caption`/`data-label` 텍스트 — 전부 **선(hairline)과
캡션**이다. 픽셀 면적으로 환산하면 화면 전체의 1~2% 수준으로, "10%"에 크게 못 미친다.

더 중요한 문제: 이 브리핑이 정의한 Accent의 역할("버튼, 선택 상태, 사용자 행동 유도")을
수행하는 색이 **현재 하나도 없다.** 코드 전수 조사 결과:

- 진짜 "선택 상태" 강조는 `--color-brand-gold`(선택 카드 테두리)가 담당 — 이건 Accent
  축이 아니라 Brand 축의 책임 분담이라 원래 설계 의도상 맞다.
- "버튼"은 아래 2절에서 보듯 스타일이 아예 없거나(Unity 기본), 있어도 accent-glow를
  1px 테두리로만 쓰고 배경은 Secondary 색(`bg-panel`)을 그대로 쓴다.

**결론**: 70/20은 만족하지만, 10%의 "역할"이 잘못 배정되어 있다. accent-glow는
장식(테두리/코너컷)에 예산을 다 쓰고, 정작 사용자가 "여기를 눌러야 한다"고 인지해야
할 버튼에는 단 한 번도 채움(fill)으로 등장하지 않는다.

---

## 2. 현재 버튼이 왜 배경에 묻히는가 — 두 가지 서로 다른 실패

코드 전수 조사 결과, 문제는 하나가 아니라 **두 가지 독립된 실패 유형**이다.

### 유형 A — 스타일이 아예 없어서 Unity 기본 버튼이 노출됨

`.popup-close`(`CountryPopup.uss`/`ResearchPopup.uss`에 동일 코드 중복), `.status-close-btn`
(`CountryStatusPanel.uss`), `.mainmenu-back`(`MainMenu.uss`) — 셋 다 코드가 다음과 같다:

```css
.popup-close {
    width: 28px;
    height: 28px;
    padding: 0;
    flex-shrink: 0;
}
```

`background-color`/`border-color`/`color` 어느 것도 지정되어 있지 않다. `Theme.uss`의
전역 `Button` 규칙(`Button:active`의 scale, `Button:disabled`의 opacity)만 상속받고,
그 외에는 UI Toolkit 런타임 기본 버튼 스킨(밝은 회색 라운드 사각형)이 그대로 노출된다.
이건 **다크 Tactical 캔버스 위에서 튀는 것**이지 "묻히는" 것과는 결이 다르다 — 다만
팔레트 어디에도 없는 4번째 회색(엔진 기본값)이 끼어들면서 "이 화면이 미완성/디버그
상태처럼 보인다"는 인상을 준다. `EndingScreen.uss`에는 정확히 이 버그를 고친 이력이
코드 주석으로 남아 있다("버그 리포트: 기존엔 width/height/margin만 지정하고 배경·테두리·
폰트가 없어 Unity 기본 런타임 버튼(둥근 회색)이 그대로 노출돼...") — 즉 팀은 이미 한
번 이 문제를 발견하고 `.ending-button`은 고쳤지만, 같은 패턴이 다른 4곳에는 아직
반영되지 않은 상태다.

### 유형 B — 스타일은 있는데 배경색이 패널과 동일해서 "그냥 패널의 일부"처럼 보임

`.ending-button`, `.popup-footer-button`(`RankingPanel.uss`/`ResearchPopup.uss` 각각 중복
정의), `.tab-button`(`Hud.uss`)은 이미 "고쳐졌다"고 되어 있지만 실제로는:

```css
.popup-footer-button {
    border-width: 1px;
    border-color: var(--color-accent-glow);
    border-radius: 0;
    background-color: var(--color-bg-panel);   /* ← 자신이 놓인 팝업의 배경색과 완전히 동일 */
    ...
}
```

버튼의 `background-color`가 `--color-bg-panel`인데, 이 버튼이 놓이는 `RankingPanel.uss`/
`ResearchPopup.uss`의 루트 컨테이너(`.ranking-root`, `.popup-root`) 배경도 정확히
`--color-bg-panel`이다. 즉 버튼과 그것을 담은 패널이 **완전히 같은 색**이고, 차이는
1px 테두리 하나뿐이다. 모바일 화면에서 1px 테두리는 시야 거리상 거의 인지되지 않아
"버튼이 패널에 녹아든" 것처럼 보인다 — 이게 사용자가 말한 "배경에 묻힌다"의 정확한
원인이다. `Theme.uss`에 이미 `--color-button-secondary: rgb(70, 70, 90)`라는 버튼
전용 채움색 토큰이 정의돼 있지만, `Hud.uss`의 `.tab-button--secondary` 딱 한 곳에서만
쓰이고 나머지는 전부 `--color-bg-panel`을 재사용해 버려 이 토큰의 존재 의미가 무색해졌다.

### 근본 원인 (공통)

두 유형 모두 같은 뿌리에서 나온다 — **DESIGN.md에 Button 컴포넌트 규칙 자체가 없다.**
`Tactical.uss`(공용 스타일시트, DESIGN.md가 source_of_truth로 지정한 파일)를 전수
확인한 결과 Button 관련 규칙은 0줄이다. `tactical-panel`/`corner-cut`/`data-row` 같은
공용 컴포넌트는 전부 여기 있는데 버튼만 없다. 그 결과 화면마다(팝업 6개, 메뉴 2개,
HUD 1개) 버튼을 매번 새로 만들거나(중복 코드), 아예 잊어버리는(유형 A) 일이 반복됐다.

---

## 3. Plague Inc / DEFCON / Frostpunk 대비 부족한 점

세 게임 모두 어두운 UI를 쓰지만 공통적으로 지키는 원칙이 하나 있다: **행동 가능한
요소(버튼, 선택된 유닛, CTA)는 항상 "칠해진 면(solid fill)"으로 표현되고, 장식/구조
요소는 선(line)으로 표현된다.** 즉 "칠 vs 선"으로 상호작용 가능 여부를 구분한다.

- **Plague Inc**: 업그레이드/치료제 버튼은 채도 높은 단색 채움 사각형(초록 계열
  "구매 가능", 진행 바는 꽉 찬 색). 테두리만 있는 버튼이 없다 — 액션은 항상 면.
- **DEFCON**: 단색 초록 와이어프레임이 기본이지만, 선택 가능한 유닛/발사 컨트롤처럼
  실제로 누를 수 있는 요소는 밝기·채도가 확 뛰는 채움 상태로 전환된다. 장식 격자선은
  항상 어둡고 흐린 초록, 행동 요소만 밝은 초록 — 밝기 대비가 극단적으로 크다.
- **Frostpunk**: 어두운 UI 위에 건설/코어 액션 버튼만 따뜻한 호박색(amber) 단색
  채움으로 확실히 튀게 만든다. 나머지 패널·아이콘은 전부 무채색.

현재 Contagion Project의 Tactical UI는 이 원칙의 절반만 구현되어 있다 — "장식은 선"은
정확히 지켜지고 있지만(`accent-glow` 헤어라인, `corner-cut`), "행동은 면"이 없다.
accent-glow 예산이 전부 장식(선)에 쓰이고 정작 버튼(면)에는 한 번도 배정되지 않은 게
바로 이 차이다. 세 게임 모두 "10%"에 해당하는 가장 밝고 채도 높은 색을 오직 **누를 수
있는 것**에만 쓰고 그 외에는 절대 쓰지 않는다 — 이 프로젝트는 반대로 그 색을 누를 수
없는 구조물(테두리)에 쓰고 있다.

---

## 4. Color System v2 — 산출물

### 설계 원칙

새 색상을 추가하지 않는다. 기존 토큰 중 **이미 정의돼 있지만 죽어있던 2개**를 활성화한다:

1. `--color-button-secondary: rgb(70, 70, 90)` — `Theme.uss`에 이미 존재, `Hud.uss`
   `.tab-button--secondary` 1곳에서만 사용 중. 이걸 Secondary Button의 표준 채움색으로
   승격한다.
2. `rgb(10, 30, 15)` — `MainMenu.uss`의 `.mainmenu-next`에 하드코딩된 "accent 배경 위
   텍스트용 어두운 색". 토큰명이 없을 뿐 이미 실전에서 검증된 값이다. 이걸
   `--color-text-on-accent`로 명명해 Theme.uss에 승격한다(신규 색상이 아니라 기존
   하드코딩값의 토큰화).

### Background / Surface / Border / Text / Info — 변경 없음

기존 DESIGN.md 1~3번 섹션(Canvas & Panel, Surface & Border, Text) 그대로 유지. 이미
70:20에 부합하므로 손대지 않는다.

### Accent / Warning / Danger — 역할 재정의(값은 그대로, "용도"만 확장)

| 토큰 | 값 | 기존 역할 | v2 추가 역할 |
|---|---|---|---|
| `--color-accent-glow` | rgb(150,255,185) | 구조(테두리/코너컷/캡션) | **Primary Button 채움** |
| `--color-accent-glow-soft` | rgba(150,255,185,.35) | 구조(soft 테두리) | Primary Button `:active` 채움 |
| `--color-button-secondary` | rgb(70,70,90) | (거의 미사용) | **Secondary Button 채움** — 신규 활성화 |
| `--color-status-dead` | rgb(220,90,90) | 국가 데이터(사망) | **Danger Button 채움** (파괴적 액션 전용, 국가 데이터 문맥 밖에서 재사용) |
| `--color-status-danger` | rgb(255,140,120) | 국가 데이터(위험) | Danger Button `:active` 채움 |
| `--color-text-on-accent`(신규 토큰명) | rgb(10,30,15) | (MainMenu.uss 하드코딩) | Primary Button 텍스트 |

> `--color-status-dead`/`--color-status-danger`를 버튼에 재사용하는 것은 DESIGN.md
> "Severity Colors Don't" 규칙("국가 데이터 전용, UI 크롬에 쓰지 않는다")과 부딪힐 수
> 있다 — 이 충돌은 4-3절에서 별도로 명시적으로 처리한다(무단 재해석 금지, 문서상
> 예외 조항으로 명문화 필요).

### 4-1. 70:20:10 v2 적용안

배분 자체(70/20/10)는 변경하지 않는다. **10% 안에서의 배분 방식만 바꾼다**:

- 기존: Accent 10% = 구조(테두리/코너컷/캡션) 100%, 행동유도 0%
- v2: Accent 10% = 구조(테두리/코너컷/캡션) 약 70% + 행동유도(버튼 채움) 약 30%

즉 accent-glow가 "선"으로 쓰이던 총량은 그대로 유지하되, 화면당 1~2개뿐인 Primary
Button(확인/다음/연구 시작 등 확정 액션)에 한해 accent-glow를 "면"으로도 쓴다. 코너컷·
헤어라인 사용은 그대로라 전체 밀도는 유지되면서, 정작 "눌러야 하는 곳"에서만 이 색이
극적으로 진해지는 대비가 생긴다 — DEFCON/Frostpunk가 쓰는 방식과 동일한 전략이다.

### 4-2. Button System 설계

3가지 변형 × 4가지 상태.

**Primary Button** — 확정/전진 액션 전용(예: "다음", "확인", "연구 시작", "복귀"). 화면당
보통 1개, 많아야 2개.

| 상태 | 배경 | 테두리 | 텍스트 | 비고 |
|---|---|---|---|---|
| Default | `--color-accent-glow`(솔리드) | 없음(또는 동색 1px) | `--color-text-on-accent` | `radius: 0` |
| Hover | (미정의) | — | — | 터치 우선이라 의도적으로 생략, 5-1절 참고 |
| Pressed(`:active`) | `--color-accent-glow-soft` | — | — | `scale: 0.96` 병기 |
| Disabled | 위 Default에 `opacity: 0.5` | — | — | 전역 규칙 상속 |

**Secondary Button** — 닫기/취소/뒤로가기 등 비확정 액션. 아이콘형 소형 버튼 포함.

| 상태 | 배경 | 테두리 | 텍스트 |
|---|---|---|---|
| Default | `--color-button-secondary` | 1px `--color-accent-glow-soft` | `--color-text-primary` |
| Hover | (미정의) | — | — |
| Pressed | `--color-surface-hover`(기존 토큰 재사용) | — | — |
| Disabled | 위 Default에 `opacity: 0.5` | — | — |

**Danger Button** — 파괴적/비가역 액션 전용(게임 재시작 확정, 저장 삭제 등 — 현재
코드베이스에는 아직 인스턴스가 없으나 향후를 위해 지금 정의).

| 상태 | 배경 | 테두리 | 텍스트 |
|---|---|---|---|
| Default | `--color-status-dead` | 없음 | `--color-text-primary` |
| Hover | (미정의) | — | — |
| Pressed | `--color-status-danger` | — | — |
| Disabled | 위 Default에 `opacity: 0.5` | — | — |

### 4-3. Severity 색 재사용에 대한 예외 처리 (중요)

Danger Button이 `--color-status-dead`를 쓰는 것은 기존 DESIGN.md 규칙과 충돌 소지가
있다. 이를 무단으로 넘기지 않고, v2에서는 **Status Semantics 표에 5번째 축을 몰래
추가하는 대신, 기존 규칙 문구 자체를 명시적으로 개정**해야 한다:

> 기존: "Severity 색상을 노드 상태에 재사용하지 않는다... 노드 상태는 '플레이어
> 진행도' 축으로 의미가 완전히 다르다."
>
> v2 개정안: "Severity 색상을 노드 상태·국가 데이터 외 문맥에 재사용하지 않는다.
> **단, Danger Button은 예외로 한다** — '위험/파괴적 행동'이라는 의미가 국가 데이터의
> '위험/사망' 의미와 실제로 합치하므로, 색상 축이 아니라 의미 축이 일치하는 유일한
> 교차 사용 허용 사례로 명문화한다."

이 예외를 문서화하지 않고 코드만 고치면 다음 감사에서 다시 "규칙 위반"으로 지적될
것이므로, 실제 구현 전에 DESIGN.md 개정과 함께 반드시 명문화되어야 한다.

---

## 5. DESIGN.md 수정안 — 섹션별 구체 반영 위치

코드/DESIGN.md는 아직 건드리지 않는다는 전제하에, 승인 시 반영할 위치만 명시한다.

### 5-1. Interaction Rules 섹션 (현재 1줄짜리 "버튼" 규칙 교체)

현재:
> **버튼**: `:active` 시 `scale: 0.96 0.96`만 적용, 배경색은 절대 건드리지 않는다.

이 문장은 **개정이 필요**하다 — v2 Button System은 `:active`에서 배경색을 명시적으로
바꾸기 때문이다(Primary/Secondary/Danger 각각 다른 pressed 배경). 개정안:

> **버튼**: 모든 `Button` 엘리먼트는 Button System(Primary/Secondary/Danger) 중 하나를
> 명시적으로 적용해야 한다. `:active` 시 `scale: 0.96 0.96`은 변형과 무관하게 공통
> 적용되며, 추가로 각 변형이 정의한 pressed 배경색(4-2절)으로 전환된다. `:disabled`는
> `opacity: 0.5`. 별도 `:hover`는 정의하지 않는다 — 터치 우선 기기(갤럭시 S25 울트라)
> 대상이라 지속되는 마우스 hover 이벤트가 없고, 탭 후 hover가 남는 모바일 특유의
> "stuck hover" 버그를 피하기 위함이다. 이 hover 생략 규칙은 `Button`에만 적용되며,
> 선택 가능 카드/행(`pathogen-card` 등, 아래 항목)은 기존대로 `:hover`를 유지한다
> (에디터 마우스 테스트·향후 태블릿 대응 목적).

### 5-2. Color System 섹션 — 새 하위 항목 "### 9. Button System" 추가

8번(구조용 발광색) 다음에 추가. 내용은 본 문서 4-2절 표 그대로.

### 5-3. YAML frontmatter `colors:` 블록에 2개 추가

```yaml
  button-secondary: "rgb(70, 70, 90)"       # --color-button-secondary (기존 토큰, Button System으로 승격)
  text-on-accent: "rgb(10, 30, 15)"          # --color-text-on-accent (신규 토큰명 — MainMenu.uss 하드코딩값 승격)
```

### 5-4. YAML frontmatter `components:` 블록에 3개 추가

```yaml
  button-primary: { bg: accent-glow, text: text-on-accent, pressed-bg: accent-glow-soft }
  button-secondary: { bg: button-secondary, border: accent-glow-soft, pressed-bg: surface-hover }
  button-danger: { bg: status-dead, pressed-bg: status-danger }
```

### 5-5. Component Library 섹션

`tab-button / tab-button--secondary` 항목을 Button System 참조로 재작성 — 현재
`Hud.uss`와 `UpgradeTree.uss`에 이름은 같고 계약이 다른 두 `.tab-button`이 존재한다는
사실을 문서에 명시하고(중복/충돌 위험 고지), 승격 시 어느 쪽을 Secondary Button 기반으로
통합할지 결정 필요.

### 5-6. Status Semantics 표

"재정의 금지 범위" 열에 Danger Button 예외를 각주로 추가(4-3절 문구 그대로).

### 5-7. Do/Don't 섹션

Do에 추가: "모든 Button 엘리먼트는 Primary/Secondary/Danger 중 하나를 명시 적용한다 —
스타일 없는 기본 Button을 화면에 노출하지 않는다."

Don't에 추가: "버튼 배경에 `--color-bg-panel`/`--color-surface`(패널·카드 색)를 재사용하지
않는다 — 버튼은 반드시 `--color-button-secondary` 이상의 채도로 패널과 구분한다."

---

## 6. 실제 구현 우선순위

노출 빈도·버그 성격을 기준으로 4단계 + 후속 정리 1단계.

**1순위 — 유형 A 버그 수정 (Secondary Button 적용)**
`popup-close`(CountryPopup.uss + ResearchPopup.uss 2곳), `status-close-btn`
(CountryStatusPanel.uss), `mainmenu-back`(MainMenu.uss). 이유: Unity 기본 회색이 실제로
노출되는 유일한 4개 인스턴스이자, 팝업/메인메뉴 진입 시마다 매번 보이는 최다 접점 요소.
가장 저비용·최고효과.

**2순위 — HUD 버튼 (Primary/Secondary 구분 적용)**
`Hud.uss`의 `.tab-button`/`.tab-button--secondary`(하단 action-strip). 이유: GamePlay
화면 전체에서 상시 노출되는 유일한 네비게이션 — 현재는 테두리 색만 있고 채움 차등이
없어 어떤 탭이 활성 상태인지 시각적으로 구분되지 않는다.

**3순위 — Research Database 버튼**
`UpgradeTree.uss`의 `.tab-button`/`.tab-button--active`(브랜치 탭, HUD와 이름 충돌),
`ResearchPopup.uss`의 `.popup-footer-button--confirm`("연구 시작"을 Primary Button으로
승격 — 현재는 `accent-glow-soft` 배경만 얹은 준-Primary 상태).

**4순위 — Country Database 대륙 헤더**
`CountryStatusPanel.uss`의 `.continent-header`. 순수 버튼은 아니지만 상호작용 행이며,
현재 `:hover`만 있고 `:active`(누르는 순간의 피드백)가 전혀 없다 — 아코디언 토글임을
더 명확히 하기 위해 Pressed 상태 추가.

**5순위(선택, 정리 작업) — 중복 정의 통합**
`popup-close`가 2개 파일에 완전히 동일한 코드로 중복 존재, `tab-button`이 2개 파일에
이름은 같고 계약이 다르게 존재. Button System을 `Tactical.uss`(공용)로 승격하면서 이
중복을 함께 해소할 수 있다(선택 사항, 급하지 않음).

---

## 7. 다음 단계 (승인 필요 사항)

이 문서는 분석·설계까지만 다룬다. 실제 반영을 진행하려면 다음을 확인받아야 한다:

1. 4-3절의 "Danger Button의 Severity 색 재사용 예외" 문구를 그대로 DESIGN.md에 넣을지,
   아니면 다른 표현으로 조정할지.
2. 5-1절의 Interaction Rules 문장 개정("배경색은 절대 건드리지 않는다" → 변형별 예외 허용)에
   동의하는지 — 기존 규칙을 깨는 변경이라 별도 확인이 필요하다.
3. 6절 우선순위 1~4순위 순서에 이견이 없는지.

승인되면 (1) DESIGN.md 개정 → (2) `Tactical.uss`에 Button System 구현 → (3) 1~4순위
화면 코드 적용 순으로 진행한다.
