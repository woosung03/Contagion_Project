# Contagion Project — 전염병 주식회사 클론 게임

Unity 기반 전략 시뮬레이션 게임. 앱인토스(Apps in Toss) 플랫폼 타겟.
세션을 시작할 때마다 이 파일이 자동으로 로드된다 — 아래 순서대로 참고할 것.

---

## 세션 시작 시 읽는 순서

1. 이 파일 (`CLAUDE.md`) — 프로젝트 현황 + 규칙 (가볍게 유지)
2. `Docs/GameDesignDocument.md` — 전체 게임 설계 스펙 + 튜닝 파라미터 (기획 원본)
3. `Docs/DevLog.md` — **필요할 때만.** Step별 구현 배경·설계 변경 이유·버그 진단 아카이브.
   특정 Step/버그를 재조사할 때만 검색해서 찾아볼 것.
4. `Docs/DESIGN.md` — **UI 작업 시 항상 먼저.** 디자인 토큰(색상/타이포/스페이싱)·Surface
   Hierarchy·컴포넌트 규칙·Do/Don't. 화면 배치와 무관하게 모든 UI 변경의 기준.
5. `Docs/Archive/UI_Design.md` — **화면별 배치/와이어프레임 작업 시에만.** 화면별 적용 우선순위·
   화면 단위 설계(토큰 자체는 `DESIGN.md` 참고).
6. `Docs/Archive/Architecture.md` — **새 스크립트/UI 에셋 추가나 코드 위치 확인 시에만.** 네임스페이스·
   폴더 구조·엔진 정보.
7. `Docs/Archive/QA_Checklist.md` — **플레이테스트/기능 확인 시.** "이게 잘 되는지 확인해야 함" 항목.
8. `Docs/Archive/Release_Checklist.md` — **배포 준비 작업 시.** 저작권/라이선스/스토어 등록/SDK 최종 검증.
9. `Docs/Archive/unity-editor-task.md` — **Unity 에디터 수동 작업 확인 시.** 프리팹 연결·씬 배선·
   UIDocument 연결 등 코드 도구로 할 수 없는 작업 목록.
10. 필요 시 `C:\Game\codebase` (참조 전용 코딩 지식 위키, 별도 저장소, **수정 금지**) — Unity UI
   Toolkit / 앱인토스 SDK / LLM 에이전틱 패턴 문서. 앱인토스 연동(광고/랭킹/Safe Area/빌드)
   작업 시 `wiki/apps-in-toss-unity/_overview.md`부터 확인.

---

## 제약사항

- 타겟 화면: 세로(Portrait) 고정, 갤럭시 S25 울트라 기준(1440×3120, 19.5:9). 새 UI 화면/레이아웃
  작업 시 가로 모드는 고려하지 않아도 됨(회전 잠김).

---

## 현재 구현된 시스템

기능 단위 요약만 남긴다. 구현 배경·이유는 `Docs/DevLog.md`, 코드/폴더 구조는 `Docs/Archive/Architecture.md`
참고.

**UI 시스템 전제**: Contagion Project UI는 "Gameplay UI"/"Preparation UI"로 분리된 두 개의
디자인 시스템이 아니라, **단일 Tactical Design System**을 사용한다(`Docs/DESIGN.md`가
정본). 화면별 차이는 별도 시스템이 아니라 **Layout System**(화면 분류) 차이다 — Fullscreen
(MainMenu/CountrySelect/UpgradeTree/EndingScreen), Overlay Panel(ResearchPopup), Bottom Sheet
(CountryPopup/CountryStatusPanel/RankingPanel), HUD Chrome(resource-strip/event-dock/
population-bar/graph-panel/action-strip) 4가지. 정의는 `Docs/DESIGN.md` > 7. Layout System 참고.

- 핵심 데이터/시뮬레이션 — `Pathogen`/`Country`/`WorldState`, 틱 기반 감염·사망·치료제 진행
  (`SimulationManager`), `WorldDataManager`, `GameManager`(페이즈/난이도/일시정지)
- 세계 지도 — `WorldMap`+`CountryView`(국가별 실제 실루엣, 좌우 드래그 스크롤, `PolygonCollider2D`
  기반 국가 클릭 판정), 국가현황 리스트 패널, DNA 버블 스포너(오브젝트 풀링)
- 감염 점 오버레이 — 국가별 개별 점(면적 비례 크기/개수), `dotsEnabled=true`/`hotspotsEnabled=false`
  (핫스팟 방식은 폐기, 코드는 보존)
- 글로벌 교통망 — 항공/해운 허브 46개(28개국 보유), 유닛이 지도 위를 이동하며 도착 시 감염 전파,
  해상 항로는 육지 마스크 기반 A* 길찾기로 계산
- 교통 유닛 그래픽 — 이모지 기반(✈️ / `Ship 1.png`), carrier(감염)는 색상 윤곽선으로 구분
- 게임플레이 시스템 — `UpgradeManager`(DNA 트리, 국가 48개·병원체 6종·업그레이드 45노드),
  `HumanResistanceManager`(인류 저항 AI), `EventManager`(뉴스피드 이벤트)
- UI Toolkit 전체 — HUD/업그레이드 트리/국가 상태 패널/뉴스피드/엔딩/랭킹, 공통 `Theme.uss` 디자인 토큰
- Tactical Design System 전환 — CountrySelect(country-row accent bar+data-row, detail-panel
  tactical-panel)/MainMenu(pathogen-card tactical-panel+corner-cut+data-row, detail-panel)/
  EndingScreen(통계+스코어 패널 tactical-panel+corner-cut, data-row 4줄, hero 스코어)/
  CountryPopup(`TacticalModalController` 공용 프레임 상속 — 이후 Bottom Sheet로 전환, 아래 참고)/
  RankingPanel(tactical-panel+corner-cut 4개+tactical-panel__header+data-row+popup-footer-button
  2개 균등폭, UI Audit C등급 해소 — Tactical Design System 전체 화면 적용 완료)
- CountryPopup → Bottom Sheet 전환 완료 — 화면 하단 고정(지도를 가리지 않음), `quick-stat-grid`
  11개 필드(국기·대륙·인구·감염자·사망자·감염률·공항·항만·의료·국경 상태 + 상세보기 버튼),
  `badge-tag` severity 칩(공항/항만/의료/국경 상태). 기존 도넛 차트/세계 순위/이동 통제 섹션은
  제거되고 "상세 보기" 버튼으로 CountryStatusPanel과 연결됨(아래 참고). Country Dock
  (`CountryDockController`, 우측 상단 상시 표시 패널)은 중복 UI로 판단돼 완전히 제거됨
  (UXML/USS/씬 컴포넌트 포함)
- CountryStatusPanel → GLOBAL STATUS CENTER, 정체성 확정: "전파 전략 콘솔"이 아니라 "성과
  대시보드"(세계 감염 진행 상황 확인 화면, 근거: DevLog Step 88). GLOBAL STATUS 한줄평가/
  Hero Stats(INFECTED·DEATHS·CURE·EXTINCT 2x2 KPI, 근거: DevLog Step 89)/WORLD OVERVIEW(세계
  요약+감염 국가 현황 통합 캡션, Hero Stats와 중복되는 총 감염자·총 사망자·무감염 국가 수 행은
  제거)/ADVANCED DIAGNOSTICS(국가 상태 분포+의료 시스템 현황, 기본 접힘 아코디언)/감염자 TOP 10/
  사망자 TOP 10/48개국 목록(대륙 아코디언, accent bar, 최하단 상세 조회 도구) 순 Performance
  Dashboard, tactical-panel+코너컷+data-row+severity 4색 체계
- CountryStatusPanel 리스트→상세 팝업 드릴다운 — Research Database와 동일 패턴(리스트 행 클릭 →
  이벤트 발행 → `UIManager` → 상세 팝업). 48개국 목록의 `status-row` 클릭 시
  `CountryStatusPanelController.OnCountryRowSelected` → `UIManager` → `CountryPopupController.
  ShowCountry()`로 지도 클릭과 동일한 국가 상세 팝업이 열린다. 새 화면은 만들지 않고 기존
  `CountryPopupController`/`CountryStatusPanelController`를 재사용, `CountryPopupUI`의
  `m_SortingOrder`를 2로 올려 `CountryStatusPanelUI`(1) 위에 항상 렌더링되도록 수정
- CountryPopup Bottom Sheet → CountryStatusPanel 연결(반대 방향) — 팝업의 "상세 보기" 버튼 →
  `CountryPopupController.OnDetailRequested`(Country) 발행 → `UIManager`가
  `TransitionTo(AppScreen.GlobalStatus)`(AppScreen 상태 머신 경유, WorldMapInputLock 처리 포함)로
  연 뒤 `CountryStatusPanelController.FocusCountry(country)` 호출 — 해당 국가의 대륙 아코디언만
  자동으로 펼쳐진다(자동 스크롤/행 강조는 미구현, 후속 과제). 국가 클릭 → Bottom Sheet →
  상세보기 → Country Status 흐름 완성
- CountryStatusPanel 48개국 목록 → 대륙별 접기/펼치기 아코디언 — 6대륙(ASIA/EUROPE/NORTH
  AMERICA/SOUTH AMERICA/AFRICA/OCEANIA) 헤더 클릭으로 토글, ASIA만 초기 펼침. 국가 행
  클릭(`OnCountryRowSelected`)·`CountryPopup` 재사용 구조는 그대로 유지, `Country`에 대륙
  필드를 추가하지 않고 컨트롤러 전용 id→대륙 매핑으로 처리
- Research Database v2 — 업그레이드 트리 화면을 절대좌표 캔버스에서 브랜치 보드(계열 3+통합 1,
  진행률 n/m) + 세로 스크롤 리스트(선택된 브랜치만)로 전환, `UpgradeManager.Tree`의 실제 45개
  `UpgradeNode`에 연결 완료(코드/이름/상태/비용 전부 실데이터). 연구 항목 행 클릭 →
  `UpgradeTreeView.OnResearchItemSelected(UpgradeNode)` 발행 → `UIManager`가 3개 뷰 모두 구독해
  `ResearchPopupController.Show()` 호출까지 배선 완료 — 연구 항목을 클릭하면 이름/브랜치/설명이
  담긴 상세 팝업이 실제로 뜬다(Close 버튼 포함). `UpgradeManager.OnNodeUnlocked` 구독으로 활성
  뷰의 브랜치 보드/요약을 갱신하는 부분과 팝업의 "연구 시작"/"취소" 버튼 로직은 아직 미구현
- 플랫폼 연동 — 앱인토스 보상형 광고(`GameAds`), 랭킹(게임센터 리더보드), 저장(로컬 폴백 + AIT Storage 훅)
- 화면 플로우 — `MainMenu`(병원체 선택)/`CountrySelect`(발원 국가 선택, 국기 48/48)/`GamePlay`,
  재시작 루프 안정화
- 모바일 타겟팅 — 세로 화면 고정, SafeArea 적용, 국가 지리적 재배치(경도/위도 기반)

최근 작업 이력(Step 단위)은 `Docs/DevLog.md` 참고 — 가장 최근 기록은 Step 85(CountryStatusPanel
리스트→상세 팝업 드릴다운 확장, `CountryPopupUI` sortingOrder 충돌 수정 포함). Step 84 이후
`UIManager`가 3개 뷰의 `OnResearchItemSelected`를 구독해 `ResearchPopupController.Show()`를
호출하는 배선(커밋 7 절반)을 완료했으나 아직 DevLog에 Step으로 기록되지 않음 — 다음 DevLog 갱신
시 반영 필요. 검증은 `Docs/Archive/QA_Checklist.md` 참고.

---

## 현재 TODO

지금 실행해야 할 **다음 작업**만 남긴다. "확인/검증"류 항목은 `Docs/Archive/QA_Checklist.md`,
배포 점검은 `Docs/Archive/Release_Checklist.md`, 에디터 수동 배선은 `Docs/Archive/unity-editor-task.md`로 이동됨.

**Research Database v2 — 커밋 7 나머지 절반** (근거:
`Docs/Archive/ResearchDatabase_V2_ImplementationPlan.md` §1/§4)

- 커밋 1~6 완료. 커밋 7 중 "`UIManager`가 3개 `UpgradeTreeView.OnResearchItemSelected` 구독 →
  `ResearchPopupController.Show()` 호출"까지 완료 — 연구 항목 행을 클릭하면 이름/브랜치/설명이
  담긴 팝업이 실제로 뜬다.
- `UpgradeManager.OnNodeUnlocked` 구독으로 활성 뷰(브랜치 보드/요약)를 갱신하는 나머지 절반이
  남음(계획서 §4 커밋 7 표 3번째 항목).
- 이후 폴리싱(`LockReason()`/CTA 버튼 문구 4갈래, 커밋 8) → 구 코드 정리(`buy-button` UXML 제거
  등, 커밋 9) → 문서 반영(`DESIGN.md`/`QA_Checklist.md`/`unity-editor-task.md`, 커밋 10) → 에디터
  씬 배선(`ResearchPopupUI` GameObject)+실기기 검증(커밋 11)까지 계획서 §1/§4 순서 그대로 진행.
- Phase 2~4(`environmentResistance`/`medicalBurdenModifier`/`unlockedFlags` 소비, 이번 범위
  아님)는 `Docs/Archive/ResearchDatabase_RuntimeSystems.md` §9 순서를 따르고, 그 문서 §11의 미결정
  항목과 `NodeMapping.md` §8의 잔여 항목(DNA 비용 프리미엄·항원 변이 확률 밸런스)을 착수 전 확인.

**UI 기술 부채 — Tactical.uss 로컬 중복 정의 제거** (근거: `Docs/Archive/UI_Design.md` §15
Historical Notes, §16.1)

- `Hud.uss`(155~165행)에 `tactical-panel`/`corner-cut`/`data-row`가 `Tactical.uss`와
  별개로 로컬 정의되어 있음 — `Tactical.uss` 참조로 교체하고 로컬 정의 제거 필요.
- `UpgradeTree.uss`(84~145행)도 동일한 로컬 중복(노드 상태 variant는 UpgradeTree 전용이라
  잔류 유지). 두 파일 모두 기능상 문제는 없음(같은 값 이중 정의일 뿐) — 제거 후 45노드/
  코너컷/상태색 회귀 확인 필요.

**조사 필요**

- 앱인토스 SDK 연동 착수 전 Unity 6.x 공식 지원 여부 재확인

---

## 프로젝트 규칙

- 이 프로젝트는 개발 대상이다 (참조 전용인 `C:\Game\codebase`와 다름). 자유롭게 수정한다.
- **CLAUDE.md 역할은 두 가지뿐이다**: (1) 현재 프로젝트 상태, (2) 다음에 해야 할 작업. 새 정보를
  적기 전 반드시 판단한다 — "이 정보가 현재 상태 또는 다음 작업인가?" **NO → 아래 표에 따라 이동.**
  CLAUDE.md에서 제거하는 정보는 삭제하지 않고 반드시 이동한다.

  | 내용 유형 | 이동할 문서 |
  |---|---|
  | Step별 작업 내역, 구현 과정, 설계 변경 이유, 버그 원인 분석, 디버깅 기록, 시행착오, 성능 최적화 과정 | `Docs/DevLog.md` |
  | 게임 규칙, 감염/치료제 공식, 밸런스 수치, 튜닝 파라미터, 시스템/경제/자원/AI 설계 | `Docs/GameDesignDocument.md` |
  | 디자인 토큰, 색상 체계, Typography, Spacing, Surface Hierarchy, 공통 UI 컴포넌트 규칙, Do/Don't | `Docs/DESIGN.md` |
  | 화면별 배치/와이어프레임, 화면 적용 우선순위 | `Docs/Archive/UI_Design.md` |
  | 코드 네임스페이스, 폴더 구조, 스크립트/에셋 위치, 엔진/렌더파이프라인 정보 | `Docs/Archive/Architecture.md` |
  | 기능 검증, 플레이테스트 체크리스트, "확인해볼 것" 형태 항목 | `Docs/Archive/QA_Checklist.md` |
  | 저작권/라이선스 확인, 스토어 등록 준비, SDK 최종 검증, 출시 전 점검 | `Docs/Archive/Release_Checklist.md` |
  | 프리팹/SerializedField/UIDocument 연결, 씬 배선, Build Settings 수정 | `Docs/Archive/unity-editor-task.md` |

- **TODO 관리**: 항목은 명령문 1줄로 쓴다. 배경 설명은 "(근거: DevLog Step N)"만 남기고 본문은
  DevLog에 적는다. 해결이 확인된 항목은 즉시 삭제한다(보류·취소선 금지). 검증성 항목은 애초에
  TODO에 넣지 않고 `Docs/Archive/QA_Checklist.md`에 바로 적는다.
- **완료된 시스템 목록 관리**: 기능명만 나열한다(Step 번호·서술 문장 금지). 새 시스템이 생기면
  이 목록에 한 줄만 더한다.
- **Git 우선 원칙**: 작업 이력의 원본은 Git이다. CLAUDE.md는 현재 상태 설명 문서일 뿐 이력
  보관소가 아니다. 오래된 구현 내역은 Git 커밋 히스토리와 `Docs/DevLog.md`에서 확인한다.
