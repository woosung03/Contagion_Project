# Contagion Project — 전염병 주식회사 클론 게임

Unity 기반 전략 시뮬레이션 게임. 앱인토스(Apps in Toss) 플랫폼 타겟.
세션을 시작할 때마다 이 파일이 자동으로 로드된다 — 아래 순서대로 참고할 것.

---

## 세션 시작 시 읽는 순서

1. 이 파일 (`CLAUDE.md`) — 프로젝트 현황 + 규칙
2. `Docs/GameDesignDocument.md` — 전체 게임 설계 스펙 (기획 원본)
3. 필요 시 `C:\Game\codebase` (참조 전용 코딩 지식 위키, 별도 저장소) — Unity UI Toolkit / 앱인토스 Unity SDK / LLM 에이전틱 패턴 문서
   - 이 위키는 **수정하지 않는다.** 읽고 패턴만 이 프로젝트에 적용한다.
   - 특히 앱인토스 연동(광고 Step10, 리더보드/랭킹 Step11-12, Safe Area, 빌드) 작업 시
     `wiki/apps-in-toss-unity/_overview.md`부터 확인.

---

## 프로젝트 정보

- 엔진: Unity 6000.3.10f1 (URP 템플릿) — 위키의 앱인토스 권장 버전 목록(2023.3/2022.3 LTS)보다 최신.
  Step 10(광고 SDK)/11(랭킹) 착수 전 앱인토스 SDK가 Unity 6.x를 공식 지원하는지 재확인 필요.
- 렌더 파이프라인: URP (프로젝트 기본 템플릿 그대로 사용)
- 코드 네임스페이스: `Contagion.Data`, `Contagion.Managers`, `Contagion.Gameplay`, `Contagion.UI`, `Contagion.Ads`, `Contagion.Ranking`, `Contagion.Utils`
- 스크립트 위치: `Assets/Scripts/{Data, Managers, Gameplay, UI, Ads, Ranking, Utils}`
- UI 에셋 위치: `Assets/UI/*.uxml`, `Assets/UI/*.uss` (UI Toolkit)

---

## 진행 상황 (설계 문서 14절 Step 1~13 기준)

### 코드 구현 완료 — Step 1~13 전부

**단, Unity 에디터 GUI 작업(씬/프리팹/에셋 배치)은 사용자 요청으로 아직 진행 안 함 — 아래 "씬/에셋 배선 필요" 섹션 참고.**

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

부가 인프라(설계 문서에 명시된 Core Manager이지만 Step 번호가 없어 배선 목적으로 최소 구현):
- `Managers/GameManager.cs` — 페이즈(Incubation/Spread/Endgame) 판정, 난이도, 일시정지.
- `Managers/WorldDataManager.cs` — Country 리스트 + WorldState 저장소, 변경 이벤트 발행.

### Step 7 구현 메모

- 설계 문서 8절의 6개 이벤트(자연재해/정치불안/의료파업 = 병원체 유리, WHO긴급회의/국제공조/백신임상성공 = 인류 유리)를
  `EventManager`에 하드코딩된 개별 메서드로 구현. 문서에 정확한 발동 확률/쿨다운/임계값이 없어
  인스펙터에서 조정 가능한 `NewsEventSettings`(확률+쿨다운)와 이벤트별 임계값 필드로 노출.
- `SimulationManager.OnTickCompleted` 구독 → 매 틱 조건 검사 → 확률 통과 시 효과 적용 + `OnNewsEvent` 발행.
- 백신 임상 성공은 1회성(oneShot) 이벤트로 처리 — 게임당 한 번만 발동.

### Step 8 구현 메모 (UI Toolkit)

- HUD(`HudController`+`NewsFeedController`), 업그레이드 트리(`UpgradeTreeView`), 국가 팝업(`CountryPopupController`),
  엔딩(`EndingScreenController`), 랭킹 패널(`RankingPanelController`) — 총 5개 UIDocument 화면.
- 각 컨트롤러는 자기 데이터 소스(WorldDataManager/SimulationManager/EventManager 등)에 **직접** 구독한다.
  `UIManager`는 패널 간 조정(탭 클릭→업그레이드 화면 열기, 랭킹 버튼→랭킹 패널, 재시작, 부활 광고)만 담당.
- 업그레이드 트리는 좌표 기반 그래프 대신 **카테고리별 리스트**로 단순화 (설계 문서에 노드 좌표 데이터가 없음).
  실제 그래프 시각화(선 연결 등)는 후속 작업.
- 엔딩 화면 점수 계산: 설계 문서 11절(바이오하자드 점수)과 14절(Score = 바이오하자드점수 × 난이도배율 × 클리어속도보너스)이
  "클리어 속도"를 중복 정의하고 있어(11절 점수 구성에 이미 날짜 항목 포함), `EndingScreenController.ComputeFinalScore`에서
  바이오하자드점수(날짜+업그레이드효율+병원체배율) × 난이도배율만 적용하도록 정리함.

### Step 9 구현 메모 (ScriptableObject)

- `Pathogen`/`Country`/`UpgradeNode`에 `Clone()` 추가 — SO 자산은 "템플릿"이고, 실제 플레이는 항상 복제본을 사용해
  원본 자산이 오염되지 않게 함. `GameDataBootstrapper`가 씬 시작 시 SO → 복제 → 각 매니저(`SetCountries`/`SetPathogen`/`SetTree`) 주입.
- 병원체 선택(MainMenu)·발원 국가 선택(CountrySelect) 씬이 아직 없어서, 지금은 `GameDataBootstrapper` 인스펙터에
  고정 지정한 `selectedPathogen`/`startingCountryId`로 시작한다. 씬 전환이 생기면 정적 세션 설정 클래스로 교체할 것.

### Step 10~13 구현 메모 (앱인토스 SDK 연동 — 중요)

- **AIT.\* 참조는 전부 `#if UNITY_WEBGL && !UNITY_EDITOR` 안에 있다** — 즉 앱인토스 Unity SDK
  (`im.toss.apps-in-toss-unity-sdk`)가 아직 `Packages/manifest.json`에 설치되지 않아도 에디터에서는
  정상 컴파일되고, 에디터 Play에서는 전부 "비 AIT 환경 폴백" 경로로 동작한다 (로그만 찍고 실패 콜백).
  **실제 광고/리더보드/AIT Storage 동작 확인은 SDK 설치 후 WebGL 빌드 + 콘솔 QR로 실기기 검증해야 한다** (에디터 mock 로그는 검증 불인정 — 위키 error-iaa-pitfalls/error-leaderboard-pitfalls 참고).
- **Step 11 설계 변경**: 설계 문서 14절은 "Firebase Firestore 또는 Supabase"에 랭킹을 저장한다고 했지만,
  앱인토스가 게임 카테고리 미니앱에 게임센터 리더보드(점수 제출 + WebView)를 기본 제공하므로 별도 백엔드 없이
  `Ranking/TossLeaderboard.cs`(위키 드롭인)로 대체했다. 대신 **글로벌 Top N 조회 API가 없어서** Step 12 랭킹 UI는
  "전체/병원체별/주간 랭킹 리스트"가 아니라 **로컬 개인 최고 기록 + 토스 리더보드 WebView 열기 버튼**으로 축소됨.
  세분화된 자체 랭킹 리스트가 정말 필요해지면 그때 Firebase/Supabase를 붙이고 `IRankingService` 구현을 교체하면 된다.
- Step 10 광고: 설계 문서 1절 표는 "배너+보상형"이라 했지만 13절 본문은 "보상형 광고만 사용"이라고 명시 —
  더 구체적인 13절을 따라 배너 광고는 구현하지 않음 (`GameAds.Rewarded` 인스턴스 하나만 존재, 업그레이드
  보너스/부활/메인메뉴 보너스 3곳이 공유 — 위키 함정 #3 "같은 adGroupId당 전역 인스턴스 1개" 준수).
  메인메뉴 DNA+5 보너스는 MainMenu UI가 아직 없어 버튼에 연결만 안 된 상태 (`GameAds.Rewarded` 재사용하면 됨).
- Step 13 저장: 에디터/비 AIT 빌드는 `Application.persistentDataPath`에 JSON 파일로 저장(즉시 동작),
  AIT 빌드는 `AIT.StorageSetItem/GetItem`을 시도하고 실패 시 로컬로 폴백. 5일(틱)마다 자동 저장 +
  `OnApplicationPause`/`OnApplicationQuit`에도 저장. userHashKey 기반 유저별 분리는 아직 미적용(단일 슬롯) —
  필요해지면 `SaveKey`에 `AIT.GetUserKeyForGame()` 접두사를 붙일 것.
- **콘솔/외부 설정 필요** (코드로 불가능, 사람이 해야 함): 앱인토스 콘솔에서 미니앱을 **게임 카테고리**로 등록
  (아니면 게임센터 API 전부 `40000` 에러), 수익화 메뉴에서 광고 그룹 생성 후 실 `adGroupId` 발급,
  `Packages/manifest.json`에 AIT SDK git URL 등록.

### 플레이테스트 중 발견/수정한 버그 (2026-07-03)

- **정수 캐스팅 정지 버그**: `SimulationManager.RunTick()`에서 `newDeaths`/`newInfected`를 `(long)` 캐스팅으로
  계산했더니, 인구가 작아져서 "감염자 수 × 비율" 값이 1.0 미만이 되면 매 틱 0으로 잘려 감염자/사망자 수가
  특정 값(테스트 중 500명)에서 영구 정지하는 버그 발견. `StochasticRound()`(확률적 반올림)로 교체해 해결 —
  값이 1 미만이어도 그 비율만큼의 확률로 최소 1은 계속 진행된다.
- **국가 간 전파가 전혀 안 일어남**: 버그가 아니라 데이터 누락이었다 — `CountryDatabase`의 각 국가 항목에
  `neighborCountryIds`/`airRouteCountryIds`/`seaRouteCountryIds`를 채워야 `SimulationManager.SpreadBetweenCountries`가
  동작한다. 국가 에셋 만들 때 이 3개 리스트도 반드시 채울 것 (양방향으로 — A→B뿐 아니라 B→A도).
- **패배 조건이 사실상 발생 불가능했던 문제**: 설계 문서 1절 "감염자 0명 + 생존자 존재 (치료제 완성 후 박멸)"을
  구현할 때, 원래는 감염자가 우연히 자연 소멸하길 기다리는 구조였는데, 감염자 수가 적을 때는 새 감염이
  재발생할 확률이 사망 확률보다 높을 수 있어(둘 다 낮은 확률 이벤트) 감염자가 1~2명 근처에서 무한정
  버티는 현상이 생겼다. `cureProgress`가 100%에 도달하는 순간 전 세계 감염자를 즉시 0으로 만드는
  `SimulationManager.EradicatePathogen()`을 추가해 "치료제 완성 → 박멸"을 확정적 트리거로 바꿔 해결.
  (즉, 감염자가 안 줄어드는 게 정상일 수 있음 — HUD의 치료제% 가 100%가 안 됐으면 아직 정상 진행 중.)
- **치료제가 시작하자마자 100%를 찍는 버그**: 4.3 공식 `Σ(healthFunding × researchMultiplier)`을 그대로 구현했더니,
  국가가 몇 개(테스트 3~5개)만 있어도 국가별 기여도를 그냥 더하기만 해서 첫 틱부터 cureProgress가 1.0을
  훌쩍 넘어버렸다 (예: High 개발국 1개만 있어도 healthFunding 0.2~1.0 × researchMultiplier 1.5로 틱당 0.3~1.5씩 증가).
  `SimulationManager`에 `Cure Progress Coefficient`(기본 0.002) 감쇠 계수를 추가해 `cureIncrease`에 곱하도록 수정.
  이 값을 낮출수록 치료제 완성까지 걸리는 일수가 길어진다 — 원하는 게임 길이(예: 200~500일)에 맞춰
  플레이테스트로 다시 조정할 것. 국가 수가 늘어나면(실제 서비스용 전체 국가) cureIncrease 합계 자체가 커지므로
  이 계수도 같이 재조정 필요.

### Step 14 구현 메모 (기본 플레이 퀄리티 개선, 2026-07-03 추가)

플레이테스트 완료 후 "모바일 플레이 기준으로 기본 플레이 퀄리티를 높이자"는 요청으로 코드만으로 가능한
개선을 진행. 나무위키 백로그(Docs/PlagueIncReference.md) 중 2개(국가별 개별 붕괴 단계, 난이도별 확산
보정)와 이벤트 1개(처형/폭격)를 반영하고, 피드백/손맛 관련 항목 3개를 추가했다.

| 항목 | 파일 | 내용 |
|------|------|------|
| 국가별 개별 붕괴 단계 | `Data/Enums.cs`(`CountryCollapseStage`), `Data/Country.cs`(`GetCollapseStage()`), `Managers/HumanResistanceManager.cs` | 사망률 20/50/70/95/100% 임계값으로 국가별 상태 판정. 무질서(50%+)/무정부근접(70%+)은 `healthFunding`을 추가로 감쇠(0.6배/0.25배), 완전 무정부(95%+)는 연구 완전 중단 + 감염자가 없어도 매 틱 소량 치안붕괴 사망 발생 |
| 난이도별 확산 보정 | `Managers/GameManager.cs`(`GetDifficultySpreadMultiplier()`), `Managers/SimulationManager.cs` | 기존엔 난이도가 치료 속도만 바꿨음. Casual 1.3배/Normal 1.0/Brutal 0.8/MegaBrutal 0.6배로 `newInfected` 계산에 곱해 고난이도에서 신중한 플레이가 의미 있어지도록 함 |
| 처형/폭격 이벤트 | `Managers/EventManager.cs`(`ApplyExecutionOrBombing`) | 아직 감염 초기(국가 감염비율 5% 이하)인 국가를 노려 감염자 30~100% 삭감 + 국경/공항/항구 강제 봉쇄. 기존 6개 이벤트와 동일한 `TryTrigger` 패턴 재사용 |
| 국가 색상 부드러운 전환 | `Gameplay/CountryView.cs` | 기존엔 매 틱 색이 즉시 바뀜(뚝뚝 끊김). `UpdateVisual()`은 목표색만 갱신하고 `Update()`에서 지수감쇠 Lerp로 부드럽게 전환 |
| DNA 버블 피드백 | `Gameplay/DnaBubble.cs`, `Gameplay/FloatingTextEffect.cs`(신규) | 스폰 시 스케일 팝인 애니메이션(ease-out) 추가. 수집 시 "+N" 텍스트가 위로 떠오르며 페이드아웃 — Unity 내장 폰트(`LegacyRuntime.ttf`/`Arial.ttf`)로 런타임에 동적 생성해서 별도 에셋/프리팹 불필요 |
| AudioManager 인프라 | `Managers/AudioManager.cs`(신규) | DNA 수집/마일스톤/뉴스 이벤트(긍정·부정)/승리/패배 시점에 훅이 걸린 사운드 매니저. `DnaBubble`에 정적 이벤트 `OnAnyCollected` 추가해 인스턴스 참조 없이 구독 가능. **AudioClip 필드는 전부 비어있음 — 실제 효과음 파일은 사용자가 직접 준비/임포트해야 함(코드로 생성 불가). 비어있어도 에러 없이 조용히 무시됨** |

추가로 위 6개 기능이 실제로 실행되는지 콘솔에서 바로 확인할 수 있도록 각 지점(국가별 붕괴 단계 변경,
국경 봉쇄, 뉴스 이벤트 발동, 국가 색상 목표값 갱신, DNA 버블 스폰/수집/만료, 난이도 배율, AudioManager
재생 시도)에 `Debug.Log`/`Debug.LogWarning`을 추가했다 — 특히 `FloatingTextEffect`는 URP 프로젝트에서
레거시 Font 셰이더가 렌더링 안 될 수 있다는 점까지 로그에 남기도록 해서, 화면에 안 보이면 콘솔 로그로
"코드는 도는데 렌더링만 안 되는지" vs "애초에 실행이 안 되는지"를 구분할 수 있게 했다.

이 중 **AudioManager만 새 씬 오브젝트가 필요**하다 (아래 "씬/에셋 배선 필요" 참고). 나머지는 전부 기존에
이미 씬에 배치된 컴포넌트(SimulationManager/GameManager/HumanResistanceManager/EventManager/
CountryView/DnaBubble)의 코드만 확장한 것이라 추가 배선 없이 바로 적용된다 — 새로 추가된 인스펙터
필드는 기본값으로 자동 채워지며, 밸런싱하고 싶으면 그때 조정하면 된다.

### 씬/에셋 배선 필요 (코드만으로는 안 되는 작업 — 다음에 진행할 부분)

- `MainMenu` / `CountrySelect` / `GamePlay` 씬 생성 (현재 기본 씬만 존재)
- `Bootstrap` 오브젝트에 `GameManager`, `WorldDataManager`, `SimulationManager`, `UpgradeManager`,
  `HumanResistanceManager`, `EventManager`, `GameDataBootstrapper`, `SaveManager`, `RankingManager` 부착
- UI용 오브젝트에 `UIDocument` + 각 컨트롤러(`HudController`+`NewsFeedController` 같은 GameObject,
  `UpgradeTreeView`, `CountryPopupController`, `EndingScreenController`, `RankingPanelController` 각각) 부착 후
  UIDocument의 Source Asset에 대응 `.uxml` 연결, `UIManager`에 5개 컨트롤러 참조 연결
- `WorldMap` 오브젝트 + 국가별 `CountryView`(스프라이트, BoxCollider2D) 배치, 각 `countryId` 인스펙터 입력
- `DnaBubble` 프리팹 제작 (SpriteRenderer + CircleCollider2D) 후 `BubbleSpawner.bubblePrefab`에 연결
- `CountryDatabase`/`PathogenDefinition`/`UpgradeTreeDatabase` 에셋 생성 (우클릭 > Create > Contagion > ...) 후
  실제 국가/병원체/트리 데이터 입력, `GameDataBootstrapper`에 연결
- 앱인토스 SDK 설치(`Packages/manifest.json`) + 콘솔에서 게임 카테고리 등록 + 광고 그룹 생성
- (Step 14) `AudioManager` 오브젝트 생성 + 스크립트 부착 + 효과음 에셋 준비 후 연결 (아래 순서 참고)

---

## 나무위키 참고 자료 (원본 게임 대비 보완 아이디어, 2026-07-03 추가)

동아리 부장님이 아닌 사용자가 직접 원본 Plague Inc. 나무위키 문서(시스템/전략/이벤트/상태)를 링크로
제공해 현재 구현과 비교 분석했다. 상세 내용은 `Docs/PlagueIncReference.md` 참고.

**Step 14에서 반영 완료** (위 "Step 14 구현 메모" 참고):
1. 국가별 사망률 기반 개별 붕괴 단계 (`Country.GetCollapseStage()` + `HumanResistanceManager`)
2. 난이도별 전염성 보정 (`GameManager.GetDifficultySpreadMultiplier()`)
3. 처형/폭격 이벤트 (`EventManager.ApplyExecutionOrBombing()`)

**아직 반영 안 함** (우선순위 낮음, `Docs/PlagueIncReference.md` 참고): 세계 상태 텍스트 세분화, 국가별
치료 자금 상한선, 국경 폐쇄 우선순위(공항>국경>항구 순차 폐쇄), 올림픽 등 플레이버 이벤트.

---

## 설계 문서 대비 구현 시 내린 결정 (다음 세션이 알아야 할 것)

설계 문서 4절 공식에는 `spreadFactor`, `climateModifier`, `severityFactor`, `researchMultiplier`,
`drugResistanceReduction` 등 정의되지 않은 계수가 여러 개 있다. 아래처럼 합리적으로 유도/치환했다 —
밸런싱 필요 시 여기부터 조정:

- `climateModifier` = `Pathogen.GetEnvironmentResistance(country.climate)` (0~1, 미설정 시 기본 1=중립)
- `countryHealthLevel` / `healthcareCapacity` = `Country.HealthLevel` (developmentLevel → Low 0.2 / Mid 0.5 / High 0.8)
- `researchMultiplier` = `Country.ResearchMultiplier` (Low 0.2 / Mid 0.8 / High 1.5)
- `severityFactor` = `Pathogen.severity` 그대로 사용
- `drugResistanceReduction` = `pathogen.drugResistance * drugResistanceCoefficient`(SimulationManager 인스펙터 값)
- `spreadFactor`, 국가 간 전파 확률, DNA 마일스톤 간격, 저항 단계별 봉쇄 임계값은 모두
  `SimulationManager` / `HumanResistanceManager` 인스펙터에 노출된 튜닝 값 — 플레이테스트로 조정할 것.
- 원 설계 문서는 `environmentResistance`를 `float[4]` (Cold/Hot/Humid/Arid 고정 순서)로 정의했으나
  `Country.climate`(Arid/Temperate/Cold/Humid)와 순서가 어긋나 버그 유발 위험이 있어
  `List<ClimateResistanceEntry>` (climate → resistance 매핑)로 변경했다.

---

## 규칙

- 이 프로젝트는 개발 대상이다 (참조 전용인 `C:\Game\codebase`와 다름). 자유롭게 수정한다.
- 새 Step을 진행할 때마다 위 "진행 상황" 표를 갱신해서 다음 세션이 이어서 작업할 수 있게 한다.
