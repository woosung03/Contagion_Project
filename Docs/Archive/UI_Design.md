# UI_Design.md — 화면 설계 문서

> **문서 역할 안내**: 이 문서는 **화면 설계 문서**다. 디자인 토큰·색상 체계·타이포그래피·
> 간격·Surface Hierarchy·컴포넌트 정의/사용 규칙·Do/Don't는 전부 `Docs/DESIGN.md`(디자인
> 시스템 문서)가 정본이다. 이 문서는 오직 (1) 화면별 설계, (2) 화면별 적용 방향, (3) 구현
> 우선순위, (4) 실행 계획, (5) 화면 구조 분석만 다룬다. 두 문서가 충돌하면 **토큰/컴포넌트
> 값은 DESIGN.md, 화면 배치·우선순위·실행 계획은 이 문서**가 우선한다.
>
> 이 문서는 원래 토큰·컴포넌트 스펙을 함께 담은 "제안서"로 시작했으나(§2/§3/§5/§7/§18에
> 있던 Design Tokens 확장안·`tactical-panel` CSS 정의·UXML 템플릿안·Design Tokens
> 최종본·Tactical Design System v2 정리), `DESIGN.md` 신설에 맞춰 그 내용을 전부
> `DESIGN.md`로 이관하고 이 문서에서는 삭제했다(정보 자체는 소실되지 않았다 — DESIGN.md
> Color System/Typography/Spacing/Component Library/Usage Rules 참고). 실행되지 않은
> 채 남아있던 제안은 §15 Historical Notes로 분리했다.
>
> **⚠ 문서 전역 경고 (문서 무결성 감사, 2026-07-22)**: 이 문서 전체(§0/§2/§3/§4/§6/§9/§12/§14
> 등)에서 반복 언급되는 **"Country Dock"은 이후 완전히 제거됐다** — `CountryPopupController`
> 기반 Bottom Sheet(지도를 가리지 않는 하단 고정 팝업, `quick-stat-grid`/`badge-tag`)로
> 대체됐다(CLAUDE.md "완료된 시스템" 목록, 커밋 2e4483e). "Country Dock 시각 언어를 다른
> 화면에 재사용" 식의 서술이 나오면 현재는 **CountryPopup(Bottom Sheet)**을 그 대상으로
> 읽을 것 — 컴포넌트 이름만 바뀌었을 뿐 "국가 데이터를 판독행으로 보여준다"는 설계 의도
> 자체는 CountryPopup이 이어받았다. §7(UpgradeTree)도 별도로 이중 superseded — 해당 절
> 상단 배너 참고.

Step 57~61에서 HUD(Hud.uxml/uss)에 확립된 "감염병 통제센터 콘솔" 시각 언어를, 나머지 7개
화면(MainMenu/CountrySelect/UpgradeTree/CountryPopup/EndingScreen/RankingPanel + 신규 필요
화면)으로 확장하기 위한 설계안이다. **레이아웃/기능/C# 로직은 그대로 두고 USS 클래스와
소량의 UXML 래퍼만 추가**하는 것을 원칙으로 한다 — Step 58 HUD 리디자인 때 검증된 방식
(엘리먼트 `name`은 그대로 두고 클래스/구조만 재배치 → 컨트롤러 코드 무변경)을 그대로
반복한다.

---

## 0. 현재 상태 진단 (as-is)

### 0.0 Density Mode 개요 (정의는 DESIGN.md 참고)

> UI Design System Audit 결론: 이 프로젝트는 "Gameplay UI"/"Preparation UI"로 분리된 두
> 디자인 시스템을 쓰지 않는다. 모든 화면이 하나의 Tactical Design System(`DESIGN.md`)을
> 공유하며, 화면별 차이는 **Density Mode**(정보 밀도 프리셋) 차이일 뿐이다. 각 모드의
> 목적/폰트 스케일/허용·금지 컴포넌트 정의는 `DESIGN.md` > UI Density Modes가 정본이다 —
> 이 문서(UI_Design.md)는 **어느 화면이 어느 모드에 속하는지**와 **그 모드를 화면에서
> 구체적으로 어떻게 배치하는지**만 다룬다.

| 화면 | Density Mode |
|---|---|
| HUD / Country Dock / Event·News Dock | Tactical Readout Mode |
| CountryPopup | Tactical Readout Mode |
| UpgradeTree | Tactical Readout Mode |
| ResearchPopup | Tactical Readout Mode |
| CountryStatusPanel | Tactical Readout Mode |
| RankingPanel | Tactical Readout Mode |
| MainMenu | Briefing Terminal Mode |
| CountrySelect | Briefing Terminal Mode |
| EndingScreen | Debrief Mode |

아래 §2 이하의 화면별 절 각각에도 해당 화면의 Density Mode를 명시해 둔다.

### 0.1 이미 확립된 언어 — 구현 현황 (스펙은 DESIGN.md 참고)

Step 57~61에서 HUD가 확립하고, 현재 `DESIGN.md`가 정본으로 관리하는 토큰/컴포넌트 목록.
여기서는 **어느 화면에 이미 반영됐는지**만 진단한다 — 색상 값·CSS 정의는 다루지 않는다.

| 요소 | DESIGN.md 참고 위치 | 반영 화면 |
|---|---|---|
| accent-glow 헤어라인 / corner-cut | Component Library, Usage Rules > Corner Cut | HUD, UpgradeTree만 — 나머지 5개 화면 미반영 |
| data-row / data-label / data-value | Component Library | HUD, UpgradeTree만 |
| tactical-panel | Component Library, Usage Rules > Tactical Panel | HUD(event-dock/country-dock), UpgradeTree(detail-panel)만 |
| severity 색상 | Status Semantics, Usage Rules > Severity Colors | HUD(population-bar/뉴스 dot/Country Dock)만 |
| stat-chip | Component Library | HUD(resource-strip)만 |
| world-status-frame | Component Library | HUD만 |
| graph-frame(스파크라인) | Component Library, Data Visualization Rules | HUD만 |
| 노드 상태 4색(LOCKED/AVAILABLE/ACTIVE/MAXED) | Color System §7, Usage Rules > Node State Colors | UpgradeTree만 |

이 8가지가 아직 나머지 화면(MainMenu/CountrySelect/EndingScreen/RankingPanel/CountryPopup)
에 확산되지 않은 상태다 — 이 문서 §2 이하의 목적은 **이 확산 작업의 화면별 계획**이다.

### 0.2 나머지 화면의 현재 상태

MainMenu / CountrySelect(별도 `CountrySelect.uxml`이지만 `MainMenu.uss`를 그대로 공유 —
클래스명이 100% 동일) / UpgradeTree(Step 62에서 전환 완료) / CountryPopup / EndingScreen /
RankingPanel은 UpgradeTree를 제외하면 전부 **HUD 리디자인 이전의 "일반 다크 모바일 카드"
톤**이다 — 공통점:

- 카드: `color-surface` 반투명 흰색 배경 + 2px `color-border`(흰색 계열) + `radius-md`
  둥근 모서리
- 코너 브래킷/발광 헤어라인 없음, 격자선(grid-line) 없음
- data-label/data-value 계층 없음(`detail-panel__title`+`__desc`처럼 제목+설명 문단
  구조뿐)
- 선택 강조는 금색(`--color-border-selected`) 하나로 통일 — severity 색상 체계 미사용

즉 **HUD/UpgradeTree만 "통제센터", 나머지는 여전히 "일반 앱"** 인 상태 — 이번 작업의 핵심
문제.

### 0.3 존재하지 않는 화면 (요청 8개 중 실제 갭)

| 요청 화면 | 실제 상태 |
|---|---|
| 이벤트 상세 창 | 없음. `EventManager`는 `NewsFeedController`의 event-dock 한 줄 피드로만 소비됨(확장 모달 없음) |
| 설정 창 | 없음(코드/UXML 전무) |
| 알림(Notification) UI | 없음. 이벤트는 event-dock 피드 안에만 나타나고 화면 중앙에 뜨는 토스트/배너 없음 |
| 팝업/모달 | `CountryPopupController` + `CountryPopup.uxml/uss`가 존재하지만 **Step 28-2 이후 클릭 트리거가 제거된 죽은 코드**(Country Dock으로 대체됨). 구조 자체는 재사용 가능한 모달 골격으로 남아있음 |

이 네 가지는 "기존 화면 스타일링"이 아니라 **신규 컴포넌트 설계**가 필요하다 — §2에서
다룬다.

---

## 1. 리팩토링 실행 원칙

1. **엘리먼트 `name`은 절대 바꾸지 않는다** — 모든 컨트롤러가 `root.Q<T>("이름")`으로
   쿼리하므로, 클래스 추가/구조 래핑만으로 스타일을 바꾼다(Hud 리디자인 때 검증된 방식).
2. **기존 클래스는 제거하지 않고 병기한다** — `pathogen-card--selected`,
   `tree-node--unlocked`처럼 C#이 `EnableInClassList`로 토글하는 클래스는 그대로 두고, 새
   tactical 클래스를 추가로 붙인다.
3. **토큰 확장/색상 하드코딩 금지 규칙은 `DESIGN.md` Do/Don't 참고** — 화면 작업 시에도
   그대로 적용한다. 화면별로 새 색상·간격 값을 만들지 않는다.
4. **`tactical-panel`/`data-row`/`corner-cut`은 이미 `Tactical.uss`로 승격되어 있다**
   (`DESIGN.md` Component Library 참고, 재검증 결과는 §15 Historical Notes) — 화면
   작업 시 이 공용 클래스를 그대로 소비하고, 화면별로 새로 정의하지 않는다.
5. **UI Toolkit 제약 준수** — `clip-path`/`box-shadow`/`background-repeat`/`:last-child`
   미지원(`DESIGN.md` Interaction Rules "USS 기술적 제약" 참고). 코너컷/헤어라인/격자선처럼
   이미 검증된 대체 기법만 재사용하고 새로운 미지원 CSS 시도를 하지 않는다.
6. **정보 밀도 vs 가독성** — 세로 모바일 좁은 폭에서 격자선/코너컷은 얇게(1~2px) 유지하고,
   판독 텍스트 크기는 화면 성격에 따라 차등한다(HUD는 압축, 메뉴류는 기존 폰트 크기 유지) —
   전체 화면을 HUD처럼 극단적으로 압축하지 않는다. (시스템 차원의 밀도 원칙은 `DESIGN.md`
   Design Principles #6에 있으며, 이 항목은 그것을 화면별로 어떻게 차등 적용할지에 대한
   화면 설계 판단이다.)

---

## 2. 화면별 분석

### 2.1 메인 메뉴 (MainMenu — 병원체 선택)

- **Density Mode**: Briefing Terminal Mode(`DESIGN.md` > UI Density Modes 참고)
- **역할**: 세션 시작점, 병원체(6종) 선택 → "브리핑룸"에 해당. 콘솔 톤보다는 약간 격식
  있는 "작전 개시 전 브리핑" 느낌이 맞다(HUD와 완전히 동일한 압축 밀도는 과함).
- **재사용**: `pathogen-card`에 `tactical-panel` 보더 톤(`accent-glow-soft` 1px) 추가 +
  좌상단 코너컷 2개(전체 4개는 카드가 작아 과함, tl/br 2개만으로 "판독 카드" 암시). 선택
  시 기존 `pathogen-card--selected`(금색 강조)는 유지 — 이건 "선택 확정" UX 언어라
  병원체 화면 고유로 남겨도 됨. `detail-panel`은 `data-row`/`data-label`/`data-value`
  구조로 전환(현재는 문단형 desc라 국가 선택 화면만큼 표 형태 정보가 많지 않으면 무리해서
  바꿀 필요는 없음 — 병원체 스탯 3~4개면 data-row가 적합, 서술형 설명 문장이면 그대로
  문단 유지).
- **신규**: `.tactical-panel__title` 스타일을 `mainmenu-title`에 캡션 톤(자간 넓은 대문자,
  `"PATHOGEN SELECT"` 식 영문 캡션을 타이틀 위에 작게 얹는 것도 고려 가능 — 선택 사항).
- **우선순위**: 중(사용자가 게임에 들어가기 전 첫인상 — 톤 전환 체감 크지만 기능 리스크
  낮음).

### 2.2 국가 선택 화면 (CountrySelect — MainMenu.uxml 공유)

- **Density Mode**: Briefing Terminal Mode(`DESIGN.md` > UI Density Modes 참고)
- **역할**: 발원 국가 48개 중 1개 선택. 국가 정보 카드가 이미 Country Dock/국가현황 패널과
  포맷이 겹친다(인구/기후/의료수준) — **일관성 확보가 가장 시급한 화면**.
- **재사용**: `country-row`는 `country-dock`과 사실상 같은 데이터를 다루므로
  `data-row`/`data-label`/`data-value` 그대로 적용 — 국기(`country-row__flag`)는 유지하고
  텍스트 부분만 판독행으로 전환. `detail-panel`(선택 국가 상세)도 `tactical-panel` +
  `data-row` 구조로 바꾸면 Country Dock과 완전히 동일한 시각 언어가 되어 "다음 세션에
  어차피 볼 것과 지금 고르는 것이 같은 시스템"이라는 느낌을 줌.
- **신규**: 없음 — 순수 기존 언어 이식.
- **우선순위**: **높음** — 국가 데이터 표현이 Country Dock/국가현황 패널과 3중으로 겹치는
  유일한 화면이라 통일 효과가 가장 크고, 클래스 추가만으로 끝나 리스크도 낮음.

### 2.3 업그레이드 화면 (UpgradeTree)

> **⚠ [문서 무결성 감사, 2026-07-22]** 아래 내용은 Step 62 시점 설계다 — 이후 캔버스 승격
> (Commit 1~5)과 UIDocument 1개 통합(이번 세션)으로 이중 superseded됨. 상세 정정 내용은
> **§7 상단 배너** 참고.

- **Density Mode**: Tactical Readout Mode(`DESIGN.md` > UI Density Modes 참고)
- **역할**: DNA 트리 노드 45개 — "무기고/연구 콘솔"에 해당. 이미 세 카테고리(전파/증상/
  능력) 색상 테두리(`--color-node-*`)를 쓰고 있어 severity 색상 체계와 유사한 사고방식이
  이미 있음.
- **재사용**: `upgrade-root` 배경은 이미 `--color-bg-panel-alt`(전용 다크톤)라 유지. 헤더
  (`upgrade-header__category-title`)에 `tactical-panel__header` 하단 그리드라인 추가로
  "판독 헤더" 느낌 부여. `tree-node`는 카테고리 보더색은 유지하되 `tree-node--unlocked`
  (해금) 상태에 코너컷 미니어처(2개, tl/br) 부여 — "활성 노드" 표시를 카테고리 색과 별개로
  시각적으로 구분. `detail-panel`(노드 설명)도 2.2와 동일하게 `data-row` 전환 가능(비용/
  효과 수치 부분만).
- **신규**: 트리 노드 간 연결선(`UpgradeTreeView.DrawConnections`, Painter2D)에
  `--color-grid-line`/`--color-accent-glow-soft` 팔레트를 맞추면 "회로도" 인상 강화.
- **우선순위**: 중(노드 45개 전수 스타일 조정은 `UpgradeTreeView.CreateNodeElement()`가
  노드를 전부 C#으로 생성하므로 UXML만으로는 끝나지 않고 C# 쪽 클래스/라벨 부여 로직도
  같이 손봐야 함).
- **상세 설계**: HUD 다음으로 가장 오래 보는 화면이라는 점을 감안해 훨씬 깊게 다룬 설계안을
  **§7**에 별도로 정리했다 — 노드 내부 레이아웃, LOCKED/AVAILABLE/ACTIVE/MAXED 4단계 상태
  표현, 연결선 회로도화, 상세 패널의 "연구 분석 콘솔"화, 카테고리 "연구실(LAB)" 명명까지
  포함.

### 2.4 이벤트 상세 창 (신규 — 현재 없음)

- **Density Mode**: Tactical Readout Mode(`DESIGN.md` > UI Density Modes 참고) — event-dock
  피드의 확장 뷰이므로 HUD와 동일 모드를 따른다.
- **역할**: event-dock 피드는 한 줄 요약뿐 — 이벤트를 탭했을 때 "왜 발생했는지/효과가
  무엇인지" 자세히 보여줄 확장 뷰가 없다. "통제센터 경보 상세 판독" 컨셉으로 신규 설계
  필요.
- **재사용**: `tactical-panel` + `corner-cut` 4개 + `tactical-panel__header`(이벤트 제목) +
  본문은 `data-row`(발생 원인/영향받는 국가/효과 수치)로 구성. 심각도(Positive/Negative/
  Flavor)는 기존 `news-entry-dot--positive/--negative/--flavor` 색상을 그대로 상단
  스트라이프나 아이콘 칩(`stat-chip`)으로 재사용.
- **신규 컴포넌트**: `EventDetailPopupController.cs`(신규, 작게) — `NewsFeedController.
  AddEntry()`가 만드는 각 행에 클릭 콜백을 달아 해당 이벤트 데이터를 이 패널에 채우는
  방식. 팝업 골격은 2.7(모달)에서 만드는 공용 모달 프레임을 재사용.
- **우선순위**: 낮음~중(신규 기능이라 "스타일 확장"보다 "기능 추가"에 가까움 — 이번 작업의
  핵심 범위인 "기존 화면 재도장"과는 성격이 다르므로 별도 스코프로 분리 권장).

### 2.5 설정 창 (신규 — 현재 없음)

- **Density Mode**: Tactical Readout Mode(`DESIGN.md` > UI Density Modes 참고) — 공용 모달
  프레임(§2.7) 위에서 `data-row`(항목+토글) 구조를 쓰므로 Layer A로 분류한다.
- **역할**: 사운드/언어/데이터 초기화 등 — 통제센터의 "시스템 설정 패널".
- **재사용**: 2.7 공용 모달 프레임 + `data-row`(항목명 + 토글/슬라이더) 그대로 적용 가능한
  가장 단순한 화면. 새 시각 언어를 가장 적은 위험으로 시범 적용해볼 좋은 첫 대상이기도 함.
- **우선순위**: 낮음(설정 항목 자체가 아직 게임 기능으로 정의 안 됨 — TODO에 언급도 없음.
  화면이 실제로 필요해지는 시점에 맞춰 진행).

### 2.6 게임오버/엔딩 화면 (EndingScreen)

- **Density Mode**: Debrief Mode(`DESIGN.md` > UI Density Modes 참고)
- **역할**: 결과 리포트 — "작전 종료 브리핑". 극적인 느낌(현재 `--color-bg-scrim` 풀스크린
  암전)은 유지하되 결과 수치(`ending-detail`/`ending-score`)를 판독 데이터로 전환하면 게임
  전체와의 일관성이 생김.
- **재사용**: `ending-root`는 유지(스크림 배경 자체는 tactical 언어와 무관, 연출 목적).
  `ending-detail` 여러 줄을 `data-row`(생존/사망/붕괴국 수 등 항목별)로 재구성 — 현재는
  문단 나열이라 항목 간 구분이 약함. `ending-score`는 `tactical-panel` 프레임(코너컷 포함)
  으로 감싸 "최종 스코어 판독창"처럼 강조.
- **신규**: 없음.
- **우선순위**: 중(플레이 마무리 인상에 영향 크지만 화면 요소 수가 적어 작업량 작음).

### 2.7 팝업 및 모달 UI (공용 프레임 신규 설계, 죽은 코드 재활용)

- **Density Mode**: Tactical Readout Mode(`DESIGN.md` > UI Density Modes 참고) — CountryPopup
  기반 공용 모달이며, 이 모달 위에서 소비되는 화면(2.4/2.5)도 동일 모드를 따른다.
- **역할**: 이벤트 상세/설정/확인창 등 여러 화면이 공유할 **공용 모달 골격**이 지금 없다 —
  각 화면이 팝업을 만들 때마다 새로 UXML을 짤 위험. `CountryPopup.uxml/uss`(죽은 코드,
  Country Dock으로 대체되어 클릭 트리거만 제거된 상태)가 이미 "화면 중앙 고정폭 팝업"
  골격을 갖추고 있으므로 **새로 만들지 말고 이 구조를 공용 모달 베이스로 재활용**한다.
- **재사용**: `popup-root`(중앙 고정폭) 구조를 `tactical-panel`로 전환 + 코너컷 4개 추가.
  `popup-title`은 `tactical-panel__title`, `popup-row`는 `data-row`로 대체. 배경은
  `--color-bg-panel` 그대로(이미 국가현황/랭킹과 톤이 맞음).
- **신규**: `TacticalModalController.cs` 같은 범용 열기/닫기 헬퍼로 승격(현재
  `CountryPopupController` 로직 — Show/Hide + 배경 클릭 시 닫힘 등 — 을 국가 전용에서
  범용으로 일반화)하면 2.4(이벤트 상세)/2.5(설정)이 전부 이 위에서 재사용 가능.
- **우선순위**: **높음** — 다른 신규 화면(이벤트 상세/설정)의 전제 조건이라 먼저 만들어두는
  게 전체 일정에 유리.

### 2.8 알림(Notification) UI (신규 — 현재 없음)

- **Density Mode**: Tactical Readout Mode(`DESIGN.md` > UI Density Modes 참고) — HUD 크롬의
  연장선(resource-strip 하단 배너)이므로 Layer A를 따른다.
- **역할**: "국경 붕괴", "치료제 발견 임박" 같은 중대 이벤트를 피드 스크롤 안이 아니라 화면
  상단에 잠깐 떠서 즉시 시선을 끄는 배너. DEFCON류 "경보 배너"에 해당.
- **재사용**: `world-status-frame`(위험 시에만 강조 배경 붙는 기존 패턴)을 확대한 형태로
  설계 — 평소엔 안 보이다가 트리거 시 `resource-strip` 바로 아래 얇은 배너로 슬라이드 인.
  색상은 severity 체계(danger=적, info=파랑) 그대로. 코너컷은 배너가 얇아 생략(패널이
  아니라 스트립 형태이므로 코너컷 언어보다는 좌측 `stat-chip` 색상 칩 언어가 더 어울림).
- **신규**: `NotificationBannerController.cs`(신규, `EventManager`가 이미 발행하는 이벤트
  중 `NewsEventCategory.Negative` 이상 심각도만 필터링해 트리거 — 데이터 소스는 100%
  재사용, 표시 레이어만 추가).
- **우선순위**: 낮음(있으면 좋지만 기존 event-dock으로 이미 정보 전달은 되고 있어 필수는
  아님).

---

## 3. 컴포넌트 적용 매트릭스

컴포넌트 정의·색상·CSS는 전부 `DESIGN.md` Component Library 참고 — 이 표는 **어느
컴포넌트가 어느 화면에 적용되는지**만 정리한다.

| 컴포넌트(DESIGN.md 참고) | 적용/재사용 화면 |
|---|---|
| `tactical-panel` + corner-cut | 전 화면 패널의 기본 프레임(MainMenu 카드+상세, CountrySelect 상세, EndingScreen 통계+스코어, RankingPanel, Modal) |
| `data-row`/`data-label`/`data-value` | CountrySelect 국가카드, UpgradeTree 노드 상세, EndingScreen 결과, 이벤트 상세 |
| severity 색상 | 모든 data-value 강조, 알림 배너 |
| `stat-chip` | 알림 배너, 이벤트 상세 심각도 표시 |
| `world-status-frame` 패턴(평시 숨김/위험시 강조) | 알림 배너의 트리거 로직 모델 |
| `graph-frame`(기준선+격자선) | 향후 랭킹 화면에 추세 그래프 추가할 경우 재사용 |
| `CountryPopup.uxml/uss`(죽은 코드) | 공용 모달 프레임의 물리적 시작점(§2.7) |

---

## 4. USS 클래스 구조 — 파일 배치

```
Assets/UI/
├── Theme.uss          (기존 — 토큰은 DESIGN.md가 정본, 값 변경 시 여기 한 곳만 수정)
├── Tactical.uss        (생성 완료 — tactical-panel/data-row/corner-cut 등 공용 클래스.
│                         재검증 결과는 §15 Historical Notes 참고)
├── Hud.uss             (기존 — event-dock/country-dock 등. Tactical.uss와 일부 클래스가
│                         로컬 중복 정의돼 있음, §15 참고. 당장 리팩터 우선순위는 낮음)
├── MainMenu.uss         (data-row 등 추가 클래스만 덧붙임, 기존 클래스 유지)
├── CountryStatusPanel.uss
├── UpgradeTree.uss      (Tactical.uss와 일부 클래스 로컬 중복, §15 참고)
├── EndingScreen.uss
├── RankingPanel.uss
├── CountryPopup.uss     (→ TacticalModal.uss로 개명 검토, §2.7 참고)
└── (신규) EventDetailPopup.uss / SettingsPanel.uss / NotificationBanner.uss
```

각 화면 UXML의 `<Style src="…">` 순서만 `Theme.uss` → `Tactical.uss` → `화면.uss`로 한 줄
추가하면 끝난다(모든 uxml이 이미 `Theme.uss`를 최우선 로드하는 규칙을 따르고 있으므로 동일
패턴 반복).

---

## 5. 모바일 세로 화면 최적화

- 타겟 해상도(1440×3120, 19.5:9)는 CLAUDE.md에 이미 고정 — 가로 모드 대응 불필요, 그대로
  적용.
- 코너컷/헤어라인은 좁은 폭에서도 두께 절대값(1~2px)이라 스케일 왜곡 없음 — %기반
  레이아웃과 충돌하지 않는다(국가현황 패널이 이미 % 인셋으로 전환된 선례 재사용, Step 61
  이전 커밋 참고).
- `data-row`는 `justify-content: space-between`이라 좁은 폭에서도 라벨/값이 항상 좌우
  끝에 고정 — CountrySelect처럼 리스트 폭이 좁은 화면에 특히 적합.
- 터치 타겟(`--touch-target-min: 48px`)은 코너컷/헤어라인 장식과 무관한 별도 레이어이므로
  시각 언어 확장이 버튼 탭 영역을 줄이지 않음(장식은 `picking-mode: Ignore`로 항상 클릭
  통과 — 이 규칙 자체는 `DESIGN.md` Usage Rules > Corner Cut에 있음).
- 코너컷의 배치 위치(화면 루트 금지, 개별 패널 단위만)와 밀도 기준(패널 수 적음 → 4개
  전부, 리스트 행처럼 많음 → accent-bar)은 `DESIGN.md` Usage Rules > Corner Cut 참고 —
  전체화면 패널(MainMenu/UpgradeTree)에 코너컷을 화면 네 귀퉁이에 직접 붙이면 SafeArea와
  겹칠 수 있으므로 그 규칙을 그대로 따른다.

---

## 6. 구현 우선순위

| 순위 | 대상 | 이유 |
|---|---|---|
| 1 | CountrySelect (2.2) | 데이터가 Country Dock/국가현황 패널과 3중 중복 — 통일 효과 최대, 리스크 최소(클래스만 추가) |
| 2 | 공용 모달 프레임 (2.7) | 이벤트 상세/설정 등 후속 신규 화면의 전제 조건 |
| 3 | MainMenu (2.1) | 게임 진입 첫 화면, 작업량 적음 |
| 4 | EndingScreen (2.6) | 요소 수 적어 작업량 작음, 마무리 인상 개선 |
| 5 | UpgradeTree (2.3) | 노드 45개 규모라 손이 많이 감(코드 생성 로직 확인 필요) |
| 6 | 알림 배너 (2.8) | 신규 기능 — 스타일보다 로직 비중 큼 |
| 7 | 이벤트 상세 창 (2.4) | 신규 기능, 공용 모달(2순위) 완료 후 착수 |
| 8 | 설정 창 (2.5) | 게임 기능 자체가 아직 미정의 — 항목이 정해진 뒤 진행 |

각 순위는 독립적으로 진행 가능(서로 blocking 관계 없음, 단 4/7/8은 2번 공용 모달을 전제로
함). 매 항목 완료 시 CLAUDE.md 최근 작업 표에 Step 번호로 기록하고, 상세 근거는 이 문서가
아니라 `Docs/DevLog.md`에 남긴다(프로젝트 규칙 — CLAUDE.md는 현황 브리핑, DevLog는 이력).

---

## 7. UpgradeTree — "연구실" 전환 상세 설계

**Density Mode**: Tactical Readout Mode(§0.0/`DESIGN.md` > UI Density Modes 참고)

> **⚠ 이중으로 superseded됨 (문서 무결성 감사, 2026-07-22)** — 아래 7.1~7.9는 Step 62 시점의
> 설계(detail-panel + 단일 실선 연결 + 노드 상태 4클래스)를 서술한다. 이후 **두 번** 더
> 뒤집혔다: (1) **UpgradeTree 캔버스 승격**(Commit 1~5, `Docs/Archive/unity-editor-task.md`
> §11 참고)이 `detail-panel`/`branch-board`/`research-row`를 전부 제거하고 Painter2D 기반
> 절대좌표 트리 캔버스(`TreePathElement` 연결선 + `tree-node`)로 교체, 노드 상세는
> `ResearchPopupController`(별도 팝업)로 이관. (2) **HUD Overlay Architecture 리팩터**(이번
> 세션, 2026-07-22)가 카테고리별 UIDocument 3개(TransmissionTreeUI/SymptomTreeUI/
> AbilityTreeUI) 구조를 CountryStatusPanel과 동일한 **UIDocument 1개** 구조로 통합. 즉 아래
> §7.1(현재 구조 분석)의 `detail-panel`/`node-scroll`(빈 채로 C#이 채움)/연결선 서술은
> **현재 코드와 다르다.** 현재 구조의 정본은 `Assets/UI/UpgradeTree.uxml`/`UpgradeTree.uss`/
> `UpgradeTreeView.cs` 코드 자체와 `Docs/DESIGN.md`(17/21절, "UpgradeTree 캔버스 복귀"로
> 이미 갱신됨)다. 이 절은 "레이아웃을 어떻게 개선했었는가"의 역사 기록으로만 남겨둔다 —
> 실행 참고용으로 쓰지 말 것.

핵심 플레이 루프(지도 관찰 ↔ 업그레이드 선택 반복)상 HUD 다음으로 가장 오래 보는 화면인데도
아직 일반 스킬트리 톤이라 통제센터(HUD=상황실) ↔ 연구실(UpgradeTree) 관계가 느껴지지 않는다.
이 절은 **레이아웃 전면 교체 없이, 기존 좌표 배치·연결선·해금 로직을 그대로 둔 채** 스타일과
"표시 방식"만 바꾸는 안이다. 실제 게임플레이 규칙(`UpgradeManager.CanUnlock`/`TryUnlock`/
`GetEffectiveCost`)은 손대지 않는다 — 전부 읽기 전용으로만 참조한다.

### 7.1 현재 구조 분석

- UXML(`UpgradeTree.uxml`): `upgrade-header`(카테고리 제목+이전/다음 화살표, DNA·광고·닫기
  버튼) → `node-scroll`(빈 ScrollView, 내용은 전부 C#이 채움) → `detail-panel`(제목 Label +
  설명 Label 한 덩어리 + 구매 버튼).
- C#(`UpgradeTreeView.cs`): 노드 45개 중 이 창이 담당하는 카테고리 9개만 필터링해
  `CreateNodeElement()`로 매번 새로 생성(절대좌표 배치, 열간격 140px/행간격 110px/카테고리간
  240px — `DefaultUpgradeTreeFactory.cs`). 노드 하나는 라벨 2개(이름, 비용/"해금됨")뿐이고
  클래스는 `tree-node`/`tree-node--{category}`/`tree-node--unlocked`/`tree-node--selected`
  4종이 전부 — **상태가 사실상 이분법(해금 전/후)** 이라 LOCKED와 AVAILABLE이 시각적으로
  구분되지 않는다(둘 다 "N DNA"라는 같은 톤의 텍스트만 보임).
- 연결선은 `DrawConnections()`가 `Painter2D`로 직선 하나만 긋는다 — 선행 해금 시 녹색 실선
  (3px), 아니면 흐린 흰색(2px, alpha 0.2). 대각선 직선이라 회로도보다는 "실선 연결" 느낌.
- 상세 패널은 `BuildDescription()`이 비용/선행노드/효과를 **문자열 하나로 이어붙여** 한
  Label에 넣는다 — 항목별 구분이 없어 "설명문"처럼 읽힌다.
- **결론**: 데이터 모델(`UpgradeNode`/`UpgradeManager`)은 이미 상태 판단에 필요한 정보(해금
  여부, 선행조건 충족 여부, 잔여 DNA 대비 구매 가능 여부)를 전부 갖고 있다 — 부족한 건 이를
  4단계로 나눠 **표시**하는 코드뿐이다. 즉 이번 작업은 순수 프레젠테이션 레이어 확장이다.

### 7.2 노드 상태 4단계 — 판정 방법

색상/테두리/강조 방식은 `DESIGN.md` Color System §7(노드 상태), Usage Rules > Node State
Colors가 정본이다. 여기서는 **화면(UpgradeTree)에서 4단계를 판정하는 계산 로직**만 다룬다 —
기존 public API만으로 읽기 전용 계산(신규 게임 로직 없음):

```csharp
// UpgradeTreeView.cs — CreateNodeElement() 안에서 계산 (신규 필드/로직 없음, 전부 조회)
bool prereqsMet = node.prerequisites.All(pid => UpgradeManager.Instance.GetNode(pid)?.isUnlocked ?? true);
bool affordable = WorldDataManager.Instance.State.dnaPoints >= UpgradeManager.Instance.GetEffectiveCost(node);
bool isLeaf = !UpgradeManager.Instance.Tree.Any(n => n.prerequisites.Contains(node.id)); // 이 노드를
                                                                                          // 선행조건으로 삼는 다음 노드가 없음

string state =
    node.isUnlocked ? (isLeaf ? "maxed" : "active")
    : !prereqsMet    ? "locked"
    : "available";   // affordable 여부는 available 안에서 서브 톤(밝기)으로만 구분, 별도 상태 아님
```

| 상태 | 게임 의미 |
|---|---|
| LOCKED | 선행조건 미충족 |
| AVAILABLE | 선행 충족, 구매 대기(`affordable=false`면 `stat-chip`만 회색 톤) |
| ACTIVE | 해금 완료, 효과 적용 중 |
| MAXED | 해금 완료 + 그 갈래의 마지막 노드(더 진행할 다음 노드 없음) |

`state` 문자열은 그대로 `tree-node--{state}` 클래스명이 된다(`tree-node--locked/
--available/--active/--maxed`) — 실제 CSS는 `UpgradeTree.uss`에 있고 규칙은 `DESIGN.md`
Component Library(tree-node) 참고.

### 7.3 노드 내부 레이아웃 — "연구 모듈 카드"

`CreateNodeElement()`가 라벨 2개만 넣던 것을 4개 요소의 세로 스택으로 바꾼다(구조만 추가,
`node.id`/`effects`/`cost` 등 읽는 데이터는 동일):

```
┌──────────────────────┐
│ TRANS-001             │  ← code (data-label 톤, 자간 넓힘) — node.id 기반 자동 생성
│ 공기 전파 I            │  ← name (기존 DisplayName() 그대로, tree-node__label 유지)
│ STATUS: ACTIVE         │  ← state (7.2 상태, tree-node__status 신규)
│ DNA 12                 │  ← cost/기존 tree-node__cost, MAXED/ACTIVE면 "COMPLETE"로 대체
└──────────────────────┘
```

`code`(예: `TRANS-001`)는 새 데이터 필드가 아니라 **카테고리 접두어 + 그 카테고리 내
순번**으로 뷰에서 즉석 계산한다(정렬 기준은 이미 있는 `position.y`(티어)→`position.x`
(갈래) 오름차순):

```csharp
private static string CategoryPrefix(UpgradeCategory c) => c switch
{
    UpgradeCategory.Transmission => "TRANS",
    UpgradeCategory.Symptom => "SYM",
    UpgradeCategory.Ability => "ADAPT",
    _ => "NODE"
};
// nodes는 이미 category로 필터링된 리스트 — 순번만 부여
var ordered = nodes.OrderBy(n => n.position.y).ThenBy(n => n.position.x).ToList();
string code = $"{CategoryPrefix(node.category)}-{(ordered.IndexOf(node) + 1):000}";
```

노드 박스 자체(테두리 두께/색)는 7.2 상태 규칙(`DESIGN.md` 참고)을 그대로 적용하고,
카테고리 구분은 기존처럼 전체 테두리 색으로 우선순위를 두지 않고 **좌측 4px 세로 바
(accent bar)** 하나로 축소한다 — 상태색(테두리)과 카테고리색(좌측 바)이 동시에 보여도
서로 위계가 겹치지 않도록 분리하기 위함(`DESIGN.md` Usage Rules > Node State Colors 참고).

### 7.4 상세 정보 패널 — "연구 분석 콘솔"

`detail-panel`을 `tactical-panel`(코너컷 4개 + 헤더) 구조로 바꾸고, `BuildDescription()`이
만들던 문자열 한 덩어리를 `data-row` 여러 줄로 분해한다.

```
┌ SYMPTOM ANALYSIS ─────────────┐  ← tactical-panel__title (카테고리 ANALYSIS 고정 접미)
│ TRANSMISSION      +2          │  ← data-row (effects 항목별 1행, statName 한글 라벨 매핑)
│ SEVERITY          +1          │
│ LETHALITY         +0          │
│ ──────────────────────────    │  ← grid-line 구분
│ DNA COST          5            │  ← GetEffectiveCost() 그대로
│ STATUS            AVAILABLE   │  ← 7.2 상태, data-value 색상 적용
└────────────────────────────────┘
      [ 구매 (5 DNA) ]            ← 기존 buy-button 그대로
```

`SelectNode()`의 역할은 그대로(선택 노드 조회 → 버튼 활성화 판정)이되, `_detailDesc` 라벨
하나에 문자열을 채우던 부분만 `data-row` 리스트를 만드는 헬퍼로 교체한다(효과 없는 노드는
"효과 없음" 한 줄만 남김). UXML의 `detail-panel__desc` 자리를 `ui:ScrollView` 또는 빈
`VisualElement` 컨테이너(`detail-rows`)로 바꿔 C#이 `data-row`를 Add하는 방식 — Country
Dock/국가현황 패널에서 이미 검증된 "컨테이너 하나 + 행 여러 개" 패턴 재사용.

### 7.5 카테고리 명명 — "연구실(LAB)"

`UpgradeTreeView.CategoryLabel()` 한 함수만 바꾸면 전체 반영(호출부 변경 없음):

| 카테고리 | 기존 | 제안 |
|---|---|---|
| Transmission | 감염 경로 | TRANSMISSION LAB |
| Symptom | 증상 | SYMPTOM LAB |
| Ability | 능력 | ADAPTATION LAB |

한글 UI 안에서 영문 캡션만 쓰면 이질감이 있을 수 있어, `category-title-label`은 영문
캡션(작게, `tracking-caption` 자간) + 그 아래 기존 한글 라벨(크게)을 2줄로 병기하는 것을
권장 — HUD의 `world-status-frame__caption`(영문 대문자 캡션 + 값)과 동일한 문법이라 두
화면이 같은 "표기 규칙"을 공유하게 된다.

### 7.6 연결선 — "회로도화"

현재: 대각선 직선 1개, 색은 활성(녹색 solid)/비활성(흰색 alpha 0.2) 2단계뿐.

제안(전부 `DrawConnections()` 내부 `Painter2D` 호출만 바꾸면 됨 — 좌표 계산 로직·선행조건
순회는 그대로):

- **색상 통일**: 활성 선은 `--color-accent-glow`(HUD 헤어라인과 동일 색), 비활성 선은
  `--color-grid-line`(HUD 격자선과 동일 색) — 화면마다 색 정의를 새로 만들지 않고
  `Theme.uss` 토큰을 그대로 `Color` 값으로 변환해서 쓴다(코드에서 `new Color(150/255f,
  255/255f, 185/255f, 1f)` 식으로 하드코딩하되, 주석에 "Theme.uss --color-accent-glow와
  동일값" 명시 — USS 변수를 C#에서 직접 읽을 수는 없어 값 동기화는 수동이지만 최소 1곳에서만
  관리).
- **경로를 대각선 → 꺾은선(elbow)으로 변경**: `MoveTo(from)` → `LineTo(중간점 x는 to.x,
  y는 from.y)` → `LineTo(to)` 2단 折선으로 바꾸면 회로 기판의 배선처럼 보인다(선행조건이
  보통 같은 열(x)에서 다음 티어(y)로만 이어지므로 대부분 케이스에서 자연스러운 ㄴ자/ㄱ자
  꺾임이 됨).
- **연결점 마커**: `from`/`to` 좌표에 지름 4px 정사각형을 `painter.BeginPath()`+`Rect()`
  (또는 작은 원)로 추가 — "포트/단자" 느낌의 마감점, 국가현황 패널의 `news-entry-dot`과
  동일하게 "점 하나로 위치를 찍는" 언어를 재사용.
- **MAXED 노드에서 뻗어나가는 선**은 굵기를 살짝 키워(4px) 완료된 갈래의 배선이 시각적으로
  "완결된 회로"처럼 보이게 강조 가능(선택 사항).

`graph-frame`(스파크라인 배경)을 캔버스 전체 배경으로 그대로 가져다 쓰는 것은 권장하지
않는다 — `graph-frame`은 좁은 고정 크기 위젯 전용으로 설계된 프레임(104×52px)이라 가변
크기의 노드 캔버스(카테고리별로 최대 canvasWidth/Height가 다름)에는 맞지 않는다. 대신
`--color-grid-line` 팔레트"만" 캔버스 배경에 옅게 깔아(예: `node-scroll`에
`background-color: rgba(150,255,185,0.03)` 정도의 아주 옅은 톤 1단) 회로 기판 재질감만
차용한다.

### 7.7 모바일 세로 최적화

- 카테고리당 9노드 세로 스크롤(이미 `ScrollViewMode.Vertical`로 전환돼 있음, Step 이력상
  폴리싱 완료 — 유지) — 7.3의 4줄 레이아웃으로 노드 높이가 늘면(현재 60px) 스크롤 총
  길이만 늘어날 뿐 가로 폭에는 영향 없음. `NodeHeight` 상수만 살짝 키우는 것(60→약 72px,
  4줄이 들어갈 여유)을 권장 — `DefaultUpgradeTreeFactory`의 행 간격(110px)이 이미 여유
  있어 겹칠 걱정 없음.
- 상태 색상 4단계를 처음 보는 플레이어를 위해, 헤더(`upgrade-header`) 아래에 작은 범례
  (`LOCKED · AVAILABLE · ACTIVE · MAXED` + 색상 칩 4개, `stat-chip` 재사용)를 한 줄
  추가하는 것을 고려할 수 있다 — HUD의 `world-status-frame`처럼 "상시 표시되는 판독 보조선"
  역할.
- 코드(`TRANS-001`)/상태(`STATUS: ACTIVE`) 캡션은 `font-size-xs`(13px)로 이미 충분히 작아
  4줄이 60~72px 안에 들어간다 — 폰트 축소나 별도 트렁케이션 로직 추가 불필요.

### 7.8 UXML 구조 제안

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="Theme.uss" />
    <Style src="Tactical.uss" />
    <Style src="UpgradeTree.uss" />
    <ui:VisualElement name="upgrade-root" class="upgrade-root">
        <ui:VisualElement name="upgrade-header" class="upgrade-header tactical-panel__header">
            <!-- 기존 upgrade-header__nav / __actions 그대로, 클래스만 추가 -->
            ...
        </ui:VisualElement>

        <ui:ScrollView name="node-scroll" class="node-scroll" .../>
        <!-- 내용은 여전히 C#이 채움 — UXML 변경 없음 -->

        <ui:VisualElement name="detail-panel" class="detail-panel tactical-panel">
            <!-- corner-cut 4개: 직접 반복 삽입(템플릿 미사용 — §15 Historical Notes 참고) -->
            <ui:VisualElement class="corner-cut corner-cut--tl" picking-mode="Ignore" />
            <ui:VisualElement class="corner-cut corner-cut--tr" picking-mode="Ignore" />
            <ui:VisualElement class="corner-cut corner-cut--bl" picking-mode="Ignore" />
            <ui:VisualElement class="corner-cut corner-cut--br" picking-mode="Ignore" />
            <ui:VisualElement class="tactical-panel__header">
                <ui:Label name="detail-title" class="tactical-panel__title" />
            </ui:VisualElement>
            <ui:VisualElement name="detail-rows" class="detail-rows" />
            <!-- 기존 detail-desc 라벨 자리를 컨테이너로 교체 — C#이 data-row를 Add -->
            <ui:Button name="buy-button" text="구매" class="detail-panel__buy" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

> 이 절을 처음 쓸 당시엔 `<ui:Template>`+`<ui:Instance>`로 corner-cut 4개를 템플릿화하는
> 안(`CornerCutSet.uxml`)을 함께 제안했으나, 실제 구현(`UpgradeTree.uxml`)은 위처럼 4줄을
> **직접 반복 삽입**했다 — 템플릿 메커니즘은 이 프로젝트에서 실증된 적이 없다(§15
> Historical Notes 참고). 위 스니펫도 그 실제 방식에 맞춰 갱신했다.

변경 라인 수는 `<Style>` 1줄 추가, `detail-panel` 내부만 재구성 — `upgrade-header__nav`/
`__actions`, `node-scroll` 자체는 완전히 그대로 유지.

### 7.9 상태별 클래스 매핑

색상/테두리 CSS는 `UpgradeTree.uss`에 정의돼 있고 규칙은 `DESIGN.md` Component Library
(tree-node)가 정본이다. 여기서는 7.2의 `state` 문자열이 실제로 어떤 클래스에 대응하는지만
남긴다:

| state 문자열 | 클래스 |
|---|---|
| `locked` | `tree-node--locked` |
| `available` | `tree-node--available` |
| `active` | `tree-node--active` |
| `maxed` | `tree-node--maxed` |

기존 `tree-node--unlocked`/`tree-node--selected` 클래스명은 그대로 남겨도 무방(위 4단계와
병행 사용 — `unlocked`는 "해금 여부" 원본 플래그, `active`/`maxed`는 그 위에 얹는 세분화
표시이므로 서로 대체가 아니라 계층 관계).

### 7.10 정리 — "상황실 ↔ 연구실" 관계

| | HUD(상황실) | UpgradeTree(연구실) |
|---|---|---|
| 배경 톤 | `--color-bg-scrim*` (지도 위 오버레이) | `--color-bg-panel-alt` (독립 콘솔 화면) — 유지 |
| 판독 단위 | `country-dock__row` (국가 상태) | `data-row` (효과 수치) — 동일 계약 재사용 |
| 발광 강조 | `accent-glow` 헤어라인/코너컷 | 동일 토큰, ACTIVE/MAXED 노드 강조로 재해석 |
| 상태 신호 | severity 색상(감염/사망/위험) | 브랜드 색상(DNA초록/금색) — 의미 축 분리, 톤은 공유 |
| 캡션 문법 | `world-status-frame__caption`(영문 대문자+값) | `category-title-label` 2줄 병기(영문 LAB+한글) |

두 화면이 서로 다른 색상 의미(국가 데이터 vs 노드 상태)를 쓰면서도 같은 토큰·같은 코너컷/
헤어라인/data-row 문법을 공유하기 때문에, "같은 통제센터 안의 다른 콘솔"이라는 인상이
성립한다. 색을 분리한 이유 자체는 `DESIGN.md` Usage Rules > Severity Colors / Node State
Colors 참고.

---

## 8. MainMenu — "PATHOGEN BRIEFING TERMINAL" 상세 설계

**Density Mode**: Briefing Terminal Mode(§0.0/`DESIGN.md` > UI Density Modes 참고)

**현재 구조**(`MainMenu.uxml`/`MainMenuController.cs`): `pathogen-list`(ScrollView) 안에
`pathogen-card`(이름 + 스탯 4종을 `■■■□□` 텍스트 막대로 합쳐 한 Label에 표시) 반복 → 선택 시
`detail-panel`(제목 + `FlavorText` 문단) → `next-button`. 병원체는 6종뿐이라(카드 수가 적음)
UpgradeTree(45노드)나 CountrySelect(48행)만큼 밀도 압박이 없다 — 오히려 카드를 조금 여유
있게 꾸며도 되는 화면.

- **pathogen-card 개선**: `tactical-panel` 보더 톤(1px `accent-glow-soft`) + **코너컷 4개
  전부**(카드가 6개뿐이라 4개를 다 써도 화면이 붐비지 않음 — `DESIGN.md` Usage Rules >
  Corner Cut의 "패널 수가 적으면 4개 전부" 기준). 스탯 표시는 `StatBar()`(■□ 텍스트)를
  그대로 두되, 하나의 Label에 4개를 이어붙이던 것을 `data-row` 4줄(전염력/중증도/치사율/
  내성 각 1행, 값 칸에 막대 텍스트)로 분해 — `MainMenuController.CreatePathogenCard()`만
  손보면 됨(읽는 데이터 `pathogen.Infectivity` 등은 동일, 표시만 분해).
- **detail-panel 개선**: `tactical-panel` + 코너컷 4개로 감싸고 헤더를 `tactical-panel__header`
  (하단 그리드라인)로. `FlavorText`는 서술형 문장이라 `data-row`로 쪼개지 않고 문단 그대로
  유지(§1 원칙 #6 — 정보 밀도보다 가독성이 우선인 케이스).
- **선택 상태 개선**: 기존 `pathogen-card--selected`(금색 테두리)는 그대로 유지 —
  CountrySelect의 `country-row--selected`와 동일한 "선택 확정" 언어라 병원체 화면만
  다르게 갈 이유가 없다(§1 원칙 #2, 기존 클래스 병기).
- **tactical-panel 적용 방법**: `pathogen-card`/`detail-panel` 클래스에 `tactical-panel`을
  추가 병기 — 배경색은 각 화면 고유값(`pathogen-card`는 `--color-surface` 그대로) 유지하고
  `tactical-panel`은 테두리/헤더 규약만 제공(`DESIGN.md` Usage Rules > Tactical Panel,
  배경색 강제 없음).
- **코너컷 적용 위치**: `pathogen-card` 6개 전부 + `detail-panel` 1개. 화면 루트
  (`mainmenu-root`) 자체에는 붙이지 않는다(`DESIGN.md` Usage Rules > Corner Cut — SafeArea
  충돌 방지).
- **정보 계층 구조 개선**: `mainmenu-title`(현재 "전염병 주식회사") 위에 영문 캡션 한 줄
  ("PATHOGEN BRIEFING TERMINAL", `tracking-caption` 자간, `--color-accent-glow`) 추가 —
  7.5의 "영문 캡션 + 한글 타이틀 2줄 병기" 문법을 그대로 재사용해 두 화면의 표기 규칙을
  통일한다. 서브타이틀("병원체를 선택하세요")은 그대로 유지.

---

## 9. CountrySelect — "GLOBAL DEPLOYMENT TERMINAL" 상세 설계

**Density Mode**: Briefing Terminal Mode(§0.0/`DESIGN.md` > UI Density Modes 참고)

**현재 구조**(`CountrySelect.uxml`/`CountrySelectController.cs`): `country-list`
(ScrollView) 안에 `country-row`(국기 + 이름 + "인구 N · 개발수준" 한 줄 meta) 48행 반복 →
선택 시 `detail-panel`(제목 + "인구/기후/의료 수준" 3줄 문단) → `back-button`/`start-button`.
**주의**: `CountrySelect.uxml`은 별도 파일이지만 `MainMenu.uss`를 그대로 참조한다(클래스명
100% 동일) — 즉 §8에서 `pathogen-card`/`detail-panel` 관련 클래스를 바꾸면 이 화면에도
자동 반영된다. 반대로 `country-row` 전용 클래스는 이 화면에만 쓰이므로 별도로 다뤄야 한다.

- **국가 리스트 개선**: `country-row`는 48행이나 되므로(6개뿐인 pathogen-card와 달리)
  코너컷은 생략하고 UpgradeTree `tree-node` 선례처럼 **좌측 accent bar**(4px)로 대체 —
  색상은 국가의 `developmentLevel`(의료수준)에 매핑해 스캔하면서 국가별 위험도를 감지할 수
  있게 한다(아래 severity 활용 방안 참고). 국기(`country-row__flag`)/이름은 그대로 두고,
  `country-row__meta`(현재 "인구 N · 개발수준" 한 줄 텍스트)만 `data-row` 2줄(인구/
  의료수준)로 분해할지는 리스트 행 높이 제약(48개 스크롤) 때문에 신중히 — **한 줄 유지
  권장**, data-row 분해는 아래 상세 패널에서만 진행.
- **국가 상세 패널 개선**: `detail-panel`(선택 국가)을 `tactical-panel` + 코너컷 4개 +
  `data-row` 3줄(인구/기후/의료수준)로 완전 전환 — `CountrySelectController.SelectCountry()`
  가 문자열 한 덩어리(`_detailDesc.text = $"인구: ...\n기후: ...\n의료 수준: ..."`)로
  채우던 부분을 UpgradeTree `AddDetailRow()`와 동일한 패턴의 헬퍼로 교체.
- **Country Dock 시각 언어 재사용 — 이 화면의 핵심 요구사항**: Country Dock
  (`country-dock__row`/`__label`/`__value`)과 CountrySelect의 detail-panel이 **물리적으로
  같은 CSS 클래스**(`data-row`/`data-label`/`data-value`, 현재 `Tactical.uss`로 이미
  승격되어 있음)를 쓰게 되면 두 화면은 클래스 정의가 동일해 "완전히 같은 시각 언어"가
  자동으로 보장된다 — 화면마다 각자 비슷하게 베끼는 것보다 훨씬 강한 일관성. 다만 **데이터
  항목 자체는 다르다**: Country Dock은 게임 진행 중(인구/감염률/사망률/의료수준/국경/상태/
  항공·항구, Step 61에서 확장) 값을 보여주고, CountrySelect는 게임 시작 전(감염률·사망률이
  항상 0이라 의미 없음)이므로 인구/기후/의료수준만 보여준다 — **문법은 같고 내용만 화면
  성격에 맞게 다른 것**이 정확한 관계다.
- **data-row 적용**: 위 상세 패널 3줄 + (선택 시) 국기/이름 헤더를 `tactical-panel__header`로.
- **tactical-panel 적용**: `detail-panel`에 적용(2.2/8절과 동일 패턴), `country-row`는
  리스트 항목이라 미적용(코너컷·풀 패널 프레임 대신 accent bar만).
- **severity 색상 활용 방안**: `developmentLevel`을 감염병 대응 역량의 "위협 신호"로
  재해석 — `High`(선진국) → `--color-status-info`(안정적, 파랑), `Mid`(개발도상국) →
  `--color-text-secondary`(중립), `Low`(저개발국) → `--color-status-danger`(취약, 적).
  이건 이미 게임 로직에 있는 3단계 enum을 색으로 매핑하는 것뿐이라 새 데이터 없이 바로
  적용 가능 — "이 국가에서 시작하면 의료 붕괴가 더 빠르다"는 전략적 신호를 발원 국가 선택
  단계에서부터 미리 읽을 수 있게 해준다(Plague Inc류 게임에서 실제로 유의미한 선택 기준).
  이 색 자체의 의미 고정은 `DESIGN.md` Usage Rules > Severity Colors 참고.

---

## 10. EndingScreen — "OPERATION REPORT / FINAL PANDEMIC ANALYSIS" 상세 설계

**Density Mode**: Debrief Mode(§0.0/`DESIGN.md` > UI Density Modes 참고)

**현재 구조**(`EndingScreen.uxml`/`EndingScreenController.cs`): `result-title`(승/패 텍스트) +
`result-detail`("N일 경과") + `score-label`("바이오하자드 점수: N") + 부활/재시작 버튼 2개.
`GLOBAL INFECTED`/`GLOBAL DEATHS`/`COLLAPSED NATIONS`는 지금 화면에 아예 없는 항목이다 —
아래는 실제 존재하는 데이터를 확인한 결과다:

| 요청 항목 | 실제 데이터 소스 | 비고 |
|---|---|---|
| GLOBAL INFECTED | `WorldDataManager.State.infectedCount` | **주의**: 이건 "누적 감염 경험자"가 아니라 종료 시점의 "현재 감염자 수" 스냅샷이다(이 게임은 회복 개념이 없어 감염자=현재 감염 중인 사람). 라벨을 그대로 "GLOBAL INFECTED"로 써도 무방하나, 오해 소지가 있다면 "감염자(종료 시점)"처럼 부제를 다는 것을 권장 |
| GLOBAL DEATHS | `WorldDataManager.State.deadCount` | 누적 사망자라 그대로 사용 가능, 정확함 |
| COLLAPSED NATIONS | **신규 파생값** — `WorldDataManager.Countries.Count(c => c.GetCollapseStage() >= CountryCollapseStage.FullCollapse)` | 저장된 필드가 아니라 종료 시점에 48개국을 순회해 집계하는 값. 신규 게임 로직이 아니라 이미 있는 `GetCollapseStage()`(국가현황 패널·Country Dock에서 이미 쓰는 함수)를 재사용하는 표시 전용 계산이라 원칙에 어긋나지 않는다 |
| FINAL SCORE | `score-label`(기존 `ComputeFinalScore()`) | 그대로 사용 |

- **최종 통계 표시 방식**: `result-title`(승/패)은 지금처럼 크고 극적인 히어로 타이틀로
  유지 — 이 요소까지 캡션화하면 엔딩의 임팩트가 죽는다(§1 원칙 #6, HUD처럼 전면 압축
  금지). 그 아래에 `tactical-panel`(코너컷 4개) 하나로 GLOBAL INFECTED/GLOBAL DEATHS/
  COLLAPSED NATIONS 3개를 `data-row`로 묶는다.
- **data-row 활용**: 위 3개 항목 + (선택) "경과일" 항목까지 4줄로 구성 가능(`result-detail`
  의 "N일 경과"도 흡수해 하나의 리포트 패널로 통합하면 지금처럼 타이틀 아래 흩어진 텍스트
  3개 대신 패널 하나로 정리됨).
- **tactical-panel 활용**: 통계 패널 1개 + 스코어 패널 1개(아래), 화면 루트(`ending-root`)에는
  미적용(§5 원칙).
- **최종 점수 강조 방법**: `score-label`을 통계 패널과 분리해 별도 `tactical-panel`
  (코너컷 4개, `--color-brand-gold` 테두리, `font-size-hero` 유지)로 감싸 "판독창 안의
  최종 판정"처럼 독립 강조 — UpgradeTree의 MAXED 노드(금색+4코너)와 같은 "완결/최종" 신호
  문법을 재사용하는 것이라 시스템 내 의미가 일관된다.
- **결과 리포트 형태 구성(제안 순서)**: ① 승/패 히어로 타이틀(그대로) → ② "OPERATION
  REPORT" 영문 캡션(§8과 동일 표기 문법) → ③ 통계 `tactical-panel`(GLOBAL INFECTED/DEATHS/
  COLLAPSED NATIONS/경과일) → ④ FINAL SCORE 강조 패널(금색) → ⑤ 버튼 2개(기존 그대로).
- **C# 변경 범위**: `EndingScreenController.HandleGameEnded()`에서 `_resultDetail.text = ...`
  한 줄이던 부분을 `data-row` 여러 줄 채우는 헬퍼로 교체 + COLLAPSED NATIONS 집계 LINQ 한
  줄 추가. `ComputeFinalScore()`/`ComputeBiohazardScore()` 등 점수 계산 로직은 전혀 손대지
  않는다.

---

## 11. RankingPanel — 실제 데이터 정정 + "OPERATOR THREAT RECORD" 설계

**Density Mode**: Tactical Readout Mode(§0.0/`DESIGN.md` > UI Density Modes 참고)

**먼저 바로잡을 것**: 이 화면은 "국가별 TOP INFECTED / TOP DEATHS / TOP COLLAPSE" 랭킹이
아니다. `RankingPanelController.cs` 주석에 명시돼 있듯 **앱인토스에는 글로벌 Top N 조회
API가 없어서**, 이 화면은 애초에 "개인 최고 기록(비교하자드 점수) 1개 표시 + 앱인토스 외부
리더보드(WebView) 여는 버튼"으로 축소 구현돼 있다. 국가별 감염/사망/붕괴 집계 데이터
자체가 이 화면에 없다 — "국가현황 패널과 차별화"라는 요청 전제가 사실은 처음부터 성립하지
않는다(둘이 다루는 데이터가 원래 겹치지 않는다). 이름이 "랭킹"이라 국가 순위표를 연상시키는
것이 오해의 원인으로 보인다.

- **국가현황 패널과 차별화**: 애초에 겹치지 않으므로 "차별화 방법"이 아니라 이 화면 고유의
  정체성("개인 기록판")을 명확히 하는 방향으로 접근한다.
- **TOP INFECTED/TOP DEATHS/TOP COLLAPSE 같은 Tactical Display 표현**: 지금 있는 데이터로는
  구현 불가능 — 진짜로 원한다면 로컬 저장에 게임별 상세 기록(감염자 수/사망자 수/붕괴국 수)을
  남기고 "내 최근 플레이 중 최고 감염/최고 사망/최다 붕괴" 같은 **개인 기록 로컬 랭킹**으로
  재해석하는 것은 가능하다 — 단 이건 스타일링이 아니라 `RankingManager.SubmitRun()`을
  확장하는 **기능 추가**라 이번 스코프(기존 화면 재도장) 밖이다. 별도 TODO로 분리 제안.
- **제안(현재 데이터에 맞는 축소 설계)**: "OPERATOR THREAT RECORD" 캡션 + `tactical-panel`
  (코너컷 4개) 안에 `data-row` 1줄("개인 최고 기록" / `pb`값, 금색 강조). 외부 리더보드
  버튼은 기존 그대로 두되 "외부 링크로 이동" 신호로 `stat-chip`(색상 사각 칩)을 라벨 앞에
  붙여 "이 버튼은 게임 밖으로 나간다"는 걸 시각적으로 구분. `note-label`(에디터/비앱인토스
  환경 안내 문구)은 `data-value--locked` 톤(회백색, UpgradeTree LOCKED 색상 재사용)으로
  "지금은 비활성" 느낌을 준다.
- **정렬 기준 표현**: 정렬할 리스트 자체가 없으므로(항목이 1개) 해당 없음 — 향후 로컬 기록
  랭킹 기능이 실제로 추가되면 그때 `data-row` 리스트 + 정렬 기준 캡션(예: "정렬: 최고
  점수순")을 헤더에 추가하는 패턴을 §7 UpgradeTree 범례(상태 4단계 색상 안내)처럼 재사용할
  수 있다.

---

## 12. CountryPopup — 완전 삭제 vs Tactical Modal Base 전환

**Density Mode**: Tactical Readout Mode(§0.0/`DESIGN.md` > UI Density Modes 참고)

**결론부터**: 완전 삭제는 비추천, **Tactical Modal Base로 전환(일반화)** 을 권장한다.

- **완전 삭제 여부**: 비추천. `CountryPopupController.cs`는 이미 `WorldMap.OnCountryClicked`
  구독, `Populate(Country)` 패턴, Show/Hide 라이프사이클이 검증된 채로 존재한다(단지 씬에서
  GameObject가 비활성화돼 죽은 코드가 됐을 뿐 — 클래스 자체는 지금 다시 활성화해도 정상
  동작한다, `HandleCountryClicked`가 여전히 `WorldMap.OnCountryClicked`를 구독하고 있음을
  코드로 확인). 이 검증된 뼈대를 버리고 나중에 모달이 필요할 때 처음부터 새로 만드는 것보다,
  지금 일반화해 재사용하는 편이 공수·리스크 모두 낫다.
- **Tactical Modal Base 전환 가능성**: 높음. `CountryPopupController`를 국가 전용에서
  범용으로 바꾸는 방법 두 가지: (a) `Populate(Country)` 시그니처를 `SetTitle(string)`/
  `AddRow(string, string)`/`ClearRows()`처럼 범용화하고 국가 필드 매핑은 호출부(예: 지도
  클릭 핸들러)로 옮긴다, (b) 아예 신규 `TacticalModalController`(§13)를 만들고
  `CountryPopupController`는 그 위에서 국가 데이터 매핑만 담당하는 얇은 래퍼로 남긴다 —
  **(b)를 권장**(기존 클래스를 억지로 범용화하면서 국가 전용 로직과 뒤섞이는 것보다, 범용
  기반과 국가 전용 소비자를 분리하는 편이 깔끔).
- **공용 모달 시스템으로 승격 가능성**: 높음. `CountryPopup.uxml`(`popup-root` 구조)을
  그대로 물리적 시작점으로 삼아 `tactical-panel` 전환 후, 지도에서 국가를 탭했을 때 "요약은
  Country Dock(상시)·상세는 이 팝업(필요시)"으로 **재활성화**하는 것도 가능 — 반드시 죽은
  채로 둘 필요는 없다(선택 사항, 이번 스코프 필수는 아님).

---

## 13. Tactical Modal System 설계 (이벤트 상세/설정/확인창/정보창 공용)

**Density Mode**: Tactical Readout Mode(§0.0/`DESIGN.md` > UI Density Modes 참고) — 이
공용 모달 위에서 소비되는 모든 화면(이벤트 상세/설정/확인창)이 이 모드를 상속한다.

- **CountryPopup 재활용 가능성**: §12 결론 그대로 — `popup-root`를 물리적 시작점으로 삼는다.
- **tactical-panel 적용**: `popup-root`에 `tactical-panel` 클래스 추가, 배경은 기존
  `--color-bg-panel`(이미 국가현황 패널·랭킹 패널과 톤이 맞음) 유지.
- **corner-cut 적용**: 4개 전부 — 모달은 화면당 1개만 뜨므로 `DESIGN.md` Usage Rules >
  Corner Cut의 "패널 수가 적으면 4개 전부" 기준에 해당한다(MainMenu의 pathogen-card와
  같은 근거).
- **공통 구조 설계(제안)**: `TacticalModal.uxml` 신규 템플릿 —
  ```xml
  <ui:UXML xmlns:ui="UnityEngine.UIElements">
      <ui:VisualElement name="modal-root" class="popup-root tactical-panel">
          <ui:VisualElement class="corner-cut corner-cut--tl" picking-mode="Ignore" />
          <ui:VisualElement class="corner-cut corner-cut--tr" picking-mode="Ignore" />
          <ui:VisualElement class="corner-cut corner-cut--bl" picking-mode="Ignore" />
          <ui:VisualElement class="corner-cut corner-cut--br" picking-mode="Ignore" />
          <ui:VisualElement class="tactical-panel__header">
              <ui:Label name="modal-title" class="tactical-panel__title" />
              <ui:Button name="modal-close" text="✕" class="popup-close" />
          </ui:VisualElement>
          <ui:VisualElement name="modal-rows" class="detail-rows" />
          <ui:VisualElement name="modal-footer" class="modal-footer" />
      </ui:VisualElement>
  </ui:UXML>
  ```
  `modal-rows`는 `data-row`를 Add하는 빈 컨테이너(UpgradeTree `detail-rows`와 동일 계약),
  `modal-footer`는 확인/취소 등 액션 버튼을 담는 슬롯(비어있으면 그냥 안 보임). corner-cut은
  4줄 직접 반복으로 표기했다 — 이 프로젝트에서 UXML 템플릿 인스턴스는 실증된 적이 없다
  (§15 Historical Notes 참고).
- **C# 측(신규, 작게)**: `TacticalModalController` — `Show(string title)`/`Hide()`/
  `AddRow(string label, string value, string valueClass = null)`/`ClearRows()`/
  `VisualElement Footer`(버튼을 붙일 수 있게 컨테이너 노출) 정도의 범용 API만 제공. 이벤트
  상세/설정/확인창은 각각 이 컨트롤러를 참조하는 얇은 wrapper 스크립트(또는 그냥 이
  컨트롤러를 직접 사용)만 작성하면 된다 — UpgradeTree의 `AddDetailRow()` 헬퍼와 완전히
  같은 패턴이라 이미 한 번 검증된 접근이다.
- **UXML 템플릿화 여부**: 미채택 확정(§15 Historical Notes) — 이 컴포넌트도 다른 화면과
  동일하게 직접 반복 삽입 방식을 따른다.

---

## 14. 구현 우선순위 재평가 (남은 5개 대상)

§6의 우선순위 표는 "화면 전체(신규 포함 8개)" 기준이었다. 이번엔 **남은 5개**(MainMenu/
CountrySelect/EndingScreen/RankingPanel/Tactical Modal)만 놓고 체감 효과·구현 난이도·리스크
기준으로 다시 평가한다(UpgradeTree/HUD는 완료됐으므로 제외).

| 순위 | 대상 | 체감 효과 | 구현 난이도 | 리스크 | 근거 |
|---|---|---|---|---|---|
| 1 | CountrySelect (§9) | 높음 | 낮음 | 낮음 | Country Dock과 데이터 3중 중복 해소 — 게임 시작 전 본 데이터 형식을 이후 계속 보게 됨. 리스트 행은 accent bar만 추가, 상세 패널만 구조 전환, controller 이름/로직 무변경 |
| 2 | MainMenu (§8) | 중 | 낮음 | 낮음 | 게임 첫인상이라 효과는 있지만 카드 6개뿐이라 CountrySelect만큼 절대적이지 않음. 작업량 가장 적음 |
| 3 | EndingScreen (§10) | 중상 | 낮음~중 | 낮음 | "OPERATION REPORT" 컨셉 자체의 임팩트는 크지만 COLLAPSED NATIONS 신규 집계 계산이 하나 필요해 MainMenu보다 아주 약간 손이 감 |
| 4 | Tactical Modal System (§12·13) | 간접적(그 자체로 보이는 화면이 아니라 인프라) | 중 | 중 | `TacticalModalController` 신규 설계 + `CountryPopupController` 일반화 리팩터가 필요해 나머지 3개보다 변경 범위가 큼. 다만 이후 이벤트 상세/설정 등의 전제 조건이라 **장기 효과는 가장 크다** — "지금 당장 눈에 띄진 않지만 먼저 깔아두는" 인프라 성격 |
| 5 | RankingPanel (§11) | 낮음 | 낮음(축소판) / 높음(진짜 TOP 랭킹 원할 시) | 낮음(축소판) | 사용 빈도 낮은 화면 + 데이터 자체가 원래 빈약(개인 기록 1개 + 외부 버튼)해서 극적 효과를 내기 어려움. "TOP INFECTED/DEATHS/COLLAPSE" 풀 컨셉은 스타일링이 아니라 로컬 랭킹 기능 신설이라 스코프 아웃 |

**요약**: `CountrySelect > MainMenu > EndingScreen > Tactical Modal > RankingPanel` 순으로
진행할 것을 권장한다. 단 Tactical Modal은 이벤트 상세/설정 화면을 만들 계획이 임박했다면
4순위보다 앞당겨도 무방(선행 인프라 특성상 "언제 필요해지는가"에 따라 순서가 유동적).

---

## 15. Historical Notes (폐기·미채택 제안 기록)

이 문서의 이전 버전에는 디자인 시스템 토큰/컴포넌트 스펙(구 §2/§3/§7/§18)과 UXML 템플릿화
제안(구 §5)이 함께 있었다. `DESIGN.md` 신설에 맞춰 실제로 채택되어 정착한 스펙은 전부
`DESIGN.md`로 이관했고, 그중 **실행되지 않은 채 제안으로만 남아 있던 것**을 여기 정리한다
(정보 손실 방지 목적 — 필요해지면 재검토).

- **코너컷 크기 토큰화** — `--corner-cut-length`/`--corner-cut-thickness`를 `Theme.uss`에
  추가해 화면마다 코너컷 길이를 다르게 쓸 수 있게 하자는 제안(구 §2). **미채택** —
  `Hud.uss`/`UpgradeTree.uss` 모두 `13px`/`2px` 하드코딩 그대로다. 화면이 늘어도 코너컷
  크기를 다르게 쓸 필요가 실제로는 없었다.
- **콘솔 배경 토큰** — `--color-bg-console: rgba(9, 12, 10, 0.97)`을 추가해 전체화면
  "패널형" 화면(MainMenu/UpgradeTree 등) 전용 배경으로 쓰자는 제안(구 §2). **미채택** —
  `--color-bg-panel-alt`(기존 토큰)로 이미 충분히 커버됨.
- **UXML 템플릿화**(`CornerCutSet.uxml`, `TacticalModal.uxml`) — `<ui:Template>` +
  `<ui:Instance>`로 코너컷 4개/모달 골격을 재사용하자는 제안(구 §5, §13). **미채택** —
  이 프로젝트에서 UXML 템플릿 인스턴스 메커니즘이 한 번도 실증되지 않았고, 실제
  `UpgradeTree.uxml` 구현은 corner-cut 4개를 매번 **직접 반복 삽입**했다(§7.8 실제 스니펫
  참고). 향후 화면이 크게 늘어 반복 비용이 부담되면 재검토할 것.
- **Tactical.uss 승격 계획**(구 §18) — **부분 실행됨**. `Assets/UI/Tactical.uss` 파일
  자체는 실제로 생성되어 `tactical-panel`/`tactical-panel__header`/`tactical-panel__title`/
  `corner-cut`(4방향)/`data-row`/`data-label`/`data-value`(+ severity variant:
  `--infected`/`--dead`/`--danger`/`--info`)/`tactical-caption`/`accent-bar-row`/
  `detail-rows`를 담고 있다 — `DESIGN.md` Component Library가 이를 정본으로 기술한다.
  다만 계획에 있던 **"화면별 로컬 중복 정의 제거"는 실행되지 않았다** — 재검증 결과
  `Hud.uss`(155~165행)와 `UpgradeTree.uss`(84~145행) 양쪽에 `tactical-panel`/`corner-cut`/
  `data-row`의 동일 정의가 `Tactical.uss`와 별개로 여전히 로컬로 남아있다(*이 사실은 구
  §18/§20 작성 시점엔 UpgradeTree.uss만 지목됐으나, 이번 재검증에서 Hud.uss도 동일하게
  로컬 중복임을 확인했다*). 기능상 문제는 없으나(같은 값을 두 번 정의할 뿐) 코드 정리는
  이 문서(화면 설계)의 스코프 밖이므로, 필요 시 별도 리팩터 세션(§16 실행 계획과는 다른
  "코드 정리" 세션)으로 분리해 진행한다.
- **Theme.uss 신규 토큰 필요 없음**(구 §18) — "5개 화면 분석에서 신규 토큰이 필요한 케이스는
  없었다"는 진단은 여전히 유효하다. 색상/간격/타이포는 전부 `DESIGN.md`의 기존 토큰 조합으로
  표현한다.

---

## 16. Incremental Rewrite 실행 계획 (실제 파일 대조 완료)

§8~13 설계안을 실제 프로젝트 파일(`MainMenu.uxml/uss/cs`, `CountrySelect.uxml/cs`,
`EndingScreen.uxml/uss/cs`, `CountryPopup.uxml/uss/cs`, `UpgradeTree.uss`, `Hud.uss`,
`Theme.uss`, `Country.cs`, `WorldDataManager.cs` 전문)과 대조해 이번 작업(MainMenu/
CountrySelect/EndingScreen/Tactical Modal, RankingPanel·알림·이벤트상세·설정 제외) 실행에
바로 쓸 수 있는 파일 단위 계획으로 정리한다.

### 16.0 대조 결과 — §8~13과 실제 코드의 차이 (재검증 완료)

4개 화면 모두 §8~13 서술(구조/클래스명/컨트롤러 로직)과 실제 코드가 정확히 일치한다.
다만 토큰/파일 구조 관련 제안 중 실제로는 채택되지 않았거나 부분 실행된 부분이 있어 이번
계획에 반영한다(전체 목록은 §15 Historical Notes 참고):

- **`Assets/UI/Tactical.uss`는 이제 실제로 존재한다** — §16.1의 "생성" 작업은 이미 완료된
  상태이며, 남은 작업은 §16.1에서 다시 서술한다. `UpgradeTree.uss`/`Hud.uss` 양쪽에 로컬
  중복이 남아있는 점도 재검증으로 확인했다(§15).
- **`CornerCutSet.uxml` 템플릿 미사용** — `UpgradeTree.uxml`(29~33행)은 corner-cut 4개를
  **직접 4줄 반복 삽입**했다(§15 Historical Notes 참고). 이번 4개 화면도 검증된 실제
  선례(직접 반복)를 그대로 따른다 — 템플릿화는 하지 않는다.
- **`CountrySelect.uxml`은 전용 `.uss` 파일이 없다** — `MainMenu.uss`를 그대로 참조한다
  (§9 서술과 일치). 즉 `pathogen-card`/`detail-panel` 클래스를 `MainMenu.uss`에서 바꾸면
  CountrySelect에도 **자동으로** 반영된다 — 두 화면을 따로 두 번 고칠 필요가 없는 대신,
  한쪽만 고치려는 시도(예: CountrySelect 전용 `.uss` 신설)는 화면 간 불일치를 만들 수
  있어 지양한다.
- **`CountryPopupController`는 여전히 살아서 구독 중** — `WorldMap.OnCountryClicked`
  (50행)와 `WorldDataManager.OnCountryChanged`(56행) 구독이 실제로 남아있다(씬에서
  GameObject만 비활성화된 상태). §12의 "재활성화하면 바로 동작한다" 판단이 코드로 확인됐다.
- **`Country.GetCollapseStage()`** (`Data/Country.cs` 78행) 존재 확인,
  `CountryCollapseStage` enum 순서가 `Normal(0) < FullCollapse(1) < Disorder(2) <
  NearAnarchy(3) < FullAnarchy(4)`라 §10의 `>= CountryCollapseStage.FullCollapse` 비교가
  의도대로 "붕괴 이상 전부"를 잡아낸다.
- **`CountryStatusPanel.uss`에는 tactical 관련 로컬 정의가 없음** — 로컬 중복 정리 대상은
  `Hud.uss`/`UpgradeTree.uss` 두 곳뿐이다.
  [Step 80 갱신] 이 진단 당시 `CountryStatusPanel`은 애초에 전환 대상 화면 목록(§0.1/§16
  머리말)에 없었으나, "GLOBAL STATUS CENTER" 세계 감염 현황 센터로 재설계하며 tactical-panel/
  코너컷/data-row/severity 4색 체계로 전환 완료됨 — 이제 이 화면도 로컬 tactical 정의가
  존재한다(자세한 내용은 `Docs/DevLog.md` Step 80 참고).

### 16.1 공통 인프라 (0순위 — 4개 화면 작업의 전제)

1. ~~`Assets/UI/Tactical.uss` 신규 생성~~ **완료됨(재검증 확인)** — `.tactical-panel`,
   `.tactical-panel__header`, `.tactical-panel__title`, `.corner-cut`(+`--tl/--tr/--bl/
   --br`), `.data-row`, `.data-label`, `.data-value`(+ severity variant
   `--infected/--dead/--danger/--info`), `.tactical-caption`, `.accent-bar-row`,
   `.detail-rows`가 이미 파일에 있다. 노드 상태 variant(`--locked/--available/--active/
   --maxed`)는 계획대로 `UpgradeTree.uss`에 잔류 중(노드 상태는 UpgradeTree 전용 의미라
   Tactical.uss로 옮기지 않는 것이 맞다).
2. **남은 작업**: `Hud.uss`(155~165행)와 `UpgradeTree.uss`(84~145행)에 남아있는
   `tactical-panel`/`corner-cut`/`data-row`(non-variant 베이스분)의 **로컬 중복 정의를
   제거**하고 `Tactical.uss` 참조로 교체 — 노드 45개/코너컷/상태색이 그대로 보이는지 회귀
   확인(`Docs/QA_Checklist.md`에 항목 추가 권장). 이 작업은 화면 신규 적용과 무관하게
   독립적으로 진행 가능하다.
3. 코너컷 삽입은 §16.0에서 확정한 대로 **각 화면 UXML에 4줄 직접 반복**(템플릿 미사용).

### 16.2 화면별 실행 계획 — 우선순위(§14): CountrySelect → MainMenu → EndingScreen → Tactical Modal

**① CountrySelect** (§9)
- UXML(`CountrySelect.uxml`): `detail-panel` 클래스에 `tactical-panel` 병기 + corner-cut
  4줄 + `tactical-panel__header`(헤더 래퍼) 추가, `detail-desc` Label 자리를 `detail-rows`
  컨테이너(`VisualElement`)로 교체. `<Style src="Tactical.uss">` 한 줄 추가.
- USS(`MainMenu.uss`, 공유): `.country-row`에 `border-left-width: 4px` 추가(코너컷 대신
  accent bar, 48행 리스트라 §5/§15 원칙), `.country-row--dev-high/--dev-mid/--dev-low`
  3종 신설(각각 `--color-status-info`/`--color-text-secondary`/`--color-status-danger`
  테두리색).
- C#(`CountrySelectController.cs`): `CreateCountryRow()`에 `country.developmentLevel` →
  위 3클래스 매핑 한 줄 추가. `SelectCountry()`의 `_detailDesc.text = $"인구:...\n기후:...\n
  의료 수준:..."`(186~188행) 3줄 문자열 대입을 `detail-rows`에 `data-row` 3개(인구/기후/
  의료수준)를 Add하는 헬퍼로 교체 — `AddDetailRow(string label, string value)` 형태로
  UpgradeTree `SelectNode()` 패턴 재사용.
- 리스크: 낮음. 이벤트 시그니처(`OnCountryConfirmed`/`OnBackRequested`) 무변경.

**② MainMenu** (§8)
- UXML(`MainMenu.uxml`): `detail-panel`은 ①에서 `MainMenu.uss`에 추가한 tactical 스타일을
  공유하므로 자동 반영 — corner-cut 4줄만 추가. `pathogen-card`는 코드에서 생성되므로
  UXML 변경 없음(카드 자체는 `MainMenuController.CreatePathogenCard()`가 만듦). `<Style
  src="Tactical.uss">` 한 줄 추가. `mainmenu-title` 위에 영문 캡션 Label 추가는 선택 사항.
- USS(`MainMenu.uss`): `.pathogen-card`에 `tactical-panel` 병기 규칙 추가(테두리만, 배경은
  `--color-surface` 유지).
- C#(`MainMenuController.cs`): `CreatePathogenCard()`(78~95행)에서 corner-cut 4개
  VisualElement Add + `stats` 단일 Label(87~90행)을 `data-row` 4줄(전염력/중증도/치사율/
  내성)로 분해하는 헬퍼로 교체. `pathogen-card--selected`(금색)는 그대로 유지.
- 리스크: 낮음. 카드 6개뿐이라 반복 규모 작음.

**③ EndingScreen** (§10)
- UXML(`EndingScreen.uxml`): `result-title` 유지, `result-detail`+`score-label` 자리에
  통계 `tactical-panel`(코너컷 4개, `data-row` 4줄: GLOBAL INFECTED/DEATHS/COLLAPSED
  NATIONS/경과일) + 스코어 전용 `tactical-panel`(코너컷 4개, `--color-brand-gold` 테두리)
  2개 패널 추가. `<Style src="Tactical.uss">` 한 줄 추가.
- USS(`EndingScreen.uss`): 두 패널의 화면 고유 배경/여백만 추가(테두리/코너컷은
  Tactical.uss가 커버).
- C#(`EndingScreenController.cs`): `HandleGameEnded()`(70~88행)에 COLLAPSED NATIONS 집계
  1줄 추가 — `WorldDataManager.Instance.Countries.Count(c => c.GetCollapseStage() >=
  CountryCollapseStage.FullCollapse)`. `_resultDetail.text`/`_scoreLabel.text` 단순 대입을
  `data-row` Add 헬퍼로 교체. `ComputeFinalScore()`/`ComputeBiohazardScore()` 등 점수 계산
  로직은 전혀 손대지 않음.
- 리스크: 낮음~중(신규 LINQ 집계 1줄 추가가 유일한 로직 변경).

**④ Tactical Modal** (§12·13)
- UXML(`CountryPopup.uxml`): `popup-root`에 `tactical-panel` 병기 + corner-cut 4줄,
  `popup-title` → `tactical-panel__title`, `population-label`~`status-label` 6개 Label을
  `modal-rows` 컨테이너(`data-row` Add 방식)로 교체.
- C#(신규 `TacticalModalController.cs`): `Show(string title)`/`Hide()`/`AddRow(string label,
  string value, string valueClass = null)`/`ClearRows()`/`Footer` 프로퍼티 제공.
  `CountryPopupController.cs`는 `WorldMap.OnCountryClicked`/`WorldDataManager.OnCountryChanged`
  구독(46~67행)은 그대로 두고, `Populate()`(87~98행) 내부만 `TacticalModalController`의
  `AddRow()` 호출로 교체하는 얇은 wrapper로 축소.
- 리스크: 중(신규 클래스 설계 필요, 다만 대상이 죽은 코드라 실사용 화면 회귀 위험은 없음).

### 16.3 세션 분리 (프로젝트 컨벤션 — 화면별 별도 세션 순차 진행)

| 세션 | 범위 |
|---|---|
| A | §16.1 공통 인프라(로컬 중복 제거) + ① CountrySelect |
| B | ② MainMenu |
| C | ③ EndingScreen |
| D | ④ Tactical Modal |

각 세션 종료 시 CLAUDE.md "현재 구현된 시스템"에 한 줄 추가 + `Docs/DevLog.md`에 Step
번호로 근거 기록(프로젝트 규칙 — CLAUDE.md는 현황, DevLog는 이력).

### 16.4 수정 대상 파일 목록 (전체)

| 파일 | 변경 유형 |
|---|---|
| `Assets/UI/Hud.uss` | 로컬 `tactical-panel`/`corner-cut`/`data-row` 중복 정의 제거, `Tactical.uss` 참조로 교체 |
| `Assets/UI/UpgradeTree.uss` | 로컬 공통 tactical 정의 제거(상태 variant만 잔류) |
| `Assets/UI/UpgradeTree.uxml` | (이미 `Tactical.uss` 참조 확인 필요 — 16.0 재검증 시 확인) |
| `Assets/UI/MainMenu.uss` | `pathogen-card`/`detail-panel`/`country-row` 클래스 보강 |
| `Assets/UI/MainMenu.uxml` | `<Style src="Tactical.uss">`, corner-cut 4줄, 캡션 Label(선택) |
| `Assets/UI/CountrySelect.uxml` | `<Style src="Tactical.uss">`, corner-cut 4줄, `detail-rows` 컨테이너 |
| `Assets/Scripts/UI/MainMenuController.cs` | `CreatePathogenCard()` data-row 분해 |
| `Assets/Scripts/UI/CountrySelectController.cs` | `CreateCountryRow()` accent bar 매핑, `SelectCountry()` data-row 헬퍼 |
| `Assets/UI/EndingScreen.uxml` | `<Style src="Tactical.uss">`, 통계·스코어 tactical-panel 2개 |
| `Assets/UI/EndingScreen.uss` | 화면 고유 패널 배경/여백 보강 |
| `Assets/Scripts/UI/EndingScreenController.cs` | COLLAPSED NATIONS 집계, data-row 헬퍼 |
| `Assets/UI/CountryPopup.uxml` | `<Style src="Tactical.uss">`, tactical-panel 구조 전환 |
| `Assets/UI/CountryPopup.uss` | 화면 고유분만 정리 |
| `Assets/Scripts/UI/CountryPopupController.cs` | 얇은 wrapper로 축소 |
| `Assets/Scripts/UI/TacticalModalController.cs` | 신규 — 범용 모달 API |

각 파일 변경은 §16.2의 화면 단위로 독립 실행 가능(①~④ 서로 blocking 없음, 단 전부 §16.1
공통 인프라 완료를 전제로 함).
