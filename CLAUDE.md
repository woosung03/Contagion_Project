# Contagion Project — 전염병 주식회사 클론 게임

Unity 기반 전략 시뮬레이션 게임. 앱인토스(Apps in Toss) 플랫폼 타겟.
세션을 시작할 때마다 이 파일이 자동으로 로드된다 — 아래 순서대로 참고할 것.

---

## 세션 시작 시 읽는 순서

1. 이 파일 (`CLAUDE.md`) — 프로젝트 현황 + 규칙 (가볍게 유지)
2. `Docs/GameDesignDocument.md` — 전체 게임 설계 스펙 (기획 원본)
3. `Docs/DevLog.md` — **필요할 때만.** 각 Step의 상세 구현 배경, 설계 문서와 다르게 간 이유,
   버그 진단 과정 아카이브. 특정 Step/버그를 다시 조사해야 할 때 검색해서 찾아볼 것 — 매 세션
   시작 시 통째로 읽을 필요는 없음.
4. 필요 시 `C:\Game\codebase` (참조 전용 코딩 지식 위키, 별도 저장소) — Unity UI Toolkit / 앱인토스 Unity SDK / LLM 에이전틱 패턴 문서
   - 이 위키는 **수정하지 않는다.** 읽고 패턴만 이 프로젝트에 적용한다.
   - 특히 앱인토스 연동(광고, 리더보드/랭킹, Safe Area, 빌드) 작업 시
     `wiki/apps-in-toss-unity/_overview.md`부터 확인.

---

## 프로젝트 정보

- 엔진: Unity 6000.3.10f1 (URP 템플릿)
- 렌더 파이프라인: URP (프로젝트 기본 템플릿 그대로 사용)
- 코드 네임스페이스: `Contagion.Data`, `Contagion.Managers`, `Contagion.Gameplay`, `Contagion.UI`, `Contagion.Ads`, `Contagion.Ranking`, `Contagion.Utils`
- 스크립트 위치: `Assets/Scripts/{Data, Managers, Gameplay, UI, Ads, Ranking, Utils}`
- UI 에셋 위치: `Assets/UI/*.uxml`, `Assets/UI/*.uss` (UI Toolkit)
- **타겟 화면**: 세로(Portrait) 고정, 갤럭시 S25 울트라 기준(1440×3120, 19.5:9). 새 UI 화면/레이아웃
  작업 시 가로 모드는 고려하지 않아도 됨(회전 잠김).
- 국가 48개(`Assets/New Folder/CountryDatabase.asset`), 병원체 6종, 업그레이드 트리 45노드
  (`DefaultUpgradeTreeFactory.cs`). 인구는 **실제 인구 수 그대로**(스케일 없음) 사용.

---

## 현재 구현된 시스템

기능 단위 요약만 남긴다. Step별 구현 배경·설계 결정 이유·버그 진단 과정은 전부 `Docs/DevLog.md`에
있음(Step 번호로 검색).

- 핵심 데이터/시뮬레이션 — `Pathogen`/`Country`/`WorldState`, 틱 기반 감염·사망·치료제 진행
  (`SimulationManager`), `WorldDataManager`, `GameManager`(페이즈/난이도/일시정지)
- 세계 지도 — `WorldMap`+`CountryView`(국가별 실제 실루엣, 좌우 드래그 스크롤), 국가현황 리스트 패널,
  DNA 버블 스포너(오브젝트 풀링)
- 감염 점 오버레이 — 국가별 개별 점(면적 비례 크기/개수), `dotsEnabled=true`/`hotspotsEnabled=false`
  (핫스팟 방식은 폐기, 코드는 보존)
- 글로벌 교통망 — 항공/해운 허브 46개(28개국 보유), 유닛이 지도 위를 이동하며 도착 시 감염 전파,
  해상 항로는 육지 마스크 기반 A* 길찾기로 계산
- 교통 유닛 그래픽 — 이모지 기반(✈️ / `Ship 1.png`), carrier(감염)는 색상 윤곽선으로 구분
- 게임플레이 시스템 — `UpgradeManager`(DNA 트리), `HumanResistanceManager`(인류 저항 AI),
  `EventManager`(뉴스피드 이벤트)
- UI Toolkit 전체 — HUD/업그레이드 트리/국가 상태 패널/뉴스피드/엔딩/랭킹, 공통 `Theme.uss` 디자인 토큰
- 데이터화 — ScriptableObject(`CountryDatabase`/`PathogenDefinition`/`UpgradeTreeDatabase`) +
  `GameDataBootstrapper`가 씬 시작 시 주입
- 플랫폼 연동 — 앱인토스 보상형 광고(`GameAds`), 랭킹(게임센터 리더보드), 저장(로컬 폴백 + AIT Storage 훅)
- 화면 플로우 — `MainMenu`(병원체 선택)/`CountrySelect`(발원 국가 선택, 국기 48/48)/`GamePlay`,
  재시작 루프 안정화
- 모바일 타겟팅 — 세로 화면 고정, SafeArea 적용, 국가 지리적 재배치(경도/위도 기반)

---

## 최근 작업 (Step 57~61)

| Step | 내용 | 파일 |
|------|------|------|
| 57 | 상세패널 오버플로우 수정 | MainMenu.uss |
| 58 | HUD 3줄 레이아웃 개편 | Hud.uxml, Hud.uss |
| 59 | 누락 국기 30개 추가 | Flags/*.png |
| 60 | 핵심 공백 지역 허브 16개 추가 | DefaultTransportHubFactory.cs |
| 61 | 국가현황 패널 렉 수정(행 캐싱, 전체재생성 제거) + Dock에 상태/항공·항구 추가 | CountryStatusPanelController.cs, CountryDockController.cs, Hud.uxml/uss |

---

## 현재 TODO

**다음 세션 시작 시 확인**

- GamePlay.unity 정상 로드 + MainMenu→CountrySelect→게임 시작 플로우 확인 (근거: DevLog Step 56)
- "시작 버튼 즉시 패배" 재현 여부 확인 — 재현 시 `[FLOW][GameDataBootstrapper] BeginGame`/
  `SeedStartingInfection` 로그로 `startingCountryId` 전달 확인 (근거: DevLog Step 54)
- CountrySelect/MainMenu 상세 패널 텍스트 3줄이 안 잘리는지, 리스트 스크롤 확인 (근거: DevLog Step 57)
- HUD 3줄 레이아웃에서 큰 숫자·위협 단계 텍스트 표시 시 옆 요소 안 밀리는지 확인 (근거: DevLog Step 58)
- CountrySelect 48개국 국기 전부 표시, 콘솔 경고 없는지 확인 (근거: DevLog Step 59)
- 신규 허브 16개 위치, 신규 항로 15개 육지 관통 여부, 신규 국가 교통 유닛 동작 확인 (근거: DevLog Step 60)
- 국가현황 패널 연 상태로 감염 확산 중 프레임 드랍 해소 확인(프로파일러로 틱당 UI 재빌드 없는지),
  Country Dock 신규 행(상태/항공·항구) 줄바꿈·색상 충돌 없는지 확인 (근거: DevLog Step 61)

**감염 점 오버레이 확인** (`dotsEnabled=true`, `hotspotsEnabled=false`)

- 감염률 비례 표시 여부, 국가 색상 오버레이에 가리지 않는지
- 러시아·캐나다 등 대형 국가에서 점 뭉개짐 여부 → `infectionDotDiameterScale` 조정
- 점 코어 또렷함 vs 다른 요소 침범 여부 → `coreFraction` 조정

**그 외 실플레이 확인**

- DNA 버블이 소형 국가 실루엣 안에서 스폰되는지
- 교통 노선/carrier 색상 대비가 과하지 않은지
- `Ship 1.png` 슬라이스/윤곽선 스케일 정상 로드 확인
- 이동 방향 회전값(비행기 45°/배 180°)·윤곽선 두께 확인
- 인구 스케일 변경 후 치료제/이벤트 타이밍 체감 확인
- `carrierChanceScale`(기본 25) 타이밍 적절성 확인

**배포 전**

- Twemoji 기반 그래픽(비행기/배 이모지 + 윤곽선 파생물) 저작자 표시 검토

**씬/에셋 배선 필요**

- `MainMenu`/`CountrySelect`/`GamePlay` 씬 분리 (현재 씬 하나에서 UIDocument 패널 on/off로 대체 중)
- `DnaBubble` 프리팹 제작 후 `BubbleSpawner.bubblePrefab`에 연결
- 앱인토스 SDK 설치 — pnpm lockfile 충돌로 보류 중, 재시도 시 `pnpm install --no-frozen-lockfile` 수동 실행 필요
- `AudioManager` 오브젝트 생성 + 효과음 에셋 준비/연결
- (선택) MainMenu DNA+5 보상형 광고 버튼 — `GameAds.Rewarded` 재사용

**기타**

- 앱인토스 SDK 연동 착수 전 Unity 6.x 공식 지원 여부 재확인 필요

---

## 튜닝 포인트 (참고용 — `GameDesign.md` 생성 시 이관 예정)

- `climateModifier` = `Pathogen.GetEnvironmentResistance(climate)`
- `healthcareCapacity` = `Country.HealthLevel` (Low 0.2 / Mid 0.5 / High 0.8)
- `researchMultiplier` = `Country.ResearchMultiplier` (Low 0.2 / Mid 0.8 / High 1.5)
- `severityFactor` = `Pathogen.severity`
- `drugResistanceReduction` = `pathogen.drugResistance × drugResistanceCoefficient` (SimulationManager)
- `spreadFactor`/`landBorderSpreadChance`/DNA 마일스톤 간격/저항 단계 임계값 — SimulationManager·
  HumanResistanceManager 인스펙터 값, 플레이테스트로 조정
- 항공/해운 전파는 `TransportManager` 인스펙터 값 사용 (`SimulationManager`의
  `airRouteSpreadChance`/`seaRouteSpreadChance`는 미사용 필드)
- `Cure Progress Coefficient`(기본 0.002) — 국가 48개 기준 재조정 필요 시 여기부터

---

## 프로젝트 규칙

- 이 프로젝트는 개발 대상이다 (참조 전용인 `C:\Game\codebase`와 다름). 자유롭게 수정한다.
- **CLAUDE.md 목적**: 새 세션 시작 시 "지금 무엇이 되어 있고 무엇을 해야 하는가"를 빠르게 파악하는
  핵심 브리핑 문서. 작업 이력 저장소가 아니다(이력은 `Docs/DevLog.md`).
- **새 항목 추가 판단 기준**: CLAUDE.md에 뭔가 추가하기 전에 "새 세션 시작 시 반드시 알아야 하는
  정보인가?"를 먼저 판단한다. 아니라면 이 파일에 넣지 않고 `Docs/DevLog.md`(이력/버그/시행착오/설계
  이유) 또는 향후 생성될 `UI_Design.md`/`GameDesign.md`(원칙/밸런스 설계)에 기록한다.
- **넣어도 되는 것**: 현재 구현된 기능 목록(기능 단위, Step 번호 불필요), 최근 작업 최대 5개 요약,
  지금 실행해야 할 TODO, 프로젝트 전역 규칙.
- **넣으면 안 되는 것**: Step별 구현 과정 서술, 버그 원인 분석, 수치 조정 히스토리, 설계 논증,
  이미 해결된 항목의 흔적.
- **최근 작업 관리**: 표는 항상 5개 이하로 유지한다. 6번째 Step이 추가되면 가장 오래된 항목은
  삭제한다(상세 이력은 이미 DevLog에 있으므로 압축 이동할 필요 없음).
- **TODO 관리**: 항목은 명령문 1줄("확인할 것: X")로 쓴다. 배경 설명이 필요하면
  "(근거: DevLog Step N)"만 남기고 본문은 DevLog에 적는다. 해결이 확인된 항목은 즉시 삭제한다
  (보류하거나 취소선만 긋지 않는다).
- **완료된 시스템 목록 관리**: 기능명만 나열한다(Step 번호·서술 문장 금지). 새 시스템이 생기면
  이 목록에 한 줄만 더한다.
- **DevLog.md 사용**: 모든 "왜/어떻게/무엇이 잘못됐었는지"는 DevLog.md에 적는다. 세션 시작 시
  통째로 로드하지 않고, 특정 Step/버그를 재조사할 때만 검색해서 연다.
- **Git 우선 원칙**: 작업 이력의 원본은 Git이다. CLAUDE.md는 현재 상태 설명 문서일 뿐 이력
  보관소가 아니다. 오래된 구현 내역은 Git 커밋 히스토리와 DevLog.md에서 확인한다.
