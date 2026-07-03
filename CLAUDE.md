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

- 엔진: Unity 6000.3.10f1 (URP 템플릿) — 위키의 앱인토스 권장 버전 목록(2023.3/2022.3 LTS)보다 최신.
  앱인토스 SDK 연동 착수 전 Unity 6.x 공식 지원 여부 재확인 필요.
- 렌더 파이프라인: URP (프로젝트 기본 템플릿 그대로 사용)
- 코드 네임스페이스: `Contagion.Data`, `Contagion.Managers`, `Contagion.Gameplay`, `Contagion.UI`, `Contagion.Ads`, `Contagion.Ranking`, `Contagion.Utils`
- 스크립트 위치: `Assets/Scripts/{Data, Managers, Gameplay, UI, Ads, Ranking, Utils}`
- UI 에셋 위치: `Assets/UI/*.uxml`, `Assets/UI/*.uss` (UI Toolkit)
- **타겟 화면**: 세로(Portrait) 고정, 갤럭시 S25 울트라 기준(1440×3120, 19.5:9). Step 24에서 확정 —
  `PanelSettings.referenceResolution`/`ProjectSettings.defaultScreenOrientation`/`GamePlay.unity`의
  Main Camera `orthographic size`(3.8)와 세계 지도 국가 배치가 전부 이 비율 기준으로 맞춰져 있음.
  새 UI 화면/레이아웃 작업 시 가로 모드는 고려하지 않아도 됨(회전 잠김).

---

## 진행 상황 (Step 1~22 전부 완료)

**단, Unity 에디터 GUI 작업(씬/프리팹/에셋 배치) 중 일부는 아직 미완 — 아래 "씬/에셋 배선 필요" 참고.**
각 Step의 상세 배경·설계 결정 이유·버그 진단 과정은 전부 `Docs/DevLog.md`에 있음 (표는 요약만).

| Step | 내용 | 파일 |
|------|------|------|
| 1 | 데이터 클래스 | `Data/Enums.cs`, `Data/Pathogen.cs`, `Data/Country.cs`, `Data/WorldState.cs`, `Data/UpgradeNode.cs` |
| 2 | SimulationManager (틱 기반 감염/사망/치료제) | `Managers/SimulationManager.cs` |
| 3 | WorldMap (국가 바인딩/색상/클릭) | `Gameplay/WorldMap.cs`, `Gameplay/CountryView.cs` |
| 4 | UpgradeManager (DNA/트리) | `Managers/UpgradeManager.cs` |
| 5 | BubbleSpawner (DNA 수집, 오브젝트 풀링) | `Gameplay/BubbleSpawner.cs`, `Gameplay/DnaBubble.cs`, `Utils/ObjectPool.cs` |
| 6 | 인류 저항 AI | `Managers/HumanResistanceManager.cs` |
| 7 | EventManager (뉴스 피드 + 6종 이벤트) | `Managers/EventManager.cs` |
| 8 | UI 전체 연결 (HUD/업그레이드/국가팝업/뉴스피드/엔딩) | `UI/*.cs` + `Assets/UI/*.uxml,*.uss` + `Managers/UIManager.cs` |
| 9 | ScriptableObject 데이터화 | `Data/PathogenDefinition.cs`, `Data/CountryDatabase.cs`, `Data/UpgradeTreeDatabase.cs`, `Managers/GameDataBootstrapper.cs` |
| 10 | 보상형 광고 SDK 연동 | `Ads/TossFullScreenAd.cs`, `Ads/GameAds.cs` |
| 11 | 랭킹 연동 (게임센터로 대체) | `Ranking/*.cs`, `Managers/RankingManager.cs` |
| 12 | 랭킹 UI (로컬 PB + WebView로 축소) | `UI/RankingPanelController.cs` + `Assets/UI/RankingPanel.*` |
| 13 | 저장 시스템 (로컬 폴백 + AIT Storage 훅) | `Managers/SaveManager.cs` |
| 14 | 국가별 붕괴단계, 난이도별 확산보정, 처형/폭격 이벤트, 국가색상 부드러운 전환, DNA버블 연출, AudioManager 인프라 | `Enums.cs`, `Country.cs`, `HumanResistanceManager.cs`, `GameManager.cs`, `EventManager.cs`, `CountryView.cs`, `DnaBubble.cs`, `FloatingTextEffect.cs`(신규), `AudioManager.cs`(신규) |
| 15 | 업그레이드 트리 27노드 세분화 + 좌표/연결선 시각화 + 비용 가산율 | `DefaultUpgradeTreeFactory.cs`(신규), `UpgradeNode.cs`, `UpgradeManager.cs`, `UpgradeTreeView.cs` |
| 16 | 나무위키 백로그 잔여 4항목: 세계 사망률 텍스트, 국가별 자금 상한선, 국경 폐쇄 순차화, 플레이버 이벤트 | `Enums.cs`, `WorldState.cs`, `HumanResistanceManager.cs`, `Country.cs`, `EventManager.cs` |
| 17 | 국가 4→18개, 병원체 1→6종 확장 + 신규 국가 CountryView 씬 배치 | `CountryDatabase.asset`, `Pathogen_*.asset`(신규 5개), `GamePlay.unity` |
| 18 | MainMenu(병원체 선택)/CountrySelect(국가 선택) 화면 신규 + 게임 시작 플로우 게이팅 | `MainMenuController.cs`, `CountrySelectController.cs`, `GameManager.cs`, `GameDataBootstrapper.cs`, `UIManager.cs`, `GamePlay.unity` |
| 19 | 게임 시작-끝-재시작 루프 안정화 (DontDestroyOnLoad 매니저 6개 `ResetForNewGame()`) | `SimulationManager.cs`, `WorldState.cs`, `WorldDataManager.cs`, `GameManager.cs`, `HumanResistanceManager.cs`, `EventManager.cs`, `SaveManager.cs` |
| 20 | 재시작 후 CountrySelect 완전 먹통 버그 수정 (Bootstrap→SceneUICoordinator 분리) | `GamePlay.unity` |
| 21 | HUD 스파크라인 그래프(감염자/사망자/치료제) + 업그레이드 트리 전파/증상/능력 3분할 창 | `HudSparkline.cs`(신규), `HudController.cs`, `UpgradeTreeView.cs`, `UIManager.cs`, `GamePlay.unity` |
| 22 | 국기 아이콘 18개국 추가 | `Resources/Flags/*.png`(신규 18개), `CountrySelectController.cs`, `MainMenu.uss` |
| 23 | 세계 지도 국가별 실제 실루엣 적용 (플레이스홀더 회색 사각형 → 실제 국가 모양) | `Resources/CountryShapes/*.png`(신규 18개), `Gameplay/CountryView.cs` |
| 24 | 모바일 세로 화면(갤럭시 S25 울트라, 1440×3120, 19.5:9) 타겟팅 — 화면 회전 잠금, PanelSettings/Camera 세로 재조정, 지도 재배치 | `ProjectSettings/ProjectSettings.asset`, `Assets/UI Toolkit/PanelSettings.asset`, `Assets/Scenes/GamePlay.unity` |

부가 인프라(설계 문서에 명시된 Core Manager이지만 Step 번호가 없어 배선 목적으로 최소 구현):
- `Managers/GameManager.cs` — 페이즈(Incubation/Spread/Endgame) 판정, 난이도, 일시정지.
- `Managers/WorldDataManager.cs` — Country 리스트 + WorldState 저장소, 변경 이벤트 발행.
- Step 13 완료 직후 초기 플레이테스트에서 버그 4건 발견/수정(정수 캐스팅 정지, 국가 간 전파 데이터
  누락, 패배 조건 무한 대기, 치료제 조기 100%) — 상세는 `Docs/DevLog.md` "초기 플레이테스트 버그" 참고.

### 씬/에셋 배선 필요 (코드만으로는 안 되는 작업 — 다음에 진행할 부분)

- `MainMenu` / `CountrySelect` / `GamePlay` 씬 생성 (현재 기본 씬만 존재 — 지금은 씬 하나에서 UIDocument
  패널을 켜고 끄는 방식으로 대체 중)
- `WorldMap` 오브젝트 + 국가별 `CountryView` — 18개국 전부 씬 파일 직접 편집으로 배치 완료. Step 23에서
  실제 국가 실루엣으로 교체했음(스프라이트 하드코딩이 아니라 `Resources.Load` 런타임 로드 방식이라 Unity
  에디터 GUI 조작 불필요). 지리적으로 정확한 위치 배치는 아님(Step 17 참고, 대륙별 추상 그리드) — 필요하면
  Unity 에디터에서 각 국가 오브젝트를 드래그로 재배치 가능(`CountryView`는 `countryId` 문자열로만 동작).
- `DnaBubble` 프리팹 제작 (SpriteRenderer + CircleCollider2D) 후 `BubbleSpawner.bubblePrefab`에 연결
- `CountryDatabase`/`PathogenDefinition`/`UpgradeTreeDatabase`는 이미 에셋으로 존재하고 `GameDataBootstrapper`에
  연결돼 있음 — 추가 데이터 조정만 필요하면 그때그때 텍스트 편집으로 가능
- 앱인토스 SDK 설치(`Packages/manifest.json`) + 콘솔에서 게임 카테고리 등록 + 광고 그룹 생성
  — **한 번 시도했다가 되돌림**: manifest.json에 git 의존성을 추가했더니 SDK 에디터 툴링의 pnpm install이
  `ERR_PNPM_LOCKFILE_CONFIG_MISMATCH`로 실패해 노이즈만 뿜어서 제거함. 게임 마지막 단계로 미루기로 한 상태.
  재시도 시 SDK 패키지 캐시 폴더(`Library/PackageCache/im.toss.apps-in-toss-unity-sdk@.../`)에서
  `pnpm install --no-frozen-lockfile` 수동 실행 또는 `pnpm-lock.yaml` 삭제 후 재설치 시도 (사람이 터미널에서
  실행 필요 — Node/pnpm 로컬 설치 상태도 같이 확인).
- `AudioManager` 오브젝트 생성 + 스크립트 부착 + 효과음 에셋 준비 후 연결 (에셋은 사람이 준비/임포트해야 함)
- (선택) MainMenu 화면의 DNA+5 보상형 광고 버튼 — `GameAds.Rewarded` 재사용해서 붙이기만 하면 됨, 아직 미완료

---

## 나무위키 참고 자료 (원본 게임 대비 보완 아이디어)

사용자가 원본 Plague Inc. 나무위키 문서(시스템/전략/이벤트/상태)를 근거로 제공한 백로그.
`Docs/PlagueIncReference.md`의 항목은 Step 14 + Step 16으로 **전부 반영 완료**됐다. 상세는
`Docs/DevLog.md`의 Step 14/16 항목 참고.

---

## 설계 문서 대비 구현 시 내린 결정 (요약 — 상세 근거는 Docs/DevLog.md)

설계 문서 4절 공식에는 `spreadFactor`, `climateModifier`, `severityFactor`, `researchMultiplier`,
`drugResistanceReduction` 등 정의되지 않은 계수가 여러 개 있다. 밸런싱 필요 시 여기부터 조정:

- `climateModifier` = `Pathogen.GetEnvironmentResistance(country.climate)` (0~1, 미설정 시 기본 1=중립)
- `countryHealthLevel` / `healthcareCapacity` = `Country.HealthLevel` (developmentLevel → Low 0.2 / Mid 0.5 / High 0.8)
- `researchMultiplier` = `Country.ResearchMultiplier` (Low 0.2 / Mid 0.8 / High 1.5)
- `severityFactor` = `Pathogen.severity` 그대로 사용
- `drugResistanceReduction` = `pathogen.drugResistance * drugResistanceCoefficient`(SimulationManager 인스펙터 값)
- `spreadFactor`, 국가 간 전파 확률, DNA 마일스톤 간격, 저항 단계별 봉쇄 임계값은 모두
  `SimulationManager` / `HumanResistanceManager` 인스펙터에 노출된 튜닝 값 — 플레이테스트로 조정할 것.
- `environmentResistance`는 설계 문서 원안(`float[4]` 고정 순서)이 `Country.climate` enum 순서와 어긋나는
  버그 유발 위험이 있어 `List<ClimateResistanceEntry>` (climate → resistance 매핑)로 변경했다.
- 국가 수 확장(4→18개, Step 17)으로 `SimulationManager`의 `Cure Progress Coefficient`(기본 0.002)가
  다시 커 보일 수 있음 — 치료제가 너무 빨리 100%를 찍으면 이 값부터 낮출 것.

---

## 규칙

- 이 프로젝트는 개발 대상이다 (참조 전용인 `C:\Game\codebase`와 다름). 자유롭게 수정한다.
- 새 Step을 진행할 때마다 위 "진행 상황" 표에 **한 줄 요약만** 추가한다. 상세 구현 배경·버그 진단
  과정·설계 결정 이유는 `Docs/DevLog.md`에 적어서 이 파일이 계속 가볍게 유지되도록 한다.
