# Contagion Design System v2

> **이 문서의 지위**: 이 문서(`Docs/DESIGN.md`)가 디자인 토큰·컴포넌트 계약·Do/Don't의 유일한
> Source of Truth다. 철학 문서가 아니라 **구현 명세서**다. "적절히"/"상황에 따라"/"권장" 같은
> 표현은 의도적으로 배제했다 — 모든 규칙은 수치·표·계약으로 표현한다.

---

## 0. Document Contract

**목적**: 이 문서 하나만 읽고 HUD/CountryPopup/CountryStatus/UpgradeTree/Ranking/MainMenu/
CountrySelect/ResearchPopup/EndingScreen 및 향후 신규 화면을 동일한 품질·스타일로 구현할 수
있어야 한다.

**Source of Truth 정의**:
- 이 문서(`Docs/DESIGN.md`)가 디자인 토큰·컴포넌트 계약·Do/Don't의 유일한 정본이다.
- 실제 구현은 `Assets/UI/Theme.uss`(토큰)와 `Assets/UI/Tactical.uss`(공용 컴포넌트)가 담당하며,
  두 파일의 값은 반드시 이 문서와 1:1로 일치해야 한다. 불일치가 발견되면 **이 문서가 아니라
  코드를 문서에 맞게 고친다**(코드가 어쩌다 그렇게 됐다면 그건 버그다).
- 화면별 `.uss`(Hud.uss, CountryPopup.uss 등)는 이 문서의 컴포넌트 계약을 소비만 한다 — 화면
  전용 파일에서 새 색상 토큰, 새 radius 값, 새 stroke 굵기를 도입하지 않는다.

**문서 우선순위**: 토큰 값 충돌 시 이 문서가 화면별 설계 문서(`Docs/UI_Design.md` 등)보다
항상 우선한다. 화면 배치/와이어프레임은 이 문서 범위 밖이다.

**해석 규칙**: 이 문서에 없는 수치가 필요하면 새로 화면에서 정하지 않는다 — 이 문서에
항목을 먼저 추가한 뒤 구현한다.

---

## 1. Design Principles

1. **항상 다크(Always-Dark)** — 라이트모드 파생 없음. 모든 화면의 바닥은 `--color-bg-root`
   계열이다.
2. **각짐 우선(Angularity-first)** — 기본 radius는 0이다. 예외는 5절(Radius Rules)에 정확히
   3개만 등재되며, 그 외 신규 radius 도입 금지.
3. **헤어라인, 그림자 없음** — USS는 `box-shadow`를 지원하지 않는다(엔진 제약). 위계는 배경
   alpha 단계(2절 Surface Ladder)와 테두리 굵기(6절 Stroke System)로만 표현한다.
4. **4색 축 분리(Color-Axis Separation)** — 브랜드/Severity/노드카테고리/구조발광색은 서로
   다른 4개의 독립 축이다. 한 축의 색을 다른 축의 의미로 재사용하지 않는다(자세한 규칙 2절).
5. **판독 우선(Readout-first)** — 신규 수치 표시는 항상 14절(Data Row System) 또는
   13절(Badge System)로 표현한다. 서술형 문단 텍스트로 되돌리지 않는다.
6. **모바일 세로 고정** — 참조 해상도 1440×3120(19.5:9), 가로 모드 미지원. 터치 타겟 최소
   48px(8절/9절에서 정확히 규정).
7. **엔진 제약이 곧 규칙이다** — `box-shadow`/`clip-path`/`mask`/`background-repeat`/
   `:last-child` 미지원은 "제약"이 아니라 이 시스템의 미학적 근거다. 코너컷·헤어라인·정적
   라인 배치·"옅은 색이라 마지막 줄이 남아도 무방"이 그 대응책이며, 신규 컴포넌트 설계 시
   이 4가지를 요구하는 방식을 먼저 배제한다.

---

## 2. Color System

### 2.1 Brand Colors (고정 3색 — 늘리지 않는다)

| 토큰 | 값 | 의미 | 재정의 금지 범위 |
|---|---|---|---|
| `--color-brand-dna` | `rgb(120, 220, 140)` | 병원체 DNA/치료제 — 플레이어 자원 | severity·노드상태 축에 전용 금지 |
| `--color-brand-gold` | `rgb(255, 210, 90)` | 강조/선택/화폐성 텍스트, 선택 테두리(`--color-border-selected`와 동일값) | 위와 동일 |
| `--color-brand-premium` | `rgb(120, 80, 200)` | 프리미엄(광고 보상) 액션 전용 | 위와 동일 |

### 2.2 Severity Colors (국가 데이터 전용 4색 — 고정)

| 토큰 | 값 | 의미 |
|---|---|---|
| `--color-status-infected` | `rgb(255, 170, 90)` | 감염 |
| `--color-status-dead` | `rgb(220, 90, 90)` | 사망 |
| `--color-status-danger` | `rgb(255, 140, 120)` | 위험/경고 |
| `--color-status-info` | `rgb(140, 210, 255)` | 정보/건강(양호 상태 포함) |

**Do**: 국가 관련 신규 수치 UI(population-bar, status-row, distribution-item, badge-tag)는
이 4색을 그대로 재사용한다.
**Don't**: 노드 상태(2.4절), UI 크롬(대륙 헤더 등), 브랜드 강조에 이 4색을 쓰지 않는다.

### 2.3 Node Category Colors (업그레이드 카테고리 3색, 40% 알파 고정)

| 토큰 | 값 | 의미 |
|---|---|---|
| `--color-node-transmission` | `rgba(120, 190, 255, 0.4)` | 전파 계열 |
| `--color-node-symptom` | `rgba(255, 140, 100, 0.4)` | 증상 계열 |
| `--color-node-ability` | `rgba(200, 140, 255, 0.4)` | 능력 계열 |

**적용 위치 고정**: 좌측 4px accent-bar(`border-left-width`)에만 사용한다. 카드 전체 배경에
칠하지 않는다 — 2.4절 노드 상태 축과 위치를 분리해 같은 카드 안에서 두 축이 충돌하지 않게
한다.

**실사용 확인(2026-07-17 재조사): 현재 소비처 0건.** `research-row`(좌측 accent-bar로 이 3색을
쓰던 유일한 컴포넌트)가 UpgradeTree 캔버스 복귀(17절 참고)로 완전히 제거되면서, 이 3개 토큰은
`Theme.uss`에 정의만 남고 어떤 `.uss` 파일에서도 참조되지 않는다(`--color-surface-mint`와 같은
"정의는 있으나 미사용" 상태, 2.5절 참고). 캔버스 화면은 카테고리별로 별도 탭/화면을 이미
분리해서 보여주므로 노드 단위 카테고리색 구분이 애초에 불필요해졌다 — 삭제 여부는 미결정이며
토큰 자체는 유지한다.

### 2.4 노드 상태 축 (Severity와 별개 — 브랜드색 재사용)

| 상태 | 텍스트/테두리 색 | 배경(있는 경우) |
|---|---|---|
| Locked | `--color-text-tertiary` | 없음, `opacity: 0.5` |
| Available | `--color-accent-glow` | 없음 |
| Active | `--color-brand-dna` | `rgba(120, 220, 140, 0.12)` |
| Maxed | `--color-brand-gold` | `rgba(255, 210, 90, 0.10)` |

**Locked opacity 정정(2026-07-17 UI Polish Audit)**: 과거 `research-row--locked`(제거된 리스트 UI)
시절 값 `0.7`이 문서에 남아 있었다. `tree-node--locked`로 교체되며(17절) `0.5`로 함께 바뀌었는데
문서만 갱신되지 않았다 — git 이력(`bf089f4`) 확인 결과 border-color(`--color-border`→
`--color-text-tertiary`)와 opacity(`0.7`→`0.5`)가 같은 커밋에서 함께 바뀐 의도적 변경이며,
`0.5`는 `Button:disabled`(9절, `Theme.uss`)의 기존 "비활성" 관례값과도 일치한다 — 코드가 아니라
문서가 틀렸던 것으로 판단해 표를 코드에 맞춰 정정했다.

### 2.5 구조용 발광색 (Structural Glow — 콘텐츠 색으로 사용 금지)

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-accent-glow` | `rgb(150, 255, 185)` | 패널 테두리, 코너컷, 캡션, 포커스 |
| `--color-accent-glow-soft` | `rgba(150, 255, 185, 0.35)` | 약한 테두리(기본 tactical-panel 테두리) |
| `--color-grid-line` | `rgba(150, 255, 185, 0.12)` | data-row/status-row 구분선, 격자선 |
| `--color-surface-mint` | `rgba(150, 255, 185, 0.04)` | **현재 미사용.** `Theme.uss`에는 정의돼 있지만 소비처(`hero-stat-tile`/`status-row`/`continent-header`)가 이후 라운드에서 배경을 완전히 제거해 실제 참조가 0건이다 — 삭제하지 않고 정의만 남겨둔 상태(부록 A 참고). |

### 2.6 Border Colors (중립 테두리 — 신규 등재, 2026-07-17 UI Polish Audit)

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-border` | `rgba(255, 255, 255, 0.15)` | 중립 테두리(구조용 발광색을 쓰지 않는 자리) — `country-row__flag`/`badge-tag`(`Tactical.uss` 정본, 13절) 테두리, `Theme.uss` Button 등 |
| `--color-border-hover` | `rgba(255, 255, 255, 0.35)` | 위 토큰의 hover 강조(에디터/PC 확인용) |

`Theme.uss`에 실존하며 `CountryPopup.uss`/`MainMenu.uss`/`Tactical.uss` 3개 파일에서 실사용 중이었으나
이 문서에 등재가 누락돼 있었다(0절 "Theme.uss 값은 반드시 이 문서와 1:1 일치해야 한다" 위반 상태) —
값 정정이 아니라 순수 등재 누락이라 코드는 그대로 두고 문서만 보강한다.

### 2.7 Text Colors

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-text-primary` | `rgb(255, 255, 255)` | 헤드라인/강조 |
| `--color-text-secondary` | `rgb(210, 210, 210)` | 값 텍스트 기본 |
| `--color-text-tertiary` | `rgb(180, 180, 180)` | 라벨/캡션/잠금 상태 |
| `--color-text-info` | `rgb(180, 220, 255)` | 정보성 텍스트(2.2절 `status-info`와 다른 토큰 — 혼동 금지) |

### 2.8 Surface Ladder (배경 alpha 계단)

| 토큰 | 값 | 사용처 |
|---|---|---|
| `--color-bg-root` | `rgba(8, 8, 16, 1)` | Fullscreen 화면 바닥(MainMenu/CountrySelect) — 0.97이던 시절 뒤에서 계속 렌더링되는 WorldMap이 옅게 비쳐 카드마다 다른 얼룩으로 보이는 문제로 완전 불투명 전환(경위는 DevLog.md) |
| `--color-bg-panel-alt` | `rgba(10, 10, 20, 0.92)` | Fullscreen "패널형" 화면(UpgradeTree) |
| `--color-bg-panel` | `rgba(15, 15, 25, 0.95)` | Overlay Panel(ResearchPopup)/Bottom Sheet Extended-Compact(RankingPanel) 등 팝업류 |
| `--color-bg-panel-strong` | `rgba(9, 9, 16, 1)` | Bottom Sheet Compact(CountryPopup)/Extended-Dense(CountryStatus) — 지도가 비쳐 보이면 안 되는 화면, 완전 불투명 |
| `--color-bg-scrim` | `rgba(0, 0, 0, 0.85)` | 엔딩 스크림 |
| `--color-bg-scrim-strong` | `rgba(0, 0, 0, 0.75)` | **현재 미사용.** HUD 하단바(graph-panel/action-strip) 전용으로 등재돼 있었으나 실측(2026-07-17 UI Polish Audit) 결과 두 컴포넌트 다 `--color-bg-panel`을 쓴다 — 정의만 남기고 삭제하지 않는다(부록 A 참고) |
| `--color-bg-scrim-soft` | `rgba(0, 0, 0, 0.55)` | **현재 미사용.** HUD 상단바(event-dock) 전용으로 등재돼 있었으나 실측 결과 `--color-bg-panel`을 쓴다 — HUD 5개 컴포넌트(resource-strip/event-dock/population-bar/graph-panel/action-strip) 전부 동일하게 `--color-bg-panel` 하나로 통일돼 있다(2026-07-17 UI Polish Audit로 정정 — 기존엔 event-dock 하나만 이 상태로 각주 처리됐으나 graph-panel/action-strip도 동일했음) |
| `--color-surface` | `rgba(255, 255, 255, 0.08)` | 카드/행 기본 배경(population-bar__track 등). `pathogen-card`/`country-row`/`status-row`/`detail-panel`은 이후 라운드에서 배경을 제거해 더 이상 이 토큰을 쓰지 않는다(경위는 DevLog.md). `tree-node`(17절)는 이 토큰이 아니라 `--color-bg-panel`을 쓴다 |
| `--color-surface-soft` | `rgba(255, 255, 255, 0.06)` | 트랙/프레임 배경(population-bar__track 등) |
| `--color-surface-selected` | `rgba(255, 210, 90, 0.12)` | 선택된 카드/행 배경 |
| `--color-surface-hover` | `rgba(255, 255, 255, 0.12)` | hover 타겟(에디터/PC 확인용) |

**Do**: 신규 패널은 위 8단계 중 하나를 반드시 선택한다. **Don't**: 중간 alpha 값을 임의로
만들지 않는다.

---

## 3. Typography

폰트는 프로젝트 기본(시스템/한글 지원) 단일 서체를 쓴다 — 별도 display/mono 서체 도입 금지
(한글 줄바꿈·자간 계산이 깨지는 리스크, 기존 검증 결론 유지).

| 토큰 | font-size | weight | letter-spacing | usage |
|---|---|---|---|---|
| `--font-size-caption-2xs` | 9px | bold | 0.5px | badge-tag 최소 텍스트 — 실사용처는 `CountryStatusPanel`(`status-row__badges .badge-tag`)이다. CountryPopup의 badge-tag는 실측 결과 font-size가 지정돼 있지 않아(Unity 기본 크기) 이 토큰을 쓰지 않는다 — 최초 등재 시 사용처를 잘못 표기한 오류를 정정 |
| `--font-size-caption-xs` | 10px | bold | 1px | world-status-frame__caption 등 초소형 유틸 라벨 *[신규 등재]* |
| `--font-size-caption` | 11px | bold | 1px(`--tracking-caption`) | tactical-caption, lab-caption *[신규 등재 — 4개 이상 화면에서 이미 반복 사용 중이던 값을 정식 토큰으로 승격. `section-caption`/`research-row__code`는 UpgradeTree 캔버스 복귀(17절, 2026-07-17)로 소비처인 `research-row` 자체가 제거되어 현재 사용례 없음 — 값 자체는 다른 화면에서 여전히 유효해 토큰은 유지]* |
| `--font-size-xs` | 13px | bold | 0.5px(`--tracking-label`) | data-label/data-value, 판독 행 기본 크기 |
| `--font-size-sm` | 14px | bold | 0.5px | stat-label, popup 값 텍스트 |
| `--font-size-body` | 16px | regular | 0 | 일반 본문(그래프 라벨 등) |
| `--font-size-md` | 18px | bold | 0 | status-row__name, hero-stat-tile__label |
| `--font-size-lg` | 20px | bold | 0 | country-row__name, ranking-value |
| `--font-size-xl` | 24px | bold | 0 | hero-stat-tile__value, upgrade-header__dna |
| `--font-size-xxl` | 26px | bold | 0 | 예약(현재 미사용 확인 — 사용 전 이 문서 갱신 필수) |
| `--font-size-display` | 32px | bold | 0 | mainmenu-title |
| `--font-size-hero` | 40px | bold | 0 | 엔딩 판정 타이틀 |

> **line-height는 이 문서에 포함하지 않는다.** 프로젝트 참고 위키
> `codebase/wiki/unity-ui-toolkit/css-to-uss-support.md`(72~76행)에 `line-height: ❌`(미지원)로
> 명시돼 있고, 실제 USS 11개 파일 전수 검색 결과 이 프로퍼티의 사용례가 0건이다. 지원되지
> 않는 프로퍼티를 규정하면 그대로 구현 시도 시 실패하므로, weight/letter-spacing까지만
> 규정하고 line-height는 컬럼 자체를 제거한다. 줄간격 표현이 필요하면 UI Toolkit이 지원하는
> `-unity-paragraph-spacing` 등 대체 프로퍼티의 지원 여부를 먼저 확인한 뒤 이 문서에 별도
> 항목으로 추가한다.

**레거시 예외(하드코딩, 신규 작업 금지) — 1:1 매핑표**:

| 레거시 값 | 위치 | 대체 토큰 |
|---|---|---|
| `17px` | MainMenu `detail-panel__desc` | `--font-size-body`(16px) |
| `21px` | MainMenu `pathogen-card__name` | `--font-size-lg`(20px) |
| `22px` | MainMenu `detail-panel__title` | `--font-size-xl`(24px) |

전부 토큰 미사용 하드코딩이다. 기존 화면은 그대로 두되(회귀 리스크), **신규 화면에서 이
레거시 값을 복제하지 않는다** — 위 표의 대체 토큰만 쓴다.

---

## 4. Spacing System — 4px Base Grid

| 토큰 | 값 |
|---|---|
| `--space-xs` | 4px |
| `--space-sm` | 8px |
| `--space-md` | 12px |
| `--space-lg` | 16px |
| `--space-xl` | 20px |
| `--space-xxl` | 24px |

**베이스는 4px다(8px 아님)** — 6단계 전부 4의 배수(4/8/12/16/20/24)이며, `20px`은 8의 배수가
아니므로 과거 문서의 "8px 베이스" 서술은 폐기한다.

**Do**: 패딩/마진은 항상 이 6개 값 중 하나를 쓴다. **Don't**: `14px`, `18px` 같은 임의 여백을
만들지 않는다(단, 아이콘/작은 장식 요소의 `2px`/`3px`/`6px` 같은 미세 조정은 예외로
허용한다 — data-row 하단 여백 2px, corner-cut 오프셋 등 이미 실측 확인된 관행).

---

## 5. Radius Rules (현재 실사용 기준 재정의)

과거 "3단계 스케일" 서술은 폐기한다. 실측 결과 각 토큰은 서로 다른 용도로 고정된 것이지
"단계"가 아니다.

| 토큰 | 값 | 용도 | 실사용 확인 |
|---|---|---|---|
| `--radius-none`(암묵적 `0`) | 0px | **기본값.** tactical-panel, corner-cut 부착 패널, 모든 버튼, 모든 Modal/Bottom Sheet | 압도적 다수 |
| `--radius-sm` | 3px | 코너컷이 없는 "판독 프레임" 계열 전용: population-bar__track, graph-frame, world-status-frame, badge-tag | Hud.uss/CountryPopup.uss 확인. `branch-row`/`research-row`는 UpgradeTree 캔버스 복귀(17절)로 제거된 소비처 — 현재는 소비하지 않음 |
| `--radius-md` | 6px | **메뉴 카드 예외 전용.** `country-row`(MainMenu/CountrySelect 화면, 7절 Fullscreen 분류) 단 1곳 — 각짐 원칙(Design Principles 2)의 의도된 예외 | MainMenu.uss 1곳만 확인 |
| `--radius-lg` | 8px | **재도입 금지.** `RankingPanel.uss`에서 `border-top-left/right-radius`로 실제 시도됐으나 코너컷과 충돌해 제거된 이력이 있다(해당 파일 주석에 기록) — "미사용"이 아니라 "시도 후 폐기됨". 코너컷이 붙는 어떤 패널에도 다시 쓰지 않는다 | RankingPanel.uss에 제거 이력 주석 확인, 현재 활성 사용처 없음 |
| (토큰 미부여, 하드코딩) | `24px`(`.tree-node`) / `12px`(`.tree-node--merge`) | **설계 원칙 이탈(미승격 상태) — UpgradeTree 캔버스 노드 전용.** Research Database v2(리스트형 `research-row`)가 Painter2D 캔버스로 되돌아가며(17절) 원형에 가까운 노드 실루엣을 위해 도입됨. 위 3개 공식 토큰 중 어느 것도 아니고, "각짐 우선"(1절 원칙 2) 및 "임의 radius 하드코딩 금지"(본 Don't 항목)에 명백히 위배된다 — 공식 예외로 승격되지 않은 잠정 상태(부록 A 참고)이며, 삭제/토큰화/공식 예외화 중 하나로 정리가 필요하다 | UpgradeTree.uss `.tree-node`/`.tree-node--merge` 실측 |

**Do**: 새 패널/버튼/모달/Bottom Sheet는 항상 0. 코너컷 없는 판독 프레임만 `--radius-sm`.
MainMenu/CountrySelect의 `country-row`(메뉴 카드)만 `--radius-md`.
**Don't**: `--radius-lg`를 재도입하지 않는다(과거 시도 후 코너컷과 충돌해 제거된 이력 —
위 표 참고). 임의의 radius 값(`2px`, `4px` 등)을 하드코딩하지 않는다 — `country-row__flag`의
`2px` 하드코딩과 `.tree-node`의 `24px`/`12px`는 둘 다 레거시/미결정 예외이며 신규 복제 금지.

---

## 6. Stroke System — "두께는 굵기가 아니라 의미다"

같은 두께가 서로 다른 의미를 표현하지 않는다. 항상 아래 5단계 중 하나를 선택한다.

| 토큰 | 값 | 의미 | 실사용 예 |
|---|---|---|---|
| `--stroke-hairline` | 1px | 구조(기본 테두리) | tactical-panel, data-row 하단선 |
| `--stroke-active` | 2px | 진행 상태(전체 테두리에만) | `tree-node`(전 상태 공통 기본 테두리 굵기, 17절) |
| `--stroke-sub-accent` | 2px | 보조 이분 상태(좌측 바에만) | data-row--open/--closed |
| `--stroke-selected` | 3px | 선택 상태 | country-row--selected, pathogen-card--selected. `tree-node--merge`(17절)도 같은 3px을 쓰지만 "선택"이 아닌 제3의 의미(합류 노드) — 6절 하단 위반 사례 참고 |
| `--stroke-accent-bar` | 4px | 카테고리 강조(좌측 바) | status-row 좌측 severity bar. `tree-node--final`(17절)도 같은 4px을 전체 테두리에 쓰지만 "카테고리 강조"가 아닌 제3의 의미(최종 진화 노드) — 6절 하단 위반 사례 참고 |

**주의**: `--stroke-active`(2px, `border-width`)와 `--stroke-sub-accent`(2px,
`border-left-width`)는 값은 같지만 **적용 프로퍼티가 다르다** — 전체 테두리 자리의 2px는
항상 "진행 상태", 좌측 바 자리의 2px는 항상 "보조 이분 상태"를 의미한다. 같은 프로퍼티
자리에서 두 의미를 혼용하지 않는다.

**실사용 확인(2026-07-17 재조사) — 위반 사례 1건, 미해결**: `UpgradeTree.uss`의
`.tree-node--merge`(전체 테두리 3px)와 `.tree-node--final`(전체 테두리 4px)이 각각
`--stroke-selected`(3px, "선택 상태" 전용)와 `--stroke-accent-bar`(4px, "카테고리 강조·좌측 바
전용")와 같은 값을 전체 테두리 자리에 쓰면서도 "선택"도 "카테고리"도 아닌 제3의 의미(합류
노드/최종 진화 노드 여부)를 표현한다 — 토큰을 참조하지 않는 하드코딩(`3px`/`4px` 리터럴)이라
값 충돌은 아니지만, 같은 굵기가 이미 다른 화면에서 다른 의미로 고정된 상태에서 세 번째 의미가
추가된 것이므로 6절의 "같은 두께가 서로 다른 의미를 표현하지 않는다" 원칙 위반이다. 5절
radius 이탈 항목과 같은 성격의 미결정 사안 — 정리 필요.

---

## 7. Layout System — 화면 분류 4종

| 분류 | 정의 | 해당 화면 |
|---|---|---|
| **Fullscreen** | 화면 전체를 차지하는 루트 캔버스. `--color-bg-root` 또는 `--color-bg-panel-alt` | MainMenu, CountrySelect, UpgradeTree, EndingScreen |
| **Overlay Panel** | 화면 중앙에 뜨는 독립 모달(가장자리 비고정). `TacticalModalController` 상속 | ResearchPopup |
| **Bottom Sheet** | 화면 하단에 고정 앵커, 지도/HUD를 완전히 가리지 않음(12절 참고) | CountryPopup(Compact), CountryStatusPanel·RankingPanel(Extended) |
| **HUD Chrome** | Gameplay 화면에 상시 존재하는 크롬(도킹 패널 포함) | resource-strip, event-dock, population-bar, graph-panel, action-strip |

신규 화면을 만들 때 반드시 이 4개 중 하나로 먼저 분류한 뒤 해당 절의 정확한 수치를 적용한다.
분류 없이 임의 레이아웃을 만들지 않는다.

---

## 8. Panel System

### `tactical-panel` (기본 패널 셸)

| 속성 | 값 |
|---|---|
| `border-width` | 1px(`--stroke-hairline`) |
| `border-color` | `--color-accent-glow-soft` |
| `border-radius` | 0(고정, 5절 예외 미적용) |
| 배경 | 지정하지 않음(맥락별 Surface Ladder에서 소비 측이 선택) |

### `tactical-panel__header`

| 속성 | 값 |
|---|---|
| `padding-bottom` | 3px |
| `margin-bottom` | `--space-xs`(4px) |
| `border-bottom-width` | 1px |
| `border-bottom-color` | `--color-grid-line` |

### Corner Cut(코너컷)

| 속성 | 값 |
|---|---|
| 크기 | 13px × 2px |
| 색상 | `--color-accent-glow` |
| 위치(4종) | tl: `top:5px; left:-4px; rotate:45deg` / tr: `top:5px; right:-4px; rotate:-45deg` / bl: `bottom:5px; left:-4px; rotate:-45deg` / br: `bottom:5px; right:-4px; rotate:45deg` |
| 부착 규칙 | 화면당 인스턴스 1~6개(적은 패널)에는 4개 전부. 수십 개 반복 리스트 행에는 부착 금지 — 대신 6절 `--stroke-accent-bar`(4px 좌측 바) 사용 |

---

## 9. Button System

| 속성 | Primary | Secondary | Danger(예약) |
|---|---|---|---|
| `height` | `--touch-target-min`(48px) | 48px(단, 헤더 아이콘 버튼은 32px 예외 — 아래 참고) | 48px |
| `border-width` | 1px | 1px(`--stroke-hairline`) | 1px |
| `border-color` | `--color-accent-glow` | `--color-accent-glow` | `--color-status-danger` |
| `border-radius` | 0 | 0 | 0 |
| `background-color` | `--color-accent-glow-soft` (프리미엄 액션은 `--color-brand-premium`) | `--color-bg-panel` | `--color-bg-panel` |
| `color` | `--color-text-primary` | `--color-text-primary` | `--color-status-danger` |
| `:active` | `scale: 0.96 0.96`(색은 유지) | 동일 | 동일 |
| `:disabled` | `opacity: 0.5` | 동일 | 동일 |

**헤더 아이콘 버튼 예외**: 팝업/패널 헤더의 닫기(✕) 버튼은 32×32px(Secondary 규격 상속,
높이만 예외) — CountryPopup `popup-close`, CountryStatus `status-close-btn` 실측 기준.

**Fullscreen 판정 버튼 예외**: `EndingScreen`(Fullscreen/Debrief, 7절)의 재시작/부활 버튼은
높이 56px(폭 180px 고정, Primary/Secondary 색상 규칙은 그대로 — `ending-button`은 Secondary
배경+`--color-accent-glow` 테두리, `ending-button--revive`는 `--color-brand-premium` 배경).
Fullscreen 판정 화면의 확정 액션에 한해 48px보다 큰 이 높이를 허용한다 — Bottom
Sheet/Overlay Panel/HUD Chrome에서는 복제하지 않는다.

**Do**: `background-color`/`border`/`border-radius`를 항상 명시적으로 지정한다(미지정 시
Unity 기본 회색 버튼이 노출되는 실패가 최소 5개 화면에서 반복 발생 — 20절 참고).
**Don't**: 색이 있는 버튼(브랜드색 배경)에서 `:active` 시 배경색을 바꾸지 않는다 — `scale`만
사용.

---

## 10. Card System

| 컴포넌트 | 배경 | 테두리 | radius | 비고 |
|---|---|---|---|---|
| `tree-node`(UpgradeTree 캔버스, 17절) | `--color-bg-panel`, `active`/`maxed` 상태는 배경도 상태색으로 채움 | 2px, 상태 4단계 색(2.4절) 전체 테두리. `--merge`는 3px, `--final`은 4px+골드로 override | `24px`(원형에 가까운 실루엣), `--merge`는 `12px` — 둘 다 5절 공식 3값 밖의 하드코딩(설계 이탈, 5절 참고) | 카테고리색(2.3절)은 미사용(캔버스가 카테고리별 화면 자체를 분리하므로 불필요) |
| `pathogen-card` | 없음(투명) | tactical-panel 상속(코너컷 4개) | 0 | Fullscreen 메뉴 카드. 배경 제거 경위는 DevLog.md |
| `country-row` | 없음(투명) | 2px `--color-accent-glow-soft` + 좌측 4px | `--radius-md`(6px, 5절 예외) | MainMenu/CountrySelect(7절 Fullscreen). 배경 제거, 외곽 테두리 회색→민트 변경 경위는 DevLog.md |
| `status-row` | 없음(투명) | 좌측 4px severity색(2.2절) + 하단 1px `--color-grid-line` | 0 | CountryStatus 48개국 리스트(16절). 배경 제거·하단 구분선 추가 경위는 DevLog.md |
| 선택 상태(공통) | `--color-surface-selected` | 3px `--color-border-selected`(골드) | 각 카드 규칙 유지 | `--stroke-selected` |

---

## 11. Modal System (현재 `TacticalModalController` 기준)

**계약(고정 UXML 이름)**: `modal-root`(class: `tactical-panel` + 분류에 맞는 위치 클래스),
`modal-title`(Label), `modal-close`(Button), `modal-rows`(class: `detail-rows`, 컨트롤러가
`data-row`를 동적으로 Add), `modal-footer`(비어있어도 됨, 액션 버튼 슬롯).

**API 계약**: `Show(string title)`(display:Flex + 타이틀 대입), `Hide()`(display:None),
`ClearRows()`, `AddRow(label, value, valueClass?, rowClass?)`, `AddSectionCaption(text)`.
현재 `AddRow`/`AddSectionCaption`을 실제로 호출하는 화면은 없다(`CountryPopupController`/
`ResearchPopupController` 둘 다 자체 표시 방식을 쓴다) — 계약은 유지하되 소비처는 부록 A 참고.

**Do**: 신규 Overlay Panel/Modal은 이 계약을 그대로 상속한다(`TacticalModalController` 상속,
새 베이스 클래스 만들지 않음).
**Don't**: `modal-rows`/`AddRow` 계약을 그리드형 UI(13절 Badge/14절 Data Row 이외의 구조)로
억지로 확장하지 않는다 — 그런 화면은 자체 named-Label 캐싱 패턴(12절 CountryPopup 참고)을
쓴다.

### 11.1 Modal Footer Button (`Tactical.uss` 정본 — RankingPanel/ResearchPopup 소비)

| 속성 | 값 |
|---|---|
| `modal-footer` | `flex-direction:row; justify-content:space-between; margin-top: --space-sm` — 버튼 2개 균등폭 배치 컨테이너 |
| `popup-footer-button` | `flex-grow:1; flex-basis:0; height: --touch-target-min; border-width:1px; border-color: --color-accent-glow; border-radius:0; background-color: --color-bg-panel; color: --color-text-primary` — 기본형(Secondary 톤) |
| `popup-footer-button--confirm` | `background-color: --color-accent-glow-soft; border-color: --color-accent-glow` — 확정 액션 강조(예: "N DNA · 연구하기", "리더보드 열기") |

**소비처**: `RankingPanel.uss`(확인/닫기), `ResearchPopup.uss`(연구하기/취소) — 둘 다 로컬
재정의 없이 `Tactical.uss` 정본을 그대로 사용한다. **ResearchPopup의 확정 버튼 텍스트는
고정값이 아니다**(2026-07-17 재조사, 17절 참고) — `ResearchPopupController`가 노드 상태에
따라 4갈래로 갈아끼운다: 구매 가능(`"N DNA · 연구하기"`, `--confirm` 활성) / DNA 부족
(`"DNA {부족량} 부족"`, 비활성) / 이미 해금(`"연구 완료"`, 비활성) / 선행조건 미충족
(`"다음 필요 연구: {노드명, 노드명}"`, 비활성 — `UpgradeManager.GetMissingPrerequisites()`가
계산한 미해금 선행 id 목록을 `UpgradeTreeView.GetDisplayName()`으로 변환해 조립, LockReason
System·2026-07-17). `popup-footer-button--confirm` 클래스 자체는 항상 유지하고 `SetEnabled()`와
텍스트만 바뀐다 — 클래스를 갈아끼우지 않는다.
**Do**: 버튼 2개 이하인 Modal/Overlay Panel footer는 이 컴포넌트를 재사용한다.

---

## 12. Bottom Sheet System (신규 정의 — 이전에 없던 규격)

Bottom Sheet는 두 변형(Compact/Extended)으로 표준화한다. 신규 화면은 반드시 이 중 하나를
선택한다 — 임의의 top/bottom % 값을 새로 정하지 않는다.

### 12.1 Compact (예: CountryPopup)

| 항목 | 규격 |
|---|---|
| **Anchoring** | `position: absolute; left:0; right:0; bottom:0` — `top` 지정하지 않음(콘텐츠 높이 기반) |
| **Width** | 100% |
| **Height** | 콘텐츠 기반, **`max-height: 60%`**(신규 규정 — 상한 없던 기존 관행 수정) |
| **Safe Area** | 하단 `padding-bottom`에 모바일 제스처 바 인셋 반영(신규 규정 — 기존 미반영) |
| **Header** | 높이 자유(콘텐츠 기반), 국기/타이틀/부제/닫기(32px) 가로 배치 |
| **Content** | `padding: --space-lg`(16px), 내부 그리드는 13/14절 컴포넌트 사용 |
| **Footer** | 버튼 0~2개, 높이 `--touch-target-min`(48px), Primary 규격(9절) |
| 배경 | `--color-bg-panel-strong` |
| radius | 0 |

### 12.2 Extended (예: CountryStatus, Ranking)

| 항목 | 규격 |
|---|---|
| **Anchoring** | `position: absolute; left:0; right:0; bottom:12.5%`(Hud 하단 action-strip 항상 노출) |
| **top 프리셋(실사용 기준 — 표준화 완료)** | **Dense**(대시보드형, 콘텐츠 많음): `top: 3%`(CountryStatusPanel.uss 실측) / **Compact**(확인·액션형, 콘텐츠 적음): `top: 42%`(RankingPanel.uss 실측) — 이 둘 중 하나만 선택, 새 % 금지 |
| **Width** | 100% |
| **Safe Area** | 상단 SafeArea 인셋을 `top` 프리셋에 더한다(신규 규정) |
| **Header** | `tactical-panel__header` 가로 배치(타이틀 + 닫기 28px) |
| **Content** | `ScrollView`(`flex-grow:1; flex-basis:0; overflow` 상위 컨테이너는 `overflow:hidden` 필수 — 세로 오버플로우 버그 재발 방지, 20절 참고) |
| **Footer** | 없음(콘텐츠 내부 버튼으로 대체) |
| 배경 | Dense: `--color-bg-panel-strong`(완전 불투명) / Compact: `--color-bg-panel` |
| radius | 0 |

**Do**: CountryStatus(콘텐츠 많음) = Dense(`top:3%`), Ranking(콘텐츠 적음) = Compact(`top:42%`) —
두 화면 모두 이 표준을 그대로 따른다.
**Don't**: `top` 값을 3%/42% 외의 임의 값으로 만들지 않는다.

---

## 13. Badge System (CountryPopup `badge-tag` 기준)

| 속성 | 값 |
|---|---|
| `padding` | `2px --space-xs`(2px 4px) |
| `border-width` | 1px |
| `border-radius` | `--radius-sm`(3px) |
| `align-self` | `flex-start`(부모 폭 전체로 늘어나지 않음) |
| 배경 | 지정하지 않음(투명, 테두리+텍스트 색으로만 표현) |

### Severity 모디파이어(4종 고정 — 배타 적용, `CountryPopupController.ApplySeverityClass()` 최초 구현. `CountryStatusPanelController.ApplySeverityClass()`도 동일 규약으로 `status-row__badges`(공항/항구/국경)에 적용)

| 클래스 | 텍스트/테두리 색 | 의미 |
|---|---|---|
| `.badge-tag--success` | `--color-status-info` | 개방/양호/선진국 |
| `.badge-tag--warning` | `--color-status-infected` | 폐쇄/주의/개발도상국 |
| `.badge-tag--danger` | `--color-status-dead` | 위험/저개발국 |
| `.badge-tag--info` | `--color-text-info` | 중립 정보(현재 예약, 실사용 대기) |

**Do**: 4개 모디파이어는 항상 배타적으로 적용(이전 클래스 전부 제거 후 하나만 추가).
**Don't**: badge-tag에 배경색을 채우지 않는다(테두리+텍스트만 — 구조용 발광색과 시각적으로
구분되게).

---

## 14. Data Row System (CountryStatus/CountrySelect/EndingScreen 공통)

`Tactical.uss`의 `data-row`/`data-label`/`data-value` 계약 — 이 문서가 정의하는 **가장 기본이
되는 수치 판독 단위**다. 신규 수치 판독 UI가 필요하면 항상 이 컴포넌트부터 검토한다.

| 요소 | 속성 |
|---|---|
| `data-row` | `flex-direction:row; justify-content:space-between; padding-bottom:2px; margin-bottom:2px; border-bottom-width:1px; border-bottom-color: --color-grid-line` |
| `data-label` | `color: --color-text-tertiary; font-size: --font-size-xs(13px); letter-spacing: --tracking-label(0.5px); flex-shrink:0` |
| `data-value` | `color: --color-text-secondary; font-size: --font-size-xs; bold; letter-spacing: --tracking-label; flex-shrink:1; min-width:0; white-space:normal; -unity-text-align: upper-right` |

### Severity 모디파이어(값 텍스트 색상만 교체)
`.data-value--infected`(`--color-status-infected`) / `.data-value--dead`(`--color-status-dead`)
/ `.data-value--danger`(`--color-status-danger`) / `.data-value--info`(`--color-status-info`)

### 노드 상태 모디파이어(UpgradeTree 전용 축 — Severity와 교차 금지, 현재 미사용)
`.data-value--locked`(`--color-text-tertiary`) / `.data-value--available`(`--color-accent-glow`)
/ `.data-value--active`(`--color-brand-dna`) / `.data-value--maxed`(`--color-brand-gold`)

**실사용 확인(2026-07-17 재조사)**: 위 4개 모디파이어는 `UpgradeTree.uss`에 정의만 남아있고
`UpgradeTreeView.cs`의 어떤 코드도 `data-value`/`data-row` 클래스를 적용하지 않는다(UpgradeTree
캔버스 복귀로 상세 리스트 자체가 사라짐, 17절 참고) — 노드 상태는 이제 `tree-node--{state}`가
전담한다(2.4절 색 규칙은 동일하게 재사용). 삭제 여부 미결정, 정의만 유지.

**소비처**: CountryStatus(world-summary-rows, ranking-infected/dead) / CountrySelect / EndingScreen
— 전부 `Tactical.uss`의 `data-row`/`data-label`/`data-value` 클래스를 직접 붙이는 방식이다.
`TacticalModalController.AddRow()`(11절, `CountryPopupController`/`ResearchPopupController`가
상속)는 헬퍼로 존재하지만 실제 호출부가 전체 코드베이스에 0건이다(부록 A "코드 정리 대상"
참고) — 두 팝업 다 이 헬퍼를 거치지 않고 각자 다른 방식(quick-stat-grid, popup-bullet 등)으로
수치를 표시한다. HUD는 이 컴포넌트를 직접 소비하지 않는다.

---

## 15. HUD Rules

| 컴포넌트 | 높이/폭 | 배경 | 테두리 |
|---|---|---|---|
| `resource-strip` | 34px | `--color-bg-panel` | 좌우하단 1px `--color-accent-glow-soft` |
| `event-dock` | 폭 360px, 좌상단 절대배치 | `--color-bg-panel` | 1px `--color-accent-glow-soft`, 코너컷 4개 |
| `population-bar__track` | 높이 14px | `--color-surface-soft` | 1px, radius `--radius-sm` |
| `graph-panel` 프레임 | 104×52px | `--color-surface-soft` | 1px, radius `--radius-sm` |
| `action-strip` 버튼 | `--touch-target-min`(48px) | 9절 Button System 그대로 |

3세그먼트 비율형 데이터(population-bar)는 항상 healthy→infected→dead 고정 순서,
`flex-basis:0` + `flex-grow` 비율로만 표현(절대 픽셀 계산 금지). 추세형(graph-panel)은
프레임 고정(104×52px) + 기준선 1개(`--color-accent-glow-soft`) + 보조 격자 2개(25%/75%,
`--color-grid-line`).

---

## 16. Country Status Rules

- 분류: Bottom Sheet — Extended / Dense(`top:3%`, 12.2절).
- Hero Stats: 2×2 타일, 타일 폭 48%, 값 폰트 `--font-size-xl`(24px). 배경 없음(투명), 테두리
  `--color-accent-glow`(저반복 4개뿐이라 full 강도).
- 대륙 아코디언: 코너컷 금지(리스트 반복 요소), 배경 없음(투명), 좌측바 `--color-accent-glow`
  상시 노출 — hover는 `background-color: --color-surface-hover`만 담당(터치 기기에서
  `:hover`가 거의 안 걸려 좌측바 색 자체를 hover 전용으로 두지 않음). 대륙 헤더는 "국가
  데이터"가 아니라 "UI 크롬"이라 Severity 4색 사용 금지(2.2절 Don't).
- 48개국 리스트 행(`status-row`): 배경 없음(투명), 좌측 4px severity accent-bar(2.2절 4색),
  하단 1px `--color-grid-line` 구분선, radius 0. 감염률/사망률은 `data-row` 2줄이 아니라
  `Hud.uss .population-bar__summary`와 동일 문법의 한 줄 요약("감염 N% · 사망 N%",
  `--font-size-xs`/`--color-text-secondary`)으로 표시한다. 공항/항구/국경 배지(`badge-tag`)는
  13절과 동일 규약(`CountryStatusPanelController.ApplySeverityClass()`)으로 개방/폐쇄에 따라
  `badge-tag--success`/`badge-tag--warning`을 배타 적용한다.
- `overflow: hidden` + `flex-direction: column`을 status-root에 항상 명시(세로 오버플로우로
  콘텐츠가 배경 밖으로 새어나가 지도가 비치는 버그 재발 방지, 20절 사례 참고).

---

## 17. UpgradeTree Rules

> **화면 구조 전면 교체 완료(2026-07-17, Commit 1~5).** Research Database v2의 리스트형 UI
> (`branch-board`/`research-row`/`detail-panel`)는 전부 제거되었고, `node-scroll` 안에는
> Painter2D 절대좌표 캔버스(`tree-node` + 연결선)만 존재한다. 제거된 컴포넌트의 구현 배경·
> 삭제 경위는 `Docs/DevLog.md`("Commit 1~5 — UpgradeTree 캔버스 승격" 항목)를 참고 — 이 문서는
> 현재 상태만 서술한다.

- 분류: Fullscreen(`--color-bg-panel-alt`).
- 탭 3개 고정, 높이 `--touch-target-min`(48px), 선택 강조는 새 색상 없이 하단 2px
  `--color-border-selected`(골드) 밑줄로만 표현(무변경).
- **트리 캔버스**: `node-scroll.contentContainer`를 `UpgradeTreeView.BuildTreeCanvas()`가 직접
  채운다. 노드의 `node.position`(월드 좌표, `DefaultUpgradeTreeFactory.cs`)을 이 카테고리 노드들의
  최소값 기준으로 런타임에 정규화해 캔버스 로컬 좌표로 배치한다(카테고리별 x 오프셋 하드코딩
  없음). 카테고리(전파/증상/적응)는 이미 탭으로 화면 자체가 분리되어 있어, 노드 카드 안에서
  카테고리색(2.3절)을 다시 표시하지 않는다 — 2.3절 실사용 확인 참고.
- **`tree-node`**(캔버스 노드 카드): `position:absolute`, `border-width:2px`, `border-radius:24px`
  (원형에 가까운 실루엣 — 5절 공식 3값 밖의 하드코딩, 설계 이탈 미해결), 배경 `--color-bg-panel`.
  상태 4단계(2.4절 색 규칙 재사용)는 `tree-node--locked/available/active/maxed` 클래스가
  전체 테두리(+active/maxed는 배경도) 색으로 표현 — 좌측 accent-bar 방식이 아니라 **전체
  테두리/배경 방식**으로 상태를 표현한다(구 `research-row`의 "카테고리=좌측 바, 상태=전체"
  위치 분리 규칙 자체가 카테고리 축 소멸로 무의미해짐).
  - `tree-node--merge`(합류 노드, 선행이 2개 이상이고 리프가 아님): `border-width:3px`,
    `border-radius:12px`로 override.
  - `tree-node--final`(최종 진화 노드, 이 노드를 선행으로 삼는 다른 노드가 없음): `border-width:4px`
    + `border-color: --color-brand-gold`로 override(상태와 무관하게 항상 골드 — CSS 파일 내
    선언 순서상 마지막 규칙이라 `locked`/`available` 등과 동시 적용돼도 이 색이 이긴다).
  - `merge`/`final`이 6절 Stroke 의미(3px=선택, 4px=카테고리 강조)와 충돌하는 점은 6절 하단에
    위반 사례로 기록.
- **연결선(`TreePathElement`)**: `generateVisualContent`+`Painter2D`로 직접 그리는 커스텀
  `VisualElement`(`pickingMode: Ignore`로 클릭을 가로채지 않음). `prerequisites` 그래프의
  "선행 → 자신" 각 쌍을 선분 하나로 그린다 — 목적지 노드가 해금됐으면 밝고 굵게(`rgba(150,255,185,0.9)`,
  6px, `--color-accent-glow`와 동일 색), 아니면 흐리고 얇게(`rgba(150,255,185,0.28)`, 3px).
  Unity UI Toolkit에 네이티브 라인 프리미티브가 없어 도입된 캔버스 전용 그리기 기법이며, 이
  문서의 다른 어떤 화면도 쓰지 않는 유일한 사례다.
- **연구 상세 팝업**: 노드 클릭 시 `ResearchPopupController.Show()`가 열리고(Overlay Panel,
  11절 Modal System 계약 그대로), 비용/효과/주의/버튼 상태 4갈래/고급 정보(Foldout)까지
  전부 이 팝업이 전담한다 — 상세 스펙은 21.3절. 확인 버튼은 구매 가능 상태에서만 활성화되고
  `UpgradeManager.TryUnlock()`을 직접 호출한다(성공 시에만 `Hide()`). 잠김/DNA 부족/이미
  해금 상태에서는 애초에 버튼이 비활성화되므로 "클릭했는데 실패"하는 경로 자체가 사라졌다
  (`LockReason()`처럼 "왜 잠겼는지" 구체적 사유를 보여주는 건 아직 미구현 — CLAUDE.md TODO
  참고). UpgradeTree 화면 자체는 더 이상 `data-row`/`detail-rows`를 쓰지 않는다(14절 참고) —
  수치 판독은 전부 ResearchPopup 쪽으로 이동했다.

---

## 18. Component Registry (실제 USS 파일 기준)

| 파일 | 역할 | 이 문서와의 관계 |
|---|---|---|
| `Theme.uss` | 전 토큰(`:root`) 정의 | **1차 Source of Truth 구현체** — 이 문서 2~6절과 값이 반드시 일치 |
| `Tactical.uss` | 공용 컴포넌트: tactical-panel/corner-cut/data-row/modal-footer/popup-footer-button | **2차 Source of Truth 구현체** — 8/11.1/14절 |
| `Hud.uss` | resource-strip/event-dock/population-bar/graph-panel/action-strip | 15절 소비처. 로컬 중복 정의 없음(corner-cut은 Tactical.uss 참조로 정리 완료, UI Audit 1차) |
| `CountryPopup.uss` | Bottom Sheet(Compact), quick-stat-grid | 12.1절 구현체. badge-tag(13절)는 원래 이 파일이 최초 구현체였으나 두 번째 소비처(CountryStatusPanel) 추가 시 `Tactical.uss`로 공용 승격됐다 — 로컬 중복 정의는 2026-07-17 UI Polish Audit로 제거, 이제 `Tactical.uss`가 정본 |
| `CountryStatusPanel.uss` | Bottom Sheet(Extended/Dense) | 12.2/16절 소비처. `.status-row .data-row`(더 이상 어떤 자식도 매치하지 않는 죽은 규칙 — 감염률/사망률이 `status-row__stats-summary` 단일 Label로 교체됨) 정리 대상, 코드 변경 필요(부록 A 참고). `--color-surface-mint`(2.5절)는 이 파일이 아니라 어떤 `.uss`에도 참조가 없다 — 2026-07-17 UI Polish Audit로 "이 파일의 잔재" 서술 정정 |
| `RankingPanel.uss` | Bottom Sheet(Extended/Compact) | 12.2절 소비처. 로컬 중복 정의 없음(modal-footer/popup-footer-button은 Tactical.uss 참조로 정리 완료, UI Audit 1차) |
| `UpgradeTree.uss` | Fullscreen, `tree-node` 캔버스(17/21.1/21.2절) | 17절 소비처. `research-row`/`branch-board`/`detail-panel`은 제거됨(경위는 DevLog.md). `tactical-panel`/`corner-cut`/`data-row`/`data-label` 참조는 UXML에서 이미 제거되어 현재 미사용, `data-value--locked/available/active/maxed`(14절)는 정의만 남고 코드가 더 이상 적용하지 않는 죽은 규칙 — 정리 대상 |
| `MainMenu.uss` | Fullscreen, pathogen-card/country-row/detail-panel | 10절 소비처, `--radius-md` 유일 사용처(5절), 21.9절(Pathogen Stat Row) 구현체 |
| `CountrySelect.uss` | MainMenu.uss 확장(개발수준 accent bar 색만 추가) | 10절 소비처 |
| `ResearchPopup.uss` | Overlay Panel(중앙 모달), 노드 상세(비용/효과/주의/고급 정보) | 11/11.1절 Modal 계약 소비처(modal-footer/popup-footer-button은 Tactical.uss 참조, 공용 부분 로컬 중복 없음). 2026-07-17 재조사(17절 ResearchPopup 확장, 21.3절 참고) — `popup-cost`/`popup-section`/`popup-bullet`/`popup-advanced`(Foldout)/`popup-bar-*` 등 이 화면 고유 로컬 컴포넌트 신규 추가, 전부 기존 토큰만 재사용(신규 색상 토큰 없음) |
| `EndingScreen.uss` | Fullscreen(Debrief) | 9절 Button System 소비처, 과거 실패 사례 발생 파일(20절) |

---

## 19. Do

- 신규 패널은 항상 7절 4분류 중 하나로 먼저 분류하고, 해당 절의 정확한 수치를 그대로 쓴다.
- 신규 수치 판독은 14절 Data Row 또는 13절 Badge System을 재사용한다 — 새 판독 컴포넌트를
  만들기 전에 이 둘로 표현 가능한지 먼저 검토한다.
- 카테고리색은 좌측 accent-bar(4px), 상태색은 테두리 전체+배경 — 위치를 분리한다(단, UpgradeTree
  `tree-node`는 카테고리 축 자체가 없는 예외 — 17절 참고. 신규 화면은 이 예외를 근거로 축 분리
  원칙을 생략하지 않는다).
- 버튼은 항상 9절 표의 `background-color`/`border`/`border-radius`를 전부 명시적으로
  지정한다.
- Bottom Sheet는 12절의 두 변형(Compact/Extended) 중 하나만 쓴다.
- 공용으로 쓰일 가능성이 있는 컴포넌트는 **처음부터** `Tactical.uss`에 정의한다("나중에 승격"
  금지, 20절 근거).

## 20. Don't — 실제 발생했던 실패 사례 기반

- **Unity 기본 버튼 노출**: `background-color`/`border`/`border-radius` 중 하나라도 누락하면
  회색 기본 버튼이 그대로 보인다 — `EndingScreen.uss .ending-button`에서 최초 발생, 이후
  동일 패턴이 최소 4개 화면(RankingPanel/MainMenu/CountryStatusPanel/CountryPopup)에서
  반복 확인·수정됨. **신규 버튼은 9절 표를 그대로 복사해서 시작한다.**
- **Severity 색상 오용**: 대륙 헤더(UI 크롬)에 Severity 4색을 쓰려다 "이건 국가 데이터가
  아니라 크롬"이라는 이유로 금지된 사례(CountryStatusPanel.uss 주석에 실제 기록) — 노드 상태
  축(2.4절)에도 Severity색을 재사용하지 않는다.
- **Surface 계층/오버플로우 위반**: `status-root`에 `flex-direction`/`overflow`를 명시하지
  않아 콘텐츠가 박스 경계 밖으로 흘러넘쳐 배경이 비치는(지도가 보이는) 버그가 실제 발생 —
  Bottom Sheet Extended는 항상 `overflow:hidden` + `flex-direction:column` 명시(16절).
- **Local Component 중복 생성(해결됨)**: `tactical-panel`/`corner-cut`/`data-row`/`modal-footer`/
  `popup-footer-button`이 `Tactical.uss` 승격 이후에도 `Hud.uss`/`UpgradeTree.uss`/
  `RankingPanel.uss`/`ResearchPopup.uss`에 로컬로 재정의된 채 남아있었다 — 같은 값을 이중
  정의하는 것 자체가 버그는 아니지만, 두 곳이 어긋나기 시작하면 화면마다 다른 결과가 나오는
  근본 원인이 될 수 있었다. UI Audit 1~5차(2026-07)로 전부 제거하고 `Tactical.uss` 참조로
  전환 완료(18절 Component Registry 참고) — **공용으로 쓰일 가능성이 있는 컴포넌트는 처음부터
  `Tactical.uss`에 정의한다**는 교훈만 19절 Do에 유지한다.
- **Stroke 의미 충돌**: 좌측 accent-bar(보조 이분 상태)에 `--stroke-selected`(3px)를 쓰다가
  "선택 상태"와 굵기가 겹쳐 혼동된 사례, `--stroke-active`(2px)를 좌측 바에 썼다가 "보조
  이분 상태"와 혼동된 사례 — 둘 다 실제로 발생해 수정됨. 6절 표의 프로퍼티별 의미를 정확히
  지킨다.
- **Radius 임의 도입**: `country-row__flag`에 `2px`를 하드코딩한 사례처럼, 5절 표에 없는
  radius 값을 새로 만들지 않는다.

---

## 21. Screen-Specific Components (실제 구현 기준 신규 문서화)

이 절은 화면별 `.uss`에 이미 구현되어 있으나 이전 버전 문서에 등재되지 않았던 컴포넌트를
기록한다. **새 디자인이 아니라 기존 구현의 사실 확인**이다 — 목적/사용 화면/구성 요소/사용
규칙만 서술하고 값은 실제 코드에서 그대로 인용한다.

### 21.1 Tree Node (UpgradeTree 캔버스 노드)

| 속성 | 값 |
|---|---|
| 목적 | 연구 트리 한 노드를 절대좌표 위에 배치하는 카드 — 상태 4단계(잠김/가능/진행/완료)와 합류·최종 진화 여부를 시각적으로 구분 |
| 사용 화면 | UpgradeTree(캔버스, 17절) |
| 구성 요소 | `tree-node`(카드) > 텍스트(노드명) — 별도 라벨 서브클래스 없이 카드 자체가 텍스트를 직접 담음 |
| 사용 규칙 | 상태 4단계(2.4절 색 재사용)는 전체 테두리(+active/maxed는 배경도)로 표현 — 좌측 accent-bar가 아니라 **전체 테두리/배경** 방식(구 `research-row`의 "카테고리=좌측 바, 상태=전체" 위치 분리는 카테고리 축 소멸로 무의미해짐, 2.3절 참고). `tree-node--merge`(합류)는 `border-width:3px`, `tree-node--final`(최종 진화)은 `border-width:4px`+골드로 override — 둘 다 6절 Stroke 의미(3px=선택, 4px=카테고리 강조)와 충돌하는 미해결 사안(6절 하단 참고). `border-radius`(24px/`--merge`는 12px)는 5절 공식 3값 밖의 하드코딩 — 설계 이탈 미해결(5절 참고) |

### 21.2 Connector Line (TreePathElement)

| 속성 | 값 |
|---|---|
| 목적 | 노드 간 선행조건 관계("선행 → 자신")를 선분으로 시각화 — 해금 여부에 따라 밝기/굵기로 진행 상태 표현 |
| 사용 화면 | UpgradeTree(캔버스, 17절) |
| 구성 요소 | `TreePathElement`(신규 C# 클래스, `VisualElement` 상속) — UXML 마크업 없음, `UpgradeTreeView.BuildLineSegments()`가 계산한 좌표 목록을 코드로 넘겨받아 그린다 |
| 사용 규칙 | Unity UI Toolkit에 네이티브 라인 프리미티브가 없어 `generateVisualContent`+`Painter2D`로 직접 그린다(이 문서의 다른 어떤 화면도 쓰지 않는 유일한 사례). `pickingMode: Ignore`로 노드 클릭을 가로채지 않는다(필수 — 빠뜨리면 모든 노드 탭이 막힘). 목적지 노드 해금 시 밝고 굵게(`rgba(150,255,185,0.9)`, 6px), 미해금 시 흐리고 얇게(`rgba(150,255,185,0.28)`, 3px) — 값은 `--color-accent-glow`와 동일 색이지만 C# 리터럴로 직접 지정(USS 변수 참조 불가, Painter2D는 C# 레벨 API) |

### 21.3 ResearchPopup 노드 상세 컴포넌트

| 속성 | 값 |
|---|---|
| 목적 | 노드 클릭 시 뜨는 팝업 안에서 비용→효과→주의→구매 버튼→고급 정보 순서로 구매 의사결정에 필요한 정보를 위계대로 노출 |
| 사용 화면 | ResearchPopup(Overlay Panel, 11절) |
| 구성 요소 | `popup-cost`(Label) > `popup-effect-section`(`popup-section` + `popup-effect-list`) > `popup-warn-section`(`popup-section` + `popup-warn-list`) > `modal-footer`의 확정 버튼(11.1절 4갈래) > `popup-advanced`(`Foldout`, 기본 접힘) > `popup-advanced-bars` |
| `popup-cost` | 비용 텍스트("비용 N DNA" 또는 이미 해금 시 "연구 완료"), `--color-brand-gold`, `--font-size-lg` bold |
| `popup-bullet`/`popup-bullet--warn` | 효과/주의 불릿 한 줄. 효과는 `node.effects` 중 값이 0이 아닌 항목만("스탯명 ±N%p"), 주의는 severity/lethality **증가**시에만("발각 위험 증가"/"치사율 상승") — 두 섹션 다 내용이 없으면 컨테이너 자체를 `display:none`. 주의 불릿은 2.2절 severity 4색을 재사용하지 않고 `--color-brand-gold`로만 구분(20절 "Severity 색상 오용" 사례와 같은 실수 방지) |
| `popup-advanced`(Foldout) | UI Toolkit 표준 `Foldout` 컨트롤 최초 사용 사례. 감염성/심각성/치사율/약물저항 4종을 이 노드가 실제로 건드리는지와 무관하게 항상 전부 표시 — "현재 값"은 `WorldDataManager.CurrentPathogen`(실제 진행 중인 병원체 데이터, 데모 값 아님), "적용 후 값"은 `Mathf.Clamp01(현재값 + node.GetEffect(stat))` |
| `popup-bar-track`/`popup-bar-fill` | 트랙+채움 2줄(현재/적용후) 막대 — 15절 `population-bar__track`과 동일한 "트랙+채움" 문법 재사용, `--radius-sm` |

### 21.4 Hero Stat Tile

| 속성 | 값 |
|---|---|
| 목적 | 성과 대시보드의 핵심 4개 지표(INFECTED/DEATHS/CURE/EXTINCT)를 2×2 큰 숫자 타일로 강조 |
| 사용 화면 | CountryStatusPanel(GLOBAL STATUS CENTER) |
| 구성 요소 | `hero-stats`(2×2 wrap 컨테이너) > `hero-stat-tile`(폭 48%) > `hero-stat-tile__label` + `hero-stat-tile__value`(`--font-size-xl`) |
| 사용 규칙 | 값 색상은 14절 `data-value--*` severity 모디파이어를 그대로 얹어 재사용 가능(신규 색 도입 없음). 배경 없음(투명), 테두리 `--color-accent-glow`(full) — 10절 참고 |

### 21.5 Quick Stat Grid

| 속성 | 값 |
|---|---|
| 목적 | 국가 핵심 지표(국기/대륙/인구/감염자 등 11필드)를 2열 그리드로 5초 내 스캔 가능하게 배치 |
| 사용 화면 | CountryPopup(Bottom Sheet Compact) |
| 구성 요소 | `quick-stat-grid`(flex-wrap 컨테이너) > `quick-stat-cell`(폭 50%) > `quick-stat-cell__label` + `quick-stat-cell__value` |
| 사용 규칙 | "등급"성 필드(공항/항만/의료/국경 상태)는 `quick-stat-cell__value`에 13절 `badge-tag`를 추가 클래스로 결합한다 |

### 21.6 Distribution Row

| 속성 | 값 |
|---|---|
| 목적 | 국가를 4버킷(SAFE/WARNING/DANGER/COLLAPSE)으로 분류한 개수를 가로 4열로 표시 |
| 사용 화면 | CountryStatusPanel(Advanced Diagnostics — 국가 상태 분포, 의료 시스템 현황 2곳에서 동일 구조 재사용) |
| 구성 요소 | `distribution-row`(4열 균등폭) > `distribution-item`(`stat-chip` + `__label` + `__count`) |
| 사용 규칙 | `stat-chip`은 `Hud.uss` 정의(6×6px 색상 사각형)를 그대로 재사용. 색상은 2.2절 severity 4색을 `stat-chip--safe/--warning/--danger/--collapse`로 매핑 |

### 21.7 Segment Tab

| 속성 | 값 |
|---|---|
| 목적 | 3개 카테고리(전파/증상/적응) 전환용 상단 탭, 밑줄로 선택 상태 표시 |
| 사용 화면 | UpgradeTree 헤더(`upgrade-header__tabs`) |
| 구성 요소 | `.tab-button`(투명 배경, 테두리 없음, 하단 2px 투명) + `.tab-button--active`(하단 `--color-border-selected` 밑줄, 텍스트 `--color-brand-gold`) |
| 사용 규칙 | **이름 충돌 주의**: `Hud.uss`에도 동명의 `.tab-button`(action-strip 하단 버튼 — 배경/테두리가 있는 완전히 다른 9절 Button System 컴포넌트)이 존재한다. 서로 다른 UXML이 각자 로드해 런타임 충돌은 없으나, 신규 작업 시 두 컴포넌트를 혼동하지 않는다 |

### 21.8 Accordion (대륙별 접기/펼치기)

| 속성 | 값 |
|---|---|
| 목적 | 48개국 리스트를 6대륙 단위로 묶어 접기/펼치기 |
| 사용 화면 | CountryStatusPanel(48개국 목록) |
| 구성 요소 | `continent-section` > `continent-header`(화살표 + 라벨, hover 시 강조) + `continent-body`(펼침 시 국가 행 컨테이너, 접힘 시 컨트롤러가 `display:none`) |
| 사용 규칙 | 코너컷 부착 금지(8절 "수십 개 반복 리스트" 규칙), 좌측 accent-bar는 `--color-accent-glow`(상시 full — 과거 soft 기본값+hover 시 full 강조 방식이었으나 터치 기기에서 hover가 거의 안 걸려 폐기) — 대륙 헤더는 "국가 데이터"가 아니라 "UI 크롬"이라 2.2절 severity 4색 사용 금지 |

### 21.9 Pathogen Stat Row (MainMenu)

| 속성 | 값 |
|---|---|
| 목적 | 병원체 카드의 스탯 4종(전염력/중증도/치사율/내성) 막대 옆에 정확한 수치(%)를 병기하고, 막대 색을 스탯별로 분리해 정보 밀도와 판독성을 높인다 |
| 사용 화면 | MainMenu(`pathogen-card__stats-rows`) |
| 구성 요소 | `pathogen-card__stat-row` > `__stat-label` + `__stat-track`(`__stat-fill`) + `__stat-value`(신규) |
| `pathogen-card__stat-value` | `width:44px; flex-shrink:0; -unity-text-align:middle-right; font-size: --font-size-xs; color: --color-text-secondary` — 신규 토큰 없음, 라벨과 동일 크기·`data-value` 기본색 재사용. 텍스트는 `{value01:P0}`(13절/16절과 동일 포맷 관례) |
| `pathogen-card__stat-fill--*` | 4종 modifier: `--infectivity`(`--color-accent-glow`, 기존 유지) / `--severity`(`--color-status-infected`) / `--lethality`(`--color-status-danger`) / `--resistance`(`--color-status-info`) |

> **미확정 사항**: `--severity`/`--lethality`/`--resistance` modifier는 2.2절이 "국가 데이터
> 전용"으로 고정한 severity 4색을 병원체 스탯(국가 데이터 아님)에 재사용한다 — 이는 설계
> 이탈로 되돌리지도, 2.2절의 공식 예외로 승격하지도 않은 **잠정 상태**다. 결정 대기 항목은
> 부록 A 참고.

> **Population Bar**는 이미 15절 HUD Rules에 규격(높이 14px, `--radius-sm`, `flex-basis:0` +
> `flex-grow` 비율, healthy→infected→dead 고정 순서)이 문서화되어 있다 — 이 절에서 중복
> 등재하지 않는다.

### 21.10 CountrySelect 개발수준 색상 매핑 (신규 등재, 2026-07-17 UI Polish Audit)

| 속성 | 값 |
|---|---|
| 목적 | 국가 리스트에서 의료 수준(`developmentLevel`)을 별도 배지 없이 두 지점(리스트 행 좌측바, 상세 패널 값 텍스트)의 색으로 즉시 판독하게 한다 |
| 사용 화면 | CountrySelect(`country-row`, MainMenu.uss 공유 + `CountrySelect.uss` 확장) |
| 구성 요소 | (1) `country-row--dev-high/mid/low` — `country-row` 좌측 4px accent-bar 색 modifier(10절). (2) 상세 패널 "의료 수준" `data-row`의 `data-value--info`/`data-value--danger`(14절 severity 모디파이어 재사용, Mid는 모디파이어 없음=기본색) |
| 색상 매핑 | High(선진국) = `--color-status-info`(accent-bar) / `data-value--info` · Mid(개발도상국) = `--color-text-secondary`(accent-bar, severity색 아님) / 모디파이어 없음 · Low(저개발국) = `--color-status-danger`(accent-bar) / `data-value--danger` |
| 사용 규칙 | `CountrySelectController.DevAccentClass()`/`DevValueClass()`가 부여. badge-tag(13절)가 아니라 accent-bar+data-value 조합이다 — 새 색상 축을 만들지 않고 기존 severity 4색 중 2개(info/danger)만 재사용, Mid는 의도적으로 중립색(강조 없음) |

---

## 부록 A. 미확정 정책 항목 및 코드 정리 대상

> v1→v2 개정 요약과 Direction C 리디자인 라운드별 이력은 `Docs/DevLog.md`로 이관됐다
> ("DESIGN.md v1→v2 개정 이력"/"Direction C(Data Wall Terminal) 진행 이력" 항목, 2026-07-17) —
> 이 문서는 현재 상태만 서술한다는 원칙(0절)에 따라 완료된 변경의 라운드별 서술은 본문에
> 남기지 않는다. 아래는 아직 결정되지 않았거나 실행되지 않은 항목만 추적한다.

**미확정 정책 항목**:
- **severity 축의 병원체 스탯 재사용**(21.9절 Pathogen Stat Row) — `pathogen-card__stat-fill--
  severity/lethality/resistance`가 2.2절이 "국가 데이터 전용"으로 고정한 severity 4색을
  병원체 스탯에 재사용한다. (a) 2.2절에 예외 각주를 추가해 공식화하거나, (b) 다른 축(예:
  브랜드색 계열)으로 되돌리는 두 갈래 중 아직 결정되지 않았다. 결정 전까지 코드는 유지하되
  본문 원칙(2.2절)은 개정하지 않는다.
- **UpgradeTree 캔버스의 5절/6절 이탈**(17절/21.1절) — `tree-node`의 `border-radius`(24px/12px,
  5절 공식 3값 밖)와 `tree-node--merge`/`--final`의 테두리 굵기(3px/4px, 6절의 기존 의미와
  충돌)가 아직 공식 예외로 승격되지 않았다. 캔버스가 실기기에서 최종 검증된 뒤 판단.

**코드 정리 대상(문서 범위 밖, 별도 승인 필요)**:
- `CountryStatusPanel.uss`의 `.status-row .data-row` 규칙 — 더 이상 어떤 자식 요소도 매치하지
  않는 죽은 코드(18절 참고).
- `Theme.uss`의 `--color-surface-mint` 토큰 — 미사용(2.5절 참고), 삭제 여부 미결정.
- `Theme.uss`의 `--color-node-transmission/symptom/ability`(2.3절) — 소비처 0건, 삭제 여부 미결정.
- `Theme.uss`의 `--color-bg-scrim-strong`/`--color-bg-scrim-soft`(2.8절) — HUD 5개 컴포넌트가
  전부 `--color-bg-panel`을 쓰면서 소비처 0건(2026-07-17 UI Polish Audit로 확인), 삭제 여부
  미결정.
- `UpgradeTree.uss`의 `.data-value--locked/available/active/maxed`(14절) — 소비처 0건.
- `TacticalModalController.AddRow()`/`AddSectionCaption()`(11절) — 전체 코드베이스에서 호출부
  0건(ResearchPopup의 마지막 호출이 Commit 4에서 제거됨). `CountryPopupController`도 자체
  named-Label 패턴을 쓰고 이 API는 쓰지 않는다 — 계약 자체를 유지할지 재검토 필요.
