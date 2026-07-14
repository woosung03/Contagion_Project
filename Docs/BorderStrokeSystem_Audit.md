# Border / Stroke System Audit — Contagion Project Tactical UI

> 조사 전용 문서. 코드/USS/DESIGN.md 수정 없음. Color System은 다루지 않는다(불가피하게
> 두께 인식에 영향을 주는 지점에서만 알파값을 "진단 근거"로 인용하며, 색상 변경은
> 제안하지 않는다).

**조사 범위**: `Theme.uss`, `Tactical.uss`, `Hud.uss`, `UpgradeTree.uss`,
`CountryStatusPanel.uss`, `CountryPopup.uss`, `MainMenu.uss`, `CountrySelect.uss`,
`RankingPanel.uss`, `ResearchPopup.uss`, `EndingScreen.uss` (11개 전수 읽음) + 각 화면
`.uxml`의 `<Style src>` 로드 순서 + 클래스 조합(`AddToClassList`) 대조 + `PanelSettings.asset`
스케일 설정.

---

## 1. 현재 Border Width 사용 현황 (실측)

USS 파일 전체에서 `border(-*)-width` 선언을 전수 집계한 원값 분포:

| 값 | 등장 횟수 | 대표 선택자 |
|---|---|---|
| `1px` | 29 | `tactical-panel`, `data-row`(bottom), `event-dock`, `country-dock`, `population-bar__track`, `world-status-frame`, `tab-button`(Hud), `popup-footer-button`, `ending-button`, `branch-row__progress`, `research-row`(기본), `global-status-banner` 등 |
| `2px` | 5 | `country-row`(기본, MainMenu.uss), `research-row--active/--maxed`, `branch-row--selected`, `tab-button--active`(UpgradeTree, bottom만) |
| `3px` | 5 | `pathogen-card--selected`, `country-row--selected`, `research-row--selected`, `data-row--open/--closed`(left) |
| `4px` | 5 | `accent-bar-row`, `section-caption`(left), `research-row`(left), `status-row`(left) |
| `0px` | 1 | `tab-button`(UpgradeTree, 기본 — 위/좌/우 0, 아래만 2px) |

여기에 "border 속성은 아니지만 실질적으로 선 두께로 기능하는" 요소가 별도로 존재한다:

| 요소 | 두께 | 구현 방식 |
|---|---|---|
| `corner-cut`(4종) | **2px**(길이 13px) | `border`가 아니라 `width:13px; height:2px;`의 회전된 배경 블록 |
| `resource-strip__sep` | 1px | `width:1px`의 배경 블록(세로 구분선) |
| `stat-graph-item__baseline`/`__gridline` | 1px | `height:1px`의 절대배치 블록 |

**요약**: 실제 사용 중인 "선 두께"는 `0 / 1 / 2 / 3 / 4px` 5종 + 코너컷 전용 `2px` 블록까지
사실상 **6개 값**이 혼재한다. `DESIGN.md`에는 이 값들을 모은 단일 스케일 표가 없고,
컴포넌트별 서술 안에 개별적으로("1px accent-glow-soft", "2px" 등) 흩어져 있다.

---

## 2. Corner Cut 시스템 분석

`corner-cut`은 `Tactical.uss` / `Hud.uss` / `UpgradeTree.uss` **3개 파일에 각각 독립적으로
정의**되어 있다(`width:13px; height:2px; background-color: accent-glow; ±45deg`). 값 자체는
3곳 모두 바이트 단위로 동일 — 의도적 복제이지 지금 당장의 수치 불일치는 아니다.

다만 이 복제 구조는 실제로 이미 한 번 드리프트를 냈다: `Tactical.uss`의 `.tactical-panel`은
`border-radius: 0`을 포함하지만, `UpgradeTree.uss`가 로컬로 들고 있는 동일 이름의
`.tactical-panel`에는 `border-radius: 0`이 빠져 있다(`UpgradeTree.uss` 151~154행). 코너컷의
"완전한 각짐" 전제가 이 화면에서만 이론상 깨질 수 있는 상태다 — Border System 파일 3중화가
**"두께"가 아니라 "다른 속성"에서 먼저 사고를 낸 사례**이며, 두께 통일 작업을 미루면 같은
경로로 두께도 드리프트될 수 있다는 근거로 인용할 만하다.

더 중요한 구조적 지점: **코너컷(2px, 100% 불투명 `accent-glow`)과 그 코너컷이 붙는 패널의
실제 테두리(1px, 35% 불투명 `accent-glow-soft`)는 두께도 다르고 불투명도도 다르다.**
같은 패널 위에서 "귀퉁이 장식선"이 "패널 테두리 그 자체"보다 물리적으로 2배 굵고 훨씬
진하다 — 시각적으로 "테두리는 흐릿한데 귀퉁이만 도드라진다"는 인상을 만든다. 이 알파값
차이는 색상 축 조정 대상이 아니라 여기서는 **두께 인식에 개입하는 진단 근거**로만 언급한다.

---

## 3. Divider / Separator 분석

`data-row`(`Tactical.uss`/`Hud.uss`(`country-dock__row`)/`UpgradeTree.uss` 3곳 로컬 정의)는
전부 `border-bottom-width: 1px` + `--color-grid-line`로 **완전히 일치**한다 — 이 시스템 안에서
가장 일관성이 좋은 부분이다. `resource-strip`/`event-dock__header`/`country-dock__header`/
`popup-donut-row`/`popup-description`의 하단 구분선도 전부 1px + grid-line 계열로 동일하다.

예외 하나: `graph-panel`의 `border-top-width: 1px`은 색상이 `--color-grid-line`이고,
`action-strip`의 `border-top-width: 1px`은 색상이 `--color-accent-glow-soft`다. 둘 다 HUD
하단 바 경계선인데 두께는 같지만(1px) 진하기가 다르다 — 색상 축 문제라 이 보고서
범위에서는 "존재만 기록"한다.

좌측 accent-bar 계열(`accent-bar-row`=4px, `section-caption`=4px, `research-row`=4px,
`status-row`=4px)은 전부 4px로 일치하지만, **`data-row--open`/`data-row--closed`(둘 다
`Tactical.uss`)만 3px**다. 같은 "좌측 강조 바" 언어를 쓰면서 유일하게 다른 값 — 이 시스템
안에서 가장 명확한 수치 불일치 사례 중 하나다.

---

## 4. Button Border 분석

가장 심각한 발견은 여기다: **`.tab-button` 클래스명이 `Hud.uss`와 `UpgradeTree.uss`에
독립적으로 정의**되어 있는데, 두 정의가 완전히 다른 스트로크 설계를 쓴다.

| | `Hud.uss` `.tab-button` (하단 액션 바) | `UpgradeTree.uss` `.tab-button` (상단 탭 선택자) |
|---|---|---|
| 테두리 | 4방향 `border-width: 1px` | `border-width: 0`(전체) + `border-bottom-width: 2px`만 |
| 색상 | `accent-glow`(주 액션) / `border`(보조) | 기본 `transparent`, `--active`일 때만 `border-selected` |
| 성격 | "박스형" 버튼(사방 테두리) | "밑줄형" 탭(하단만) |

두 화면은 `Hud.uxml`/`UpgradeTree.uxml`이 각각 별도 UIDocument라 실제 런타임 캐스케이드
충돌은 없다(각자 로드하는 `.uss`가 다름). 하지만 **동일한 클래스명이 완전히 다른 두께
문법을 갖는 것 자체가 유지보수 리스크**다 — 향후 두 화면을 참고해 세 번째 탭형 컴포넌트를
만들 때 이름을 그대로 재사용하면 어느 쪽 문법을 따라야 하는지 코드만 봐서는 알 수 없다.

그 외 액션 버튼류(`popup-footer-button`, `ending-button`, `tab-button--secondary`)는 전부
1px로 일치한다 — 버튼 두께 자체는 (탭 버튼 예외를 빼면) 비교적 일관적이다.

---

## 5. 패널 Border 분석

`tactical-panel`을 상속/조합하는 패널(`popup-root`, `ranking-root`, `detail-panel`,
`branch-board`, `ending-stats-panel`, `ending-score-panel`, `status-root`, `pathogen-card`)은
전부 `.uxml`에서 `class="... tactical-panel"`로 명시 조합되어 있고, 전부 1px
accent-glow-soft를 상속받는다 — 이 계열은 실제로 일관적이다.

문제는 **`tactical-panel`을 쓰지 않고 독립적으로 1px 테두리를 재선언한 컴포넌트가
10곳 이상**이라는 점이다(`event-dock`, `country-dock`, `world-status-frame`,
`population-bar__track`, `stat-graph-item__graph-frame`, `global-status-banner`,
`stat-label--mortality--active` 등). 지금은 값이 우연히 전부 1px로 일치하지만, 이는
"공용 컴포넌트를 상속해서" 일치하는 게 아니라 "각자 타이핑한 숫자가 우연히 같아서"
일치하는 상태다. `Theme.uss`/`Tactical.uss`에 두께를 변수화한 토큰이 없기 때문에(6절 참고)
누군가 한 곳만 조정해도 나머지 9곳은 그대로 남는다.

**가장 눈에 띄는 개별 사례** — `.country-row`(`MainMenu.uss`, `CountrySelect.uss`에서
공유): `tactical-panel`을 쓰지 않고 독자적으로 `border-width: 2px`를 선언한다. 코드 주석에
그 이유가 명시되어 있다:

> "border-left-width는 이 rule 안에서 override — accent-bar-row에 맡기면 로드 순서상
> 이 shorthand가 나중에 적용되어 4px accent bar가 2px로 눌려버린다"

즉 2px는 **디자인 의도로 선택된 두께가 아니라 캐스케이드 버그를 피하기 위한 임시
값**이다. 반면 바로 옆 메뉴 화면의 `.pathogen-card`는 같은 "선택 가능한 리스트 카드"
역할인데 `tactical-panel`(1px)을 그대로 쓴다. 결과적으로 **거의 동일한 역할의 두 카드
컴포넌트가 기본 테두리 1px과 2px로 서로 다르게 렌더링**된다 — 사용자가 보고한 "화면마다
선 두께가 다르게 보인다"를 가장 직접적으로 설명하는 사례.

선택(`--selected`) 상태 두께도 갈라진다: `pathogen-card--selected`/`country-row--selected`/
`research-row--selected`는 3px로 일치하지만, `branch-row--selected`(`UpgradeTree.uss`
Research Database v2 브랜치 보드)만 2px다 — "선택 상태 = 3px"이라는 코드베이스 관행에서
벗어난 유일한 예외.

---

## 6. 모바일 관점 평가

`PanelSettings.asset`은 `m_ScaleMode: 2`(Scale With Screen Size), `m_ReferenceResolution:
480×1040`으로 설정되어 있다. 타겟 기기(갤럭시 S25 울트라, 1440×3120)와 비교하면 정확히
**3.0배 스케일**이 걸린다 — 즉 USS `1px`는 실제로 물리 픽셀 약 3px로 렌더링된다.
숫자만 보면 "1px가 실기기에서 아예 안 보일 정도로 얇다"고 보긴 어렵다(3px는 일반적인
하이라인 범위 안).

그럼에도 "얇아 보인다"는 체감이 나오는 이유는 두께보다 **불투명도**일 가능성이 높다.
현재 대부분의 구조용 테두리(`tactical-panel` 포함)는 `--color-accent-glow-soft`
(35% 알파)를 쓴다. 3배 스케일로 물리 두께는 확보되어도, 35% 알파는 갤럭시 S25 울트라급
고밀도 디스플레이(500ppi 이상)에서 얇은 선일수록 안티에일리어싱과 겹쳐 시각적으로
더 흐리게 지각된다 — "두께"와 "존재감"이 분리되는 지점이다. 이번 조사는 색상을 다루지
않으므로 결론을 내리지 않지만, **두께만 올리는 조치로는 체감 개선이 제한적일 수 있고,
알파 문제가 남아있다면 별도 후속 조사가 필요**하다는 점은 기록해 둔다.

추가로 실기기 관점에서 확인된 취약점: `.ending-score-panel`은 `border-color:
var(--color-brand-gold)`만 선언하고 자체 `border-width`가 없다 — `tactical-panel` 조합에
의존해 1px을 상속받는 구조다(`.uxml`에서 확인됨). 의도대로 동작은 하지만, 이런 "너비 없는
색상 오버라이드"가 반복되면 향후 어느 한 컴포넌트가 `tactical-panel` 클래스를 빼먹는
순간 테두리 자체가 사라지는(0px) 리스크가 있다 — 지금 당장의 버그는 아니나 취약한 패턴.

---

## 7. 일관성 평가

디자인 시스템 수준에서 보면 **부분적으로 일관, 전체적으로는 미검증 상태**다.

- **일관적인 축**: hairline divider(1px, `data-row`/헤더 하단선), 액션 버튼(1px, 탭 버튼
  예외 제외), accent-bar(4px, `data-row--open/closed` 예외 제외), `tactical-panel` 상속
  계열(1px, `.uxml`에서 명시적으로 조합된 8개 패널).
- **불일치 축**: `country-row` 기본 테두리(2px, 유일하게 다른 "카드" 컴포넌트),
  `branch-row--selected`(2px, "선택=3px" 관행 이탈), `data-row--open/closed`(3px,
  "accent-bar=4px" 관행 이탈), `.tab-button` 클래스명 충돌(같은 이름, 완전히 다른 두께
  문법 2종).

핵심 원인은 값 자체보다 **토큰화 부재**다. `Theme.uss :root`에는 색상(`--color-*`),
간격(`--space-*`), 반경(`--radius-*`), 폰트 크기(`--font-size-*`), 자간(`--tracking-*`)이
전부 CSS 변수로 정의되어 있는데, **두께(`border-width`)만 유일하게 변수화되지 않고
8개 파일에 리터럴 숫자로 흩어져 있다.** 색상/간격/폰트는 "한 곳만 고치면 전체 반영"되는
구조지만, 두께는 화면마다 개발자가 그때그때 숫자를 타이핑한 결과다 — 이번 조사에서 나온
1/2/3/4px 혼재는 "일관성이 무너졌다"기보다 애초에 **일관성을 강제하는 장치가 코드베이스에
없었다**는 쪽에 더 가깝다.

---

## 8. 전술 UI 사례 비교 (DEFCON / Frostpunk / Military HUD)

이 장르(DEFCON 벡터 지도, Frostpunk 생존 HUD, 군사 전술 콘솔)의 공통된 스트로크 문법은
두 가지다.

1. **이진법적 두께 대비** — "구조선(항상 존재)"과 "강조/경고선(상태 발생 시)" 사이에
   두께 차이를 크게 벌린다. 중간값을 최소화해서, 두께가 바뀌는 순간 자체가 "상태가
   바뀌었다"는 신호로 읽히게 만든다. 1px과 1.5px처럼 가까운 값 두 개를 같이 쓰지 않는다.
2. **같은 두께 = 같은 의미**를 화면 전체에서 절대 어기지 않는다. 예를 들어 "경고 상태"가
   항상 같은 두께+같은 색으로만 표현되면, 플레이어는 색을 읽기 전에 두께만 보고도
   상태를 구분할 수 있다.

Contagion Project는 **색상 축("구조용 발광색 accent-glow" 단일 브랜드 전압) 개념은 이미
이 장르의 문법을 잘 채택**했다 — 이 부분은 강점이다. 반면 **두께 축은 아직 이 원칙을
따르지 못한다.** 위 7절에서 확인했듯 `2px`는 어떤 곳에서는 "기본 상태"(`country-row`),
어떤 곳에서는 "진행 상태"(`research-row--active/--maxed`), 어떤 곳에서는 "선택
상태"(`branch-row--selected`)로 세 가지 다른 의미에 겹쳐 쓰이고 있다. 참고 장르
기준으로는 이것이 가장 치명적인 이탈이다 — 두께 자체가 의미를 잃으면, 플레이어는
매번 색과 텍스트를 다시 읽어야 하고 "두께만 보고 즉각 판단"하는 전술 UI 특유의 속도감이
사라진다.

---

## 결론 — 가장 큰 시각적 문제 하나

**두께가 얇아서가 아니라, 같은 숫자(특히 2px)가 화면마다 서로 다른 의미(기본 상태 /
진행 상태 / 선택 상태)로 겹쳐 쓰이고 있고, 그 값들이 토큰화되지 않은 채 8개 파일에
리터럴 숫자로 흩어져 있어서 "정밀한 전술 콘솔"이 아니라 "패널마다 규칙이 다른 UI"로
읽힌다.** 1px 자체는 3배 스케일로 실기기에서 물리적으로 안 보이는 두께가 아니다 —
문제는 두께의 절대값이 아니라 **두께-의미 매핑의 부재**다.

---

## 권장 Border System v2 (제안 — 미적용)

| 티어 | 두께 | 현재 대응 | 비고 |
|---|---|---|---|
| Divider (Hairline) | 1px | `data-row`, 헤더 하단선, `popup-donut-row`, `popup-description` | 이미 일관적 — 유지 |
| Structural / Panel Edge | 1px | `tactical-panel` 및 그 파생 패널 8종 | 이미 일관적 — 유지, 단 독자 재선언 10여 곳을 이 티어로 흡수 권장(5절) |
| Neutral Card Border | 1px | `branch-row`(기본), `research-row`(잠김/가능) | `country-row` 기본을 2px→1px로 맞추는 것을 권장(5절 근거) |
| Accent Bar (좌측 강조) | 4px | `accent-bar-row`, `section-caption`, `research-row`(left), `status-row` | `data-row--open/--closed`의 3px을 4px로 통일 권장 |
| Progress/Active State | 2px | `research-row--active/--maxed` | "노드 자체 진행도"만 이 값을 쓰도록 의미 고정 |
| Selected/Focus State | 3px | `pathogen-card/country-row/research-row --selected` | `branch-row--selected`의 2px을 3px로 맞춰 Progress 티어와 분리 권장 |
| Decorative Tick (Corner Cut) | 2px(길이 13px) | `corner-cut` 4종 | 두께 자체는 유지하되, "패널 테두리보다 굵고 진한 것은 의도적 강조 장치"라는 설명을 문서화 필요(현재는 문서화 없이 우연히 이렇게 됨) |

**공통 제안**: `Theme.uss :root`에 `--stroke-hairline: 1px`, `--stroke-accent-bar: 4px`,
`--stroke-active: 2px`, `--stroke-selected: 3px` 같은 변수를 추가해, 색상/간격/폰트와
동일한 방식으로 두께도 토큰화하는 것이 근본 해결책이다(7절 원인 분석과 직결).

## DESIGN.md 수정 필요 여부

**필요.** 현재 `DESIGN.md`는 두께 값을 Component Library 각 항목 설명 안에 개별
서술("border: 1px accent-glow-soft" 등)로만 흩어 놓았고, Spacing System(§236)처럼
독립된 두께 스케일 표가 없다. 제안 위치:

- **Spacing System 섹션 바로 뒤에 "Stroke System" 섹션 신설** — 위 v2 표를 그대로
  옮기고, "두께=의미" 매핑을 명문화(예: "2px는 항상 진행 상태, 3px는 항상 선택 상태").
- **Surface Hierarchy 표**(§254)에 "두께" 열을 추가해 단계별 두께를 명시.
- **Usage Rules → Corner Cut 절**(§339)에 "코너컷은 패널 테두리보다 의도적으로 굵고
  진하다"는 문장을 추가해 2절의 발견을 설계 의도로 고정.

(이 보고서는 실제 수정을 하지 않는다 — 위는 어디에 반영해야 하는지에 대한 제안일 뿐이다.)

## 실제 적용 우선순위 (제안)

1. **1순위 — `tactical-panel` / `corner-cut` / `data-row` 3중 복제 정리**: `Hud.uss`,
   `UpgradeTree.uss`가 `Tactical.uss`를 로드하지 않아 로컬 사본을 유지 중(2절 드리프트
   사례의 근본 원인). 토큰화(`--stroke-*`)와 함께 진행하면 향후 드리프트를 원천 차단.
2. **2순위 — Selectable Card/Row 계열 통일**: `country-row` 기본 2px→1px,
   `branch-row--selected` 2px→3px, `data-row--open/--closed` 3px→4px. 사용자가 실제로
   보고한 "화면마다 두께가 달라 보인다" 증상과 가장 직접 연결된 항목들.
3. **3순위 — Button System**: `.tab-button` 클래스명 충돌 해소(예: UpgradeTree 쪽을
   `.tab-selector`로 개명 검토) — 값 자체보다 향후 재사용 시 혼선을 막는 목적.
4. **4순위 — CountryStatusPanel 등 개별 화면 잔여 점검**: `status-row`/`continent-header`
   등은 이미 4px 관행을 따르고 있어 우선순위가 낮음 — 1~3순위 정리 후 회귀 확인 차원.
5. **5순위 — DESIGN.md Stroke System 섹션 반영**: 위 실제 적용이 끝난 뒤 최종 확정된
   값 기준으로 문서화(코드가 먼저, 문서가 나중 — 지금 조사한 v2안이 그대로 확정된다는
   보장은 없으므로).
