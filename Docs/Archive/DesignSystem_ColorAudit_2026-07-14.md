# Contagion Project 디자인 시스템 색상 감사 보고서

- 작성일: 2026-07-14
- 대상: `Docs/DESIGN.md`(Source of Truth) + 실제 구현(Assets/UI/*.uss)
- 범위: HUD, Research Database(UpgradeTree/ResearchPopup), Country Status(CountryStatusPanel/CountryPopup), Main Menu(MainMenu/CountrySelect)
- 성격: **분석 전용 보고서**. 코드·USS·문서(DESIGN.md 등)는 일체 수정하지 않았다.

---

## 1. DESIGN.md 색상 시스템 정의 현황

DESIGN.md는 색상을 4개의 독립된 "축"으로 분리해 정의한다. 사용자가 요청한 7개 카테고리를
DESIGN.md의 실제 구조에 매핑하면 다음과 같다.

| 요청 카테고리 | DESIGN.md 대응 | 정의 여부 |
|---|---|---|
| Primary | 없음(별도 "Primary" 토큰 부재) | **미정의** |
| Secondary | `--color-button-secondary: rgb(70,70,90)` | Theme.uss에만 존재, **DESIGN.md 색상표에는 없음** |
| Accent | `--color-accent-glow` / `--color-accent-glow-soft`(민트, 구조 전용) + 브랜드 3색(DNA초록/골드/프리미엄퍼플) | 정의됨(단, "장식용 구조색"과 "콘텐츠 강조색"이 다른 토큰으로 분리) |
| Warning | `--color-status-danger`(rgb 255,140,120) — "위험" 의미로 warning 역할 겸함 | 정의됨(단, "국가 데이터 전용"으로 스코프 제한) |
| Danger | `--color-status-dead`(rgb 220,90,90) | 정의됨(위와 동일 스코프 제한) |
| Background | `--color-bg-root/panel/panel-strong/panel-alt/scrim-*` 6종 | 정의됨(표면 계단 체계) |
| Panel | `--color-surface` / `--color-surface-soft` / `--color-border` | 정의됨 |

핵심은 **DESIGN.md에 "Button"이라는 컴포넌트 색상 규칙 자체가 사실상 없다**는 점이다.
문서 전체에서 "button"이 언급되는 곳은 Component Library의 `tab-button / tab-button--secondary`
한 줄뿐이며, Interaction Rules는 버튼의 `:active`(스케일 축소)와 `:disabled`(투명도)만 규정한다.
버튼의 **배경색·hover색·pressed색**을 규정하는 조항은 문서 어디에도 없다. 반면 "선택 가능한
카드/행"의 hover 트랜지션은 명확히 규정돼 있다(`.pathogen-card`, `.country-row`, `.tree-node`
등). 즉 **카드형 컴포넌트는 색상 규칙이 있지만, 버튼형 컴포넌트는 규칙이 비어 있다.**

또한 `--color-button-secondary`는 Theme.uss `:root`에는 선언돼 있지만 DESIGN.md 색상 표
1~8절 어디에도 등장하지 않는 **고아 토큰(orphan token)**이다. 실제로 이 토큰을 쓰는 곳은
HUD 하단 "랭킹" 보조 버튼(`.tab-button--secondary`) 단 한 곳이다.

---

## 2. 실제 UI 사용 현황 (4개 화면 기준)

Theme.uss/Tactical.uss를 제외한 8개 화면 전용 USS 파일(Hud/MainMenu/CountryStatusPanel/
UpgradeTree/CountryPopup/CountrySelect/RankingPanel/ResearchPopup/EndingScreen)에서 토큰
사용 빈도를 집계했다.

| 토큰 그룹 | 사용 횟수 | 비고 |
|---|---|---|
| `accent-glow` + `accent-glow-soft` (구조용 발광 민트) | **55회** | 거의 전 패널의 테두리/코너컷/캡션 — 사실상 화면 전체를 관통하는 유일한 "칼라"지만 콘텐츠에는 못 쓴다는 규칙(4번 축 분리) 때문에 늘 얇은 테두리 1px로만 등장 |
| `bg-panel/bg-scrim` 계열(무채색 배경) | 17회 | 팝업·바 배경 |
| `surface/surface-soft` (반투명 흰색 카드 배경) | 19회 | 8%/6% 알파 — 거의 안 보이는 수준 |
| `border`(중립 헤어라인, 15% 흰색) | 11회 | 비선택 카드 기본 테두리 |
| `text-secondary/text-tertiary`(회색조 텍스트) | 28회 | 라벨/서브텍스트 |
| `grid-line`(옅은 민트 구분선) | 12회 | data-row 하단선 |
| **무채색·구조색 소계** | **약 142회** | |
| `status-infected/dead/danger/info`(4색, 국가 데이터 전용) | 42회 | Country Status/Popup에만 집중 |
| `brand-dna/gold/premium`(3색, 브랜드 강조) | 20회 | 주 CTA·선택 표시·프리미엄 버튼에 한정 |
| **유채색(콘텐츠 축) 소계** | **약 62회** | |

무채색·구조색 대 유채색 비율은 약 **7:3**이다. 게다가 "구조색"인 accent-glow조차 단일
민트 색조 하나뿐이라, 화면을 스크린샷으로 보면 사실상 "짙은 회색·검정 배경 + 얇은 민트
테두리 + 흰색/회색 텍스트"가 대부분이고, 실제 유채색(초록/금색/보라/주황/빨강/파랑)은
버튼 1~2개, 상태 뱃지 몇 개에만 국지적으로 등장한다.

화면별로 보면:

- **HUD**: resource-strip/action-strip 배경은 전부 `bg-scrim`(검정 반투명), 유채색은
  DNA 카운터 텍스트(`brand-gold`)와 감염/사망 그래프 선(status 4색)뿐. 하단 액션 바
  버튼 2개 중 1개(`.tab-button--secondary`)는 미문서화 회청색(`rgb(70,70,90)`) 사용.
- **Research Database**(UpgradeTree/ResearchPopup): 브랜치 보드·연구 리스트 대부분이
  `surface`(흰 8%) + `border`(흰 15%) 무채색 카드다. 상태별 색(잠김/가능/활성/완료)이
  존재하긴 하나 "가능" 상태는 `accent-glow-soft`(35% 알파 민트) 테두리만 있어 육안 구분이
  약하다. `ResearchPopup.popup-close`(닫기 ✕ 버튼)는 **배경색·테두리색 선언이 아예 없다.**
- **Country Status**: `CountryStatusPanel.status-close-btn`도 배경·테두리 선언이 없다.
  나머지는 severity 4색이 가장 활발히 쓰이는 화면이라 상대적으로 색이 있는 편.
- **Main Menu**: `CountrySelect.mainmenu-back`(뒤로 버튼)도 배경·테두리 선언이 없다.
  선택된 병원체/국가 카드는 `border-selected`(금색)로 확실히 구분되지만, 비선택 카드는
  `surface`(8%)+`border`(15%)로 배경과 거의 분간이 안 된다.

**중요 발견**: `.popup-close`(CountryPopup·ResearchPopup 공용 ✕ 버튼), `.status-close-btn`
(CountryStatusPanel ✕ 버튼), `.mainmenu-back`(CountrySelect 뒤로 버튼) — 이 4개 버튼은
`width/height/padding`만 지정돼 있고 배경색·테두리색이 전혀 없다. 즉 **Unity UI Toolkit의
기본 런타임 버튼 스타일(둥근 회색 버튼)이 그대로 노출된다.** 흥미롭게도 `EndingScreen.uss`
76번 줄 주석에 이미 이 정확한 버그가 기록돼 있다:

> "기존엔 width/height/margin만 지정하고 배경·테두리·폰트가 없어 Unity 기본 런타임
> 버튼(둥근 회색)이 그대로 노출돼 Tactical Design System과 어긋났다."

`ending-button`은 이 버그를 발견해 고쳤지만, **같은 버그가 4곳에 그대로 남아 있다** —
그것도 하필 사용자가 지목한 Research Database와 Country Status 화면의 팝업 닫기 버튼,
Main Menu 계열의 뒤로 버튼이다. 사용자가 모든 팝업/화면 전환에서 가장 먼저·가장 자주
누르는 버튼이 정확히 이 4개라는 점에서 체감 영향이 크다.

---

## 3. 문제점 분석

**회색 계열 과다 여부 — 그렇다.** 무채색·구조색:유채색 비율이 약 7:3이며, 이는 설계
의도(축 4 분리, 브랜드 3색 고정, severity는 국가 데이터 전용)가 정확히 지켜진 결과이기도
하다. 문제는 그 설계가 "UI 크롬(패널/버튼/헤더)에는 회색과 얇은 민트 테두리만 허용한다"는
규칙과 사실상 동치라는 점 — 국가 데이터 화면이 아닌 한 화면 전체가 무채색으로 수렴한다.

**버튼이 배경과 충분히 구분되는가 — 아니다, 두 가지 이유로.**
1. 문서화된 버튼(`tab-button`, `mainmenu-next`, `ending-button`, `popup-footer-button`)조차
   상당수가 "거의 검은 배경(`bg-panel`) + 1px 얇은 테두리"라는 tactical-panel과 동일한
   시각 언어를 쓴다. 패널과 버튼이 같은 룩을 공유해 "이게 눌리는 요소"라는 신호가 약하다.
2. 미문서화 버튼(닫기 ✕ 4종)은 아예 다른 룩(Unity 기본 회색 버튼)이라 반대로 이질적이다.
   결과적으로 화면마다 버튼이 "거의 안 보이거나" 또는 "튀는 회색 이물질"이거나 둘 중
   하나가 되어 일관성이 없다.

**선택 상태 대비 — 스펙상으로는 준수하나 기준선이 너무 낮다.** 선택 시 금색
(`border-selected`, 3px)로 바뀌는 규칙 자체는 명확하고 실제로도 잘 지켜지고 있다
(MainMenu pathogen-card/country-row, UpgradeTree tree-node--active/--maxed 모두 확인됨).
다만 **비선택 기준선**(`border`: 흰색 15% 알파, `surface`: 흰색 8% 알파)이 거의 인지
불가능한 수준이라, "선택 대 비선택"이 아니라 "선택된 것 대 배경에 녹아든 것"으로 보인다.
대비가 약하게 느껴지는 원인은 선택색이 약해서가 아니라 **비선택 카드가 카드로도 잘 안
보이기 때문**이다.

**Tactical UI 느낌 vs 산업용 프로그램 느낌.** DEFCON/Frostpunk류 tactical HUD의 핵심은
"어두운 배경 + 발광 헤어라인"이지만, 그 시스템들은 보통 하나의 발광색만 쓰지 않고
상태별로 색이 계속 바뀌며 강한 포화도의 포인트가 화면 곳곳에 능동적으로 등장한다. 이
프로젝트는 accent-glow 민트 하나가 거의 모든 테두리를 담당하고, 실제 강조는 3개 브랜드
색으로 극히 제한돼 있어 — 시각적으로는 "짙은 회색 캔버스에 옅은 선 하나"라는 단조로운
패턴이 반복된다. 여기에 위 2절의 미스타일드 버튼(Unity 기본 회색)까지 섞이면 "군사
콘솔"보다는 "설정값이 비어 있는 산업 소프트웨어"처럼 읽히는 것이 자연스럽다.

**Plague Inc 스타일과 비교.** Plague Inc는 어두운 배경은 공유하지만 UI 크롬 자체에
포화도 높은 색(빨강/주황/청록 등)을 적극적으로 쓰고, 버튼은 항상 배경과 뚜렷이 다른
채도 높은 필드 컬러를 가진다. 이 프로젝트는 반대로 "콘텐츠 색은 국가 데이터에만,
UI 크롬은 무채색+구조색만"이라는 정반대 원칙을 세워뒀다 — 원칙 자체는 일관되고
의도적이지만, 그 결과 버튼류의 "눌러야 할 것 같은 느낌"이 Plague Inc 대비 약하다.

---

## 4. DESIGN.md 적합성 평가

색상 규칙 자체(표면 계단 5단계, 4개 축 분리, 브랜드 3색 고정, severity 4색 스코프 제한)는
**설계적으로 잘못되지 않았다.** 오히려 "색을 남발하지 않는다"는 절제된 원칙은 tactical UI
장르에서 흔한 접근이고, 실제로 country-data 화면(Country Status)에서는 이 원칙이 잘
작동하고 있다.

문제는 두 갈래로 나뉜다.

1. **문서 공백 (일부 규칙 추가 필요)** — Button 컴포넌트에 대한 색상 규칙이 DESIGN.md에
   존재하지 않는다. Component Library에 `tab-button` 한 줄만 있을 뿐, "버튼 기본 배경/
   버튼 보조 배경/버튼 hover/버튼 pressed"를 규정하는 조항이 없다. `--color-button-secondary`
   토큰도 문서화 없이 Theme.uss에만 존재한다. 이는 규칙이 "틀렸다"기보다 **애초에 안
   쓰였다**는 뜻이다.
2. **구현 이탈 (구현 수정 필요)** — 닫기 버튼 4종이 어떤 토큰도 참조하지 않고 Unity 기본
   스타일로 방치돼 있다. 이것은 DESIGN.md 규칙을 어긴 게 아니라 **규칙을 아예 적용하지
   않은 누락**이며, EndingScreen에서 이미 한 번 발견·수정된 동일 버그의 재발이다.

즉 "색상 규칙이 잘못 설계됨"은 해당하지 않고, "색상 규칙은 정상인데 구현이 어긋남"과
"일부 규칙만 수정(정확히는 **추가**) 필요"가 동시에 해당한다.

---

## 5. 개선안 — Tactical UI 관점의 색상 체계 재설계

단순 버튼 색 교체가 아니라, **"버튼"을 DESIGN.md의 5번째 정식 컴포넌트 축으로 승격**하는
방향을 제안한다. 기존 4개 색상 축(브랜드/severity/노드 카테고리/구조 발광색)은 그대로
유지하고, 그 위에 "인터랙션 상태색" 레이어를 얹는 방식이다. 새 색상을 무한정 늘리지
않고 **기존 토큰을 재조합**하는 데 초점을 맞췄다(DESIGN.md의 "3색 고정" 철학 존중).

| 상태 | 제안 값 | 근거 |
|---|---|---|
| **Accent(추천)** | `--color-accent-glow` 유지, 단 "구조 전용" 제한을 풀어 **주 버튼의 배경 채움색**으로도 승인 — `rgba(150,255,185,0.20)` | 이미 화면 전체에 55회 등장하는 유일한 구조색을 버튼에도 그대로 확장하면 새 색 추가 없이 "패널과 다른 버튼 룩"을 만들 수 있다 |
| **Button(주 버튼)** | 배경 `rgba(150,255,185,0.20)` + 테두리 `--color-accent-glow` 1px, `radius:0` 유지 | 현재 `bg-panel`(거의 검정) 채움을 걷어내고 accent-glow를 배경에도 써서 패널과 버튼을 시각적으로 분리 |
| **Button(보조 버튼)** | 배경 `--color-surface-hover`(흰 12%) + 테두리 `--color-border-hover`(흰 35%) — **`--color-button-secondary`(rgb 70,70,90) 폐기** | 이미 존재하지만 미문서화·미사용에 가까운 orphan 토큰을 없애고, 이미 정의된 hover 토큰을 "보조 버튼 기본 상태"로 재활용 — 새 토큰 0개 추가 |
| **Hover** | 카드: `surface`→`surface-hover`(8%→12%), `border`→`border-hover`(15%→35%) 그대로 유지. **버튼에도 동일 트랜지션 계약 확장** — 현재 Interaction Rules는 카드에만 적용되고 Button에는 적용 안 됨 | 새 색 없이 "카드 hover 규칙"을 "버튼 hover 규칙"으로 범위만 확장 |
| **Selected** | `--color-border-selected`(금색) 유지, 다만 **비선택 기준선을 `border` 15%→22% 알파로 소폭 상향** | 선택색 자체는 이미 충분히 강함 — 비선택 카드가 배경에 묻히지 않도록 바닥선만 올려 상대적 대비를 확보 |
| **Alert(경고/파괴적 액션)** | `--color-status-danger`(rgb 255,140,120) 배경 20% + 테두리 solid — "확인창의 위험 버튼" 전용으로 스코프 확장 | 기존 severity 축을 "국가 데이터 전용"에서 "위험을 뜻하는 모든 곳"으로 최소 확장. 새 색 추가 없음 |
| **정보 강조(Info)** | `--color-status-info`(rgb 140,210,255) 배경 15% — 툴팁/안내 배너 전용 | 기존 정보색을 그대로 배너/헬프 텍스트 배경에 재사용 |

### 정리

- 새로 만들어야 하는 색은 **0개**다. 모든 제안은 기존 토큰의 **적용 범위 확장**(구조 전용
  → 버튼 배경도 허용) 또는 **재사용**(hover 토큰을 보조 버튼 기본값으로)이다.
- 제거를 제안하는 토큰은 `--color-button-secondary`(rgb 70,70,90) 1개뿐 — 문서화도 안
  됐고 시각적으로도 주변 회색 배경과 구분이 안 되는 유일한 진짜 "미스컬러"다.
- DESIGN.md에는 **Component Library에 `Button` 항목 신설**(배경/테두리/hover/pressed
  4상태 표) + **Interaction Rules의 hover 트랜지션 적용 대상에 Button 포함**만 추가하면
  된다. 기존 8개 절(Canvas/Surface/Text/브랜드/Severity/노드/발광색/Do·Don't)은 그대로
  유지해도 무방하다.
- 구현 쪽에서는 `.popup-close`(2곳) / `.status-close-btn` / `.mainmenu-back` 4개 버튼에
  누락된 배경·테두리 선언을 채우는 작업이 우선순위가 가장 높다 — 사용자가 매 화면에서
  가장 먼저 만나는 버튼이자, 이미 한 번 같은 문제를 고친 전례(EndingScreen)가 있어
  수정 방법도 이미 코드베이스 안에 참고 사례로 존재한다.

---

## 최종 결론

**DESIGN.md 유지, 구현 수정 필요.**

색상 축 분리·표면 계단·브랜드 3색 고정 등 DESIGN.md의 핵심 규칙은 설계적으로 타당하며
폐기하거나 재작성할 이유가 없다. 다만 정확히 한 가지 지점 — **Button 컴포넌트의 색상
규칙이 문서에 아예 없다**는 공백은 존재하며, 이는 "잘못된 규칙"이 아니라 "누락된 규칙"이므로
전면 개정이 아닌 **Component Library에 Button 절 신설이라는 추가 작업**으로 해결된다.
사용자가 체감한 문제의 상당 부분(회색 과다, 산업용 프로그램 느낌)은 이 문서 공백의
결과이자, 이미 한 번 발견·수정 전례가 있는 동일한 미스타일드 버튼 버그가 4곳에서
재발한 결과다 — 즉 **원인의 대부분은 문서가 아니라 그 문서를 다 채우지 않은 구현**에 있다.
