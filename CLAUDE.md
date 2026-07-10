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
5. `Docs/UI_Design.md` — **화면별 배치/와이어프레임 작업 시에만.** 화면별 적용 우선순위·
   화면 단위 설계(토큰 자체는 `DESIGN.md` 참고).
6. `Docs/Architecture.md` — **새 스크립트/UI 에셋 추가나 코드 위치 확인 시에만.** 네임스페이스·
   폴더 구조·엔진 정보.
7. `Docs/QA_Checklist.md` — **플레이테스트/기능 확인 시.** "이게 잘 되는지 확인해야 함" 항목.
8. `Docs/Release_Checklist.md` — **배포 준비 작업 시.** 저작권/라이선스/스토어 등록/SDK 최종 검증.
9. `Docs/unity-editor-task.md` — **Unity 에디터 수동 작업 확인 시.** 프리팹 연결·씬 배선·
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

기능 단위 요약만 남긴다. 구현 배경·이유는 `Docs/DevLog.md`, 코드/폴더 구조는 `Docs/Architecture.md`
참고.

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
  CountryPopup(신규 `TacticalModalController` 공용 프레임 상속, modal-root+tactical-panel)
- CountryPopup 국가 대시보드 확장 — 감염 상태 도넛 차트(`CountryDonutChart`, Painter2D)/감염
  통계(감염률·치사율 추정·생존자 수)/의료 시스템 부하(신규 계산식 4단계)/세계 순위(감염자·사망자·
  감염률 각각 별도 행)/이동 통제 섹션(공항·항구·국경, 섹션 캡션+좌측 accent bar), 최근 국가 이벤트는
  후속 과제로 보류. `TacticalModalController`에 `AddSectionCaption()`/`AddRow(rowClass)` 공용 확장 추가
- CountryStatusPanel → GLOBAL STATUS CENTER 세계 감염 현황 센터 — GLOBAL STATUS 한줄평가/세계
  요약(population-bar+data-row 6줄)/감염 국가 현황(감염·무감염·소멸 국가 수)/국가 상태 분포
  (SAFE·WARNING·DANGER·COLLAPSE 4버킷)/의료 시스템 현황(정상·주의·과부하·붕괴 4버킷)/랭킹 2종
  (종합 위협도·감염자 각 TOP 10)/48개국 목록(accent bar 유지) 순 Tactical Dashboard, tactical-panel+
  코너컷+data-row+severity 4색 체계
- 플랫폼 연동 — 앱인토스 보상형 광고(`GameAds`), 랭킹(게임센터 리더보드), 저장(로컬 폴백 + AIT Storage 훅)
- 화면 플로우 — `MainMenu`(병원체 선택)/`CountrySelect`(발원 국가 선택, 국기 48/48)/`GamePlay`,
  재시작 루프 안정화
- 모바일 타겟팅 — 세로 화면 고정, SafeArea 적용, 국가 지리적 재배치(경도/위도 기반)

최근 작업 이력(Step 단위)은 `Docs/DevLog.md` 참고 — 가장 최근은 Step 81(CountryStatusPanel에
GLOBAL STATUS 한줄평가(WorldState.GetMortalityStage()+세계 감염률 조합, 신규 계산 없음)/감염 국가
현황(감염·무감염·소멸 국가 수)/의료 시스템 현황(정상·주의·과부하·붕괴 4버킷, 국가 상태 분포와 동일
UI 재사용)을 추가하고, 기존 감염률·치사율·의료부하 TOP10 랭킹 3개를 종합 위협도(ThreatIndex,
감염률40%+치사율30%+의료부하30% 가중합) TOP10 하나로 통합(랭킹 섹션 4개→2개) — "정보량을 늘리지
말고 전략적 판단 데이터를 추가하라"는 요청에 따라 6개 후보 중 사용자가 승인한 4개만 구현
(GLOBAL HOTSPOT 카드·교통 허브 위험도는 제외). 신규 데이터 모델 없음. 검증은 `Docs/QA_Checklist.md`
참고) — Step 80(CountryStatusPanel을
48개국 목록뿐이던 화면에서 "GLOBAL STATUS CENTER" 세계 감염 현황 센터로 재설계 — CountryPopup은
개별 국가 상세, CountryStatusPanel은 세계 전체 요약으로 역할 분리. 세계 요약(population-bar 재사용
+ data-row 6줄)/국가 상태 분포(GetCollapseStage() 6단계를 SAFE·WARNING·DANGER·COLLAPSE 4버킷으로
집계)/랭킹 4종(감염자·감염률·치사율(추정)·의료 부하 각 TOP 10, CountryPopupController의 계산식을
독립 복제해 재사용)/48개국 목록(기존 O(1) 행 캐싱 유지, 좌측 accent bar로 버킷 색상 표시) 순.
tactical-panel+코너컷+data-row+severity 4색으로 전환 — UI_Design.md 진단 당시 이 화면은 애초
전환 대상 목록에 없었으나 이번에 합류. 도넛 차트(CountryDonutChart)는 이 화면에 맞지 않다고
판단해 미사용(population-bar로 대체). CountryPopupController/CountryPopup.uxml/.uss는 미변경.
신규 데이터 모델 추가 없음. 검증은 `Docs/QA_Checklist.md` 참고) — Step 79(CountryPopup 2차
다듬기 — 세계 순위를 "감염자 N위 · 사망자 N위 · 감염률 N위" 한 줄 압축에서 각각 별도 data-row
3개로 분리. 공항/항구/국경 3행은 시인성 개선을 위해 "이동 통제" 섹션 캡션 + 좌측 accent bar
(`data-row--open`/`data-row--closed`)로 그룹화 — 아이콘 폰트 글리프는 실기기 폰트 지원 검증이
안 돼(Hud.uss news-entry-dot 주석과 동일 이유) 텍스트+색상 바 조합을 대신 택함.
`TacticalModalController`에 `AddSectionCaption()` 신규 + `AddRow()` 4번째 인자(`rowClass`) 추가 —
공용 확장이라 다른 소비 화면(CountrySelect/EndingScreen/UpgradeTree) 영향 없음. 검증은
`Docs/QA_Checklist.md` 참고) — Step 78은 CountryPopup 국가
대시보드 1차 확장 — 사전 조사 문서 `Docs/CountryStatus_Dashboard_Investigation.md`의 최종 추천안 중
사용자 승인 4항목만 구현: 감염 상태 도넛 차트(`CountryDonutChart.cs` 신규, HudSparkline과 동일한
Painter2D 방식)/감염 통계(감염률·치사율 추정·생존자 수)/의료 시스템 부하(감염률×(1-의료수준) 계산식
신규, 기존 CountryCollapseStage와는 별개 축)/세계 순위(48개국 LINQ 정렬, OnWorldStateChanged 추가
구독으로 팝업이 열려있는 동안 매 틱 갱신). 기존 "감염자"/"사망자" data-row 2줄은 도넛 범례로 대체.
"최근 국가 이벤트"는 NewsEvent에 countryId가 없고 HumanResistanceManager 봉쇄 변화가 Debug.Log뿐이라
구조화된 이력이 없어 이번 범위에서 제외(후속 Step 후보). Unity 에디터 미접속으로 Painter2D 렌더링
미검증 — 검증은 `Docs/QA_Checklist.md` 참고) — Step 77은 인구 축약 표기 위치
정정 — CountryPopup은 상세 정보 창이므로 N0 전체 숫자로 되돌리고, Step 76에서 도입한
`FormatPopulation()`은 대신 `CountryDockController`(우측 상단 상시 표시 위젯, `.country-dock` 폭
140px로 CountryPopup보다 더 좁음)로 이관 — CountryPopup=전체 숫자/Country Dock=축약 숫자로 역할
분리. 검증은 `Docs/QA_Checklist.md` 참고) — Step 76은 CountryPopup 인구수/
공항·항구·국경 오버플로우 조사·수정 — 원인은 `Tactical.uss` `.data-value`에 wrap/shrink 처리가
없던 공용 결함, 320px 폭인 CountryPopup에서 처음 드러남. `Tactical.uss` 공용 수정 + 인구 억/만
축약 표기 + 공항/항구/국경 개별 행 분리로 해결, MainMenu 48행 리스트는 별도 오버라이드로 기존
한 줄 동작 보존. 검증은 `Docs/QA_Checklist.md` 참고 — Step 75(감염 점 실제 커버리지
검증 후 크기 공식 재수정 — 한국/일본/영국 기준 감염률 100% 시 실제 면적 대비 커버리지를 계산해보니
Step 74의 sqrt 완화로도 여전히 일본 118.7%/영국 105.7%로 설계 목표(70%)를 초과하고 있었다. 원인은
`diameter`·점 개수가 이미 국가별로 70% 커버리지를 만족하도록 공동 계산된 완결값인데
`CountryView.SetupInfectionDots()`가 그 위에 국가 크기 비례 배율(Step 53의 sizeRatio, Step 74의
sqrt(sizeRatio))을 또 곱한 이중 반영이었음 — 배율을 완전히 제거하고 `diameter`를 그대로 최종
크기로 쓰도록 단순화(`infectionDotDiameterScale`은 다시 전 국가 공통 전역 배율로만 사용). diameter·
count 자체가 이미 실제 국가 면적에 비례하므로 배율 없이도 대형국 강조 효과는 유지되며, 48개국 전부
설계 목표(70%)를 정확히 만족. Unity 에디터 미접속으로 실제 렌더링 검증은 못 했고 오프라인 수치
계산으로만 확인 — 남은 검증은 `Docs/QA_Checklist.md` "감염 점 오버레이 확인" 섹션 참고) — Step 74는
같은 버그를 sqrt로 1차 완화(대형국 배율 32배→13.5배, 이후 Step 75로 대체) — Step 73은 국가 선택
콜라이더를
`BoxCollider2D`(사각형)에서 `PolygonCollider2D`(실제 실루엣)로 전환 — 인접국 클릭 시 오탭되는
문제의 근본 원인이 Step 72의 사각형 폴백 콜라이더 자체였음을 조사로 확인 후 조치. `Assets/Editor/
CountryShapePhysicsShapeGenerator.cs` 신규(48개 `CountryShapes` 텍스처의 "Generate Physics Shape"
임포트 옵션을 배치로 켜는 에디터 전용 툴), `CountryView`는 `Sprite.GetPhysicsShapeCount()/
GetPhysicsShape()`로 임포트 시점에 구워진 실제 국가 실루엣 폴리곤을 콜라이더 경로로 사용하고
Awake()에서 씬에 남은 레거시 `BoxCollider2D`를 자가 정리(씬 파일 미변경). Editor 배치 미실행
국가는 Step 72 방식(사각형)으로 자동 폴백. **Generate Physics Shapes 배치 실행 2026-07-10 사용자
확인 완료** — 남은 검증은 `Docs/QA_Checklist.md` "PolygonCollider2D 전환 검증" 섹션(Validate 실행,
인접국/다도서국/극소국 클릭 테스트) 참고). Step 72는 국가 클릭 반경이 월드맵 전체로 잡히던 문제 수정
— Step 71은 엔딩 화면 점수 잘림 + 버튼 디자인 불일치 수정 — Step 64는 CountrySelect 마무리
(9절/16.2 ①)/Step 65는 MainMenu 감사(8절/16.2 ②)/Step 66은 EndingScreen 감사(10절/16.2 ③)/
Step 67은 CountryPopup → Tactical Modal Base 승격(12절/13절/16.2 ④)/Step 68은 이벤트 디스패치
방어 강화/Step 69는 자가진단 경고 로그 3종 추가).

---

## 현재 TODO

지금 실행해야 할 **다음 작업**만 남긴다. "확인/검증"류 항목은 `Docs/QA_Checklist.md`,
배포 점검은 `Docs/Release_Checklist.md`, 에디터 수동 배선은 `Docs/unity-editor-task.md`로 이동됨.

**UI 디자인 시스템 적용** (근거: `Docs/UI_Design.md`, CountrySelect/MainMenu/EndingScreen/
Tactical Modal은 Step 64/65/66/67로 완료. 남은 1개 화면은 §14 — `RankingPanel`)

- RankingPanel tactical-panel 축소판 전환 (`Docs/UI_Design.md` §11, §16 실행계획에서는 스코프 아웃 — 착수 시 재검토)

**버그 — 결정 필요** (근거: DevLog Step 67)

- 국가 탭 시 Country Dock(HUD)과 CountryPopup(모달)이 동시에 뜬다 — `CountryPopupController`가
  Step 28-2 이후 죽은 코드로 방치된 줄 알았으나 실제로는 `WorldMap.OnCountryClicked` 구독이
  살아있고 씬 GameObject도 활성 상태. 자동 팝업을 계속 띄울지, Country Dock으로 완전히
  대체하고 팝업 트리거는 끌지 결정 필요(끄는 쪽이면 `CountryPopupController.Subscribe()`의
  `WorldMap.OnCountryClicked` 구독 제거 또는 `CountryPopupUI` GameObject 비활성화 한 줄이면 됨)

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
  | 화면별 배치/와이어프레임, 화면 적용 우선순위 | `Docs/UI_Design.md` |
  | 코드 네임스페이스, 폴더 구조, 스크립트/에셋 위치, 엔진/렌더파이프라인 정보 | `Docs/Architecture.md` |
  | 기능 검증, 플레이테스트 체크리스트, "확인해볼 것" 형태 항목 | `Docs/QA_Checklist.md` |
  | 저작권/라이선스 확인, 스토어 등록 준비, SDK 최종 검증, 출시 전 점검 | `Docs/Release_Checklist.md` |
  | 프리팹/SerializedField/UIDocument 연결, 씬 배선, Build Settings 수정 | `Docs/unity-editor-task.md` |

- **TODO 관리**: 항목은 명령문 1줄로 쓴다. 배경 설명은 "(근거: DevLog Step N)"만 남기고 본문은
  DevLog에 적는다. 해결이 확인된 항목은 즉시 삭제한다(보류·취소선 금지). 검증성 항목은 애초에
  TODO에 넣지 않고 `Docs/QA_Checklist.md`에 바로 적는다.
- **완료된 시스템 목록 관리**: 기능명만 나열한다(Step 번호·서술 문장 금지). 새 시스템이 생기면
  이 목록에 한 줄만 더한다.
- **Git 우선 원칙**: 작업 이력의 원본은 Git이다. CLAUDE.md는 현재 상태 설명 문서일 뿐 이력
  보관소가 아니다. 오래된 구현 내역은 Git 커밋 히스토리와 `Docs/DevLog.md`에서 확인한다.
