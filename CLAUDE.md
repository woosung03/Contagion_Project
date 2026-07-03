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

**Step 14 플레이테스트 중 발견한 버그**: `HumanResistanceManager`의 완전 무정부 치안붕괴 사망 로직이
`country.deadCount`만 증가시키고 `infectedCount`는 그대로 둬서, `LivingPopulation`(=population-deadCount)이
줄어드는데 감염자 수는 안 줄어들어 감염률이 100%를 초과(로그에서 606% 확인됨)하는 버그가 있었다.
치안붕괴로 죽은 사람 중 일부는 원래 감염자였다고 보는 게 합리적이므로, `unrestDeaths` 반영 직후
`country.infectedCount = Math.Min(country.infectedCount, country.LivingPopulation)`으로 클램프해서 해결.

### Step 15 구현 메모 (업그레이드 시스템 세분화, 2026-07-03 추가)

"업그레이드 시스템 세분화" 요청에 AskUserQuestion으로 방향을 확인한 결과 (1) 노드 개수/내용 확장,
(2) 트리 좌표/연결선 시각화, (3) 비용/밸런스 세분화 3가지를 선택함 (노드별 효과 타입 다양화는 선택 안 함
— 기존 4개 stat(infectivity/severity/lethality/drugResistance) 범위 내에서만 세분화).

| 항목 | 파일 | 내용 |
|------|------|------|
| 27노드 세분화 트리 | `Data/DefaultUpgradeTreeFactory.cs`(신규) | 감염경로/증상/능력 각 9개(3티어+최종노드)를 선행조건 체인+좌표(position)까지 포함해 코드로 정의. `GameDataBootstrapper`가 `upgradeTreeDatabase` 에셋 미지정 시 이 폴백을 사용하므로 **ScriptableObject 에셋을 만들지 않아도 바로 27노드로 플레이 가능** |
| 노드 좌표 필드 | `Data/UpgradeNode.cs` | `Vector2 position` 추가 (`Clone()`에도 반영) — 트리 시각화에 사용 |
| 비용 세분화 | `Managers/UpgradeManager.cs`(`GetEffectiveCost()`) | 같은 카테고리에서 이미 해금한 노드 수만큼 비용에 가산율(기본 0.15)을 곱함 — 나무위키 "진화 시 다음 특성 비용 증가" 반영. 한 카테고리만 몰아 찍기보다 골고루 찍도록 유도. `CanUnlock`/`TryUnlock`/UI 전부 이 실비용 기준으로 통일 |
| 트리 좌표/연결선 시각화 | `UI/UpgradeTreeView.cs`, `Assets/UI/UpgradeTree.uss` | 카테고리별 리스트 렌더링을 버리고 `node.position` 절대 좌표 배치로 교체. 선행조건 연결선은 UI Toolkit `Painter2D`(`generateVisualContent`)로 직접 그림 — 선행 노드가 해금됐으면 초록 굵은 선, 아니면 흐린 회색 선 |

**전부 코드만으로 완료 — 새 씬/에셋 배선 필요 없음**: `UpgradeTreeDatabase` 에셋도, 프리팹도 새로 만들
필요가 없다. 기존에 이미 씬에 있는 `GameDataBootstrapper`/`UpgradeManager`/`UpgradeTreeView`(UIDocument)가
그대로 27노드 세분화 트리 + 좌표 배치 + 연결선을 사용한다. 나중에 실제 `UpgradeTreeDatabase` 에셋을 만들어
연결하면 그쪽이 우선 사용되고, 폴백은 자동으로 무시된다.

**Step 15 플레이테스트 중 발견한 버그 2가지**:
- **트리가 아예 안 보임**: 사용자가 예전에 `GameDataBootstrapper`의 `Upgrade Tree Database` 슬롯에
  빈 `UpgradeTreeDatabase` 에셋을 연결해둔 상태였음 — 에셋이 지정되면 그쪽이 우선이라 새 폴백(27노드)이
  무시되고 빈 트리로 시작됐다. 슬롯을 비우면(None) 폴백이 정상 적용됨. **다음에 실제 트리 데이터를
  채운 에셋을 만들 게 아니라면 이 슬롯은 계속 비워둘 것.**
- **노드끼리 겹쳐 보임**: `DefaultUpgradeTreeFactory`의 카테고리별 x좌표 간격이 노드 폭(140px)보다
  좁아서(감염경로 마지막 칸과 증상 첫 칸 사이 120px) 카테고리 경계에서 겹쳤다. 카테고리 사이 간격을
  100px 이상으로 재배치해 해결 (감염경로 40~540 / 증상 640~1140 / 능력 1240~1740). 트리 캔버스가
  화면보다 넓어져서 `UpgradeTreeView`의 `node-scroll`도 세로 전용에서 양방향(`VerticalAndHorizontal`)
  스크롤로 변경 — 모바일 좁은 화면에서 특히 필요.

### Step 16 구현 메모 (나무위키 백로그 잔여 4항목, 2026-07-03 추가)

`Docs/PlagueIncReference.md`에서 Step 14 때 보류했던 "급하지 않음" 4항목(세계 상태 텍스트, 국가별 자금
상한선, 국경 폐쇄 순서, 플레이버 이벤트)을 전부 반영. 이번에도 전부 코드(+UXML/USS 텍스트 편집)만으로
완료 — UXML/USS는 텍스트 파일이라 Unity 에디터에서 드래그해 붙일 새 씬 오브젝트가 필요 없다(기존
UIDocument가 이미 해당 .uxml을 Source Asset으로 참조 중이므로 요소를 추가하면 바로 인식된다).

| 항목 | 파일 | 내용 |
|------|------|------|
| 세계 사망률 위험도 텍스트 세분화 | `Data/Enums.cs`(`WorldMortalityStage`), `Data/WorldState.cs`(`GetMortalityStage()`), `Managers/HumanResistanceManager.cs`(`OnMortalityStageChanged`) | 기존 `ResistanceStage`(plagueVisibility 기준, 인류 대응 축)와 별개로 사망률(deadCount/totalPopulation) 기준 축 추가 — 0%(Stable)/0~1%(위협 시작)/1~20%(세계를 위협)/20%+(인류 멸종 임박) 4단계. 변경 시점마다 로그+이벤트 발행 |
| HUD/뉴스피드 노출 | `UI/HudController.cs`, `UI/NewsFeedController.cs`, `Assets/UI/Hud.uxml`(`world-status-label` 신규), `Assets/UI/Hud.uss`(`.stat-label--mortality`) | HUD 하단 스탯바에 사망률 위험도 텍스트 상시 표시(Stable일 땐 빈 문자열), 단계가 바뀔 때마다 뉴스피드에도 별도 항목 추가 |
| 국가별 치료 자금 상한선 | `Data/Country.cs`(`healthFundingCap`, 기본 1f) | `HumanResistanceManager.ApplyPolicy()`가 매 틱 계산한 `funding`을 이 상한선으로 최종 클램프(`Mathf.Min`). 평소엔 1(제한 없음)이라 기존 동작과 동일 |
| 자연재해 → 자금 상한선 영구 감소 | `Managers/EventManager.cs`(`ApplyNaturalDisaster`, `naturalDisasterFundingCapPenalty=0.15f`) | 나무위키 "재해로 사망자가 나면 그 나라 치료 자금 투자 한계치가 낮아진다" 반영 — 자연재해 이벤트가 뜬 국가는 `healthFundingCap`이 영구적으로 0.15씩 깎임(여러 번 맞으면 계속 낮아짐, 최저 0) |
| 국경 폐쇄 순차화 (공항>국경>항구) | `Managers/HumanResistanceManager.cs`(`ApplySequentialClosure`, `sequentialClosureMargin=0.1f`) | 기존엔 `highDevLockdownThreshold`/`midDevLockdownThreshold` 하나만 넘으면 국경/공항/항구를 동시에 닫았음. 이제 이 임계값을 "국경" 기준으로 두고, 공항은 그보다 0.1 낮은 visibility에서 먼저, 항구는 0.1 높은 visibility에서 마지막에 닫히도록 3단계로 분리. `EventManager`의 처형/폭격·국제공조 이벤트처럼 "즉시 전면 봉쇄"하는 강제 이벤트는 의도적으로 그대로 둠(그 자체가 극적인 순간용 예외 처리라 순차화 대상이 아님) |
| 올림픽 등 플레이버 이벤트 | `Managers/EventManager.cs`(`NewsEventCategory.Flavor`, `ApplyFlavorEvent()`, `flavorEventTexts[]`) | 게임 수치에 전혀 영향 없는 순수 텍스트 이벤트 4종(올림픽/월드컵/콘서트/우주정거장) — 기존 `TryTrigger` 패턴 그대로 재사용하되 조건 없이(`true`) 확률+쿨다운만으로 발동. 뉴스피드에 회색 이탤릭(`.news-entry--flavor`)으로 구분 표시 |

**전부 코드+UXML/USS 텍스트 편집만으로 완료 — 새 씬/프리팹 배선 필요 없음.** `Country.healthFundingCap`
기본값이 1f라 이번 변경으로 기존 세이브 데이터/밸런스가 깨지지 않는다(자연재해를 맞은 국가만 서서히
낮아짐). `WorldMortalityStage`/`Flavor` 이벤트도 기존 열거형에 값을 추가한 것이라 하위 호환.

### Step 17 구현 메모 (콘텐츠 확장 — 국가/병원체, 2026-07-03 추가)

"게임 퀄리티를 높이자"는 요청에 AskUserQuestion으로 방향(비주얼/연출, 콘텐츠 확장, UI/UX 폴리싱 — 사운드는
상업적으로 이용 가능한 에셋을 구해야 해서 이번엔 제외)과 순서(콘텐츠 확장 먼저, 국가 15~20개 규모)를
확인한 뒤 진행. 앱인토스 SDK/콘솔 작업(Step 미분류, "씬/에셋 배선" 7번)은 사용자 요청으로 게임 마지막
단계로 연기.

| 항목 | 파일 | 내용 |
|------|------|------|
| 국가 4개→18개 확장 | `Assets/New Folder/CountryDatabase.asset` | China/India/USA/Korea 4개 + Japan/Indonesia/Saudi Arabia/Germany/UK/France/Russia/Nigeria/Egypt/South Africa/Brazil/Mexico/Canada/Australia 14개 신규. 대륙별로 분산 배치, 기후/개발수준/인구를 현실 비례에 맞춰 조정(단 절대값은 기존 4개국과 비슷한 스케일 유지 — 실제 인구 그대로 넣으면 기존에 튜닝된 치료제 진행 계수 등이 깨짐). neighbor/air/sea route 그래프는 Python 스크립트로 대칭(양방향) 검증 후 생성 — 기존 4개국 데이터에 있던 버그 2개(CHI→IND 편도 연결, IND의 seaRoute 자기참조)도 이번에 같이 수정됨 |
| 병원체 1종→6종 확장 | `Assets/New Folder/Pathogen_{Virus,Fungus,Parasite,Prion,BioWeapon}.asset` (신규) + `Pathogen_Bacteria.asset` (보강) | 원작 Plague Inc. 6대 병원체 아키타입 반영: 바이러스(고전염력·저치사율·변이로 약물내성 확보), 곰팡이(습한 기후 특화, 포자 확산), 기생충(초기 가시성 극히 낮음, 동물/곤충/혈액 매개), 프리온(극저전염력·고치사율), 생물무기(전 기후 저항 1, 강력하지만 증상이 뚜렷해 조기 발각). 각각 `environmentResistance`(기후별 저항)와 `transmissionRoutes` 채움. 기존 Bacteria도 비어있던 두 필드 + flavorText 보강 |

**전부 코드/에셋 텍스트 편집만으로 완료.**

**추가 작업 (2026-07-03, 같은 날 이어서): 신규 국가 14개 CountryView 자동 배치.** 사용자가 "지도도
자동으로 그릴 수 있지 않나?"라고 물어 확인해본 결과, `GamePlay.unity`는 텍스트(YAML) 파일이라 Unity
에디터 없이도 직접 편집 가능하다는 걸 활용해 — Python으로 좌표 충돌 검사(기존 4개국과 최소 0.5 유닛
간격)를 거친 뒤, 14개 국가 각각의 GameObject(Transform+SpriteRenderer+BoxCollider2D+CountryView)를
씬 파일에 직접 추가하고 `WorldMap` 오브젝트의 `m_Children`에도 등록했다. 기존 4개국이 쓰던 것과
**동일한 플레이스홀더 스프라이트**(`다운로드.jpg`, guid `9e4edc0b758c08440b64493c1e2db59b`)를 재사용—
새 이미지 임포트는 Unity 에디터의 실제 텍스처 처리 과정을 거쳐야 fileID가 확정되는데, 이 세션엔
Unity 에디터도 이미지 생성 도구도 없어서 새 스프라이트를 만들 수 없었다. 결과적으로 18개국 전부
동일한 회색 사각형이 감염률에 따라 색만 바뀌는 상태 — **실제 국가 모양/지도 아트는 여전히 사람이
Unity 에디터(또는 이미지 생성 도구)로 작업해야 하는 부분**이다. 위치는 지리적으로 정확한 좌표가 아니라
대륙별로 대충 뭉쳐 배치한 추상적 그리드(카메라 orthographic size 5 기준, 기존 4개국이 원점 근처
작은 범위에만 몰려있던 걸 참고해 x:[-2.6,2.6] y:[-2.0,2.3] 범위로 확장). 실제 배치가 마음에 안 들면
Unity 에디터에서 각 국가 오브젝트를 드래그로 재배치하면 된다 — `CountryView`는 계층구조가 아니라
`countryId` 문자열로만 동작하므로 위치를 옮겨도 게임 로직엔 전혀 영향 없음.

**주의**: 씬 파일을 텍스트로 직접 편집했으므로, Unity 에디터가 이 프로젝트를 열어둔 상태였다면
다음에 에디터에서 저장할 때 이번 변경이 덮어써질 수 있다. Unity를 열기 전에 씬을 다시 로드
(리로드)하거나, 에디터가 닫혀 있는 상태에서 열어서 확인할 것.

**밸런스 주의**: 국가 수가 4→18개로 늘면서 선진국(High dev) 수도 늘어(기존 2개→JPN/GER/UK/FRA/CAN/AUS
추가로 8개) 치료제 진행 총 기여도(`cureIncrease` 합계)가 커진다. `SimulationManager`의
`Cure Progress Coefficient`(기존 0.002로 튜닝됨)를 낮춰야 할 수 있음 — 다음 플레이테스트에서 치료제가
너무 빨리 100%를 찍으면 이 값부터 조정할 것 (CLAUDE.md 기존 메모에서 이미 예견된 사항).

병원체 선택은 Step 18에서 MainMenu 화면으로 구현 완료 — 아래 참고.

### Step 18 구현 메모 (MainMenu/CountrySelect 화면 신규 제작, 2026-07-03 추가)

"UI/UX 폴리싱" 방향 중 "MainMenu/CountrySelect 화면 신규 제작"을 우선 진행. 기존 프로젝트는 별도 씬
전환 없이 단일 `GamePlay` 씬 안에서 UIDocument 패널을 켜고 끄는 구조라(HUD/업그레이드트리/국가팝업/엔딩/랭킹
전부 이 패턴), MainMenu/CountrySelect도 새 Unity 씬을 만들지 않고 같은 패턴의 UIDocument 패널 2개로
구현했다. 국기 아이콘은 사용자 요청대로 이번엔 넣지 않고 빈 슬롯만 예약해둠(아래 참고).

| 항목 | 파일 | 내용 |
|------|------|------|
| 병원체 선택 화면 | `UI/MainMenuController.cs`, `Assets/UI/MainMenu.uxml`, `Assets/UI/MainMenu.uss` | `GameDataBootstrapper.AvailablePathogens`(6종)로 카드 리스트 생성, 감염력/중증도/치사율/약물저항 4개 스탯을 텍스트 바(■/□)로 표시. 카드 선택 시 `OnPathogenConfirmed` 이벤트 발행 |
| 발원 국가 선택 화면 | `UI/CountrySelectController.cs`, `Assets/UI/CountrySelect.uxml`(스타일은 `MainMenu.uss` 재사용) | `GameDataBootstrapper.AvailableCountries`(18개)로 국가 리스트 생성. 각 행에 `country-row__flag`라는 **빈 VisualElement를 국기 아이콘 자리로 미리 예약**만 해둠 — 사용자가 "국기는 나중에 한꺼번에 처리하자"고 명시적으로 보류했기 때문에 실제 스프라이트는 아직 안 넣음. `OnCountryConfirmed`/`OnBackRequested` 이벤트 발행 |
| 게임 시작 플로우 게이팅 | `Managers/GameManager.cs`(`isPaused` 기본값 `true`), `Managers/GameDataBootstrapper.cs`(싱글톤 `Instance`, `AvailablePathogens`/`AvailableCountries`, `BeginGame(pathogen, countryId)`), `Managers/UIManager.cs` | `UIManager.Start()`에서 게임 진입 시 무조건 일시정지 + MainMenu 노출. MainMenu→(병원체 확정)→CountrySelect→(국가 확정)→`GameDataBootstrapper.BeginGame()`(병원체 세팅+감염 시드+`GameManager.SetPaused(false)`) 순서로 진행. `GameDataBootstrapper`에 `skipMainMenu` 플래그를 추가해뒀으니, 나중에 테스트용으로 메뉴 없이 바로 시작하고 싶으면 인스펙터에서 켜면 됨(현재 `0`=꺼짐, 항상 메뉴부터 시작) |
| 씬 배선 | `Assets/Scenes/GamePlay.unity` | `MainMenuUI`/`CountrySelectUI` GameObject 2개를 기존 `CountryPopupUI`와 동일한 패턴(GameObject+Transform+UIDocument+컨트롤러 MonoBehaviour)으로 씬 파일에 직접 추가하고 SceneRoots에 등록. `GameDataBootstrapper` MonoBehaviour에 `availablePathogens`(6개 병원체 에셋 참조)+`skipMainMenu: 0` 필드 추가. `UIManager` MonoBehaviour에 `mainMenuController`/`countrySelectController` 참조 연결. **에셋 생성부터 씬 배선까지 전부 텍스트 편집으로 완료 — Unity 에디터 GUI 조작 없이 끝남** (앞선 Step들과 같은 방식, Grep으로 fileID 충돌/중복 없음 검증 완료) |

**전부 코드+에셋 텍스트 편집만으로 완료.** Unity 에디터에서 열어서 재생(Play) 확인만 하면 됨 —
MainMenu(병원체 카드 6개 표시·선택) → CountrySelect(국가 18개 표시·선택, 뒤로가기 버튼으로 MainMenu
복귀 가능) → 확정 시 HUD가 뜨고 시뮬레이션이 진행되는지 확인. 확인 중 이상 있으면 콘솔 로그부터 볼 것
(각 컨트롤러의 `OnEnable`/`Show`/`Hide`에 별도 로그는 안 심어뒀음 — 필요하면 추가 요청할 것).

국기 아이콘은 여전히 빈 슬롯(`country-row__flag`) 상태 — 나중에 "비주얼/연출" 또는 국기 일괄 작업 시
스프라이트만 꽂으면 됨(코드 변경 불필요, USS에 `background-image`만 지정하거나 컨트롤러에서 스프라이트
딕셔너리 조회 추가).

### Step 19 구현 메모 (게임 시작-끝-재시작 루프 안정화, 2026-07-03 추가)

"플레이는 문제 없다"는 확인 후 "게임 시작-게임 끝-게임 재시작 구조 만들자" 요청으로, 실제로 엔딩까지
가서 재시작 버튼을 눌렀을 때 벌어질 일을 코드로 추적했다. 결론: **씬 리로드(`SceneManager.LoadScene`)만
으로는 재시작이 제대로 안 된다.** `GameManager`/`WorldDataManager`/`SimulationManager`/
`HumanResistanceManager`/`EventManager`/`SaveManager`/`UpgradeManager`/`RankingManager`/`AudioManager`
9개 매니저가 전부 `DontDestroyOnLoad`라 씬을 리로드해도 죽지 않고 이전 판의 내부 상태를 그대로 들고
살아남는다. `GameDataBootstrapper.Start()`가 국가/트리는 매번 새로 주입해서 괜찮지만, 그 외 누적
상태는 아무도 초기화하지 않고 있었다.

**가장 치명적인 버그**: `SimulationManager._gameEnded`가 한 번 `true`가 되면 절대 안 풀렸다 —
`TickLoop()`의 `if (_gameEnded) continue;`와 `EvaluateEndConditions()`의 조기 `return`이 계속 걸려서,
**재시작한 새 게임은 화면만 뜨고 시뮬레이션 틱이 영원히 0개 진행되는 상태**였다(HUD는 보이는데
아무 것도 안 움직임 — 겉으로는 "멀쩡해 보이는" 조용한 버그라 실제로 끝까지 플레이해보기 전엔
발견하기 어려움). 부활(패배 후 광고 시청 → 이어하기) 경로도 같은 플래그 때문에 동일하게 멈춰있었다.

| 매니저 | 리셋 안 하면 생기는 문제 | 추가한 메서드 |
|---|---|---|
| `SimulationManager` | `_gameEnded=true`로 재시작해도 틱이 영원히 안 돎(가장 치명적). 마일스톤 딕셔너리도 이전 판 수치가 남아 DNA 버블이 한참 안 뜸 | `ResetForNewGame()` (`_gameEnded=false` + 마일스톤 Clear), 부활 전용 경량 버전 `ClearGameEndedFlag()` |
| `WorldState`/`WorldDataManager` | `cureProgress`/`plagueVisibility`/`dnaPoints`/`currentDay`가 이전 판 값으로 시작(치료제 100%였던 판 다음 판은 몇 틱 만에 즉시 종료돼버림) | `WorldState.Reset()`, `WorldDataManager.ResetForNewGame()` |
| `GameManager` | `currentPhase`가 `Endgame`인 채로 새 게임이 시작됨 | `ResetForNewGame()` |
| `HumanResistanceManager` | `_lastStage`/`_lastCollapseStage` 등이 이전 판 값이라 게임 시작 직후 혼란스러운 역행 로그(치명적이진 않음) | `ResetForNewGame()` |
| `EventManager` | `_lastTriggeredDay`(쿨다운 기준일)가 이전 판의 큰 day 값이라 새 게임은 `currentDay - lastDay`가 한참 음수 → 사실상 모든 뉴스 이벤트가 오랫동안 안 뜸. `_firedOnce`(백신 임상 성공 등 1회성) 미초기화 시 두 번째 판부터 영원히 발동 안 함 | `ResetForNewGame()` |
| `SaveManager` | `_lastAutoSaveDay`가 이전 판 값이라 새 게임 자동 저장이 지연(치명적이진 않음) | `ResetForNewGame()` |

`GameDataBootstrapper.Start()`(씬 로드마다 항상 실행 — 첫 시작이든 재시작이든) 맨 앞에
`ResetPersistentManagersForNewGame()`을 추가해 위 6개를 전부 호출하도록 배선했다. `UpgradeManager`는
이미 매번 `SetTree()`로 새 인스턴스를 주입받아 문제 없었고, `RankingManager`/`AudioManager`는 판 사이에
남는 누적 상태가 없어 그대로 둠.

부수적으로 `UIManager.HandleRestartRequested()`의 의미 없는 `GameManager.SetPaused(false)` 선호출을
제거(리로드 직후 새 `UIManager.Start()`가 다시 멈추므로 무의미했음)하고, `HandleReviveRequested()`에
`SimulationManager.Instance?.ClearGameEndedFlag()` 호출을 추가했다(부활은 재시작이 아니라 "같은 판
이어가기"라 `ResetForNewGame()`을 쓰면 안 되고, 종료 플래그만 푸는 경량 메서드가 필요했음).

**전부 코드만으로 완료.** Unity 에디터에서 확인할 것: 플레이 → (빠르게 끝내고 싶으면 치료제
진행/난이도 조정) → 엔딩 화면 도달 → "재시작" 클릭 → MainMenu부터 다시 시작하고, 새 판에서 감염이
실제로 진행되는지(HUD 수치가 매 초 바뀌는지) 확인. 패배 엔딩에서는 "부활"(광고) 버튼도 눌러서
같은 판이 실제로 이어지는지 같이 확인.

### Step 20 구현 메모 (재시작 후 CountrySelect 완전 먹통 버그 — 진짜 근본 원인, 2026-07-03 추가)

Step 19를 코드로 완료한 뒤 사용자가 실제로 "승리 → 재시작"까지 플레이해보니, MainMenu에서 병원체를
고르고 CountrySelect까지는 뜨는데 그 화면에서 국가 목록이 하나도 안 보이고 뒤로/시작 버튼을 눌러도
아무 반응이 없는 새로운 버그가 나왔다. Step 19의 6개 매니저 리셋과는 별개의, 더 근본적인 구조적
버그였다 — 아래 진단 과정과 원인을 남긴다 (다음에 비슷한 "재시작 후 UI가 먹통" 버그가 생기면 여기부터
의심할 것).

**진단 과정**: `UIManager`/`GameDataBootstrapper`/`CountrySelectController`/`EndingScreenController`
전 구간에 `[FLOW]` 태그 + `Time.realtimeSinceStartup` + `GetInstanceID()`를 찍는 로그를 심어 재현
로그를 받아본 결과: (1) "재시작" 클릭 → `SceneManager.LoadScene(0)` 호출까지는 정상, (2)
`CountrySelectController`는 재시작 후 **진짜로 새 인스턴스**(다른 instanceId)가 생성됨 — 즉 씬
자체는 리로드되고 있었다, (3) 그런데 **`GameDataBootstrapper.Start()`와 `UIManager.Start()`
로그가 재시작 후 한 번도 다시 안 찍힘** — 이 둘은 리로드돼도 새로 초기화가 안 되고 있었다는 뜻.

**근본 원인**: `GameDataBootstrapper`와 `UIManager`가 `GameManager`/`WorldDataManager`/
`SimulationManager` 등과 **같은 "Bootstrap" GameObject에 컴포넌트로 같이 붙어있었다.**
`GameManager.Awake()`가 `DontDestroyOnLoad(gameObject)`를 호출하는데, 이 `gameObject`는 Bootstrap
오브젝트 전체를 가리킨다 — Unity의 `DontDestroyOnLoad`는 **GameObject 단위**로 동작하므로, 그 위에
같이 얹혀있던 `GameDataBootstrapper`/`UIManager`도 (자기 스크립트에는 `DontDestroyOnLoad` 호출이
전혀 없는데도) 덩달아 씬 리로드에서 살아남아 버렸다. 이 둘은 원래 "씬이 리로드될 때마다 Start()가
다시 실행되면서 모든 걸 새로 초기화한다"는 전제로 설계됐는데(Step 18/19가 전부 이 전제 위에 있음),
실제로는 최초 1회만 Start()가 실행되고 이후 재시작부터는 죽지 않고 예전 상태를 그대로 들고
있었다 — 특히 `UIManager`는 예전 `mainMenuController`/`countrySelectController` 필드가 **이미
파괴된 예전 씬의 오브젝트를 가리키는 채로 멈춰있었고**, 새로 생성된 `CountrySelectController`의
`OnBackRequested`/`OnCountryConfirmed` 이벤트에는 `UIManager`가 한 번도 재구독을 안 했으니
구독자 수 0 — 버튼을 눌러도 클릭 이벤트 자체는 뜨지만(로그로 확인됨) 아무도 듣고 있지 않아 화면이
안 바뀌는 상태였다.

**수정**: `GameDataBootstrapper`/`UIManager`를 Bootstrap에서 떼어내 **`SceneUICoordinator`라는
별도의(= `DontDestroyOnLoad` 호출이 전혀 없는) GameObject**로 옮겼다. 나머지 8개
(`GameManager`/`WorldDataManager`/`SimulationManager`/`UpgradeManager`/`HumanResistanceManager`/
`EventManager`/`SaveManager`/`RankingManager`)는 전부 자기 스크립트 안에서 직접
`DontDestroyOnLoad(gameObject)`를 호출하므로 Bootstrap에 그대로 둬도 각자 알아서 영속된다 —
**한 GameObject에 "영속돼야 하는 매니저"와 "씬마다 새로 초기화돼야 하는 매니저"를 섞어 놓으면 안
된다는 게 이번에 얻은 교훈.** 앞으로 Bootstrap에 새 매니저를 추가할 때는 그 매니저가 정말
`DontDestroyOnLoad`가 필요한지 먼저 확인할 것 — 필요 없다면 `SceneUICoordinator`(또는 새 비영속
오브젝트)에 붙여야 한다.

디버깅용으로 추가한 `[FLOW]` 로그(`UIManager`/`GameDataBootstrapper`/`CountrySelectController`/
`EndingScreenController`)는 굳이 지우지 않고 남겨뒀다 — 평소엔 Console 검색창에 `[FLOW]`를 안 치면
안 보이고, 나중에 비슷한 "재시작 후 씬 어딘가가 안 먹힘" 류 버그가 또 생기면 바로 재사용할 수 있다.

**전부 코드+씬 텍스트 편집만으로 완료.** Unity 에디터에서 Stop → Play로 완전히 새로 시작한 뒤 확인할
것: 승리(또는 패배) → 재시작 클릭 → MainMenu → 병원체 선택 → CountrySelect에서 **국가 18개 목록이
보이고 클릭해서 선택되는지, 시작/뒤로 버튼이 정상 동작하는지** 반드시 확인. 이전에는 이 경로가
완전히 먹통이었다.

### Step 21 구현 메모 (HUD 가시성 개선 — 스탯 그래프화 + 업그레이드 창 3분할, 2026-07-03 추가)

"텍스트로만 정보가 주어져서 가시성이 떨어진다"는 피드백으로 두 가지를 개선했다.

| 항목 | 파일 | 내용 |
|------|------|------|
| 감염자/사망자/치료제 스파크라인 그래프 | `UI/HudSparkline.cs`(신규), `UI/HudController.cs`, `Assets/UI/Hud.uxml`, `Assets/UI/Hud.uss` | UI Toolkit엔 내장 차트 위젯이 없어서 `UpgradeTreeView`의 연결선처럼 `Painter2D`로 직접 그리는 재사용 가능한 `HudSparkline` 클래스를 만들었다. 최근 최대 200개 샘플을 그 구간의 최솟값~최댓값으로 자동 스케일링해서 그린다(절대 수치 축은 표시 안 함 — 정확한 값은 옆 텍스트 라벨이 담당, 그래프는 추세만). `HudController.HandleWorldStateChanged`(매 틱=하루 호출)에서 `infectedCount`/`deadCount`/`cureProgress*100`을 매번 샘플로 추가 |
| 업그레이드 트리 전파/증상/능력 → 완전히 별개인 창 3개 | `UI/UpgradeTreeView.cs`, `Assets/UI/UpgradeTree.uxml`(`category-title-label` 추가), `Managers/UIManager.cs`, `Assets/Scenes/GamePlay.unity` | 기존엔 탭 버튼 3개가 전부 같은 창(27노드가 한 캔버스에 다 있어 직접 스크롤해서 찾아야 함)을 열었다. `UpgradeTreeView`에 `category` 필드를 추가해 "이 창은 이 카테고리만 담당"하도록 바꾸고, `RebuildTree()`가 `UpgradeManager.Tree`를 그 카테고리로 필터링(9개만)한 뒤 그 카테고리의 최소 x좌표를 빼서 항상 0부터 시작하도록 정규화(DefaultUpgradeTreeFactory의 절대좌표는 카테고리별로 40/640/1240부터 시작이라 안 그러면 왼쪽에 큰 빈 여백이 생김). 씬에 같은 `UpgradeTree.uxml`을 재사용하는 GameObject 2개(`SymptomTreeUI`/`AbilityTreeUI`, 기존 `UpgradeTreeUI`는 `TransmissionTreeUI`로 개명)를 추가하고, `UIManager`가 `transmissionTreeView`/`symptomTreeView`/`abilityTreeView` 3개 참조를 각각 열고 나머지 둘은 닫도록 라우팅 |

**주의**: `UpgradeTreeView.Show()`가 `UpgradeCategory? focusCategory` 파라미터를 받던 걸 파라미터 없는
`Show()`로 바꿨다(카테고리가 이제 인스펙터 고정값이라 호출 시점에 지정할 필요가 없어짐) — 혹시 다른
곳에서 `Show(category)` 형태로 호출하는 코드가 남아있으면 컴파일 에러가 나니 확인할 것(현재는
`UIManager.HandleUpgradeTabClicked`만 호출하며 이미 새 시그니처로 수정 완료).

**전부 코드+씬 텍스트 편집만으로 완료.** Unity 에디터에서 확인할 것: HUD 하단 감염자/사망자/치료제
칸에 작은 그래프가 표시되고 플레이 중 값이 바뀌면 그래프가 갱신되는지, 전파/증상/능력 탭을 각각
눌렀을 때 서로 다른(그 카테고리 9개 노드만 있는) 창이 열리는지, 다른 탭을 누르면 이전 창이 닫히는지.

### 씬/에셋 배선 필요 (코드만으로는 안 되는 작업 — 다음에 진행할 부분)

- `MainMenu` / `CountrySelect` / `GamePlay` 씬 생성 (현재 기본 씬만 존재)
- `Bootstrap` 오브젝트에 `GameManager`, `WorldDataManager`, `SimulationManager`, `UpgradeManager`,
  `HumanResistanceManager`, `EventManager`, `GameDataBootstrapper`, `SaveManager`, `RankingManager` 부착
- UI용 오브젝트에 `UIDocument` + 각 컨트롤러(`HudController`+`NewsFeedController` 같은 GameObject,
  `UpgradeTreeView`, `CountryPopupController`, `EndingScreenController`, `RankingPanelController` 각각) 부착 후
  UIDocument의 Source Asset에 대응 `.uxml` 연결, `UIManager`에 5개 컨트롤러 참조 연결
- `WorldMap` 오브젝트 + 국가별 `CountryView`(스프라이트, BoxCollider2D) 배치 — 18개국 전부 씬 파일
  직접 편집으로 배치 완료(위 Step 17 "신규 국가 14개 CountryView 자동 배치" 참고). **단, 전부 같은
  플레이스홀더 스프라이트를 재사용한 상태라 실제 국가 모양 아트가 필요하면 사람이 Unity 에디터에서
  스프라이트를 교체해야 함.**
- `DnaBubble` 프리팹 제작 (SpriteRenderer + CircleCollider2D) 후 `BubbleSpawner.bubblePrefab`에 연결
- `CountryDatabase`/`PathogenDefinition`/`UpgradeTreeDatabase` 에셋 생성 (우클릭 > Create > Contagion > ...) 후
  실제 국가/병원체/트리 데이터 입력, `GameDataBootstrapper`에 연결
- 앱인토스 SDK 설치(`Packages/manifest.json`) + 콘솔에서 게임 카테고리 등록 + 광고 그룹 생성
  — **한 번 시도했다가 되돌림(2026-07-03)**: manifest.json에 git 의존성을 추가했더니 Unity가 패키지를
  resolve하면서 SDK 자체 에디터 툴링이 pnpm install을 자동 실행했는데
  `ERR_PNPM_LOCKFILE_CONFIG_MISMATCH`(락파일의 `overrides` 설정 불일치)로 실패해 계속 에러 로그를 뿜음.
  게임 마지막 단계로 미루기로 한 상태에서 불필요한 노이즈라 manifest.json에서 다시 제거함. 나중에 Step 7
  진행할 때 재추가하고, 이 에러가 다시 뜨면 SDK 패키지 캐시 폴더(`Library/PackageCache/im.toss.apps-in-toss-unity-sdk@.../`)
  안에서 `pnpm install --no-frozen-lockfile`을 수동 실행하거나, 해당 폴더의 `pnpm-lock.yaml`을 삭제 후
  재설치하는 방향으로 해결 시도할 것 (사람이 터미널에서 실행해야 함 — Unity 내장 pnpm 실행 경로 문제일 수도 있어
  Node/pnpm이 로컬에 정상 설치돼 있는지도 같이 확인).
- (Step 14) `AudioManager` 오브젝트 생성 + 스크립트 부착 + 효과음 에셋 준비 후 연결 (아래 순서 참고)

---

## 나무위키 참고 자료 (원본 게임 대비 보완 아이디어, 2026-07-03 추가)

동아리 부장님이 아닌 사용자가 직접 원본 Plague Inc. 나무위키 문서(시스템/전략/이벤트/상태)를 링크로
제공해 현재 구현과 비교 분석했다. 상세 내용은 `Docs/PlagueIncReference.md` 참고.

**Step 14에서 반영 완료** (위 "Step 14 구현 메모" 참고):
1. 국가별 사망률 기반 개별 붕괴 단계 (`Country.GetCollapseStage()` + `HumanResistanceManager`)
2. 난이도별 전염성 보정 (`GameManager.GetDifficultySpreadMultiplier()`)
3. 처형/폭격 이벤트 (`EventManager.ApplyExecutionOrBombing()`)

**Step 16에서 나머지 전부 반영 완료** (아래 "Step 16 구현 메모" 참고): 세계 상태 텍스트 세분화, 국가별
치료 자금 상한선, 국경 폐쇄 우선순위(공항>국경>항구 순차 폐쇄), 올림픽 등 플레이버 이벤트.
`Docs/PlagueIncReference.md`의 백로그 항목은 이제 전부 반영 완료 상태다.

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
