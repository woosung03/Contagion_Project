---
title: UI Design System 계층 감사 — Gameplay UI vs Preparation UI 분리 타당성 검토
date: 2026-07-15
type: audit
scope: 코드/USS/DESIGN.md 수정 없음 — 분석 보고서만
sources:
  - Docs/DESIGN.md
  - Docs/UI_Design.md
  - Docs/Architecture.md
  - Docs/DevLog.md (Step 57~86)
  - Assets/UI/*.uxml, *.uss (11개 화면 전수 조사)
---

# UI Design System 계층 감사

## 0. 결론 먼저 (TL;DR)

**가설은 절반만 맞는다.** 실제 코드(Theme.uss/Tactical.uss/11개 화면 UXML·USS)를 전수 조사한
결과, Contagion Project에는 **토큰·컴포넌트 수준에서 서로 다른 두 개의 디자인 시스템이
존재하지 않는다** — `Theme.uss` 한 곳의 `:root` 변수를 11개 화면 전부가 공유하고,
`tactical-panel`/`corner-cut`/`data-row`/severity 4색/Stroke 5단계가 Gameplay·Preparation
구분 없이 동일하게 쓰인다. 특히 CountrySelect의 상세 패널은 Country Dock(HUD)과 **물리적으로
같은 CSS 클래스**를 쓰도록 의도적으로 설계됐다(`UI_Design.md` 9절) — 오히려 "게임 시작 전에
본 데이터 형식을 게임 중에도 계속 본다"는 통일 효과를 노린 설계다.

다만 실기기에서 사용자가 느낀 차이는 **착시가 아니라 실재한다.** 화면들은 하나의 시스템 안에서
**두 가지 "밀도 모드"** 로 갈라져 있다 — HUD/CountryStatusPanel/CountryPopup/ResearchPopup/
RankingPanel/UpgradeTree는 `font-size-xs`(13px) 중심의 압축 판독 밀도로, MainMenu/
CountrySelect/EndingScreen은 `font-size-lg~hero`(20~40px) 중심의 여유 있는 브리핑 밀도로
렌더링된다. 이는 우연한 불일치가 아니라 `DESIGN.md` Design Principle #6("밀도와 절제")이
이미 명문화한 의도된 분기다.

**최종 결론**: 새로운 2계층 컴포넌트/토큰 체계를 만들 필요는 없다. 대신 이미 코드에 존재하는
"압축 판독 모드"와 "여유 브리핑 모드"라는 두 밀도 프리셋을 `DESIGN.md`에 **공식 명명·문서화**하는
것을 권장한다 — 근거와 구체적 제안은 7절/9절 참고.

---

## 1. UI 분류표

요청 대상 11개 중 **9개는 실제 화면(UIDocument)**, 2개(CountryDock/Event·News UI)는
**독립 화면이 아니라 HUD(`Hud.uxml`) 내부의 서브 컴포넌트**다 — 이 사실 자체가 이미 "화면
단위 분리"보다 "화면 내부 도킹 패널"이라는 다른 층위의 설계임을 보여준다.

| # | UI | 실체 | 분류 | 파일 |
|---|---|---|---|---|
| 1 | MainMenu | 독립 화면 | Preparation | `MainMenu.uxml/uss` |
| 2 | CountrySelect | 독립 화면(우스 공유) | Preparation | `CountrySelect.uxml/uss` + `MainMenu.uss` |
| 3 | HUD | 독립 화면(상시 오버레이) | Gameplay | `Hud.uxml/uss` |
| 4 | CountryDock | **HUD 내부 서브패널**(독립 화면 아님) | Gameplay | `Hud.uxml/uss` `.country-dock` |
| 5 | Event UI / News UI | **HUD 내부 서브패널**(독립 화면 아님, 상세 모달 없음) | Gameplay | `Hud.uxml/uss` `.event-dock`/`.news-scroll` |
| 6 | CountryPopup | 독립 화면(모달) | Gameplay | `CountryPopup.uxml/uss` |
| 7 | UpgradeTree | 독립 화면 | Gameplay | `UpgradeTree.uxml/uss` |
| 8 | ResearchPopup | 독립 화면(모달, UpgradeTree 하위) | Gameplay | `ResearchPopup.uxml/uss` |
| 9 | CountryStatusPanel | 독립 화면(GLOBAL STATUS CENTER) | Gameplay | `CountryStatusPanel.uxml/uss` |
| 10 | RankingPanel | 독립 화면(모달) | **경계형**(아래 참고) | `RankingPanel.uxml/uss` |
| 11 | EndingScreen | 독립 화면 | **경계형**(아래 참고) | `EndingScreen.uxml/uss` |

**경계형으로 분류한 이유**: RankingPanel/EndingScreen은 플레이 도중 등장하지 않고 "세션의
끝"(게임오버 시점)에만 뜬다는 점에서 실시간 모니터링(Gameplay)도, 사전 의사결정
(Preparation)도 아닌 **제3의 국면 — "결과 브리핑"**에 해당한다. 사용자가 제시한 A/B 2분류에
억지로 끼워 맞추면 왜곡이 생기는 지점이라 별도 표기했다(7절에서 구조 제안에 반영).

---

## 2. 화면별 역할 분석

| 화면 | 역할 | 정보 소비 방식 | 근거(UI_Design.md) |
|---|---|---|---|
| MainMenu | 병원체 6종 중 1개 확정 | 의사결정(비교 후 선택) | 2.1 "브리핑룸" |
| CountrySelect | 발원 국가 48개 중 1개 확정 | 의사결정(비교 후 선택) | 2.2 |
| HUD | 매틱 갱신되는 세계 지표 상시 감시 | 상황 인지(passive monitoring) | — |
| CountryDock | 선택 국가 7개 지표 상시 표시 | 상황 인지 | — |
| Event/News | 이벤트 로그 스트림 | 정보 소비(passive feed, 상세 조회 불가) | 2.4 "확장 뷰 없음" |
| CountryPopup | 선택 국가 심층 대시보드(도넛/순위/이동통제) | 정보 소비(drill-down) | 12절 |
| UpgradeTree | DNA 트리 45노드 탐색+구매 | 의사결정(자원 배분) | 7절 |
| ResearchPopup | 연구 노드 1개 상세+시작 확정 | 의사결정(단건 확정) | ResearchDatabase_V2 문서군 |
| CountryStatusPanel | 48개국 동시 비교+랭킹 | 상황 인지(global overview) | Step 80/81/85/86 |
| RankingPanel | 개인 최고 기록 확인+외부 리더보드 진입 | 정보 소비(약함, 데이터 빈약) | 11절 |
| EndingScreen | 세션 결과 요약+재시작/부활 결정 | 정보 소비 + 의사결정 혼합 | 10절 |

패턴이 뚜렷하다: **HUD 계열(3~9번)은 "인지"가 중심**이고 **메뉴 계열(1~2번)은 "결정"이
중심**이다. 다만 UpgradeTree/ResearchPopup처럼 Gameplay로 분류되면서도 의사결정 비중이 큰
화면이 섞여 있어, "Gameplay=인지, Preparation=결정"이라는 단순 이분법도 완전히 깔끔하지는
않다 — 이 화면은 "게임 중 이루어지는 의사결정"이라 Gameplay UI 언어(압축 판독)를 쓰는 게
맞다는 것이 현재 코드의 실제 판단이었고(DevLog Step 62), 실기기 검증도 이 판단을 거치며
확정됐다.

---

## 3. 현재 디자인 언어 분석 — Tactical Console vs Briefing Room

11개 화면의 실제 UXML/USS를 직접 대조한 결과:

| 축 | Tactical Console 쪽(HUD/CountryPopup/ResearchPopup/CountryStatusPanel/UpgradeTree/RankingPanel) | Briefing Room 쪽(MainMenu/CountrySelect/EndingScreen) |
|---|---|---|
| 기본 폰트 크기 | `--font-size-xs`(13px)/`--font-size-sm`(14px) 중심 | `--font-size-lg`~`--font-size-hero`(20~40px) 중심 |
| 정보 단위 | `data-row`(라벨:값) 반복 — CountryPopup 6~10행, UpgradeTree detail-panel 3~5행 | 문단형 설명(`detail-panel__desc`) 병행 — MainMenu FlavorText, EndingScreen 타이틀 |
| 코너컷 밀도 | 화면당 패널 1~2개(팝업/상세패널)에 4개 전부 — 리스트 행(48개국)에는 미적용 | pathogen-card **6장 전부**에 4개(카드 수가 적어 허용, `UI_Design.md` 8절) |
| 캡션 문법 | `tactical-panel__title`(11~13px, `tracking-caption`) | `tactical-caption`(11px) 동일 클래스지만 큰 타이틀(`mainmenu-title` 32px) 아래 보조로만 사용 |
| 배경 | `--color-bg-panel`/`--color-bg-panel-strong`(불투명, 지도가 안 비쳐야 함) | `--color-bg-root`(전체화면 캔버스) |

두 그룹 다 **같은 컴포넌트 클래스(`tactical-panel`/`corner-cut`/`data-row`/`tactical-caption`)를
그대로 쓴다** — 클래스가 다른 게 아니라, 같은 클래스를 두 가지 스케일(폰트 크기)과 두 가지
밀도(판독행 vs 문단)로 "연주"하고 있는 것에 가깝다. `DESIGN.md` Design Principle #6이 이미 이
관계를 "HUD는 압축된 font-size-xs, 메뉴류는 기존 크기 유지 — 화면 전체를 HUD처럼 극단적으로
압축하지 않는다"고 명문화해뒀다.

**즉 "Tactical Console 언어"와 "Briefing Room 언어"는 이미 존재하지만, 그 차이는 컴포넌트
교체가 아니라 같은 컴포넌트의 스케일 변주(density variant)로 구현되어 있다.** 이 구분에
공식 이름이 없다는 것이 지금 유일하게 빠진 조각이다.

---

## 4. DESIGN.md와의 충돌 여부

**충돌 없음.** `DESIGN.md`는 이미 이 두 층위를 예견하고 있다:

- Design Principle #5(각짐)는 "브리핑룸 톤의 `country-row`/`pathogen-card` 같은 메뉴류
  예외는 `UI_Design.md` 화면 설계 재량이며, HUD/UpgradeTree 계열 "전술 디스플레이"에는
  적용하지 않는다"고 **명시적으로 두 그룹을 구분**해뒀다.
- Stroke Semantic Table의 "예외" 행("`country-row`/`pathogen-card` 기본 테두리 2px —
  메뉴카드 예외... 이미 각짐 규칙에서 면제된 카드군")도 동일한 구분을 다른 각도(테두리
  두께)에서 재확인한다.
- Color System §9(CountrySelect developmentLevel → severity 매핑)는 오히려 **Preparation
  화면이 Gameplay 화면의 severity 축을 그대로 가져다 쓰도록** 설계됐다 — 두 계층이 색상
  의미를 공유해야 한다는 전제가 이미 있다.

다시 말해 `DESIGN.md`는 "하나의 시스템, 화면 성격에 따른 재량 허용"이라는 입장을 이미
취하고 있고, 지금 구현 상태(3절)는 그 입장과 정확히 일치한다. **2계층으로 공식 분리해도
DESIGN.md를 개정할 필요가 없다** — 이미 있는 예외 조항을 "정식 모드"로 승격 표기하는 문서
작업만 남는다(7절 제안 참고).

유일한 기술 부채는 디자인 철학 차원이 아니라 **코드 정리 차원**이다: `Tactical.uss`가
2번 이상 재사용되는 공용 클래스를 담기 위해 만들어졌음에도(`UI_Design.md` §15/§18),
`Hud.uss`(157~167행)와 `UpgradeTree.uss`(160~202행)에는 `tactical-panel`/`corner-cut`/
`data-row`의 **동일한 값이 로컬로 중복 정의**돼 있다(값 자체는 일치하므로 화면에 보이는
결과물엔 차이가 없다). `UpgradeTree.uxml`은 심지어 `<Style src="Tactical.uss">`를 아예
로드하지 않는다(`Theme.uss` + `UpgradeTree.uss`뿐). 시각적 충돌은 아니지만, 두 계층으로
공식 분리하는 시점에 이 중복도 함께 정리하는 것을 권장한다(8절 유지보수 평가 참고).

---

## 5. 공통으로 유지해야 하는 요소

전 화면이 이미 공유하고 있고, 계층을 나누더라도 반드시 유지해야 하는 것:

- **`Theme.uss` 단일 토큰 소스** — 색상/간격/타이포/Stroke 5단계가 `:root` 한 곳에서만
  정의되고 11개 화면 전부가 참조한다. 이걸 화면군별로 쪼개는 순간 "값을 한 곳만 고치면
  전체 반영" 원칙이 깨진다.
- **Stroke System 5단계**(hairline/active/selected/accent-bar/sub-accent) — 두께=의미
  매핑은 화면 성격과 무관한 구조적 규칙.
- **Severity 4색**(infected/dead/danger/info) — CountrySelect가 이미 이 축을 가져다 쓰는
  선례(4절)가 있어, Preparation 화면도 이 축에서 배제하면 안 됨.
- **Button System 3종**(Primary/Secondary/Danger) — MainMenu/EndingScreen/CountryPopup/
  ResearchPopup/RankingPanel 전부 동일한 "배경/테두리/radius 명시" 규칙으로 Unity 기본
  회색 버튼 버그를 수정한 이력을 공유한다(3절 표에서 확인). 계층을 나눠도 버튼 규칙까지
  갈라지면 과거 버그가 재발할 위험이 크다.
- **corner-cut/tactical-panel 셸** — "각진 모서리 + 발광 헤어라인"이라는 최상위 정체성은
  두 그룹 모두의 지문(fingerprint)이라 여기서 갈라지면 "같은 게임"이라는 인상 자체가
  깨진다.
- **data-row / detail-rows 계약** — 컨트롤러가 런타임에 행을 채우는 패턴 자체가 재사용
  코드 자산(`AddDetailRow`류 헬퍼)이라, 계층을 나눠도 이 계약은 공유해야 향후 화면 추가
  비용이 커지지 않는다.

---

## 6. 의도적으로 달라져야(이미 달라진) 요소

- **폰트 스케일** — Gameplay는 `xs~sm`(13~14px) 압축, Preparation은 `lg~hero`(20~40px)
  여유. 이미 그렇게 구현돼 있고(3절), 이 차이를 없애 통일하면 오히려 HUD 판독성이나
  MainMenu 첫인상 중 하나를 희생하게 된다 — DESIGN.md Principle #6이 이미 이 방향을
  금지하고 있다("전체 화면을 HUD처럼 극단적으로 압축하지 않는다").
- **코너컷 인스턴스 규모** — Gameplay 쪽은 "화면당 핵심 패널 1~2개"에만 4개(리스트 행엔
  accent-bar로 대체), Preparation 쪽은 카드 6장 전부에 4개. `DESIGN.md` Usage Rules >
  Corner Cut이 이미 "패널 수 적음 → 4개 전부 / 리스트 행처럼 많음 → accent-bar"라는
  **밀도 기반** 규칙으로 정의해뒀다 — 이건 화면 계층이 아니라 인스턴스 개수가 기준이라는
  점이 중요하다(계층을 나눠도 규칙 자체는 "화면 성격"이 아니라 "개수"로 유지해야 함).
  실제로 UpgradeTree의 branch-board(카테고리당 4개)와 detail-panel(화면당 1개)도 코너컷
  4개 전부를 쓴다 — Gameplay 화면 안에서도 개수 기준이 우선한다는 증거.
  같은 Gameplay 계열이라도 branch-board(4개 인스턴스)와 status-list(48개 인스턴스)는
  전자만 코너컷을 쓰고 후자는 `status-row--{severity}` accent-bar만 쓴다.
- **정보 표현 방식** — Preparation은 문단형 설명(`FlavorText`, `detail-panel__desc`)을
  일부 허용(MainMenu 8절 — "서술형 설명 문장이면 그대로 문단 유지"), Gameplay는 거의
  전부 `data-row` 판독행으로 강제된다. 이 차이도 "정보의 성격"(브리핑 텍스트 vs 실시간
  수치)에서 자연스럽게 갈리는 것이지 임의 규칙이 아니다.
- **배경 불투명도** — CountryStatusPanel은 `--color-bg-panel-strong`을 완전 불투명(알파
  1.0)까지 올려 지도가 절대 비치지 않게 했다(Theme.uss 주석, "지도는 주인공이 아니다"
  피드백 대응) — Gameplay 화면 중에서도 "지도 위 오버레이"(HUD/CountryPopup)와 "지도를
  가리는 전체화면 대시보드"(CountryStatusPanel/UpgradeTree)는 서로 다른 불투명도 정책을
  쓴다. 이 역시 Gameplay 내부의 세분화이지, A/B 이분류로는 설명되지 않는 지점이다.

---

## 7. 실제 프로젝트 구조 제안

**2계층 컴포넌트 시스템 신설은 비권장.** 대신 아래처럼 **1개 시스템 + 2개 명명된 밀도
프리셋(named density preset)** 구조를 제안한다 — 이미 코드가 이렇게 동작하고 있으므로,
문서에 이름만 붙이면 된다.

### Layer A — Tactical Readout Mode (기존 "Gameplay UI")
- 대상: HUD(+CountryDock/Event·News), CountryPopup, UpgradeTree(+ResearchPopup),
  CountryStatusPanel, RankingPanel
- 컨셉: "감염병 통제센터 콘솔" — Global Surveillance Center / Pandemic Monitoring Console
  (`DESIGN.md`가 이미 이 이름을 쓰고 있음, 새로 지을 필요 없음)
- 폰트: `xs`/`sm` 고정, data-row 강제, 코너컷은 인스턴스 개수 기준(≤6개면 4개, 많으면
  accent-bar)

### Layer B — Briefing Terminal Mode (기존 "Preparation UI")
- 대상: MainMenu, CountrySelect
- 컨셉: `UI_Design.md`가 이미 명명한 "PATHOGEN BRIEFING TERMINAL" / "GLOBAL DEPLOYMENT
  TERMINAL"을 그대로 승격
- 폰트: `lg`~`hero`, 문단형 설명 허용, 코너컷 카드 전부 적용(카드 수 적음)

### Layer C — Debrief Mode (신규 명명 제안, "경계형" 처리)
- 대상: EndingScreen, RankingPanel 중 결과 요약 성격이 강한 부분
- 컨셉: "작전 종료 브리핑" — Layer A의 판독 밀도(`data-row` 4~5줄 통계 패널)와 Layer B의
  임팩트(hero 폰트 타이틀/스코어)를 **의도적으로 혼합**한다는 점을 명문화. 지금 EndingScreen
  코드가 실제로 이렇게 돼 있다(`ending-title`은 hero 40px, `stats-rows`는 `data-row`
  판독행) — 이건 실수가 아니라 "결과 발표는 크게, 근거 수치는 조밀하게"라는 의도된 하이브리드
  설계다. 이 층을 A 또는 B 어느 한쪽에 강제로 편입시키면 오히려 지금 잘 작동하는 설계를
  왜곡하게 된다.

RankingPanel은 Layer A(판독 톤: tactical-panel+data-row+corner-cut)로 이미 완전히
전환됐지만(1절 "경계형" 각주), 데이터 자체가 빈약해(11절 UI_Design.md) 정보 소비 경험은
Layer C에 더 가깝다 — 컴포넌트는 A, 역할은 C인 혼재 사례로 별도 주석만 남기고 재분류는
권장하지 않는다(작업 리스크 대비 효과가 낮음).

---

## 8. 사용자 경험 관점 평가

### 하나의 스타일로 통일하는 경우
**장점**: 학습 비용 최소화(플레이어가 한 번 "판독행" 문법을 익히면 전 화면에 적용), 코드
재사용 극대화(`data-row`/`AddDetailRow` 계약 하나로 전 화면 커버), 신규 화면 추가 시 결정
피로 없음("이 화면은 A냐 B냐" 고민 불필요).
**단점**: MainMenu/CountrySelect처럼 "첫인상이 중요한 저빈도 화면"까지 HUD급 압축 밀도로
누르면 게임 진입 시점의 몰입감이 떨어진다 — 실제로 과거 시도(HUD 리디자인 직후)에서 이
문제가 감지돼 Design Principle #6이 만들어진 정황이 `UI_Design.md`에 남아있다.

### 두 계층(A/B)으로 나누는 경우
**장점**: 화면의 "체류 시간"과 "정보 갱신 빈도"에 맞는 밀도를 각각 최적화할 수 있다 —
HUD는 매틱 갱신되는 데이터를 순간적으로 스캔해야 하므로 압축이 유리하고, MainMenu는 6장
카드를 천천히 비교하므로 여유가 유리하다. 지금 실기기 테스트에서 사용자가 감지한 "만족/
불만족 차이"도 이 최적화가 이미 대체로 잘 되어 있다는 신호로 해석할 수 있다.
**단점**: 공식적으로 "계층"이라는 이름을 붙이는 순간, 향후 신규 화면(설정 창/이벤트 상세 등,
`UI_Design.md` 2.4/2.5)을 만들 때마다 "이건 A냐 B냐 C냐"를 매번 판정해야 하는 의사결정
비용이 새로 생긴다 — 지금은 이 판정이 암묵적으로 "화면이 지도 위에 뜨는가"로 거의 자동
결정되지만, 이름이 생기면 그 자동성이 오히려 형식적 절차로 굳어질 위험이 있다.

**둘 다의 리스크를 줄이는 절충안**이 7절 제안(1시스템+명명된 밀도 프리셋)이다 — "계층"을
컴포넌트/토큰 레벨이 아니라 **문서상 명명(naming) 레벨**에서만 분리하면, 위 통일안의 장점
(코드 재사용/토큰 단일화)과 분리안의 장점(화면별 최적 밀도)을 동시에 취하면서, 신규 화면
추가 시의 판정 기준도 이미 있는 규칙(코너컷 Do/Don't의 "인스턴스 개수" 기준, Principle #6의
"화면 성격" 기준)을 그대로 재사용할 수 있어 새로운 의사결정 비용이 발생하지 않는다.

---

## 9. 장기 유지보수 관점 평가 및 최종 결론

**유지보수 관점**: 토큰이 `Theme.uss` 한 곳에 있는 한, 계층을 나누든 안 나누든 유지보수
비용 자체는 크게 달라지지 않는다 — 진짜 유지보수 리스크는 계층 여부가 아니라 4절에서 지적한
`Tactical.uss` 참조 누락(`Hud.uss`/`UpgradeTree.uss` 로컬 중복)이다. 이 정리를 미룬 채
2계층 문서화를 먼저 진행해도 무방하지만(값이 동일해 시각적 리스크는 없음), 두 작업을
같은 세션에 묶으면 "계층 이름 정리 + 참조 경로 정리"를 한 번에 검증할 수 있어 효율적이다.

**최종 결론**:

> Contagion Project는 **하나의 UI 컨셉(Tactical Design System)으로 유지**하되, 그 위에
> 이미 실존하는 **밀도 프리셋 2~3종(Tactical Readout / Briefing Terminal / Debrief)을
> `DESIGN.md`에 정식 명명·문서화**하는 것이 정답이다. "Gameplay UI / Preparation UI"라는
> 별도의 컴포넌트·토큰 체계로 완전히 갈라서는 방향은 채택하지 않는다 — 근거는 (1) 11개
> 화면 전수 조사 결과 토큰/컴포넌트 클래스가 이미 100% 공유되고 있고, (2) `DESIGN.md`
> Principle #5/#6과 Stroke Semantic Table 예외 조항이 이미 이 "한 시스템 안의 재량"
> 구조를 명문화해뒀으며, (3) CountrySelect의 존재 이유 자체가 "Preparation 화면에서도
> Gameplay 화면과 같은 데이터 문법을 미리 보여준다"는 **의도된 계층 간 통합**이기 때문이다.
> 사용자가 실기기에서 느낀 만족/불만족의 차이는 계층이 분리되어 있어서가 아니라, 밀도
> 프리셋 적용이 화면마다 고르지 않게(또는 아직 이름 없이 암묵적으로만) 이루어지고 있어서일
> 가능성이 높다 — 다음 작업은 "2계층 분리"가 아니라 "이미 있는 밀도 프리셋에 이름을 붙이고
> 전 화면에 일관되게 재확인"이어야 한다.

---

## 부록: 재검증에 사용한 원본 근거 위치

| 주장 | 근거 파일:위치 |
|---|---|
| Theme.uss 단일 토큰 소스 | `Assets/UI/Theme.uss` 16~127행 (`:root`) |
| 11개 화면 전부 Tactical.uss/Theme.uss 참조 | 각 화면 `.uxml` 1~5행 `<Style src>` |
| UpgradeTree만 Tactical.uss 미참조(로컬 중복) | `Assets/UI/UpgradeTree.uxml` 1~3행, `UpgradeTree.uss` 160~202행 |
| Hud.uss 로컬 중복 | `Assets/UI/Hud.uss` 157~167행 (`.corner-cut`) |
| Design Principle #5/#6 | `Docs/DESIGN.md` "Design Principles" 5·6번 |
| Stroke 예외 조항 | `Docs/DESIGN.md` "Stroke Semantic Table" 마지막 행 |
| CountrySelect-CountryDock 의도적 통합 | `Docs/UI_Design.md` 9절 "Country Dock 시각 언어 재사용" |
| CountryStatusPanel 배경 불투명도 예외 | `Assets/UI/Theme.uss` 20~27행 주석 |
| RankingPanel 데이터 빈약 진단 | `Docs/UI_Design.md` 11절 |
| EndingScreen hero+data-row 하이브리드 | `Assets/UI/EndingScreen.uxml` 6~27행, `.uss` 9~65행 |
