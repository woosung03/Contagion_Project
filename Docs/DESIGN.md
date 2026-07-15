# Contagion Design System v2 (DRAFT)

> **이 문서의 지위**: 이것은 초안(DRAFT)이다. `Docs/DESIGN.md`(현재 Source of Truth)를 대체하지
> 않았다 — 검토 후 승인되면 `Docs/DESIGN.md`를 이 문서 내용으로 교체하는 별도 작업이 필요하다.
> 이 문서는 철학 문서가 아니라 **구현 명세서**다. "적절히"/"상황에 따라"/"권장" 같은 표현은
> 의도적으로 배제했다 — 모든 규칙은 수치·표·계약으로 표현한다.

---

## 0. Document Contract

**목적**: 이 문서 하나만 읽고 HUD/CountryPopup/CountryStatus/UpgradeTree/Ranking/MainMenu/
CountrySelect 및 향후 신규 화면을 동일한 품질·스타일로 구현할 수 있어야 한다.

**Source of Truth 정의**:
- 이 문서(`Docs/DESIGN.md`, 승인 후)가 디자인 토큰·컴포넌트 계약·Do/Don't의 유일한 정본이다.
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
칠하지 않는다 — 2.5절 노드 상태 축과 위치를 분리해 같은 카드 안에서 두 축이 충돌하지 않게
한다.

### 2.4 노드 상태 축 (Severity와 별개 — 브랜드색 재사용)

| 상태 | 텍스트/테두리 색 | 배경(있는 경우) |
|---|---|---|
| Locked | `--color-text-tertiary` | 없음, `opacity: 0.7` |
| Available | `--color-accent-glow` | 없음 |
| Active | `--color-brand-dna` | `rgba(120, 220, 140, 0.12)` |
| Maxed | `--color-brand-gold` | `rgba(255, 210, 90, 0.10)` |

### 2.5 구조용 발광색 (Structural Glow — 콘텐츠 색으로 사용 금지)

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-accent-glow` | `rgb(150, 255, 185)` | 패널 테두리, 코너컷, 캡션, 포커스 |
| `--color-accent-glow-soft` | `rgba(150, 255, 185, 0.35)` | 약한 테두리(기본 tactical-panel 테두리) |
| `--color-grid-line` | `rgba(150, 255, 185, 0.12)` | data-row 구분선, 격자선 |

### 2.6 Text Colors

| 토큰 | 값 | 용도 |
|---|---|---|
| `--color-text-primary` | `rgb(255, 255, 255)` | 헤드라인/강조 |
| `--color-text-secondary` | `rgb(210, 210, 210)` | 값 텍스트 기본 |
| `--color-text-tertiary` | `rgb(180, 180, 180)` | 라벨/캡션/잠금 상태 |
| `--color-text-info` | `rgb(180, 220, 255)` | 정보성 텍스트(2.2절 `status-info`와 다른 토큰 — 혼동 금지) |

### 2.7 Surface Ladder (배경 alpha 계단)

| 토큰 | 값 | 사용처 |
|---|---|---|
| `--color-bg-root` | `rgba(8, 8, 16, 0.97)` | Fullscreen 화면 바닥(MainMenu/CountrySelect) |
| `--color-bg-panel-alt` | `rgba(10, 10, 20, 0.92)` | Fullscreen "패널형" 화면(UpgradeTree) |
| `--color-bg-panel` | `rgba(15, 15, 25, 0.95)` | Bottom Sheet — Compact(CountryPopup), 팝업류 |
| `--color-bg-panel-strong` | `rgba(9, 9, 16, 1)` | Bottom Sheet — Extended(CountryStatus) 전용, 완전 불투명 |
| `--color-bg-scrim` | `rgba(0, 0, 0, 0.85)` | 엔딩 스크림 |
| `--color-bg-scrim-strong` | `rgba(0, 0, 0, 0.75)` | HUD 하단바(graph-panel/action-strip) |
| `--color-bg-scrim-soft` | `rgba(0, 0, 0, 0.55)` | HUD 상단바(event-dock) — *실측: 실제로는 `--color-bg-panel` 사용 중, 3.x 정리 대상(18절 참고)* |
| `--color-surface` | `rgba(255, 255, 255, 0.08)` | 카드/행 기본 배경(tree-node, pathogen-card 등) |
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
| `--font-size-caption-2xs` | 9px | bold | 0.5px | badge-tag 최소 텍스트(CountryPopup) *[신규 등재 — 기존 하드코딩값을 토큰화]* |
| `--font-size-caption-xs` | 10px | bold | 1px | world-status-frame__caption 등 초소형 유틸 라벨 *[신규 등재]* |
| `--font-size-caption` | 11px | bold | 1px(`--tracking-caption`) | tactical-caption, lab-caption, section-caption, research-row__code *[신규 등재 — 4개 이상 화면에서 이미 반복 사용 중이던 값을 정식 토큰으로 승격]* |
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
| `19px` | MainMenu `mainmenu-subtitle` | `--font-size-lg`(20px) |
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
| `--radius-sm` | 3px | 코너컷이 없는 "판독 프레임" 계열 전용: population-bar__track, graph-frame, world-status-frame, branch-row, research-row, badge-tag | Hud.uss/UpgradeTree.uss/CountryPopup.uss 6곳 확인 |
| `--radius-md` | 6px | **메뉴 카드 예외 전용.** `country-row`(MainMenu/CountrySelect 화면, 7절 Fullscreen 분류) 단 1곳 — 각짐 원칙(Design Principles 2)의 의도된 예외 | MainMenu.uss 1곳만 확인 |
| `--radius-lg` | 8px | **재도입 금지.** `RankingPanel.uss`에서 `border-top-left/right-radius`로 실제 시도됐으나 코너컷과 충돌해 제거된 이력이 있다(해당 파일 주석에 기록) — "미사용"이 아니라 "시도 후 폐기됨". 코너컷이 붙는 어떤 패널에도 다시 쓰지 않는다 | RankingPanel.uss에 제거 이력 주석 확인, 현재 활성 사용처 없음 |

**Do**: 새 패널/버튼/모달/Bottom Sheet는 항상 0. 코너컷 없는 판독 프레임만 `--radius-sm`.
MainMenu/CountrySelect의 `country-row`(메뉴 카드)만 `--radius-md`.
**Don't**: `--radius-lg`를 재도입하지 않는다(과거 시도 후 코너컷과 충돌해 제거된 이력 —
위 표 참고). 임의의 radius 값(`2px`, `4px` 등)을 하드코딩하지 않는다 — `country-row__flag`의
`2px` 하드코딩은 레거시 예외이며 신규 복제 금지.

---

## 6. Stroke System — "두께는 굵기가 아니라 의미다"

같은 두께가 서로 다른 의미를 표현하지 않는다. 항상 아래 5단계 중 하나를 선택한다.

| 토큰 | 값 | 의미 | 실사용 예 |
|---|---|---|---|
| `--stroke-hairline` | 1px | 구조(기본 테두리) | tactical-panel, data-row 하단선, research-row 기본 |
| `--stroke-active` | 2px | 진행 상태(전체 테두리에만) | research-row--active/--maxed |
| `--stroke-sub-accent` | 2px | 보조 이분 상태(좌측 바에만) | data-row--open/--closed |
| `--stroke-selected` | 3px | 선택 상태 | research-row--selected, branch-row--selected, country-row--selected, pathogen-card--selected |
| `--stroke-accent-bar` | 4px | 카테고리 강조(좌측 바) | tree-node/research-row 좌측 accent bar, status-row 좌측 severity bar |

**주의**: `--stroke-active`(2px, `border-width`)와 `--stroke-sub-accent`(2px,
`border-left-width`)는 값은 같지만 **적용 프로퍼티가 다르다** — 전체 테두리 자리의 2px는
항상 "진행 상태", 좌측 바 자리의 2px는 항상 "보조 이분 상태"를 의미한다. 같은 프로퍼티
자리에서 두 의미를 혼용하지 않는다.

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
| `tree-node`/`research-row` | `--color-surface` | 1px `--color-border` + 좌측 4px 카테고리색 | `--radius-sm`(research-row만) | 상태 4단계는 2.4절 |
| `pathogen-card` | `--color-surface` | tactical-panel 상속(코너컷 4개) | 0 | Fullscreen 메뉴 카드 |
| `country-row` | `--color-surface` | 2px `--color-border` + 좌측 4px | `--radius-md`(6px, 5절 예외) | MainMenu/CountrySelect(7절 Fullscreen) |
| `status-row` | `--color-surface-soft` | 좌측 4px severity색(2.2절) | 0 | CountryStatus 48개국 리스트 |
| 선택 상태(공통) | `--color-surface-selected` | 3px `--color-border-selected`(골드) | 각 카드 규칙 유지 | `--stroke-selected` |

---

## 11. Modal System (현재 `TacticalModalController` 기준)

**계약(고정 UXML 이름)**: `modal-root`(class: `tactical-panel` + 분류에 맞는 위치 클래스),
`modal-title`(Label), `modal-close`(Button), `modal-rows`(class: `detail-rows`, 컨트롤러가
`data-row`를 동적으로 Add), `modal-footer`(비어있어도 됨, 액션 버튼 슬롯).

**API 계약**: `Show(string title)`(display:Flex + 타이틀 대입), `Hide()`(display:None),
`ClearRows()`, `AddRow(label, value, valueClass?, rowClass?)`, `AddSectionCaption(text)`.

**Do**: 신규 Overlay Panel/Modal은 이 계약을 그대로 상속한다(`TacticalModalController` 상속,
새 베이스 클래스 만들지 않음).
**Don't**: `modal-rows`/`AddRow` 계약을 그리드형 UI(13절 Badge/14절 Data Row 이외의 구조)로
억지로 확장하지 않는다 — 그런 화면은 자체 named-Label 캐싱 패턴(12절 CountryPopup 참고)을
쓴다.

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
| **top 프리셋(신규 표준화 — 기존 3%/42% 임의값 대체)** | **Dense**(대시보드형, 콘텐츠 많음): `top: 5%` / **Compact**(확인·액션형, 콘텐츠 적음): `top: 40%` — 이 둘 중 하나만 선택, 새 % 금지 |
| **Width** | 100% |
| **Safe Area** | 상단 SafeArea 인셋을 `top` 프리셋에 더한다(신규 규정) |
| **Header** | `tactical-panel__header` 가로 배치(타이틀 + 닫기 28px) |
| **Content** | `ScrollView`(`flex-grow:1; flex-basis:0; overflow` 상위 컨테이너는 `overflow:hidden` 필수 — 세로 오버플로우 버그 재발 방지, 20절 참고) |
| **Footer** | 없음(콘텐츠 내부 버튼으로 대체) |
| 배경 | Dense: `--color-bg-panel-strong`(완전 불투명) / Compact: `--color-bg-panel` |
| radius | 0 |

**Do**: CountryStatus(콘텐츠 많음) = Dense, Ranking(콘텐츠 적음) = Compact로 이미 실측과
일치한다 — 두 화면 모두 마이그레이션 없이 새 표준에 그대로 부합.
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

### Severity 모디파이어(4종 고정 — `CountryPopupController.ApplySeverityClass()`가 배타 적용)

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

## 14. Data Row System (HUD/CountryStatus/UpgradeTree 공통)

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

### 노드 상태 모디파이어(UpgradeTree 전용 축 — Severity와 교차 금지)
`.data-value--locked`(`--color-text-tertiary`) / `.data-value--available`(`--color-accent-glow`)
/ `.data-value--active`(`--color-brand-dna`) / `.data-value--maxed`(`--color-brand-gold`)

**소비처**: HUD(없음, 직접 소비 안 함) / CountryStatus(world-summary-rows, ranking-infected/dead)
/ UpgradeTree(detail-rows) — 3개 화면이 물리적으로 동일한 클래스를 공유해야 한다(로컬 재정의
금지, 18절 참고).

---

## 15. HUD Rules

| 컴포넌트 | 높이/폭 | 배경 | 테두리 |
|---|---|---|---|
| `resource-strip` | 34px | `--color-bg-panel` | 좌우하단 1px `--color-accent-glow-soft` |
| `event-dock` | 폭 168px, 좌상단 절대배치 | `--color-bg-panel` | 1px `--color-accent-glow-soft`, 코너컷 4개 |
| `population-bar__track` | 높이 14px | `--color-surface-soft` | 1px, radius `--radius-sm` |
| `graph-panel` 프레임 | 104×52px | `--color-surface-soft` | 1px, radius `--radius-sm` |
| `action-strip` 버튼 | `--touch-target-min`(48px) | 9절 Button System 그대로 |

3세그먼트 비율형 데이터(population-bar)는 항상 healthy→infected→dead 고정 순서,
`flex-basis:0` + `flex-grow` 비율로만 표현(절대 픽셀 계산 금지). 추세형(graph-panel)은
프레임 고정(104×52px) + 기준선 1개(`--color-accent-glow-soft`) + 보조 격자 2개(25%/75%,
`--color-grid-line`).

---

## 16. Country Status Rules

- 분류: Bottom Sheet — Extended / Dense(`top:5%`, 12.2절).
- Hero Stats: 2×2 타일, 타일 폭 48%, 값 폰트 `--font-size-xl`(24px).
- 대륙 아코디언: 코너컷 금지(리스트 반복 요소), 좌측 `--color-accent-glow-soft` accent-bar만
  사용 — 대륙 헤더는 "국가 데이터"가 아니라 "UI 크롬"이라 Severity 4색 사용 금지(2.2절 Don't).
- 48개국 리스트 행(`status-row`): 좌측 4px severity accent-bar(2.2절 4색), radius 0.
- `overflow: hidden` + `flex-direction: column`을 status-root에 항상 명시(세로 오버플로우로
  콘텐츠가 배경 밖으로 새어나가 지도가 비치는 버그 재발 방지, 20절 사례 참고).

---

## 17. UpgradeTree Rules

- 분류: Fullscreen(`--color-bg-panel-alt`).
- 탭 3개 고정, 높이 `--touch-target-min`(48px), 선택 강조는 새 색상 없이 하단 2px
  `--color-border-selected`(골드) 밑줄로만 표현.
- `research-row`: 좌측 4px 카테고리색(2.3절) + 상태 4단계(2.4절, 테두리 전체+배경)는 위치
  분리 — 카테고리는 좌측 바, 상태는 전체 테두리/배경.
- `branch-board`: 진행률 바는 population-bar와 동일 기법(`flex-basis:0` + `flex-grow`),
  트랙 radius `--radius-sm`.
- `detail-panel`: `position:relative`(코너컷 기준점), radius 0(코너컷과 충돌 방지).

---

## 18. Component Registry (실제 USS 파일 기준)

| 파일 | 역할 | 이 문서와의 관계 |
|---|---|---|
| `Theme.uss` | 전 토큰(`:root`) 정의 | **1차 Source of Truth 구현체** — 이 문서 2~6절과 값이 반드시 일치 |
| `Tactical.uss` | 공용 컴포넌트: tactical-panel/corner-cut/data-row | **2차 Source of Truth 구현체** — 8/14절 |
| `Hud.uss` | resource-strip/event-dock/population-bar/graph-panel/action-strip | 15절 소비처. `tactical-panel`/`corner-cut` **로컬 재정의 존재 — 제거 대상**(20절) |
| `CountryPopup.uss` | Bottom Sheet(Compact), quick-stat-grid, badge-tag | 12.1/13절 최초 구현체 — badge-tag의 정본 |
| `CountryStatusPanel.uss` | Bottom Sheet(Extended/Dense) | 12.2/16절 소비처 |
| `RankingPanel.uss` | Bottom Sheet(Extended/Compact) | 12.2절 소비처 |
| `UpgradeTree.uss` | Fullscreen, research-row/branch-board | 17절 소비처. `tactical-panel`/`corner-cut`/`data-row` **로컬 재정의 존재 — 제거 대상**(20절) |
| `MainMenu.uss` | Fullscreen, pathogen-card/country-row/detail-panel | 10절 소비처, `--radius-md` 유일 사용처(5절) |
| `CountrySelect.uss` | MainMenu.uss 확장(개발수준 accent bar 색만 추가) | 10절 소비처 |
| `ResearchPopup.uss` | Overlay Panel(중앙 모달) | 11절 Modal 계약 소비처 |
| `EndingScreen.uss` | Fullscreen(Debrief) | 9절 Button System 소비처, 과거 실패 사례 발생 파일(20절) |

---

## 19. Do

- 신규 패널은 항상 7절 4분류 중 하나로 먼저 분류하고, 해당 절의 정확한 수치를 그대로 쓴다.
- 신규 수치 판독은 14절 Data Row 또는 13절 Badge System을 재사용한다 — 새 판독 컴포넌트를
  만들기 전에 이 둘로 표현 가능한지 먼저 검토한다.
- 카테고리색은 좌측 accent-bar(4px), 상태색은 테두리 전체+배경 — 위치를 분리한다.
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
- **Local Component 중복 생성**: `tactical-panel`/`corner-cut`/`data-row`가 `Tactical.uss`
  승격 이후에도 `Hud.uss`/`UpgradeTree.uss`에 로컬로 재정의된 채 남아있다(18절 표에 명시) —
  같은 값을 이중 정의하는 것 자체가 버그는 아니지만, 두 곳이 어긋나기 시작하면 화면마다 다른
  결과가 나오는 근본 원인이 된다. **정리 완료 전까지 이 두 파일의 tactical-panel/corner-cut을
  참고하지 않는다 — 항상 `Tactical.uss` 것을 기준으로 삼는다.**
- **Stroke 의미 충돌**: 좌측 accent-bar(보조 이분 상태)에 `--stroke-selected`(3px)를 쓰다가
  "선택 상태"와 굵기가 겹쳐 혼동된 사례, `--stroke-active`(2px)를 좌측 바에 썼다가 "보조
  이분 상태"와 혼동된 사례 — 둘 다 실제로 발생해 수정됨. 6절 표의 프로퍼티별 의미를 정확히
  지킨다.
- **Radius 임의 도입**: `country-row__flag`에 `2px`를 하드코딩한 사례처럼, 5절 표에 없는
  radius 값을 새로 만들지 않는다.

---

## 부록 A. 기존 DESIGN.md 대비 변경 요약

| 항목 | v1(현재) | v2(이 초안) |
|---|---|---|
| 문서 성격 | 철학 설명 위주 | 수치/표/계약 위주 |
| Typography | 크기 9개 나열만 | 12개(캡션 3단계 신규 등재) + weight/letter-spacing/usage 전부 명시(line-height는 UI Toolkit 미지원 확인 후 제외) |
| Spacing 베이스 서술 | "8px" (실제는 4px 배수) | "4px" 로 정정 |
| Radius | 3단계 스케일(대부분 미사용) | 실사용 기준 3개 값 + 각각의 정확한 용도 고정, `--radius-lg`는 시도 후 제거된 이력 명시(재도입 금지) |
| Source of Truth 목록 | 4개 파일만 | 11개 파일 전부(18절 Component Registry) |
| Bottom Sheet | 개념 없음, 화면마다 임의 % | Compact/Extended 2변형, 정확한 anchoring 표(12절) |
| Modal | 문서화 안 됨 | `TacticalModalController` 계약 명문화(11절) |
| Badge System | 없음 | CountryPopup `badge-tag` 기준 정식 컴포넌트화(13절) |
| UI Density Modes(3분류) | 있음, Country Dock 등 최신성 저하 | 폐기 — 7절 Layout System(4분류)으로 대체 |
| Do/Don't | 일반론 | 실제 발생한 실패 사례 기반(20절) |
