# Research Database v2 — 브랜치 현황판 + 연구 콘솔 + Research Popup 상세 구조 설계

`Docs/ResearchDatabase_UI_FinalReview.md`가 6개 조합 비교 끝에 확정한 **1순위: E(브랜치+연구
콘솔) + 팝업**을 사용자가 최종 채택했다. 이 문서는 그 결정을 재검토하지 않고, 실제 구현에
들어가기 전 필요한 한 단계 더 깊은 설계 — 브랜치 현황판의 구체적 배치, 화면 전체 와이어프레임,
Research Popup 내부 구조, 기존 코드와 충돌 없는 구현 방안, `DESIGN.md` 반영안 — 을 정리한다.

**순수 UX/UI 구조 설계 문서. 코드/UXML/USS 작성 없음.** 근거로 실제 코드를 확인했다:
`Assets/Scripts/UI/UpgradeTreeView.cs`, `Assets/UI/UpgradeTree.uxml`/`.uss`,
`Assets/Scripts/UI/TacticalModalController.cs`, `Assets/UI/CountryPopup.uxml`,
`Assets/UI/CountryStatusPanel.uss`, `Assets/UI/Hud.uss`, `Assets/Scripts/Data/
DefaultUpgradeTreeFactory.cs`, `Assets/Scripts/Data/UpgradeNode.cs`, `Docs/DESIGN.md`,
`Docs/UpgradeTree_ResearchDatabase_NodeMapping.md`.

---

## 0. 전제 확인 — 데이터 구조가 이미 "브랜치 4개 고정"을 보장한다

`DefaultUpgradeTreeFactory.cs`/`NodeMapping.md`를 직접 확인한 결과, 3개 카테고리(전파/증상/적응)
전부 예외 없이 **"기반 갈래 3개 + 통합 갈래 1개 = 4브랜치"** 구조다. 우연이 아니라
`UpgradeTreeView.BuildDummyBranches()`가 이미 이 4묶음 이름을 카테고리별로 정확히 쓰고 있다
(전파: 공기 계열/수인성 계열/접촉·동물매개 계열/통합 연구, 증상: 표준형/은신형/공격형/통합 연구,
적응: 변이/은신/구조 강화/통합 연구).

이 사실은 브랜치 현황판 설계에 중요하다 — **행 개수가 카테고리를 넘나들어도 항상 4개로
고정**되므로, 현황판 자체는 내부 스크롤이나 "더 보기" 없이 고정 높이 컴포넌트로 설계할 수 있다.
(향후 카테고리/브랜치가 추가되면 이 전제가 깨지므로, 4-1. 확장성 항목에서 별도로 짚는다.)

---

## 1. 브랜치 현황판 배치 분석

### 1.1 후보 비교 — 기존 컴포넌트 재사용 관점

프로젝트에는 이미 "여러 항목을 동시에 요약해 보여주는" 두 패턴이 있다.

| 패턴 | 원본 | 형태 | 4브랜치에 적용 시 문제 |
|---|---|---|---|
| `distribution-row`/`distribution-item` | `CountryStatusPanel.uss` | 가로 4열, 열마다 칩+라벨+숫자 | 브랜치명이 "접촉·동물매개 계열"처럼 길어 4열로 쪼개면 줄바꿈이 심해지고, 진행률(n/m)을 칩 하나로 표현하기 어려움 |
| `population-bar` | `Hud.uss` | 세로 1줄, 세그먼트 3개가 `flex-grow` 비율로 폭 분할 | 브랜치가 4개라 이대로 못 씀 — 다만 **막대 자체의 구현 기법(Painter2D 없이 flex-grow 비율)은 그대로 재사용 가능** |

결론: 4열 가로 배치(`distribution-row` 그대로 복제)는 브랜치명 가독성 때문에 기각한다. 대신
**세로 4행 리스트**(이미 `ResearchDatabase_UI_FinalReview.md`의 E 옵션 목업이 스케치한 형태)를
채택하되, 각 행 안의 진행률 막대는 `population-bar__segment`의 `flex-grow` 비율 기법을 2세그먼트
(완료분/잔여분)로 축소해 재사용한다. 즉 **새 컴포넌트가 아니라 기존 두 패턴의 조합**이다 —
행 레이아웃은 `data-row`류 세로 스택, 막대는 `population-bar`류 비율 막대.

### 1.2 확정 레이아웃 — `branch-board` (신규, 로컬)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ ▶ 공기 계열            2/4  ██████░░░░ ┃  ← 선택됨(강조 테두리 + accent-bar)
┃   수인성 계열          0/3  ░░░░░░░░░░ ┃
┃   접촉·동물매개 계열    0/3  ░░░░░░░░░░ ┃
┃ 🔒 통합 연구            0/2  ░░░░░░░░░░ ┃  ← 잠김(선행 브랜치 미충족, 회색조)
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

- **행 4개 고정, 세로 스택.** 각 행 높이는 터치 타겟 최소값(`--touch-target-min`)을 만족해야
  하므로 32~36px, 4행 + 패딩으로 현황판 총 높이 약 160~180px — `UI_FinalReview.md`가 우려한
  "4존 압박" 문제를 이 정도 높이면 피할 수 있다고 판단한다(실측은 QA 단계에서 확인).
- **행 구성**: `[선택 마커] [브랜치명] [n/m] [진행률 막대]` — 브랜치명이 길어도 막대·숫자가
  고정폭이라 이름만 줄어들면 되므로 `research-row__name`과 동일하게 `white-space: normal`
  대신 **말줄임(`text-overflow`) 대신 폭을 넉넉히 주고 필요시 1회 줄바꿈 허용**(짧은 카테고리
  라벨이라 2줄까지 감안해도 행 높이가 크게 흔들리지 않음).
- **선택 상태**: 좌측 accent-bar(카테고리색, `research-row`와 동일 문법) + 텍스트 강조. `▶`
  글리프는 실제로는 아이콘 폰트가 아니라 `border-left` accent-bar 색 강조 + 굵게 처리로
  대체한다(프로젝트 관례 — 아이콘 폰트 글리프 실기기 미검증 문제, `TacticalModalController.
  AddSectionCaption()` 주석과 동일 이유).
- **잠김 상태(통합 연구)**: 선행 브랜치(보통 3개 기반 갈래) 진행이 일정 기준 미달이면
  `opacity: 0.6` + "🔒" 대신 텍스트 "잠김" 배지(회색) — `research-row--locked`의 opacity
  규칙을 그대로 계승.
- **진행률 막대**: `population-bar__segment`와 동일하게 완료 세그먼트(`flex-grow: unlockedCount`)
  + 잔여 세그먼트(`flex-grow: totalCount - unlockedCount`), 색상은 브랜치 상태에 따라
  카테고리색(진행 중) vs `--color-text-tertiary`(잠김)로 구분.
- **탭 동작**: 브랜치 행 탭 → 그 브랜치를 "활성 브랜치"로 지정 → 하단 연구 리스트가 해당
  브랜치 항목만 다시 렌더링(옵션 D의 "세그먼트 전환" 동작을 그대로 흡수, 다만 UI는 세그먼트
  칩이 아니라 이 현황판 자체가 선택자 역할을 겸함).
- **기본 선택값**: 카테고리 진입 시 "미완료 항목이 있는 첫 브랜치"를 자동 선택(순서: 공기→
  수인성→접촉/동물매개→통합). 화면을 나갔다 재진입 시 마지막 선택을 기억할지는 4-3 결정
  필요 항목으로 남긴다.

### 1.3 왜 가로 세그먼트(D 옵션)가 아니라 세로 리스트인가

`ResearchDatabase_MobileNav_Investigation.md`가 이미 지적했듯 세그먼트 칩은 브랜치명을
축약해야 해서("공기"/"수인성"/"접촉"/"통합") 정보가 줄어든다. 세로 리스트는 브랜치 풀네임 +
진행률을 동시에 상시 노출하면서도, 고정 4행이라 세그먼트의 유일한 강점(공간 절약)을 거의
따라잡는다 — "4브랜치 고정"이라는 이 프로젝트 데이터의 특수성이 두 안의 격차를 좁힌 것.

---

## 2. Research Database 전체 레이아웃 와이어프레임

참조 폭 480pt 기준. 카테고리는 전파(TRANSMISSION) 예시.

```
┌──────────────────────────────────────┐
│ [전파] 증상  적응              DNA:120 │  ← 상단 탭 3개(기존 유지, Commit 1)
├──────────────────────────────────────┤
│ TRANSMISSION LAB                      │  ← lab-caption(기존 유지)
│┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓│
│┃▶공기 계열          2/4 ██████░░░░  ┃│  ← 브랜치 현황판(신규, 1절)
│┃ 수인성 계열        0/3 ░░░░░░░░░░  ┃│
│┃ 접촉·동물매개 계열  0/3 ░░░░░░░░░░  ┃│
│┃🔒통합 연구          0/2 ░░░░░░░░░░  ┃│
│┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛│
├──────────────────────────────────────┤
│ TRANS-001 공기전파I    완료      —    │  ← 연구 리스트(활성 브랜치만, 기존
│ TRANS-002 공기전파II  진행중  18DNA  │    research-row 재사용, 최대 4행)
│ TRANS-003 비말전파    가능   22DNA  │
│ TRANS-004 비말전파강화 잠김      —    │
├──────────────────────────────────────┤
│ 공기 계열 · 2/4 해금                  │  ← 하단 패널(신규 역할, 3절 참고)
│ 다음 추천: TRANS-003 비말전파(22DNA)  │    "브랜치 요약 패널"로 축소
│ ────────────────────────────────      │
│         [ 광고 보고 DNA +10 ]         │
└──────────────────────────────────────┘
        ↓ TRANS-003 행 탭
┌ ─ ─ ─ ─ ─ ─ ─ (배경 딤) ─ ─ ─ ─ ─ ─ ┐
│  ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │
│  ┃ 비말 전파                  ✕┃  │  ← Research Popup(3절)
│  ┃ 기침·재채기로 배출된 비말을  ┃  │
│  ┃ 통해 근접 감염이 강화된다   ┃  │
│  ┃ ──────────────────────      ┃  │
│  ┃ 효과      전파력 +6%        ┃  │
│  ┃ 비용      22 DNA            ┃  │
│  ┃ 선행조건  공기 전파 II ✓    ┃  │
│  ┃ 상태      연구 가능         ┃  │
│  ┃    [ 닫기 ]  [ 연구 시작 ]  ┃  │
│  ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │
└──────────────────────────────────────┘
```

화면을 3존(탭·현황판 / 리스트 / 브랜치 요약)으로 유지하고, 항목 상세는 전부 팝업으로
뺐다는 점이 핵심 — `UI_FinalReview.md`의 메타 발견("상시 정보는 거시, 온디맨드 정보는 미시")을
그대로 따른다.

---

## 3. 하단 패널 재정의 — "연구 분석 콘솔" → "브랜치 요약 패널"

CLAUDE.md가 이미 명시한 신규 역할(현재 브랜치 요약/진행률/완료 수/다음 추천 연구)을 구체화한다.

```
┌──────────────────────────────────┐
│ 공기 계열 · 2/4 해금              │  ← data-row: 브랜치명 + 진행률
│ 다음 추천: 비말 전파 (22 DNA)     │  ← data-row: 다음 추천 연구
│         [ 광고 보고 DNA +10 ]     │  ← 기존 ad-bonus-button 유지
└──────────────────────────────────┘
```

- **표시 정보 2줄**: (1) 활성 브랜치명 + `n/m 해금`, (2) "다음 추천 연구" — 활성 브랜치 내에서
  상태가 `available`인 항목 중 가장 코드 번호가 낮은(=선행 단계가 가장 앞선) 항목 1개.
  후보가 없으면(전부 완료 또는 전부 잠김) "이 브랜치는 모두 연구 완료" / "선행 조건을
  먼저 해금하세요" 문구로 대체.
- **삭제되는 것**: 항목별 이름/설명/효과/비용/상태 표시, "연구 시작" 매수 버튼 — 전부 팝업으로
  이관(3절). 광고 보너스 버튼(`ad-bonus-button`)은 항목 상세와 무관한 재화 로직이라 그대로 유지.
- **컨테이너**: 기존 `detail-panel`(`tactical-panel` + 코너컷 4개) 셸은 그대로 쓴다 — 헤더
  타이틀만 "연구를 선택하세요"에서 활성 브랜치명으로 바뀌고, `detail-rows`에 채우는 `data-row`
  개수가 4개(이름/설명/비용/상태)에서 2개(브랜치 요약/추천)로 줄어드는 것뿐이라 UXML 변경이
  전혀 없다(4절에서 구체화).
- **갱신 시점**: 브랜치 현황판에서 다른 브랜치를 선택할 때, 그리고 팝업에서 "연구 시작"으로
  DNA를 소비해 상태가 바뀌었을 때(팝업이 닫히며 콜백) 둘 다 갱신.

---

## 4. Research Popup 구조 설계

### 4.1 표시 정보 — 필드별 처리

| 필드 | 표시 방식 | 비고 |
|---|---|---|
| 연구명 | `modal-title`(팝업 헤더, 기존 `TacticalModalController` 계약) | `CountryPopup`과 동일 |
| 설명 | 헤더 바로 아래 문단 텍스트 1줄(`white-space: normal`) | `detail-rows`가 아닌 별도 `Label` — 문단형 설명은 `data-row`에 억지로 넣지 않는다(DESIGN.md `detail-rows` 규칙: "문단형 설명 텍스트로 되돌리지 않는다"는 반복 판독 정보에 대한 규칙이지, 1회성 도입부 설명까지 금지하는 건 아니라고 해석 — 4-2에서 재확인 필요) |
| 효과 | `data-row`("효과" · "전파력 +6%") — 효과가 2개 이상이면(예: `infectivity`+`severity` 동시 부착 노드) `data-row`를 효과 개수만큼 반복하지 않고 **한 행에 쉼표로 병기**("전파력 +6%, 중증도 +1%") | 노드당 효과가 보통 1~3개뿐이라 병기가 리스트보다 간결 |
| 비용 | `data-row`("비용" · "22 DNA") — 이미 완료된 노드는 "완료"로 대체 | |
| 선행조건 | `data-row`를 선행 개수만큼 반복, 각 행 값에 충족(✓)/미충족 표시 — 선행 없으면 "없음" 1행 | `AddSectionCaption("선행 조건")`으로 그룹 헤더 삽입(기존 `CountryPopupController`의 "이동 통제" 섹션과 동일 패턴) |
| 상태 | `data-row`("상태" · "연구 가능"/"진행 중"/"완료"/"잠김") — 값 색상은 기존 `data-value--{state}` 4색 그대로 재사용 | 신규 색상 없음 |

### 4.2 버튼 구성 — `modal-footer`

```
[ 닫기 ]              [ 연구 시작 (22 DNA) ]
```

- **잠김(locked)**: `[ 닫기 ]`만 노출, 연구 시작 버튼은 숨김 또는 `SetEnabled(false)` +
  라벨 "선행 조건 필요"(회색, `UpgradeTree.uss`의 `research-row--locked` 톤 계승).
- **연구 가능(available)**: `[ 닫기 ]` + `[ 연구 시작 (N DNA) ]` — DNA 부족 시 버튼은 보이되
  비활성화 + 라벨 "DNA 부족"으로 교체(기존 `UpgradeManager` DNA 잔량 조회로 판정, 신규 로직
  아님).
- **진행 중(active, 이미 해금됨이지만 leaf가 아님)/완료(maxed)**: `[ 닫기 ]`만 — "연구 시작"
  버튼 자체를 렌더링하지 않는다(비활성 버튼을 남겨두는 것보다 명확).
- **레이아웃**: `modal-footer`는 현재 어떤 화면도 실사용하지 않는 빈 컨테이너다(`CountryPopup`은
  닫기 버튼을 헤더의 `popup-close`로 이미 처리해서 footer를 안 씀) — Research Popup이
  `modal-footer`의 **첫 실사용 사례**가 된다. 버튼 2개를 가로 배치·균등폭(`flex-grow: 1`
  각각, 사이 여백)으로 두는 규칙을 신규로 정의해야 하며, 이는 4절 DESIGN.md 반영안에도 포함.

### 4.3 상태 배지 색상 재확인

`data-value--locked/--available/--active/--maxed` 4색은 이미 `UpgradeTree.uss`에 정의돼 있고
팝업 상태 표시 색과 정확히 1:1 대응(잠김=회색/가능=accent-glow/진행중=DNA 녹색/완료=금색) —
신규 색상 정의가 필요 없다.

---

## 5. 구현 방안 — 기존 UXML/USS/컨트롤러와의 관계

### 5.1 신규 파일

| 파일 | 역할 | 참조 원본 |
|---|---|---|
| `Assets/UI/ResearchPopup.uxml` | Research Popup 뼈대 — `modal-root`(`tactical-panel`)+코너컷 4개+헤더+설명 Label+`modal-rows`+`modal-footer` | `CountryPopup.uxml` 구조를 그대로 복제(주석에 "Research Popup 버전" 명시), 설명 `Label` 한 줄만 추가 |
| `Assets/UI/ResearchPopup.uss` | 설명 텍스트 스타일 + `modal-footer` 2버튼 가로 배치(4-2) | `CountryPopup.uss`의 `popup-*` 로컬 클래스 패턴 계승 |
| `Assets/Scripts/UI/ResearchPopupController.cs` | `TacticalModalController` 상속, `Show(UpgradeNode node, ...)` 오버로드로 4-1 표 항목 채움 | `CountryPopupController.cs`가 이미 확립한 "베이스 상속 + Show() 오버라이드" 패턴 그대로 |

씬에는 `CountryPopupUI`와 동일하게 **화면 전체에 공유되는 팝업 GameObject 1개**를 추가한다
(카테고리별 3개 UIDocument와 별개 — 어느 카테고리 화면에서 항목을 탭해도 이 팝업 하나가 뜬다).

### 5.2 기존 파일 변경 (충돌 없음 확인)

- **`UpgradeTree.uxml`**: `node-scroll` 위에 브랜치 현황판 컨테이너(`branch-board`, 신규
  `VisualElement`) 1개만 추가. `detail-panel`/`detail-rows`/`buy-button` 등 기존 엘리먼트는
  이름·계층 구조 무변경 — `SelectDummyItem()`이 채우던 내용만 `BuildBranchSummary()`(신규
  메서드)로 대체되므로 UXML을 건드릴 필요가 없다. 다만 `buy-button`은 이제 "연구 시작"이 아니라
  브랜치 요약 패널에는 어울리지 않으므로 **제거하거나 숨김 처리**(하단 패널엔 광고 보너스
  버튼만 남고 구매 버튼은 팝업으로 완전히 이관) — 이 한 줄만 UXML 변경.
- **`UpgradeTree.uss`**: 신규 클래스만 추가(`branch-board`, `branch-row`, `branch-row__label`,
  `branch-row__progress`, `branch-row__progress-fill`, `branch-row--selected`,
  `branch-row--locked`) — 기존 `.research-row`/`.section-caption`/`.detail-panel` 등은 무변경.
  `.research-row`는 오히려 **역할이 단순해진다**: 지금은 모든 브랜치의 행을 한 화면에 그리지만,
  v2에서는 활성 브랜치 행만 그리므로 `BuildResearchList()`가 순회하는 브랜치 수가 1개로
  줄어든다(성능·스크롤 모두 이득, 클래스 자체는 그대로).
- **`UpgradeTreeView.cs`**: 다음 메서드만 추가/변경, 기존 이벤트(`OnCategoryRequested`)·씬 배선
  무변경.
  - 신규: `BuildBranchBoard()` — 4브랜치 진행률 집계 + 행 생성(1절).
  - 신규: `SelectBranch(DummyBranch)` — 활성 브랜치 상태 저장 + `BuildResearchList()`/
    `BuildBranchSummary()` 재호출.
  - 신규: `BuildBranchSummary()` — 3절 2행 채움, 기존 `SelectDummyItem()`의 상세 채움 로직을
    대체(그 메서드는 삭제 또는 `OpenResearchPopup()`로 이름·내용 교체).
  - 변경: `BuildResearchList()` — 전체 브랜치 순회 대신 `_activeBranch` 하나만 렌더링.
  - 변경: 항목 행 클릭 콜백 — 기존 `SelectDummyItem(item)` 대신 신규 이벤트
    `OnResearchItemSelected`를 발행해 `ResearchPopupController.Show()`를 트리거(카테고리별
    3개 `UpgradeTreeView`가 팝업 1개를 공유해야 하므로, 팝업 오픈 책임은 `UpgradeTreeView`가
    직접 갖지 않고 상위 조정자(`UIManager` 또는 신규 코디네이터)가 구독하는 편이 `CountryPopup`
    구독 패턴과 일관적 — 4-3 결정 필요 항목 참고).

### 5.3 Commit 2(실제 UpgradeNode 연결)와의 관계

`ResearchDatabase_MVP_ImplementationPlan.md`의 Commit 2 범위(더미 → 실제 `UpgradeManager.Tree`
연결)는 **구 인라인 리스트 레이아웃을 전제로 작성**됐다(브랜치 헤더는 있지만 브랜치 진행률
집계·팝업은 없음). 이번 v2 구조가 확정되면서 두 가지 옵션이 생긴다.

- **A. 통합 진행(권장)**: 브랜치 보드·팝업·요약 패널 구조 변경을 Commit 2와 합쳐서 한 번에
  실제 데이터로 진행. 어차피 "노드→브랜치" 그룹핑 테이블(`NodeMapping.md`가 이미 정리한
  갈래별 id 목록)이 브랜치 진행률 집계와 실제 데이터 연결 둘 다에 필요한 선행 작업이라, 따로
  하면 그룹핑 코드를 두 번 만지게 된다.
  - 정확한 이유: 브랜치 보드의 `n/m` 집계 없이는 UI 구조 검증(코너컷/막대 렌더링 확인)만
    가능하고 실제 게임 로직 검증은 못 하므로, 순수 UI 셸만 먼저 만드는 것의 이득이 Commit 1
    때보다 작다.
- **B. 단계 분리**: 이번엔 구조만(더미 브랜치 카운트로 보드 렌더링, 팝업은 더미 항목 하나로
  동작 검증) → Commit 2에서 실 데이터 연결. Commit 1과 동일한 전략을 반복하는 것이라 리스크는
  가장 낮지만 커밋이 하나 늘어난다.

이 문서는 방향만 제시하고 최종 선택은 5-1 결정 필요 항목으로 남긴다.

---

## 6. `DESIGN.md` 반영안

### 6.1 Component Library — 신규 항목 추가 (§277~332 뒤, `tab-button` 다음)

```
### branch-board / branch-row — 브랜치 진행률 리스트
`population-bar__segment`의 flex-grow 비율 막대 기법을 2세그먼트(완료/잔여)로 재사용한
세로 리스트. 각 행은 `data-row`류 레이아웃(라벨 좌측, 값 우측)에 진행률 막대가 붙은 확장형.
선택 상태는 `research-row--selected`와 동일하게 좌측 accent-bar + 굵게. 항목 수가 고정된
소규모 집합(현재는 4개)을 "요약 + 선택자"로 동시에 쓸 때 채택 — 후보가 8개를 넘으면 자체
스크롤 또는 다른 패턴을 재검토(§4-1 확장성 참고).
```

### 6.2 Usage Rules — 신규 서브섹션 (§398 Interaction Rules 근처, 또는 신규 "### Modal Footer")

```
### Modal Footer
`modal-footer`(`TacticalModalController.Footer`)에 액션 버튼을 둘 이상 두는 경우, 가로 배치 +
`flex-grow: 1` 균등폭을 기본으로 한다(부차 액션은 `tab-button--secondary`의 중립 테두리,
주 액션은 `accent-glow` 발광 테두리로 구분 — 기존 tab-button 톤 재사용, 신규 버튼 색상 축
추가 금지).
```

### 6.3 Do / Don't 추가

```
Do: 항목(연구/업그레이드/향후 이벤트 등) 상세 정보는 항상 팝업(TacticalModalController
상속)에 담고, 상시 노출 패널에는 요약·집계 정보만 둔다 — Research Database v2 설계에서
검증된 원칙(ResearchDatabase_UI_FinalReview.md 메타 발견).

Don't: 상시 노출 리스트/보드에 "다음 항목 미리보기" 이상의 상세 텍스트(효과·비용 전체
목록)를 욱여넣지 않는다 — 정보 밀도 문제가 되풀이된다(Commit 1의 인라인 리스트 문제,
`ResearchDatabase_MobileNav_Investigation.md` A안 참고).
```

이 세 군데는 실제 `DESIGN.md` 편집은 이번 문서 범위가 아니다(코드/문서 변경 금지 원칙과
동일하게 이 설계 문서도 실제 파일을 건드리지 않는다) — 구현 커밋 시점에 함께 반영한다.

---

## 7. 결정 필요 사항 (다음 세션)

1. **Commit 순서**: 5-3의 A(통합 진행, 권장) vs B(구조 먼저 더미로 검증 후 Commit 2에서
   데이터 연결) 중 선택.
2. **팝업 오픈 책임 소재**: `UpgradeTreeView`(카테고리별 3개 인스턴스) 각자가 팝업을 직접
   열지, 상위 코디네이터가 이벤트를 구독해 열지 — `CountryPopupController`가 `WorldMap.
   OnCountryClicked`를 직접 구독하는 현재 패턴(CLAUDE.md 버그 항목에서 이미 "죽은 코드인 줄
   알았는데 살아있었다"는 혼란의 원인으로 지목된 그 패턴)을 반복할지, 아니면 이번 기회에 더
   명시적인 단일 진입점으로 정리할지.
3. **브랜치 선택 기억 정책**: 카테고리 화면 재진입 시 마지막으로 보던 브랜치를 기억할지,
   항상 "미완료 첫 브랜치"로 초기화할지.
4. **설명 문단 vs `detail-rows` 규칙 충돌 여부**: `DESIGN.md` `detail-rows` 컴포넌트 설명의
   "문단형 설명 텍스트로 되돌리지 않는다"가 반복 판독 정보(리스트·상시 패널)에 대한 규칙인지,
   팝업 1회성 설명 문단까지 포함하는지 재확인 필요 — 이번 설계(4-1)는 전자로 해석해 팝업에
   문단 `Label`을 허용했다.
5. **4브랜치를 넘는 확장 시나리오**: 지금은 우연이 아니라 데이터 설계(3갈래+통합)로 4개가
   고정이지만, 향후 카테고리·브랜치가 늘어날 경우 `branch-board`가 고정 높이를 벗어나는
   시점의 대응(자체 스크롤 vs 접기)은 이번 범위에서 제외.
