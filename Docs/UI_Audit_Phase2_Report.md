# UI Audit Phase 2 — 분석 보고서

> 범위: MainMenu / CountrySelect / UpgradeTree / CountryStatusPanel 4개 화면.
> 대상 파일: `MainMenu.uxml/uss`, `CountrySelect.uxml/uss`, `UpgradeTree.uxml/uss`,
> `CountryStatusPanel.uxml/uss`, `Theme.uss`, `Tactical.uss`, `DESIGN.md`, `UI_Design.md`,
> 관련 컨트롤러(`MainMenuController.cs`/`CountrySelectController.cs`/`UpgradeTreeView.cs`).
> **코드/USS/문서 수정 없음 — 분석만 수행.**

이 문서의 결론을 한 문장으로 요약하면: **4개 화면 모두 "색이 부족해서"가 아니라, 이미 완료된
Tactical 리디자인 작업이 화면별 구조·CSS 캐스케이드·정보 밀도 규칙과 부분적으로 충돌하고
있어서 만족스럽지 않다.** 특히 UpgradeTree는 실제로 깨진 버튼(Unity 기본 회색)과 CSS 우선순위
버그를 갖고 있고, CountryStatusPanel은 정보 위계 부재, MainMenu/CountrySelect는 Density Mode
경계가 문서상으로만 존재하고 실제 스타일(Surface Hierarchy)에는 반영되지 않은 문제다.

---

## 1. MainMenu (병원체 선택)

### 문제점
- 병원체 카드(`pathogen-card`) 6장이 배경(`--color-bg-root`, 거의 완전한 검정)과 거의
  구분되지 않는다 — 기본 테두리가 `tactical-panel`의 `accent-glow-soft`(민트 35% 알파)
  1px, 배경은 `--color-surface`(흰색 8% 알파)뿐이라 선택 전 카드 6개가 "옅은 회색 덩어리"로
  뭉쳐 보인다.
- 카드 내부 스탯 4종(전염력/중증도/치사율/내성)이 `data-row`(`MainMenuController.cs:96-103`,
  `MakeStatRow`)로 렌더링되는데, 라벨(`data-label`, tertiary 회색)과 값(`data-value`,
  secondary 회색)이 사실상 같은 톤의 회색이고 값 자체도 `■■■□□` 텍스트 글리프 하나뿐이라
  6장의 카드를 눈으로 비교하기 어렵다.
- 선택 상태(`pathogen-card--selected`)는 3px 골드 테두리 + 골드 틴트 배경으로 뚜렷하지만,
  "선택 전" 상태가 너무 흐려서 상대적으로 선택 상태만 튀어 보이고 화면 전체의 위계가
  "선택된 카드 1개 vs 안 보이는 카드 5개"로 이분화된다.

### 근본 원인
색상 팔레트 자체(민트/골드/회색)는 문제가 아니다. 근본 원인은 **정보 밀도 규칙 위반**이다.
`DESIGN.md` UI Density Modes는 MainMenu를 Layer B(Briefing Terminal Mode)로 지정하고,
`data-row`는 "상세 패널 한정"이라고 명시한다(`DESIGN.md:165-172`, Layer B 허용 컴포넌트:
"`data-row`(상세 패널 한정)"). 그런데 실제 구현은 리스트에 노출되는 **카드 6장 각각**에
`data-row`를 4줄씩(`MainMenuController.cs:99-102`) 박아 넣었다 — Layer A(Tactical Readout)
전용 컴포넌트를 Layer B 리스트 화면에 그대로 이식한 것이다. 그 결과 "여유 있게 보여주고
결정을 유도"해야 할 브리핑 화면이 HUD/UpgradeTree와 동일한 판독 문법(작은 xs 폰트, 촘촘한
data-row)으로 압축되어 "브리핑룸"이 아니라 "축소된 콘솔"처럼 보인다.

### 색상 문제인가
아니다. 토큰 자체(surface 8%, accent-glow-soft 35%)는 `DESIGN.md` Surface Hierarchy
Level 3(Card/row) 정의를 정확히 따르고 있다. 문제는 **Layer B 화면에도 Layer A와 동일한
Surface Hierarchy 값을 그대로 썼다**는 점이다 — `DESIGN.md`의 Density Mode 정의(§UI Density
Modes)는 폰트 스케일과 컴포넌트 허용 목록만 다루고, "Layer B는 카드 배경/테두리 alpha를
더 진하게 써도 된다" 같은 **Surface Hierarchy 차등 규정이 아예 없다** — 이것이 스펙 갭이다.

### 구조 문제인가
그렇다. Layer A 컴포넌트(`data-row`)가 Layer B 리스트에 오용된 것과, "선택 전 카드가 거의
안 보임"이 겹쳐 화면 전체가 "무엇을 먼저 봐야 하는지" 불분명한 상태다.

### Density Mode 적합성
**부적합.** `DESIGN.md`가 스스로 금지한 조합("`data-row`는 상세 패널 한정")을 화면 구현이
어기고 있다. 문서와 코드가 불일치.

**진짜 문제**: MainMenu → **색상 문제가 아니라 Density Mode 컴포넌트 오용으로 인한 선택 경험
문제** — 6장의 카드가 서로 구분되지 않아 "고르는" 재미가 없다.

---

## 2. CountrySelect (국가 선택)

### 문제점
- `MainMenu.uss`를 공유하므로 MainMenu와 동일한 저대비 카드 문제를 그대로 물려받는다(48개
  행이라 체감은 더 큼).
- `country-row` 좌측 4px accent bar가 국가의 `developmentLevel`(선진국/개발도상국/저개발국)을
  `--color-status-info`(파랑)/`--color-text-secondary`(회색)/`--color-status-danger`(빨강)로
  표시한다(`CountrySelectController.cs:210-217`, `CountrySelect.uss:13-22`). 이 3색 중 파랑과
  빨강은 `DESIGN.md`의 **Severity 4색**(감염/사망/위험/정보, `DESIGN.md:248-259`)에서 그대로
  가져온 것이다.
- 국가 선택 후 상세 패널은 `data-row` 3줄(인구/기후/의료 수준)로 잘 구성되어 있다(Layer B의
  "상세 패널 한정" 규칙을 정확히 지킨 유일한 지점).
- `country-row--selected`가 `border-width: 3px`(전체 4방향, `MainMenu.uss:82-86`)를 적용하는데,
  이는 앞서 개별 지정된 `border-left-width: 4px`(개발수준 accent bar)를 **선택 순간 3px
  골드로 덮어버린다** — CSS 캐스케이드상 후행 규칙이 shorthand로 4방향을 재선언하면 이전의
  `border-left-width` 단독 선언보다 우선한다.

### 근본 원인
1) MainMenu와 동일한 Density Mode/Surface Hierarchy 문제(위 1절 참고, 48행이라 더 두드러짐).
2) **Severity 색상 축의 재사용 오남용**: `DESIGN.md`의 Color-axis Separation 원칙
   (`DESIGN.md:91-93`, "브랜드 악센트/severity/카테고리/구조용 발광색은 서로 다른 4개의
   축이다. 한 축의 색을 다른 축의 의미로 재사용하지 않는다")과 Severity Colors Do/Don't
   (`DESIGN.md:468-479`)는 이 4색을 "국가가 처한 상황"(감염/사망/위험/정보)에만 쓰라고
   못박는다. 그런데 CountrySelect는 **아직 병원체가 퍼지기도 전**의 "개발 수준"이라는
   전혀 다른 의미에 같은 색(특히 danger=빨강)을 붙였다. 이후 게임 플레이 중
   `CountryStatusPanel`에서 같은 빨강이 "COLLAPSE(붕괴)"를 의미하는 것을 플레이어가 보게
   되면, 두 화면의 빨강이 서로 다른 것을 가리키는데도 "이 나라는 원래 위험한 나라였구나"라는
   잘못된 인상을 남길 수 있다 — 이는 문서가 스스로 경계한 정확히 그 실수다.
3) 선택 시 accent-bar(개발수준 정보)가 사라지는 것은 UpgradeTree에서도 동일하게 발견되는
   패턴(3절 참고)으로, **"카테고리색은 좌측 바, 상태색은 테두리 전체"라는 설계 원칙과
   실제 CSS 우선순위가 어긋나는 시스템 차원의 버그**다.

### 색상 문제인가
부분적으로 그렇다 — 다만 "색이 부족하다"가 아니라 **"색의 의미 축이 잘못 배정됐다"**는
쪽이다. 색을 더 쓰거나 바꾸는 문제가 아니라 개발수준에는 severity 축이 아닌 별도 팔레트를
써야 한다.

### 구조 문제인가
그렇다. Density Mode 오용(1절과 동일) + accent-bar/selected 테두리 CSS 우선순위 충돌.

### Density Mode 적합성
부적합(MainMenu와 동일 사유). 다만 상세 패널의 `data-row` 3줄 구성만큼은 Layer B 규칙을
정확히 지킨 모범 사례로, 이 패턴을 리스트가 아니라 상세 패널에만 쓰는 원칙을 다른 화면에도
참고할 수 있다.

**진짜 문제**: CountrySelect → **색상 문제가 아니라 Severity 색상 축 오배정 + 선택 시
개발수준 정보 소실 문제** — 48개국이 다 비슷해 보이는 이유는 "색이 옅어서"가 아니라
"색의 의미가 이 화면에서 임시로 발명된 것"이기 때문이다.

---

## 3. UpgradeTree (업그레이드 창)

### 문제점 — 실측 가능한 버그
- `.detail-panel__buy`(연구 시작 버튼, `UpgradeTree.uss:333-336`)에는
  `background-color`/`border-*`/`border-radius` 선언이 **전혀 없다**:
  ```
  .detail-panel__buy {
      height: var(--touch-target-min);
      margin-top: var(--space-sm);
  }
  ```
  `DESIGN.md` Button System은 이 정확한 패턴을 "과거 버그"로 명시한다(`DESIGN.md:513-517`,
  "Unity 기본 런타임 버튼(둥근 회색)이 방치되면... EndingScreen.uss .ending-button이 이
  버그였다가 수정된 전례") — `EndingScreen`/`MainMenu.mainmenu-back`/
  `CountryStatusPanel.status-close-btn`/`CountryPopup.popup-close`는 전부 이 수정을 받았지만
  **`UpgradeTree`의 "연구 시작" 버튼만 여전히 미수정 상태**다. `UpgradeTreeView.cs:323`에서
  이 버튼을 매번 쿼리해 `SetEnabled`/`text`를 갱신하므로 죽은 코드가 아니라 **실제로 화면에
  노출되는 라이브 버튼**이다. 연구 항목을 선택할 때마다 Unity 기본 회색 pill 버튼이 나타날
  가능성이 매우 높다.
- 연구 항목 행(`research-row`)의 카테고리색(좌측 4px accent bar, `UpgradeTree.uss:260-262`)과
  상태색(active/maxed, `UpgradeTree.uss:306-318`)이 **CSS 캐스케이드에서 충돌**한다.
  `.research-row--active`/`.research-row--maxed`는 `border-color`/`border-width`를 **4방향
  shorthand로 재선언**하는데, 이 규칙이 스타일시트에서 카테고리색 규칙보다 **뒤에** 온다
  (260행 vs 306행). CSS는 동일 특이도에서 후행 규칙이 이기므로, 연구 중(active)이거나
  완료(maxed)된 노드는 카테고리색 accent bar가 **테두리색으로 통째로 덮여 사라진다**.
  `.research-row--selected`(320행, 3px 골드)도 동일한 방식으로 한 번 더 덮는다.
  `DESIGN.md` Node State Colors 절(`DESIGN.md:481-494`)은 "카테고리 정체성은 좌측
  accent-bar, 상태는 테두리 전체+배경으로 위치를 분리해 **두 축이 겹치지 않게 한다**"고
  명시적으로 약속하지만, 실제 CSS는 이 약속을 어기고 있다 — **플레이어가 가장 자주 보게
  되는(연구 중/완료) 노드일수록 카테고리 정보를 잃는 역설**이 발생한다.
- 브랜치 보드(`branch-board`, 카테고리당 브랜치 4개 진행률판)는 `tactical-panel` +
  코너컷 4개로 강조되어 있는 반면, 실제 상호작용 대상인 연구 목록(`research-row` 리스트)은
  프레임 없이 개별 카드만 나열된다 — "요약판(브랜치 보드)"이 "실제 작업 목록(연구 행)"보다
  시각적으로 더 무겁게 강조되는 위계 역전이 있다.

### 근본 원인
1) 버튼 미스타일링은 **문서화된 패턴을 이 화면 하나만 놓친 단순 누락**이다 — 색상 팔레트
   문제가 아니라 선언 자체의 부재.
2) 카테고리/상태 색상 충돌은 **CSS 우선순위(source order) 설계 결함**이다 — 두 축을 분리한다는
   설계 의도(DESIGN.md)는 맞지만, 구현은 "state 클래스가 category 클래스보다 뒤에 오면 state가
   category를 통째로 덮는다"는 CSS 기초 규칙을 고려하지 않았다. 색을 다시 고른다고 해결되는
   문제가 아니라, `border-left-color`만 별도로 재선언하거나 `border-color`를
   `border-top/right/bottom-color`로 분리하는 구조 수정이 필요하다.
3) 브랜치 보드 vs 연구 목록의 위계 역전은 "코너컷은 화면당 개수가 적은 패널에만"이라는
   Corner Cut 규칙(`DESIGN.md:452-466`)을 브랜치 보드에는 지키고 연구 목록에는 의도적으로
   생략(48행 규칙과 동일 이유)했기 때문인데, 그 결과 "요약"이 "본문"보다 강조되는 부작용을
   문서가 예견하지 못했다.

### 색상 문제인가
아니다. 세 문제 모두 색상 팔레트가 아니라 **선언 누락**과 **CSS 우선순위**의 문제다.

### 구조 문제인가
그렇다 — 이번 4개 화면 중 가장 명확하게 구조/구현 버그로 귀결되는 화면이다.

### Density Mode 적합성
**적합.** 상단 탭(3개, 고정 노출로 진행 인디케이터 겸용) + 브랜치 보드(요약) + 연구 목록
(data-row 없이 research-row 자체 판독행 구조 사용) + 상세 패널(`data-row`) 구성은 Layer A
(Tactical Readout Mode)의 밀도·컴포넌트 규칙과 부합한다. 밀도 자체는 문제가 아니다.

**진짜 문제**: UpgradeTree → **색상 문제가 아니라 (1) 미스타일링된 버튼과 (2) 카테고리/상태
색상의 CSS 우선순위 충돌로 인한 정보 유실 문제** — 연구를 진행할수록 오히려 어떤 계열
(전파/증상/적응) 노드인지 알아보기 어려워진다.

---

## 4. CountryStatusPanel (국가현황 — GLOBAL STATUS CENTER)

### 문제점
- 단일 `status-scroll` 안에 다음 8개 섹션이 순서대로 쌓여 있다: ①GLOBAL STATUS 배너
  ②세계 요약(population-bar + data-row 6줄) ③감염 국가 현황(data-row 3줄) ④국가 상태 분포
  (4버킷) ⑤의료 시스템 현황(4버킷) ⑥종합 위협도 TOP 10(data-row 10줄) ⑦감염자 TOP 10
  (data-row 10줄) ⑧48개국 목록(대륙별 아코디언, 최대 48행). 스크롤 하나에 최소 40개 이상의
  판독 행이 이어진다.
- 8개 섹션 전부가 동일한 `.modal-section-caption`(11px, accent-glow, 볼드)으로 구분되어
  있어 "이 화면에서 가장 먼저/가장 중요하게 봐야 할 지표가 무엇인지"가 시각적으로 드러나지
  않는다. `global-status-banner`(코드 주석상 "세계 상황 한 줄 평가" — 사실상 이 화면의
  헤드라인)조차 `font-size-md`(18px)로, 바로 아래 "국가 상태 분포"의 카운트 숫자
  (`distribution-item__count`, 역시 18px)와 동일한 무게로 렌더링된다.
- 실사용 빈도가 가장 높을 것으로 보이는 "48개국 목록"(개별 국가 드릴다운의 진입점,
  `CLAUDE.md` 최근 작업 이력 참고)이 스크롤 맨 아래, 즉 7개 섹션을 다 지나야 도달하는
  위치에 있다.
- 코너컷은 화면 최외곽 `status-root` 1곳에만 적용되고(적절 — Corner Cut Don't 규칙 준수),
  48행 리스트에는 코너컷 대신 좌측 accent-bar만 사용(적절). 즉 **코너컷/accent-bar 규칙
  자체는 정확히 지켜지고 있다** — 이 화면의 문제는 코너컷 오남용이 아니다.

### 근본 원인
색상 축(Severity 4색)은 `DESIGN.md` 규칙을 정확히 따르고 있다 — SAFE=info(파랑),
WARNING=infected(주황), DANGER=danger(빨강), COLLAPSE=dead(진한 빨강)로 국가 데이터 전용
의미에만 쓰였고, 노드 상태나 UI 크롬에 새어나가지 않았다(CountrySelect의 개발수준 오남용과
달리 이 화면은 색상 축 사용이 올바르다). 문제는 **정보 계층(Hierarchy) 설계 자체가 없다**는
점이다 — `DESIGN.md` Design Principle #1(판독 우선, "이 정보가 몇 개의 data-row/stat-chip으로
표현되는가"를 먼저 정하고 장식을 얹는다)은 각 섹션 *내부*의 표현 방식은 규정하지만, **여러
섹션 *사이*의 우선순위**는 규정하지 않는다. 그 결과 컨트롤러가 기능을 하나씩 추가할 때마다
(세계 요약 → 감염 국가 현황 → 국가 상태 분포 → 의료 시스템 현황 → 랭킹 2종 → 48개국 목록,
`CLAUDE.md` 구현 이력 순서와 일치) 동일한 `modal-section-caption` 틀을 반복 사용해왔고,
화면은 "기능이 늘어난 순서대로 쌓인 목록"이 되었다 — 사용자가 실제로 원하는 "지금 세계가
어떤 상태인지 한눈에, 그다음 필요하면 국가별로 파고든다"는 시선 흐름과 어긋난다.

### 색상 문제인가
아니다. Severity 4색 적용은 이 화면에서 가장 모범적으로 지켜지고 있다(비교: CountrySelect는
같은 축을 오남용, 이 화면은 정확히 사용).

### 구조 문제인가
그렇다 — 정보 위계 부재 + 실사용 빈도 대비 잘못된 스크롤 위치가 핵심이다.

### Density Mode 적합성
**형식적으로는 적합, 실질적으로는 과밀.** Layer A(Tactical Readout Mode)의 "정보 밀도: 높음
— 화면 하나에 수십 개의 판독 행이 반복될 수 있음"(`DESIGN.md:149-150`)이라는 조항이 문자
그대로는 8개 섹션+40행 이상을 정당화하지만, 같은 문서의 Design Principle #1("무엇을 먼저
봐야 하는지")과는 긴장 관계에 있다 — 밀도가 높아도 되는 것과 위계가 없어도 되는 것은 다른
문제인데, 현재 화면은 전자만 지키고 후자를 놓쳤다.

**진짜 문제**: CountryStatusPanel → **색상 문제가 아니라 8개 섹션이 동일한 무게로 나열된
정보 위계 문제** — 가장 중요한 한 줄 진단(GLOBAL STATUS)과 가장 자주 쓰는 기능(48개국 목록)
모두 화면 안에서 부각되지 못하고 있다.

---

## 5. 화면 간 공통 패턴

4개 화면을 가로질러 반복되는 시스템 차원의 이슈가 2가지 발견됐다 — 개별 화면 문제가 아니라
디자인 시스템/구현 규칙의 구조적 공백이다.

1. **"선택 시 border-width 전체 재선언" 관용구가 좌측 accent-bar를 항상 지운다.**
   `pathogen-card--selected`(MainMenu, 영향 없음 — 카테고리 축이 없어서), `country-row
   --selected`(CountrySelect, 개발수준 accent 소실), `research-row--active/--maxed/
   --selected`(UpgradeTree, 카테고리 accent 소실) 세 곳 모두 동일한 원인이다. `Stroke
   System`(`DESIGN.md:333-365`)은 두께의 "의미"는 정의했지만, **같은 요소에 좌측 바(카테고리)와
   전체 테두리(상태/선택)가 동시에 존재할 때 CSS 우선순위를 어떻게 지킬지**는 규정하지
   않았다. `border-color`/`border-width` shorthand 대신 `border-left-color`를 항상 별도
   규칙으로 마지막에 재선언하는 공용 규칙을 `Tactical.uss`나 `DESIGN.md` Stroke System에
   추가하는 방향이 근본 해결책이다(이번 조사 범위에서는 제안만 하며 수정하지 않았다).
2. **Density Mode는 "폰트 스케일/컴포넌트 허용 목록"만 정의하고 "Surface Hierarchy 강도"는
   Layer 불문 동일하다.** MainMenu/CountrySelect(Layer B)가 HUD/UpgradeTree(Layer A)와
   시각적으로 다르게 "느껴져야" 함에도 카드 배경 alpha·테두리 alpha는 완전히 동일한 토큰을
   쓴다. 이 때문에 "브리핑룸"이라는 컨셉이 텍스트(캡션 문구)로만 존재하고 시각적으로는
   구현되지 않는다.

---

## 6. 우선 수정 순위

| 순위 | 화면 | 이슈 | 근거 | 유형 | 예상 난이도 |
|---|---|---|---|---|---|
| 1 | UpgradeTree | `.detail-panel__buy` 배경/테두리/radius 미지정 — Unity 기본 회색 버튼 노출 가능성 | `UpgradeTree.uss:333-336` | 구현 누락(버그) | 낮음 — 선언 3줄 추가 |
| 2 | UpgradeTree | 카테고리색(좌측 accent-bar) vs 상태색(active/maxed/selected)의 CSS 우선순위 충돌로 카테고리 정보 소실 | `UpgradeTree.uss:260-262` vs `296-323` | 구조(CSS 우선순위) | 중 — `border-left-color` 재선언 규칙 정리 |
| 3 | CountryStatusPanel | 8개 섹션 동일 가중치 — 핵심 지표(GLOBAL STATUS)/최다 사용 기능(48개국 목록) 미부각 | `CountryStatusPanel.uxml:12-98` 전체 구조 | 구조(정보 위계) | 높음 — 섹션 재배치/헤드라인 강조 설계 필요 |
| 4 | MainMenu / CountrySelect | Layer A 전용 `data-row`가 Layer B 리스트 카드에 반복 사용, Surface Hierarchy가 Layer 불문 동일해 브리핑 화면이 콘솔처럼 보임 | `MainMenuController.cs:96-103`, `DESIGN.md:165-172` | 구조(Density Mode 위반) + 스펙 갭 | 중 — 카드 표현 방식 재설계 |
| 5 | CountrySelect | 개발수준(developmentLevel) accent-bar가 Severity 4색 축을 재사용 — 게임 시작 전 단계에 "위험" 신호 오인 소지 | `CountrySelectController.cs:210-217` | 색상(의미 축 오배정) | 낮음 — 별도 3색 팔레트 신설 |
| 6 (공통) | 전 화면 | "선택 시 border-width 전체 재선언" 관용구가 좌측 accent-bar를 지우는 시스템 패턴 | §5-1 참고 | 구조(디자인 시스템 공백) | 중 — `Tactical.uss` 공용 규칙화 |

---

## 7. 최종 결론 — 화면별 한 문장 요약

- **MainMenu** → 색상 문제가 아니라 **Density Mode 컴포넌트 오용(리스트에 상세 패널용
  data-row를 이식)으로 인한 선택 경험 문제**.
- **CountrySelect** → 색상 문제가 아니라 **Severity 색상 축을 개발수준에 오배정한 의미
  충돌 + 선택 시 accent-bar 소실 문제**.
- **UpgradeTree** → 색상 문제가 아니라 **미스타일링된 CTA 버튼과 카테고리/상태 색상의 CSS
  우선순위 충돌로 인한 정보 유실 문제**.
- **CountryStatusPanel** → 색상 문제가 아니라 **8개 섹션이 동일 가중치로 나열된 정보 계층
  부재 문제**.

공통적으로, 이번 4개 화면은 HUD 사례와 마찬가지로 "색을 더 쓰거나 바꾸는 것"으로는 해결되지
않는다. 다음 단계로 진행한다면 위 §6 순위표의 1~2번(UpgradeTree 버튼/CSS 우선순위)이 가장
적은 리스크로 가장 명확한 개선을 만들어낼 수 있는 지점이다.
