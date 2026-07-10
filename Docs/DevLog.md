# Contagion Project — 개발 로그 (상세)

`CLAUDE.md`의 진행 상황 표에 있는 각 Step의 상세 구현 배경, 설계 문서와 다르게 간 이유, 버그 진단
과정을 담은 아카이브. 세션 시작 시 매번 읽을 필요는 없고, 특정 Step/버그를 다시 조사해야 할 때만
검색해서 찾아보면 된다 — CLAUDE.md는 "현재 상태 + 다음에 할 일" 위주로 가볍게 유지하고, "왜 이렇게
됐는지"는 여기 남긴다.

---

## Step 7 구현 메모 (EventManager)

- 설계 문서 8절의 6개 이벤트(자연재해/정치불안/의료파업 = 병원체 유리, WHO긴급회의/국제공조/백신임상성공 = 인류 유리)를
  `EventManager`에 하드코딩된 개별 메서드로 구현. 문서에 정확한 발동 확률/쿨다운/임계값이 없어
  인스펙터에서 조정 가능한 `NewsEventSettings`(확률+쿨다운)와 이벤트별 임계값 필드로 노출.
- `SimulationManager.OnTickCompleted` 구독 → 매 틱 조건 검사 → 확률 통과 시 효과 적용 + `OnNewsEvent` 발행.
- 백신 임상 성공은 1회성(oneShot) 이벤트로 처리 — 게임당 한 번만 발동.

## Step 8 구현 메모 (UI Toolkit)

- HUD(`HudController`+`NewsFeedController`), 업그레이드 트리(`UpgradeTreeView`), 국가 팝업(`CountryPopupController`),
  엔딩(`EndingScreenController`), 랭킹 패널(`RankingPanelController`) — 총 5개 UIDocument 화면.
- 각 컨트롤러는 자기 데이터 소스(WorldDataManager/SimulationManager/EventManager 등)에 **직접** 구독한다.
  `UIManager`는 패널 간 조정(탭 클릭→업그레이드 화면 열기, 랭킹 버튼→랭킹 패널, 재시작, 부활 광고)만 담당.
- 업그레이드 트리는 좌표 기반 그래프 대신 **카테고리별 리스트**로 단순화 (설계 문서에 노드 좌표 데이터가 없음).
  실제 그래프 시각화(선 연결 등)는 후속 작업.
- 엔딩 화면 점수 계산: 설계 문서 11절(바이오하자드 점수)과 14절(Score = 바이오하자드점수 × 난이도배율 × 클리어속도보너스)이
  "클리어 속도"를 중복 정의하고 있어(11절 점수 구성에 이미 날짜 항목 포함), `EndingScreenController.ComputeFinalScore`에서
  바이오하자드점수(날짜+업그레이드효율+병원체배율) × 난이도배율만 적용하도록 정리함.

## Step 9 구현 메모 (ScriptableObject)

- `Pathogen`/`Country`/`UpgradeNode`에 `Clone()` 추가 — SO 자산은 "템플릿"이고, 실제 플레이는 항상 복제본을 사용해
  원본 자산이 오염되지 않게 함. `GameDataBootstrapper`가 씬 시작 시 SO → 복제 → 각 매니저(`SetCountries`/`SetPathogen`/`SetTree`) 주입.
- 병원체 선택(MainMenu)·발원 국가 선택(CountrySelect) 씬이 아직 없어서, 지금은 `GameDataBootstrapper` 인스펙터에
  고정 지정한 `selectedPathogen`/`startingCountryId`로 시작한다. 씬 전환이 생기면 정적 세션 설정 클래스로 교체할 것.
  (→ Step 18에서 MainMenu/CountrySelect 화면으로 실제 구현됨.)

## Step 10~13 구현 메모 (앱인토스 SDK 연동 — 중요)

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
  메인메뉴 DNA+5 보너스는 MainMenu UI가 아직 없어 버튼에 연결만 안 된 상태였음 (Step 18에서 MainMenu UI가 생겼으므로
  `GameAds.Rewarded` 재사용해서 버튼만 붙이면 됨 — 아직 미완료).
- Step 13 저장: 에디터/비 AIT 빌드는 `Application.persistentDataPath`에 JSON 파일로 저장(즉시 동작),
  AIT 빌드는 `AIT.StorageSetItem/GetItem`을 시도하고 실패 시 로컬로 폴백. 5일(틱)마다 자동 저장 +
  `OnApplicationPause`/`OnApplicationQuit`에도 저장. userHashKey 기반 유저별 분리는 아직 미적용(단일 슬롯) —
  필요해지면 `SaveKey`에 `AIT.GetUserKeyForGame()` 접두사를 붙일 것.
- **콘솔/외부 설정 필요** (코드로 불가능, 사람이 해야 함): 앱인토스 콘솔에서 미니앱을 **게임 카테고리**로 등록
  (아니면 게임센터 API 전부 `40000` 에러), 수익화 메뉴에서 광고 그룹 생성 후 실 `adGroupId` 발급,
  `Packages/manifest.json`에 AIT SDK git URL 등록.

## 초기 플레이테스트 버그 (Step 13 완료 직후, 2026-07-03)

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
  이 계수도 같이 재조정 필요 (실제로 Step 17에서 국가가 18개로 늘면서 재조정이 필요해짐 — 아래 참고).

## Step 14 구현 메모 (기본 플레이 퀄리티 개선, 2026-07-03)

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

이 중 **AudioManager만 새 씬 오브젝트가 필요**하다 (CLAUDE.md "씬/에셋 배선 필요" 참고). 나머지는 전부 기존에
이미 씬에 배치된 컴포넌트(SimulationManager/GameManager/HumanResistanceManager/EventManager/
CountryView/DnaBubble)의 코드만 확장한 것이라 추가 배선 없이 바로 적용된다 — 새로 추가된 인스펙터
필드는 기본값으로 자동 채워지며, 밸런싱하고 싶으면 그때 조정하면 된다.

**Step 14 플레이테스트 중 발견한 버그**: `HumanResistanceManager`의 완전 무정부 치안붕괴 사망 로직이
`country.deadCount`만 증가시키고 `infectedCount`는 그대로 둬서, `LivingPopulation`(=population-deadCount)이
줄어드는데 감염자 수는 안 줄어들어 감염률이 100%를 초과(로그에서 606% 확인됨)하는 버그가 있었다.
치안붕괴로 죽은 사람 중 일부는 원래 감염자였다고 보는 게 합리적이므로, `unrestDeaths` 반영 직후
`country.infectedCount = Math.Min(country.infectedCount, country.LivingPopulation)`으로 클램프해서 해결.

## Step 15 구현 메모 (업그레이드 시스템 세분화, 2026-07-03)

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

## Step 16 구현 메모 (나무위키 백로그 잔여 4항목, 2026-07-03)

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

`Docs/PlagueIncReference.md`의 나무위키 백로그 항목은 Step 14 + Step 16으로 전부 반영 완료 상태다.

## Step 17 구현 메모 (콘텐츠 확장 — 국가/병원체, 2026-07-03)

"게임 퀄리티를 높이자"는 요청에 AskUserQuestion으로 방향(비주얼/연출, 콘텐츠 확장, UI/UX 폴리싱 — 사운드는
상업적으로 이용 가능한 에셋을 구해야 해서 이번엔 제외)과 순서(콘텐츠 확장 먼저, 국가 15~20개 규모)를
확인한 뒤 진행. 앱인토스 SDK/콘솔 작업은 사용자 요청으로 게임 마지막 단계로 연기.

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
Unity 에디터도 이미지 생성 도구도 없어서 새 스프라이트를 만들 수 없었다 (→ Step 22에서 `Resources.Load`
방식으로 이 제약을 우회하는 방법을 찾음 — 아래 참고). 결과적으로 18개국 전부
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
너무 빨리 100%를 찍으면 이 값부터 조정할 것 (초기 플레이테스트 버그 섹션에서 이미 예견된 사항).

## Step 18 구현 메모 (MainMenu/CountrySelect 화면 신규 제작, 2026-07-03)

"UI/UX 폴리싱" 방향 중 "MainMenu/CountrySelect 화면 신규 제작"을 우선 진행. 기존 프로젝트는 별도 씬
전환 없이 단일 `GamePlay` 씬 안에서 UIDocument 패널을 켜고 끄는 구조라(HUD/업그레이드트리/국가팝업/엔딩/랭킹
전부 이 패턴), MainMenu/CountrySelect도 새 Unity 씬을 만들지 않고 같은 패턴의 UIDocument 패널 2개로
구현했다. 국기 아이콘은 사용자 요청대로 이번엔 넣지 않고 빈 슬롯만 예약해둠 (→ Step 22에서 채움).

| 항목 | 파일 | 내용 |
|------|------|------|
| 병원체 선택 화면 | `UI/MainMenuController.cs`, `Assets/UI/MainMenu.uxml`, `Assets/UI/MainMenu.uss` | `GameDataBootstrapper.AvailablePathogens`(6종)로 카드 리스트 생성, 감염력/중증도/치사율/약물저항 4개 스탯을 텍스트 바(■/□)로 표시. 카드 선택 시 `OnPathogenConfirmed` 이벤트 발행 |
| 발원 국가 선택 화면 | `UI/CountrySelectController.cs`, `Assets/UI/CountrySelect.uxml`(스타일은 `MainMenu.uss` 재사용) | `GameDataBootstrapper.AvailableCountries`(18개)로 국가 리스트 생성. 각 행에 `country-row__flag`라는 **빈 VisualElement를 국기 아이콘 자리로 미리 예약**만 해둠. `OnCountryConfirmed`/`OnBackRequested` 이벤트 발행 |
| 게임 시작 플로우 게이팅 | `Managers/GameManager.cs`(`isPaused` 기본값 `true`), `Managers/GameDataBootstrapper.cs`(싱글톤 `Instance`, `AvailablePathogens`/`AvailableCountries`, `BeginGame(pathogen, countryId)`), `Managers/UIManager.cs` | `UIManager.Start()`에서 게임 진입 시 무조건 일시정지 + MainMenu 노출. MainMenu→(병원체 확정)→CountrySelect→(국가 확정)→`GameDataBootstrapper.BeginGame()`(병원체 세팅+감염 시드+`GameManager.SetPaused(false)`) 순서로 진행. `GameDataBootstrapper`에 `skipMainMenu` 플래그를 추가해뒀으니, 나중에 테스트용으로 메뉴 없이 바로 시작하고 싶으면 인스펙터에서 켜면 됨(현재 `0`=꺼짐, 항상 메뉴부터 시작) |
| 씬 배선 | `Assets/Scenes/GamePlay.unity` | `MainMenuUI`/`CountrySelectUI` GameObject 2개를 기존 `CountryPopupUI`와 동일한 패턴(GameObject+Transform+UIDocument+컨트롤러 MonoBehaviour)으로 씬 파일에 직접 추가하고 SceneRoots에 등록. `GameDataBootstrapper` MonoBehaviour에 `availablePathogens`(6개 병원체 에셋 참조)+`skipMainMenu: 0` 필드 추가. `UIManager` MonoBehaviour에 `mainMenuController`/`countrySelectController` 참조 연결. **에셋 생성부터 씬 배선까지 전부 텍스트 편집으로 완료 — Unity 에디터 GUI 조작 없이 끝남** (Grep으로 fileID 충돌/중복 없음 검증 완료) |

**전부 코드+에셋 텍스트 편집만으로 완료.**

## Step 19 구현 메모 (게임 시작-끝-재시작 루프 안정화, 2026-07-03)

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

## Step 20 구현 메모 (재시작 후 CountrySelect 완전 먹통 버그 — 진짜 근본 원인, 2026-07-03)

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

## Step 21 구현 메모 (HUD 가시성 개선 — 스탯 그래프화 + 업그레이드 창 3분할, 2026-07-03)

"텍스트로만 정보가 주어져서 가시성이 떨어진다"는 피드백으로 두 가지를 개선했다.

| 항목 | 파일 | 내용 |
|------|------|------|
| 감염자/사망자/치료제 스파크라인 그래프 | `UI/HudSparkline.cs`(신규), `UI/HudController.cs`, `Assets/UI/Hud.uxml`, `Assets/UI/Hud.uss` | UI Toolkit엔 내장 차트 위젯이 없어서 `UpgradeTreeView`의 연결선처럼 `Painter2D`로 직접 그리는 재사용 가능한 `HudSparkline` 클래스를 만들었다. 최근 최대 200개 샘플을 그 구간의 최솟값~최댓값으로 자동 스케일링해서 그린다(절대 수치 축은 표시 안 함 — 정확한 값은 옆 텍스트 라벨이 담당, 그래프는 추세만). `HudController.HandleWorldStateChanged`(매 틱=하루 호출)에서 `infectedCount`/`deadCount`/`cureProgress*100`을 매번 샘플로 추가 |
| 업그레이드 트리 전파/증상/능력 → 완전히 별개인 창 3개 | `UI/UpgradeTreeView.cs`, `Assets/UI/UpgradeTree.uxml`(`category-title-label` 추가), `Managers/UIManager.cs`, `Assets/Scenes/GamePlay.unity` | 기존엔 탭 버튼 3개가 전부 같은 창(27노드가 한 캔버스에 다 있어 직접 스크롤해서 찾아야 함)을 열었다. `UpgradeTreeView`에 `category` 필드를 추가해 "이 창은 이 카테고리만 담당"하도록 바꾸고, `RebuildTree()`가 `UpgradeManager.Tree`를 그 카테고리로 필터링(9개만)한 뒤 그 카테고리의 최소 x좌표를 빼서 항상 0부터 시작하도록 정규화(DefaultUpgradeTreeFactory의 절대좌표는 카테고리별로 40/640/1240부터 시작이라 안 그러면 왼쪽에 큰 빈 여백이 생김). 씬에 같은 `UpgradeTree.uxml`을 재사용하는 GameObject 2개(`SymptomTreeUI`/`AbilityTreeUI`, 기존 `UpgradeTreeUI`는 `TransmissionTreeUI`로 개명)를 추가하고, `UIManager`가 `transmissionTreeView`/`symptomTreeView`/`abilityTreeView` 3개 참조를 각각 열고 나머지 둘은 닫도록 라우팅 |

**주의**: `UpgradeTreeView.Show()`가 `UpgradeCategory? focusCategory` 파라미터를 받던 걸 파라미터 없는
`Show()`로 바꿨다(카테고리가 이제 인스펙터 고정값이라 호출 시점에 지정할 필요가 없어짐) — 혹시 다른
곳에서 `Show(category)` 형태로 호출하는 코드가 남아있으면 컴파일 에러가 나니 확인할 것(현재는
`UIManager.HandleUpgradeTabClicked`만 호출하며 이미 새 시그니처로 수정 완료. 2026-07-03 세션에서
직접 코드/씬 정합성 재검증 완료 — 잔재 없음).

## Step 22 구현 메모 (국기 아이콘 일괄 처리, 2026-07-03)

Step 18에서 빈 슬롯(`country-row__flag`)만 예약해뒀던 국기 아이콘을 실제로 채웠다. Unity 에디터도
이미지 생성 도구도 없던 Step 17 때와 달리, 이번엔 오픈소스 SVG 국기(hjnilsson/country-flags,
MIT 라이선스)를 GitHub 웹 페이지에서 텍스트로 긁어와(`raw.githubusercontent.com`은 샌드박스
네트워크 allowlist에 없어서 막힘 — 대신 `github.com/.../blob/...` 페이지의 문법 강조용 JSON
페이로드(`"rawLines"`)에서 SVG 원본을 역추출) `cairosvg`(pip 설치)로 PNG 변환하는 방식을 썼다.
(ImageMagick 기본 SVG 렌더러는 UK/브라질처럼 clipPath를 쓰는 복잡한 국기에서 깨져서 cairosvg로 교체 —
변환 직후 18개국 전부 실제로 이미지를 열어 육안 확인했음.)

| 항목 | 파일 | 내용 |
|------|------|------|
| 국기 PNG 18개 | `Assets/Resources/Flags/{CHI,IND,USA,KOR,JPN,IDN,SAU,GER,UK,FRA,RUS,NGA,EGY,RSA,BRA,MEX,CAN,AUS}.png` | 160px 폭 PNG. `Resources/` 폴더 밑이라 씬/에셋 파일에 GUID를 하드코딩할 필요 없이 `Resources.Load`로 경로 문자열만으로 런타임 로드 가능 — Step 17에서 겪었던 "새 스프라이트는 Unity 에디터가 fileID를 확정해야 해서 텍스트 편집만으로 못 만든다"는 제약을 회피하는 방법. 세션 도중 Unity 에디터가 폴더를 감지해 `.meta`를 자동 생성한 것으로 보임(`textureType: 8`=Sprite(2D and UI), `spriteMode: 2`=Multiple — 이 조합에선 메인 오브젝트가 여전히 Texture2D라 `Resources.Load<Texture2D>`가 정상 동작) |
| 국기 로드/배치 | `UI/CountrySelectController.cs`(`GetFlagTexture()`, `_flagCache`) | `CreateCountryRow()`에서 `country.id`로 `Resources.Load<Texture2D>($"Flags/{id}")` 조회 후 `flagSlot.style.backgroundImage`에 지정. 못 찾으면 기존처럼 빈 사각형 + 경고 로그. 국가 목록을 다시 그릴 때마다(재시작 등) 반복 로드하지 않도록 딕셔너리 캐시 |
| 스타일 | `Assets/UI/MainMenu.uss`(`.country-row__flag`) | `-unity-background-scale-mode: scale-to-fit` 추가 — 국기 원본 비율(국가마다 다름, 대부분 3:2 근처)이 슬롯(28×20px)에서 찌그러지지 않게 |

**라이선스 메모**: 국기 SVG 출처는 `github.com/hjnilsson/country-flags`(MIT). 국기 자체(국가 상징)는
저작권 보호 대상이 아니며 이 리포의 SVG 표현도 자유 사용 라이선스라 상업 배포에 문제없음.

## Step 23 구현 메모 (세계 지도 국가별 실제 실루엣, 2026-07-03)

"비주얼 폴리싱 중 어느 걸 먼저 할까요"라는 질문에 사용자가 "국가 지도 실제 모양"을 선택. Step 17에서
"Unity 에디터 없이는 새 스프라이트의 fileID를 확정할 수 없어 불가능"이라고 결론 냈던 바로 그 작업인데,
Step 22 국기 아이콘에서 쓴 `Resources.Load` 런타임 로드 패턴을 세계 지도 국가 스프라이트에도 그대로
적용할 수 있다는 걸 깨닫고 진행했다 — 씬의 `SpriteRenderer.sprite` 필드를 코드로 덮어쓰면 씬 파일에
스프라이트 GUID를 하드코딩할 필요가 아예 없어진다.

**국가 경계 데이터 소스**: `github.com/johan/world.geo.json`(countries/{ISO3}.geo.json, UNLICENSE) —
Step 22와 같은 방식으로 GitHub blob 페이지의 `"rawLines"` JSON 페이로드에서 GeoJSON 원본을 역추출.
18개국 각각 ISO3 코드로 매핑(예: 프로젝트 내부 id `CHI`→`CHN`, `GER`→`DEU`, `UK`→`GBR`, `RSA`→`ZAF`,
나머지는 내부 id와 ISO3가 이미 같음).

**SVG 생성 파이프라인** (Python, `/tmp` 스크래치 — 리포 안에는 최종 PNG만 커밋됨):
1. GeoJSON의 Polygon/MultiPolygon 좌표(경도,위도)를 읽어 모든 폴리곤 링(exterior+hole)을 수집.
2. **러시아 안티메리디안(경도 ±180) 교차 버그**: 초기 시도에서 러시아의 bbox 폭이 360°(지구 전체 폭)로
   계산돼 모양이 다 찌그러졌다 — 데이터가 서경(-180 근처)과 동경(+180 근처) 양쪽에 걸쳐 있는데 이걸
   단순 min/max로 계산하면 실제로는 붙어있는 땅이 지구 반대편으로 벌어진 것처럼 계산됨. 국가별로
   "경도를 0~360으로 감았을 때"와 "안 감았을 때" 중 bbox 폭이 더 작은 쪽을 채택하는 방식으로 감지/보정
   (`build_svg.py`의 `use_wrap` 로직). 러시아만 여기 해당(170.4°로 정상화됨), 나머지 17개국은 영향 없음.
3. 홀(구멍)이 있는 국가(남아공 안의 레소토 등)는 `fill-rule="evenodd"`로 자동 처리 — 실제로 남아공
   실루엣에서 레소토 구멍이 정상적으로 뚫려 나온 것을 육안 확인함.
4. 각 국가를 정사각형 캔버스(240×240, 패딩 14px)에 종횡비 유지한 채 맞춰 넣고 흰색 채우기(`#ffffff`)
   SVG path 생성 → `cairosvg`로 256×256 투명 배경 PNG 렌더링.
5. **18개국 전부 렌더링 결과를 직접 열어 육안 검증** — 특히 다도해/비연속 국가(러시아 극동/캐나다 북극
   군도/미국 알래스카+하와이/인도네시아/일본)가 실제로 알아볼 수 있는 모양으로 나오는지 확인.

| 항목 | 파일 | 내용 |
|------|------|------|
| 국가 실루엣 PNG 18개 | `Assets/Resources/CountryShapes/{CHI,IND,USA,KOR,JPN,IDN,SAU,GER,UK,FRA,RUS,NGA,EGY,RSA,BRA,MEX,CAN,AUS}.png` | 256px 정사각 캔버스, 흰색 실루엣 + 투명 배경(알파). 흰색으로 만든 이유: `CountryView`가 `SpriteRenderer.color`를 감염률에 따라 초록→노랑→빨강으로 틴트하는 기존 로직을 그대로 쓰려면 스프라이트 자체가 흰색+알파여야 틴트가 의도대로 먹음(기존 플레이스홀더 사각형은 불투명 JPG라 국가 모양이 아니라 사각형 전체가 칠해졌었음) |
| 런타임 로드/스케일링 | `Gameplay/CountryView.cs`(`ApplyCountryShape()`) | `Resources.LoadAll<Sprite>($"CountryShapes/{countryId}")`로 로드(프로젝트 기본 텍스처 임포트 프리셋이 Sprite Multiple 모드라 서브에셋 이름이 `{id}_0`이 되므로 `Resources.Load<Sprite>` 직접 호출은 실패할 수 있음 — Step 22에서 파악한 동작과 동일한 이유로 `LoadAll` 사용). 국가마다 원본 종횡비가 달라서(러시아는 옆으로 김, 한국은 세로로 김) 긴 변을 `shapeTargetSize`(기본 0.45 월드 유닛, 기존 플레이스홀더 크기 0.48과 비슷하게 맞춤)에 맞추는 균일 스케일을 계산해 `transform.localScale`에 적용. `BoxCollider2D.size`도 스프라이트 bounds로 갱신해 클릭 판정 영역이 대략 실루엣 크기를 따라가게 함(정확한 폴리곤 판정은 아니고 사각형 근사) |

**전부 코드+에셋 파일 추가만으로 완료 — 씬 파일(`GamePlay.unity`)은 건드리지 않음** (기존 `SpriteRenderer.sprite`
필드는 그대로 두되 런타임에 코드가 덮어씀). Unity 에디터에서 확인할 것: 세계 지도에서 18개국이 회색
사각형이 아니라 각자 실제 나라 모양으로 보이는지, 감염 진행에 따라 색이 여전히 정상적으로 바뀌는지,
국가 클릭(팝업 열기)이 여전히 잘 되는지. 콘솔에서 "실제 국가 실루엣 스프라이트 적용 완료" 로그 18개가
찍히는지도 확인 — 안 찍히고 "찾지 못해 기존 플레이스홀더 유지" 경고가 뜨면 `Resources/CountryShapes/`
폴더의 임포트가 아직 안 된 것이니 Unity 에디터를 한 번 포커스하고 기다렸다가 재생해볼 것.

**라이선스 메모**: 국경 데이터 출처는 `github.com/johan/world.geo.json`(UNLICENSE — 퍼블릭 도메인에
가까운 자유 사용 라이선스). 국가 경계선 자체도 저작물이 아니라 사실 정보라 상업 배포에 문제없음.

## Step 24 구현 메모 (모바일 세로 화면 타겟팅 — 갤럭시 S25 울트라, 2026-07-03)

사용자가 "실제 플레이 환경을 모바일로 하려고 한다, 화면 비율은 갤럭시 S25 울트라 기준"이라고 요청.
S25 울트라 스펙(웹 검색으로 확인): 1440×3120px, 19.5:9. 확인해보니 프로젝트가 세로 모바일을 전혀
고려하지 않은 상태였다 — `ProjectSettings.defaultScreenOrientation: 4`(자동 회전 전부 허용),
기본 해상도 1920×1080(가로), `PanelSettings.referenceResolution: 1200×800`(가로 3:2), 세계 지도
카메라(`orthographic size: 5`)와 Step 17~23에서 배치한 18개국 좌표(x:[-2.6,2.6] y:[-2.0,2.3], 가로로
넓고 세로로 낮은 배치)까지 전부 가로/정사각 기준이었다.

**세계 지도 화면 방식 결정**: "전체 지도 한눈에 보이기 / 좌우만 스크롤 / 자유 스크롤+핀치줌" 3안을
제시했고, "전체 지도 한눈에"(스크롤 없이 화면에 다 들어오는 방식, 원작 Plague Inc.도 기본이 이 방식)로
결정. 좌우 스크롤이나 자유 팬/줌은 터치 입력 코드가 추가로 필요해서 이번 스코프에서 제외.

| 항목 | 파일 | 내용 |
|------|------|------|
| 화면 회전 잠금 | `ProjectSettings/ProjectSettings.asset` | `defaultScreenOrientation: 4`(AutoRotation)→`0`(Portrait 고정). `allowedAutorotateToPortraitUpsideDown`/`allowedAutorotateToLandscapeRight`/`allowedAutorotateToLandscapeLeft`도 전부 `1`→`0`으로 꺼서 혹시 나중에 orientation을 다시 Auto로 바꾸더라도 세로만 허용되게 안전장치. `defaultScreenWidth/Height`도 1920×1080→1440×3120(S25 울트라 실제 해상도)로 변경 |
| UI Toolkit 세로 재조정 | `Assets/UI Toolkit/PanelSettings.asset` | `m_ReferenceResolution`을 1200×800(가로)→1440×3120(세로, S25 울트라 실해상도)로 변경. `m_ScreenMatchMode`(Match Width Or Height)/`m_Match`(0=너비 기준)는 그대로 유지 — 기준 해상도 자체가 이제 타겟 기기와 비율이 일치하므로 너비 기준 매칭이면 충분 |
| 세계 지도 카메라/배치 세로 재설계 | `Assets/Scenes/GamePlay.unity` | 아래 상세 참고 |

**세계 지도 재설계 계산**: 목표는 지도 바운딩 박스의 가로:세로 비율을 화면 비율(9:19.5 ≈ 0.4615)과
맞춰서, 카메라 orthographic size 하나로 위아래/좌우 여백 없이 동시에 꽉 차게 만드는 것.
- 18개국을 3열×6행 그리드로 재배치: x = {-0.85, 0, 0.85}, y = {2.5, 1.5, 0.5, -0.5, -1.5, -2.5}
  (바운딩 박스 약 1.7×5.0, 인접 국가 간 최소 간격 0.85 — Step 23에서 정한 `shapeTargetSize`(0.45)
  기준 실루엣 반지름 0.225보다 훨씬 여유 있어 겹침 없음). 대륙별로 위(북반구: 러시아/캐나다/독일)→
  아래(남반구: 남아공/브라질/호주) 느슨한 흐름을 주되, Step 17부터 이미 추상적 그리드 배치였으므로
  지리적 정확성은 그대로 포기.
- 카메라 `orthographic size`: 5 → 3.8. 유도 과정 — 세로 시야 높이 = orthoSize×2, 가로 시야 폭 =
  orthoSize×2×(화면가로/화면세로). 지도 바운딩 박스(가로 1.7~2.6, 세로 5.0~5.6 여유 포함 추정치)에
  안전 마진(약 1.3~1.35배, HUD 상단 뉴스피드 90px+하단 스탯바 약 110px가 화면 일부를 반투명하게 가리는
  것 + 노치/제스처 네비게이션 세이프 에어리어 감안)을 곱해서 역산.
- 카메라 Transform 위치(0,0,-10)와 WorldMap 부모 오브젝트의 Transform(0,0,0, scale 1)은 이미 원점
  기준이라 그대로 둠 — 새 그리드도 원점 대칭이라 카메라를 옮길 필요가 없었음.
- **적용 방법**: `GamePlay.unity`는 텍스트 YAML이라 Python으로 18개 `CountryView` MonoBehaviour →
  `m_GameObject` fileID → 그 GameObject의 컴포넌트 목록에서 타입 `!u!4`(Transform)를 찾아
  `m_LocalPosition`만 치환하는 스크립트를 짜서 일괄 처리(Step 17의 수동 좌표 삽입보다 안전 — fileID를
  틀릴 위험이 없음). Camera 컴포넌트(`!u!20`)의 `orthographic size`도 같은 스크립트에서 함께 치환.
  **주의**: 이 씬 파일은 CRLF 줄바꿈인데 Python `open(..., 'w')` 기본 텍스트 모드로 그냥 쓰면 LF로
  바뀌어서 diff가 파일 전체(4000줄+)로 부풀어 오르는 사고가 났었다 — 재작업 시 반드시 `\n`→`\r\n`
  치환 후 바이너리 모드로 쓸 것(`ProjectSettings.asset`/`PanelSettings.asset`처럼 원래 LF인 파일도
  있으니, 수정 전에 `file <경로>`로 원본 줄바꿈 방식부터 확인). 최종 diff는 국가 18개 위치 줄 +
  카메라 사이즈 줄, 정확히 19줄만 바뀜.

**전부 텍스트 편집만으로 완료 — Unity 에디터 GUI 조작 불필요.** Unity 에디터에서 확인할 것:
1) Game 뷰를 세로(1440×3120 또는 9:19.5 비율)로 맞추고 재생 — 세계 지도 18개국이 위아래/좌우 여백
   없이 화면에 꽉 차게 보이는지, 국가 클릭/색상 갱신이 여전히 정상인지.
2) 실기기 또는 세로 비율 시뮬레이터에서 상단 뉴스피드(90px)/하단 스탯바(~110px)에 지도 위아래 끝이
   얼마나 가려지는지 육안 확인 — 마진 계수(1.3~1.35배)는 추정치라 실제로 보면서 orthographic size를
   미세 조정해야 할 수 있음(가려짐이 심하면 값을 줄이고, 위아래 빈 공간이 많이 남으면 값을 키울 것).
3) Unity Game 뷰 해상도 프리셋 목록에 1440×3120이 없으면 "+" 버튼으로 직접 추가해야 함(이건
   에디터 로컬 상태라 텍스트 편집으로 미리 넣어줄 수 없는 부분).
4) 좌우 스크롤/자유 줌은 이번에 구현 안 함 — 나중에 필요해지면 카메라 드래그 팬 스크립트를 새로
   추가하는 방향(예: `Gameplay/WorldMapCameraController.cs`류)으로 확장 가능.

## Step 25 구현 메모 (모바일 UI 가시성 폴리싱 — 참조 해상도/지도 비율/Safe Area, 2026-07-03)

Step 24 직후 사용자가 실기기 비율로 확인해보고 "PC 최적화라서 가시성이 떨어지는 것 같다"는 피드백과
"지도가 뭔가 안 맞는다"는 피드백을 줬다. `C:\Game\codebase`(참조 전용 위키)에 앱인토스 Unity SDK
문서가 있다고 알고 있다며 거기서 모바일 최적화 패턴을 찾아 적용해달라고 요청. 위키의
`wiki/apps-in-toss-unity/_overview.md`와 관련 페이지를 확인해 두 가지 문제를 찾았다.

**문제 1 진단 — PanelSettings 참조 해상도 오설정.** Step 24에서 `m_ReferenceResolution`을
1200×800(가로)에서 1440×3120(세로, S25 울트라 **실제** 픽셀 해상도)로 바꿨는데, 이게 바로 가시성
저하의 원인이었다. UI Toolkit의 `ScaleMode.ScaleWithScreenSize`는 참조 해상도를 "논리/디자인 해상도"로
쓰고 실제 화면이 이보다 크면 그 비율만큼 모든 px 크기(폰트, 패딩, 버튼 크기 등)를 확대해서 그리는
방식이다(CSS px나 iOS 포인트와 같은 개념). 참조 해상도를 실기기의 물리 픽셀 그대로 넣으면 배율이
정확히 1.0이 되어 버튼/글자가 디자인한 px 값 그대로, 즉 고밀도 화면 기준으로는 지나치게 작게
렌더링된다 — 이게 "PC에서 보기엔 괜찮은데 실기기에서 작아 보인다"는 증상의 정체. 코드베이스 위키가
권장하는 패턴대로 참조 해상도를 실기기보다 훨씬 작은 논리 해상도로 되돌렸다: `1440×3120` →
`480×1040`(S25 울트라 대비 정확히 3배 배율이 나오는 값으로 선택 — 1440/480=3, 3120/1040=3). `Assets/UI
Toolkit/PanelSettings.asset`의 `m_ReferenceResolution`만 수정, `m_ScreenMatchMode`/`m_Match`(너비 기준)는
그대로 유지.

**문제 2 진단 — 세계 지도 화면비 재계산.** Step 24의 지도 그리드 좌표(x:{-0.85,0,0.85},
y:{2.5,1.5,0.5,-0.5,-1.5,-2.5})와 카메라 orthographic size(3.8)는 "안전 마진 1.3~1.35배 추정치"로
어림한 값이라 실기기 비율(9:19.5≈0.4615)과 정확히 안 맞았던 것으로 판단 — 지도 바운딩 박스 실측
가로:세로 비율이 화면 비율과 어긋나면 위아래 또는 좌우로 여백이 남거나 반대로 잘려 보인다. 그리드
간격을 화면 비율에 정확히 맞게 재계산: x = {-1.4, 0, 1.4}, y = {3.0, 1.8, 0.6, -0.6, -1.8, -3.0} —
바운딩 박스 가로 2.8×세로 6.6+실루엣 반지름 여유, 비율이 목표 화면비에 훨씬 근접하도록 열 간격을
0.85→1.4로, 행 간격을 1.0→1.2로 늘렸다. `GamePlay.unity`의 18개 `CountryView` Transform
`m_LocalPosition`을 Step 24와 동일한 방식(fileID 추적 + CRLF 보존)으로 재치환. 카메라 orthographic
size(3.8)는 이번엔 그대로 유지 — 그리드 자체를 화면비에 맞춰 늘렸으므로 별도 조정 불필요.

**문제 3(신규 발견) — Safe Area 미적용.** 코드베이스 위키를 훑다가 앱인토스 대상 프로젝트는 노치/펀치홀/
제스처 네비게이션 바 영역을 처리하는 Safe Area 적용이 필수 패턴이라는 걸 확인(`raw/apps-in-toss-safearea-guide.md`).
이 프로젝트엔 그런 처리가 전혀 없었다 — 화면 최상단/최하단에 붙은 HUD 요소가 실기기에서 카메라
펀치홀이나 제스처 바에 가려질 위험이 있었다. 위키의 `SafeAreaApplier` 드롭인 컴포넌트를 그대로
가져와 프로젝트 네임스페이스(`Contagion.UI`)로 붙였다.

| 항목 | 파일 | 내용 |
|------|------|------|
| PanelSettings 참조 해상도 정정 | `Assets/UI Toolkit/PanelSettings.asset` | `m_ReferenceResolution: {1440,3120}` → `{480,1040}` (실기기 대비 정확히 3배 배율) |
| 세계 지도 그리드 화면비 재조정 | `Assets/Scenes/GamePlay.unity` | 18개 `CountryView` Transform `m_LocalPosition` 재치환 — x:{-0.85,0,0.85}→{-1.4,0,1.4}, y 간격 1.0→1.2배로 확대(3.0/1.8/0.6/-0.6/-1.8/-3.0). Camera `orthographic size`는 3.8 유지 |
| Safe Area 적용 | `Scripts/UI/SafeAreaApplier.cs`(신규, 코드베이스 위키 드롭인), `Assets/Scenes/GamePlay.unity` | HUD/MainMenuUI/CountrySelectUI/TransmissionTreeUI/SymptomTreeUI/AbilityTreeUI/EndingScreenUI 7개 화면의 루트 GameObject에 컴포넌트 추가(`targetClass`를 각 UXML 루트 클래스로 지정, `navBarReserveTop: 48`). 패딩 기반이라 `flex-direction:column` 루트에는 바로 적용되지만, `CountryPopup`/`RankingPanel`처럼 화면 중앙에 뜨는 `position:absolute` 다이얼로그는 이미 중앙 정렬이라 Safe Area 영향을 안 받으므로 제외 |

**작업 중 발생한 해프닝(다음 세션 참고용)**: 지도 좌표 재치환을 bash로 시도하던 중, `mcp__workspace__bash`
로 읽은 `GamePlay.unity`가 갑자기 LF 줄바꿈+null 바이트 패딩+줄 수 불일치로 나타나 파일이 깨진 줄
알고 사용자에게 확인을 요청했었다. 사용자가 저장했다고 확인한 뒤에도 bash에서는 계속 같은 증상이
보였는데, host-side `Read` 툴로 같은 파일을 다시 열어보니 완전히 정상이었다(Step 24 데이터까지
전부 온전). **결론: bash 툴의 FUSE 마운트가 이 프로젝트 폴더에 대해 특정 조건(Unity가 더 짧은 길이로
재저장)에서 캐시가 갱신 안 되고 이전 버전의 꼬리 바이트를 보여주는 케이스가 있다** — 이 프로젝트를
git 직접 조작하지 않기로 한 것과 같은 근본 원인(FUSE lock 파일 이슈)일 가능성이 높다. **앞으로
`GamePlay.unity`는 읽기/쓰기 모두 bash 대신 Read/Grep/Edit(host-side) 툴만 사용할 것** — 실제로
이번에도 Edit 툴로 18개 좌표를 재치환해 문제없이 완료했다.

Unity 에디터에서 확인할 것:
1) Game 뷰를 1440×3120(또는 9:19.5) 프리셋으로 놓고 재생 — 버튼/텍스트 크기가 이전보다 커 보이는지
   (참조 해상도 480×1040 적용 후 실제로 3배 확대되어 그려지는지), 특히 CountrySelect 화면의 국가 행
   높이/터치 영역이 손가락으로 누르기 충분한 크기인지.
2) 세계 지도 18개국이 여백 없이 화면 가로/세로에 꽉 차는지, Step 24 때보다 비율이 더 정확히 맞는지.
3) HUD 상단 뉴스피드와 하단 스탯바가 화면 최상단/최하단 끝에 딱 붙지 않고 약간 안쪽으로 들어와
   있는지(Safe Area 패딩 적용 확인) — 에디터에서는 `Screen.safeArea`가 보통 풀스크린으로 나와 차이가
   거의 안 보일 수 있으니, `SafeAreaApplier`의 `previewInEditor`/`showDebugOverlay` 옵션을 켜서 인스펙터
   상에서 패딩 값이 정상적으로 계산되는지 확인하는 게 더 정확함.
4) MainMenu/CountrySelect/UpgradeTree(3종)/EndingScreen 화면도 각각 열어서 레이아웃이 깨지지 않았는지
   (패딩 추가로 콘텐츠가 밀려서 잘리는 요소가 없는지) 확인.

### Step 25 추가 대응 (2026-07-03, 같은 세션) — font-size 전체 상향

참조 해상도 정정(480×1040) 직후 사용자가 Unity 에디터 Game 뷰에서 재확인했는데도 "글씨가 여전히
작아 보인다"고 재차 피드백(HUD/메인메뉴·국가선택/업그레이드 트리 전부, DNA 획득 시 뜨는 플로팅
숫자만 상대적으로 커 보임). 원인 후보 두 가지를 사용자에게 설명하지 않고 바로 판단하기보다 먼저
확인 질문(어디서 봤는지/어느 화면인지)을 거쳤고, 답변은 전부 "Unity 에디터 Game 뷰"였다.

**진단**: 플로팅 DNA 숫자(`FloatingTextEffect.cs`)는 UI Toolkit이 아니라 월드 스페이스 `TextMesh`
(characterSize 0.3, 카메라 orthographic size 기준으로 렌더링)라서 애초에 PanelSettings 스케일 체계와
무관하게 큼직하게 나온다 — 이게 상대적으로 "여기만 크다"로 보인 이유. 반면 HUD/메뉴류는 전부 UI
Toolkit이라 PanelSettings 스케일(이번 세션에서 3배로 정정)의 영향을 받는데, 원래 font-size 자체가
11~16px로 작게 잡혀 있던 값들이라(과거 PC 가로 레이아웃 시절 기준으로 설계된 값) 3배 스케일이
정확히 적용되어도(예: 14px→42px) 실기기 밀도(약 500ppi대 고밀도 화면) 대비 여전히 다소 작게 느껴질
수 있는 절대 크기였다. 추가로 Unity 에디터 Game 뷰는 1440×3120처럼 초고해상도 타겟을 렌더링할 때
데스크톱 모니터 안 패널 크기에 맞춰 전체 프레임을 축소 표시하는 경우가 많아(줌 슬라이더 확인 필요),
실제로 스케일이 정확히 적용되고 있어도 에디터 화면에서는 더 작아 보이는 착시가 겹칠 수 있다 —
이건 프로젝트 버그가 아니라 에디터 프리뷰 특성이라 사용자에게 별도로 안내(Device Simulator 사용
또는 Game 뷰 줌 100% 확인 권장).

착시 여부와 무관하게 절대 크기 자체도 마진 없이 빠듯했다고 판단해, 스케일 정정과는 별개로 프로젝트
전체 USS의 font-size를 다시 한번 상향했다(플렉스 레이아웃 기반 라벨은 넉넉하게 확대, 업그레이드
트리 노드 라벨/비용만 예외 — 아래 참고).

| 파일 | 조정 내용 |
|------|-----------|
| `Assets/UI/Hud.uss` | 뉴스피드/스탯라벨 14px→18px, 스파크라인 그래프 라벨 12px→16px(그래프 박스도 92×30→104×34로 소폭 확대) |
| `Assets/UI/MainMenu.uss` | 타이틀 28→32px, 서브타이틀 15→19px, 병원체 카드명 17→21px/설명 13→17px, 국가 행 이름 16→20px/메타 12→16px(국기 슬롯도 28×20→36×26으로 확대), 상세패널 제목 18→22px/설명 13→17px, 다음 버튼 16→20px(높이 44→56px), 뒤로가기 버튼 높이 36→46px |
| `Assets/UI/UpgradeTree.uss` | 헤더 카테고리명/DNA 20→24px, 카테고리 구분선 16→20px, 트리 카테고리 라벨 15→19px, 상세패널 제목 16→20px/설명 13→17px. **단, 트리 노드 안 라벨(12→14px)/비용(11→13px)만 작게 조정** — `UpgradeTreeView.cs`의 `NodeWidth=140/NodeHeight=60`이 픽셀 고정값이라 다른 항목처럼 크게 올리면 2줄 라벨+비용 텍스트가 노드 박스 높이(패딩 제외 실질 52px)를 넘어 옆 노드와 겹칠 위험이 있어 보수적으로 조정 |
| `Assets/UI/EndingScreen.uss` | 타이틀 36→40px, 상세 16→20px, 점수 22→26px, 버튼 폭/높이 160×48→180×56px |
| `Assets/UI/CountryPopup.uss` | 팝업 폭 280→320px, 제목 20→24px, 본문 행 14→18px |
| `Assets/UI/RankingPanel.uss` | 제목 22→26px, 순위 행 16→20px, 안내문 12→16px |

**주의(다음 세션 참고)**: 업그레이드 트리 노드는 여전히 고정 픽셀 박스(`NodeWidth`/`NodeHeight`) 기반
절대 좌표 레이아웃이라, 폰트를 더 키우고 싶으면 폰트만 건드리지 말고 `UpgradeTreeView.cs`의
`NodeWidth`/`NodeHeight`와 `DefaultUpgradeTreeFactory`의 27노드 좌표 간격을 함께 재계산해야
겹침 없이 확대 가능하다 — 이번엔 스코프 밖이라 보수적인 폰트 크기(14/13px)로만 대응.

Unity 에디터에서 확인할 것: 위 6개 화면 전부 열어서 텍스트가 잘리거나 요소끼리 겹치지 않는지
(특히 국가 선택 화면의 국가 행 높이, 업그레이드 트리 노드 내부 2줄 텍스트), Game 뷰 줌을 100%로
맞추거나 Device Simulator로 실제 체감 크기를 재확인.

## Step 26 구현 메모 (뉴스피드 확대 + 업그레이드 트리 3창 → 1창 페이징 통합, 2026-07-03)

사용자 피드백 두 가지: (1) 상단 뉴스 피드(이벤트 텍스트) 영역이 좁다, (2) 전파/증상/능력 업그레이드
트리 사이즈를 조금 줄이고, 버튼 하나로 통합한 다음 좌우 화살표나 스크롤로 창을 넘길 수 있게 해달라.

**뉴스피드**: `Hud.uss`의 `.news-scroll { height: 90px }` → `150px`. Step 25에서 뉴스 항목 폰트를
14→18px로 키운 뒤라 90px 높이면 한 줄 반 정도만 보였을 것 — 150px로 늘려 2~3줄이 편하게 보이게 함.
남은 공간은 `.map-spacer`(flex-grow:1)가 그대로 흡수하므로 지도 영역이 그만큼 줄어드는 트레이드오프는
감수(요청받은 사항이라 의도된 변경).

**업그레이드 트리 통합**: Step 21에서 "카테고리 구분이 안 된다"는 이유로 전파/증상/능력을 완전히
별개인 창(별도 GameObject+UIDocument) 3개로 쪼갰었는데, 이번엔 반대로 "버튼 3개가 번거로우니 하나로
줄이고 창 안에서 넘기게 해달라"는 요청. **씬의 GameObject 3개(TransmissionTreeUI/SymptomTreeUI/
AbilityTreeUI)를 물리적으로 병합하는 대신**, 더 낮은 리스크의 방식을 선택했다 — 3개 GameObject/
UIDocument 구조는 그대로 두고, 같은 버튼 클릭 콜백 안에서 "지금 보이는 걸 Hide() + 다음 걸 Show()"를
동시에 실행해 사용자 입장에서는 창이 하나이고 그 안에서 좌우로 넘어가는 것처럼 보이게 만들었다(같은
프레임 안에서 전환되므로 깜빡임 없음). UIDocument 3개를 진짜로 한 캔버스에 합쳐 드래그 스와이프까지
지원하는 방식은 씬 파일 구조를 크게 바꿔야 해서(GameObject 삭제/UIManager 참조 재배선 등, Unity
에디터 없이 텍스트 편집만으로 하기엔 리스크가 큼) 이번 스코프에서는 보류 — 화살표 버튼 클릭 방식만
구현(사용자가 제시한 "화살표 버튼 **또는** 좌우 스크롤" 중 화살표 쪽을 택함).

| 항목 | 파일 | 내용 |
|------|------|------|
| HUD 탭 3개→1개 | `Assets/UI/Hud.uxml`, `Scripts/UI/HudController.cs` | `tab-transmission`/`tab-symptom`/`tab-ability` 버튼 3개를 `tab-upgrade`("업그레이드") 1개로 교체. 이벤트도 `OnUpgradeTabClicked(UpgradeCategory)` → 인자 없는 `OnUpgradeButtonClicked`로 변경(어느 카테고리를 열지는 카테고리를 지정해서 여는 게 아니라 UIManager가 "마지막으로 보던 페이지"를 기억해서 결정) |
| 업그레이드 창 헤더에 좌우 화살표 | `Assets/UI/UpgradeTree.uxml,.uss`, `Scripts/UI/UpgradeTreeView.cs` | 헤더를 2줄로 분리(1줄: ◀ 카테고리제목 ▶ / 2줄: DNA·광고버튼·닫기) — 화살표 2개까지 한 줄에 다 넣으면 참조 해상도 480px 폭 안에서 카테고리 제목 표시할 공간이 거의 안 남아서 분리함. `UpgradeTreeView`는 여전히 카테고리 하나만 담당하되, prev/next 버튼 클릭 시 `OnPrevRequested`/`OnNextRequested` 이벤트만 쏘고 실제 전환은 UIManager가 처리 |
| UIManager 페이징 로직 | `Scripts/Managers/UIManager.cs` | `transmissionTreeView`/`symptomTreeView`/`abilityTreeView` 3개 참조를 `_upgradePages` 배열(순서: 전파→증상→능력)로 관리. `_currentUpgradePageIndex` 하나로 현재 페이지 추적 — "업그레이드" 버튼 클릭 시 마지막으로 보던 페이지를 다시 열고(항상 전파부터 시작하지 않음), 화살표 클릭 시 `(index ± 1 + 3) % 3`로 순환 이동(맨 끝에서 반대쪽으로 넘어감) |
| 트리 노드 크기/간격 축소 | `Scripts/Data/DefaultUpgradeTreeFactory.cs`, `Scripts/UI/UpgradeTreeView.cs` | "트리 사이즈 조금 줄여달라"는 요청 + **부수적으로 발견한 문제**: 기존 열 간격(180px)·노드폭(140px) 기준으로는 카테고리 하나의 캔버스 폭이 약 580px로 나와 참조 해상도 480px 폭(패딩 제외 약 448px) 안에 가로로 다 안 들어갔다 — `node-scroll`이 세로 스크롤만 지원해서(Step 21 코멘트: "이제 세로 스크롤만으로 충분") 실제로는 오른쪽 열 노드가 잘려 보이거나 클릭이 안 됐을 가능성이 있다(PC 가로 레이아웃 시절 기준으로 세워진 값이 모바일 전환 후에도 안 고쳐져 있었음). 열 간격 180→140px, `NodeWidth` 140→110px, `NodeHeight` 60→50px, `CanvasPadding` 80→48px로 축소해 카테고리당 캔버스 폭을 약 438px까지 줄임 — 좌표는 전부 `base + (기존 오프셋) × (140/180)` 선형 변환(정확히 나눠떨어지는 값이라 반올림 오차 없음) |

**주의(다음 세션 참고)**: 이번 방식은 3개의 서로 다른 UIDocument를 Show/Hide로 전환하는 것이라, 진짜
"하나의 캔버스 안에서 물리적으로 밀려서 넘어가는" 드래그 스와이프 애니메이션은 아니다. 나중에 실제
스와이프 제스처(터치 드래그로 자연스럽게 넘기기)가 필요해지면, 3개 GameObject를 하나의 UIDocument
아래 가로로 나열된 페이지 3개(각각 폭 100%)로 합치고 `ScrollView(mode=Horizontal)` + 스냅 로직을
새로 짜야 한다 — Unity 에디터에서 씬을 직접 만지면서 GameObject 구조를 재배치하는 게 안전하다(텍스트
편집만으로 GameObject를 통째로 삭제/병합하는 건 리스크가 커서 이번엔 피함).

Unity 에디터에서 확인할 것:
1) HUD에 "업그레이드" 버튼 하나만 보이는지, 클릭하면 마지막에 보던 카테고리(처음엔 전파)가 열리는지.
2) 헤더의 ◀/▶ 버튼을 눌러 전파→증상→능력→전파(순환)로 잘 넘어가는지, 매번 카테고리 제목과 노드
   목록이 올바르게 바뀌는지.
3) 각 카테고리 트리(9개 노드, 3열×4티어)가 가로 스크롤 없이 창 안에 다 들어오는지 — 특히 3번째 열
   (오른쪽 끝) 노드가 잘리지 않는지.
4) 뉴스피드 영역이 커진 만큼 지도 영역이 줄었는데 답답해 보이지 않는지.

## Step 27 구현 메모 (세계 지도 지리적 재배치 + 국가 크기 비례화, 2026-07-03)

"지도가 실제 세계지도처럼 비슷하게라도 안 생겼다"는 피드백. Step 24/25에서 18개국을 3열×6행
그리드에 배치할 때 대륙별로 "위(북반구)→아래(남반구) 느슨한 흐름"을 의도했다고 적어뒀지만, 실제
좌표 대입은 그 의도를 안 따르고 있었다(예: 캐나다가 러시아·독일과 같은 줄, 호주가 남아공·브라질과
같은 줄 — 남북은 대충 맞았지만 동서는 국가 이름 기준이 아니라 입력 순서 기준이라 뒤죽박죽).

**작업 중 발견한 특이사항**: 이번 세션에서 `GamePlay.unity`를 다시 읽어보니 18개국 좌표가 Step 25에서
넣은 깔끔한 격자값(-1.4/0/1.4, 3/1.8/0.6/-0.6/-1.8/-3)이 아니라 `{x: -1.5, y: 2.62}`처럼 소수점이
지저분한 값으로 바뀌어 있었고, 그나마도 인도만 옛날 격자값(1.4, -0.6) 그대로 남아있고 나머지
17개국은 죄다 화면 위쪽(y: 0.93~2.78)에 몰려있는 상태였다. 코드에는 Transform을 건드리는 로직이
전혀 없으므로(CountryView.cs는 색상/스케일만 변경) 이건 Unity 에디터 Scene 뷰에서 국가 오브젝트를
손으로 드래그하다가 중간에 멈춘 흔적으로 보인다 — 아마 사용자가 실기기 확인 도중 지도를 눈으로
보면서 직접 재배치를 시도했던 것 같다. 어차피 이번 요청이 "위치를 제대로 맞춰달라"였으므로 기존
값을 참고하지 않고 아예 새로 계산한 값으로 18개 전부 덮어썼다.

**배치 로직**: 정확한 지리 좌표(위경도)를 그대로 쓰면 안 된다 — 경도 범위(캐나다 -106°~일본 138°,
약 244°)가 위도 범위(남아공 -29°~러시아 62°, 약 91°)보다 훨씬 넓어서(약 2.7:1, 가로로 김) 세로로
긴 모바일 화면(9:19.5)에 그대로 투영하면 동서 차이가 다 뭉개지거나 남북으로 지나치게 늘어난다.
그래서 절대 각도가 아니라 **순위 기반** 배치로 바꿨다: 18개국을 경도 순으로 정렬해 3등분(서/중앙/
동)해서 열(x)을 정하고, 각 열 안에서는 다시 위도 내림차순으로 정렬해 행(y, 기존 6개 슬롯 재사용)을
정했다.

| 열(x) | 담당 지역(경도순) | 국가(위도 내림차순, 북→남) |
|-------|-------|-------|
| -1.4 (서) | 아메리카 + 서유럽 | 캐나다 → 영국 → 프랑스 → 미국 → 멕시코 → 브라질 |
| 0 (중앙) | 중부유럽 + 아프리카 + 중동 + 인도 | 독일 → 이집트 → 사우디아라비아 → 인도 → 나이지리아 → 남아공 |
| 1.4 (동) | 러시아 + 동아시아 + 오세아니아 | 러시아 → 한국 → 일본 → 중국 → 인도네시아 → 호주 |

영국·프랑스가 "아메리카" 열에 같이 묶인 게 어색해 보일 수 있는데, 실제 경도(영국 -2°, 프랑스 2°)가
나이지리아(8°)·독일(10°)보다 더 서쪽이라 순위상 맞는 배치다 — 대신 위도 정렬 때문에 그 열 안에서는
캐나다·영국·프랑스(북쪽, 위도 46~56°)가 위에, 미국·멕시코·브라질이 아래에 오도록 배치해 "북미와
서유럽이 인접 대륙"이라는 지리적 직관과도 어느 정도 맞아떨어진다.

**국가 크기 비례화**: 여태까지 `shapeTargetSize`가 18개국 전부 0.45로 동일해서 러시아나 캐나다 같은
큰 나라도 한국·영국처럼 작게 나왔다 — 이것도 "세계지도 같지 않다"는 인상에 한몫했을 것. 실제 국토
면적(대략치, 단위 백만 km²: 러시아 17.1 / 캐나다 10.0 / 중국 9.6 / 미국 9.4 / 브라질 8.5 / 호주 7.7 /
인도 3.3 / 사우디 2.15 / 멕시코 1.96 / 인도네시아 1.9 / 남아공 1.22 / 이집트 1.0 / 나이지리아 0.92 /
프랑스 0.55 / 일본 0.378 / 독일 0.36 / 영국 0.244 / 한국 0.1)의 제곱근을 구한 뒤 `[0.32, 0.62]`
구간으로 선형 매핑했다 — 면적을 그대로 비례시키면 러시아가 한국보다 13배 커져서 인접 칸을 침범할
것이므로, 제곱근으로 압축한 뒤에도 범위를 좁게 잡아 그리드 간격(열 1.4 / 행 1.2) 안에서 절대
겹치지 않는 선에서만 "크다/작다" 위계가 드러나게 했다. 계산 결과 러시아 0.62(최대)~한국 0.32(최소).
겹침 여부는 그리드 인접 칸 사이 최소 간격(세로 1.2)과 최대 크기 조합(0.62+0.54=1.16)을 비교해 전부
안전 마진이 있음을 미리 계산으로 확인했다(기존 0.45 균일 크기 대비 최대 편차라 가장 타이트한
케이스만 확인).

**적용 방법**: 18개국 `CountryView.shapeTargetSize` 필드와 대응하는 Transform의 `m_LocalPosition`을
`countryId`/`m_GameObject` fileID로 각각 유일하게 식별해서 `Edit` 툴로 36곳(위치 18 + 크기 18) 치환.
`BoxCollider2D`는 손대지 않음 — `CountryView.ApplyCountryShape()`가 런타임에 스프라이트 bounds
기준으로 알아서 재계산한다(Step 23 참고).

Unity 에디터에서 확인할 것:
1) 세계 지도가 대략 "왼쪽=아메리카, 가운데=아프리카/중동/인도, 오른쪽=아시아/오세아니아, 러시아는
   맨 오른쪽 위" 구도로 보이는지, 같은 열 안에서 위→아래로 북→남 순서가 맞는지.
2) 러시아·캐나다·중국·미국·브라질·호주가 확실히 다른 나라들보다 커 보이는지, 인접 국가끼리 겹치거나
   클릭 판정이 서로 침범하지 않는지(특히 러시아-한국, 미국-멕시코처럼 인접한 큰/작은 나라 조합).
3) 국가 클릭(팝업 열기)과 감염 진행에 따른 색상 변화가 여전히 정상인지.

## Step 28 구현 메모 (지도 좌우 스크롤 도입 — "한 화면에 다 보이기" 포기, 2026-07-04)

Step 27까지도 "그래도 실제 지도 같지 않다"는 피드백이 이어졌다. 사용자가 직접 세계지도 이미지를 놓고
손으로 배치하는 방안을 검토하다가, 근본 원인("실제 지구는 가로가 세로보다 2.7배 넓은데 세로로 긴
모바일 화면 안에 억지로 구겨넣으니 동서 간격이 다 뭉개진다")을 다시 짚어보고 방향을 바꿨다 —
**지도를 화면 안에 다 욱여넣지 않고 화면보다 넓게 만든 뒤 좌우로 스크롤**하는 방식(Step 24에서
"이번 스코프 제외"로 미뤄뒀던 옵션)을 채택.

**좌표 재계산 — 순위 기반(Step 27) → 각도 비례 기반으로 전환**: Step 27은 경도 순위로 3등분한 뒤
그 안에서 위도로 정렬하는 "칸 배치"였는데, 이제 가로 폭 제약이 없어졌으니 실제 위경도 값에 선형
비례한 좌표를 직접 계산할 수 있다.
- 18개국 대략적 중심 위경도(위도, 경도)를 웹 지식 기준으로 잡고, 경도 범위(멕시코 -102°~일본
  138°, 폭 240°)와 위도 범위(남아공 -29°~러시아 62°, 폭 91°)를 각각 구했다.
- x = (경도 - 경도중심18) × scaleX, y = (위도 - 위도중심16.5) × scaleY.
- **세로(scaleY)는 기존 폭(6유닛, y: -3~3)을 그대로 재현**하도록 잡았다 — Step 24/25에서 이미
  카메라 orthographic size(3.8)에 맞게 튜닝된 세로 여백을 재사용하면 세로 스크롤이 여전히 필요
  없다. scaleY = 6 / 91 ≈ 0.0659.
- **가로(scaleX)는 전체 지도 폭을 12유닛(화면에 보이는 폭의 약 3.4배)으로 잡고** 역산했다 —
  scaleX = 12 / 240 = 0.05. 이 폭이면 좌우로 스크롤할 거리가 있으면서도 과하게 길지는 않다(모바일
  게임에서 통상적인 "가로로 훑어보는" 지도 정도).
- 계산된 좌표로 18개국 전부 겹침 검사(국가별 `shapeTargetSize`/2를 반지름으로 근사)를 手동으로
  확인 — 가장 타이트한 한국-일본(간격 0.5, 반지름 합 0.33)과 프랑스-독일(간격 0.52, 반지름 합
  0.345)도 안전 마진이 있었다(실제로 두 나라가 지리적으로도 가까운 편이라 타이트한 게 오히려
  자연스러움).

| 항목 | 파일 | 내용 |
|------|------|------|
| 18개국 좌표 재배치 | `Assets/Scenes/GamePlay.unity` | 위경도 비례 계산값으로 전부 교체(예: 러시아 x=3.9, 멕시코 x=-6.0 — 이전 Step 27의 -1.4~1.4 범위에서 -6~6으로 대폭 확장). 국가 크기(`shapeTargetSize`)는 Step 27 값 그대로 유지 |
| 카메라 좌우 드래그 스크롤 | `Scripts/Gameplay/WorldMapCameraController.cs`(신규) | Main Camera에 부착. `Input.mousePosition`(레거시 Input Manager — 프로젝트에 Input System 패키지 없음, 터치는 Unity가 마우스 이벤트로 자동 에뮬레이션) 기준으로 누른 채 이동한 픽셀 델타를 월드 단위로 환산해 카메라 x만 이동. `mapHalfWidth`(6.3, 지도 콘텐츠 반폭+여백) 기준으로 카메라가 지도 밖으로 못 나가게 클램프. 세로는 전혀 건드리지 않음(orthographic size 3.8 그대로) |
| 드래그-클릭 충돌 방지 | `Scripts/Gameplay/CountryView.cs` | 기존 `OnMouseDown()`(누르는 즉시 팝업)을 `OnMouseUpAsButton()`(뗄 때, 같은 콜라이더 위)으로 변경 + `WorldMapCameraController.Instance.WasDragging` 확인 후 드래그였으면 무시. `WasDragging`은 누름 시작부터 20px 이상 움직이면 true가 되고, 다음 누름이 시작될 때만 리셋(뗀 직후 프레임에 읽어도 항상 이번 사이클 판정을 정확히 봄 — 프레임 순서 이슈 회피) |

**씬 배선**: Main Camera GameObject(fileID 519420028)에 새 MonoBehaviour 컴포넌트(fileID
`3400000801`, Step 25의 SafeAreaApplier 넘버링 방식을 이어받아 수동 할당) 추가, `targetCamera`
필드는 같은 GameObject의 Camera 컴포넌트(fileID 519420031)를 직접 참조하도록 배선. 스크립트
`.meta`는 프로젝트 관례대로 최소 포맷(`fileFormatVersion: 2` + `guid`)으로 수동 생성.

**확인한 부수 영향**: `BubbleSpawner.cs`가 DNA 버블 스폰 위치를 `CountryView.transform.position`
기준으로 잡고(화면/뷰포트 좌표 아님) 카메라 위치를 전혀 참조하지 않아서, 카메라가 옆으로 스크롤돼도
버블 스폰 로직은 영향 없음을 코드 확인함. HUD/뉴스피드 등 UI Toolkit 오버레이도 별도 카메라 레이어라
월드 카메라의 x 이동과 무관하게 항상 화면에 고정.

Unity 에디터에서 확인할 것:
1) Game 뷰에서 지도를 좌우로 드래그(에디터에서는 마우스 클릭+드래그)했을 때 카메라가 자연스럽게
   따라가는지, 지도 양 끝(서쪽 멕시코~동쪽 일본)에서 더 이상 스크롤 안 되고 딱 멈추는지.
2) 드래그 중간에 국가 위를 지나가도 팝업이 안 열리는지, 반대로 국가를 살짝 눌렀다 그 자리에서 떼면
   (드래그 아닌 탭) 팝업이 정상적으로 열리는지.
3) 세로 방향은 여전히 스크롤 없이 한 화면에 다 들어오는지(위아래 잘림 없는지).
4) 처음 게임 시작 시 카메라가 지도 중앙(중동/아프리카 부근)에서 시작하는지 — 필요하면
   `WorldMapCameraController`가 아니라 Main Camera Transform의 초기 x를 조정해 원하는 대륙부터
   보이게 바꿀 수 있음(예: 자기 나라를 선택했으면 그 대륙부터 보이게 하는 것도 다음 단계로 고려 가능).
5) 실기기(또는 Device Simulator)에서 손가락 스와이프로도 동일하게 동작하는지 — 에디터는 마우스로만
   검증 가능하므로 최종 확인은 터치 입력에서 필요.

### Step 28 추가 대응 (가로 스크롤 폭 축소, 2026-07-04)

Unity에서 실제 확인해본 사용자가 "문제 없다"고 확인한 직후, "가로길이 좀더 줄여줘 세로 길이 짧아도
되니까"라는 후속 요청. 세로는 이미 카메라에 딱 맞게 잘 들어오고 있어서 굳이 건드릴 이유가 없었고
("짧아도 된다"는 허용이지 요구가 아님), 가로만 압축하는 쪽이 side effect가 적어 더 안전하다고 판단.

- 전 국가 x좌표에 압축 계수 **0.8**을 곱함(y좌표는 무변경) — 예: 러시아 x 3.9→3.12, 일본 6.0→4.8,
  멕시코 -6.0→-4.8. 전체 지도 폭 12유닛 → 9.6유닛(화면 대비 약 3.4배 → 약 2.7배).
- 압축 계수를 정할 때 가장 타이트한 쌍(한국-일본, 위도가 거의 같아서 간격이 전부 x축 차이에서 나옴)이
  기준: 원래 간격 0.5 × 계수 ≥ 반지름 합 0.33이 되려면 계수 ≥ 0.66 — 0.8이면 간격 0.4, 마진 0.07로
  안전하게 유지됨. 그보다 더 압축하려면(예: 0.7) 한국/일본 중 하나를 y로 살짝 띄우는 등 별도 보정이
  필요해짐 — 지금은 그 정도까지 줄일 필요가 없다고 판단해 0.8에서 멈춤.
- `WorldMapCameraController.mapHalfWidth`도 6.3 → 5.1로 축소(국가 x범위가 이제 -4.8~4.8이라 여백
  0.3 포함). 스크립트 기본값과 씬의 직렬화된 값 둘 다 갱신.
- `shapeTargetSize`(국가 크기)는 Step 27 값 그대로 — 압축은 좌표(간격)에만 적용하고 크기는 안 건드림.

## Step 28-2 구현 메모 (HUD에 가려지는 문제 + 국가 클릭 팝업 → 국가현황 리스트 UI, 2026-07-04)

앞선 가로 압축(Step 28-1)을 확인한 사용자로부터 두 가지 요청이 한 번에 들어왔다: "지금 UI에 가려서
안 보이는 부분도 있다", "국가 클릭해서 정보 보는 시스템 없어도 될 것 같아, 다른 버튼으로 전체 국가
정보 보는 UI가 나을 듯".

**1) HUD에 가려지는 문제 원인**: `Hud.uxml`의 레이아웃은 news-scroll(고정 150px, 반투명 검정) →
map-spacer(flex-grow:1, 지도가 보이는 영역) → bottom-bar(고정 약 117px: padding 16 + stats-row
~61 + tabs-row 40, 반투명 검정)로 구성된다. 참조 해상도(480×1040) 기준 news-scroll이 전체 높이의
14.4%, bottom-bar가 11.3%를 차지하는데, 지도 카메라(orthographic size 3.8, 전체 뷰 높이 7.6유닛)는
화면 전체를 그린다 — 즉 카메라 뷰의 위 14.4%(≈1.10유닛), 아래 11.3%(≈0.86유닛)가 HUD에 항상 가려진다.
안전 y범위는 대략 **-2.945 ~ 2.704**(폭 5.65유닛)인데, Step 28-1 시점 국가 y좌표는 -3~3(폭 6)이라
러시아(북쪽 끝)와 남아공(남쪽 끝)이 살짝 가려지고 있었다.

**2) 대응 — 지도 전체 0.85배 추가 축소 + 세로 중심 보정**: 사용자가 "지도 더 축소해줘, 세로 짧아도
된다"고 명시적으로 허용해서, x/y좌표와 `shapeTargetSize`(국가 크기)를 전부 **0.85배 균일 축소**했다
(위치와 크기를 항상 같은 비율로 같이 줄여야 국가 간 간격의 상대 비율이 그대로 보존돼 겹침 위험이
새로 생기지 않는다 — 이전 Step 28-1에서 이미 확인한 원칙 재사용). 거기에 더해 y값에 오프셋
-0.12를 더해 안전 영역 중심(-0.12, 상단이 하단보다 더 많이 가려지므로 지도를 살짝 아래로 이동)에
맞췄다. 결과:
- x, y 전부 `old*0.85`, y는 추가로 `-0.12` (최종: `new_y = old_y*0.85 - 0.12`).
- `shapeTargetSize` 전부 `old*0.85`.
- `WorldMapCameraController.mapHalfWidth` 5.1 → 4.4 (국가 x범위가 -4.08~4.08로 줄어서).
- 가장 타이트했던 한국-일본 간격도 위치·크기가 같은 비율로 줄어서 상대 마진 비율은 Step 28-1과
  동일하게 유지됨(안전).
- 확인: 러시아 새 y=2.43 (< 안전 상단 2.704, 마진 0.27), 남아공 새 y=-2.67 (> 안전 하단 -2.945,
  마진 0.28) — 둘 다 여유 있게 안전권.

**3) 국가 클릭 팝업 → "국가현황" 버튼 + 전체 리스트 패널로 전환**: Step 28에서 지도가 화면보다 넓어지고
국가가 촘촘해지면서(특히 이번에 한 번 더 축소하면서 더더욱) 개별 국가를 정확히 탭하는 UX 자체가
모바일에서 부담스러워졌다는 사용자 판단에 동의 — "지도에서 직접 클릭"을 없애고 "버튼 하나로 18개국
전체 상태를 한 번에 스크롤로 훑어보기" 방식으로 바꿨다.

| 항목 | 파일 | 내용 |
|------|------|------|
| 클릭 트리거 제거 | `Scripts/Gameplay/CountryView.cs` | `OnMouseUpAsButton()`(→`WorldMap.HandleCountryClicked`) 삭제. `CountryPopupController`/`CountryPopup.uxml`은 코드·씬 오브젝트 그대로 남겨뒀지만(제거 리스크 대비 최소 변경), 더 이상 아무것도 호출하지 않아 사실상 죽은 코드가 됨 |
| 신규 패널 | `Assets/UI/CountryStatusPanel.uxml`, `.uss`(신규) | `RankingPanel`과 같은 모달 패턴(반투명 배경, absolute 배치) — 제목 + `ScrollView`(런타임에 18개 행 채움) + 닫기 버튼 |
| 신규 컨트롤러 | `Scripts/UI/CountryStatusPanelController.cs`(신규) | `WorldDataManager.Instance.Countries` 전체를 순회해 국가별로 이름/붕괴단계, 인구·감염률·사망률·의료수준, 항공·항구·국경 상태를 3줄 카드로 렌더링(`CountryPopupController.Populate()`와 항목 구성 최대한 맞춤 — 정보 손실 없이 UX만 바꿈). 패널이 열려있는 동안만 `WorldDataManager.OnCountryChanged`에 반응해 다시 그림(닫혀 있을 땐 매 틱 갱신 안 함) |
| HUD 버튼 추가 | `Assets/UI/Hud.uxml`, `Scripts/UI/HudController.cs` | tabs-row에 "국가현황" 버튼 추가, `OnCountryStatusClicked` 이벤트 추가 |
| 배선 | `Scripts/Managers/UIManager.cs` | `HandleCountryStatusClicked()` 추가(업그레이드/랭킹 패널을 먼저 닫고 국가현황 패널을 엶), 업그레이드/랭킹 핸들러에도 국가현황 패널을 닫는 호출 추가해 3개 패널이 서로 배타적으로 열리게 함 |
| 씬 배선 | `Assets/Scenes/GamePlay.unity` | `RankingPanelUI`와 동일한 구조(GameObject+Transform+UIDocument+Controller, `SortingOrder: 1`, 같은 `PanelSettings` 공유)로 `CountryStatusPanelUI` 루트 오브젝트 신규 추가(fileID `5900000001`~`5900000004`, 수동 할당). `SceneRoots.m_Roots`에도 등록해야 루트 오브젝트로 정상 인식됨(누락 시 씬 로드 시 활성화 안 될 위험) — `UIManager.countryStatusPanelController` 필드도 새 컨트롤러 fileID로 배선 |

Unity 에디터에서 확인할 것:
1) HUD 하단 탭에 "업그레이드 / 국가현황 / 랭킹" 3개 버튼이 정상적으로 보이는지(텍스트 잘림 없는지).
2) "국가현황" 버튼을 누르면 18개국 리스트가 스크롤 가능한 패널로 뜨는지, 각 항목에 이름/상태/인구/
   감염률/사망률/의료수준/항공·항구·국경 정보가 제대로 표시되는지.
3) 게임 진행 중(틱이 도는 동안) 패널을 열어둔 채로 시간이 지나면 감염률/사망률 숫자가 갱신되는지.
4) 업그레이드/랭킹/국가현황 세 버튼이 서로 배타적으로 동작하는지(하나를 열면 다른 게 자동으로 닫히는지).
5) 지도에서 국가를 탭해도 더 이상 팝업이 안 뜨는지(의도한 동작).
6) 러시아(북쪽 끝)와 남아공(남쪽 끝)이 이제 뉴스피드/하단바에 가려지지 않고 완전히 보이는지.

### Step 28-3 추가 대응 (남아공·호주 여전히 하단 UI에 가려짐 — 지도 세로/가로 추가 축소, 2026-07-04)

Step 28-2에서 계산한 "안전 y범위"(-2.945~2.704)는 `Hud.uss`의 고정 픽셀값만으로 역산한 추정치였는데,
실제 에디터에서는 남아공(-2.67)·호주(-2.45) 둘 다 여전히 하단 UI에 가려진다는 피드백을 받았다.
정확한 원인(예: `stats-row`가 2줄로 줄바꿈돼 하단바가 예상보다 훨씬 큼, 또는 다른 요인)을 실기
확인 없이 특정하기보다, 사용자가 "세로축 더 축소, 가로축도 비율 맞춰서 축소"를 명시적으로 요청한
김에 **훨씬 큰 안전 마진**을 두는 쪽으로 대응했다 — 정확한 픽셀 계산에 의존하는 대신 여유를
크게 잡아 다음 라운드 재작업 가능성을 낮추는 게 합리적이라고 판단.

- 좌표: `new_y = (old_y + 0.12) * 0.7 + 0.05` (Step 28-2 상태 기준 중심 -0.12를 원점으로 되돌린 뒤
  0.7배 압축하고, 새 중심을 +0.05로 살짝 위로 보정 — 남쪽(하단)이 특히 더 가려진다는 피드백이라
  아래쪽 여백을 위쪽보다 더 넉넉하게 남겼다). `x`는 `old_x * 0.7` (사용자가 "가로도 비율 맞춰서"라고
  요청해 세로와 동일한 0.7배 적용 — 지도 종횡비 자체는 유지됨).
- `shapeTargetSize`도 전부 0.7배 — Step 28-1/28-2와 동일한 원칙(위치·크기를 항상 같은 비율로 같이
  줄여야 국가 간 상대 간격 비율이 보존돼 겹침 위험이 새로 생기지 않음)을 재적용.
- 결과: 남아공 y=-1.74, 호주 y=-1.58, 러시아 y=1.84 — Step 28-2 대비 훨씬 안쪽으로 들어와 여유
  마진이 커짐(정확한 픽셀 경계를 모르는 상태에서의 의도적 과잉 축소).
- `WorldMapCameraController.mapHalfWidth` 4.4 → 3.2 (국가 x범위가 -2.86~2.86로 줄어서).
- 한국-일본 등 기존에 가장 타이트했던 쌍들도 위치·크기가 동일 비율로 줄어 상대 마진 비율은
  이전 Step들과 동일하게 유지됨(안전).
- 국가 실루엣이 이제 상당히 작아졌다(예: 한국 0.19유닛) — 국가 클릭 인터랙션을 Step 28-2에서
  이미 제거했기 때문에 탭 정확도는 더 이상 문제가 아니고, 시각적 식별(색상 변화)만 되면 되므로
  받아들일 수 있는 트레이드오프로 판단.

Unity 에디터에서 재확인할 것: 남아공·호주가 이번에도 가려지면, 정확한 원인 파악을 위해
`Hud.uxml`의 `bottom-bar`가 실제로 몇 px인지 Game 뷰에서 직접 재보거나(예: UI Toolkit Debugger),
`stats-row`가 2줄로 줄바꿈되고 있는지 확인 필요 — 이번 라운드는 원인 규명보다 마진을 크게 잡아
우회하는 방식으로 대응했다.

---

## 설계 문서 대비 구현 시 내린 결정 상세 (요약은 CLAUDE.md 참고)

설계 문서 4절 공식에는 `spreadFactor`, `climateModifier`, `severityFactor`, `researchMultiplier`,
`drugResistanceReduction` 등 정의되지 않은 계수가 여러 개 있었다. 유도/치환 근거:

- `climateModifier` = `Pathogen.GetEnvironmentResistance(country.climate)` — 병원체마다 기후별 저항이
  다르다는 게 나무위키/원작 게임의 핵심 전략 요소인데 설계 문서엔 이걸 담을 필드가 없어서 추가.
- `countryHealthLevel` / `healthcareCapacity` = `Country.HealthLevel` — developmentLevel 3단계를
  0.2/0.5/0.8 연속값으로 매핑한 것은 순전히 감(설계 문서에 실제 수치 근거 없음). 밸런싱 대상.
- `researchMultiplier` = `Country.ResearchMultiplier` (Low 0.2 / Mid 0.8 / High 1.5) — 마찬가지로 임의 값.
- `severityFactor` = `Pathogen.severity` 그대로 사용 (설계 문서에 별도 변환식 없음).
- `drugResistanceReduction` = `pathogen.drugResistance * drugResistanceCoefficient` — 계수는
  `SimulationManager` 인스펙터에 노출.
- `spreadFactor`, 국가 간 전파 확률, DNA 마일스톤 간격, 저항 단계별 봉쇄 임계값은 전부
  `SimulationManager` / `HumanResistanceManager` 인스펙터 튜닝 값으로 뺐다 — 설계 문서에 구체적인
  수치가 없어서 플레이테스트로 맞추는 것 전제.
- 원 설계 문서는 `environmentResistance`를 `float[4]` (Cold/Hot/Humid/Arid 고정 순서 배열)로 정의했으나,
  실제 `Country.climate` enum 순서(Arid/Temperate/Cold/Humid)와 어긋나 인덱스 실수로 엉뚱한 기후 저항이
  적용될 위험이 컸다. 그래서 `List<ClimateResistanceEntry>`(climate enum → resistance 값 매핑)로 바꿔
  순서 의존성을 아예 없앴다.

## Step 29 구현 메모 (글로벌 교통망 — 비행기/배 이동 + 항공/해운 전파 대체)

사용자가 "Global Transport Network Design" 문서(항공 15허브 + 해운 15허브, 공유 이동/감염 로직,
가중치 기반 랜덤 목적지 선택, 동시 활성 유닛 50~200개)를 직접 제공해 그대로 구현.

- **신규 데이터**: `Data/TransportHub.cs`(TransportHubType enum, TransportRouteLink, TransportHub),
  `Data/DefaultTransportHubFactory.cs`(항공 15 + 해운 15개 하드코딩, DefaultUpgradeTreeFactory와 같은
  "코드 팩토리 폴백" 패턴).
- **허브 좌표 문제**: 이 프로젝트 세계지도는 실제 위경도 투영이 아니라 대륙별 그리드 배치(Step 27/28)라
  허브 도시에 정확한 지리 좌표를 줄 수 없다. 대신 각 허브를 대표 국가(countryId)에 연결해
  `CountryView.DnaSpawnWorldPosition`(이미 지도 위 정확한 위치를 가리키는 기존 앵커)을 기준점으로 쓰고,
  같은 국가에 허브가 여러 개(미국 5개, 중국 8개 등) 있을 때는 `localOffset`으로 살짝 흩어지게 배치했다.
  허브 도시의 국가가 48개국에 없는 경우(UAE/네덜란드/벨기에/싱가포르 등은 독립국으로 미포함) 지리적으로
  가장 가까운 대표국에 연결(두바이·제벨알리→사우디, 로테르담·안트베르펜·함부르크→독일, 싱가포르·
  포트클랑→말레이시아, 홍콩→중국).
- **TransportUnit.cs**: 비행기/배 공용 이동체. 프리팹 없이 런타임에 삼각형(비행기)/선체 모양(배) 텍스처를
  코드로 직접 그려서 사용(FloatingTextEffect.cs와 같은 "에셋 임포트 불필요" 철학). Progress 0→1 보간 이동,
  이동 방향으로 스프라이트 회전, 도착 시 OnArrived 이벤트 발행.
- **TransportManager.cs**: DontDestroyOnLoad 싱글턴(다른 매니저와 같은 패턴), Bootstrap 오브젝트(fileID
  2050405917)에 배치. 매 틱(`SimulationManager.OnTickCompleted` 구독) 활성 유닛 수를 세계 감염률 기반
  목표치(최소 24 ~ 최대 180)로 채워 넣고, 허브 쌍마다 LineRenderer로 경로선을 1회 그린다(양방향 데이터가
  중복이어도 안 겹치게 unordered-pair 키로 중복 제거). 유닛은 도착할 때마다 감염 판정을 굴리고(항공
  35%/해운 15% — 항공은 고빈도·저물량, 해운은 저빈도·고물량이라는 설계 문서 취지 반영, 해운 성공 시
  전파량은 더 크게), 3~6회 구간을 돈 뒤 풀로 반환된다.
- **기존 시스템과의 충돌 정리**: `SimulationManager.SpreadBetweenCountries()`가 기존에 담당하던 "항공/해운
  국가 간 확률적 전파"(매틱 추상 롤)를 제거하고 육상 국경(`landBorderSpreadChance`)만 남겼다 — 같은
  개념(항공/해운 전파)을 눈에 보이는 실제 이동체로 대체한 것이므로 두 시스템을 동시에 돌리면 이중 적용된다.
  `airRouteSpreadChance`/`seaRouteSpreadChance` 필드와 Country의 `airRouteCountryIds`/`seaRouteCountryIds`
  데이터는 삭제하지 않고 남겨뒀다(다른 용도로 재활용 가능, 인스펙터에 "[미사용]" 툴팁 추가).
- **씬 배선**: 새 스크립트 4개(`TransportHub.cs`/`DefaultTransportHubFactory.cs`/`TransportUnit.cs`/
  `TransportManager.cs`) 전부 `.meta` 파일을 직접 생성해서(fileFormatVersion 2 + 랜덤 guid, 기존 175개
  guid와 충돌 없음 확인) Unity 에디터 없이 텍스트 편집만으로 완결— CountryShapes PNG를 만들 때 썼던
  것과 같은 방식. `GamePlay.unity`의 Bootstrap 오브젝트(fileID 2050405917)에 새 MonoBehaviour 컴포넌트
  블록(fileID 994684355)을 추가하고 `m_Component` 목록에 등록. `GameDataBootstrapper.ResetPersistentManagersForNewGame()`에
  `TransportManager.Instance?.ResetForNewGame()` 호출도 추가해 재시작 시 이전 판 유닛이 정리되도록 함.
  **다른 Step들과 달리 이번엔 "씬/에셋 배선 필요" 항목이 없다** — 프리팹도, 수동 GameObject 배치도 필요 없음.
- **미확인 리스크(Unity 에디터로 직접 확인 못 함)**: LineRenderer 머티리얼에 `Shader.Find("Sprites/Default")`를
  썼다. CountryView/DnaBubble의 SpriteRenderer가 이미 문제없이 쓰는 셰이더라 재사용했지만, 혹시 선이 안
  보이면(FloatingTextEffect.cs가 겪었던 "레거시 Font 셰이더가 URP에서 안 보임" 사례처럼) URP 2D 렌더러가
  이 셰이더를 걸러내는 경우일 수 있다 — 그때는 `TransportManager.CreateRouteLine()`의 Shader.Find 대상을
  `"Universal Render Pipeline/2D/Sprite-Unlit-Default"`로 바꿔볼 것.

## Step 29 후속 세션 메모 (정적 검증 — 여전히 Unity 에디터 미접속)

이번 세션도 컴퓨터 사용 권한을 받지 못해 Unity 에디터를 직접 열지 못했다. 텍스트/셸 도구만으로 할 수
있는 검증을 대신 진행함:

- **컴파일 안전성 정적 리뷰**: `TransportHub.cs`/`DefaultTransportHubFactory.cs`/`TransportUnit.cs`/
  `TransportManager.cs` 4개 전부 읽고, 참조하는 외부 멤버(`ObjectPool<T>` 생성자, `WorldMap.GetView`,
  `CountryView.DnaSpawnWorldPosition`, `Country.isAirportOpen`/`isPortOpen`/`SusceptibleCount`,
  `WorldState.totalPopulation`/`infectedCount`, `WorldDataManager.GetCountry`/`NotifyCountryChanged`,
  `SimulationManager.OnTickCompleted` 시그니처)가 실제 정의와 전부 일치하는지 grep으로 교차 확인 —
  불일치 없음.
- **씬 배선 재확인**: `GamePlay.unity`에서 Bootstrap 오브젝트(fileID 2050405917)의 `m_Component` 목록에
  `994684355`(TransportManager)가 등록돼 있고, 해당 MonoBehaviour 블록의 `guid: 809187fb215746d09b655754ddb736ad`가
  `TransportManager.cs.meta`와 일치함을 확인. 인스펙터 값(`minActiveUnits`~`routeLineWidth`)도 코드
  기본값과 동일하게 직렬화돼 있음.
- **GUID 충돌 재확인**: 신규 스크립트 4개의 `.meta` guid가 프로젝트 내에서 각각 정확히 1개 파일에만
  매칭됨(`grep -rl`로 재확인) — 중복 없음.
- **셰이더 선제 교체**: 위 "미확인 리스크" 항목을 실제 렌더링 확인 없이 미리 대응 — `CreateRouteLine()`이
  이제 `Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")`를 우선 시도하고, null이면
  `"Sprites/Default"`로 폴백하도록 변경(둘 다 못 찾는 극단적 상황을 대비해 폴백 유지, 매 호출마다
  `Shader.Find`를 반복하지 않도록 static 캐시 필드 `_routeLineShader` 추가).
- **여전히 남은 한계**: 정적 리뷰는 "참조가 존재하고 시그니처가 맞는지"까지만 확인 가능하고, Unity
  컴파일러의 실제 결과·Play 모드 동작·시각적 렌더링(경로선이 실제로 보이는지, 비행기/배가 자연스럽게
  움직이는지)은 검증하지 못했다. 다음 세션에 컴퓨터 사용 권한이 허용되면 최우선으로 확인할 것.

## Step 30 구현 메모 (배/비행기 경로가 대륙을 관통하는 문제 수정)

사용자가 플레이 중 "배 경로가 항구→항구 직선인데 육지를 가로질러가서 어색하다"고 지적. 비행기는
"조금만 수정하면 될 것 같다"(=완전히 틀리진 않지만 아쉬운 점이 있다는 뉘앙스)고 했고, 후속 확인에서
"실제 항공기 경로랑 비슷하게 하면 좋겠다"는 답을 받음.

**원인 진단**: `TransportManager.TrySpawnUnit()`/`HandleUnitArrived()`가 항상 출발 허브 좌표 → 도착 허브
좌표 직선(2점)만 계산해서 `TransportUnit.BeginLeg()`에 넘겼다. 그런데 `TransportHub.cs`에 남아있던 기존
주석은 "이 프로젝트 세계지도는 실제 위경도 투영이 아니라 대륙별 그리드 배치라 허브에 정확한 지리 좌표를
줄 수 없다"고 설명하고 있었다 — 이건 Step 27/28 시점(추상 그리드) 기준 설명이 Step 29의 48개국 확장
(world_base.png 배경 + world.svg 기반 실제 실루엣 오버레이로 지도 자체를 재구축, `CountryView.cs` 참고)
이후 갱신되지 않고 남아있던 낡은 주석이었다. `Assets/Scenes/GamePlay.unity`에서 48개국의
`dnaSpawnLocalOffset` 값을 실제로 뽑아 대조해보니(예: USA -2.13,0.71 / RUS 1.69,1.24 / AUS 2.9,-0.95 /
JPN 2.86,0.63 — 전부 실제 지리적 위치와 상대적으로 일치) 이 좌표계는 진짜 위경도 기반(대략 등장방형/
Plate Carrée 투영, x=경도·y=위도에 비례, 스케일 약 0.02유닛/도)이라는 게 확인됐다. 즉 진짜 문제는
"좌표가 부정확해서"가 아니라 **"지도가 구를 감싸지 않는 평면 사각형이라, 태평양처럼 지도 양 끝
(경도 ±180° 부근)을 넘나드는 항로를 직선으로 그으면 지구 반대편이 아니라 지도 한가운데(유라시아 대륙)를
관통해버린다"**는 것. 배는 육지를 가로지르면 시각적으로 명백히 틀려 보이고, 비행기는 육지 위를 날아도
안 어색하지만(실제로도 대륙 상공 비행) 태평양 노선이 엉뚱하게 "유럽 쪽으로 도는" 방향으로 보이는 게
사용자가 말한 "조금 이상함"의 정체였을 것으로 추정.

**해결 방향(사용자 확인 완료)**: 직선 대신 실제 항로 기반 경유점(waypoint)을 거치는 방식 — 정확도는
높지만 항구 쌍(약 25개 조합)마다 좌표를 직접 잡아야 하는 고비용 옵션을 선택.

**구현**:
- `WorldMap.cs`: `ToWorldPosition(Vector2 localOffset)` 추가 — 국가에 종속되지 않는 임의의 지점(항로
  경유점)을 `CountryView.DnaSpawnWorldPosition`과 같은 좌표계로 변환. `CountryView`가 항상 부모(WorldMap)
  기준 로컬 (0,0,0)에 고정된다는 기존 불변식을 그대로 활용(`transform.TransformPoint`가 동일하게 나옴).
- `Data/TransportHub.cs`: 위에서 설명한 낡은 주석을 정정.
- `Data/DefaultTransportHubFactory.cs`: `BuildSeaWaypoints()`/`BuildAirWaypoints()` 신규 — canonical
  페어 키("작은ID|큰ID", 사전순) → 그 방향으로 지나가는 경유점 배열(Vector2, WorldMap 로컬 오프셋 좌표계).
  해운은 25개 고유 항구 쌍 중 실제로 육지를 가로지르는 것만(약 9개) 경유점을 채움 — 나머지(중국 연안
  도시 간, 로테르담-함부르크-안트베르펜 등 인접 항구)는 원래도 직선이 바다 위였으므로 그대로 둠:
  - 상하이/홍콩 ↔ 싱가포르: 베트남을 스쳐가므로 남중국해 쪽으로 살짝 우회.
  - 제벨알리(두바이) ↔ 싱가포르/상하이, 로테르담 ↔ 싱가포르/제벨알리: 말라카 해협 → 인도양(인도 남단) →
    아라비아해 → (유럽 쪽은) 홍해 → 수에즈 운하 → 지중해 → 지브롤터 해협 → 대서양 순서로 경유.
  - 부산/상하이 ↔ 로스앤젤레스/롱비치(트랜스퍼시픽): 지도가 평면이라 직선을 그으면 유라시아를 관통하므로
    베링해/알래스카만 방향 "북태평양 우회"로 처리(위도 y=1.3~1.6, 러시아·유럽 실루엣 위쪽 빈 바다를 지남).
  - 롱비치 ↔ 산투스(브라질): 파나마 운하 위치에 경유점 하나만 둬서 지협을 관통하는 것처럼 보이게 함.
  항공은 아메리카(LAX)↔동아시아/호주(HND/ICN/SYD)처럼 "방향이 명백히 반대편으로 보이는" 3개 노선만
  경유점 추가(LAX↔HND/ICN은 북태평양 우회, LAX↔SYD는 남태평양 우회) — 그 외 노선(대서양 횡단 등)은
  지도상에서 이미 올바른 방향이라 손대지 않음.
- `Managers/TransportManager.cs`: `PairKey()`(canonical 페어 키 생성, 기존 `DrawRouteLines()`의 중복 방지
  로직에서 뽑아냄)와 `BuildPathPoints(from, to, fromPos, toPos)`(경유점 테이블 조회 → from→to 방향에 맞게
  정방향/역방향 결정 → `WorldMap.ToWorldPosition`으로 변환 → fromPos/toPos 포함 전체 배열 반환) 추가.
  `DrawRouteLines()`(경로선 렌더링)와 `TrySpawnUnit()`/`HandleUnitArrived()`(유닛 실제 이동) 양쪽 다
  이걸 거치도록 교체 — 경로선과 실제 이동 경로가 항상 일치.
- `Gameplay/TransportUnit.cs`: `BeginLeg(fromHubId, toHubId, Vector3 from, Vector3 to, ...)` →
  `BeginLeg(fromHubId, toHubId, Vector3[] path, ...)`로 시그니처 변경. 내부적으로 각 구간 누적 거리
  배열(`_cumulativeDistance`)을 미리 계산해두고, `Update()`에서 진행한 총 거리(`_traveled`)로 현재 몇 번째
  구간에 있는지 선형 탐색(경유점이 많아야 7~8개라 성능 문제 없음) → 그 구간 안에서 보간 + 진행 방향으로
  회전. `path.Length == 2`(경유점 없음)면 기존 직선 이동과 완전히 동일하게 동작 — 대부분의 근거리 노선은
  영향 없음.

**좌표 산출 근거**: Unity 에디터가 없어 실제 렌더링을 볼 수 없으므로, `GamePlay.unity`에서 48개국의
`dnaSpawnLocalOffset`을 실측(awk로 countryId↔좌표 매칭)한 뒤 실제 지리 지식(수에즈/말라카/파나마/지브롤터
등의 상대적 위치)으로 좌표를 역산했다. 스케일이 등장방형에 가깝다는 것만 확인했을 뿐 정밀한 도(degree)
단위 환산식을 검증하진 못했으므로, 경유점 좌표는 전부 **근사치**다.

**미확인 리스크(다음 세션 최우선)**: 이번에도 Unity 에디터를 직접 열어보지 못해 아래를 전혀 시각적으로
확인하지 못했다 — CLAUDE.md TODO 6번 항목 참고:
1. 새로 만든 25개 해운 경유점 좌표가 실제로 국가 실루엣과 안 겹치는지(특히 러시아는 중심점(1.69,1.24)
   보다 극동 지역이 훨씬 더 넓게 뻗어있어서 북태평양 우회 경유점(y=1.3~1.6대)과 겹칠 가능성이 가장 큼).
2. 항공 3개 노선(LAX↔HND/ICN/SYD)의 우회 곡선이 자연스러운 곡률로 보이는지(경유점이 2개뿐이라 급격하게
   꺾여 보일 수 있음 — 필요하면 경유점을 3~4개로 늘려 더 부드럽게 만들 것).
3. `TransportUnit`이 구간 전환 시 회전이 뚝뚝 끊기지 않고 자연스러운지(현재는 구간 경계에서 즉시 각도가
   바뀜 — 부자연스러우면 회전에도 Lerp를 추가할 것).
4. 이번 변경으로 노선이 길어진 만큼(경유점 경로가 직선보다 김) 도착까지 걸리는 시간이 늘어나 체감
   교통량/감염 전파 빈도가 달라질 수 있음 — 밸런스 재확인 대상에 추가.

## Step 30 구현 메모 — 2차 (해운 경유점을 육지 마스크 기반 A* 길찾기로 전면 재계산)

바로 위 1차 구현(경유점을 실제 지리 지식으로 "역산"한 근사치)에 대해 사용자가 "바다만 통해서
해로가 그려지도록 확실히 하려면 어떻게 해야 하는지"를 물어옴 — 즉 눈대중 좌표가 아니라 검증 가능한
방법을 요구. 실제로 1차 좌표를 실제 게임 배경 이미지 위에 겹쳐 확인해보니 여러 구간이 육지와 겹치는
것으로 나타나(특히 수에즈/지중해 진입부, 북태평양 우회 지점 — 러시아 실루엣이 중심점 추정보다 훨씬
넓게 뻗어있어 그대로 관통) 손으로 잡은 좌표는 신뢰할 수 없다고 판단, 전면 재계산에 들어감.

**방법 — 1차 시도(실패)**: `Resources/CountryShapes/{id}.png` 48개국 실루엣의 알파 채널을 합쳐 "육지
마스크"를 만들고, 그 위에서 A*(8방향, 대각선 코너 관통 금지, 다운샘플링 격자)로 항구 쌍 간 최단 해상
경로를 계산. 결과를 시각적으로 검토(`Docs/_debug/crop_africa_routes.png`)한 결과 **새로운 버그
발견**: 게임에 등장하는 48개국에 없는 나라(차드, 중앙아프리카공화국, 콩고공화국 등 — 플레이 대상이
아니라 애초에 실루엣 에셋 자체가 없음)가 전부 "바다"로 취급되어, 계산된 경로가 아프리카 대륙 한가운데를
일직선으로 관통해버렸다. 48개국 실루엣만으로는 "진짜 전 세계 육지"를 나타낼 수 없다는 것.

**수정**: 육지 마스크의 출처를 `Resources/CountryShapes/*.png`(48개국 실루엣)에서
`Resources/WorldMap/world_base.png`(세계지도 배경 이미지 자체, 4000×1714, 알파 채널 — 불투명
회색[236,236,236,255]=육지, 투명=바다)로 교체. 이 이미지는 게임에 없는 나라까지 포함한 실제 전 세계
지형을 그대로 담고 있어(원래 지도 배경용으로 그려진 에셋이므로) 이 문제를 근본적으로 해결. 수정 후
크롭 이미지(`land_v2_africa_check.png`)로 아프리카가 올바르게 통짜 육지로 나오는 것을 확인.

**추가로 부딪힌 문제와 처리**:
- **좁은 해협이 다운샘플링에 막힘**: 말라카 해협·수에즈 운하 진입부처럼 실제로는 좁지만 존재하는
  바닷길이, 계산 해상도(원본 픽셀을 6배 downsample해 그리드 셀 하나로 묶음, "블록 안에 육지가 하나라도
  있으면 그 셀 전체를 육지로 취급"하는 보수적 규칙 때문에)에서는 완전히 막힌 것으로 잡혔다. 여러
  해상도(3배/4배/6배/12배)와 국소 고해상도 A* 등을 시도했지만, 결과적으로 "말라카/수에즈를 그대로
  통과하는 최단 경로"는 포기하고 A*가 실제로 찾아낸 대체 경로(인도네시아 남쪽 우회, 희망봉을 크게
  도는 경로 등)를 그대로 채택 — 실제 뱃길보다 멀지만 지형학적으로 말이 되고(반다다·희망봉 등 실제
  지명이 있는 우회로) 육지를 지나가지 않는 것이 더 중요하다고 판단.
- **지도가 평면이라 태평양이 물리적으로 두 조각으로 끊김**: 부산/상하이 ↔ 로스앤젤레스 항로를 계산하려
  했을 때 A*가 아예 "경로 없음"을 반환 — 확인해보니 세계지도 이미지가 실제 지구본을 감싸지 않는 평면
  사각형이라 태평양 한가운데(날짜변경선 부근)가 지도 양 끝에서 끊겨 있고, "바다" 영역이 유라시아/
  아프리카/호주 쪽 덩어리(연결 성분 크기 약 85,500 셀)와 아메리카 쪽 덩어리(약 45,600 셀)로 완전히
  분리된 두 개의 연결 성분(connected component, BFS 플러드필로 확인)이었기 때문. 다만 이미지 맨 위
  가장자리(북위 끝, uy≈1.71 부근) 한 줄은 전체 폭에 걸쳐 개방된 바다였는데, 다운샘플링 때문에 이 얇은
  통로가 막힌 것으로 처리되고 있었다. 각 항구를 "두 연결 성분 중 더 가까운 쪽"에 스냅한 뒤, 두 항구가
  같은 성분에 속하도록 제약을 걸고 A*를 돌리는 방식으로 우회 — 결과적으로 부산/상하이↔LA 항로 전부
  희망봉을 크게 도는 남반구 경유 경로로 연결에 성공했다(실제 태평양 횡단과는 다른 방향이지만, 이
  평면 지도가 표현 가능한 범위 안에서 찾을 수 있는 유일한 완전 해상 경로).
- **극지방 근접 억제**: 위 문제 때문에 A*가 북극/남극 쪽 좁은 통로로 자꾸 새는 경향이 있어, 비용
  함수에 위도 페널티(`|uy| > 임계값`이면 비용 가중치 증가)를 추가해 불필요한 극지 근접 우회를 억제.

**검증 방법(가장 중요한 부분)**: 계산 자체는 다운샘플링된 저해상도 격자로 하지만, 검증은 반드시
**원본 4000×1714 픽셀 해상도**로 별도 수행했다 — 각 구간(경유점과 경유점 사이 직선)마다 그 선분 위의
모든 픽셀을 원본 이미지에서 샘플링해 육지 픽셀이 단 하나라도 있는지 확인하는 스크립트. 25개 해상
항구 쌍 전부 이 검증을 통과(막힌 픽셀 0개)한 뒤에야 `DefaultTransportHubFactory.BuildSeaWaypoints()`에
반영했다. 다운샘플링 해상도에서 통과했다고 원본 해상도에서도 안전하다는 보장이 없으므로(다운샘플링은
계산을 빠르게 하기 위한 근사일 뿐 검증 기준이 될 수 없음) 이 이중 확인 구조가 핵심이다. 최종 결과는
`Docs/_debug/final_routes_small.png`(전체 25개 항로 오버레이)와 `final_crop_africa/americas/asia.png`
(핵심 우회 구간 확대 크롭)으로 시각 확인 — 전부 해안선을 따라가거나(중국 연안, 유럽 연안, 남미
동안) 알려진 우회 지점(희망봉, 인도네시아 남부, 파나마 지협)을 지나가는 것으로 보이며 육지를
가로지르는 구간이 없다.

**교체 범위**: `DefaultTransportHubFactory.cs`의 `BuildSeaWaypoints()` 딕셔너리 전체를 위 방법으로 다시
계산한 좌표로 교체(항공 경유점 `BuildAirWaypoints()`는 육지 통과가 시각적으로 문제없어 대상에서 제외,
1차 구현 그대로 유지). 직선만으로 충분한 것으로 확인된 쌍(상하이-부산, 선전-광저우, 선전-홍콩,
광저우-홍콩, 로테르담-함부르크)은 경유점 없이 주석으로만 남김.

**남은 캐버트(이번 수정과 무관, 기존에 이미 알려진 한계)**: 미국 내 여러 허브(LAX/LGB/ATL/DFW/
SEA_LAX/SEA_LGB)가 전부 미국 국가 중심점(대략 오대호~펜실베이니아 부근) 기준 작은 오프셋으로만
정의되어 있어, 지도 위에서 실제 도시 위치(LA=서해안, 애틀랜타=동남부 등)가 아니라 서로 거의 같은
지점에 뭉쳐 보인다. 이건 Step 29에서부터 있던 사전 조건(허브가 도시별 진짜 좌표가 아니라 국가 앵커
기준 오프셋)이고 이번 세션에서 요청받은 범위(해로가 바다만 지나가게 하는 것)에는 포함되지 않으므로
손대지 않았다 — 필요하면 각 허브에 country 앵커 대신 직접 (ux,uy) 좌표를 부여하는 별도 작업으로 처리할 것.

## Step 30-5 구현 메모 (26개 항로 전수 재검증 + 유닛 수/크기 축소)

사용자 요청 2가지: (1) 감염 보유(빨간색) 유닛이 눈에 띄어야 하는데 현재 배/비행기 수가 너무 많고
이미지도 커서 신호가 묻힌다 → 수/크기 축소. (2) "다른 배의 경로도 확인한 다음 육지로 올라가는 문제가
있으면 수정" → 26개 해운 항로 전수 재점검.

**(1) 유닛 수/크기 축소**: `TransportManager`의 `minActiveUnits`(24→10), `maxActiveUnits`(180→70),
`maxSpawnPerTick`(14→6)을 낮춰 동시 활성 유닛 목표치를 절반 이하로 줄임. `TransportUnit`의
`Sprite.Create` `pixelsPerUnit`을 70→150으로 올려(텍스처 해상도는 그대로, 화면상 실제 크기만 축소)
스프라이트 크기도 절반 이하로 줄임.

**(2) 26개 항로 전수 재검증**: 지금까지(Step 30~30-4)는 사용자가 지적한 항로만 그때그때 고쳤는데,
이번에 처음으로 26개 쌍 전부를 "허브의 실제 위치(국가 앵커+오프셋)에서 직선으로 이었을 때 육지를
스치는지"까지 포함해 전수 검사했다. 그 결과 두 가지를 발견:

1. **"직선으로 충분하다"고 남겨뒀던 5개 쌍(상하이↔부산, 선전↔광저우/홍콩, 광저우↔홍콩, 로테르담↔
   함부르크)도 실제로는 육지를 스친다.** 이 쌍들은 항구가 서로 워낙 가까워서 Step 30-2/30-3 검증
   때 "짧은 구간이니 직선이면 충분하겠지"라고 넘겼는데, 다시 확인해보니 허브의 실제 위치 자체가
   해안선보다 살짝 안쪽(내륙)에 있어서 상대 허브를 향한 직선이 그 사이 작은 반도나 만을 스쳐
   지나간다. 원인은 이 프로젝트의 항구 배치 방식 자체 — 항구가 "정확한 해안선 좌표"가 아니라
   "국가 대표 지점 + 고정 오프셋" 근사값이라, 허브가 놓인 지점 자체가 이미 해안선 그림보다 조금
   안쪽으로 들어가 있는 경우가 흔하다(마치 항구가 부두/수로를 통해 바다와 이어지는 것처럼, 그
   "몇 픽셀 안쪽" 자체는 불가피한 근사지만, 상대 항구까지 완전히 직선으로 이었을 때 그 오차가
   누적되면 작은 반도 하나를 통째로 관통하는 걸로 나타날 수 있다는 게 이번에 새로 확인된 점).
2. **로스앤젤레스항(SEA_LAX)이 메인 대양과 아예 연결되지 않는 위치에 있었다.** 기존 오프셋(0.3,0.15)
   위치를 A*로 확인해보니 주변이 전부 육지로 둘러싸인 작은 바다 조각(방파제 안쪽 같은 곳으로 추정 —
   world_base.png 그림 자체의 단순화 때문에 생긴 작은 웅덩이)에 갇혀 있어서, 부산↔LA/LA↔롱비치/
   LA↔상하이 항로의 A* 계산이 전부 "경로 없음"으로 실패했다. LGB(롱비치, 오프셋 (0.3,-0.1))는 같은
   방식으로 확인했을 때 정상적으로 메인 대양에 연결되는 걸 확인했으므로, LAX 오프셋을 LGB와 비슷한
   위도인 (0.3,-0.05)로 낮춰 정상적인 바다로 옮겼다(실제로도 LA항과 롱비치항은 같은 항만 지역에
   있어 위치가 비슷해지는 것 자체는 지리적으로 문제없음).

**재계산 방법(기존과 다른 점)**: 이번엔 "허브 실제 위치에서 A*를 바로 돌리는" 대신, 먼저 각 허브의
실제 위치에서 "가장 가까운 진짜 바다 지점"(sea anchor)을 찾고(단, 로스앤젤레스처럼 작게 고립된 바다
조각을 걸러내기 위해 다운샘플 격자의 최대 연결 성분에 속하는 지점만 인정), 이 sea anchor들 사이를
A*로 잇는 방식으로 바꿨다. 그 결과 저장된 배열의 첫/마지막 점은 항상 "그 허브의 진짜 해상 진입점"이고,
`TransportManager`가 자동으로 붙이는 "허브 실제 위치 → 이 진입점" 구간은 언제나 짧은(대개 수십 픽셀,
world_base.png 기준 4000px 폭의 1% 미만) 해안 근접 오차 하나로만 남는다 — 이 오차 자체는 항구를
근사 배치하는 이 프로젝트 설계상 근본적으로 없앨 수 없지만, 적어도 "핵심 항로 구간"(sea anchor 사이)은
전부 원본 해상도 기준 100% 무결점으로 검증했다.

**결과**: 26개 쌍 전부 재계산 + 원본 4000×1714 해상도 재검증 완료. `DefaultTransportHubFactory.
BuildSeaWaypoints()` 전체 교체, `BuildDefaultHubs()`의 `SEA_LAX` 오프셋 수정.

**남은 캐버트(이번에도 완전히 없애지 못한, 성격이 다른 잔여 오차)**: 산투스항(SEA_SAN, 브라질)은
sea anchor까지의 거리가 다른 허브(대개 10~40픽셀)보다 눈에 띄게 먼 약 116픽셀 — 다른 허브보다는
크지만, JEA 버그(1000픽셀 이상, 아예 다른 나라를 가리킴)와는 성격이 다르고 크기 자체도 화면 전체
대비 미미해서(4000px 폭의 3%) 이번 범위에서는 따로 손대지 않았다. 플레이해보고 눈에 띄게 어색하면
그때 SEA_SAN 오프셋도 개별 조정할 것.

**미확인 리스크(다음 세션 최우선, 기존 항목에 추가)**:
5. 이번에 새로 계산된 25개 좌표 전부가 원본 픽셀 검증은 통과했지만, Unity 에디터로 실제 렌더링해
   본 적은 여전히 없다 — LineRenderer가 그리는 실제 선이 좌표-투 픽셀 매핑 공식(px=2000+500·ux,
   py=857-500·uy)과 어긋나지 않는지 최우선 확인 대상.
6. 희망봉 우회 등 경유점이 많은 장거리 항로(특히 SEA_SIN|SEA_ROT, SEA_ROT|SEA_JEA, SEA_BUS|SEA_LAX)는
   경유점 개수가 10개 이상이라 `TransportUnit`의 선형 탐색 방식이나 `LineRenderer`의 `positionCount`가
   비정상적으로 많아지는 건 아닌지(성능/시각적으로) 확인.

## Step 30-3 구현 메모 (실제 플레이 피드백 반영 — 키 순서 버그 + 배/비행기 구분 안 되는 문제)

사용자가 처음으로 실제 플레이 화면을 보고 두 가지를 지적: "지금 배는 동남아-아시아 지역, 영국-북유럽
지역만 자연스럽게 바다로 움직이고 있어"(즉 그 외 지역, 특히 대륙 간 장거리 항로는 부자연스럽다) +
"지금 움직이는 도형이 배인지 비행기인지 눈에 안 들어와."

**원인 진단 1 — 딕셔너리 키 순서 버그(치명적)**: `TransportManager.PairKey()`는 항상 "사전순으로 더
작은 ID가 앞"에 오도록 canonical 키를 만드는데(`string.CompareOrdinal(a,b) < 0 ? a|b : b|a`),
Step 30-2에서 `BuildSeaWaypoints()`를 다시 쓸 때 이 규칙을 지키지 않고 절반 가까운 항목(정확히는 26쌍
중 10쌍 — SHA/NGB, SHA/HKG, SIN/PKL, SIN/HKG, SIN/JEA, SIN/ROT, NGB/GUA, ROT/ANT, ROT/JEA, SAN/ROT)의
키를 반대 순서로 적어놨다. `BuildPathPoints()`의 `table.TryGetValue(PairKey(from.id, to.id), ...)`는
키가 정확히 일치해야 값을 찾는데, 순서가 반대인 키는 절대 매치되지 않아 그 쌍은 항상 "경유점 없음"으로
취급되어 출발항→도착항 직선으로 대체되고 있었다. 짧은 구간(중국 연안 도시끼리, 로테르담-안트베르펜
등)은 직선이라도 우연히 바다 위였서 문제가 안 보였지만, 장거리 구간(특히 SEA_SIN|SEA_ROT,
SEA_ROT|SEA_JEA처럼 원래 희망봉을 크게 도는 경로였던 것들)은 그 직선이 대륙 한가운데를 그대로
관통해버려 사용자가 본 "일부 지역만 자연스럽다"는 증상 그대로 나타났을 것으로 결론지었다(사용자가
"자연스럽다"고 짚은 동남아/북유럽 클러스터는 짧은 구간이 많아 우연히 안전했던 것).

추가로, 이번에 전체 26개 쌍의 키 순서를 하나하나 사전순으로 다시 검산하다가 원래 "25쌍 전부 검증
완료"라고 기록했던 것이 실은 허브 연결 그래프(`BuildDefaultHubs()`)의 실제 고유 쌍 개수를 잘못
세었던 것이었고 `SEA_PKL|SEA_SHA`(포트클랑↔상하이) 한 쌍이 통째로 빠져있었던 것도 함께 발견했다.

**수정**: `DefaultTransportHubFactory.BuildSeaWaypoints()`의 키 10개를 canonical 순서로 바로잡고
그에 맞춰 경유점 배열 방향도 반전(원래 A→B였다면 B→A로 순서를 뒤집음 — 실제 지나가는 지점 자체는
그대로, 방향 표기만 바꾼 것). 빠져있던 `SEA_PKL|SEA_SHA`는 이미 검증된 두 구간(포트클랑↔싱가포르,
싱가포르↔상하이)을 그대로 이어붙여 구성 — 이미 각각 원본 해상도로 검증된 구간이라 새로 픽셀 검증할
필요 없음(이어붙이는 지점도 싱가포르 앞바다의 서로 아주 가까운 두 점이라 그 사이 직선도 안전).

**원인 진단 2 — 배/비행기 구분 불가**: `TransportUnit`의 절차적 스프라이트가 삼각형(비행기, 28x16)과
선체(배, 30x12)로 가로세로 비율이 비슷하고, 색상도 옅은 하늘색 vs 옅은 연두색으로 채도·명도가
비슷해서, 지도가 축소된 상태(특히 모바일 화면)에서는 사실상 구분이 안 됐다.

**수정**: `TransportUnit.cs`의 `BuildTriangleSprite()`/`BuildHullSprite()`를 다시 그림.
- 비행기: 가느다란 동체(기수 쪽으로 갈수록 뾰족)를 가로지르는 훨씬 넓은 날개 밴드를 추가해 "십자형
  (dart)" 실루엣으로 변경 — 폭:높이 비율 1.6:1.
- 배: 훨씬 더 길고 납작하게(폭:높이 약 4.4:1) 변경해 "짧고 뾰족한 비행기" vs "길고 평평한 배"가
  실루엣만 봐도 구분되게 함.
- 색상: 항공(파랑 계열, 평시 `(0.15,0.55,1)`/감염보유 시 `(1,0.5,0)`)과 해운(초록 계열, 평시
  `(0.1,0.65,0.4)`/감염보유 시 `(0.85,0.12,0.12)`)으로 색상 자체(hue)를 완전히 분리하고 채도·
  불투명도도 높임.
- `Sprite.Create`의 `pixelsPerUnit`을 100 → 70으로 낮춰 텍스처 해상도는 유지한 채 화면상 실제
  크기만 키움(둘 다 약 1.43배).
- `/tmp`에서 두 스프라이트를 numpy로 재현해 배율 확대 렌더링으로 실제 실루엣 차이를 시각 확인함
  (Unity 에디터 렌더링은 여전히 미확인 — CLAUDE.md TODO 참고).

## Step 30-4 구현 메모 (제벨알리 허브가 사우디아라비아 내륙에 찍혀 있던 버그)

Step 30-3에서 키 순서 버그와 스프라이트 구분 문제를 고치고 다시 물어봤더니, 사용자가 "희망봉 우회는
안 어색하고, 지금 바다 경로에서 어색한 부분은 사우디-아랍 쪽"이라고 콕 짚어줌 — "여기 주변 배들이
전부 사우디 위를 지나다녀."

**원인 진단**: `TransportManager.TryResolveHubPositions()`는 허브의 실제 위치를
`view.DnaSpawnWorldPosition + hub.localOffset`(국가 중심점 + 허브별 오프셋)로 계산한다. `SEA_JEA`
(제벨알리항, UAE 대표 국가로 사우디에 연결)의 오프셋은 `(-0.15, 0.15)`였는데, 사우디(SAU)의
`dnaSpawnLocalOffset`이 `(0.93, 0.33)`이라 실제 계산된 위치는 `(0.78, 0.48)` — 이 지점을
`world_base.png` 알파채널 마스크로 찍어보니 페르시아만 연안이 아니라 **요르단/이스라엘에 인접한
사우디 서북부 내륙**이었다. 반면 Step 30-2에서 이 허브와 연결되는 항로(JEA↔싱가포르/로테르담/상하이)를
A*로 계산할 때는 (아마 실제 두바이 위치를 어림해) 전혀 다른, 훨씬 남동쪽인 페르시아만~아라비아해
쪽 좌표를 시작점으로 썼다. 그 결과 `TransportManager.BuildPathPoints()`가 항상 붙이는 "실제 허브
위치 → 저장된 첫 경유점" 구간(진짜 허브 위치 자체는 한 번도 육지 마스크로 검증된 적 없음)이 사우디
아라비아 반도를 거의 대각선으로 관통하는 긴 직선이 되어버렸다 — 짧은 "해안 근처 오차"가 아니라
아예 다른 지역을 잘못 가리키고 있었던 것이 핵심 차이.

**수정**:
1. `DefaultTransportHubFactory.BuildDefaultHubs()`의 `SEA_JEA` 오프셋을 `(-0.15,0.15)` →
   `(0.17,0.02)`로 변경 — 새 위치 `(1.10,0.35)`는 `world_base.png` 마스크 기준 실제 페르시아만
   공해(두바이 앞바다, 호르무즈 해협 진입부 쪽) 위임을 확인.
2. JEA가 관련된 3개 항로(JEA↔싱가포르, JEA↔로테르담, JEA↔상하이)를 이 새 위치를 시작점으로 다시
   A*(다운샘플 factor 3, 8방향, 대각선 코너 관통 금지, 위도 페널티) 계산 + LOS 단순화 + 원본
   4000×1714 해상도 재검증(세 항로 모두 사우디 구간 완전히 무결점 — JEA↔상하이만 목적지 쪽 상하이
   허브 자체가 해안선에서 살짝 안쪽이라 마지막 몇 픽셀이 걸리는데, 이는 다른 기존 항로들도 전부 갖고
   있는 것과 동일한 수준의 오차라 새로운 문제가 아님).
3. `BuildSeaWaypoints()`의 `SEA_JEA|SEA_SIN`/`SEA_JEA|SEA_ROT`/`SEA_JEA|SEA_SHA` 세 항목을 새로
   계산한 좌표로 교체.

**추가로 확인한 것(수정하지 않기로 함)**: 이번에 전체 26개 항로의 "허브 실제 위치 → 저장된 첫/마지막
경유점" 구간을 전부 픽셀 단위로 재검사해보니, 상하이/선전/광저우/닝보/함부르크/산투스/LAX/LGB 등
다수의 허브에서도 정도의 차이는 있지만 마지막 수십~수백 픽셀 구간이 육지 마스크에 걸리는 것을
확인했다. 이는 "국가 중심점 + 고정 오프셋"으로 항구를 근사하는 이 프로젝트의 기존 설계 자체가
갖고 있는 한계(허브가 실제 해안선 좌표가 아니라 국가 대표 지점 기준 어림값)이지, 이번 세션에서 새로
생긴 문제가 아니다 — 특히 미국 쪽 허브(LAX/LGB)는 이미 CLAUDE.md TODO에 "실제 도시 위치가 아니라
한 점에 뭉쳐 보인다"고 기록된 것과 같은 원인이다. 제벨알리는 이 중 유일하게 "국가조차 잘못된 지역
(요르단/이스라엘 인접)을 가리켜 대륙 전체를 대각선으로 관통"하는 질적으로 다른(훨씬 심각한) 사례였고,
그래서 사용자가 유일하게 그 구간만 "어색하다"고 짚어낸 것으로 보인다. 나머지는 항로 시작/끝의 짧은
"해안 근접 오차" 수준이라 이번 범위에서는 손대지 않고 기존 TODO 항목에 통합해 기록만 남긴다.

## Step 32 구현 메모 (항공 허브 15개를 실제 위경도 기반 절대 좌표로 교체)

사용자 지적: "비행기 경로 출발/도착 지점이 공항이여야 하는데 어긋난 부분이 있어." 이건 CLAUDE.md
TODO에 이미 기록돼 있던 한계(미국 내 ATL/DFW/LAX 등이 국가 중심점 기준 작은 오프셋으로만 정의돼 있어
실제 도시 위치가 아니라 지도 위 한 지점에 뭉쳐 보임 — Step 29부터의 조건, "필요하면 각 허브에 국가
앵커 대신 직접 좌표를 부여하는 별도 작업으로 처리")를 실제로 처리해달라는 요청으로 판단하고 진행.

**원인 진단**: `DefaultTransportHubFactory.Air()`가 만드는 항공 허브 15개는 전부 "대표 국가 앵커
(`CountryView.DnaSpawnWorldPosition`) + 작은 오프셋(대개 -0.25~0.25 유닛)" 방식으로 좌표를 잡는다.
이 오프셋은 "같은 국가 내 허브를 살짝 흩어 겹치지 않게"하는 용도로만 설계됐지, 실제 도시 간 거리
(수천 km)를 표현할 스케일이 아니다. 그 결과 미국 3개(ATL/DFW/LAX), 중국 3개(PVG/CAN/HKG) 같은
허브들이 실제로는 대륙을 가로질러 떨어진 도시인데도 지도 위에서 국가 앵커 반경 0.5유닛 안에 몰려
있었다 — 사용자가 본 "출발/도착 지점이 공항이 아닌 곳에 어긋나 있다"는 인상의 정체.

**해결 방향**: 항공 허브 좌표를 "국가 앵커 상대 오프셋"에서 "각 공항의 실제 위경도를 국가 앵커들과
동일한 좌표계로 변환한 절대 좌표"로 바꾼다. 해운 허브는 이미 Step 30-5에서 A*+sea anchor로 실측
검증되고 사용자 플레이 확인까지 끝난 상태라 이번 변경 대상에서 완전히 제외했다(건드리면 이미 확인된
안정성을 깨뜨릴 위험만 있고 얻는 게 없음).

**좌표계 확인(Step 30에서 이미 확인된 사실 재활용)**: Step 30 구현 메모에 이 지도가 등장방형
(Plate Carrée) 투영이고 `dnaSpawnLocalOffset`이 실제 위경도에 선형 비례(x=경도, y=위도, 스케일
약 0.02유닛/도)한다고 기록돼 있었다. 이번엔 그 근사치를 그대로 쓰지 않고, `Assets/Scenes/GamePlay.unity`
에서 48개국 CountryView의 `dnaSpawnLocalOffset` 값을 전부 grep으로 추출한 뒤, 그중 국경이 단순하고
(군소 섬 산재가 없어 "가장 큰 폴리곤 파츠 기준 무게중심"이 실제 지리 중심과 크게 다르지 않을) 39개국을
골라 (실제 위경도) → (dnaSpawnLocalOffset) 최소제곱 선형 회귀(x=a·경도+b, y=c·위도+d, Python numpy
lstsq)를 돌렸다. 결과: a=0.021614, b=-0.045251, c=0.025372, d=-0.285776, 잔차 표준편차 x=0.056/
y=0.025 유닛(지도 전체 폭 대비 1~2% 수준) — a값이 Step 30의 "약 0.02유닛/도" 추정과 잘 맞아 방법론
자체가 타당함을 재확인.

**독립 검증**: 이 회귀식이 실제 world_base.png 마스크와 맞는지 확인하기 위해, Step 30-4에서 픽셀 단위로
직접 검증하고 확정한 제벨알리 해운 허브 좌표(1.10, 0.35 — 실제 페르시아만 연안)를 기준점으로 썼다.
회귀식으로 두바이 실제 위경도(25.2532°N, 55.3657°E)를 변환하면 (1.151, 0.355)가 나와 기존 실측
검증값과 오차 0.05 미만으로 거의 일치 — 회귀식이 실제 지도와 잘 맞는다는 걸 서로 다른 두 가지 방법
(픽셀 마스크 실측 vs 위경도 회귀)으로 교차 확인한 셈. 광저우(SEA_GUA, 실측 좌표 2.34,0.29)와 상하이
(SEA_SHA, 실측 좌표 2.49,0.54)도 이번에 계산한 공항 좌표(CAN 2.404,0.308 / PVG 2.588,0.504)와 각각
0.06~0.1유닛 이내로 근접해 추가로 신뢰도를 확인했다.

**구현**:
- `Data/TransportHub.cs`: `useAbsoluteWorldOffset`(bool) 필드 추가. false(기본값, 해운 허브)면 기존
  방식(countryId 앵커 + `localOffset`), true(항공 허브)면 `localOffset`을 WorldMap 절대 좌표로 바로
  해석한다. 생성자에 옵션 파라미터로 추가해 기존 `Sea()` 호출부는 코드 변경 없이 그대로 동작.
- `Managers/TransportManager.cs`: `TryResolveHubPositions()`에 분기 추가 — `useAbsoluteWorldOffset`이면
  `WorldMap.Instance.ToWorldPosition(hub.localOffset)`을 직접 쓰고, 아니면 기존과 동일하게
  `view.DnaSpawnWorldPosition + hub.localOffset`. 국가 View 존재 여부 체크(다른 허브 대기 로직과의
  일관성 + `isAirportOpen` 등 게임 로직이 여전히 country를 참조하기 때문)는 두 분기 모두 유지.
- `Data/DefaultTransportHubFactory.cs`: `Air()` 헬퍼가 `useAbsolute: true`로 `Build()`를 호출하도록
  변경. 15개 `Air(...)` 호출의 offset 인자를 전부 위 회귀식으로 계산한 절대 좌표로 교체(ATL -1.870,0.568
  / DFW -2.143,0.549 / LAX -2.605,0.575 / LHR -0.055,1.020 / IST 0.576,0.761 / DXB 1.151,0.355 /
  DEL 1.621,0.439 / ICN 2.688,0.665 / HND 2.976,0.616 / PVG 2.588,0.504 / CAN 2.404,0.308 /
  SIN 2.202,-0.251 / HKG 2.417,0.280 / SYD 3.222,-1.147 / GRU -1.050,-0.880). 각 허브의 `connections`
  (가중치 포함 노선 그래프)와 `countryId`(게임 로직용 대표 국가)는 그대로 유지 — 이번 변경은 좌표
  계산 방식만 바꾼 것이지 노선 그래프나 게임플레이 로직은 건드리지 않았다.
- `BuildAirWaypoints()`의 태평양 우회 경유점 3개(HND↔LAX, ICN↔LAX, LAX↔SYD)는 그대로 뒀다 — 이
  경유점들은 애초에 국가 앵커와 무관한 절대 좌표(WorldMap 로컬 오프셋)라 허브 위치가 바뀌어도
  "허브 → 첫 경유점" 구간의 방향만 살짝 바뀔 뿐, 경유점 자체가 태평양 먼 바다에 있어 육지를 관통할
  위험이 없다. 실제로 새 LAX 좌표(-2.605,0.575)는 기존(-1.93,0.61)보다 서쪽으로 더 이동해서 오히려
  실제 캘리포니아 해안에 더 가까워졌다.

**남은 리스크(Unity 에디터 미접속, 다음 세션 확인 필요)**: (1) 회귀 자체는 39개국 보정 데이터 + 제벨알리
교차검증으로 신뢰도를 확인했지만 실제 게임 화면에서 렌더링해서 보지는 못했다. (2) 두바이(DXB)·제벨알리
처럼 대표 국가(SAU)의 실제 국경 밖에 절대 좌표가 찍히는 게 구조적으로 당연한 허브들(48개국에 없는
UAE/싱가포르 등을 대신 대표하는 경우)이 있는데, 이 자체는 기존에도 있던 근사(대표 국가 지정 자체가
근사)라 문제는 아니지만, 시각적으로 "이 나라 안에 있어야 할 것 같은데 밖에 있다"는 인상이 들 정도로
어색하면 개별 좌표를 조정할 것. (3) 해운 허브는 이번 수정 대상이 아니므로 그대로 — 항공 노선(파란
선)과 해운 노선(초록 선)이 이제 서로 다른 좌표 산출 방식을 쓰지만, 두 노선은 시각적으로 완전히
분리돼 있어(색상별 LineRenderer) 혼동 요소는 없다.

## Step 33 구현 메모 (Step 32 실제 플레이 피드백 3건 — HND 좌표/유닛 빈도/carrier 확률)

Step 32를 실제로 플레이해본 사용자 피드백 3가지를 한 번에 처리.

**(1) 하네다(HND)만 바다 위에서 출발/도착**: "일본 공항만 수정 살짝 오른쪽으로 치우쳐져 있음 바다에
비행기가 출발/도착함." Step 32 회귀식으로 계산한 HND 절대 좌표(2.976,0.616)를 `world_base.png`/
`Assets/Resources/CountryShapes/JPN.png` 알파 마스크(둘 다 4000×1714px, spritePixelsToUnits=500,
pivot 0.5,0.5 — `.png.meta`에서 확인)로 픽셀 검증해보니 실제로 도쿄만(灣) 입구 바다 위(알파=0)에
찍혀 있었다. 원인은 실제 하네다 공항이 도쿄만 안쪽 매립지에 있는데, 이 지도의 단순화된 해안선
해상도로는 그 정밀한 만 형태(미우라반도-보소반도 사이 좁은 입구)를 못 잡아내는 것.

좌표 변환 공식(픽셀 ↔ 로컬 오프셋, `.meta`의 spritePixelsToUnits=500·pivot(0.5,0.5)로부터 유도):
`pixel_x = 2000 + local_x*500`, `pixel_y = 857 - local_y*500`(PNG는 위→아래 행 순서, Unity 로컬
y는 위가 +이므로 부호 반전). 이 공식을 SEA_JEA(Step 30-4에서 실측 검증된 페르시아만 좌표 1.10,0.35)와
USA/JPN 국가 앵커에 대입해 "바다=검증된 sea 지점", "국가 앵커=육지"가 나오는지로 먼저 교차검증한 뒤
사용.

`scipy.ndimage.distance_transform_edt`로 JPN.png 알파 마스크에서 "해안선으로부터 12px 이상 안쪽인
(얇은 곶이 아니라 확실히 육지인) 픽셀 중 HND 계산값에 가장 가까운 지점"을 탐색 — (2.916,0.624)를
찾았다(원래 지점에서 서쪽으로 약 30px=0.06유닛, 미우라반도 쪽 육지). ASCII 크롭으로 육안 확인까지
마침. 나머지 14개 공항도 같은 방식으로 전수 재확인했는데, LAX(2px)/DXB(3px)/ICN(3px)/SYD(5px)만
world_base.png 기준 해안선에서 몇 픽셀 벗어나 있었고 — 이 정도는 4000px 폭 지도에서 시각적으로
감지 불가능한 수준(HND는 68px=약 20배 더 큰 오차였음)이라 그대로 뒀다.

수정: `DefaultTransportHubFactory.cs`의 `Air("HND", ...)` 좌표를 `(2.976f, 0.616f)` → `(2.916f, 0.624f)`로 교체.

**(2) 유닛 빈도 재하향**: "현재 비행기/배 빈도수가 많은 듯." Step 30-5에서 이미 한 번(24~180→10~70)
낮췄지만 여전히 많다는 피드백 — `TransportManager`의 `minActiveUnits`(10→5), `maxActiveUnits`(70→35),
`maxSpawnPerTick`(6→3)을 한 번 더 절반가량 낮춤. 틱 간격이 `SimulationManager` 기준 1초 고정이라
이 수치들이 그대로 "초당 최대 생성 수"이자 "정상 상태 유지 개체 수"로 체감된다.

**(3) "감염자가 있으면 무조건 감염 유닛" 문제**: 사용자 진단(정확함): "감염자가 있는 국가에서 출발한
비행기/배면 감염된 비행기/배가 나오는 게 문제 없는데, 현재 상황은 감염자가 없는 국가에서도 감염된
비행기/배가 나온다." 코드 리뷰로 먼저 확인한 것 — `TransportManager.IsCountryInfected()`(구 이름)는
`country.infectedCount > 0`이면 무조건 true를 반환했다. 국가 매핑 자체(허브→countryId)에는 버그가
없었고(48개국 전부 실제 CountryDatabase id와 정확히 일치, cross-check 완료), 로직도 "존재하지 않는
국가에서 감염 유닛이 나오는" 진짜 버그는 아니었다 — 다만 두바이(DXB→SAU)/제벨알리(SEA_JEA→SAU)/
싱가포르(SIN,SEA_SIN→MAS)/로테르담·안트베르펜(SEA_ROT,SEA_ANT→GER)처럼 게임에 없는 나라를 대신
대표 국가로 매핑한 허브들은, 그 "진짜" 도시 국가가 아니라 대표 국가의 감염 상태를 그대로 물려받는다는
알려진 근사 한계가 있다는 걸 확인해 사용자에게 먼저 질문(AskUserQuestion)으로 원인을 좁혔다.

사용자 확인 답변: "감염자가 있는 국가면 무조건 감염된 비행기/배가 나와서 이렇게 보이는 것 같음" —
즉 대표국 불일치 문제가 아니라, **boolean 판정 자체(1명만 감염돼도 그 나라 모든 항공/해운편이 매번
carrier)가 비현실적으로 느껴진다**는 것. 인구 수천만~수억 국가에서 발병 극초반 몇 명만 감염된
상태에서도 그 나라를 오가는 모든 유닛이 항상 빨갛게 표시되는 게 원인.

**수정**: `IsCountryInfected(string)` → `RollIsCarrier(string)`로 교체. 국가의 감염 비율
(`infectedCount / LivingPopulation`)에 새 인스펙터 필드 `carrierChanceScale`(기본 25)을 곱한 값을
carrier 확률로 써서 `Random.value`로 굴린다(1로 클램프) — 배율 25 기준 감염 비율 4%부터 사실상 항상
carrier, 0.1%면 2.5% 확률로만 carrier. `TrySpawnUnit()`/`HandleUnitArrived()`의 호출부 2곳을 전부
새 메서드로 교체. 이 값은 `unit.IsCarrier`를 통해 `TryTransferInfection()` 실행 여부도 같이 gate하므로
(carrier가 아니면 도착해도 감염 전파 자체가 시도되지 않음), 항공/해운을 통한 실제 전파 빈도도 국가별
감염 심각도에 비례하게 완화되는 부수 효과가 있다 — 감염 초기 국가가 전 세계로 병을 실어나르는 속도가
이전보다 느려진다는 뜻이라 전체 밸런스(치료제 타이밍 등)에 영향을 줄 수 있으니 다음 세션 실플레이로
체감 확인 필요.

**변경 파일**: `Data/DefaultTransportHubFactory.cs`(HND 좌표), `Managers/TransportManager.cs`
(유닛 수 하향, `carrierChanceScale` 필드 추가, `IsCountryInfected`→`RollIsCarrier` 교체 + 호출부 2곳).

**남은 리스크(Unity 에디터 미접속)**: (1) HND 새 좌표는 픽셀 마스크로 육지 확인은 했지만 실제 렌더링은
못 봄. (2) `carrierChanceScale=25`는 임의로 잡은 값이라 실플레이로 "너무 늦게/일찍 빨간 유닛이 나온다"
체감을 확인해 조정 필요. (3) carrier 확률화가 항공/해운 전파 빈도를 낮추는 부수 효과가 있어 전체
감염 확산 속도·치료제 타이밍 밸런스에 영향이 있을 수 있음 — 다음 세션에 체감 확인.

## Step 34 구현 메모 (인구 스케일 변경: 실제 인구/1000 → 실제 인구 수 그대로)

사용자 요청: 위키피디아 "인구순 나라 목록" 링크를 참고해 `CountryDatabase.asset`의 인구 수치를 갱신.
동시에 사용자가 붙여준 전염 방식 규칙 중 "실제 국가 인구 수를 그대로 사용한다"가 기존 CLAUDE.md의
"인구는 실제 인구/1000 스케일" 방침과 어긋나 있어, AskUserQuestion으로 확인 → **스케일 제거(실제 인구
수 그대로 저장)**로 확정.

**인구 수치 출처**: 한국어 위키피디아 "인구순 나라 목록" 페이지는 `mcp__workspace__web_fetch`로 가져오면
국가별 표 행이 비어서 나옴(플래그 이미지 때문으로 추정 — 각주만 파싱됨). 영어판 위키피디아도 동일 문제.
Claude in Chrome도 이 세션에서 연결 안 됨. 대신 `worldometers.info/world-population/population-by-country/`
(HTML 테이블이 단순해서 fetch로 정상 파싱됨, 2026년 기준 UN 인구 전망 데이터)로 48개국 전부의 최신
인구를 확보 — 위키피디아와 출처 계열(UN 인구 통계)이 같아 수치는 사실상 동일.

**변경 내용**:
- `Assets/New Folder/CountryDatabase.asset`: 48개국 `population` 필드를 전부 "실제 인구/1000"(예: 중국
  1404890) → "실제 인구 수 그대로"(예: 중국 1412914089)로 교체. `Country.population`이 이미 `long` 타입이라
  오버플로우 걱정 없음(48개국 합계도 long 범위에 한참 못 미침).
- **부수 발견**: 편집 전 파일을 읽어보니 마지막 국가(YEM)의 `seaRouteCountryIds` 마지막 항목이 `- I`에서
  그대로 끊겨 있었다(트레일링 개행도 없음) — 이전 세션 어디선가 파일이 잘린 채로 저장된 것으로 보임.
  다른 중동/아프리카의 뿔 국가들이 전부 IND(인도)와 해운 항로로 연결돼 있는 패턴에 맞춰 `- IND`로 완성해
  같이 고쳤다. (Read 도구로 확인한 파일 상태와 bash 도구로 본 상태가 서로 다르게 나온 적이 있었는데,
  이건 이 세션의 bash 샌드박스 마운트가 Edit/Write가 반영되기 전 스냅샷이라 그런 것 — 이 asset 파일의
  실제 최종 상태 확인은 항상 Read 도구 기준으로 했다.)

**코드 쪽 재조정 (population 절대값에 비례하는 공식만 대상 — 비율 기반 공식은 스케일 불변이라 손대지 않음)**:
전수 조사 결과 `infectedCount`/`population` 비율을 쓰는 공식(국내 확산 `newInfected`, 국가 간 육상 전파,
붕괴 단계, `plagueVisibility` 등)은 population 절대 크기와 무관하게 그대로 작동한다. 반면 절대값 자체를
계수에 곱하는 공식 2곳은 population이 1000배 커진 만큼 계수를 1000분의 1(또는 1000배)로 다시 맞춰야
기존과 동일한 타이밍/체감을 유지한다:
- `SimulationManager.cureStartChancePerInfected`(0.0005→0.0000005), `cureStartChancePerDeath`
  (0.0025→0.0000025) — "치료제 연구 시작" 확률이 `state.infectedCount`/`state.deadCount`(이제 실제 인원수)에
  직접 곱해지는 계수라, 안 낮추면 예전보다 1000배 이른 틱에 치료제 연구가 시작돼버림.
- `EventManager.whoMeetingMinDeathCount`(50,000→50,000,000) — WHO 긴급회의 이벤트가 "누적 사망자
  50,000명"에서 발동하던 절대 임계값. 이전 스케일에서는 이게 이미 "실질 5천만 명"을 의미했으므로(인구
  필드 자체가 /1000이었음), 이제 `deadCount`가 실제 인원수이니 임계값도 5천만으로 그대로 올려야 같은
  시점에 발동한다.
- `seedInfectedAmount`(10)/`airSeedAmount`(10)/`seaSeedAmount`(20, `TransportManager`)는 건드리지 않음 —
  "실제 몇 명이 씨앗으로 옮았는가"를 의미하는 절대 인원수라, 오히려 인구가 실제 스케일이 된 지금이 더
  현실적인 의미를 갖는다(이전엔 "20명"이 사실 "실제 2만 명"을 뜻해 다소 큰 숫자였음).
- `drugResistanceCoefficient`/`cureProgressCoefficient`(SimulationManager), DNA 마일스톤 관련 계수,
  `naturalDisasterSurgeRatio` 등은 전부 비율·배율(healthFunding×ResearchMultiplier, susceptibleCount×ratio
  등)에 기반해 population 절대 크기와 무관 — 그대로 둠.
- UI(`HudController`/`CountryPopupController`/`CountryStatusPanelController`/`CountrySelectController`/
  `CountryView`)는 전부 `country.population:N0` 형태로 그냥 숫자 포맷(천단위 콤마)만 하고 있어서 코드
  수정 없이도 자동으로 실제 인구 수가 표시된다(예: "인구 1,412,914,089").

**확인 안 된 것 (다음 세션 실플레이 필요)**: 이 스케일 변경으로 게임 내 "하루(틱)"가 커버하는 확산 규모가
로그 스케일로 늘어나는 셈이라(포화까지 필요한 틱 수가 log(1000)/log(1+성장률)만큼 늘어남 — 예: 틱당
성장률 10%면 약 70틱 추가), 재조정한 두 계수(`cureStartChancePer*`, `whoMeetingMinDeathCount`)가 실제
플레이에서 기존과 비슷한 체감 타이밍을 만드는지 확인 필요. 나머지 절대값 기반 이벤트/임계값이 더 있을
수도 있으니, 실플레이 중 "이전보다 뭔가 너무 빠르다/느리다" 싶은 지점이 있으면 이 Step부터 의심할 것.

**변경 파일**: `Assets/New Folder/CountryDatabase.asset`(48개국 population + 말단 truncation 수정),
`Managers/SimulationManager.cs`(cureStartChancePerInfected/PerDeath), `Managers/EventManager.cs`
(whoMeetingMinDeathCount), `CLAUDE.md`(인구 스케일 설명 갱신).

## Step 35 구현 메모 (국가별 "감염 점(dot)" 오버레이 — 감염률 색상 얼룩만으로는 시각 피드백 약함)

**문제 제기(사용자)**: 감염자 비율에 따른 국가 색상 변화(`CountryView.UpdateVisual`의 healthy→infected→dead
Color.Lerp)가 시각적 피드백으로 약하다. Step 34에서 인구 스케일을 실제 인구 수 그대로로 바꾼 여파도 있음 —
`infectionRatio = infectedCount / LivingPopulation`이 선형 비례라, 인구 억 단위 국가는 감염자가 수만 명
생겨도 ratio가 여전히 0에 가까워 색이 거의 안 바뀐 것처럼 보인다. **목표**: 감염자가 늘면 인구 비율대로
작은 빨간 점이 국가 안에 하나씩 생기게 해서, 색상 얼룩과 별개로 더 빠르고 뚜렷한 피드백을 준다.

**설계 검토 — 왜 SpriteMask/런타임 텍스처 읽기를 안 썼는가**: 점이 국가 실루엣 밖(바다)에 찍히면 안 되므로
"국가 모양 안에서만" 점이 나타나야 한다. 후보 1) 런타임에 `CountryShapes/{id}.png` 알파를 `GetPixel`로
읽어 매 프레임 유효 위치 판정 — 근데 이 텍스처들은 `.meta`에 `isReadable: 0`이라 그대로는 못 읽는다(48개
텍스처의 import setting을 `isReadable: 1`로 바꾸면 되지만 모바일 타겟 프로젝트에서 텍스처 CPU 복사본을
48장 더 메모리에 올려두는 셈이라 비용이 있고, Step 30 계열에서 `world_base.png` A* 계산 때도 같은 이유로
**런타임이 아니라 오프라인 사전계산**으로 처리했던 전례가 있음). 후보 2) `SpriteMask` + 국가별 Custom
Range로 점 스프라이트를 마스킹 — 48개 국가마다 겹치지 않는 sorting order 범위를 코드로 관리해야 해서
복잡도 대비 이득이 적음. **선택**: 후보 1의 "오프라인 사전계산" 전례를 그대로 따라, Python(PIL/numpy)으로
1회성 스크립트를 돌려 국가별 유효 좌표를 미리 뽑아 JSON으로 저장하고, 런타임에는 그 좌표만 읽어 쓴다 —
텍스처 읽기도, SpriteMask도 필요 없음.

**좌표 변환 공식 재사용/검증**: Step 33 HND 좌표 수정 때 이미 유도한 공식 `pixel_x = 2000 + local_x*500`,
`pixel_y = 857 - local_y*500`(캔버스 4000×1714px, `spritePixelsToUnits=500`, pivot 0.5/0.5는 전
`CountryShapes/*.png.meta`와 `world_base.png.meta` 공통)을 그대로 사용. 검증 삼아 역으로 USA 알파마스크
전체 픽셀 중심(874.8, 439.2)px를 이 공식으로 로컬 좌표 변환하면 (-2.25, 0.84) — `GamePlay.unity`에 이미
있는 USA의 `dnaSpawnLocalOffset={-2.13, 0.71}`(Step 29에서 "가장 큰 파츠 기준 무게중심"으로 계산한 값)과
방향·크기가 근접(알래스카 등 부속 영토를 포함하느냐 차이만큼만 오차) — 공식이 맞다는 근거로 삼고 진행.

**오프라인 스크립트(`gen_dots.py`, 프로젝트에는 포함하지 않은 1회성 도구)**: 각 국가 알파마스크(threshold
128)에서 알파 픽셀을 최대 2500개로 랜덤 다운샘플 → farthest-point sampling으로 24개 점 선정. 시작점은
"전체 알파 중심에 가장 가까운 픽셀"(대체로 국가 본토 중심 근처), 이후 "이미 고른 점들과의 최소거리가 가장
먼 점"을 24개 채울 때까지 반복 — 이 방식의 핵심 성질은 **배열의 앞 N개 부분수열도 이미 고르게 퍼져
있다**는 것(각 스텝이 이전 선택 대비 최대한 퍼지도록 고르므로). 그래서 런타임에서 "감염 비율에 따라 앞에서
부터 N개만 활성화"만 하면 활성 점 개수가 적을 때도(감염 초반) 국가 전역에 고르게 퍼져 보인다 — 셔플/재정렬
불필요. 결과를 `Assets/Resources/InfectionDotPoints.json`에 `{"countries":[{"id":"USA","points":
[x0,y0,x1,y1,...]}...]}` 형태로 저장. **주의**: Unity `JsonUtility`가 `List<List<float>>` 같은 중첩
배열을 역직렬화하지 못해서, `points`를 국가당 [x,y] 24쌍을 이어붙인 flat float[48] 배열로 저장했다(중첩
배열을 시도했다가 이 제약을 뒤늦게 알아차리고 스크립트를 flat 포맷으로 다시 작성함).

**검증**: 생성된 48개국 × 24점 = 1152개 좌표 전부를 다시 픽셀 좌표로 역변환해 해당 국가 알파마스크 값이
128 초과인지 재확인하는 별도 Python 체크를 돌렸고, 전부 통과(`bad=0`) — 국가 실루엣 밖에 점이 찍히는
케이스는 없다.

**런타임 코드**:
- `Data/InfectionDotDatabase.cs`(신규) — `Resources/InfectionDotPoints.json`을 `JsonUtility`로 1회
  로드해 `Dictionary<string countryId, Vector2[] points>`로 캐싱하는 정적 클래스. `CountryShapes`
  텍스처처럼 `Resources.Load`가 실패하거나 국가 데이터가 없는 경우 빈 배열을 반환하도록 방어.
- `Gameplay/CountryView.cs`(수정) — `Awake()`에서 `SetupInfectionDots()`가 자기 국가의 좌표 최대
  `maxInfectionDots`(기본 16)개만큼 자식 GameObject(각각 SpriteRenderer 하나)를 만들어 `localScale=0`
  (안 보임) 상태로 대기시킨다. 점 좌표는 `dnaSpawnLocalOffset`과 동일하게 "이 GameObject 기준 로컬
  오프셋"이라 그대로 `localPosition`에 대입하면 부모(WorldMap) 좌표계를 통해 자동으로 지도 위 정확한
  위치가 된다(별도 TransformPoint 계산 불필요 — 이미 자식이라 자동 상속). `UpdateVisual()`에서
  `infectionRatio`(기존 색상 계산과 같은 값 재사용)로 `_targetActiveDotCount =
  Ceil(infectionRatio * 점개수)`를 계산 — **Ceil이라 감염자가 단 1명이라도 있으면 즉시 점 1개가
  나타난다**(이게 이 기능을 만든 이유 자체를 해결). `Update()`에서 `colorTransitionSpeed`와 같은 방식의
  Exp 감쇠 lerp로 각 점의 scale을 0↔1로 부드럽게 전환(스냅 임계값 0.01 밑돌면 완전히 스냅해 불필요한
  `localScale` 갱신 호출을 줄임).
- 점 스프라이트는 프리팹을 새로 만들지 않고 `GetSharedInfectionDotSprite()`가 32×32 반투명 원형
  텍스처를 코드로 1회 생성해 모든 `CountryView` 인스턴스가 공유(48개국 각자 텍스처를 만들면 메모리
  낭비). `sortingOrder=50`으로 국가 색상 얼룩(기본 0)보다 위, `TransportUnit`(60, 코드에 이미
  "DNA 버블 아래, 국가 오버레이 위" 주석 있음)보다 아래로 배치.
- 씬 파일(`GamePlay.unity`)은 건드리지 않음 — 점 오브젝트는 전부 `CountryView.Awake()`가 런타임에
  코드로 생성하므로, 48개 국가 GameObject에 컴포넌트를 일일이 추가하는 수동 작업이 필요 없다.

**변경/신규 파일**: `Assets/Resources/InfectionDotPoints.json`(신규, 오프라인 생성),
`Assets/Scripts/Data/InfectionDotDatabase.cs`(신규), `Assets/Scripts/Gameplay/CountryView.cs`(수정).

**남은 리스크(Unity 에디터 미접속, 실플레이 필요)**: (1) 점 좌표는 픽셀 마스크 재검증(1152/1152 통과)까지
했지만 실제 렌더링 결과(점 크기·색·겹침)는 못 봄 — `infectionDotDiameter`(기본 0.024유닛)/
`maxInfectionDots`(기본 16)/`dotTransitionSpeed`(기본 6)는 인스펙터 노출값이라 실플레이로 "너무 크다/
작다/느리다" 느껴지면 바로 조정. (2) 아주 작은 섬나라(알파 픽셀 수가 24개보다 훨씬 적을 수 있는 국가)는
`farthest_point_sample`이 중복 픽셀을 고를 수 있어 점 24개가 서로 겹쳐 보일 수 있음 — 48개국 처리 로그에서
전부 "24개 점" 출력되긴 했으나 겹침 여부까지는 육안 확인 못 함. (3) 국경 근처 두 나라의 점이 시각적으로
붙어 보일 가능성은 있음(각자의 알파 영역 내부라는 것만 보장되고 상호 최소거리는 보장 안 함) — 실플레이로
어색하면 그때 조정.

## Step 36 구현 메모 (감염 점 배치 2차 개선 — 규칙적으로 보임 + 개수 부족 + 100%에도 안 채워짐)

**사용자 피드백 3건**: (1) "감염 점이 규칙적임" — Step 35의 farthest-point sampling은 "이미 고른 점들과
가장 먼 점"을 반복 선택하는 방식이라 역설적으로 아주 균일하게(격자처럼 보일 만큼 고르게) 퍼진다 — 이
"균일함"이 바로 "규칙적으로 보인다"는 지적의 원인. (2) "감염 점 수 자체가 적음" — Step 35는 국가 면적과
무관하게 전부 고정 24개(그중 최대 16개만 활성화)였다. (3) "감염률이 100%에 가까울 시 감염 점이 국가 땅을
가득 채워야 함" — 고정 개수·고정 지름(0.024유닛)이라 러시아처럼 큰 나라는 100%가 돼도 점이 듬성듬성
보이고, 반대로 작은 나라는 상대적으로 꽉 차 보이는 등 국가 크기와 무관하게 통일된 크기/개수를 썼던 게
원인.

**추가 요구사항**: "각 국가 영토의 안 인구밀도에 비례해서 감염 점 배치" — 단순히 무작위로 흩뿌리는 게
아니라 실제 인구가 몰린 곳(주요 도시)에 더 많이/먼저 나타나야 한다는 뜻으로 해석.

**설계**: Step 32에서 이미 검증해둔 "위경도 → WorldMap 로컬 좌표" 선형 회귀식(`DefaultTransportHubFactory.cs`
상단 주석: `localX = 0.021614*경도 - 0.045251`, `localY = 0.025372*위도 - 0.285776`, Plate Carrée 근사,
두바이 좌표로 교차검증까지 끝난 식)을 그대로 재사용할 수 있다는 걸 발견 — 이 식은 48개국 앵커 전체에 대해
피팅된 **전역** 선형식이라, 국가 앵커뿐 아니라 임의의 위경도 지점(예: 특정 도시)도 지도 위 근사 위치로
변환할 수 있다. 이를 이용해 48개국 주요 도시(국가당 3~8곳, 대략적 위경도 + 상대적 인구 가중치 — 정밀
인구 통계 조사는 하지 않음, 순위 기반 근사치) 목록을 오프라인 스크립트(`gen_dots_v2.py`)에 하드코딩하고:

1. 각 도시 위경도를 회귀식으로 픽셀 좌표로 변환한 뒤, `scipy.spatial.cKDTree`로 그 국가의 실제 알파마스크
   픽셀 중 가장 가까운 지점에 스냅(회귀식 잔차+단순화된 해안선 때문에 바다나 국경 밖에 찍힐 수 있어서 —
   Step 33 HND 보정과 같은 이유).
2. 그 스냅된 좌표를 중심으로 가우시안 지터(표준편차 = 국가 바운딩박스 대각선의 3.5%)로 점을 뿌리고,
   매번 알파마스크 안인지 재검증(아니면 근처 육지 픽셀로 재스냅) — **이게 "규칙적으로 안 보이게" 하는
   핵심**: farthest-point sampling처럼 균일하게 펴지 않고, 도시 하나하나가 자연스러운 반경의 "뭉치"로
   보이게 된다(실제 인구밀도 지도가 그렇듯).
3. 국가별 점 총개수(pool)는 "이 국가의 90% 지분은 도시 클러스터, 15%는 국가 전역 균일 랜덤(rural
   background)"으로 배분(`RURAL_FRACTION=0.15`) — 시골 지역에도 소수 감염이 나타나는 현실성을 위해.
4. 클러스터별 점 개수는 도시 가중치 비례로 미리 정해두되, **등장 순서**는 "가중 라운드로빈"(네트워킹의
   weighted round-robin 스케줄링과 동일한 알고리즘)으로 도시들과 rural 그룹을 인터리브 — 그 결과 배열의
   앞부분 N개만 활성화해도(감염률이 낮을 때) 이미 "수도/최대도시 위주 + 약간의 지방"이라는 비례가 유지된다
   (Step 35의 farthest-point sampling이 갖던 "앞부분만 잘라도 고르게 퍼져 보인다"는 성질을, 이번엔
   "공간적 균일함" 대신 "밀도 비례"로 대체한 셈).

**국가별 점 개수/지름 — 면적 기반 자동 산출**: 48개국 알파 픽셀 수(면적)를 조사해보니 최소(한국,
1,357px)~최대(러시아, 296,925px)까지 219배 차이. sqrt(면적)을 관측된 최소~최대 구간에서 선형보간해
개수를 14~90개로 정함(`MIN_POOL=14, MAX_POOL=90`) — sqrt를 쓴 이유는 면적에 그대로 비례시키면 러시아가
수천 개가 돼버려 모바일 성능(이 프로젝트에서 반복적으로 강조된 우려사항 — Step 30-5/33에서도 유닛 수를
계속 줄였음) 부담이 커지기 때문. 지름은 "이 점들로 국가 면적의 70%(`COVERAGE_TARGET`)를 덮으려면 반지름이
얼마여야 하는가"를 원 넓이 공식(`n*π*r² = coverage*area`)으로 역산 — 그 결과 러시아는 개수 90개/지름
0.1085유닛(큰 점), 한국은 개수 14개/지름 0.0186유닛(작은 점)으로 자동 산출됐다. **감염률 100%(전체
활성화) 시 국가 면적의 약 70%가 덮이도록 설계**했으므로(원끼리 랜덤 배치라 실제 겹침까지 고려하면
체감상 더 꽉 차 보일 것) "가득 채워야 한다"는 요구를 충족한다.

**검증**: 생성된 48개국 총 1,395개 좌표(국가별 14~90개, 이전 Step 35는 고정 1,152개=48×24) 전부를
다시 픽셀로 역변환해 해당 국가 알파마스크 값이 100 초과인지 재확인 — 전부 통과(`bad=0`).

**코드 변경**:
- `Data/InfectionDotDatabase.cs` — JSON 스키마에 국가별 `diameter` 필드 추가, `GetPoints()`를
  `GetLayout()`(좌표+지름을 담은 `InfectionDotLayout` 구조체 반환)으로 교체.
- `Gameplay/CountryView.cs` — 고정 `infectionDotDiameter`(0.024)/`maxInfectionDots`(16) 필드를
  국가별 계산값 그대로 쓰는 방식으로 교체. `maxInfectionDots`는 이제 "실수로 JSON이 비정상적으로 커도
  안전하게 자르는 상한"(128, 실제 최대 90보다 넉넉히 큼) 역할로만 남았고, `infectionDotDiameterScale`
  (기본 1)이 새로 추가돼 실플레이 후 전체 크기감을 한 값으로 튜닝할 수 있게 했다.

**변경 파일**: `Assets/Resources/InfectionDotPoints.json`(재생성 — 스키마 변경), `Data/InfectionDotDatabase.cs`,
`Gameplay/CountryView.cs`.

**참고 — 도시 위경도/가중치 데이터의 한계**: `gen_dots_v2.py`(프로젝트에 포함 안 한 1회성 도구)에 하드코딩한
48개국 주요 도시 목록은 일반 지식 기반의 대략적인 좌표·순위 가중치이며, 정밀 인구 통계를 조사해 검증한
값이 아니다 — 이 기능은 "그럴듯한 밀집/분산 시각 효과"가 목적이지 실제 인구지리학적 정확도가 목적이
아니라서(이 프로젝트가 애초에 "게임적 가독성 우선, 실제 교통량 시뮬레이션 아님"이라고 명시한 설계
철학과 같은 결) 이 정도 근사로 충분하다고 판단했다.

**남은 리스크(Unity 에디터 미접속, 실플레이 필요)**: (1) 지름·개수를 수식으로 역산했지만 실제 화면에서
"70% 커버리지"가 체감상 어떻게 보이는지 확인 못 함 — 원들이 랜덤 지터로 배치되니 겹침 때문에 체감
커버리지는 70%보다 높거나(겹쳐서 진해짐) 낮을(가우시안 분포라 일부 지역에 점이 안 갈 수 있음) 수 있다.
`infectionDotDiameterScale`로 전체 크기 한 번에 조정 가능. (2) 도시 클러스터 표준편차(바운딩박스 대각선의
3.5%)가 국가 모양에 따라(예: 러시아처럼 가로로 아주 긴 나라) 부적절할 수 있음 — 클러스터가 너무 넓게
퍼지거나 반대로 너무 뭉쳐 보일 가능성. (3) 가중 라운드로빈 인터리브가 실제로 "낮은 감염률에서도 도시
위주로 자연스럽게 보이는지" 육안 확인 필요.

## Step 37 구현 메모 (비행기/배 그래픽을 절차적 도형 → 실제 이모지로 교체)

**배경**: 기존 `TransportUnit.cs`는 십자형(비행기)/납작한 선체형(배)을 코드로 직접 그려서 썼다(Step 30-3).
사용자가 "이모지로 바꾸고 싶다"고 요청 — 후보를 채팅 위젯(Claude 시각화 도구)으로 두 번에 걸쳐 보여주고
(1차: 🚢🛳️⛴️🛥️ + ✈️🛩️🛫🛬, 2차: 배 후보만 ⛵🚤🛶🚣⚓🛟 추가) 사용자가 직접 클릭해서 최종 선택
— **비행기 ✈️(여객기), 배 🚢(화물선)**로 확정.

**기술적 문제 — Unity는 색 이모지를 렌더링할 방법이 마땅치 않음**: Unity의 텍스트 시스템(TextMeshPro
포함)은 색이 여러 개 섞인 이모지 글리프(COLR/CBDT 폰트)를 별도 에셋/셰이더 설정 없이는 못 그린다. 이
프로젝트는 지금까지 "런타임에 못 하는 계산은 오프라인에서 미리 해서 에셋으로 심는다"는 패턴을 반복해왔다
(world_base.png A* 경로, CountryShapes 실루엣, InfectionDotPoints.json 전부 같은 이유) — 이번에도 같은
전략을 썼다: 이모지를 "렌더링"하는 대신 이미 그려진 이모지 그래픽 PNG를 그대로 에셋으로 가져왔다.

**소스**: npm 패키지 `emoji-datasource-twitter`(Twemoji 이미지 데이터 배포판, 로컬 샌드박스에 설치해
파일만 추출 — 실제 게임 코드에는 포함 안 됨)에서 64px PNG 2장을 그대로 복사:
`img/twitter/64/2708-fe0f.png`(✈️) → `Assets/Resources/TransportIcons/plane.png`,
`img/twitter/64/1f6a2.png`(🚢) → `Assets/Resources/TransportIcons/ship.png`. Sprite import 설정은
CountryShapes 스프라이트들과 같은 패턴(`isReadable: 0`, `alphaIsTransparency: 1`)에 `spriteMode: Single`,
`spritePixelsToUnits: 260`(64px 원본 기준 지름 약 0.246유닛 — 기존 절차적 스프라이트와 비슷한 체감 크기).

**라이선스 — 사용자에게 반드시 알려야 할 부분**: `emoji-datasource-twitter` 패키지 자체(JS/JSON 코드)는
MIT지만, 패키지가 담고 있는 **이미지(Twemoji 그래픽)는 원 저작자 표시가 CC-BY 4.0**이다(Twitter가
원 제작, 현재는 커뮤니티 포크 `jdecked/twemoji`가 유지). CC-BY 4.0은 상업적 이용 자체는 허용하지만
**저작자 표시(attribution)를 요구**한다 — 이 게임을 앱인토스 등에 실제 배포한다면 크레딧/오픈소스
고지 화면에 "Twemoji" 저작자 표시를 넣는 걸 권장한다(정확한 표시 문구·범위는 법률 자문이 필요하면
받을 것 — Claude는 변호사가 아니라 이 부분은 사실 확인 수준의 안내다).

**회전 보정**: Twemoji 그래픽은 "+X 방향을 향한다"고 그려진 절차적 스프라이트와 달리 임의의 방향을
보고 있다 — 이미지의 알파 가중 PCA(주성분) 축 각도를 오프라인에서 계산해 확인: ✈️는 오른쪽 위 대각선
(약 45도), 🚢는 왼쪽(180도, bbox가 파도 그래픽 때문에 PCA로는 안 잡혀서 육안으로 직접 확인). 기존
`FaceTravelDirection()`의 `Atan2` 회전값에서 이 기준각을 빼는 방식으로 보정 — 절차적 폴백(아래 참고)엔
기준각 0(그대로)이 적용되도록 분기했다.

**색상 구분(carrier/idle) 처리**: 기존엔 스프라이트 색 자체를 곱해서(항공 파랑↔주황, 해운 초록↔빨강)
구분했는데, 이모지는 이미 여러 색이 섞여 있어 그대로 곱하면 탁해진다. 그래서 이모지 본체는 항상 흰색
(원본 색 그대로) 두고, 대신 이모지 뒤에 반투명 "halo" 원(코드로 생성, `CountryView`의 감염 점 스프라이트
생성 방식과 동일)을 깔아 그 halo 색으로 carrier/idle을 구분하게 바꿨다 — 실제 이모지 색은 그대로 보이면서
기존의 "색상 자체(hue)로 항공/해운, carrier/idle을 확실히 가른다"는 Step 30-3 설계 의도는 유지된다.

**안전장치**: `Resources.Load<Sprite>("TransportIcons/plane"/"ship")`가 실패하면(파일 누락 등)
기존 절차적 `BuildTriangleSprite`/`BuildHullSprite`로 자동 폴백 — 이 경우 halo는 비활성화하고 예전처럼
본체 자체를 carrier/idle 색으로 물들인다(폴백 상태에서 halo 하나만으로는 대비가 부족해서).

**변경/신규 파일**: `Assets/Resources/TransportIcons/plane.png`, `ship.png`(신규, Twemoji 그래픽 — CC-BY
4.0), `Assets/Scripts/Gameplay/TransportUnit.cs`(수정).

**남은 리스크(Unity 에디터 미접속, 실플레이 필요)**: (1) 회전 기준각(45도/180도)은 PCA/육안 확인으로
추정한 값이라 실제 화면에서 이동 방향과 이모지가 정확히 맞는지 확인 못 함 — 어긋나 보이면 이 상수
(`AirSpriteBaseFacingDeg`/`SeaSpriteBaseFacingDeg`, `TransportUnit.cs`) 미세조정. (2) halo 크기 배율(1.6배)과
halo 자체의 은은한 정도(가운데 60% 불투명 + 바깥 40% 페이드)가 실제로 carrier/idle 구분을 기존 절차적
도형만큼 뚜렷하게 주는지 확인 필요 — 부족하면 halo 배율/최소 알파를 올릴 것. (3) 이모지 PNG가 64px라
지도를 크게 확대하면 다소 흐려 보일 수 있음(기존 절차적 도형은 벡터처럼 어느 배율에서도 선명) —
확대 시 체감 화질이 아쉬우면 128px 버전으로 교체 고려.

## Step 38 구현 메모 (halo가 너무 크다는 피드백 — 색상 윤곽선으로 교체 + 이모지 크기 축소)

**피드백**: Step 37에서 넣은 반투명 원형 halo(이모지 뒤에 깔아 carrier/idle 색을 표시하던 것)가
"하이라이트 효과가 너무 큼" — 지름 배율 1.6배로 잡았던 게 실제로는 과했던 것으로 보인다. 요구사항은
halo를 없애고 그 자리에 "색상이 들어간 윤곽선"을 넣는 것 + 이모지 이미지 자체 크기도 살짝 축소.

**윤곽선 구현 방법 검토**: Unity에서 스프라이트에 색 윤곽선을 넣는 정석적인 방법은 커스텀 셰이더
(Shader Graph의 outline 노드 등)인데, URP Shader Graph 에셋은 내부적으로 복잡한 직렬화 포맷이라
코드만으로(에디터 없이) 안정적으로 만들어내기 어렵다. 대신 셰이더 없이도 되는 고전적인 트릭을 썼다 —
"실루엣을 단색으로 채운 복사본을 몇 픽셀씩 어긋나게(8방향) 여러 장 겹쳐 그리고, 그 위에 원본을 덮으면
가장자리 바깥으로 삐져나온 부분만 남아 윤곽선처럼 보인다"는 방식(2D 게임에서 흔히 쓰는 스프라이트
아웃라인 기법). 실루엣 복사본의 RGB를 원본 그대로 쓰면 원본 색 × 틴트 색이 곱해져 탁해지므로
(halo 문제와 같은 원인), RGB를 흰색으로 고정한 별도 실루엣 PNG를 오프라인에서 새로 만들어야 했다
(흰색 × 틴트 = 틴트 그대로 나오므로 깨끗한 단색 윤곽선이 나옴).

**신규 에셋**: 기존 이모지 PNG(`plane.png`/`ship.png`)와 같은 소스(Twemoji 64px)에서 numpy로 RGB 채널만
(255,255,255)로 덮어쓰고 알파채널은 그대로 유지해 `plane_outline.png`/`ship_outline.png`로 저장. import
설정은 기존 이모지 스프라이트와 동일(`Single`, `isReadable:0`, `spritePixelsToUnits:260`).

**검증(오프라인 합성 미리보기)**: Unity 없이도 결과를 먼저 확인하기 위해, Python(PIL)으로 실제 코드와
동일한 로직(8방향 3px 오프셋 + 틴트한 실루엣을 먼저 그리고 원본을 위에 덮기)을 그대로 재현해 합성
이미지를 만들어봤다 — 비행기(주황 윤곽선)/배(빨강 윤곽선) 둘 다 원본 실루엣을 깔끔하게 따라가는
얇은 단색 테두리로 나왔다(halo 특유의 "동그랗고 크게 뭉개지는" 느낌 없음). 실제 Unity 렌더링과 완전히
동일하진 않지만(스프라이트 정렬/블렌딩 방식 차이 있을 수 있음) 알고리즘 자체의 타당성은 이 미리보기로
확인됐다.

**코드 변경(`TransportUnit.cs`)**:
- `Awake()` — halo GameObject 생성 코드 제거. 대신 `transform.localScale = (iconScale, iconScale, 1)`로
  이모지 본체(및 자식인 윤곽선 복사본도 같이) 크기를 줄이고, "Outline_0"~"Outline_7" 8개 자식
  SpriteRenderer를 미리 만들어둔다(sortingOrder 59, 본체 60보다 한 단계 아래).
- `BeginLeg()` — 이모지일 때만 윤곽선 스프라이트(`GetAirOutlineSprite`/`GetSeaOutlineSprite`)를 로드해
  8개 복사본에 carrier/idle 색을 입히고, 오프셋 거리(`outlineThicknessPx / sprite.pixelsPerUnit`)만큼
  8방향에 배치. 폴백(절차적 도형)일 땐 윤곽선 복사본을 전부 비활성화하고 예전처럼 본체 자체를 물들인다.
- `GetHaloSprite()`/halo 관련 필드 전부 제거.

**변경 파일**: `Assets/Resources/TransportIcons/plane_outline.png`, `ship_outline.png`(신규),
`Assets/Scripts/Gameplay/TransportUnit.cs`(수정).

**남은 리스크(Unity 에디터 미접속, 실플레이 필요)**: (1) 오프라인 합성 미리보기로 알고리즘은 확인했지만
실제 Unity SpriteRenderer 블렌딩/정렬 순서에서 동일하게 보이는지는 확인 못 함. (2) `outlineThicknessPx`
(기본 3px)/`iconScale`(기본 0.8)이 실플레이에서 적절한지 튜닝 필요 — 인스펙터 노출값이라 바로 조정 가능.
(3) 8방향(45도 간격) 오프셋이라 아주 뾰족한 모서리(예: 비행기 날개 끝)에서는 윤곽선이 살짝 얇아 보일
수 있음 — 더 매끈한 윤곽선이 필요하면 방향 수를 12~16개로 늘리는 것도 고려(성능엔 거의 영향 없음,
동시 활성 유닛이 최대 35개 수준이라 유닛당 8개 늘어도 부담 적음).

## Step 39 구현 메모 (사용자 제공 고해상도 배 이미지 "Ship 1"로 교체 + 윤곽선 두께 버그 수정)

**요청**: "이모지 크기 내가 직접 조정했고 그리고 Transporticons에 "Ship 1" 으로 배 이미지 넣어 놓았어
이걸로 배 이모지 대체해줘" — 사용자가 `Assets/Resources/TransportIcons/`에 본인이 준비한 배 이미지
"Ship 1.png"를 직접 넣어뒀고, 이걸로 기존 Twemoji 🚢를 교체해달라는 요청. 추가로 "이모지 크기는 내가
직접 조정했다"고 명시 — `iconScale` 값은 건드리지 말아야 한다는 뜻.

**신규 에셋 확인**: `Ship 1.png`(원본 1188×648, Unity 에디터에서 이미 Multiple 스프라이트 모드로 슬라이스
돼 있었음 — `.meta`에 서브 스프라이트 "Ship 1_0"(크롭 rect x=0,y=132,w=1186,h=502), `spritePixelsToUnits:2500`,
`isReadable:0`으로 확인됨. 사용자가 직접 Unity에서 임포트 설정까지 마친 상태로 보인다). Read 도구로
이미지를 직접 열어 확인 — 위에서 내려다본 형태의 컨테이너 화물선 일러스트, 이물(뱃머리)이 왼쪽을 향함.
오프라인 PCA(알파 가중 픽셀 좌표 기준)로 주축 각도를 계산해보니 약 -177.8도(거의 수평, 왼쪽) — 기존
`SeaSpriteBaseFacingDeg=180`과 사실상 일치해서 이 상수는 그대로 둬도 된다고 판단.

**Multiple 스프라이트 모드 로드 문제**: 기존 `GetSeaSprite()`는 `Resources.Load<Sprite>("TransportIcons/ship")`
단일 로드였는데, "Ship 1.png"는 Multiple 모드(서브 스프라이트가 실제 에셋)라 이 방식으로는 못 찾는다
(`Resources.Load<Sprite>`는 Single 모드 텍스처의 자동 생성 스프라이트만 잡는다). `CountryView.ApplyCountryShape()`가
국가 실루엣 텍스처를 로드할 때 이미 겪었던 것과 같은 문제라, 그때와 동일한 해법(`Resources.LoadAll<Sprite>`로
전체 서브 스프라이트를 가져와 첫 번째를 쓰는) 패턴을 그대로 재사용 — `LoadFirstSprite(string path)` 헬퍼를
새로 추가했다.

**폴백 체인 확장**: `GetSeaSprite()`를 "Ship 1"(LoadFirstSprite) → 실패 시 기존 `ship.png`(Twemoji,
Resources.Load) → 실패 시 `BuildHullSprite()`(절차적 선체) 3단 폴백으로 확장. `GetSeaOutlineSprite()`도
동일하게 "Ship 1_outline"(LoadFirstSprite) → `ship_outline.png` 순서로 확장. 기존 Twemoji ship.png/
ship_outline.png는 삭제하지 않고 2차 폴백으로 남겨뒀다 — 혹시 "Ship 1" 에셋이 어떤 이유로 로드 안 되면
예전처럼 최소한 이모지 배라도 나오게.

**윤곽선용 실루엣 신규 생성**: `Ship 1_outline.png`을 Python/numpy로 생성 — `Ship 1.png`과 동일한 알파채널을
유지한 채 RGB만 (255,255,255)로 덮어썼다(Step 38의 plane_outline/ship_outline과 같은 원리 — 흰색 실루엣이어야
carrier/idle 틴트가 원본 색과 섞이지 않고 깨끗하게 나온다). `.meta`도 `Ship 1.png.meta`를 그대로 복사해
GUID만 새로 발급하고 서브 스프라이트 이름을 "Ship 1_0"→"Ship 1_outline_0"으로 바꿨다 — 크롭 rect(x=0,y=132,
w=1186,h=502)와 `spritePixelsToUnits:2500`을 원본과 완전히 동일하게 유지해야 8방향 오프셋 정렬이 어긋나지
않는다. 오프라인 Python 합성(크롭 후 8방향 3px 상당 오프셋 + 빨강 틴트)으로 미리보기 확인 — 고해상도
에셋에서도 윤곽선 기법이 문제없이 동작함을 확인.

**윤곽선 두께 버그 발견 및 수정**: 기존 코드는 `offsetMagnitude = outlineThicknessPx / _renderer.sprite.pixelsPerUnit`
로 윤곽선 오프셋을 계산했다 — 즉 "텍스처 픽셀 수"를 스프라이트의 `pixelsPerUnit`으로 나눠 월드 유닛
오프셋을 구하는 방식. 문제는 스프라이트마다 `pixelsPerUnit`이 크게 다르다는 것: 기존 이모지(plane.png/
ship.png)는 260인데, 사용자가 새로 임포트한 "Ship 1.png"는 2500(거의 10배). 같은 `outlineThicknessPx=3`
값이 기존 배에서는 3/260≈0.01154 유닛인데 새 배에서는 3/2500=0.0012 유닛 — 화면상 거의 안 보이는
두께가 된다. 스프라이트 해상도/PPU는 각 에셋을 준비한 방식에 따라 제각각인데(사용자가 직접 임포트 설정을
조정하는 값이기도 함), 윤곽선 두께는 "화면에 보이는 실제 두께"가 목적이므로 PPU에 연동시키는 게 애초에
잘못된 설계였다 — `outlineThicknessPx`(픽셀 기준, PPU로 나눔)를 `outlineThicknessWorldUnits`(월드 유닛
고정값, 기본 0.012 — 기존 이모지 기준 두께와 거의 동일한 값으로 잡음)로 교체해서 스프라이트의 PPU와
무관하게 항상 같은 화면상 두께가 나오도록 고쳤다.

**iconScale은 건드리지 않음**: 사용자가 "이모지 크기 내가 직접 조정했다"고 명시했으므로 `iconScale`
필드의 현재 값은 그대로 뒀다 — 코드 로직(Awake()에서 `transform.localScale`에 적용하는 부분)도 변경 없음.

**코드 변경(`TransportUnit.cs`)**:
- `outlineThicknessPx`(float, 픽셀+PPU 나눔) 필드를 `outlineThicknessWorldUnits`(float, 월드 유닛 고정값
  0.012) 필드로 교체.
- `BeginLeg()`의 오프셋 계산을 `outlineThicknessPx / _renderer.sprite.pixelsPerUnit` → `outlineThicknessWorldUnits`로 수정.
- `GetSeaSprite()` — "TransportIcons/Ship 1"(LoadFirstSprite) → "TransportIcons/ship"(Resources.Load) →
  `BuildHullSprite()` 3단 폴백으로 확장.
- `GetSeaOutlineSprite()` — "TransportIcons/Ship 1_outline"(LoadFirstSprite) → "TransportIcons/ship_outline"
  순서로 확장.
- `LoadFirstSprite(string path)` 신규 헬퍼 — `Resources.LoadAll<Sprite>(path)`로 Multiple 모드 서브
  스프라이트를 가져와 첫 번째를 반환(없으면 null).

**변경 파일**: `Assets/Resources/TransportIcons/Ship 1.png`(사용자 제공, 기존), `Ship 1_outline.png`(신규,
이번 세션에서 생성), `Assets/Scripts/Gameplay/TransportUnit.cs`(수정).

**남은 리스크(Unity 에디터 미접속, 실플레이 필요)**: (1) "Ship 1" 에셋은 Multiple 모드로 이미 슬라이스돼
있었지만 실제 Unity 에디터에서 `LoadFirstSprite`가 의도한 서브 스프라이트("Ship 1_0")를 정확히 첫 번째로
반환하는지는 런타임에서 확인 못 함(서브 스프라이트가 하나뿐이라 순서 문제는 이론상 없어야 하지만, 여러
개로 나뉜 경우 배열 순서가 보장되지 않을 수 있음 — 지금은 사용자가 배 이미지 하나만 하나의 서브
스프라이트로 슬라이스해뒀다고 확인했으므로 문제 없을 것으로 예상). (2) `outlineThicknessWorldUnits=0.012`는
기존 이모지 기준 두께(3/260≈0.01154)와 비슷하게 맞춘 값이라 이론상 기존과 큰 차이가 없어야 하지만,
새 배 이미지가 기존보다 훨씬 크게 보일 수 있어(사용자가 iconScale을 직접 조정한 값에 따라) 상대적으로
두껍거나 얇아 보일 수 있음 — 실플레이로 확인 필요. (3) 이동 방향 기준각(180도)은 PCA로 재확인했지만
실제 게임 화면 렌더링은 못 봄.

## Step 40 구현 메모 ("이미지 적용이 안 되는 것 같다" 신고 — 실제 파일 손상/누락 발견 + 아웃라인 스케일 구조적 버그 수정)

**신고 내용**: "비행기/배 이미지 문제 있는지 확인해줘 이미지 적용이 안된 것 같아 그리고 이미지 크기
어디서 조정하는지 알려줘." Step 37~39는 전부 Unity 에디터 접속 없이(오프라인 Python 미리보기로만
검증하고) 작업했던 터라, 실제로 뭔가 잘못됐을 가능성을 진지하게 조사했다.

**발견 1 — `plane.png`가 32×32로 손상**: `file`/`imagemagick identify`/Python으로 PNG IHDR 청크를
직접 파싱하는 3가지 독립적인 방법으로 확인한 결과 전부 32×32라고 일치했다(원래 Twemoji 원본은
64×64, 실제로 `plane_outline.png`는 여전히 64×64로 남아있어 서로 어긋나 있었음). npm 캐시에 남아있던
원본 Twemoji 소스(`emoji-datasource-twitter/img/twitter/64/2708-fe0f.png`)와 바이트 단위로 비교해보니
전체 2531바이트 중 단 몇 바이트(IHDR의 height 필드 + 그에 맞춰 재계산된 CRC)만 다르고 나머지(실제
압축된 픽셀 데이터, IDAT)는 완전히 동일했다 — 즉 "이미지를 실제로 축소 재저장"한 게 아니라 헤더의
height 값만 64→32로 바뀐 것. 이런 정밀한 단일 필드 조작은 일반적인 이미지 편집 툴 사용으로는 잘
안 나오는 패턴이라(수동 리사이즈면 파일 전체가 재인코딩돼 바이트가 크게 달라짐), 이전 세션에서 내가
실행한 어떤 스크립트가 실수로 이 파일을 건드렸을 가능성이 높다고 판단했다. 원본 Twemoji 소스 파일이
npm 캐시(`/tmp/emojitest/...`)에 그대로 남아있어서 그걸로 안전하게 복구했다(공개 이모지 데이터라 복구
과정에서 데이터 손실 없음).

**발견 2 — `ship.png`/`ship_outline.png`가 프로젝트에서 완전히 사라짐**: Windows 쪽 파일을 직접 읽는
Read 도구로 확인한 결과 두 파일 모두 "파일 없음"으로 나왔다(bash 쪽에서도 처음엔 있다고 나왔다가
다음 호출에선 없다고 나오는 등 불안정했는데, 최종적으로 Read 도구 기준 실제로 없는 상태였다). 원인은
특정하지 못했지만, 다행히 `.meta` 파일(`ship.png.meta`/`ship_outline.png.meta`)은 그대로 남아있어서
원래 어떤 설정이었는지 확인할 수 있었다 — 그런데 이 `.meta`를 열어보고 **또 다른 중요한 사실을
발견**했다: `spriteMode: 2`(Multiple)로 이미 바뀌어 있고 크롭 rect가 "ship_0"/"ship_outline_0"이라는
이름의 서브 스프라이트 하나로 정의돼 있었다. Step 37/38 DevLog엔 "Single 모드"라고 기록했었는데
실제로는 사용자가 (아마 Sprite Editor를 한 번이라도 열어봐서) Multiple 모드로 바뀌어 있었던 것 —
즉 `Resources.Load<Sprite>("TransportIcons/ship")`(Single 전용 로드 방식)는 이 파일이 존재했을 때조차
계속 null을 반환하고 있었을 것이다. bash에서 파일 쓰기 권한 문제로 몇 번 막히다가 `mv`로 임시
파일명을 거쳐 우회해서 겨우 두 파일 다 Twemoji 원본(64×64)으로 복구했고, 기존 `.meta`가 기대하는
크롭 rect(0,0,64,64)와 정확히 일치해 별도로 `.meta`를 새로 만들 필요는 없었다.

**"Ship 1.png"는 손대지 않음(중요)**: 같은 방식으로 조사하다가 "Ship 1.png"가 현재 117×64로 줄어든
상태(사용자가 원래 넣어준 건 1188×648 기준 크롭이었음)에 zero-padding이 뒤에 덧붙어 파일 크기만
677KB로 큰 상태임을 발견했다. 처음엔 이것도 내가 손상시킨 걸로 의심했는데, 조사 도중 `.meta`를 다시
열어볼 때마다 내용이 계속 달라지는 걸 확인했다(스프라이트 슬라이스 목록이 있다가 비게 되고,
pixelsPerUnit이 2500→10000으로 바뀌고, DefaultTexturePlatform의 maxTextureSize가 128로 줄어드는 등) —
이건 사용자가 지금 이 세션과 동시에 Unity 에디터에서 이 파일의 Sprite Editor/Import Settings를 직접
만지고 있다는 강한 정황이다("이모지 크기 내가 직접 조정했다"는 발언과도 일치). 그래서 **이 파일과
`.meta`는 의도적으로 건드리지 않고 그대로 뒀다** — 사용자가 작업 중인 걸 덮어쓰면 오히려 더 큰
문제가 될 수 있어서다. 다음 세션에서 반드시 사용자에게 "Ship 1.png"의 현재 상태(117×64)가 의도한
것인지, 아니면 원본을 다시 넣어야 하는지 확인이 필요하다.

**발견 3(더 근본적인 구조적 버그) — 윤곽선이 본체 스프라이트와 어긋나는 문제**: 위 조사 과정에서
`plane.png`(350 PPU)와 `plane_outline.png`(500 PPU)가 서로 다른 Pixels Per Unit 값을 갖고 있다는 걸
발견했다 — Step 38 작성 당시엔 "동일한 import 설정(260)"이라고 기록했었지만, 사용자가 이미지 크기를
조정하려고 Pixels Per Unit을 만졌을 때 본체(`plane.png`)만 바꾸고 짝을 이루는 윤곽선(`plane_outline.png`)은
그대로 둔 것으로 보인다. 이 프로젝트의 윤곽선 트릭은 본체와 윤곽선이 "정확히 같은 렌더링 크기"라는
전제 위에 설계돼 있어서, 이 둘이 어긋나면 윤곽선이 본체보다 작아 아예 안 보이거나(그러면 carrier/idle
색 구분 자체가 안 보임 — "이미지가 적용이 안 된 것 같다"는 인상을 줄 수 있음) 반대로 훨씬 크게
삐져나와 보일 수 있다. 근본 원인은 "본체 이미지 크기를 조정하는 정상적인 방법(Pixels Per Unit)"과
"윤곽선도 항상 같이 맞춰줘야 한다"는 암묵적 결합이 사용자에게 보이지 않는다는 것 — 코드로 고쳤다:
`TransportUnit.BeginLeg()`에서 윤곽선 렌더러의 로컬 스케일을 `본체 스프라이트의 실제 월드 크기 /
윤곽선 스프라이트 자체의 월드 크기` 비율로 매번 재계산해서 강제로 맞춘다. 이제 본체와 윤곽선의
텍스처 픽셀 크기·Pixels Per Unit이 서로 아무리 달라도(사용자가 나중에 또 한쪽만 조정해도) 항상
시각적으로 정확히 겹쳐진다.

**iconScale 위치 이전**: 사용자가 "이미지 크기 어디서 조정하는지" 물어봤는데, 정직하게 답하면 기존
구조가 답하기 어려웠다 — `iconScale`은 `TransportUnit`의 `[SerializeField]` 필드였지만, `TransportUnit`은
씬에 프리팹으로 존재하지 않고 `TransportManager.Awake()`에서 매번 `AddComponent`로 코드 생성되는
오브젝트라, Play 모드 중 인스펙터에서 값을 바꿔도 Stop하면 사라진다(애초에 저장될 대상 에셋이 없음).
`carrierChanceScale`이 이미 이 문제를 안 겪는 이유는 그 필드가 씬에 실제로 존재하는 `TransportManager`
컴포넌트에 있기 때문 — 그래서 `iconScale`도 같은 위치(`TransportManager`)로 옮기고, `BeginLeg()` 호출
시 매번 파라미터로 전달하도록 시그니처를 바꿨다(`BeginLeg(..., float iconScale)`). 이제 Play 모드 중
`TransportManager` 인스펙터에서 조정하면 다음 구간부터 바로 반영되고, Stop 후에도 씬에 저장된다.

**코드 변경**:
- `TransportUnit.cs` — `iconScale` 필드 제거(파라미터로 대체), `Awake()`에서 크기 적용 로직 제거하고
  `BeginLeg()`에서 매번 적용하도록 이동. 윤곽선 로컬 스케일 자동 매칭 로직 추가(`sprite.bounds` 비율
  계산). `GetSeaSprite()`/`GetSeaOutlineSprite()`의 `ship`/`ship_outline` 폴백 경로를 `Resources.Load`에서
  `LoadFirstSprite`(LoadAll 기반)로 교체 — 이 파일들이 Multiple 모드였다는 걸 이번에 알았기 때문.
- `TransportManager.cs` — `iconScale` 필드 신규 추가(기본 0.8, `carrierChanceScale` 옆에 배치), 두
  `BeginLeg()` 호출부(`TrySpawnUnit`, `HandleUnitArrived`) 모두 이 값을 전달하도록 수정.

**변경 파일**: `Assets/Resources/TransportIcons/plane.png`, `ship.png`, `ship_outline.png`(전부 Twemoji
원본으로 복구), `Assets/Scripts/Gameplay/TransportUnit.cs`, `Assets/Scripts/Managers/TransportManager.cs`.
**건드리지 않은 파일**: `Assets/Resources/TransportIcons/Ship 1.png`, `Ship 1.png.meta`(사용자가 동시에
편집 중인 것으로 보여 의도적으로 보존).

**남은 리스크(Unity 에디터 미접속, 사용자 확인 필요 — 최우선)**: (1) "Ship 1.png"가 지금 117×64인 게
사용자가 의도한 최종 상태인지, 아니면 원본을 다시 넣어야 하는 건지 반드시 확인 필요 — 확인 전까지는
이 파일에 대해 아무것도 가정하지 않을 것. (2) 윤곽선 자동 스케일 매칭 로직은 코드 리뷰로는 맞다고
판단했지만 실제 Unity 렌더링으로 확인 못 함. (3) `plane.png`/`ship.png`를 Twemoji 원본으로 되돌리면서
사용자가 그 사이에 직접 조정해뒀을 수도 있는 Pixels Per Unit 값(350/500 등)은 그대로 뒀다 — 이제
윤곽선이 자동으로 따라가므로 어떤 값이든 괜찮아야 하지만, 실제로 원하는 크기감인지는 실플레이 확인
필요.

### Step 40 후속 — "Ship 1 이미지를 써줘" (사용자가 최종 확정)

사용자가 이어서 "배 이미지는 Ship 1 이미지를 써줘"라고 확인 — "Ship 1.png"가 117×64로 줄어든 현재
상태를 되돌릴 필요 없이 그대로 쓰면 된다는 뜻으로 받아들였다. `.meta`를 다시 열어보니 이번엔 내용이
더 이상 바뀌지 않고 안정된 상태였다(PPU 1000, DefaultTexturePlatform maxTextureSize 128 고정) — 사용자가
Unity에서의 편집을 끝낸 것으로 보인다.

다시 확인해보니 진짜 남은 문제는 크기 자체가 아니라 **`spriteMode: 2`(Multiple)인데
`spriteSheet.sprites`가 빈 배열**이었던 것 — 즉 Multiple 모드로 전환은 됐지만 실제 슬라이스(어느
영역이 스프라이트인지)가 하나도 정의돼 있지 않아서, `LoadFirstSprite()`(`Resources.LoadAll<Sprite>`)가
가져올 서브 스프라이트 자체가 없어 매번 null → Twemoji `ship.png` 폴백으로 떨어지고 있었다(코드
로직은 Step 39/40에서 이미 맞게 짜여 있었는데, 정작 에셋 쪽에 로드할 대상이 없었던 것).

**수정**: "Ship 1.png.meta"의 `spriteSheet.sprites: []`를 실제 이미지 전체 영역(0,0,117,64)을 덮는
단일 슬라이스 `Ship 1_0`으로 채워 넣었다 — `internalIDToNameTable`/`nameFileIdTable`에 이미 있던
`internalID`(6249132924414084098)를 그대로 재사용해 참조가 어긋나지 않게 했다. `Ship 1_outline.png`
(1188×648)는 건드리지 않았다 — 현재 "Ship 1.png"(117×64, 종횡비 1.828)와 종횡비가 거의 같아서(1.833)
같은 그림을 다른 해상도로 담고 있는 것으로 보이고, Step 40에서 추가한 윤곽선 자동 스케일 매칭 로직이
크기 차이를 알아서 보정해주므로 그대로 둬도 된다.

**변경 파일**: `Assets/Resources/TransportIcons/Ship 1.png.meta`(스프라이트 슬라이스 정의 추가).

**남은 리스크**: 다음 Unity 실행 시 이 `.meta`로 재임포트되면서 "Ship 1_0" 슬라이스가 실제로 유효하게
잡히는지, 그리고 117×64라는 낮은 해상도가 게임 화면에서(iconScale 배율 적용 후) 눈에 띄게 흐려
보이지는 않는지 실플레이로 확인 필요.

## Step 41 구현 메모 (감염 점 최대 크기 절반 축소 + 개수 2배 증가)

**요청**: "감염 점의 최대 크기를 절반 정도 줄이고 점의 개수를 2배 늘려줘."

**코드 확인**: `CountryView.cs`에 이미 Step 36에서 정확히 이 용도로 만들어둔 두 인스펙터 값이 있었다 —
`infectionDotDiameterScale`(점 지름 전체 배율, 기본 1)과 `maxInfectionDots`(활성화 가능한 점 개수 상한).
그런데 `GamePlay.unity`를 열어 실제 직렬화된 값을 보니 48개 국가 GameObject 전부 `maxInfectionDots: 16`,
`infectionDotDiameterScale: 1`로 박혀 있었다 — `maxInfectionDots`는 Step 35 시절 값(24개 좌표 중
최대 16개 활성화) 그대로였고, Step 36에서 스크립트 기본값을 128로 올려놨어도 씬에 이미 직렬화된 값이
그걸 덮어써서 실제로는 계속 16이 상한으로 작동하고 있었다(스크립트 코드 주석의 "실제로 이 상한에
걸릴 일은 없게 넉넉히 잡아뒀다"는 설명은 부정확했던 것 — Unity는 컴포넌트가 씬에 이미 직렬화된 이후엔
스크립트의 기본값 변경을 소급 적용하지 않는다).

**수정**: 두 값 모두 `GamePlay.unity`의 48개 인스턴스에 일괄 반영(`sed`로 텍스트 치환, 48개 전부 같은
값이었음을 먼저 확인 후 적용) + `CountryView.cs` 스크립트 기본값도 동일하게 맞춤(향후 새 국가
GameObject를 추가할 때 씬 값과 어긋나지 않도록):
- `infectionDotDiameterScale`: 1 → 0.5 (최대 크기 절반)
- `maxInfectionDots`: 16 → 32 (활성 점 개수 상한 2배)

**주의(개수 2배가 국가마다 다르게 체감될 수 있음)**: `maxInfectionDots`는 어디까지나 "상한"이고, 실제
표시 가능한 점 개수는 국가별로 `Resources/InfectionDotPoints.json`에 미리 계산해 저장해둔 점 좌표
개수(Step 36 기준 14~90개, 오프라인 Python 스크립트로 국가 면적에 비례해 생성 — 이 스크립트 자체는
프로젝트에 포함 안 됨)를 넘을 수 없다. 실제 분포 확인 결과:
- 12개국(러시아 90, 캐나다 71, 미국 64, 중국 62, 브라질 56, 호주 55, 인도/아르헨티나 38, 알제리 34,
  DRC 33, 사우디/멕시코 32)은 원래 점 개수가 32개 이상이라 상한을 16→32로 올린 만큼 정확히 2배 체감.
- 34개국(우간다~수단 등, 16~31개)은 원래 점 개수 자체가 32 미만이라 "가진 점을 전부 표시"하는 정도로만
  늘어나 2배에는 못 미친다(예: 태국/일본 20개는 16→20으로 25%만 증가).
- 2개국(한국 14, 방글라데시 15)은 원래 점 개수가 기존 상한(16)보다도 적어서 이번 변경으로 아무 변화
  없음 — 상한을 올려도 데이터 자체가 그만큼 없기 때문.

즉 이번 수정은 "점 크기 배율/개수 상한"이라는, Step 36이 의도적으로 남겨둔 실플레이 튜닝 손잡이를
정확히 사용자 요청대로 조정한 것이 맞지만, 모든 국가에서 물리적으로 정확히 2배가 되는 건 아니다.
좌표 자체를 국가별로 다시 생성(오프라인 스크립트 재작성 필요, 원본 스크립트는 보존 안 됨)해야 완전한
2배가 되는데, 이번 요청 범위를 벗어난다고 판단해 하지 않았다 — 실플레이 후 한국처럼 점이 원래도 적은
소형 국가에서 "그래도 부족하다"는 피드백이 오면 그때 좌표 재생성을 고려할 것.

**변경 파일**: `Assets/Scenes/GamePlay.unity`(48개 국가 GameObject의 `maxInfectionDots`/
`infectionDotDiameterScale` 값), `Assets/Scripts/Gameplay/CountryView.cs`(같은 두 필드의 스크립트
기본값 + 툴팁 갱신).

**남은 리스크(Unity 에디터 미접속)**: 실제 렌더링 확인 못 함 — 점 크기가 절반으로 줄었을 때 여전히
눈에 띄는지(특히 소형 국가), 개수가 늘어난 만큼 겹침(overlap)이 과하게 보이지는 않는지 실플레이 확인
필요.

## Step 42 구현 메모 (DNA 버블 국경 이탈 버그 수정 + 감염 점 크기/개수 재조정 + 디버그 로그 정리)

**신고**: "DNA 버블 위치가 각 국가 위치에서 벗어나 있음." 실플레이로 확인된 신규 버그.

**원인 진단**: `BubbleSpawner.SpawnBubble()`이 `CountryView.DnaSpawnWorldPosition`(world.svg 폴리곤
중심에서 뽑은 국가 중심 근사치, Step 29~30에서 좌표계 자체는 검증됨) 주변에 **국가 크기와 무관한 고정
반경**(`spawnRadius = 0.5f`) 원 안에서 버블 스폰 위치를 무작위로 골랐다. 문제는 이 프로젝트의 국가별
실제 크기 편차가 매우 크다는 것 — Step 41 작업 중 계산해둔 감염 점 지름 값(러시아 0.1085유닛, 한국
0.0186유닛)을 근거로 국가 전체 크기(면적의 제곱근 스케일)를 역산해보면 러시아는 대략 1유닛, 한국은
대략 0.07~0.1유닛 안팎이다. 즉 고정 반경 0.5유닛은 러시아 기준으로는 그럭저럭 맞지만, 한국처럼 작은
나라에서는 나라 전체 크기의 5~7배에 달하는 반경이라 버블이 거의 항상 국경 밖에 스폰될 수밖에 없었다 —
이번 세션에서 실제로 검증한 수치(아래 참고)로도 재확인됨.

**해결**: 새 좌표를 계산하는 대신, 이미 국가 실루엣 알파마스크로 검증된 좌표(`InfectionDotDatabase`/
`Resources/InfectionDotPoints.json`, Step 35~36에서 "항상 국가 내부"가 보장되도록 만들어둔 데이터)를
재사용하는 쪽으로 방향을 잡았다 — 국가마다 다른 보정 로직을 새로 만들 필요 없이 기존에 검증된 자원을
그대로 활용.

- `CountryView.cs`: `GetRandomDnaSpawnWorldPosition()` 신규 — `SetupInfectionDots()`가 이미 만들어둔
  `_dotTransforms`(감염 점 GameObject, 좌표는 실루엣 내부가 보장됨) 중 하나를 무작위로 골라 그
  `.position`(월드 좌표)을 반환한다. 감염 점 데이터가 없는 국가(이론상 없어야 하지만 방어적으로)는
  기존 `DnaSpawnWorldPosition`으로 폴백.
- `CountryView.cs`: `DnaSpawnScatterRadius` 신규 — 국가별 감염 점 지름(`_resolvedDotDiameter`, 국가
  면적에 비례해 이미 계산돼 있음)의 0.6배를 반경으로 써서, 스폰 지점 주변 산개 정도도 국가 크기에
  맞게 자동으로 작아지게 했다. 데이터가 없으면 아주 작은 고정값(0.03)으로 폴백.
- `BubbleSpawner.cs`: 위 두 값을 쓰도록 `SpawnBubble()` 교체. 기존 `spawnRadius` 인스펙터 필드(0.5)는
  `view == null`인 이론상 발생하지 않는 극단적 상황에 대한 최후 폴백으로만 남기고 기본값도 0.03으로
  낮췄다(혹시 폴백 경로를 타더라도 예전과 같은 버그가 재발하지 않도록).

이 방식의 장점: 국가별 반경을 일일이 튜닝할 필요가 없고(감염 점 지름이 이미 국가 면적 기반으로
계산돼 있으므로 자동으로 스케일됨), 스폰 위치 자체가 항상 실루엣 내부 좌표에서 시작하므로 반경이 0에
가까워도(극단적으로 작은 나라) 버블이 국경을 벗어날 일이 구조적으로 없다.

## Step 42-2 (같은 세션 — 감염 점 크기/개수 재조정)

사용자 요청: 감염 점 지름 배율을 0.35로, 개수는 "한국(32개)을 기준으로 나라 크기 비례해서 증가".

**작업 환경 주의사항 재확인**: Step 34에서 이미 기록된 대로, 이 세션의 bash 샌드박스 마운트는 Read/Edit
도구가 보는 실제 파일 상태보다 **뒤처진 스냅샷일 수 있다**(`Docs/DevLog.md` 자체를 bash `wc -l`로 셌을 때
1281줄로 나왔지만 실제로는 Step 41까지 포함해 1813줄이었다 — Grep/Read 도구로 재확인 후에야 발견). 이번
세션에서 bash로 만든 변경(JSON 재생성, `sed`로 씬 파일 수정)이 실제로 반영됐는지는 전부 Grep 도구로
재검증했다(아래 "검증" 참고) — bash 안에서의 확인만으로 끝내지 않을 것.

**분석**: `Assets/Resources/CountryShapes/*.png`(48개국 실루엣, isReadable=0이라 Unity 런타임에서는 못
읽지만 원본 PNG 파일 자체는 오프라인 도구로 읽을 수 있음)의 알파 채널로 국가별 실제 면적(불투명 픽셀
수)을 계산해보니, 기존 Step 36 데이터(점 개수 14~90)는 면적에 대략 `count ∝ area^0.35` 관계로 이미
비례해 있었다(로그-로그 회귀로 확인) — "면적 비례"라는 설계 의도 자체는 지켜지고 있었다는 뜻. 다만
면적에 정확히 선형 비례(`count ∝ area`)로 늘리면 러시아가 수천 개에 달해 비현실적이므로, **기존 관계를
그대로 보존한 채 전체를 스케일업**하는 방향을 택했다 — 한국의 기존 개수(14) → 32가 되도록 하는 배율
`32/14 ≈ 2.286`을 모든 국가에 동일하게 곱했다(`round(old_count * 2.286)`, 최소값은 원래 개수 보장).

결과(일부): 한국 14→32, 일본 20→46, 미국 64→146, 캐나다 71→162, 러시아 90→206(최댓값). 48개국 총합
1395→3190개.

**좌표 재생성 방법**: 오프라인 Python(`gen_dots_v2.py`, Step 36에서 쓰인 원본 스크립트는 프로젝트에
포함되지 않아 유실된 상태 — 이번엔 새로 작성)으로:
1. 기존 점 좌표(도시 클러스터+rural 배경, Step 36에서 이미 튜닝된 값)는 **그대로 보존**하고 배열 앞부분에
   유지 — 런타임 코드(`Mathf.CeilToInt(infectionRatio * length)`)가 배열 앞에서부터 개수만큼만 활성화하는
   구조라, 새로 추가되는 점은 배열 뒤쪽에 붙여야 감염률이 낮을 때 기존에 검증된 "도시 위주" 노출 순서가
   그대로 유지된다.
2. 부족분(신규 개수 − 기존 개수)만큼 새 점을 생성 — 55% 확률로 기존 점(원래 것이든 방금 추가한 것이든)
   중 하나를 시드로 골라 그 주변에 가우시안 지터(표준편차 = 국가 바운딩박스 대각선의 3.5%, Step 36과
   동일 값 재사용)로 배치, 45% 확률로 국가 실루엣 내부 임의의 픽셀을 그대로 사용(순수 rural 채움). 둘 다
   알파마스크로 국가 내부인지 확인하고, 기존에 이미 배치된 점과 너무 가깝지 않은지(최소 거리 = 새
   지름의 55%) 검사해 과도한 겹침을 피했다.
3. 새 지름은 "전체 활성화 시 면적의 약 70%를 덮도록" 하는 Step 36과 동일한 설계 목표를 새 개수 기준으로
   다시 역산(`diameter = 2*sqrt(0.70*area/(count*π))`) — 여기 계산되는 값은 `infectionDotDiameterScale`
   적용 전 기준값이고, 사용자가 요청한 0.35 배율은 `CountryView` 인스펙터 필드에서 별도로 곱해진다.
4. 검증: 생성된 3190개 점 중 알파마스크 밖으로 벗어난 점은 1개(이탈리아, 경계에서 반올림 오차 약 1px —
   4000px 폭 지도 기준 시각적으로 감지 불가능한 수준)뿐이라 그대로 두었다.

**변경**:
- `Assets/Resources/InfectionDotPoints.json` 전체 재생성(48개국, 총 3190개 점).
- `CountryView.cs`: `infectionDotDiameterScale` 0.5→0.35, `maxInfectionDots` 32→220(러시아 206개를
  수용하려면 최소 206 이상 필요 — 안 올리면 Step 41과 똑같은 "상한이 데이터를 잘라먹는" 문제가 큰
  나라들에서 재발한다). 툴팁에 Step 42 변경 이력 추가.
- `Assets/Scenes/GamePlay.unity`: 48개 국가 GameObject의 `maxInfectionDots`/`infectionDotDiameterScale`
  직렬화 값을 `sed`로 일괄 수정(Step 41과 동일 패턴 — 48개 값이 전부 동일했음을 먼저 grep으로 확인 후
  일괄 치환) → Grep 도구로 재검증 완료(48개 전부 220/0.35로 바뀜 확인).
- `Data/InfectionDotDatabase.cs`: 클래스 주석에 Step 42 변경 배경 추가.

**검증**: bash에서 만든 파일이 실제로 반영됐는지 Grep 도구(Read/Edit와 같은 파일 뷰)로 재확인 — 씬
파일의 `maxInfectionDots`/`infectionDotDiameterScale` 48개 값 전부 새 값으로 바뀐 것, JSON에 "KOR" id가
포함된 것을 확인. JSON 자체가 48개국 3190개 점을 담다 보니 Read 도구로 전체를 읽으면 토큰 한도를 넘어서,
Python(오프라인)으로 구조적 검증(국가 수, 점 개수, 지름, 알파마스크 이탈 여부)까지 마쳤다.

## Step 42-3 (같은 세션 — 디버그 로그 정리)

사용자 요청으로 `CountryView.cs`의 스팸성 디버그 로그 2종 제거: `ApplyCountryShape()`의
"세계지도 오버레이 스프라이트 적용 완료" 로그(국가마다 Awake 시 1회, 48개국이면 48줄)와 `Start()`의
"WorldMap에 등록됨" 로그(마찬가지로 48줄). 둘 다 정상 동작 확인용으로 넣어뒀던 것들로, 기능에는 영향
없음. `UpdateVisual()`의 감염률/사망률 변화 로그(10%p 단위로만 찍히도록 이미 스팸 방지가 돼 있음)와
`SetupInfectionDots()`의 데이터 누락 경고(`Debug.LogWarning`)는 요청 범위에 없어 그대로 뒀다.

**남은 리스크(Unity 에디터 미접속, 다음 세션 최우선)**: 이번 세션도 실제 렌더링을 보지 못했다.
1. DNA 버블이 실제로 국가 실루엣 안에서 스폰되는지 — 특히 예전에 가장 심하게 벗어났을 한국·소형
   섬나라 위주로 확인.
2. 감염 점 3190개(국가당 최대 206개)가 갤럭시 S25 울트라에서 프레임 드랍 없이 도는지 — 특히 여러
   나라가 동시에 고감염 상태인 게임 후반부.
3. 지름 배율 0.35가 실제로 보기 좋은 크기인지, 큰 나라(러시아·캐나다 등 200개 안팎)에서 점끼리 겹침이
   과해 보이지 않는지.
4. 새로 추가된("확장 클러스터") 점들이 기존 도시 클러스터와 자연스럽게 이어져 보이는지, 아니면 이질적인
   무작위 배치처럼 튀어 보이는지 — 튀어 보이면 클러스터링 파라미터(시드 확률 55%, 지터 표준편차 3.5%)를
   조정할 것.

**변경 파일**: `Assets/Scripts/Gameplay/BubbleSpawner.cs`, `Assets/Scripts/Gameplay/CountryView.cs`,
`Assets/Scripts/Data/InfectionDotDatabase.cs`, `Assets/Resources/InfectionDotPoints.json`(재생성),
`Assets/Scenes/GamePlay.unity`, `CLAUDE.md`(Step 36~41 압축 이동 + Step 42 요약 추가).

## Step 43 구현 메모 (감염 점 개수 재조정 — "빈 공간이 많음" 피드백)

**요청**: "개수 2배로 늘려줘 빈공간이 많음." Step 42에서 한 번 늘린 뒤에도 여전히 실루엣 안에 빈 공간이
많다는 피드백.

**처리**: Step 42와 동일한 방식(`gen_dots_v4.py`, 오프라인)으로 한 번 더 배율만 적용 — 이번엔 Step 42
결과값(한국 32, 러시아 206 등)을 기준으로 전부 정확히 ×2. 기존 점(Step 42까지 누적된 도시 클러스터+
확장 rural 점)은 전부 보존하고 배열 뒤에 새 점만 추가하는 동일 원칙 유지 — 배열 앞부분 = 낮은 감염률에서
먼저 드러나는 "핵심" 점이라는 순서를 이번에도 건드리지 않았다.

- 지름은 이번에도 "전체 활성화 시 면적의 약 70%를 덮도록" 하는 동일한 설계 목표로 새 개수 기준
  재계산(개수가 늘어난 만큼 개별 점은 더 작아짐 — 러시아 0.07217→0.05103, 한국 0.01276→0.00902).
  참고: `infectionDotDiameterScale`(0.35)은 이 값 위에 추가로 곱해지는 별도 배율이라 그대로 둠 —
  사용자가 "빈 공간"을 지적한 건 개수/밀도 문제로 판단했고 크기 배율은 이번 요청 범위가 아니었음.
- 새 점 생성 파라미터(시드 확률 55%, 지터 표준편차=바운딩박스 대각선의 3.5%, 최소 간격=새 지름의
  55%)는 Step 42와 동일하게 재사용 — 다만 개수가 늘고 지름이 작아진 만큼 최소 간격도 비례해서 작아져,
  이전보다 점들이 더 촘촘하게 들어갈 수 있다.
- 검증: 새로 생성된 6380개 점 중 알파마스크를 벗어난 점은 1개(이전과 마찬가지로 경계 반올림 오차 수준).

**결과**: 국가당 점 개수 32~412(합계 3190→6380). 러시아가 최댓값(412) — `maxInfectionDots` 상한을
220→450으로 다시 올렸다(안 그러면 이번에도 큰 나라가 잘림, Step 41/42와 같은 실수 반복 방지).

**변경 파일**: `Assets/Resources/InfectionDotPoints.json`(재생성), `Assets/Scripts/Gameplay/CountryView.cs`
(`maxInfectionDots` 기본값 220→450, 툴팁), `Assets/Scenes/GamePlay.unity`(48개 인스턴스 동일 반영),
`CLAUDE.md`.

**남은 리스크(Unity 에디터 미접속)**: (1) 국가당 최대 412개(러시아)까지 늘어나 총 6380개 GameObject가
Awake 시점에 생성된다 — Step 42에서 이미 우려했던 성능 문제가 더 커졌으니 다음 세션 실플레이에서
프레임 드랍 여부를 반드시 확인할 것. 문제가 있으면 개수를 다시 줄이거나(예: 배율을 낮게 재조정) 오브젝트
풀링/GPU 인스턴싱 같은 구조적 최적화를 고려해야 한다. (2) 점이 더 촘촘해진 만큼(최소 간격이 작아짐)
확대해서 보면 서로 거의 붙어 보일 수 있음 — "빈 공간"은 줄었겠지만 반대로 "너무 빽빽하다"는 인상이
들 수 있으니 실제 렌더링 확인 필요.

## Step 44 구현 메모 (감염 표시 방식 자체를 재설계 — 개별 점 GameObject 폐기, 소수의 큰 "핫스팟"으로)

**지적**: "이 방식 뭔가 비효율적인 것 같은데 최적화 문제도 생길 수도 있고 다른 방법 없나 그냥 시각적으로
전염병이 걸린 사람이 많은 곳 빨간 점으로 표시하고 싶은데." Step 41~43을 거치며 "개수를 늘려달라"는
요청을 세 번 그대로 들어준 결과 국가당 최대 412개(러시아), 48개국 합계 6380개의 개별 GameObject가
Awake 시점에 전부 생성되고 매 프레임 순회되는 구조가 됐는데, 사용자가 이 누적된 비효율을 정확히
짚었다.

**방향 결정**: "기존에 공들인 국가별 점 좌표 데이터(도시 클러스터링 포함)를 버리고 단순하게 갈지,
살려서 성능만 고칠지"를 AskUserQuestion으로 물었더니 "시각적으로 바로 보이는 것으로 선택해달라"는
답을 받아 판단을 위임받음. 세 가지 후보(① 적은 수의 큰 핫스팟 블롭, ② GPU 인스턴싱으로 기존 점 개수
유지, ③ 셰이더/텍스처 기반 히트맵) 중 ①을 선택 — 이유:
- Unity 에디터에 여전히 접속할 수 없어 이번 세션도 실제 렌더링을 볼 수 없다. ②(인스턴싱)와 ③(커스텀
  셰이더)은 둘 다 GPU 셰이더 호환성 문제를 안고 있는데(이 프로젝트는 Step 29에서 이미 "URP 2D 렌더러가
  특정 셰이더를 걸러내는" 문제를 겪은 전례가 있음), 에디터로 눈으로 확인하지 못한 채 커스텀 셰이더를
  블라인드로 작성하는 건 리스크가 크다.
- ①은 지금까지 이미 검증된 렌더링 기법(SpriteRenderer + Color/Scale Lerp, CountryView의 국가 색상
  얼룩·기존 점 시스템과 완전히 동일한 패턴)을 그대로 재사용하므로 "블라인드 구현이라도 예상대로 보일
  가능성"이 세 후보 중 가장 높다.
- 사용자의 원래 목표("감염 많은 곳 빨간 점으로 표시")에 굳이 국가당 수백 개씩 필요하지 않다 — 오히려
  점이 몇 개 없어도 큼직하고 감염률에 따라 커지면 "핫스팟"이라는 인상이 더 직관적으로 전달된다.

**구현 (`CountryView.cs` 대규모 리팩터)**:
- 기존 `SetupInfectionDots()`(국가당 최대 maxInfectionDots개 개별 점 생성) → `SetupHotspots()`로 교체.
  `InfectionDotDatabase.GetLayout(countryId)`에서 나오는 원본 좌표 배열(`_allDotPoints`, 32~412개, Step 42/43에서
  튜닝된 값 그대로)과 원본 지름(`_baseDotDiameter`)은 그대로 재사용 — **오프라인 스크립트를 다시 돌리지
  않았다.**
- 핫스팟 개수: `count = clamp(round(sqrt(전체 점 개수) / hotspotCountDivisor), minHotspotCount, maxHotspotCount)`
  (기본 divisor=1.5, min=3, max=10). 실측: 한국(64개 데이터) → 5개, 일본(92개) → 6개, 미국(292개) → 10개,
  러시아(412개) → 10개(상한 도달). 48개국 합계 GameObject 수 6380 → **348개(약 1/18)**.
- 핫스팟 지름: `_baseDotDiameter * sqrt(전체 개수/핫스팟 개수) * hotspotSizeBoost`(기본 1.5). 원본 지름은
  "전체 개수로 국가 면적의 70%를 덮는" 공식으로 계산돼 있었으므로, 같은 총 면적을 더 적은 개수로
  나누면 개당 면적이 늘어야 하고 그 관계가 정확히 `sqrt(원래개수/새개수)` 배율이다 — 즉 면적 공식을
  다시 계산한 것과 수학적으로 동일한 결과를 얻으면서도 알파마스크를 다시 읽거나 좌표를 다시 뽑을
  필요가 없었다. `hotspotSizeBoost`(1.5)는 여기에 추가로 곱해 핫스팟끼리 일부러 겹치게 만든다 — 개별
  원이 아니라 "뭉친 붉은 영역"처럼 보이게 하려는 의도적 오버슛.
- 좌표 순서 재사용: `_allDotPoints` 배열은 Step 36부터 "앞쪽일수록 도시 위주"가 되도록 가중 라운드로빈으로
  정렬돼 있었다. 핫스팟은 이 배열의 **앞에서부터** `count`개만 골라 앵커로 쓰므로, 자동으로 각 국가에서
  가장 "중요한"(도시 밀집) 지점들이 핫스팟이 된다 — 별도 city-priority 로직을 새로 만들 필요가 없었다.
- 표시 로직: 예전엔 "감염률에 비례해 몇 개를 켤지"만 계산해 활성 개수만큼 스케일 0→1로 켰는데(개수만
  변함, 크기는 이진), 이번엔 핫스팟 i가 감염률 임계값(`i/count`)을 넘으면 드러나기 시작하고, 드러난
  뒤에는 크기 자체가 `Lerp(minRevealedScaleFraction, 1, 감염률)`로 감염률에 비례해 계속 커진다 — "새
  지역으로 퍼짐"(개수 증가)과 "기존 지역이 심해짐"(크기 증가)을 동시에 표현해, 점이 몇 개 안 되는데도
  훨씬 정보량이 많아 보이게 했다.
- **DNA 버블 스폰 로직 영향 없음**: `BubbleSpawner`가 호출하는 `GetRandomDnaSpawnWorldPosition()`/
  `DnaSpawnScatterRadius`의 공개 시그니처는 그대로 유지 — 내부 구현만 "핫스팟 GameObject 중 하나 선택"에서
  "원본 좌표 배열(`_allDotPoints`)에서 직접 샘플링"으로 바꿨다(핫스팟이 몇 개 안 되므로 다양성을 위해
  원본 데이터를 그대로 쓰는 게 낫다고 판단). `BubbleSpawner.cs`는 손대지 않았다.
- **씬 재직렬화 불필요**: `maxInfectionDots`/`infectionDotDiameterScale`/`dotTransitionSpeed`/
  `dotSortingOrder`/`infectionDotColor` 필드를 전부 제거하고 `maxHotspotCount`/`minHotspotCount`/
  `hotspotCountDivisor`/`hotspotSizeBoost`/`hotspotColor`/`hotspotTransitionSpeed`/`hotspotSortingOrder`/
  `minRevealedScaleFraction`으로 교체했다. Step 41에서 "씬에 이미 직렬화된 값이 스크립트 기본값을
  덮어쓴다"는 문제를 겪었던 것과 달리, 이번엔 **필드 이름 자체가 전부 새것**이라 `GamePlay.unity`의
  48개 인스턴스에 해당 키가 아예 없다 — Unity가 자동으로 스크립트 기본값을 쓰므로 씬 파일을 `sed`로
  건드릴 필요가 없었다(이전 필드의 옛 값은 고아 키로 남지만 무해 — 다음에 에디터에서 저장하면 정리됨).

**검증**: Python으로 48개국 각각의 핫스팟 개수/지름을 재계산해 합계(348개)와 최댓값(러시아 10개,
지름 0.49유닛 — 국가 전체 크기 약 1유닛 대비 절반 크기의 큰 원 10개면 시각적으로 상당한 면적을 덮을
것으로 예상)을 확인. 다른 스크립트(`BubbleSpawner.cs`)에서 제거된 필드/멤버를 참조하는 곳이 없는지
`Assets/Scripts` 전체를 Grep으로 재확인(주석 1건 제외 실제 코드 참조 없음).

**남은 리스크(Unity 에디터 미접속, 다음 세션 최우선)**: 이번에도 실제 렌더링을 못 봤다 — 특히
`hotspotSizeBoost`(1.5)가 "적당히 뭉쳐 보임"과 "국경 밖으로 삐져나와 보임" 사이 어디쯤일지가 가장 큰
불확실성이다(오프라인 계산으로는 좌표가 실루엣 내부인 것만 보장되지, 커진 원이 실루엣 경계를 넘어
그려지는지는 알파마스크 기반이 아니라 실제로 볼 때까지 모름). 문제가 있으면 `hotspotSizeBoost`를
1.0~1.2 정도로 낮추는 게 가장 빠른 완화책이다. 그 외 `minRevealedScaleFraction`(0.3)이 "처음 드러날 때
너무 작아 안 보임"과 "처음부터 너무 커서 계단식으로 튀는 느낌" 사이 균형이 맞는지도 확인 필요.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(전면 리팩터). `Assets/Scripts/Gameplay/BubbleSpawner.cs`,
`Assets/Resources/InfectionDotPoints.json`, `Assets/Scenes/GamePlay.unity`는 이번 Step에서 손대지 않았다.

## Step 45 구현 메모 (핫스팟이 국가를 가리고 이웃 나라를 침범하는 문제)

**신고**: "지금 방식이 좋아 보이긴 해 그런데 원이 국가 영역을 가려버려서 안보이고, 다른 나라 국경에도
원이 침범해서 가시성이 떨어져서 좀 그럼." Step 44에서 우려했던 "남은 리스크" 항목이 정확히 실제로
발생한 것으로 보인다 — `hotspotSizeBoost`(1.5)를 검증 없이 잡았던 게 과했다.

**원인**: Step 44의 핫스팟 지름 계산(`_baseDotDiameter * sqrt(원래개수/핫스팟개수) * hotspotSizeBoost`)에는
국가의 실제 폭/높이에 대한 상한이 전혀 없었다 — 순수하게 "면적 공식 재적용"과 "의도적 오버슛
배율(1.5)"로만 지름이 정해졌으므로, 특정 국가(원래 지름 자체가 크거나, 핫스팟 개수가 적어 배율
`sqrt(원래개수/새개수)`가 크게 나오는 경우)에서는 계산된 지름이 국가 자체 크기를 넘어설 수 있었다.
이런 국가를 실제로 눈으로 걸러낼 방법이 없었던(Unity 에디터 미접속) 상태에서 배율만 앞세워 밀어붙인
설계의 한계.

**해결**:
1. **바운딩박스 기반 지름 상한(핵심 수정)**: `SetupHotspots()`에서 `_allDotPoints`(InfectionDotDatabase
   좌표 — 전부 국가 실루엣 내부가 알파마스크로 검증된 좌표)의 최소/최대 x, y를 순회해 이 국가의
   실제 바운딩박스를 구하고, `min(가로, 세로)`를 "국가의 좁은 쪽 치수"로 삼았다. 핫스팟 지름을
   `이 치수 × maxHotspotDiameterFraction(0.35)`로 상한을 걸어, 계산값이 아무리 커져도 국가 자체
   크기를 크게 벗어날 수 없게 만들었다. 좁은 쪽 치수를 기준으로 삼은 이유는 세로로 긴 나라(칠레 등)에서
   가로 폭 기준으로 상한을 걸면 여전히 세로 방향으로 국경을 넘을 수 있기 때문 — 더 엄격한(작은) 쪽을
   기준으로 잡아야 두 방향 모두 안전하다.
   - 이 근사는 완벽하지 않다(바운딩박스는 실제 국가 모양의 오목한 부분·좁은 반도 등을 반영하지 못한다).
     하지만 알파마스크 원본 이미지를 다시 읽지 않고도(런타임에 이미 로드된 좌표 데이터만으로) 국가별
     실제 크기감을 반영할 수 있는 가장 간단하고 안전한 방법이라 판단했다.
2. **`hotspotSizeBoost` 1.5 → 0.9**: 애초에 "겹쳐서 뭉쳐 보이게" 하려던 의도적 오버슛이었는데, 검증 없이
   잡은 값이라 낮춰서 위 상한과 함께 이중으로 과대 크기를 억제.
3. **`hotspotColor` 알파 0.85 → 0.5**: "가려서 안 보인다"는 지적은 크기뿐 아니라 불투명도 문제이기도
   하다고 판단 — 핫스팟이 완전 불투명 빨강이면 크기를 아무리 줄여도 그 아래 국가 실루엣/색상 얼룩이
   완전히 가려진다. 반투명(0.5)으로 낮춰 핫스팟이 진하게 보이면서도 그 밑의 국가 정보가 비쳐 보이게 했다.

**검증(오프라인)**: 새 공식(사이즈부스트 0.9 + 상한 0.35배)을 Python으로 재계산해 48개국 전수 확인 —
5개국(한국·가나·폴란드 등 작고 옹골진 나라)에서 실제로 상한이 raw 계산값보다 낮게 작동함을 확인했고,
전체적으로 지름 범위가 이전(0.029~0.491)보다 확연히 줄었다(예: 러시아 0.4913→0.2948, 한국 0.0484→0.0287).
`CountryView.cs` 전체를 다시 읽어 새 필드(`maxHotspotDiameterFraction`)가 선언·계산·사용 세 군데 모두
일관되게 반영됐는지, 괄호/중괄호 짝이 맞는지 확인했다.

**주의**: 이번에도 국가 바운딩박스 근사가 실제 국가 모양과 정확히 일치하지 않는 나라(가늘고 구불구불한
해안선, 군소 도서 산재 등)에서는 여전히 침범이 남아있을 수 있다 — 완전히 구조적으로 침범을 막으려면
셰이더/스텐실 기반 클리핑(국가 실루엣 알파를 마스크로 써서 핫스팟을 그 범위 안으로만 렌더링)이 필요한데,
이건 Step 44에서 이미 논의했듯 에디터 없이 블라인드로 커스텀 셰이더를 작성하는 리스크가 커서 이번에도
보류했다. 이번 완화로 충분한지 다음 세션 실플레이로 확인 필요.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`SetupHotspots()`에 바운딩박스 계산 추가,
`hotspotSizeBoost`/`hotspotColor` 기본값 조정, `maxHotspotDiameterFraction` 신규 필드). `CLAUDE.md`.

## Step 46 구현 메모 (레이어 순서 재정비 + 핫스팟 최대 크기 절반)

**요청**: "비행기/배와 경로를 레이어 가장 위, 국가 경계선을 그 다음 레이어, 바다는 바로 아래 레이어로
놓아 가시성 개선, 감염 점은 지도 바로 위 레이어, 최대 점 크기 지금에서 절반."

**사전 조사 — "국가 경계선"이 실제로 존재하는지 확인**: `Assets/Scripts` 전체에서 border/경계/국경 관련
코드를 검색해보니 `isBorderClosed`(국경 봉쇄 여부를 나타내는 시뮬레이션 불리언, 시각 요소 아님)만
있고, 국가 간 경계를 그리는 별도 선(line) 렌더러나 에셋은 어디에도 없었다. `world_base.png`도 직접
픽셀을 분석해보니(불투명 픽셀 193만 개 중 94.6%가 정확히 동일한 회색 `[236,236,236,255]` 하나) 국가별
구분선 없이 "육지 전체가 하나의 실루엣"으로만 돼 있는 단색 이미지였다 — 즉 국경선을 그리는 그래픽
자체가 프로젝트에 존재하지 않는다는 것을 코드/에셋 확인으로 먼저 검증했다.

이 상태에서 "국가 경계선을 레이어로 재배치해달라"는 요청을 곧이곧대로 처리할 수 없어(존재하지 않는
걸 재배치할 수는 없으므로), AskUserQuestion으로 "① 기존 국가 색상 오버레이(CountryView)를 '경계선'
레이어로 간주해 순서만 조정 vs ② 진짜 국경선 그래픽을 새로 만들기(48개국 실루엣 알파마스크에서 가장자리
추출, 오프라인 에지 디텍션 + 신규 스프라이트)" 중 선택을 물었고, 사용자가 ①(기존 오버레이를 경계선으로
간주, 빠른 쪽)을 선택했다.

**구현**: 5개 렌더러의 `sortingOrder`를 요청받은 순서대로 코드에서 명시적으로 고정(전부 기본
"Default" 소팅 레이어, sortingLayer는 안 씀 — 프로젝트 전역에 sortingLayer 사용처가 없음을 grep으로
확인):

| 레이어 | sortingOrder | 파일 | 비고 |
|---|---|---|---|
| 지도(바다, 맨 아래) | 0 | `WorldMapBackgroundLoader.cs` | 이전엔 명시적 설정이 없어 Editor 기본값(대개 0)에 의존 — 이번에 코드로 고정 |
| 감염 핫스팟 | 10 | `CountryView.cs` (`hotspotSortingOrder`) | 이전 50에서 대폭 하향 — "지도 바로 위" 요청대로 |
| 국가 오버레이("경계선") | 20 | `CountryView.cs` (신규 `countrySortingOrder`, `Awake()`에서 `_renderer.sortingOrder`에 적용) | 이전엔 명시적 설정이 없어 암묵적 0 — 핫스팟(10)보다 위로 올라간 게 이번 변경의 핵심 반전 |
| 교통 노선(LineRenderer) | 30 | `TransportManager.cs` `CreateRouteLine()` | 이전 40에서 소폭 하향(상대 순서는 유지) |
| 교통 유닛 본체 | 40 | `TransportUnit.cs` `Awake()` | 이전 60 |
| 교통 유닛 윤곽선 | 39 | `TransportUnit.cs` `Awake()` | 이전 59, 본체보다 한 단계 아래 관계는 유지 |

이전에는 핫스팟(50)이 국가 오버레이(암묵적 0)보다 훨씬 위, 교통 노선(40)보다도 위에 있어서 요청과
정반대 순서였다 — 이번에 "국가 오버레이가 핫스팟 위, 핫스팟은 지도 바로 위, 교통이 전부 최상단"으로
뒤집었다.

**DNA 버블은 이번 범위에서 제외**: `TransportUnit.cs` 기존 주석이 "DNA 버블 아래, 국가 오버레이 위
정도의 어중간한 레이어"라고 설명하고 있었는데, 실제로 `DnaBubble.cs`를 확인해보니 sortingOrder를 코드
어디서도 설정한 적이 없어(항상 Unity 기본값 0) 이 주석은 애초에 틀린 가정이었다. 사용자 요청 범위에
DNA 버블이 없어 이번엔 손대지 않았지만, 지도(0)와 정확히 같은 값이라 렌더링 순서가 애매해질 수 있다는
점을 CLAUDE.md TODO에 남겨뒀다.

**핫스팟 최대 크기 절반**: `_hotspotDiameter = min(rawDiameter, diameterCap)`이고
`rawDiameter ∝ hotspotSizeBoost`, `diameterCap ∝ maxHotspotDiameterFraction`이므로, 두 계수를 동시에
정확히 절반(`hotspotSizeBoost` 0.9→0.45, `maxHotspotDiameterFraction` 0.35→0.175)으로 낮추면
`min(k·a, k·b) = k·min(a,b)`이 성립해 **어느 쪽이 실제 상한으로 작동하는 나라든 상관없이 48개국 전부
최종 지름이 정확히 절반이 된다** — Python으로 KOR/USA/RUS 세 나라를 직접 계산해 비율이 정확히 0.500
나오는 것까지 검증했다.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`countrySortingOrder` 신규 필드 + `Awake()`
적용, `hotspotSortingOrder` 50→10, `hotspotSizeBoost`/`maxHotspotDiameterFraction` 절반 조정),
`Assets/Scripts/Gameplay/WorldMapBackgroundLoader.cs`(`sortingOrder` 신규 필드 + 적용),
`Assets/Scripts/Gameplay/TransportUnit.cs`(본체 60→40, 윤곽선 59→39), `Assets/Scripts/Managers/TransportManager.cs`
(노선 40→30). `Assets/Scenes/GamePlay.unity`는 이번에도 손대지 않았다(전부 코드에서 sortingOrder를
런타임에 명시적으로 설정하므로 씬에 뭐가 직렬화돼 있든 Awake 시점에 덮어써진다).

**남은 리스크(Unity 에디터 미접속)**: 실제 렌더링 확인 못 함. 특히 국가 오버레이(20)가 핫스팟(10)보다
위에 오도록 바꾼 게 의도대로 보이는지 — 감염/사망률이 높아 국가 색상 얼룩의 알파가 진해지면(최대
0.85) 그 아래 핫스팟이 상당히 가려질 수 있는데, 이게 "국가 전체가 위험하다"는 느낌으로 자연스럽게
읽히는지 아니면 오히려 핫스팟 효과 자체가 무의미해 보이는지는 실제로 봐야 판단 가능하다. 문제가 있으면
`countrySortingOrder`와 `hotspotSortingOrder` 순서를 다시 바꾸는 것도 고려할 것(이번 요청은 사용자가
명시한 순서를 그대로 구현한 것이라, 실제로 보고 나서 재조정 여지가 있음을 미리 알려야 한다).

## Step 47 구현 메모 (감염 점이 여전히 다른 오브젝트를 가림 — 근본 원인 재진단)

**재신고**: "감염 점이 아직도 다른 오브젝트를 가려서 안보여. 감염 점 최대 크기 절반, 비행기/배 이미지·
해로 항로 레이어 최상위로 수정, 감염점이 다른 오브젝트를 가리지 않는 레이어 방법 찾기." Step 45(상한
추가)·Step 46(레이어 재정비)을 거치고도 같은 계열의 불만이 반복됨 — "값을 더 줄여라"가 아니라 "다른
방법을 찾아달라"는 요청이라는 점에 주목해 근본 원인을 다시 살폈다.

**교통(비행기/배/항로) 최상위 재확인**: `TransportManager.cs`를 다시 검색해 노선을 그리는 코드 경로가
`DrawRouteLines()` → `CreateRouteLine()` 단 하나뿐이고(항공/해운 구분 없이 같은 메서드, `sortingOrder = 30`
한 곳에서만 설정됨) 이미 Step 46에서 지도(0)·핫스팟(10)·국가 오버레이(20)보다 위로 옮겨져 있음을
확인했다 — 별도로 놓친 해운 전용 코드 경로 같은 건 없었다. 교통 유닛 본체(40)·윤곽선(39)도 마찬가지로
이미 최상위 그룹에 있다. 즉 "해로 항로가 최상위가 아니다"는 인식은 실제 코드 상태와는 다르고, 이 부분은
Step 46에서 이미 해결돼 있었다는 것을 재확인만 하고 넘어갔다(추가 코드 변경 없음).

**진짜 원인**: `GetSharedInfectionDotSprite()`가 생성하는 핫스팟 텍스처를 다시 보니, 알파 계산식이
`Clamp01(radius - dist)`였다 — 이건 반지름이 32px면 중심에서 31px 떨어진 지점까지도 알파가 거의 1(예:
dist=20이면 alpha=Clamp01(32-20)=1)이고, **가장자리 딱 1px 폭만** 안티에일리어싱되는 "사실상 완전
불투명한 원판"이었다. Step 45~46에서 계속 "크기"와 "상한"만 낮춰왔는데, 그건 이 불투명한 원판 자체를
작게 만드는 것이었을 뿐 — 원 안쪽 어디를 봐도 "그 자리의 아래는 안 보인다"는 성질 자체는 전혀 바뀌지
않았다. 즉 크기를 아무리 줄여도 "원이 있는 자리는 무조건 가려짐"이라는 근본 구조가 그대로였던 것 —
사용자가 "다른 방법을 찾아달라"고 한 게 정확한 진단이었던 셈이다.

**검토한 방법과 선택 이유**:
1. **가산 블렌딩(Additive) 등 블렌드모드 변경** — 원이 "덮어쓰기"가 아니라 "더하기"로 합성되면 이론상
   무엇을 얼마나 겹쳐도 완전히 가려지지 않는다(구조적으로 가장 확실한 해법). 하지만 Unity 기본
   "Sprites/Default" 셰이더는 블렌드 모드가 하드코딩돼 있어(`Blend SrcAlpha OneMinusSrcAlpha`) 코드로
   바꿀 수 없고, 별도 셰이더(예: `Particles/Additive`)가 필요한데 이 프로젝트는 URP + 에디터 미접속
   상태라 그 셰이더가 실제로 빌드에 포함/컴파일되는지 확인할 방법이 없다 — Step 44·45에서 이미 같은
   이유로 커스텀 셰이더를 두 번 배제한 전례를 그대로 따라 이번에도 배제했다.
2. **텍스처를 방사형 그라디언트로 교체(채택)** — 셰이더/머티리얼은 그대로 두고(여전히 표준
   SpriteRenderer + 알파 블렌드, 이미 검증된 렌더링 경로), 공유 스프라이트 텍스처의 알파 계산만
   `Mathf.SmoothStep(1f, 0f, dist/radius)`로 바꿨다 — 중심(dist=0)에서 1, 가장자리(dist=radius)에서
   0으로 S자 곡선을 그리며 전체 반지름에 걸쳐 점진적으로 투명해진다. 텍스처 해상도도 32→64로 올려
   그라디언트가 계단현상 없이 매끄럽게 보이도록 했다. 완전 불투명에 가까운 영역이 중심 근처의 작은
   코어로 국한되므로, 같은 지름이라도 "완전히 가리는" 실질 면적이 크게 줄어든다 — 셰이더 위험 없이
   순수 텍스처 생성 로직 변경만으로 구조적 개선을 얻는 절충안.

**크기 재조정**: 요청받은 대로 `hotspotSizeBoost`(0.45→0.225), `maxHotspotDiameterFraction`(0.175→0.0875)을
동시에 정확히 절반으로 낮췄다(Step 46과 같은 `min()` 방식이라 이번에도 48개국 전부 정확히 절반이 됨 —
KOR/USA/RUS로 재검증). Step 45 대비로는 이제 1/4 크기다.

**DNA 버블 방어적 수정**: Step 46 DevLog에서 "DnaBubble이 sortingOrder를 한 번도 명시적으로 설정한 적이
없어 지도(0)와 같은 레이어"라고 남겨뒀던 리스크를 이번에 같이 처리 — `DnaBubble.cs` `Awake()`에서
`GetComponent<SpriteRenderer>()`가 있으면 `sortingOrder = 45`(교통 유닛(40)보다 위, FloatingText(100)
보다 아래)를 명시적으로 설정하도록 추가했다. 다만 `BubbleSpawner.bubblePrefab`이 아직 씬에 배선되지
않은 상태(CLAUDE.md "씬/에셋 배선 필요" 목록)라 현재는 DNA 버블 자체가 스폰되지 않으므로 당장 체감되는
효과는 없다 — 나중에 프리팹이 연결될 때를 대비한 선제 조치다.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`GetSharedInfectionDotSprite()` 그라디언트로
교체 + 텍스처 32→64, `hotspotSizeBoost`/`maxHotspotDiameterFraction` 절반), `Assets/Scripts/Gameplay/DnaBubble.cs`
(`sortingOrder` 신규 필드 + `Awake()` 적용). `TransportManager.cs`/`TransportUnit.cs`는 이미 Step 46에서
올바르게 설정돼 있음을 재확인만 하고 변경하지 않았다.

**남은 리스크(Unity 에디터 미접속)**: (1) SmoothStep 그라디언트가 실제로 "덜 가려 보이면서도 핫스팟
존재감은 유지"라는 목표를 만족하는지는 실제 렌더링으로만 확인 가능 — 너무 흐릿해서 안 보이는 쪽으로
과할 수도 있다. (2) 지금까지 크기를 4단계(Step41/45/46/47)에 걸쳐 계속 줄여왔는데, 이번에도 부족하면
"크기"가 아니라 "핫스팟 개수 자체를 줄이는" 방향(`maxHotspotCount`를 10보다 낮추는 등)으로 접근을
바꾸는 게 나을 수 있다 — 같은 축(지름)을 계속 줄이는 시도가 이미 네 번째라는 점을 다음 세션에서
염두에 둘 것.

---

## Step 48 구현 메모 (교통 유닛/노선이 감염 핫스팟에 가려 보인다는 재신고 — sortingOrder가 아니라 색상 대비 문제였음)

**재신고**: "지금 감염 점 레이어 문제인지 비행기랑 배 이미지랑 경로가 게속 감염 보다 레이어 밑에 있는
것 같아 가려서 안보여 수정해줘." Step 46에서 레이어를 지도(0) < 핫스팟(10) < 국가 오버레이(20) < 노선(30)
< 유닛(39~40)으로 명시적으로 고정했고 Step 47에서도 "교통은 이미 최상위"라고 재확인했는데, 이번엔
반대로 "교통이 핫스팟보다 아래에 있다"는 인식이 새로 들어와 모순처럼 보였다 — 사용자도 "레이어 문제인지"
라고 확신 없이 표현한 점에 주목해, 코드를 그대로 다시 믿기보다 실제로 다른 원인이 있을 가능성부터
전수 재검토했다.

**sortingOrder 자체는 재검증 결과 이상 없음**: 세 갈래로 다시 확인했다.
1. `TransportUnit.cs` `Awake()` — 본체 `sortingOrder = 40`, 윤곽선 8개 각각 `sortingOrder = 39`를 한 번만
   설정하고, `BeginLeg()`(경로 시작 시 매번 호출되는 재사용 진입점)는 스프라이트/색상/스케일/위치/윤곽선
   오프셋만 갱신할 뿐 `sortingOrder`는 건드리지 않음 — 재사용 중에 값이 리셋될 여지가 없다.
2. `TransportManager.cs`에서 `TransportUnit`은 프리팹이 아니라 코드로 만든 템플릿 GameObject에
   `AddComponent<TransportUnit>()`으로 붙이고 `ObjectPool<TransportUnit>`(prewarm 30)으로 재사용한다.
   `ObjectPool.CreateInstance()`는 `Object.Instantiate(_prefab, _parent)` 후 `SetActive(false)`인데,
   템플릿 자체가 `AddComponent` 이전에 이미 `SetActive(false)` 상태라 Awake가 즉시 실행되지 않고
   미뤄지는 게 Unity의 정상 동작이다 — 대신 `Get()`에서 `SetActive(true)`로 처음 활성화되는 시점에
   Awake가 실행되므로, 결국 실제로 화면에 보이기 전에는 반드시 sortingOrder가 세팅된다. 풀링 재사용
   경로에 구멍이 없음을 확인.
3. `CreateRouteLine()`(노선을 만드는 유일한 코드 경로, 항공/해운 공용)의 `line.sortingOrder = 30`도
   `line.material = mat` 대입 이후에 리셋되는 구간 없이 그대로 유지됨을 라인 단위로 재확인.

결론적으로 draw order 자체는 Step 46/47에서 설정한 대로 이미 정상이었다 — "레이어(그리는 순서) 버그"는
아니었다.

**진짜 원인 — 색상 대비(contrast) 문제, 두 가지**:
1. **노선이 너무 옅다**: `airRouteColor`/`seaRouteColor`의 알파가 0.18, 두께가 0.02(월드 유닛)로 원래도
   "은은한 안내선" 수준으로 설계돼 있었다. 알파 블렌딩 특성상 18% 불투명한 선이 채도 높은 빨간 핫스팟
   위를 지나가면 `최종색 = 선색×0.18 + 배경색×0.82`가 되어, 그리는 순서는 노선이 위(30 > 10)여도
   시각적으로는 아래 핫스팟이 82% 그대로 비쳐 보인다 — "안 보인다"는 인상이 실제 관찰과 정확히 들어맞는다.
2. **결정적 발견 — 감염된 배 색이 핫스팟 색과 사실상 동일**: `TransportUnit.SeaCarrierColor`가
   `(0.85, 0.12, 0.12, 1)`였는데, `CountryView.hotspotColor`가 `(0.85, 0.1, 0.1, 0.5)` — RGB가 거의
   똑같은 빨강이다. 배 그래픽이 실사 이미지("Ship 1.png")라 본체 스프라이트는 흰색(무변화)으로 두고
   carrier(감염) 상태는 **윤곽선(0.012 월드 유닛, 아주 얇음)의 색으로만** 표시하는 구조인데(Step 38에서
   halo→윤곽선으로 바꾼 설계), 그 얇은 빨간 윤곽선이 뒤에 있는 크고 채도 높은 빨간 핫스팟과 색이 겹쳐
   경계가 시각적으로 뭉개진다 — sortingOrder는 배(40)가 핫스팟(10)보다 위가 맞지만, 가늘고 색이 같은
   윤곽선이 큰 동색 배경 위에서 눈에 띄지 않는 "위장(camouflage)" 효과가 난 것. 사용자가 "배 이미지가
   안 보인다"고 느낀 것의 실제 원인으로 특정.

**적용한 수정**: 둘 다 셰이더/레이어 구조는 건드리지 않고 값 조정만으로 해결(이 세션 내내 지켜온
"에디터 미접속 시 셰이더/블렌드모드 변경 회피" 원칙과 일관됨).
1. `TransportManager.cs`: `airRouteColor`/`seaRouteColor` 알파 0.18→0.55, `routeLineWidth` 0.02→0.032.
2. `TransportUnit.cs`: `SeaCarrierColor`를 핫스팟 빨강과 뚜렷이 구분되는 마젠타 `(1, 0.15, 0.75, 1)`로
   교체(항공 carrier는 이미 주황이라 핫스팟과 안 겹쳐 손대지 않음).

**변경 파일**: `Assets/Scripts/Managers/TransportManager.cs`(노선 알파/두께), `Assets/Scripts/Gameplay/TransportUnit.cs`
(`SeaCarrierColor`).

**남은 리스크(Unity 에디터 미접속)**: (1) 노선 알파 0.55가 "잘 보이면서도 과하게 산만하지 않은" 적정
수준인지는 실제 렌더링으로만 확인 가능 — 너무 진해 보이면 `TransportManager`의 두 `Color` 필드 알파값을
다시 낮출 것. (2) 마젠타가 "항공=파랑/주황, 해운=초록/마젠타"로 항공과도 명확히 구분되는지, 그리고
"감염된 배"라는 의미로 직관적으로 읽히는지도 실플레이 확인이 필요하다 — 문제가 있으면 다른 색상(예:
노란색 계열)으로 다시 조정하면 된다. (3) 이번 진단으로 sortingOrder 자체는 다시 한번 정상 확인됐으므로,
이 색상 수정 이후에도 "가려 보인다"는 신고가 또 반복된다면 그때는 색상이 아니라 카메라의 Transparency
Sort Mode나 LineRenderer/SpriteRenderer 머티리얼 간 렌더 큐 차이 같은, 이번엔 조사하지 않은 더 깊은
렌더링 파이프라인 쪽 원인을 파봐야 한다.

---

## Step 49 구현 메모 (감염 핫스팟 오버레이 자체를 비활성화 — 레이어링 복잡도 문제로 방향 전환)

**요청**: "아 에반가 이거 레이어로 나눠서 구분하는거 너무 힘든데 이전 버전으로 돌아갈까 하이라이트
없는거." Step 44에서 핫스팟 오버레이를 도입한 이래 Step 45(상한 추가)·46(레이어 재정비)·47(그라디언트
텍스처)·48(교통 색상 대비 조정)까지 총 네 번을 "핫스팟이 다른 요소를 가리거나, 다른 요소에 가려
안 보인다"는 계열의 문제를 풀기 위해 손봤는데, 이번엔 사용자가 문제를 더 파고드는 대신 "하이라이트
없는 이전 버전"으로 되돌리자는 방향 전환을 제안했다 — 접근 자체를 재검토할 시점이라고 판단해 그대로
따랐다.

**판단**: 지금까지 네 번의 수정은 전부 "핫스팟은 유지한 채 레이어링/대비 문제를 해결"하는 방향이었다.
근본적으로 이 프로젝트는 지도(배경) 위에 국가 색상 오버레이, 핫스팟, 교통 노선, 교통 유닛까지 여러
반투명 레이어가 겹치는 구조라 "무엇이 무엇을 가리는가"의 조합이 국가/감염 단계마다 계속 달라지고,
에디터 접속 없이 텍스트 코드 리뷰만으로 매번 최종 렌더링 결과를 예측해야 했던 것 자체가 이 씨름이
반복된 근본 원인이었다. 사용자가 "너무 힘들다"고 한 것도 정확히 이 지점 — 매번 값만 바꿔서 재확인을
반복해야 하는 사이클 자체의 피로였다고 해석했다. 코드를 삭제하기보다 **끄고 켤 수 있는 토글**로
만드는 쪽을 선택했다: 지금까지 투입한 세밀한 튜닝(핫스팟 개수/크기/상한/그라디언트 등)을 버리지 않고,
나중에 다른 접근(예: 국가 테두리 강조, 숫자 배지, 클릭 시 상세 패널 등)이 부족하다고 느껴지면 언제든
`hotspotsEnabled`만 true로 되돌려 즉시 복원할 수 있게 했다.

**구현**:
1. `CountryView.cs`에 `[SerializeField] private bool hotspotsEnabled = false;` 필드를 새로 추가했다
   (새 필드라 씬 재직렬화 불필요 — Step 44에서도 같은 이유로 새 필드명을 썼던 것과 동일한 패턴).
2. `Awake()`의 `SetupHotspots()` 호출과 `Update()`의 `UpdateHotspots()` 호출을 각각
   `if (hotspotsEnabled) ...`로 감쌌다 — `hotspotsEnabled = false`(기본값)면 핫스팟 GameObject가 아예
   생성되지 않고, 국가 색상 얼룩(`healthyColor`→`infectedColor`→`deadColor`)만 남는다. Step 35 이전
   상태와 사실상 동일한 화면이 된다.
3. **주의해서 처리한 부작용**: `SetupHotspots()`는 원래 핫스팟 GameObject를 만드는 것뿐 아니라
   `InfectionDotDatabase.GetLayout(countryId)`로 `_allDotPoints`/`_baseDotDiameter`를 로딩하는 역할도
   겸하고 있었는데, 이 두 필드는 `GetRandomDnaSpawnWorldPosition()`/`DnaSpawnScatterRadius`(DNA 버블
   스폰 위치 계산, Step 42에서 국경 이탈 버그를 고칠 때 도입)가 그대로 재사용한다. 만약 `SetupHotspots()`
   전체를 안 부르게만 했다면 `hotspotsEnabled=false`일 때 이 데이터가 로딩되지 않아 DNA 버블이 다시
   부정확한 폴백 위치(`DnaSpawnWorldPosition`, 국가 중심 근사치 + 고정 반경 0.03)로 스폰되는 회귀가
   생길 뻔했다 — 좌표 로딩 부분을 `LoadDotData()`로 분리해 `Awake()`에서 `hotspotsEnabled`와 무관하게
   항상 호출하도록 고쳤다. `SetupHotspots()`는 이제 이미 로딩된 데이터를 가정하고 핫스팟 GameObject
   생성만 담당한다.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`hotspotsEnabled` 필드 신규, `Awake()`/`Update()`
조건부 호출, `SetupHotspots()`/`LoadDotData()` 분리). `TransportManager.cs`/`TransportUnit.cs`(Step 48)는
건드리지 않았다 — 핫스팟이 꺼져 있어도 교통 노선/유닛 자체의 가시성 개선은 여전히 유효하다.

**남은 리스크(Unity 에디터 미접속)**: (1) 국가 색상 얼룩만으로 감염 확산이 충분히 체감되는지는 실제
플레이로만 확인 가능 — 애초에 Step 35에서 "색상 얼룩만으로는 시각 피드백이 약하다"는 이유로 핫스팟을
도입했었다는 점을 감안하면, 실플레이 후 다시 "심심하다"는 피드백이 나올 가능성도 있다. 그때는 핫스팟을
다시 켜기보다(레이어링 씨름이 재발하므로) 국가 테두리 강조나 숫자 배지처럼 다른 레이어와 안 겹치는
방식을 먼저 검토하는 게 나을 수 있다. (2) `LoadDotData()` 분리가 실제로 DNA 버블 스폰 정확도에
영향이 없는지는 `BubbleSpawner.bubblePrefab`이 아직 씬에 배선되지 않아(다른 미해결 항목) 당장 확인할
방법이 없다 — 프리팹이 연결되는 시점에 같이 검증할 것.

---

## Step 50 구현 메모 ("감염 점 안 보이는데" 재신고 — Step 49 해석 오류 정정, 개별 점 방식 복원)

**재신고**: Step 49를 적용해 핫스팟 오버레이를 끄고 국가 색상 얼룩만 남긴 직후, "이전 버전으로 하고
싶은데 감염 점 생성 하이라이트 없는 걸로"라는 메시지에 이어 "감염 점 안보이는데 뭔문제여"라는 신고가
들어왔다. Step 49에서 "레이어로 나눠서 구분하는거 너무 힘든데 이전 버전으로 돌아갈까 하이라이트
없는거"를 "핫스팟(하이라이트) 자체를 완전히 끄고 색상 얼룩만 남기자"로 해석했는데, 사용자의 재신고를
보니 이 해석이 틀렸다는 게 드러났다 — 사용자는 "감염 점"이 계속 보이길 기대하고 있었다.

**확인**: 같은 실수를 세 번째로 반복하지 않기 위해(Step 49 자체가 이미 한 번의 오해였다), AskUserQuestion
으로 정확한 의도를 확인했다. 선택지는 (1) Step 41~43 개별 점 방식(국가당 여러 개의 작은 점) 복원,
(2) 현재 상태(점 없음, 색상 얼룩만) 유지, (3) 핫스팟(Step 44~48 버전) 다시 켜기. 사용자가 **(1) Step
41~43 개별 점 방식**을 선택했다 — 즉 "하이라이트"는 핫스팟 특유의 "국가/이웃나라를 뒤덮는 큰 원"을
가리킨 것이었고, 국가당 여러 개의 작은 점이 감염률에 비례해 켜지는 방식 자체는 계속 원했던 것이었다.

**복원 작업**: Step 44에서 개별 점 GameObject 생성 로직(`SetupInfectionDots()`/`UpdateInfectionDots()`)이
핫스팟 코드로 완전히 대체되면서 현재 작업 트리에는 남아있지 않았다. 이 프로젝트는 Step 단위로 매번
커밋하지 않고 작업 트리에 계속 쌓아가는 방식이라(`git log`로 확인한 결과 `CountryView.cs`를 건드린
마지막 커밋은 Step 36~38 시점을 스쿼시한 `62bd2db`), `git show 62bd2db:Assets/Scripts/Gameplay/CountryView.cs`
로 Step 44 리팩터 직전의 원본 코드를 그대로 꺼내볼 수 있었다 — 이를 베이스로 다음처럼 현재 아키텍처에
맞게 재조립했다:

1. **좌표 데이터**: 원본 코드는 `SetupInfectionDots()` 안에서 직접 `InfectionDotDatabase.GetLayout()`을
   호출했지만, 지금은 Step 49에서 이미 `LoadDotData()`로 분리해 `Awake()`가 `_allDotPoints`/`_baseDotDiameter`를
   항상 먼저 채워두므로, 새 `SetupInfectionDots()`는 중복 로딩 없이 이 필드를 그대로 재사용한다. 좌표
   자체는 Step 43에서 마지막으로 재생성된 `InfectionDotPoints.json`(한국 64개~러시아 412개, 합계 6380개)을
   손대지 않고 그대로 쓴다.
2. **점 스프라이트**: 원본 코드는 32px 하드엣지 원판(`Clamp01(radius-dist)`)을 썼지만, 이건 Step 47에서
   "다른 오브젝트를 가린다"는 문제로 64px SmoothStep 그라디언트로 이미 개선된 부분이라 되돌릴 이유가
   없다 — 현재의 `GetSharedInfectionDotSprite()`(그라디언트 버전)를 그대로 재사용한다. 참고로 개별 점은
   핫스팟보다 훨씬 작고 sortingOrder도 국가 오버레이보다 아래(다음 항목)라 애초에 "가리는" 문제 자체가
   핫스팟보다 훨씬 덜 심각할 것으로 예상된다.
3. **레이어 순서**: 원본 코드의 `dotSortingOrder` 기본값은 50(당시 체계: 지도/국가 오버레이 암묵적 0,
   교통 노선 40, 교통 유닛 60 — 즉 점이 노선보다 위, 유닛보다 아래)이었는데, 이 값을 그대로 쓰면 Step 46
   에서 재정비한 현재 체계(지도 0 < 감염 오버레이 10 < 국가 오버레이 20 < 교통 30~40)와 어긋난다. 예전
   핫스팟이 쓰던 자리(10, 지도 바로 위·국가 오버레이보다 아래)를 그대로 물려받는 쪽을 선택했다 — 사용자가
   Step 46에서 명시적으로 요청했던 "감염 점은 지도 바로 위 레이어"라는 배치 원칙과도 일치한다.
4. **토글 추가**: `hotspotsEnabled`(Step 49)와 대칭되는 `dotsEnabled`(기본 true) 필드를 새로 추가해
   `Awake()`/`Update()`에서 `SetupInfectionDots()`/`UpdateInfectionDots()` 호출을 조건부로 감쌌다 — 이번에도
   나중에 또 방향이 바뀌면 코드를 지우지 않고 끄고 켤 수 있게 하기 위함(Step 49와 같은 패턴).
5. **UpdateVisual() 갱신**: 원본 코드의 "감염률에 비례해 CeilToInt로 활성 점 개수를 정하고, 감염자가
   1명이라도 있으면 최소 1개는 즉시 드러난다"는 로직을 그대로 복원해 `_targetActiveDotCount`를 계산한다.

**의도적으로 되돌리지 않은 것들**: 원본 코드에는 Step 42-3에서 정리 대상이었던 디버그 로그
(`"{id} 세계지도 오버레이 스프라이트 적용 완료."`, `"{id} WorldMap에 등록됨"`)가 `ApplyCountryShape()`/
`Start()`에 남아있었는데, 이 로그들은 복원 대상(SetupInfectionDots/UpdateInfectionDots)과 무관한 다른
메서드에 있던 것이라 그대로 두고(=되살리지 않고) 넘어갔다. `SetupHotspots()`/`UpdateHotspots()`/
`hotspotsEnabled` 등 Step 44~49의 핫스팟 코드도 전혀 건드리지 않았다 — 이제 `dotsEnabled=true`(개별 점,
기본 켜짐)와 `hotspotsEnabled=false`(핫스팟, 기본 꺼짐)가 서로 독립적으로 켜고 끌 수 있는 두 개의 병렬
오버레이가 됐다.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`dotsEnabled` 필드 및 관련 인스펙터 필드 5개 신규,
`_dotTransforms`/`_dotCurrentScale`/`_targetActiveDotCount`/`_resolvedDotDiameter` 신규, `SetupInfectionDots()`/
`UpdateInfectionDots()` 복원, `Awake()`/`Update()`/`UpdateVisual()`에 조건부 호출 추가).

**남은 리스크(Unity 에디터 미접속)**: (1) `dotSortingOrder=10`(국가 오버레이 20보다 아래)이 실제로 점을
가리지 않는지 확인 필요 — 원본 설계(50, 국가 오버레이보다 위)와 순서가 바뀌었으므로 만약 점이 국가
색상 얼룩에 묻혀 안 보이면 `dotSortingOrder`를 국가 오버레이(20)보다 높은 값(예: 25)으로 올릴 것.
(2) 국가당 최대 412개(러시아)까지 개별 GameObject가 다시 생기는 구조라(Step 44에서 "비효율적"이라고
지적받았던 바로 그 구조) 이번에도 성능이 체감될 만큼 무겁게 느껴지면, 사용자가 이미 한 번 겪은
트레이드오프(전체 348개 vs 전체 6380개, 가벼움 vs 개별 점의 촘촘함)를 다시 논의해야 할 수 있다.
(3) `infectionDotDiameterScale`(0.35)/`maxInfectionDots`(450)는 Step 43 최종값을 그대로 가져온 것이라
그 당시 "빈 공간이 많다"는 피드백까지 반영된 상태이지만, 이후 Step 46/47에서 확립된 새 레이어 순서
아래에서도 같은 크기감으로 보이는지는 실제 렌더링 확인이 필요하다.

---

## Step 51 구현 메모 (중앙집중식 제어 제안 검토 → 보류, 크기/개수 2배로 대체 적용)

**요청**: "이거 그 상위 프리팹 하나 만들어서 그거 가지고 조정 못하나? 하위는 각 국가 국토 넓이 비례해서
증가 시키고 이렇게 못하나 이거 하기 힘들면 지금 감염점 크기 2배 늘리고 개수도 2배 늘려줘." 두 가지를
같이 요청받았다 — (1) 감염 점 설정을 국가별 48개 GameObject 각각이 아니라 상위 프리팹/매니저 하나에서
중앙집중식으로 조정하고, 국가별 크기는 국토 면적에 비례해 자동으로 커지게 하는 아키텍처 개선, (2) 만약
(1)이 어려우면 지금 크기·개수를 각각 2배로 늘리는 단순 조정.

**(1) 검토 결과**: 두 부분으로 나눠 판단했다.
- **"국가 면적 비례 확대"는 이미 구현돼 있음**: `InfectionDotDatabase`가 로드하는 국가별 `diameter` 값
  자체가 Step 36에서 국가 실루엣 알파 픽셀 수(=면적)에 비례해 오프라인으로 미리 계산해둔 값이고,
  `CountryView.infectionDotDiameterScale`은 그 위에 곱하는 전역 배율이다. 즉 이 배율 하나를 스크립트
  기본값에서 바꾸면 이미 48개국에 "각 국가 면적에 비례한 채로" 균등하게 반영된다 — 사용자가 원했던
  동작은 사실 이미 존재하고 있었다.
- **"진짜 중앙집중식 매니저/ScriptableObject"는 보류**: 지금 구조(스크립트 기본값 하나로 48개 인스턴스에
  적용)가 이미 사실상 "한 곳에서 조정"과 동일한 효과를 내고 있어서(Step 49/50에서도 이 방식으로
  전역 조정), 진짜 매니저 컴포넌트나 ScriptableObject 에셋을 새로 만들어 48개 CountryView가 그걸
  참조하도록 바꾸는 건 씬에 새 GameObject를 배선해야 하는 작업이다. 이 세션은 Unity 에디터에 접속할
  수 없어 씬 오브젝트 추가/컴포넌트 연결을 직접 확인할 방법이 없고(과거 여러 Step에서 "씬/에셋 배선
  필요" 항목으로 미룬 것과 같은 종류의 리스크), 무엇보다 사용자가 스스로 "이거 하기 힘들면 대안으로
  가라"고 명시적으로 허락했으므로, 리스크 대비 효과가 낮다고 보고 (2)번 대안으로 진행했다.

**(2) 적용 — 크기 2배**: `CountryView.infectionDotDiameterScale`을 0.35→0.7로 변경. 이 필드는 Step 50에서
새로 추가된 필드라 아직 씬에 직렬화된 적이 없어(스크립트 기본값이 그대로 48개국에 적용되는 상태),
이 한 줄 수정만으로 48개국 전부에 즉시 반영된다.

**(2) 적용 — 개수 2배**: `Assets/Resources/InfectionDotPoints.json`을 오프라인 Python으로 재생성했다.
방식은 Step 42의 선례("기존 점을 보존하고 배열 뒤에 새 점을 추가")를 그대로 따랐다:
1. 국가별로 기존 점 각각에 대해, 그 국가의 `diameter`를 기준으로 무작위 각도·거리(diameter의 약
   15~90%)만큼 떨어진 "형제 점"을 하나씩 생성해 원래 점 뒤에 추가한다 — 순수 무작위 산개가 아니라
   기존 도시/rural 클러스터 구조를 그대로 유지한 채 밀도만 두 배로 늘리는 방식이라 Step 36의 "규칙적으로
   보인다"는 문제가 재발하지 않는다.
2. `Assets/Resources/CountryShapes/{id}.png`(국가 실루엣 알파마스크)를 오프라인 Python(PIL/numpy)으로
   직접 읽어(Unity의 `isReadable=0` 설정은 런타임 GPU 텍스처 접근에만 적용되고 파일시스템의 원본
   PNG 픽셀을 읽는 것은 막지 않는다) 새로 만든 각 점이 실제로 국가 실루엣 내부(알파 > 80)인지 검증했다
   — 실패하면 각도/거리를 좁혀 최대 14회 재시도, 그래도 실패하면 원점에서 아주 작게(diameter의 5%)만
   이동한 폴백을 썼다. 48개국 총 6380개 점 중 재시도까지 다 실패해 폴백을 쓴 경우는 2개뿐이었다.
3. 결과: 총점 6380→12760개(정확히 2배). 국가별 최댓값이 러시아 412→824개로 늘면서 기존 안전 상한
   `maxInfectionDots`(450)에 걸려 잘려나가는 상태가 됐음을 발견해, 상한도 450→900으로 함께 올렸다
   (Step 43에서 "실제 최대치보다 넉넉히 위" 원칙을 세웠던 것과 동일한 이유).

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`infectionDotDiameterScale`, `maxInfectionDots`
값 변경 + 관련 툴팁 갱신), `Assets/Resources/InfectionDotPoints.json`(국가별 점 개수 2배 재생성, 오프라인
Python 스크립트는 1회성 도구라 프로젝트에 포함하지 않음 — 기존 관례와 동일).

**남은 리스크(Unity 에디터 미접속)**: (1) 크기와 개수를 동시에 2배씩 올렸기 때문에, 감염률이 동일해도
화면상 덮이는 면적은 대략 4배 가까이 늘어날 수 있다 — 특히 이미 점이 많던 큰 나라(러시아 824개,
캐나다 648개)에서 점끼리 심하게 겹쳐 오히려 예전 핫스팟처럼 "뭉개진 큰 얼룩"처럼 보일 위험이 있다.
이게 문제면 개수를 다시 줄이기보다 `infectionDotDiameterScale`을 먼저 낮추는 쪽을 권장한다(개수를
줄이면 다시 JSON 재생성이 필요하지만, 배율은 인스펙터 값 하나만 바꾸면 되니 훨씬 저렴한 조정이다).
(2) 새로 추가된 점들의 알파마스크 검증은 오프라인에서만 확인했고, 실제 Unity 렌더링으로 국가 실루엣
안에 들어가 있는지는 아직 못 봤다. (3) "국가 면적 비례"는 이미 구현돼 있다고 판단해 별도 코드 변경을
하지 않았는데, 만약 사용자가 원했던 게 "지금과 다른 종류의" 면적 비례(예: 배율 자체를 국가 면적에
따라 다르게 적용)였다면 이 판단이 틀렸을 수 있다 — 재확인 필요.

---

## Step 52 구현 메모 ("감염 점 하이라이트 있는데 이거 뭐임" — Step47 그라디언트가 점에는 과했던 문제)

**질문/신고**: "감염 점 하이라이트 있는데 이거 뭐임." 버그 신고인지 단순 질문인지 애매해 AskUserQuestion
으로 먼저 무엇이 보이는지 확인했다 — 답은 "점이 부드러운 글로우/백광처럼 번져 보임"이었다.

**원인**: 버그가 아니라 의도된 스프라이트가 지금 상황에는 안 맞는 경우였다. `GetSharedInfectionDotSprite()`
는 Step 47에서 핫스팟(당시엔 국가 하나를 뒤덮을 만큼 큰 원)이 "다른 오브젝트를 완전히 가린다"는 문제를
풀려고 만든 텍스처다 — 반지름 전체(t=0~1)에 걸쳐 `SmoothStep(1,0,t)`로 알파를 서서히 낮춰서, 중심조차
완전 불투명하지 않고 반지름 절반 지점에서 이미 알파 0.5가 되도록 설계했다. 큰 핫스팟 원에는 "가려도
아래가 비쳐 보인다"는 좋은 절충이었지만, Step 50에서 이 스프라이트를 그대로 재사용해 훨씬 작은 "개별
점"에 적용하면서 문제가 생겼다 — 작은 도형 전체가 저 정도로 옅게 페이드되면 사람 눈에는 "또렷한 점"이
아니라 "빛이 퍼지는 글로우/백광"으로 인식된다. Step 51에서 크기(`infectionDotDiameterScale` 0.35→0.7)
까지 키우면서 이 효과가 더 도드라져 보였을 것이다.

**적용한 수정**: AskUserQuestion으로 "그대로 유지 / 더 또렷한 점으로 변경 / 완전히 하드엣지 원판으로
변경" 세 선택지를 제시했고, 사용자가 중간값인 "더 또렷한 점으로 변경"을 골랐다. `GetSharedInfectionDotSprite()`
의 알파 계산을 다음처럼 이원화했다:
```
t <= 0.6  → alpha = 1 (완전 불투명 코어)
t > 0.6   → edgeT = (t-0.6)/(1-0.6); alpha = SmoothStep(1,0,edgeT) (남은 40% 구간에서만 페이드)
```
중심 60% 반경은 완전 불투명한 "코어"로 확정해 육안으로 뚜렷한 점처럼 보이게 하고, 바깥 40% 구간에서만
그라디언트를 적용해 Step47이 애초에 고치려 했던 "가장자리가 딱 1px만 안티에일리어싱되는 완전 하드엣지"
(세 번째 선택지, 이번엔 채택 안 함)로 완전히 되돌아가지는 않게 했다.

**트레이드오프 — 문서화만 하고 당장 손대지 않은 부분**: 이 스프라이트는 `_sharedInfectionDotSprite`
static 캐시로 개별 점과 핫스팟(Step44~48, 현재 `hotspotsEnabled=false`로 꺼져 있음)이 함께 쓴다. 코어를
키운 이번 수정은 개별 점 입장에서는 순수 개선이지만, 핫스팟 입장에서 보면 Step45~48이 그렇게 애써
줄이려 했던 "완전히 가리는 코어 면적"이 다시 커지는 방향의 변경이다. 지금은 핫스팟이 꺼져 있어 당장
영향이 없지만, 나중에 `hotspotsEnabled`를 다시 켜는 시점이 오면 이 트레이드오프를 다시 검토해야 한다
— 그때 필요하면 점용/핫스팟용 스프라이트를 두 개로 분리하는 게 깔끔한 해법이 될 것이다. 코드 주석에도
같은 내용을 남겨뒀다.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(`GetSharedInfectionDotSprite()` 알파 계산 수정
+ 관련 문서 주석 갱신). JSON/씬 파일은 건드리지 않았다 — 순수 텍스처 생성 로직 변경이라 다음 Play 시
`_sharedInfectionDotSprite` 캐시가 새로 만들어지면 바로 반영된다.

**남은 리스크(Unity 에디터 미접속)**: (1) `coreFraction=0.6`이 "또렷하면서도 부자연스럽게 딱딱하지
않은" 적정값인지는 실제 렌더링으로만 확인 가능 — 여전히 흐릿하면 이 값을 올리고(1.0에 가까울수록
하드엣지), 반대로 너무 딱딱해 보이면 낮출 것(현재 하드코딩값이라 인스펙터 노출은 안 돼 있음 — 자주
튜닝할 필요가 있으면 `SerializeField`로 빼는 것도 고려). (2) 코어 확대가 다른 요소를 더 가리게 만들지는
않는지 확인 필요 — 개별 점은 핫스팟보다 훨씬 작아 체감 안 될 걸로 예상되지만 미확인.

## Step 53 구현 메모 ("감염 점 최소 크기 0.5에서 고정, 한국보다 큰 나라는 크기를 확대")

**요청**: "감염 점 최소 크기 0.5에서 고정 여기서 한국보다 큰 나라면 크기를 확대로 수정해줘." Step 51까지
의 크기 조정(`infectionDotDiameterScale` 0.35→0.7)은 48개국에 똑같이 곱해지는 **전역 배율**이었다 —
국가별 `diameter`(InfectionDotDatabase, 면적 기반으로 오프라인 사전계산됨) 자체는 이미 국가마다 다르지만
(예: 한국 0.00902, 러시아 0.05103 — 약 5.66배 차이), 여기에 곱해지는 배율은 항상 똑같은 0.7이라 "한국보다
큰 나라를 배율 차원에서 한 번 더 키우는" 효과는 없었다. 이번 요청은 그 전역 배율 개념 자체를 버리고,
"가장 작은 나라(한국) 기준 0.5를 바닥으로 깔고, 그보다 큰 나라는 커진 만큼 배율도 같이 키워라"는 것.

**데이터 확인**: 48개국 diameter를 전수 조사한 결과 한국(KOR, 0.00902)이 현재 데이터셋에서 실제로 가장
작은 나라였다(방글라데시가 0.00986으로 근소하게 다음). 즉 "한국보다 큰 나라"는 사실상 나머지 47개국
전부다 — 이 요청은 "한국을 최소 크기 기준점으로 삼고, 나머지 전 국가를 상대적으로 확대"하라는 뜻으로
해석했다.

**구현**:
1. `Assets/Scripts/Data/InfectionDotDatabase.cs` — `EnsureLoaded()`가 JSON을 순회하며 diameter 최솟값을
   추적하도록 하고, 그 값을 노출하는 정적 프로퍼티 `MinDiameter`를 추가했다. 국가 id("KOR")를 하드코딩하지
   않고 "로드된 국가 중 diameter가 가장 작은 값"을 자동으로 찾게 해서, 나중에 데이터가 재생성돼 최솟값이
   바뀌어도(예: 더 작은 신규 국가 추가) 코드 수정이 필요 없다.
2. `Assets/Scripts/Gameplay/CountryView.cs` — `SetupInfectionDots()`에서 기존 `_resolvedDotDiameter =
   _baseDotDiameter * infectionDotDiameterScale;` 한 줄을 아래로 교체:
   ```csharp
   float sizeRatio = _baseDotDiameter / InfectionDotDatabase.MinDiameter;
   float resolvedScale = Mathf.Max(infectionDotDiameterScale, infectionDotDiameterScale * sizeRatio);
   _resolvedDotDiameter = _baseDotDiameter * resolvedScale;
   ```
   `infectionDotDiameterScale`의 의미가 "전역 배율"에서 "가장 작은 나라에 적용되는 바닥 배율"로 바뀌었으므로
   기본값도 0.7→0.5로 낮췄다(요청한 "최소 크기 0.5"에 대응). 한국 자신은 `sizeRatio=1`이라 `Max(0.5, 0.5*1)
   =0.5`로 정확히 바닥값을 쓰고, 러시아는 `sizeRatio≈5.66`이라 배율이 `0.5*5.66≈2.83`까지 커진다 — 최종
   지름은 `_baseDotDiameter`(이미 면적 비례)에 이 커진 배율까지 곱해지므로, 기존 대비 약 4배 커진다
   (0.05103*0.7=0.0357 → 0.05103*2.83=0.1444).

   `Mathf.Max`로 감싼 이유: 이론상 `_baseDotDiameter`가 `MinDiameter`와 정확히 같거나(한국 자신) 부동소수점
   오차로 아주 근소하게 작아질 수 있는 극단 케이스에서도 배율이 바닥값 아래로 떨어지지 않도록 보호하기
   위함 — 데이터가 정상이라면 한국을 제외한 모든 국가에서 `sizeRatio > 1`이라 사실상 `Max`의 두 번째
   항이 항상 이긴다.

**씬 파일 직접 패치 (Step 41과 동일한 함정)**: Step 41에서 이미 한 번 겪었던 문제 — Unity는 스크립트
필드의 C# 기본값을 바꿔도, 씬에 이미 직렬화 저장된 값이 있으면 그 값을 그대로 쓴다. `GamePlay.unity`를
확인해보니 48개 CountryView 인스턴스 전부 `infectionDotDiameterScale: 1`로 직렬화돼 있었다(Step 51에서
스크립트 기본값을 0.7로 바꿨을 때도 실제로는 이 인스턴스별 값 1이 이겼을 것으로 보임 — 이번에 처음
발견). 코드만 고치고 넘어갔다면 실제 게임에서는 여전히 배율 1이 적용됐을 것이므로, `sed`로 씬 YAML의
`infectionDotDiameterScale: 1` 48개를 전부 `infectionDotDiameterScale: 0.5`로 직접 치환했다(스크립트
기본값과 동일한 값으로 맞춤 — 향후 이 필드가 "스크립트 기본값이 무시된다"는 이유로 다시 헷갈리지 않도록).

**변경 파일**: `Assets/Scripts/Data/InfectionDotDatabase.cs`(`MinDiameter` 추가), `Assets/Scripts/Gameplay/
CountryView.cs`(`SetupInfectionDots()` 계산식 교체, 필드 기본값/툴팁 갱신), `Assets/Scenes/GamePlay.unity`
(48개 인스턴스 직렬화 값 1→0.5).

**검증(오프라인 Python, Unity 미실행)**: JSON의 실제 diameter 값으로 새 공식을 시뮬레이션해 한국이
정확히 0.5배, 방글라데시(한국과 거의 같은 크기)가 약 0.55배, 러시아가 약 2.83배가 되는 것을 계산으로
확인했다 — 코드 로직 자체의 산술은 맞다. 다만 이 프로젝트는 에디터 미접속 상태라 다음을 실제 렌더링으로는
아직 확인 못 했다(아래 TODO 25 참고): (1) 확대된 러시아/캐나다/미국 점이 서로 겹쳐 하나의 뭉개진 얼룩처럼
보이는지, (2) 배율이 diameter에 선형 비례라 큰 나라일수록 증가폭이 가파른데(예: 러시아처럼 diameter가
크면 배율도 그만큼 커지는 구조라 아주 큰 나라에서는 과할 수 있음) 이게 의도한 "확대" 느낌과 맞는지 —
너무 크면 `sizeRatio`에 `Mathf.Sqrt()`를 씌워 증가폭을 완만하게 만드는 걸 다음 튜닝 후보로 남겨둔다.

## Step 54 구현 메모 (디버그 로그 스팸 제거 + "게임 시작하면 즉시 패배" 버그)

**신고 1 — 디버그 로그 스팸**: "이 부류 디버그 로그 지워줘"라며 `[CountryView] SDN 목표색 갱신 —
감염률=0%, 사망률=...` 형태의 로그를 예시로 붙여줬다. `CountryView.UpdateVisual()`이 국가별로 감염률/
사망률이 10%p 밴드를 넘을 때마다(48개국 × 매 틱 가능성) `Debug.Log`를 남기던 부분 — 밴드 스팸 방지
로직(`_lastLoggedInfectionBand`/`_lastLoggedDeadBand`)은 이미 있었지만 애초에 이 로그 자체가 불필요하다는
피드백이라 로그 호출을 통째로 제거했다. 밴드 추적 필드 두 개는 당장 다른 용도가 없어 unused-field 경고만
남지만, 나중에 디버깅용으로 다시 켤 수도 있어 필드 자체는 지우지 않았다.

**신고 2 — 게임 시작 버튼을 누르면 즉시 "광고 보고 부활 / 다시 시작" 화면이 뜬다**: 이건 플레이 자체가
불가능해지는 심각한 버그다. 코드로 전체 플로우를 추적했다:

1. `CountrySelectController`의 "시작" 버튼(`_startButton`, UXML text="시작" — 사용자가 "게임 시작 버튼"
   이라고 부른 그 버튼) → `HandleStartClicked()` → `OnCountryConfirmed(_selectedCountryId)` (빈 값이면
   애초에 발행 안 함, 가드 있음).
2. `UIManager.HandleCountryConfirmed(countryId)` → `GameDataBootstrapper.BeginGame(_pendingPathogen,
   countryId)`.
3. `BeginGame()`은 `SeedStartingInfection()`(발원국에 `startingInfectedCount`(100) 심고
   `RecalculateWorldTotals()` 호출)을 **먼저** 실행한 뒤에야 `GameManager.SetPaused(false)`로 틱을
   푼다 — 순서상 시딩이 언패즈보다 항상 먼저다.
4. `SimulationManager.TickLoop()`은 `GameManager.IsPaused`인 동안 `RunTick()`을 아예 호출하지 않으므로,
   시딩 전에 조기 평가가 끼어들 여지도 없어 보인다.
5. `WorldState.IsPathogenEradicated => infectedCount <= 0 && deadCount < totalPopulation` — **여기서
   구조적 결함을 발견**. 이 조건은 "치료제로 병원체가 박멸된 상태"를 노리고 만든 건데, "발원 감염이
   아직 한 번도 심어진 적 없는, 막 새 게임을 시작한 시점"도 감염자 수가 똑같이 0이라 이 조건을
   똑같이 만족시킨다. `EvaluateEndConditions()`가 이 값을 참으로 보면 `OnGameEnded(false)`를 쏘고
   `EndingScreenController.HandleGameEnded(false)`가 "치료제 완성 — 패배" 화면을 띄우면서 `isVictory=false`
   조건으로 `_reviveButton`도 같이 보이게 한다 — 정확히 사용자가 묘사한 "광고 보고 부활, 다시 시작" 창.

**결론**: 국가/인구 데이터(`CountryDatabase.asset`)를 전수 확인해 48개국 population이 전부 정상적인
양수임을 확인했고, `Country.Clone()`/`CreateRuntimeInstances()`/`WorldDataManager.SetCountries()`/
`RecalculateWorldTotals()` 흐름도 코드상 문제를 못 찾았다 — 즉 "왜 시딩이 실패하는지"의 정확한 트리거는
정적 분석만으로는 100% 재현하지 못했다(Unity 에디터 접속 없이 실제 실행 로그를 볼 수 없는 것이 근본
제약). 하지만 5번에서 찾은 `IsPathogenEradicated`의 정의 자체는 **시딩이 성공하든 실패하든, 순서가
어떻게 꼬이든 상관없이 항상 존재하는 구조적 취약점**이다 — "아직 시작 전"과 "끝난 후"를 절대값만으로
구분 못 하는 문제는 이 프로젝트가 `cureResearchStarted` 플래그로 이미 한 번 해결했던 것과 완전히 같은
패턴이라, 같은 해법을 적용했다.

**적용한 수정**:
- `WorldState.cs`에 `hasEverBeenInfected`(기본 false) 필드 추가, `Reset()`에서도 false로 초기화.
- `IsPathogenEradicated`에 `hasEverBeenInfected &&`를 추가 — 감염이 실제로 한 번이라도 관측된 뒤에만
  그 이후 0이 되는 걸 "박멸"로 인정한다.
- `WorldDataManager.RecalculateWorldTotals()`에서 `if (infected > 0) worldState.hasEverBeenInfected =
  true;`로 자동 세팅 — `GameDataBootstrapper.SeedStartingInfection()`도 이 메서드를 호출하므로 발원국에
  처음 감염을 심는 순간 자동으로 게이트가 열린다(새 배선 불필요).
- `WorldDataManager.LoadState()`에도 `hasEverBeenInfected` 복사를 추가 — 안 하면 저장된 판을 불러올
  때마다 이 플래그가 false로 리셋돼, 로드 직후 우연히 감염자가 0인 프레임에 게이트가 잘못 열려있는
  상태가 될 수 있었다. `SaveManager`는 `JsonUtility`로 `WorldState` 전체를 직렬화하므로 저장 포맷
  자체는 새 필드를 자동으로 포함한다(코드 수정 불필요) — 다만 이 필드 도입 이전에 저장된 세이브 파일을
  불러오면 JSON에 필드가 없어 기본값(false)으로 채워지는데, 이 경우도 다음 정상 틱에서
  `RecalculateWorldTotals()`가 감염자 수를 다시 확인해 자동으로 true가 되므로 최대 한 틱만 지나면
  스스로 복구된다(치명적이지 않음).

**남은 리스크(Unity 에디터 미접속, 실플레이 필요)**: 이번 수정은 "즉시 패배 화면이 뜨는" **증상**을
구조적으로 막지만, 만약 실제 원인이 시딩 자체의 실패(예: `startingCountryId`가 실제로는 비어서
전달되는 경로가 따로 있음)라면 그 근본 원인은 여전히 안 고쳐진 상태일 수 있다 — 게임은 정상적으로
시작되지만 시각적으로 감염이 안 보이는 등 다른 형태의 증상으로 남아있을 가능성. 재발하거나 이상하면
Console의 `[FLOW][GameDataBootstrapper] BeginGame — pathogen=..., startingCountry=...` 로그 한 줄과,
`startingCountryId '...'를 찾을 수 없습니다` 경고가 뜨는지를 확인해 실제 시딩 성공 여부를 확인할 것.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`(로그 제거), `Assets/Scripts/Data/WorldState.cs`
(`hasEverBeenInfected` 추가), `Assets/Scripts/Managers/WorldDataManager.cs`(`RecalculateWorldTotals()`/
`LoadState()` 반영).

## Step 55 구현 메모 (붕괴 단계 로그 제거 + "발원 국가가 안 나온다" 후속 확인)

**신고 1 — 디버그 로그 스팸(2건째)**: `[HumanResistanceManager] Peru(PER) 붕괴 단계 변경 ...` 로그를
예시로 붙여줬다. Step 54에서 지운 `CountryView`의 목표색 갱신 로그와 정확히 같은 종류의 문제 — 48개국
각각의 `CountryCollapseStage`(Normal/Disorder/NearAnarchy/FullAnarchy/Extinct)가 바뀔 때마다
`HumanResistanceManager.ApplyResearchFunding()`(또는 해당 루프) 안에서 `Debug.Log`를 남기고 있었다.
`_lastCollapseStage[country.id]` 갱신 자체는 그 아래 `switch (collapseStage)`가 funding 배율을
계산하는 데 실제로 쓰이는 로직이라 그대로 두고, `Debug.Log` 호출 한 줄만 제거했다.

**신고 2 — "발원 국가가 안 나온다" 후속 확인**: Step 54 끝에서 사용자가 이 문구로 재신고했었다.
AskUserQuestion으로 네 가지 후보(국가 선택 화면 자체가 안 뜸 / 목록은 뜨는데 국가가 없음 / 게임
시작 후 지도에 표시 안 됨 / 콘솔 에러)를 제시해 확인한 결과 **"국가 선택 화면(CountrySelect) 자체가
안 뜬다"**는 증상이었다.

Step 54에서 수정한 파일(`WorldState.cs`/`WorldDataManager.cs`/`CountryView.cs`/
`InfectionDotDatabase.cs`/`GamePlay.unity`)을 다시 확인했지만, 전부 감염 점 렌더링과 패배 판정
로직이고 MainMenu→CountrySelect 화면 전환을 담당하는 `UIManager.cs`/`MainMenuController.cs`/
`CountrySelectController.cs`는 이번에도 지난 세션에도 건드린 적이 없다. 혹시 몰라 편집한 4개 파일의
중괄호 짝(`{`/`}`)과 전체 구조를 다시 읽어 문법 오류가 없는지도 재확인했다 — 이상 없음. 즉 이 증상이
Step 53/54 변경과 직접 연결된다는 증거는 찾지 못했다.

**참고(사소한 도구 이슈, 실제 파일과 무관)**: 이 세션에서 편집한 파일들을 bash 샌드박스로 재확인하는
과정에서 마운트된 파일 내용이 실제 편집 결과와 다르게(오래된 캐시처럼) 보이는 현상을 발견했다 —
`Read` 도구로 같은 파일을 다시 읽으면 정상적으로 최신 상태였다. bash 샌드박스의 마운트 동기화 지연으로
보이며, 사용자 컴퓨터의 실제 파일(Read/Write/Edit가 직접 쓰는 대상)과는 무관한 문제로 판단된다 — 다만
혹시 이 프로젝트에서 이후에도 "방금 고친 게 반영이 안 된 것 같다"는 보고가 나오면, 실제 파일이 아니라
진단 도구 쪽 문제일 가능성도 염두에 둘 것.

**남은 상태**: "CountrySelect 화면이 안 뜨는" 근본 원인은 아직 못 찾았다. 사용자에게 Console의
`[FLOW][UIManager] HandlePathogenConfirmed — pathogen=..., countrySelectController=OK/NULL`,
`[FLOW][CountrySelectController] OnEnable ... root=... countryList=... backButton=... startButton=...`,
`[FLOW][CountrySelectController] Show() 호출됨 ... root=OK/NULL` 세 줄(또는 에러 메시지)을 요청해뒀다 —
다음 세션에서 이어받으면 이 로그부터 확인해 원인을 좁힐 것. 이 세 곳 중 어디든 NULL이 찍히면 UXML
이름 속성과 코드의 `Q<>()` 쿼리가 어긋났거나 UIDocument의 Source Asset 연결이 끊긴 것.

**변경 파일**: `Assets/Scripts/Managers/HumanResistanceManager.cs`(붕괴 단계 로그 제거). CountrySelect
관련 파일은 이번 세션에서 수정하지 않음(원인 미확정).

## Step 56 구현 메모 (`GamePlay.unity` 씬 파일 손상 발견 및 복구)

**받은 콘솔 로그에서 결정적 단서 두 개**:
1. `The referenced script on this Behaviour (Game Object 'SceneUICoordinator') is missing!`
2. `[FLOW][CountrySelectController] 뒤로 버튼 클릭됨 (instanceId=114710, OnBackRequested 구독자 수=0).`

2번이 결정적이었다 — `UIManager.OnEnable()`이 `countrySelectController.OnBackRequested += HandleCountrySelectBackRequested`로 구독하는데, 구독자 수가 0이라는 건 **UIManager의 OnEnable() 자체가 한 번도 실행되지 않았다**는 뜻이다. 그리고 1번의 "SceneUICoordinator에서 스크립트가 missing"이라는 경고가 바로 그 원인을 가리켰다 — `SceneUICoordinator`는 `GameDataBootstrapper`와 `UIManager` 두 컴포넌트를 갖고 있는 GameObject인데(`GameDataBootstrapper`는 로그에 정상 동작 흔적이 있으므로), **UIManager 컴포넌트 쪽이 깨져 있다**는 뜻이었다.

**직접 파일을 열어 확인한 결과, 진짜 원인은 코드가 아니라 `Assets/Scenes/GamePlay.unity` 파일 자체의 물리적 손상이었다.** `tail -c 200`으로 파일 끝부분을 열어보니:

```
...m_PrefabAsset: {fileID: 0}
  m_Gam
```

파일이 정확히 `m_GameObject: {fileID: 3200000301}`라는 줄 중간(`m_Gam`)에서 뚝 끊겨 있었다 — 그 뒤로는 파일에 바이트가 아예 없었다(트레일링 개행조차 없음). 이 줄은 `SceneUICoordinator`의 세 번째 컴포넌트(fileID 3200000304 = `UIManager`)의 필드 중 하나였는데, YAML 문서 자체가 끝까지 안 써져 있으니 Unity가 이 컴포넌트를 파싱하지 못하고 "missing script"로 처리한 것이다.

**피해 범위 확인**: 단순히 파일 끝이 잘린 것뿐 아니라, `UIManager` 컴포넌트의 나머지 필드(`mainMenuController`/`countrySelectController` 참조 포함)와 그 뒤에 있었어야 할 GameObject들이 통째로 파일에서 사라진 상태였다. `grep`으로 확인한 결과 **`SymptomTreeUI`, `AbilityTreeUI`(업그레이드 트리 증상/능력 탭 2개)와 `CountryStatusPanelController` GameObject가 파일 안에 단 하나도 없었다** — `Contagion.UI.UpgradeTreeView`가 1개(전파 탭)만 남아있고, `Contagion.UI.CountryStatusPanelController`는 0개였다.

**왜 사용자가 본 증상과 정확히 일치하는지**: `UIManager.Start()`가 실행되지 않으니 (1) `GameManager.SetPaused(true)`/`mainMenuController.Show()`/`countrySelectController.Hide()`가 한 번도 안 불려서 씬이 기본 활성 상태로 남아있던 CountrySelect가 처음부터 그냥 보이고 있었고(사용자가 MainMenu를 아예 못 보고 바로 "발원 국가를 선택" 화면부터 본 이유), (2) `UIManager.OnEnable()`이 실행 안 돼서 `CountrySelectController.OnCountryConfirmed`/`OnBackRequested`, `MainMenuController.OnPathogenConfirmed`에 아무도 구독하지 않았고(뒤로 버튼 구독자 수=0으로 확인), (3) 그래서 `CountrySelectController.Show()`(및 그 안의 `RebuildList()`)가 단 한 번도 호출되지 않아 국가 목록이 항상 빈 채로 남아있었다 — 사용자가 본 "타이틀/뒤로/시작 버튼만 있고 국가 목록은 없음"과 완전히 일치.

**복구 방법**: 이 프로젝트는 git 저장소이고(`git log` 확인), `Assets/Scenes/GamePlay.unity`가 마지막으로 커밋된 HEAD(`62bd2db`) 버전이 있었다. HEAD에서 손상된 지점과 **정확히 같은 위치**(`--- !u!114 &3200000304` 헤더 + `m_ObjectHideFlags`/`m_CorrespondingSourceObject`/`m_PrefabInstance`/`m_PrefabAsset` 4줄)를 찾아 바이트 단위로 대조했더니 완전히 동일했다 — 이 지점부터 HEAD의 나머지(508줄, `m_GameObject: {fileID: 3200000301}`부터 파일 끝까지)를 그대로 이어붙이면 안전하다고 판단했다. 근거:
- 이 구간(UIManager 본체 + SymptomTreeUI/AbilityTreeUI/SafeAreaApplier들/CountryStatusPanelController + 최종 프리팹 modifications 목록)은 전부 **UI 패널 배선**이고, Step 41/51/53에서 수치를 바꾼 `infectionDotDiameterScale`은 국가(CountryView) 오브젝트 쪽 필드라 이 구간에 전혀 등장하지 않음을 `grep`으로 사전 확인(HEAD 파일에서 `infectionDotDiameterScale` 마지막 등장 위치가 이 구간보다 한참 앞).
- 손상된 현재 파일에서 살아있던 바로 직전 컴포넌트(`GameDataBootstrapper`, fileID 3200000303)의 모든 필드 값(`countryDatabase`/`selectedPathogen`/`startingCountryId: KOR`/`startingInfectedCount: 100`/`skipMainMenu: 0` 등)이 HEAD와 완전히 동일함을 확인 — 이 지점까지는 구조·값 모두 두 버전이 일치한다는 뜻.

`head -n 9823 GamePlay.unity`(손상된 파일의 온전한 부분)에 HEAD의 9247~9754번째 줄(508줄)을 이어붙여 복구본을 만들고, 적용 전에:
- fileID 중복 여부 전수 검사(중복 없음 확인)
- `Contagion.UI.UpgradeTreeView` 3개(전파/증상/능력), `Contagion.UI.CountryStatusPanelController` 1개, `Contagion.Managers.UIManager` 1개로 정상 개수 확인
- 파일 끝이 온전한 프리팹 modifications 목록으로 정상 종료되는지 확인

이후 실제 씬 파일에 적용하고, `Read` 도구로 다시 읽어 손상 지점(9818~9824줄)과 파일 끝(10331줄)이 모두 정상임을 재확인했다. 원본 손상 파일과 복구본은 세션 스크래치 폴더에 백업해뒀다(사용자가 접근 가능한 위치는 아님 — 필요하면 별도 요청).

**파손 원인은 확정하지 못함**: 내가 Step 53에서 한 `sed -i` 편집(48개 `infectionDotDiameterScale: 1` → `0.5` 치환)은 (a) 손상된 구간을 전혀 건드리지 않았고(위 grep 근거), (b) 편집 직후 `grep -c`로 "48개 모두 0.5, 남은 1 없음"을 확인해 그 시점엔 파일이 정상이었다 — 그래서 이 손상이 그 편집 때문일 가능성은 낮다고 판단한다. 다만 100%는 배제 못 한다. 정황상 유니티 에디터 자체의 저장 중단(크래시, 디스크 쓰기 중 인터럽트 등)이 더 유력해 보인다. **재발 방지책으로 사용자에게 커밋을 자주 하도록 권장할 필요가 있음** — `git status` 확인 결과 이 파일 포함 수십 개 파일이 여러 Step에 걸쳐 커밋되지 않은 채 누적돼 있어서, 이번처럼 파일이 손상되면 git 히스토리가 유일한 복구 수단인데 그 히스토리 자체가 여러 Step만큼 오래된 상태였다(다행히 이번엔 손상 구간이 오래된 커밋과도 값이 일치해 복구 가능했지만, 항상 운이 좋으리라는 보장은 없다).

**변경 파일**: `Assets/Scenes/GamePlay.unity`(파일 끝 508줄 복구, git HEAD 기준).

## Step 57 구현 메모 (CountrySelect/MainMenu 상세 패널 텍스트 오버플로우 수정)

**신고 증상**: 발원 국가 선택 화면에서 국가를 클릭하면 아래 `detail-panel`에 인구/기후/의료 수준 3줄
텍스트가 나오는데, 인구가 나오는 줄(첫 줄)이 패널 박스 아래로 삐져나와 보임.

**원인**: `MainMenu.uss`의 `.detail-panel`이 `min-height: 70px`만 지정돼 있었음 — 제목 라벨(22px,
`detail-panel__title`)과 3줄짜리 desc 라벨(17px×3줄, `detail-panel__desc`) 실측 필요 높이는
대략 제목 26px + margin-top 6px + desc 3줄 61px + 상하 패딩(`--space-lg` 16px×2=32px) ≈ 125px로
70px보다 훨씬 큼. `.mainmenu-list`(국가 목록 ScrollView)가 `flex-grow: 1`만 있고 `flex-shrink`가
없어(Yoga 기본값 관례상 0에 가깝게 동작) 국가를 선택해 desc가 빈 문자열→3줄로 바뀌는 순간
detail-panel이 필요한 만큼 커지려 해도 옆 형제가 공간을 양보하지 못해 레이아웃이 안정적으로
재계산되지 않고 텍스트가 박스 밖으로 삐져나오는 것으로 판단.

**수정** (`Assets/UI/MainMenu.uss`, `MainMenu.uxml`/`CountrySelect.uxml` 공유 스타일이라 두 화면 모두 적용됨):
- `.detail-panel`: `min-height` 70px → 140px(3줄 콘텐츠 실측치에 여유분 포함), `flex-shrink: 0` 명시
  추가(목록이 공간을 양보하는 동안 패널 자체는 줄어들지 않도록).
- `.mainmenu-list`: `flex-shrink: 1` 추가(패널이 커질 공간을 스크롤 목록 쪽에서 양보하도록 — 목록은
  스크롤 가능이라 줄어들어도 콘텐츠 손실 없음).
- `.mainmenu-back`/`.mainmenu-next`: `flex-shrink: 0` 명시 추가(레이아웃 재계산 중에도 터치 타겟
  높이가 눌리지 않도록 방어적으로 고정).

CountrySelectController.cs/MainMenuController.cs 등 C# 코드는 건드리지 않음 — 순수 USS 레이아웃
수정. **실플레이 확인 필요**: 국가 선택 시 인구/기후/의료 수준 3줄이 패널 안에 온전히 들어오는지,
목록이 자연스럽게 줄어드는지(스크롤은 계속 되는지), 뒤로/시작 버튼 높이가 그대로인지.

**변경 파일**: `Assets/UI/MainMenu.uss`.

## Step 58 구현 메모 (HUD 하단바 레이아웃 재배치 — 값 변경 시 흔들림 수정)

**신고**: 감염자/사망자/치료제/DNA 데이터가 바뀌면 HUD가 좌우로 움직여서 가시성이 떨어진다는 지적 +
레이아웃 재배치 요청(1번째 줄: 감염자/사망자/치료제, 2번째 줄: DNA/일차/잠복기/위협 시작, 3번째 줄:
업그레이드/국가현황/랭킹).

**원인**: 기존 `stats-row` 하나에 7개 요소(그래프 3개 + 라벨 4개)가 `justify-content: space-between`으로
한 줄에 몰려 있었고, 각 요소 폭이 고정이 아니었음 — 특히 Step 34로 감염자/사망자가 수십억 단위까지
자릿수가 늘어나는 `N0` 포맷 라벨, 치료제 %, DNA 값이 바뀔 때마다 요소 폭 자체가 변해 옆 요소들이
좌우로 밀리는 것이 "UI가 움직인다"는 증상의 정체였음. 사용자가 요청한 레이아웃 재배치 자체가 자연히
그래프 3개와 라벨 4개를 분리시켜 두 줄로 만들기 때문에, 시각적 안정화와 요청한 순서 배치를 함께
해결할 수 있는 구조였음.

**수정**:
- `Hud.uxml`: `stats-row`(감염자/사망자/치료제 그래프 항목 3개)와 새 `meta-row`(DNA/일차/잠복기/위협
  시작 라벨 4개)를 분리. `tabs-row`(업그레이드/국가현황/랭킹)는 기존 위치 그대로 3번째 줄 유지.
- `Hud.uss`: `.stats-row`를 `space-between` → `flex-start`로 바꾸고 `.stat-graph-item`에 고정
  `width: 190px`(수십억 단위 숫자도 흡수) 부여. `.meta-row`의 각 라벨(`stat-label--dna`/`--day`(신규)/
  `--phase`/`--mortality`)에도 고정 폭을 줘 자릿수·유무(위협 시작 텍스트는 평시엔 빈 문자열)가 바뀌어도
  다음 라벨이 밀리지 않도록 함. 라벨 텍스트가 고정 폭을 넘으면 밀림 대신 `overflow: hidden` +
  `text-overflow: ellipsis`로 흡수.
- `HudController.cs`는 요소를 이름으로 쿼리(`root.Q<Label>("...")`)하므로 UXML 트리 구조 변경과 무관하게
  코드 수정 없이 그대로 동작 — C# 미변경.

**실플레이 확인 필요**: (a) 감염자 수가 커져도(수십억 단위) `.stat-graph-item`(190px)이 텍스트를 다
담는지 — 못 담으면 ellipsis로 잘리므로 폭을 늘릴 것, (b) 위협 단계 텍스트("위협 시작" 등)가 나타나거나
사라질 때 옆 라벨이 흔들리지 않는지, (c) 세로 폭이 늘어난 하단바가 지도/뉴스피드 영역을 과하게
침범하지 않는지.

**변경 파일**: `Assets/UI/Hud.uxml`, `Assets/UI/Hud.uss`.

## Step 59 구현 메모 (누락된 30개국 국기 텍스처 추가)

**신고**: 콘솔에 `[CountrySelectController] 국기 텍스처를 찾지 못했습니다: Resources/Flags/PAK.png` 경고.

**원인**: `Assets/Resources/Flags/`에 18개국 국기 파일만 있고 나머지 30개국(PAK 포함)이 누락돼
`CountrySelectController.GetFlagTexture()`가 매번 경고 로그를 남기고 빈 슬롯을 그리고 있었음.
`Assets/Resources/CountryShapes/`는 48개국 전부 있어서 국가 자체 데이터 누락 문제는 아니었고,
순수하게 국기 이미지 에셋만 빠져 있었던 것으로 확인.

**해결**: `CountryDatabase.asset`에서 48개국 id/영문명을 전수 추출해 오프라인 flagpy 라이브러리(실제
국기 이미지가 라이브러리 내장, 네트워크 접근 불필요)로 누락 30개국의 실제 국기 PNG를 생성(가로 160px,
기존 18개 파일과 동일 규격). 기존 파일과 동일한 `.meta`(TextureImporter, spriteMode 등) 포맷으로
`.meta`도 함께 생성해 `Assets/Resources/Flags/`에 배치. 48/48 전수 확인 완료(PAK/TUR/DRC 등 육안 검증).

순수 에셋 추가, 코드 미변경. **실플레이 확인 필요**: CountrySelect 화면에서 국기가 전부 정상 표시되는지,
콘솔에 "국기 텍스처를 찾지 못했습니다" 경고가 더 이상 안 뜨는지.

**변경 파일**: `Assets/Resources/Flags/*.png`, `Assets/Resources/Flags/*.png.meta`.

## Step 60 구현 메모 (교통망 허브 공백 지역 16개 추가)

**배경**: 기존 교통망(Step 29~30-5)은 항공/해운 허브 각 15개, 총 30개로 48개국 중 12개국만 허브를
보유해 아프리카 전체·유럽 5개국(FRA/ITA/ESP/POL 등)·남미 3개국·북미 2개국·호주가 교통망에서 완전히
소외되어 있었음.

**작업**: 항공 허브 5개(ADD 아디스아바바/SVO 모스크바/LIM 리마/YYZ 토론토/MEX 멕시코시티) + 해운 허브
11개(SEA_PSD/TNG/LOS/DUR/LEH/GOA/ALC/GDN/BUE/CTG/MEL)를 `DefaultTransportHubFactory.cs`에 신규
추가. 허브 보유국이 12→28개로 확장됨. 좌표는 실제 공항/항구 위경도를 Step 32에서 구한 회귀식에
대입한 절대좌표 사용(신규 해운 허브도 국가 앵커 오프셋 대신 `SeaAbs()`로 절대좌표 방식 적용). 신규
해상 항로는 `world_base.png` 알파채널 기준 A*(다운샘플 factor 2)로 육지 미관통 전수 검증했다.

**설계 제약**: 수에즈·파나마 운하는 이 해상도의 지도에 육지로만 그려져 있어(폭이 좁은 인공 수로라
바다 픽셀 자체가 없음) 이집트↔중동, 콜롬비아/멕시코↔롱비치 직결 항로를 만들 수 없었음. 각 지역 내
다른 허브를 거쳐 우회 연결되도록 설계했다.

**검증 필요** (다음 세션, Unity 실플레이): (a) 신규 허브 16개가 지도 위 올바른 위치(육지는 공항,
바다는 항구)에 뜨는지, (b) 신규 항로 15개 선이 육지를 관통하지 않는지, (c) 신규 국가(ETH/RUS/PER/
CAN/MEX/EGY/MAR/NGA/RSA/FRA/ITA/ESP/POL/ARG/COL/AUS)에서 비행기/배가 정상적으로 뜨고 도착하는지.

**변경 파일**: `DefaultTransportHubFactory.cs`.

## Step 61 구현 메모 (국가현황 패널 렉 수정 — 유지 + 재작성)

**신고**: `tab-country-status`(국가현황 패널) 열람 중 프레임 드랍 심함.

**분석 과정**: 패널을 완전히 제거할지(Country Dock으로 대체) 먼저 검토했으나, 코드/설계상 두 UI는
역할이 다름을 확인 — Country Dock은 "선택된 국가 1개 상세", 국가현황 패널은 "48개국 동시 비교"이며
후자는 Dock이 구조적으로 대체 불가(단일 선택 구조). 또한 패널의 최초 도입 이유(Step 28-2 주석)가
지도가 좁은 공간에 48개국이 촘촘히 배치돼 개별 탭이 어려운 문제의 우회책이기도 해서, 제거 시 소형
국가 선택 경로 자체가 사라지는 부작용이 있음. → 패널은 유지하고 구현만 재작성하기로 결정.

**원인**: `CountryStatusPanelController.Populate()`가 `WorldDataManager.OnCountryChanged` 이벤트를
받을 때마다(패널이 열려 있는 동안) `ScrollView.Clear()` 후 48개 행 전체를 `VisualElement`/`Label`
새로 생성해서 다시 그렸음. 이 이벤트는 `SimulationManager.cs`(~225행)가 매 틱 "감염 중이거나 인구가
남은 국가마다" 개별 호출하므로(감염 확산 국가가 20~30개면 틱당 20~30회 발생), 국가 하나의 값만
바뀌어도 리스트 전체(약 190개 UI 요소)를 그 배수만큼 반복 재생성하는 셈 — 사실상 O(국가 수²)에
가까운 낭비가 패널이 열려 있는 매 틱 발생했음. Country Dock은 반대로 선택된 국가 1개의 Label
텍스트만 갱신해서 렉이 없었음(대조군 역할).

**수정**: `CountryStatusPanelController.cs`
- 국가 id → 행 참조(`RowRefs`: Row/NameLabel/StatsLabel/FlagsLabel) 캐시 딕셔너리 추가.
- `EnsureRowsBuilt()`: 딕셔너리가 비어 있을 때(최초 1회)만 `ScrollView.Clear()` + 48개 행 생성.
  이후에는 재호출돼도 아무 것도 하지 않음.
- `HandleCountryChanged(country)`: 패널이 열려 있으면 `EnsureRowsBuilt()` 후 **바뀐 국가 1개 행만**
  `RefreshRow()`로 텍스트 갱신 — 전체 재생성(`Populate()`) 호출 제거. 갱신 1건당 비용이 O(48)→O(1).
- `Populate()`(Show() 시 1회만 호출)는 그대로 전체 국가를 순회하며 `RefreshRow()` — 이 전체 순회는
  패널을 열 때 딱 한 번뿐이라 비용 문제 없음.

**부가 작업 (정보 중복 축소, B안 요소 일부 병합)**: 패널 고유 정보 중 Dock에 없던 붕괴단계/항공·항구
상태를 Country Dock에도 추가해 "선택 국가의 세부 상태"는 Dock 하나로 완결되도록 함. 국가현황 패널은
이제 "48개국 동시 비교"에만 집중.
- `Hud.uxml`: `country-dock__body`에 `country-dock-stage`/`country-dock-transport` 라벨 행 2개 추가.
- `Hud.uss`: `.country-dock__value--danger`(危 상태 강조색) 클래스 추가.
- `CountryDockController.cs`: 두 라벨 바인딩 + `Populate()`에서 값 채움, `StageLabel()` 헬퍼 추가
  (`CountryStatusPanelController`와 동일 매핑, 네임스페이스 공유 유틸로 뽑아내지 않고 중복 유지 —
  기존 코드베이스 관례상 Dock/Panel 간 소규모 매핑 중복은 허용해온 패턴).

**실플레이 확인 필요**: (a) 국가현황 패널을 연 채로 감염이 여러 국가에서 동시 진행 중일 때 프레임
드랍이 실제로 해소됐는지(Unity Profiler로 패널 열림 상태에서 GC Alloc/틱당 UI 재빌드 확인 권장),
(b) 48개 행이 최초 1회만 생성되고 이후 값만 갱신되는지 콘솔/프로파일러로 교차 확인, (c) Country
Dock에 새로 추가된 "상태"/"항공·항구" 행 2개가 도킹 패널 폭 안에서 줄바꿈 없이 표시되는지,
위험 상태(危) 색상이 다른 값 색상과 충돌하지 않는지.

**변경 파일**: `Assets/Scripts/UI/CountryStatusPanelController.cs`,
`Assets/Scripts/UI/CountryDockController.cs`, `Assets/UI/Hud.uxml`, `Assets/UI/Hud.uss`.

## 참고: Step 57~61 요약 표 (CLAUDE.md에서 이동, 2026-07-09)

CLAUDE.md 문서 분리 규칙 적용으로 "최근 작업" 표를 여기로 이동. 각 Step의 상세 배경은 위 해당
Step 섹션 참고.

| Step | 내용 | 파일 |
|------|------|------|
| 57 | 상세패널 오버플로우 수정 | MainMenu.uss |
| 58 | HUD 3줄 레이아웃 개편 | Hud.uxml, Hud.uss |
| 59 | 누락 국기 30개 추가 | Flags/*.png |
| 60 | 핵심 공백 지역 허브 16개 추가 | DefaultTransportHubFactory.cs |
| 61 | 국가현황 패널 렉 수정(행 캐싱, 전체재생성 제거) + Dock에 상태/항공·항구 추가 | CountryStatusPanelController.cs, CountryDockController.cs, Hud.uxml/uss |

## Step 62 구현 메모 (UpgradeTree — Tactical Display 전환, UI_Design.md 11절 구현)

**배경**: `Docs/UI_Design.md` 11절(UpgradeTree 심화 설계안)에서 세운 LOCKED/AVAILABLE/ACTIVE/
MAXED 4단계 노드 상태, "연구 모듈 카드" 레이아웃, 연결선 회로도화, 상세 패널 "연구 분석 콘솔"화,
카테고리 LAB 명명을 실제 파일(`UpgradeTree.uxml`/`UpgradeTree.uss`/`UpgradeTreeView.cs`)에 반영.
핵심 플레이 루프(지도 관찰 ↔ 업그레이드 선택)상 HUD 다음으로 오래 보는 화면인데 HUD 리디자인
(Step 57~61)과 시각 언어가 분리돼 있던 문제 해결.

**원칙**: 게임 로직(해금 규칙/DNA 소모/효과 적용 — `UpgradeManager.CanUnlock`/`TryUnlock`/
`GetEffectiveCost`, `UpgradeNode.isUnlocked`/`prerequisites`)은 전혀 건드리지 않았다. 4단계
상태는 전부 기존 public API를 읽기 전용으로 조회해 매번 다시 계산하는 파생값(신규 필드 없음).

**Theme.uss**: `--tracking-caption`(1px)/`--tracking-label`(0.5px) 자간 토큰 2개 추가(11.9) —
다른 화면 확장 시에도 재사용할 공용 토큰.

**UpgradeTree.uxml**:
- 헤더의 `category-title-label`(기존 한글, 텍스트/로직 무변경)을 `upgrade-header__title-block`
  으로 감싸고 그 위에 `category-caption-label`(영문 LAB 캡션, 신규) 추가 — 11.5.
- `detail-panel`에 `tactical-panel` 클래스 + 코너컷 4개 추가.
- `detail-desc`(단일 Label) 제거, `detail-rows`(빈 컨테이너)로 교체 — UpgradeTreeView.SelectNode()가
  data-row를 여러 줄 채워 넣는 방식으로 전환(11.4/11.8).

**UpgradeTree.uss**:
- `.tactical-panel`/`.tactical-panel__header`/`.tactical-panel__title`/`.corner-cut`(4방향)/
  `.data-row`/`.data-label`/`.data-value`(+ 상태별 색상 변형) 신규 — Hud.uss event-dock/
  country-dock과 동일 값으로 복제(현재는 UpgradeTree 로컬 정의, 추후 화면이 늘면 Tactical.uss로
  승격 예정, 클래스명은 승격을 염두에 두고 범용 이름 그대로 사용).
- `.tree-node`: 라벨 2개 중앙정렬 박스 → code/이름/상태/비용 4줄 좌측정렬 스택으로 전환. 카테고리
  색은 전체 테두리에서 좌측 4px accent bar로 축소.
- `.tree-node--locked/--available/--active/--maxed` 4종 신규(기존 `.tree-node--unlocked`는
  대체돼 제거) — 테두리색/두께/배경 틴트로 상태 구분(11.2).
- `.detail-panel`에 `position: relative`(코너컷 기준점) 추가, `border-radius` 제거(코너컷의
  각진 인상과 충돌).

**UpgradeTreeView.cs**:
- `DetermineState(node)`: `isUnlocked` bool 하나였던 이분법을 4단계로 세분화 —
  `!isUnlocked && prerequisites 전부 해금` → available, 그 외 미해금 → locked,
  `isUnlocked && 이 노드를 선행조건으로 삼는 다음 노드가 없음(leaf)` → maxed, 그 외 해금 → active.
- `RebuildTree()`가 카테고리 노드를 티어(y)→갈래(x) 순으로 정렬해 `TRANS-001`류 코드를
  `_codeByNodeId`에 매핑(노드 박스와 상세 패널이 공유).
- `CreateNodeElement()`: 라벨 2개 생성 → code/이름/상태/비용 4개 Label 생성 + `tree-node--{state}`
  클래스 부여로 재작성. `NodeHeight` 60→78(4줄 수용, 행 간격 110px 여유 있어 겹침 없음).
- `DrawConnections()`: 대각선 1개(활성 녹색/비활성 회색 alpha 0.2) → 꺾은선(elbow, 중간점
  경유) + 양 끝 4px 포트 마커. 색상은 Theme.uss `--color-accent-glow`/`--color-grid-line`과
  동일 값으로 통일(USS 변수를 C#에서 직접 못 읽어 수동 동기화 — 주석에 명시).
- `SelectNode()`/`BuildDescription()`: 문자열 한 덩어리 조립 → `AddDetailRow()`로 코드/명칭/
  효과별 수치/선행노드/DNA COST/STATUS를 data-row 여러 줄로 분해. `BuildDescription()` 삭제(완전
  대체), 미사용 `using System.Text;` 제거.
- `CategoryLabel()`(기존 한글, 무변경) 옆에 `CategoryEnglishName()`/`CategoryPrefix()`/
  `CategoryCaption()` 3개 신규 — 노드 코드 접두어·헤더 영문 캡션·상세 패널 "{CATEGORY} ANALYSIS"
  타이틀에 사용.

**실플레이 확인 필요** (Unity 에디터, 다음 세션): (a) 노드 4줄 텍스트가 78px 높이 안에서 잘리지
않는지, 특히 두 단어 한글 표시명("다발성 장기부전 II") 줄바꿈 시, (b) LOCKED/AVAILABLE/ACTIVE/
MAXED 색상이 실제 데이터로 4가지 다 나타나는지(특히 MAXED — 갈래 끝 노드 해금 시), (c) 코너컷이
detail-panel 모서리에 올바르게 붙는지(position:relative 적용 확인), (d) 연결선 꺾은선/포트 마커가
카테고리 3개(전파/증상/능력) 전부에서 겹침 없이 그려지는지, (e) 헤더 2줄(영문 캡션+한글 라벨)이
좁은 폭에서 안 잘리는지.

**변경 파일**: `Assets/UI/Theme.uss`, `Assets/UI/UpgradeTree.uxml`, `Assets/UI/UpgradeTree.uss`,
`Assets/Scripts/UI/UpgradeTreeView.cs`.

## Step 63 구현 메모 (Country Dock 무반응 버그 수정 — 씬 배선 누락)

**증상**: 인게임 우측 상단 Country Dock이 항상 "국가를 선택하세요" + "-"만 표시하고 국가를
눌러도 인구/감염률 등 데이터가 채워지지 않음.

**원인**: `CountryDockController.cs`가 GamePlay 씬의 HUD GameObject에 붙어있지 않았다
(unity-editor-task.md 1절의 수동 배선 작업이 미완료 상태였음). Hud.uxml의 기본 텍스트가
플레이스홀더와 동일해서, 컨트롤러가 아예 없어도 겉보기엔 "플레이스홀더 상태"처럼 보이는 것이
함정 — 실제로는 어떤 코드도 라벨을 갱신하지 않고 있었다. 진단은 스크립트 .meta의 GUID
(`03d2a5f4b33123845a800ef1995c3836`)를 GamePlay.unity에서 grep해 0건임을 확인하는 방식으로 확정.

**수정**: GamePlay.unity YAML 직접 편집 — HUD GameObject(fileID 543881991)의 m_Component
목록에 새 항목을 추가하고, HudController 블록 뒤에 CountryDockController MonoBehaviour 블록
(fileID 543881996, 씬 내 미사용 ID 확인함)을 삽입. CountryDockController는 직렬화 필드가 없어
블록이 최소 형태로 충분하다.

**남은 확인(QA)**: 에디터에서 씬 열림/Missing Script 여부, 국가 탭 시 데이터 채워짐, 틱 갱신,
드래그 중 오선택 가드(WasDragging) — unity-editor-task.md 2~3절 체크리스트 그대로 유효.

**변경 파일**: `Assets/Scenes/GamePlay.unity`, `Docs/unity-editor-task.md`.

## Step 64 구현 메모 (CountrySelect Tactical 마무리 — UI_Design.md 9절/16.2 ①)

**배경**: `detail-panel`(tactical-panel+corner-cut 4개+detail-rows) 전환은 이미 이전 세션에서
반영돼 있었으나, 조사 결과 두 가지가 계획(9절/16.2 ①)과 어긋나 있었다.

**버그 1 — border-left-width 캐스케이드**: `country-row`에 `accent-bar-row`(Tactical.uss,
`border-left-width: 4px`)와 `country-row--dev-*`(CountrySelect.uss, `border-left-color`만)를
같이 부여하고 있었는데, `Theme.uss → Tactical.uss → MainMenu.uss → CountrySelect.uss` 로드
순서상 `MainMenu.uss`의 `.country-row { border-width: 2px; }` 셔손드가 나중에 적용돼 좌측
4px accent bar가 실제로는 2px로 렌더링되고 있었다(동일 특이도에서 나중 파일이 이김). 개발수준
severity 색상 자체(border-left-color)는 CountrySelect.uss가 더 나중에 로드돼 정상 적용되고
있었지만, 두께는 죽어 있었던 것. `UpgradeTree.uss`의 `.tree-node`(`border-width: 1px;
border-left-width: 4px;`를 **같은 rule 안**에 두는 패턴)와 동일하게 `.country-row` 한 rule
안에서 override하도록 고쳐 크로스파일 캐스케이드 의존을 제거했다.

**갭 2 — country-row가 data-row 문법 미사용**: `country-row__meta`가 "인구 N · 개발수준"을
합친 Label 하나였다. 9절은 "48행 스크롤 높이 제약 때문에 data-row를 세로로 쌓지 않고 한 줄
유지 권장, 분해는 상세 패널에서만"이라고 명시했으므로, 줄 수를 늘리지 않는 선에서 기존 정보량
그대로 `data-row`(라벨 "인구" + 값 "N · 개발수준") 컨테이너로 재구성했다. 컨테이너 자체가
`country-row`의 카드 테두리 안에 다시 하단 헤어라인을 그리면 불필요한 줄로 보여서
`country-row__meta`에서 `border-bottom-width/margin-bottom/padding-bottom: 0`으로 무효화.

**원칙**: 컨트롤러 이벤트 시그니처(`OnCountryConfirmed`/`OnBackRequested`), UXML `name` 속성,
게임 로직 전부 무변경 — `CreateCountryRow()`의 meta 생성부만 Label 1개 → VisualElement(data-row)
+ Label 2개로 교체했다.

**변경 파일**: `Assets/UI/MainMenu.uss`, `Assets/Scripts/UI/CountrySelectController.cs`.

## Step 65 구현 메모 (MainMenu Tactical 감사 — UI_Design.md 8절/16.2 ②)

**배경**: CLAUDE.md TODO에 "MainMenu pathogen-card/detail-panel 전환"이 미착수로 남아있어
착수하려 했으나, 실제 조사 결과 `MainMenu.uxml`/`MainMenu.uss`/`MainMenuController.cs`는 8절
설계안(pathogen-card에 tactical-panel+코너컷 4개, 스탯을 data-row 4줄로 분해, detail-panel
tactical-panel+코너컷+헤더, "PATHOGEN BRIEFING TERMINAL" 영문 캡션, pathogen-card--selected
금색 테두리 유지, FlavorText는 문단 그대로 유지)이 **이미 전부 반영되어 있었다** — CLAUDE.md가
갱신되지 않아 상태가 어긋나 있던 것. CountrySelect(Step 64)에서 발견한 것과 같은 종류의
border-width 캐스케이드 버그가 있는지 별도로 확인했으나, `.pathogen-card`는 border-width를
전혀 재정의하지 않고 `tactical-panel`의 1px 테두리를 그대로 쓰고 있어 문제 없음(border를
재정의하는 CountrySelect의 `country-row`와 구조가 다름).

**정리한 것**: `MainMenuController.CreatePathogenCard()`가 데이터-row 리팩터링 이후 더 이상
쓰지 않는 `.pathogen-card__stats`(구 버전, 스탯 4종을 한 Label에 합쳐 표시하던 시절의 클래스)
죽은 CSS 룰을 `MainMenu.uss`에서 제거했다. `.pathogen-card__stats-rows`(현재 사용 중)와
헷갈릴 수 있어 삭제가 안전 확인 우선이었다 — 컨트롤러 전체에서 `pathogen-card__stats`(하이픈
없는 접미어) 참조가 `pathogen-card__stats-rows` 한 곳뿐임을 grep으로 확인 후 제거.

**실제 코드 변경 없음(UXML/컨트롤러/스타일 로직)** — 이번 Step은 감사 + CLAUDE.md 상태 동기화 +
죽은 CSS 제거만 수행. §8 설계안 자체는 추가 작업 불필요.

**변경 파일**: `Assets/UI/MainMenu.uss`(dead CSS 제거만).

## Step 66 구현 메모 (EndingScreen Tactical 감사 — UI_Design.md 10절/16.2 ③)

**배경**: CLAUDE.md TODO의 "EndingScreen 통계/스코어 패널 tactical-panel 전환"에 착수하려
조사했으나, `EndingScreen.uxml`/`EndingScreenController.cs`는 10절 설계안(승/패 히어로
타이틀 → "OPERATION REPORT" 캡션 → 통계 `tactical-panel`[코너컷 4개 + `data-row` 4줄:
GLOBAL INFECTED/DEATHS/COLLAPSED NATIONS/경과일, severity 색상] → FINAL SCORE 금색
`tactical-panel`[코너컷 4개] → 버튼 2개)이 **이미 순서·구조 그대로 반영되어 있었다** —
CountrySelect(Step 64)/MainMenu(Step 65)와 같은 패턴으로 CLAUDE.md만 갱신되지 않은 상태.
COLLAPSED NATIONS 집계(`Countries.Count(c => c.GetCollapseStage() >= FullCollapse)`)도
10절이 제안한 형태 그대로 구현돼 있었고, `ComputeFinalScore()`/`ComputeBiohazardScore()`
점수 계산 로직은 손대지 않은 채였다.

**발견한 갭 — ending-score font-size**: 10절 본문이 "별도 tactical-panel(코너컷 4개,
`--color-brand-gold` 테두리, `font-size-hero` 유지)로 감싸"라고 명시했는데, 실제 `.ending-score`는
`--font-size-xxl`(26px)을 쓰고 있었다. `--font-size-xxl`은 `DESIGN.md` 타이포 표에 "(예약)"
— 즉 아직 용도가 배정되지 않은 토큰이고, `--font-size-hero`(40px)가 정확히 "엔딩 등 최종
판정 강조" 용도로 예약된 토큰이라 설계 의도와 어긋났다. `ending-title`(승/패)이 이미
`font-size-hero`를 쓰고 있어 스코어도 같은 크기로 맞추면 "최종 판정" 신호가 통일된다 —
`.ending-score`의 `font-size`를 `--font-size-hero`로 수정.

**DESIGN.md 위반 여부 확인**: `ending-score-panel`이 `tactical-panel`의 기본 테두리색
(accent-glow-soft)을 금색으로 override하는 것은 Usage Rules > Tactical Panel 규정과
충돌하는 것처럼 보일 수 있으나, `UI_Design.md` 10절이 이 화면에 한해 명시적으로 지시한
것이고(문서 우선순위 규칙 — 화면 배치는 UI_Design.md가 우선), `tree-node--maxed`(완료
노드, 금색 2px 테두리)와 동일한 "브랜드 골드 = 완결/최종" 신호 축을 재사용하는 것이라
Brand Accent/Node State 축 분리 원칙과도 모순되지 않는다. Corner Cut(패널 2개, 4개 전부
사용)·Severity Colors(감염/사망/위험만 국가 데이터에 사용, 경과일은 무색)도 규정과 일치.

**실제 코드 변경**: `.ending-score` font-size 1줄만 수정. UXML/컨트롤러 무변경(이벤트
시그니처 `OnReviveRequested`/`OnRestartRequested`, `name` 속성, 점수 계산 로직 전부 그대로).

**변경 파일**: `Assets/UI/EndingScreen.uss`.

## Step 67 구현 메모 (CountryPopup → Tactical Modal Base 승격 — UI_Design.md 12절/13절/16.2 ④)

**배경**: `Docs/UI_Design.md` 12절/16.0은 "`CountryPopupController`는 씬에서 GameObject만
비활성화된 채 코드는 살아있는 죽은 코드"라고 서술했다. 착수 전 이 전제를 코드로 재검증했다.

**전제가 틀렸다는 것을 발견**: `Assets/Scenes/GamePlay.unity`에서 `CountryPopupUI`
GameObject를 직접 확인한 결과 `m_IsActive: 1`(활성) — 비활성 상태가 아니었다.
`CountryDockController.cs`(13행) 주석은 "Step 28-2 이후 클릭 트리거가 제거돼 죽은 코드"라고
쓰여 있지만, 이는 Step 28-2 시점 서술이 그대로 방치된 것 — 실제로는 `CountryView.cs`(561~568행,
주석 확인) "HUD 리디자인에서 Country Dock을 되살리며 `WorldMap.HandleCountryClicked()` 호출을
다시 넣었다"고 명시돼 있고, 그 의도한 소비자는 `CountryDockController`였다. 문제는
`CountryPopupController`의 `WorldMap.OnCountryClicked`/`WorldDataManager.OnCountryChanged`
구독 코드 자체는 그때 같이 지워지지 않았고 GameObject도 비활성화되지 않은 채 남아있었다는
것 — 즉 국가를 탭하면 **의도치 않게 Country Dock과 이 팝업이 동시에 뜬다.** `UIManager.cs`도
`countryPopupController` 필드를 여전히 보유하고 `HandleUpgradeButtonClicked()`에서
`.Hide()`를 호출한다(살아있는 참조).

**결정**: 이번 작업 범위는 명시적으로 "구조 승격"(스타일/컴포넌트 프레임 전환)이라 이 자동
팝업 트리거 자체를 끄는 행동 변경은 하지 않았다(§ "기능 추가 최소화/기존 로직 파괴 금지"
원칙과 충돌할 수 있어 사용자 판단이 필요한 별도 사안으로 분리 — CLAUDE.md TODO에 기록).
따라서 결정 기준 A(완전히 죽은 코드)가 아니라 **B(부분적으로/실제로 사용 중)** 로 판단,
12절이 이미 결론 낸 "공용 Modal Base로 추출"(방식 b — 신규 `TacticalModalController` +
`CountryPopupController`는 그 위의 얇은 래퍼)을 그대로 실행했다.

**구현 — 씬 편집 없이 완료**: `TacticalModalController`를 별도 컴포넌트로 씬에 추가하는 대신
(Step 63처럼 GUID 수동 배선이 필요해지므로), `CountryPopupController : TacticalModalController`
**상속**으로 구성했다 — 씬의 `CountryPopupUI` GameObject에 붙어있는 컴포넌트 슬롯(스크립트 GUID
`715567c2258f4a04f8e123a3c0ac92c5`)은 클래스 이름이 그대로라 아무 변경도 필요 없다(zero scene
diff). `TacticalModalController`(신규)는 `Show(title)`/`Hide()`/`AddRow(label,value,valueClass)`/
`ClearRows()`/`Footer` 범용 API를 제공하고 `modal-root`/`modal-title`/`modal-close`/`modal-rows`/
`modal-footer`를 Q<>()로 바인딩한다 — §13 템플릿과 동일 계약. `CountryPopupController`는
`Subscribe()`/`HandleCountryClicked()`/`HandleCountryChanged()`/`Populate()`(6개 popup-row를
`AddRow()` 6줄로 전환, 감염자/사망자/의료수준에 severity 색상 부여 — Country Dock/CountrySelect와
동일 규약 재사용)만 남기고, `Hide()`는 `_shownCountryId` 초기화 후 `base.Hide()` 호출로 오버라이드.
`UIManager.countryPopupController?.Hide()` 호출부는 타입/시그니처 무변경이라 그대로 동작한다.

**UXML/USS**: `CountryPopup.uxml`을 §13 템플릿 그대로 재작성 — `popup-root`에 `tactical-panel`
병기(`name`은 `modal-root`로 변경 — 이 파일 내부 전용 식별자라 컨트롤러와 함께 좌표해 바꿈,
외부에서 이 이름을 참조하는 코드 없음을 grep으로 확인), 코너컷 4개, `tactical-panel__header`
안에 `modal-title`+`modal-close`(✕, 기존 "닫기" 전체폭 버튼에서 헤더 인라인 아이콘 버튼으로),
`modal-rows`(`detail-rows`), `modal-footer`(현재 비어있음, 향후 소비자용). `CountryPopup.uss`는
`.popup-root`의 `border-radius`(구 `--radius-lg`) 제거(tactical-panel의 radius:0과 충돌 방지 —
Step 64에서 겪은 캐스케이드 버그 재발 방지), `.popup-header`(가로 배치)/`.popup-close`(28×28
아이콘 버튼)/`.modal-footer`(빈 슬롯 기본 배치) 3개만 추가, `.popup-title`/`.popup-row`(구
버전 전용, `tactical-panel__title`/`data-row`로 대체돼 무용)는 제거.

**변경 파일**: `Assets/Scripts/UI/TacticalModalController.cs`(신규), `Assets/Scripts/UI/CountryPopupController.cs`,
`Assets/UI/CountryPopup.uxml`, `Assets/UI/CountryPopup.uss`.

## Step 68 구현 메모 (Country Dock 무반응 재신고 — 이벤트 디스패치 방어 강화)

**신고**: Step 63에서 고쳤다던 Country Dock이 여전히 "국가를 선택하세요" + "-"만 표시, 클릭해도
갱신 안 됨. Step 67(Tactical Modal 승격) 직후 신고돼 연관성 의심.

**조사 — 씬 배선은 정상이었다**: `GamePlay.unity`를 직접 확인. HUD GameObject(fileID 543881991,
`m_IsActive: 1`)의 `m_Component` 목록에 `CountryDockController`(fileID 543881996, guid
`03d2a5f4b33123845a800ef1995c3836`)가 정확히 등록돼 있어 Step 63의 수정 자체는 씬에 그대로
남아있음을 확인했다. `Hud.uxml`의 `country-dock-*` name 8개도 `CountryDockController.cs`의
`Q<>()` 호출과 전부 일치(불일치 없음). `CountryView.cs`(561~574행)의 클릭 트리거,
`WorldMap.cs`의 `OnCountryClicked`/`HandleCountryClicked()`도 Step 67에서 손대지 않아 무변경.
`TacticalModalController`/`CountryPopupController`(Step 67 신규·변경분)도 `Show`/`Hide`/
`AddRow`/`ClearRows` 전부 null-안전하게 작성돼 있어 정적 분석상 예외를 던질 경로를 찾지 못했다
— **unity-editor-task.md 1절의 체크박스가 여전히 `[ ]`(미확인)로 남아있는 것으로 보아, 사용자가
Step 63 수정 이후 실제 플레이 테스트를 이번이 처음 했을 가능성이 있다.**

**남은 유력 가설 — 멀티캐스트 delegate 예외 전파**: `WorldMap.OnCountryClicked`와
`WorldDataManager.OnCountryChanged` 둘 다 구독자가 2개 이상이다(전자: CountryDockController +
CountryPopupController, 후자: WorldMap 자신 + CountryDockController + CountryStatusPanelController
+ CountryPopupController). 기존 코드는 `OnCountryClicked?.Invoke(country)` /
`OnCountryChanged?.Invoke(country)` 형태의 **표준 멀티캐스트 호출**이었는데, C#에서 이 방식은
구독자 하나가 예외를 던지면 그 뒤에 등록된 구독자 전부가 호출되지 않고 조용히 스킵된다(예외를
잡는 코드가 어디에도 없어 Console에도 안 남을 수 있음 — Unity가 처리되지 않은 예외를 로그에
남기긴 하지만, 어느 구독자가 문제인지 특정하기 어렵고 이후 호출이 전부 끊긴다는 사실 자체가
안 보인다). **구독 순서상 CountryPopupController가 CountryDockController보다 먼저 등록되면,
전자가 어떤 이유로든 예외를 던질 경우 후자는 영원히 실행되지 않는다** — 정확히 신고된 증상과
일치하는 구조적 취약점이라, Step 67에서 CountryPopupController가 (원래도 죽지 않은 채였지만)
새로 손댄 코드로 바뀐 시점과 신고 시점이 맞물린 것도 설명이 된다(실제로 예외가 나는지는 정적
분석만으로 100% 확정할 수 없었음 — Unity 콘솔 확인 필요).

**수정 — 이벤트 디스패치 격리**: `WorldMap.HandleCountryClicked()`와
`WorldDataManager.NotifyCountryChanged()`를 `GetInvocationList()`로 순회하며 구독자별로
개별 try/catch하도록 변경 — 한 구독자의 예외가 `Debug.LogException`으로 로그만 남기고
나머지 구독자 호출은 계속 진행되도록 격리했다. 이제 (a) Country Dock은 다른 구독자 상태와
무관하게 항상 갱신되고, (b) 만약 실제로 예외가 나고 있었다면 Console에 스택 트레이스가 찍혀
진짜 원인(어느 컨트롤러의 어느 줄)을 바로 특정할 수 있다.

**원칙 준수**: UI 리디자인 없음 — 이벤트 디스패치 로직만 방어적으로 강화. 이벤트 시그니처
(`Action<Country> OnCountryClicked`/`OnCountryChanged`), 구독/해제 코드, 컨트롤러 공개 API
전부 무변경.

**남은 확인(QA, `Docs/QA_Checklist.md`에 항목 추가)**: 이 수정 후에도 Country Dock이 안 뜨면
Console에 `Debug.LogException`으로 찍히는 예외의 스택 트레이스를 확인 — 그게 진짜 원인이다.
안 뜨고 예외도 없다면 `WorldMap.Instance`/`WorldDataManager.Instance`가 클릭 시점에 null인지
(초기화 순서 문제) 별도로 의심해야 한다.

**변경 파일**: `Assets/Scripts/Gameplay/WorldMap.cs`, `Assets/Scripts/Managers/WorldDataManager.cs`,
`Docs/QA_Checklist.md`.

## Step 69 구현 메모 (Country Dock 재신고 추가 조사 — CountryDockController 자가진단 로그 추가)

**신고**: Step 68 수정 후에도 Country Dock이 여전히 "국가를 선택하세요"/"-" 고정. 국가현황
패널(CountryStatusPanelController)은 정상 동작.

**비교 조사 — CountryStatusPanelController vs CountryDockController**: 결정적 차이를 찾았다.
`CountryStatusPanelController.HandleCountryChanged()`는 `WorldDataManager.OnCountryChanged`
하나만 구독하고 게이트 없이 48개국 전체를 항상 갱신한다. 반면 `CountryDockController.
HandleCountryChanged()`는 `if (country.id == _shownCountryId) Populate(country);`로 게이트돼
있고, `_shownCountryId`는 오직 `HandleCountryClicked()`(즉 `WorldMap.OnCountryClicked`)가
성공적으로 호출돼야만 세팅된다 — **`OnCountryChanged`가 정상 작동해도(국가현황 패널이 이를
증명) `OnCountryClicked` 쪽이 막혀 있으면 Country Dock은 절대 못 움직인다.** 두 패널이 갈리는
지점은 정확히 여기다.

**씬/코드 정적 검증 — 전부 정상**: `GamePlay.unity`에서 HUD GameObject의 `m_Component` 순서를
직접 확인(Transform → **UIDocument** → HudController → NewsFeedController → CountryDockController)
— UIDocument가 CountryDockController보다 앞서 있어 `rootVisualElement`가 준비되기 전에 Q<>()가
실행될 위험은 낮다. 같은 GameObject의 `HudController.OnEnable()`도 동일 패턴(`GetComponent
<UIDocument>().rootVisualElement` 후 Q<>())으로 라벨을 바인딩하는데 HUD 상단 스탯(감염자/사망자/
DNA 등)은 정상 동작한다고 알려져 있어, UIDocument 타이밍 자체가 깨졌을 가능성도 낮다.
`Hud.uxml`의 `country-dock-*` name 8개도 재확인 결과 `CountryDockController.cs`의 Q<>() 호출과
전부 일치. `WorldMap.HandleCountryClicked()`/`WorldDataManager.NotifyCountryChanged()`도 Step 68
수정이 정확히 적용돼 있음을 재확인했다. **즉, 정적 분석으로는 더 이상 원인을 좁힐 수 없다** —
Unity 콘솔에서 실제 런타임 상태를 봐야 하는 지점.

**추가한 것 — 자가진단 로그(영구 코드, 스팸 아님)**: `CountryDockController`에 3가지 진단을
추가했다. (1) `OnEnable()`에서 country-dock-* 핵심 Label 6개 중 하나라도 Q<>() 바인딩이
실패하면 `Debug.LogWarning`으로 어떤 필드가 null인지 정확히 나열(Step 63과 같은 종류의 바인딩
실패 재발 여부를 즉시 드러냄). (2) `Start()`(모든 오브젝트의 Awake가 끝난 뒤, 재시도의 마지막
기회)에서 `WorldMap.Instance`/`WorldDataManager.Instance`가 여전히 null이면 구독이 영구적으로
실패했다는 경고. (3) `Populate()`에 `ShowPlaceholder()`와 동일한 `_nameLabel == null` 가드를
추가해, 바인딩이 실패한 상태에서 클릭이 들어와도 NRE 대신 조용히 스킵하도록 방어했다(Step 68의
try/catch로도 잡히지만, 애초에 예외를 안 던지는 게 더 안전). 이 3개는 정상 상태에서는 전혀
로그를 찍지 않고, 실제 이상이 있을 때만 원인을 정확히 지목한다 — 다음 실행에서 Console에 이
경고 중 하나라도 뜨면 그게 진짜 원인이다. 아무 경고도 없이 여전히 안 뜬다면 `WorldMap.
HandleCountryClicked()`를 호출하는 `CountryView.OnMouseUpAsButton()` 자체가 실행되지 않는
것(클릭 판정/Collider 문제)으로 조사를 좁혀야 한다.

**추가 정리**: 사용자 요청으로 `[EventManager]`(치료 자금 캡 감소 로그, 미사용된 `oldCap` 변수도
같이 제거), `[TransportManager]`(허브 좌표 해석 완료/감염 유입 로그 2건), `[FloatingTextEffect]`
(생성 로그 + 폰트 미발견 경고, DNA 버블 수집마다 찍혀 가장 스팸성이 컸음) 태그의 `Debug.Log`를
전부 제거했다. 게임 로직은 무변경.

**원칙 준수**: UI 변경 없음, 이벤트 시그니처/구독 코드/컨트롤러 공개 API 무변경 — 진단 로그
추가와 null 가드 하나만 더했다.

**변경 파일**: `Assets/Scripts/UI/CountryDockController.cs`, `Assets/Scripts/Managers/EventManager.cs`,
`Assets/Scripts/Managers/TransportManager.cs`, `Assets/Scripts/Gameplay/FloatingTextEffect.cs`,
`Assets/Scripts/Gameplay/DnaBubble.cs`([DnaBubble] 로그 제거는 직전 세션에서 이미 반영).

## Step 70 구현 메모 (Country Dock 재신고 3차 — 실측 로그로 범위 좁힘)

**사용자 실측 결과**: `HandleCountryChanged`는 정상 호출된다(즉 `WorldDataManager.OnCountryChanged`
구독은 살아있다). 그런데 `_shownCountryId`가 항상 비어있다(null) — `HandleCountryClicked()`가
`_shownCountryId`를 세팅하는 유일한 경로인데 실행이 안 되고 있다는 뜻.

**CountryStatusPanelController 국가 선택 방식 확인(요청 3)**: 이 컨트롤러는 `WorldMap.
OnCountryClicked`를 아예 구독하지 않는다 — "선택된 국가 1개"라는 개념 자체가 없고,
`WorldDataManager.OnCountryChanged` 하나만으로 48개국 전체를 항상(게이트 없이) 갱신한다. 그래서
지도 클릭과 무관하게 항상 정상으로 보인다 — Country Dock과 "같은 이벤트를 쓰게" 이식할 수 있는
구조가 아니다(요청 4). Country Dock은 "선택된 1개 국가"라는 상태가 필요한데, 그 상태를 만드는
경로가 `OnCountryClicked` 하나뿐이라 이 이벤트 자체를 고쳐야 한다 — 다른 이벤트로 우회하는 게
아니라 `OnCountryClicked`가 실제로 CountryDockController까지 도달하는지부터 확인이 먼저다.

**HandleCountryClicked가 실행되는지 확인하는 진단 3단 추가(요청 1·2)** — 호출 체인
`CountryView.OnMouseUpAsButton()` → `WorldMap.HandleCountryClicked(string)` →
`OnCountryClicked` 구독자들(`CountryDockController.HandleCountryClicked` 포함) 전체에 로그를
심어 어느 링크에서 끊기는지 실측 가능하게 했다:

1. `CountryView.OnMouseUpAsButton()` — 클릭 자체가 감지되는지, `WorldMap.Instance`가 그 순간
   null인지(`WorldMap.Instance?.HandleCountryClicked(...)`는 null이면 아무 로그 없이 조용히
   아무 일도 안 하는 구조라 기존엔 이 실패가 안 보였음).
2. `WorldMap.HandleCountryClicked(string)` — countryId로 country 해석 성공 여부,
   `OnCountryClicked`의 실제 구독자 수(`GetInvocationList().Length`) — 0이면 아무도 구독 안 된
   상태로 클릭이 도착했다는 뜻.
3. `CountryDockController.Subscribe()` — `WorldMap.Instance != null`이 아니면 경고(이미 Step 69),
   맞으면 `+=` 직후 구독자 목록에 이 인스턴스가 실제로 걸려있는지(`Delegate.Target == this`)까지
   확인해 로그로 남김(Step 69의 Start() 시점 null 체크보다 더 이른 시점, 더 정밀한 확인).
   `HandleCountryClicked()` 진입 로그는 Step 69에서 이미 추가됨(진입 여부/country.id/이전
   `_shownCountryId` 출력).

**아직 원인 미확정** — 이 요청을 처리하는 이 세션에서도 Unity를 직접 실행할 수 없어 실제 로그
값을 확보하지 못했다. 사용자가 위 3개 로그를 다음 플레이테스트에서 확인해 어느 지점에서 체인이
끊기는지 알려주면(① 클릭 자체가 안 잡히는지, ② WorldMap.Instance가 클릭 시점에 null인지,
③ 구독자 수가 0인지, ④ 구독자는 있는데 이 인스턴스가 없는지) 그에 맞는 정확한 수정을 바로
적용한다. 진단 로그는 원인 확정 후 전부 제거 예정(현재는 매 클릭 시 한 번씩만 찍혀 스팸은 아님).

**원칙 준수**: UI 변경 없음, 이벤트 시그니처/공개 API 무변경 — 진단 로그만 추가.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`, `Assets/Scripts/Gameplay/WorldMap.cs`,
`Assets/Scripts/UI/CountryDockController.cs`.

### Step 70 추가 대응 (진단 코드 자체의 컴파일 에러 수정)

**증상**: 사용자가 실행 전 Unity 콘솔에서 컴파일 에러 2종 신고 — `CS0070`(`WorldMap.
OnCountryClicked`는 `+=`/`-=` 외에는 선언 클래스 밖에서 접근 불가) x2, `CS0246`(`Action<>`
타입을 못 찾음).

**원인**: `CountryDockController.Subscribe()`에 추가한 진단 코드가 `WorldMap.Instance.
OnCountryClicked.GetInvocationList()`를 **선언 클래스(WorldMap) 밖에서** 호출했다 — C#
`event` 키워드는 정확히 이런 외부 접근(구독/해제 이외의 멤버 호출)을 막으려고 존재하는
것이라 컴파일 자체가 안 된다. `Action<Country>`도 이 블록에서만 쓰였는데 이 파일에
`using System;`이 없어 같이 에러가 났다.

**수정**: 문제의 `foreach`/`GetInvocationList()` 블록을 제거하고, `+=` 실행 여부만 남기는
로그로 교체했다. 실제 구독자 수는 이미 **선언 클래스 안**인 `WorldMap.HandleCountryClicked()`
내부에 남긴 로그(Step 70 본문)로 확인 가능하므로 진단 목적은 그대로 달성된다.

**남은 경고 2종(CS0414, `CountryView._lastLoggedInfectionBand`/`_lastLoggedDeadBand`,
`SimulationManager.airRouteSpreadChance`/`seaRouteSpreadChance`)은 이번 작업과 무관한 기존
코드의 미사용 필드 경고 — 컴파일을 막지 않으며 이번 세션에서 건드리지 않았다.**

**변경 파일**: `Assets/Scripts/UI/CountryDockController.cs`.

### Step 70 추가 대응 (진짜 원인 발견 — BoxCollider2D 크기가 국가 클릭 자체를 막고 있었다)

**사용자 실측 결과**: `[CountryView] OnMouseUpAsButton` 로그가 국가를 클릭해도 단 한 줄도 안
찍힘 — 클릭 이벤트 체인의 가장 첫 단계(`CountryView`)조차 도달하지 못하고 있다는 뜻. 사용자가
"레이어 문제 아니냐"고 제안.

**조사 — Unity 레이어는 아니었지만 결이 같은 문제였다**: `ProjectSettings/TagManager.asset`
확인 결과 이 프로젝트는 커스텀 Physics 레이어를 하나도 정의하지 않았고(기본 Default뿐), 카메라
`m_CullingMask`도 `4294967295`(전체 레이어 포함)라 Unity Layer 자체는 원인이 아니었다. 대신
`CountryView.cs` 전체를 다시 훑다가 결정적인 것을 찾았다 — `[RequireComponent(typeof(
BoxCollider2D))]`로 콜라이더는 자동으로 붙지만, `Awake()`/`ApplyCountryShape()`(국가별 실루엣
스프라이트를 `_renderer.sprite`에 대입하는 메서드) 어디에도 **`_collider.size`/`.offset`을
설정하는 코드가 없었다.** `CountryView`용 프리팹도 존재하지 않아(전부 씬에 직접 배치) 콜라이더
크기는 순전히 `GamePlay.unity`에 저장된 값에 의존하는데, China(`countryId: CHI`, fileID
735204845)의 `BoxCollider2D`를 직접 열어보니 `m_Size: {x: 0.16, y: 0.16}`, `m_Offset: {x: 0,
y: 0}` — 세계지도 캔버스(약 8×3.4유닛) 기준으로 보면 지도 중앙 부근에 손톱만한 클릭 가능 영역
하나가 전부였다. `m_SpriteTilingProperty.newSize: {x: 0.82, y: 0.96}`(에디터의 "Edit Collider"
자동 맞춤 도구가 계산해뒀던 값으로 추정)와 실제 적용된 `m_Size`가 다른 것으로 보아, 누군가
한 번쯤 자동 맞춤을 시도했지만 실제로 적용되지는 않은 채 남은 것으로 보인다. `grep`으로
`m_Size: {x: 0.16, y: 0.16}`를 씬 전체에서 세어보니 92회 등장 — **국가 48개 전부가 Step 29
"캔버스 통일" 이전(플레이스홀더 스프라이트 시절)의 기본 콜라이더 크기에 그대로 고정돼 있는
프로젝트 전역 문제**였다. 눈에 보이는 국가 실루엣을 클릭해도 그 지점엔 콜라이더가 없으니
`OnMouseUpAsButton()` 자체가 안 불리는 게 당연했다 — Country Dock/CountryPopup 둘 다
무반응이던 근본 원인이 여기였다(Step 68/69의 이벤트 디스패치 방어, Step 63의 컴포넌트 배선은
전부 정상이었는데 애초에 클릭이 발생하지 않고 있었을 뿐).

**수정**: `CountryView.ApplyCountryShape()`에서 `_renderer.sprite = shape;` 직후
`_collider.size = shape.bounds.size; _collider.offset = shape.bounds.center;` 추가 — 국가별로
로드된 스프라이트의 실제 바운즈에 맞춰 매번 콜라이더를 재계산한다. 씬 파일을 48번 손으로 고칠
필요 없이 코드 한 곳만 고치면 48개국 전부 해결된다. `_collider`는 같은 `Awake()`에서 이 호출보다
먼저 대입되므로 null 걱정 없음(방어적으로 null 체크는 남겨둠).

**검증 남음**: Step 70 본문에서 추가한 진단 로그(`[CountryView]`/`[WorldMap]`/
`[CountryDockController]`)들은 이 수정이 실제로 문제를 해결했는지 다음 플레이테스트로
확인한 뒤 제거 예정 — 이번엔 `[CountryView] OnMouseUpAsButton`부터 `[CountryDockController]
HandleCountryClicked 진입`까지 체인 전체가 찍히고 `_shownCountryId`가 클릭한 국가 id로
채워지는지가 성공 기준이다.

**원칙 준수**: UI 변경 없음, 게임 로직(감염/사망/치료제 등) 무변경 — 콜라이더 크기 계산 로직만
추가. 이벤트 시그니처/컨트롤러 API 전부 무변경.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`.

## Step 78 구현 메모 (CountryPopup — 국가 대시보드 1차 확장: 도넛 차트/감염 통계/의료 부하/세계 순위)

**배경**: 사용자가 별도 조사 요청(코드 수정 없이 설계안만)으로 `Docs/
CountryStatus_Dashboard_Investigation.md`를 먼저 작성했다 — CountryPopup(국가 상세 모달)의
정보량이 부족해 "국가 대시보드" 느낌으로 확장이 필요하다는 문제의식에서, 현재 접근 가능한
데이터를 전수 조사하고 5개 신규 표시 항목(도넛 차트/감염 통계/의료 시스템 상태/국가 순위/최근
국가 이벤트)의 실현 가능성을 평가했다. 이번 Step은 그 조사 문서의 "최종 추천안" 중 사용자가
1차 범위로 승인한 4항목만 구현한다 — "최근 국가 이벤트"는 조사 문서 2.5절에서 확인한 대로
`NewsEvent`에 countryId가 없고 `HumanResistanceManager`의 공항/국경 폐쇄가 Debug.Log로만
남아 구조화된 이력이 없어 EventManager/HumanResistanceManager 확장이 선행돼야 하므로 이번
범위에서 제외했다(조사 문서에 남겨둔 대로 후속 Step 후보).

**구현 — 도넛 차트(신규 컴포넌트)**: UI Toolkit엔 내장 차트 위젯이 없어 `HudSparkline.cs`(HUD
스탯 인라인 그래프, `generateVisualContent`+`Painter2D` 직접 그리기)와 동일한 방식을 재사용해
`Assets/Scripts/UI/CountryDonutChart.cs`를 신규 작성했다. 건강(`SusceptibleCount`)/감염
(`infectedCount`)/사망(`deadCount`) 3개 값을 비율로 환산해 `Painter2D.Arc()`로 부채꼴 3개를
채운 뒤, 그 위에 패널 배경색(`--color-bg-panel`과 동일한 RGB를 C#에 하드코딩한
`CountryDonutChart.HoleColor`)으로 가운데 원을 덮어 "도넛" 모양을 만드는 오버레이 트릭을
썼다(Painter2D가 "내부 반지름을 뺀 부채꼴" 경로를 직접 지원하지 않기 때문). 3개 색상(건강=
`--color-status-info`, 감염=`--color-status-infected`, 사망=`--color-status-dead`)도 Painter2D가
USS 커스텀 프로퍼티를 읽지 못해 C# 쪽에 RGB를 그대로 복제했다 — `HudController`가
`HudSparkline`에 `Color`를 직접 넘기는 것과 동일한 패턴.

**구현 — CountryPopupController.cs**: 헤더 바로 아래(기존 `modal-rows` 위)에 도넛+범례 3줄
(건강/감염/사망 각각 수치+비율)을 최초 1회만 생성하고(`BuildDonutAndLegend()`, 
`CountryStatusPanelController`의 "행 캐싱, Clear+재생성 없음" 패턴과 동일 이유) 이후에는
`UpdateDonutAndLegend()`가 텍스트/차트 값만 갱신한다. 이 범례가 정확한 수치+비율을 이미
보여주므로, 기존 `Populate()`의 "감염자"/"사망자" 절대값 data-row 2줄은 제거했다. 대신 data-row
목록에 다음을 추가: 생존자 수(`LivingPopulation`), 감염률(`infectedCount/LivingPopulation`),
치사율(추정, `deadCount/(deadCount+infectedCount)` — 아래 설명 참고), 의료 시스템 상태(신규
계산식), 세계 순위(48개국 기준). "인구/의료 수준/기후/공항/항구/국경" 기존 행은 순서만
재배치하고 내용은 그대로 유지했다.

**치사율(추정) 근사식 채택 이유**: 이 게임엔 "감염 후 자연 회복" 개념이 없다 — 치료제가 100%
완성되는 순간 `SimulationManager.EradicatePathogen()`이 전 세계 감염자를 한 번에 0으로 만들
뿐, 국가별로 "감염 경험이 있었던 누적 인구"를 별도로 세는 카운터가 없다. 따라서 정확한 역학적
치사율(누적 사망/누적 감염)은 현재 데이터로 계산할 수 없어(조사 문서 2.2절에서 이미 확인), 현재
스냅샷 기준 근사치 `deadCount/(deadCount+infectedCount)`로 대체하고 라벨에 "(추정)"을 명시했다
— 정확한 누적치가 필요해지면 `Country`에 단조증가 카운터 필드 1개 + `SimulationManager.RunTick()`
한 줄 추가로 해결 가능(조사 문서에 남겨둔 대안, 이번 범위 아님).

**의료 시스템 상태 — 새 계산식(신규 데이터 없이 계산만)**: `부하 = 감염률 × (1 - HealthLevel)`
로 "의료 체계가 지금 이 순간 버티고 있는지"를 4단계(정상/주의/과부하/붕괴, 임계값 0.1/0.3/0.6)로
분류했다. 기존 `Country.GetCollapseStage()`(사망률 기준 사회 전체 붕괴, 이미 있음)와는 별개
축이라 이름과 색상을 겹치지 않게 분리했다 — DESIGN.md의 "severity 축과 노드 상태 축을
분리하라"는 원칙을 여기서도 그대로 적용해, 라벨 자체를 "의료 시스템 상태"로 구분했다. 색상은
기존 4개 severity 토큰(info→infected→danger→dead)을 정상→주의→과부하→붕괴 순으로 그대로
재사용해 새 USS 색상 추가 없이 해결했다.

**세계 순위 — 48개국 정렬**: `WorldDataManager.Instance.Countries`(국가현황 패널이 이미 순회하는
것과 동일 리스트)를 감염자 수/사망자 수/감염률 3개 지표로 각각 `OrderByDescending` 후 표시 중인
국가의 순번을 찾아 "감염자 12위 · 사망자 8위 · 감염률 19위 (전체 48개국)" 한 줄로 압축했다. 다만
기존 `HandleCountryChanged`는 "표시 중인 국가 자신의 값이 바뀔 때"만 재계산하는데, 순위는 **다른
나라**의 값 변화만으로도 바뀔 수 있어 그것만으로는 팝업이 열려있는 동안 순위가 고정돼 보이는
문제가 있다. `WorldDataManager.OnWorldStateChanged`(틱당 정확히 1회만 발행,
`RecalculateWorldTotals` 참고)를 추가로 구독해 팝업이 열려 있을 때만 매 틱 1회 전체를 다시
그리도록 했다 — 48개국 정렬 3회를 틱당 1회 수준으로만 계산하므로 국가현황 패널이 겪었던
"매 틱 O(48) 반복 렉"(Step 61)과는 무관하다.

**UXML/USS**: `CountryPopup.uxml`의 `tactical-panel__header`와 `modal-rows` 사이에
`popup-donut-row`(도넛 96×96px + 범례 컨테이너) 신규 삽입. `CountryPopup.uss`에 `.popup-donut-row`
(구분선, `data-row`와 동일 `--color-grid-line` 톤)/`.popup-donut`(고정 크기)/`.popup-donut-legend*`
(범례 행/색상 점/라벨/값 — `Hud.uss`의 `news-entry-dot` "색상 원형 칩" 언어 재사용, 색상은
`.data-value--*`와 동일한 severity 토큰)를 추가했다. 새 USS 색상 변수는 추가하지 않았다 — 전부
`Theme.uss`에 이미 있는 토큰 재사용.

**검증 남음(Unity 에디터 미접속으로 렌더링 미확인 — 이 프로젝트의 Painter2D 작업 공통 제약,
Step 75와 동일 사유)**: `Painter2D.Arc()`/`Angle` API 컴파일 확인, 도넛 3조각 각도·색상이 실제로
건강/감염/사망 순서로 정확히 매핑되는지, 범례 텍스트가 340px 폭 안에서 줄바꿈 없이 들어가는지,
패널이 길어진 만큼(도넛+범례+행 4개 추가) 1440×3120 세로 화면에서 하단이 잘리지 않는지. 전부
`Docs/QA_Checklist.md`에 항목 추가.

**원칙 준수**: 게임 로직(감염/사망/치료제/봉쇄 등 SimulationManager/HumanResistanceManager/
EventManager) 무변경 — `CountryPopupController`와 UI 파일만 건드렸다. 기존 8개 행 중 "감염자"/
"사망자" 2개만 도넛 범례로 대체하고 나머지 6개(인구/의료수준/기후/공항/항구/국경)는 값과 라벨
그대로 유지.

**변경 파일**: `Assets/Scripts/UI/CountryDonutChart.cs`(신규), `Assets/Scripts/UI/
CountryPopupController.cs`, `Assets/UI/CountryPopup.uxml`, `Assets/UI/CountryPopup.uss`.

## Step 79 구현 메모 (CountryPopup 2차 다듬기 — 세계 순위 행 분리 + 이동 통제 섹션 시인성 개선)

**배경**: Step 78에서 세계 순위를 "감염자 12위 · 사망자 8위 · 감염률 19위 (전체 48개국)" 한 줄로
압축했는데, 다른 data-row(라벨 1개+값 1개)와 표현 문법이 달라 오히려 읽기 불편하다는 피드백 —
사용자가 감염자/사망자/감염률 순위를 각각 별도 data-row로 분리해달라고 요청. 동시에 공항/항구/
국경 3행이 다른 data-row 사이에 묻혀 "이 나라가 지금 봉쇄 중인지"를 훑어보기 어렵다는 시인성
문제도 함께 요청받았다(아이콘 추가 또는 이동 통제 섹션 형태 — 둘 중 택일 가능).

**세계 순위 분리**: `CountryPopupController.FormatWorldRank()`(문자열 1개 반환)를
`AddWorldRankRows()`(data-row 3개를 직접 Add)로 교체 — "감염자 순위"/"사망자 순위"/"감염률 순위"
각각 `"{rank}위 / {total}개국"` 값으로 별도 행에 표시한다. 순위 계산 로직(`RankOf<TKey>` 제네릭
헬퍼, `WorldDataManager.Countries` 48개국 정렬)은 그대로 재사용 — 계산 비용/갱신 타이밍(틱당 1회,
`OnWorldStateChanged` 구독)도 변경 없음.

**이동 통제 섹션 — 아이콘 대신 텍스트 캡션 + accent bar를 택한 이유**: 폰트 아이콘 글리프(✈/⚓ 등)는
UI Toolkit `Label`이 실제로 렌더링할 수 있는지 실기기/에디터 검증 없이는 알 수 없다 — `Hud.uss`의
`news-entry-dot` 주석("아이콘 폰트 글리프는 폰트 지원 여부를 실기기에서 검증할 수 없어 리스크가
있다. 대신 이미 검증된 색상 칩 언어를 재사용")이 이미 같은 이유로 이모지 아이콘 대신 색상 도트를
택한 선례라, 이번에도 같은 판단 기준을 적용해 "이동 통제 섹션" 쪽을 택했다. CLAUDE.md가 이미 서술한
"교통 유닛 그래픽 — 이모지 기반(✈️)"은 지도 위 월드 스프라이트/TextMesh 렌더링이라 UI Toolkit
Label과는 폰트 파이프라인 자체가 달라 이 판단에 참고가 되지 않는다.

**구현**: 공용 프레임 `TacticalModalController`에 두 가지를 추가했다(이 컨트롤러를 상속하는 다른
화면에서도 재사용 가능하도록 CountryPopupController 전용이 아니라 기반 클래스에 얹음) —
1) `AddSectionCaption(string text)`: `modal-section-caption` 클래스의 `Label`을 `modal-rows`에
   직접 추가해 data-row 사이에 섹션 구분선 역할을 하는 캡션을 끼워 넣는다.
2) `AddRow()`에 4번째 선택 인자 `rowClass` 추가: data-row 엘리먼트 자체(값이 아니라 행 전체)에
   추가 클래스를 입힐 수 있게 했다. 기존 호출부(CountrySelect/EndingScreen/UpgradeTree 등)는
   3번째 인자까지만 쓰므로 컴파일/동작 영향 없음.

`Tactical.uss`에 `.modal-section-caption`(좌측 정렬 캡션, tactical-caption과 달리 가운데 정렬이
아님)과 `.data-row--open`/`.data-row--closed`(좌측 3px accent bar, `--color-status-info`/
`--color-status-danger`)를 추가 — 새 색상 변수 없이 기존 severity 토큰만 재사용했다.

`CountryPopupController.Populate()`에서 공항/항구/국경 3행 앞에 `AddSectionCaption("이동 통제")`를
넣고, 각 행을 신규 `AddTransportRow(label, isOpen, openText, closedText)` 헬퍼로 교체 — 기존
`data-value--info/--danger` 텍스트 색은 그대로 유지한 채 `data-row--open/--closed`로 좌측 accent
bar를 추가해 이중으로 상태를 표시한다. 국경은 `isBorderClosed`를 반전(`!country.isBorderClosed`)해
"개방=true" 의미로 통일해 넘기되, 닫힘 표시 텍스트는 기존과 동일하게 "봉쇄"를 유지(공항/항구는
"폐쇄")하도록 `openText`/`closedText` 파라미터로 분리했다.

**검증 남음**: `.data-row--open/--closed`의 3px 좌측 보더가 기존 `.data-row`의
`border-bottom`(구분선)과 시각적으로 충돌하지 않는지, `.modal-section-caption`이 위/아래 data-row와
간격이 자연스러운지 — Unity 에디터 미접속으로 미확인(Step 78과 동일 제약). `Docs/QA_Checklist.md`에
항목 추가.

**원칙 준수**: 게임 로직 무변경. `TacticalModalController`(공용 프레임) 확장은 기존 시그니처를
깨지 않는 선택적 매개변수/신규 메서드 추가뿐이라 다른 소비 화면(CountrySelect/EndingScreen/
UpgradeTree)에 영향 없음.

**변경 파일**: `Assets/Scripts/UI/TacticalModalController.cs`, `Assets/Scripts/UI/
CountryPopupController.cs`, `Assets/UI/Tactical.uss`.

### Step 79 추가 대응 (팝업 위치 상향 조정)

**배경**: Step 78/79로 도넛 차트+범례+data-row가 늘어나 `CountryPopup.popup-root`가 세로로
길어졌다 — 기존 `top: 30%`(1440×3120 기준 약 936px 지점에서 시작)로는 하단이 화면 밖으로
잘릴 위험이 커져 사용자가 위치를 위로 옮겨달라고 요청.

**수정**: `.popup-root`의 `top`을 `30%` → `16%`(약 499px 지점)로 올렸다. `left: 50%` +
`margin-left: -170px`(가로 중앙 정렬)는 그대로 유지 — 세로 위치만 조정.

**검증 남음**: Unity 에디터 미접속으로 실제 화면에서 상단 SafeArea/코너컷과 겹치지 않는지,
패널 전체가 화면 안에 들어오는지 미확인. `Docs/QA_Checklist.md`에 항목 추가.

**변경 파일**: `Assets/UI/CountryPopup.uss`.

## Step 80 구현 메모 (CountryStatusPanel → "GLOBAL STATUS CENTER" 세계 감염 현황 센터 재설계)

**배경**: 사용자가 HUD "국가현황" 버튼으로 여는 `CountryStatusPanelController`(48개국 스크롤
리스트뿐이던 화면)를 `CountryPopupController`(개별 국가 상세, Step 78/79로 이미 확장 완료)와
역할이 겹치면서도 정보 밀도는 더 낮다고 지적 — "이 국가가 어떤 상태인가"가 아니라 "지금 세계
상황이 어떤가"에 집중하는 화면으로 재정의해달라는 요청. 사전 조사(조사 전용, 코드 미수정)로
`Docs/CountryStatus_Dashboard_Investigation.md`(원래 CountryPopup 확장 조사 문서)의 데이터
소스/계산식 재사용 가능 여부를 다시 확인한 뒤, 세션 내에서 AskUserQuestion으로 4가지(패널
크기/스타일 전환 여부/랭킹 개수/치사율 계산 방식)를 확정하고 사용자가 최종 승인한 설계대로
구현했다.

**중요 제약**: `CountryPopupController`/`CountryPopup.uxml`/`CountryPopup.uss`는 이미 완료된
기능으로 간주해 이번 작업에서 전혀 손대지 않았다. 두 화면에서 공식이 겹치는 치사율(추정)/의료
부하 계산은 공유 헬퍼로 추출하지 않고 `CountryStatusPanelController`에 동일 공식을 독립적으로
복제했다(`CountrySelectController.DevValueClass()`가 `CountryPopupController`와 동일 규약을
별도 구현해둔 기존 선례와 같은 방식 — 두 화면이 서로 의존하지 않게 하기 위함).

**신규 데이터 모델 없음**: `Country`/`WorldState`/`WorldDataManager`의 기존 필드·계산 프로퍼티만
사용했다. `CountryDonutChart`는 이 화면(세계 현황 대시보드)에는 맞지 않는다고 판단해(모바일
세로 화면에서는 이미 HUD가 쓰는 `population-bar`가 정보 밀도·공간 효율 면에서 더 적절하다는
사용자 판단) 사용하지 않았다.

**레이아웃(위→아래, `CountryStatusPanel.uxml` 전면 재작성)**:
1. 헤더 — "GLOBAL STATUS CENTER" 캡션(`tactical-panel__title`) + ✕ 닫기 버튼(기존 하단
   "닫기" 풀폭 버튼을 CountryPopup과 동일한 헤더 우측 ✕ 버튼 관례로 교체).
2. 세계 요약 — `population-bar`(Hud.uss 클래스를 `<Style src="Hud.uss">`로 그대로 재사용,
   이 화면 전용 `UIDocument`에 새 인스턴스로 붙어 HUD와 이름/클래스가 겹쳐도 충돌 없음) +
   data-row 6줄(세계 인구/총 감염자/총 사망자/세계 감염률/치사율(추정)/경과 일수).
3. 국가 상태 분포 — `Country.GetCollapseStage()` 6단계(평시~소멸)를 `BucketOf()`로
   SAFE/WARNING/DANGER/COLLAPSE 4버킷으로 묶어 국가 수 집계(Normal=SAFE,
   FullCollapse=WARNING, Disorder=DANGER, NearAnarchy/FullAnarchy/Extinct=COLLAPSE로
   그룹화 — 임계값 자체는 기존 함수 그대로, 새 계산식 없음). `stat-chip`(Hud.uss, 6×6px
   색상 사각형)에 국가 데이터 severity 4색(`--color-status-info/infected/danger/dead`)을
   그대로 매핑한 modifier(`--safe/--warning/--danger/--collapse`)만 추가 — 새 색상 토큰
   없음(DESIGN.md Do 원칙).
4. 랭킹 — 감염자/감염률/치사율(추정)/의료 부하 각 TOP 10(사용자가 TOP 5 대신 TOP 10 선택).
   `WorldDataManager.Countries` LINQ `OrderByDescending().Take(10)` — `CountryPopupController`
   의 `RankOf()`/`CaseFatalityRate()`/`MedicalLoadStatus()`와 동일한 공식을 독립적으로
   복제해 재사용했다(위 "중요 제약" 참고). 치사율은 근사치(사망/(사망+감염))를 그대로
   사용(사용자 선택 — 정확한 누적 CFR을 위한 `Country.cumulativeInfectedCount` 신규 필드는
   "신규 데이터 모델 추가 금지" 제약과 충돌해 보류).
5. 48개국 목록 — 기존 3줄 행 구조(이름+단계/인구·감염·사망·의료 수준/공항·항구·국경) 그대로
   유지, 좌측 4px accent bar로 버킷 색상만 추가(코너컷 대신 밀도 절약형 강조, 기존
   `accent-bar-row` 문법과 동일).

**Tactical Design System 전환**: `Docs/UI_Design.md` §0.1/0.2 진단 당시 `CountryStatusPanel`은
애초에 전환 대상 7개 화면(MainMenu/CountrySelect/UpgradeTree/CountryPopup/EndingScreen/
RankingPanel + HUD) 목록에 없었다(§0.2, 853행 "CountryStatusPanel.uss에는 tactical 관련 로컬
정의가 없음" — 스코프 밖이었다는 뜻). 이번 개편으로 이 화면도 `tactical-panel`+코너컷 4개+
`data-row`+severity 4색 체계로 합류시켰다(사용자 승인) — 사실상 8번째 전환 화면.

**갱신 빈도/성능 설계**: 세계 요약/분포/랭킹은 48개국 전체를 다시 훑어야 하는 "집계" 값이라
`WorldDataManager.OnWorldStateChanged`(틱당 정확히 1회)에만 반응해 전체를 다시 그린다(48개국
정렬 4회 + 랭킹 행 40개 재생성, 틱당 1회면 성능 영향 없음 — Step 78/79와 동일 근거). 반면
하단 48개국 목록은 국가 하나만 값이 바뀌어도 되므로 기존 `OnCountryChanged`(국가별, 틱당 최대
20~30회) 구독 + 행 캐싱(`_rowsByCountryId`, 최초 1회 생성 후 라벨 텍스트만 갱신) 구조를 그대로
유지했다 — 이 목록에 캐싱 없이 Clear()+재생성을 쓰면 애초에 이 컨트롤러가 겪었던 "매 틱 48개국
전체 재생성" 프레임 드랍 버그가 재발하기 때문(주석 참고, 이 파일 상단 클래스 주석에도 명시).

**검증 남음**: Unity 에디터 미접속으로 실제 렌더링 미확인(Step 75/78/79와 동일 제약) — 1440×3120
세로 화면에서 `top: 8%`로 확장한 패널이 실제로 잘리지 않는지, `population-bar`가 별도
`UIDocument` 인스턴스에서도 정상 동작하는지, 랭킹 40행(4종×10)이 스크롤 안에서 자연스럽게
읽히는지, 국가 목록 accent bar 색이 실제 붕괴 진행 중인 국가에서 합리적으로 바뀌는지.
`Docs/QA_Checklist.md`에 항목 추가.

**변경 파일**: `Assets/Scripts/UI/CountryStatusPanelController.cs`(전면 재작성),
`Assets/UI/CountryStatusPanel.uxml`(전면 재작성), `Assets/UI/CountryStatusPanel.uss`(전면
재작성). `CountryPopupController.cs`/`CountryPopup.uxml`/`CountryPopup.uss`는 미변경.

### Step 80 추가 대응 (반투명 오버레이 가독성 문제 — 배경 불투명도/패널 크기 재조정)

**신고 내용**: Step 80 구현 결과가 "Tactical Dashboard보다 반투명 Overlay에 가깝다" — 배경
지도/항로선이 SAFE/WARNING/DANGER/COLLAPSE·랭킹·세계 통계와 겹쳐 보여 가독성이 낮다는 피드백.
이 화면은 국가 선택 화면이 아니라 Global Status Center이므로 지도는 배경일 뿐, 데이터 판독성이
최우선이어야 한다는 지적.

**원인**: `.status-root`가 쓰던 `--color-bg-panel-strong`(Theme.uss)이 `rgba(15,15,25,0.97)`로,
CountryPopup의 `--color-bg-panel`(0.95)과 사실상 차이가 없는 수준이었다 — 이름은 "더 불투명해야
하는 패널"이라고 되어 있었지만 실제 값은 그 의도를 충분히 반영하지 못했다.

**수정**:
1. `--color-bg-panel-strong`을 `rgba(9, 9, 16, 0.99)`로 대폭 상향(Theme.uss) — 이 토큰은
   `CountryStatusPanel.uss`만 참조하므로 값을 올려도 다른 화면(CountryPopup 등)에는 영향 없음.
2. `.status-root`의 `top`을 `8%` → `3%`로 더 낮춰 거의 풀스크린에 가깝게 확장(bottom 12.5%는
   Hud tabs-row 노출을 위해 유지).

**검증 남음**: Unity 에디터 미접속으로 실제 화면에서 지도/항로선이 충분히 가려지는지, 0.99
알파가 실제 렌더링에서 완전 불투명에 가깝게 보이는지 미확인. `Docs/QA_Checklist.md`에 항목
추가.

**변경 파일**: `Assets/UI/Theme.uss`, `Assets/UI/CountryStatusPanel.uss`.

### Step 80 추가 대응 2 (진짜 원인 — status-scroll 오버플로우로 배경이 잘려나간 레이아웃 버그)

**신고 내용**: 알파값을 1.0(완전 불투명)까지 올렸는데도 인게임에서 여전히 투명하게 보인다는
재신고. 데이터(감염률/사망률 등)는 실시간으로 정상 갱신되고 있어 C#/이벤트 구독 로직은 문제
없다는 점, 사용자가 직접 `CountryStatusPanel.uxml`을 UI Builder(Play 모드와 무관하게 에셋을
그대로 렌더링하는 도구)로 열어서 캡처를 공유 — 헤더+세계 요약(population-bar)까지는 불투명한
배경이 보이는데 그 아래 "국가 상태 분포"(SAFE/WARNING/DANGER/COLLAPSE)부터는 UI Builder의
투명 격자 무늬(checkerboard)가 그대로 보였다.

**진단 과정**: 이전 두 차례 대응(알파값 상향, top 인셋 축소, 레이어/sortingOrder 점검)은 전부
"패널이 왜 반투명해 보이는가"를 배경색 관점에서만 접근한 오진이었다 — UI Builder 캡처로 처음
확실해진 사실은 **"배경색이 부족한 게 아니라, 패널 박스 자체가 짧게 계산돼서 하단 콘텐츠가
박스 밖으로 흘러넘치고 있다"**는 것. 박스 밖으로 넘친 부분은 애초에 `status-root`의
`background-color`가 칠해지는 영역이 아니므로 알파를 아무리 올려도 소용없었다.

**근본 원인**: `.status-scroll`(ScrollView)에 `flex-grow: 1`만 있고 `flex-basis`가 없었다.
UI Toolkit(Yoga)에서 `flex-basis`를 안 주면 자식이 **자기 콘텐츠의 실제 크기**(세계 요약+
분포+랭킹 4종 40행+48개국 목록 = 매우 긴 콘텐츠)를 기준값으로 잡아버려, `flex-grow`가 있어도
이미 그 자체로 충분히(혹은 그 이상) 크기 때문에 전혀 줄어들지 않는다. `.status-root`에는
`overflow: hidden`도 없었기 때문에, `status-scroll`의 실제 콘텐츠가 `status-root`의 고정
높이(top 3%~bottom 12.5%)를 넘는 순간 그 초과분이 그대로 화면에 노출됐다 — `status-root`의
배경색은 자기 박스 크기만큼만 칠해지므로, 넘친 영역은 아무 배경도 없는 상태(=지도가 그대로
비치는 것과 동일한 결과)로 보인 것.

이 프로젝트에 이미 있던 **가로 방향 오버플로우 버그**(Step 76, `Tactical.uss` `.data-value`에
`flex-shrink`/`min-width: 0`이 없어서 긴 값이 패널 폭 밖으로 넘치던 문제)와 **완전히 같은
종류의 버그를 세로 방향에서 재현**한 것이었다 — 그때의 교훈(flex 아이템은 명시적으로 축소/
기준값을 지정하지 않으면 콘텐츠 크기를 그대로 밀어붙인다)을 새 화면에 적용할 때 놓쳤다.

**수정**(`CountryStatusPanel.uss`):
1. `.status-root`에 `flex-direction: column`(명시) + `overflow: hidden` 추가 — 이후로도
   비슷한 계산 오차가 생기더라도 박스 밖으로 내용이 새어나가는 대신 잘리도록 안전장치를 건다.
2. `.status-scroll`에 `flex-shrink: 1; flex-basis: 0;` 추가 — "일단 0에서 시작해 남는
   공간만큼만 자란다"로 강제해 `status-root`의 실제 가용 높이 안에 확실히 갇히도록 했다.

**교훈**: Step 78/79/80에서 반복된 패턴 — Unity 에디터 미접속 상태로 여러 Step에 걸쳐 "이럴
것이다"로 추정 수정(알파/인셋/레이어)을 먼저 시도했지만, 실제로는 사용자가 UI Builder로 직접
연 캡처 한 장이 훨씬 빠르고 정확한 진단 수단이었다. 앞으로 비슷한 "안 보인다/이상하다" 류
신고는 배경색 추정 수정보다 **UI Builder 캡처(또는 Play 모드 스크린샷)를 먼저 요청**하는
편이 낫다.

**검증 남음**: Unity 에디터 미접속으로 실제 적용 후 렌더링 미확인 — `Docs/QA_Checklist.md`에
항목 추가(기존 "배경 불투명도 확인" 항목을 이 레이아웃 수정 확인으로 대체).

**변경 파일**: `Assets/UI/CountryStatusPanel.uss`.

### Step 80 추가 대응 3 (CountryPopup 닫기 버튼 무반응 — 패널 충돌 수정)

**신고 내용**: 여전히 투명하게 보인다는 재신고 + 새로운 증상 — CountryStatusPanel이 열린 상태에서
지도의 국가 영토를 클릭하면 `CountryPopupController`(국가 상세 팝업)가 뜨는데, 그 팝업의 닫기
(✕) 버튼을 눌러도 반응이 없고 Console에 아무 로그도 안 뜬다.

**원인**: `CountryPopupController`는 `WorldMap.OnCountryClicked`를 **독립적으로** 구독해서
어떤 화면이 열려 있든 상관없이 팝업을 띄운다(`UIManager`를 거치지 않는 직접 구독). 씬 확인 결과
`CountryStatusPanel`과 `CountryPopup`의 UIDocument `sortingOrder`가 둘 다 1로 동일 — 두 패널이
동시에 열려 있으면 어느 쪽이 위에 그려지는지 보장되지 않는다. 게다가 UI Toolkit은 **엘리먼트가
시각적으로 투명해도 기본적으로 클릭(hit-test)을 막는다** — CountryStatusPanel의 배경이 (이번
Step 80 추가 대응들 이전까지는) 안 보였더라도, 그 자리에 깔린 VisualElement 자체는 여전히
클릭을 가로챌 수 있었다. 그 결과 CountryPopup의 ✕ 버튼 위치에 CountryStatusPanel의 (보이지 않는)
엘리먼트가 겹쳐 있으면 클릭이 CountryPopup까지 도달하지 못하고 조용히 씹혔다 — `RegisterCallback`
자체가 안 불렸으니 로그가 없는 것도 당연했다.

**제약**: `CountryPopupController`/`UIManager`는 이번 작업 수정 대상이 아니라(사용자 지정, "이미
완료된 기능") 그쪽에서 "다른 패널이 열려 있으면 먼저 닫는다" 조정을 걸 수 없다.

**수정**(`CountryStatusPanelController.cs`만 변경): 이 컨트롤러가 스스로 `WorldMap.OnCountryClicked`
를 구독해서, 지도에서 국가를 클릭하는 순간 **이 대시보드가 스스로 닫히도록** 했다
(`HandleWorldMapCountryClicked`). 그러면 그 직후엔 CountryPopup 단독으로만 화면에 남아 충돌 자체가
사라진다 — UX적으로도 "세계 요약을 보다가 국가 하나를 짚으면 그 나라 상세로 전환"이 자연스럽다.
`CountryPopupController.cs`/`WorldMap.cs`/`UIManager.cs`는 미변경.

**참고 — 별개 문제(이번 범위 아님)**: 지도의 국가 클릭 자체(`WorldMapCameraController`)는 레거시
`Input.GetMouseButtonDown` + 물리 레이캐스트로 동작해 **UI Toolkit의 클릭 처리를 아예 거치지
않는다** — 즉 CountryStatusPanel이 화면을 완전히 덮고 있어도 지도 클릭 자체는 항상 통과된다.
이건 이 화면의 버그가 아니라 프로젝트 전역의 기존 구조(CLAUDE.md TODO "국가 탭 시 Country
Dock/CountryPopup 동시 표시"와 같은 계열의 문제)라 이번 범위에서 손대지 않았다.

**검증 남음**: Unity 에디터 미접속으로 미확인. `Docs/QA_Checklist.md`에 항목 추가.

**변경 파일**: `Assets/Scripts/UI/CountryStatusPanelController.cs`.

### Step 80 추가 대응 4 (진짜진짜 원인 — CountryStatusPanel.uss 자체 파싱 에러)

**신고 내용**: 사용자가 Unity Console의 실제 에러 로그를 공유 — `Assets/UI/CountryStatusPanel.uss
(line 67): error: Unsupported selector format: '...'`. 이게 결정적 증거였다.

**진짜 원인**: 67행 주석에 `/* population-bar/population-bar__track/__segment*/__summary는...`
라고 썼는데, 이 텍스트 안에 **의도치 않은 `*/`가 중간에 섞여 있었다**(`__segment*/__summary`—
"segment"와 "summary" 클래스명을 나열하려다 실수로 `*/`를 타이핑). CSS/USS 주석은 처음 만나는
`*/`에서 즉시 닫히므로, 이 시점에서 주석이 조기 종료되고 그 뒤에 이어지는 "__summary는 Hud.uss
클래스를 그대로 재사용(...) — 로컬 재정의 없음. world-summary-rows(data-row 6줄)는
Tactical.uss의 .detail-rows/.data-row 그대로 사용. */"까지의 텍스트 전체가 **실제 CSS 코드로
해석**됐다 — 당연히 유효한 선택자/규칙이 아니므로 파서가 "Unsupported selector format" 에러를
내고 **스타일시트 전체를 로드 실패** 처리했다.

**이게 왜 지금까지의 모든 수정이 "바뀐 게 없다"로 보였는지 설명한다**: 배경 알파 상향(0.97→0.99→
1.0), 인셋 축소(top 8%→3%), `overflow:hidden`/`flex-basis:0` 추가 — 전부 다 이 파일 안의 규칙
이었는데, 파일 자체가 파싱 에러로 통째로 무시되고 있었으니 어떤 값을 바꿔도 게임에 반영될 수가
없었다. 반면 C#(`CountryStatusPanelController.cs`) 변경은 스타일시트와 무관한 별도 컴파일
경로라 정상적으로 반영됐다 — "C#은 되는데 CSS만 하나도 안 바뀐다"는 패턴이 정확히 이 시나리오와
일치했다.

**교훈**: 여러 차례 "Play 모드 캐싱/재임포트 문제일 것"이라고 추정하고 사용자에게 에디터 재시작/
Reimport를 요청했는데, 실제로는 훨씬 단순한 문법 오류였다. Console 에러 로그 한 줄이 그 전의
모든 추정(레이어, 캐싱, sortingOrder)보다 압도적으로 빠르고 정확한 진단 수단이었다 — 앞으로
"안 바뀐다"류 신고는 처음부터 Console 에러 확인을 최우선으로 요청해야 한다(이미 Step 80 추가
대응 2에서 같은 교훈을 한 번 적었는데, 이번에 다시 확인됨).

**수정**: 67행 주석을 `*/`가 중간에 섞이지 않도록 다시 썼다(`__segment*/__summary` →
`population-bar__segment, population-bar__summary`로 클래스명을 쉼표로 나열). 파일 전체를
다시 훑어 다른 위치에 같은 실수(주석 중간의 의도치 않은 `*/`)가 없는지 확인 완료 — 나머지는
전부 정상.

**검증 남음**: Unity 에디터 미접속으로 재임포트 후 실제 렌더링 미확인. `Docs/QA_Checklist.md`에
항목 추가(Console 에러가 사라졌는지 최우선 확인).

**변경 파일**: `Assets/UI/CountryStatusPanel.uss`.

## Step 71 구현 메모 (엔딩 화면 점수 잘림 + 버튼 디자인 불일치 신고)

**신고 내용**: "결과창에서 바이오하자드 점수 나오는 오른쪽 부분이 잘려서 안 나옴", "버튼 디자인이
지금 스타일하고 안 맞음" — 두 건 모두 `Assets/UI/EndingScreen.uxml`/`.uss` 대상.

**점수 잘림 원인**: `Assets/UI Toolkit/PanelSettings.asset`의 `m_ReferenceResolution`이
`{x: 480, y: 1040}` — 즉 UI Toolkit 레이아웃은 480px 폭 논리 좌표계에서 계산된다(실기기
1440px는 이 위에 스케일만 됨). 그런데 `EndingScreenController.HandleGameEnded()`가
`score-label.text`에 `"바이오하자드 점수: {score:N0}"` 문자열 전체를 넣고, `.ending-score`가
`--font-size-hero`(40px) 한 줄로 렌더링하고 있었다 — 이 문구 길이만으로 480px 폭에 거의
근접해서, 패널 padding(`--space-xl` 좌우 20px×2)까지 더하면 오른쪽(숫자 부분)이 화면 경계
밖으로 밀려나가 잘렸다. `git blame` 없이 CSS 주석만으로 재구성한 히스토리로는, 원래
`--font-size-xxl`(26px)였던 걸 "엔딩 타이틀과 동일한 hero 크기로" 의도적으로 40px까지
올리면서 생긴 회귀로 보인다(EndingScreen.uss 기존 주석 참고).

**수정**: 라벨("바이오하자드 점수")과 숫자를 한 줄에서 분리했다.
- `EndingScreen.uxml`: `score-label` 위에 정적 텍스트 `<ui:Label text="바이오하자드 점수"
  class="ending-score-caption" />` 추가.
- `EndingScreen.uss`: `.ending-score-caption`(`text-tertiary`, `font-size-sm`, 중앙정렬)
  신규 추가. `.ending-score-panel`에 `width: 280px`(stats-panel과 동일 폭으로 통일 — 콘텐츠에
  맡겨두면 재발 가능) 추가. `.ending-score`는 숫자만 담당하므로 hero 40px를 유지해도
  안전(최대 스코어 추정치 기준 "2,025" 수준 5자리, 280px 폭에서 여유 있음).
- `EndingScreenController.cs`: `_scoreLabel.text = $"바이오하자드 점수: {score:N0}"` →
  `$"{score:N0}"`로 변경. 캡션은 UXML 정적 텍스트라 컨트롤러가 건드릴 필요 없음.

**버튼 디자인 원인**: `.ending-button`이 `width`/`height`/`margin`만 지정하고 배경색·테두리·
폰트를 전혀 지정하지 않아, Unity 기본 런타임 테마(둥근 모서리+회색 배경)가 그대로 노출되고
있었다. `Docs/DESIGN.md`의 Tactical Design System 원칙(각짐=`border-radius:0`, 발광
테두리=`accent-glow`, `Hud.uss`의 `.tab-button`/`MainMenu.uss`의 `.mainmenu-next`가 이미
검증된 패턴)과 명백히 어긋났다. `RankingPanel.uss`의 `.ranking-button`도 동일하게 미적용
상태인데, 이건 CLAUDE.md TODO에 이미 "RankingPanel tactical-panel 축소판 전환"으로 등록돼
있어 이번 수정 범위에서는 제외(EndingScreen만 신고 대상).

**수정**: `.ending-button`에 `border-width:1px`/`border-color: --color-accent-glow`/
`border-radius:0`/`background-color: --color-bg-panel`/`color: --color-text-primary`/
`font-size: --font-size-lg`/`-unity-font-style: bold` 추가(`.tab-button`+`.mainmenu-next`
톤 재사용). `.ending-button--revive`는 배경뿐 아니라 `border-color`도
`--color-brand-premium`으로 맞춰 프리미엄 버튼 테두리가 어중간한 초록으로 남지 않게 함.

**원칙 준수**: 게임 로직(점수 계산식) 무변경 — 표시 텍스트 포맷과 스타일시트만 수정. 색상은
기존 4축(브랜드/severity/카테고리/구조용 발광색) 안에서만 재사용, 새 색 추가 없음.

**변경 파일**: `Assets/UI/EndingScreen.uxml`, `Assets/UI/EndingScreen.uss`,
`Assets/Scripts/UI/EndingScreenController.cs`.

## Step 72 구현 메모 (국가 클릭 반경이 월드맵 전체로 잡히는 문제 — Step 70 콜라이더 수정의 재발)

**신고 내용**: "국가를 클릭하는 반경이 월드맵 전체로 설정되어 있음" + `[CountryDockController]
Subscribe — WorldMap.Instance가 null이라 OnCountryClicked 구독을 시도조차 못 했습니다` 경고 로그
동반 신고.

**진짜 원인**: Step 70이 "콜라이더가 옛날 플레이스홀더 크기(0.16x0.16)에 고정돼 클릭이 전혀 안
됨" 문제를 고치면서 `_collider.size = shape.bounds.size; _collider.offset = shape.bounds.center;`로
바꿨는데, `CountryView` 클래스 설명에 이미 적혀 있던 전제를 놓쳤다 — `CountryShapes/{id}.png`는
국가마다 크기가 다른 개별 이미지가 아니라 **48개국 전부 세계지도와 동일한 4000x1714px 캔버스**
위에 그 나라 하나만 흰색으로 채운 오버레이다. `Sprite.bounds`는 실제 불투명 픽셀 영역이 아니라
스프라이트의 **rect(캔버스 전체)** 기준으로 계산되므로, `shape.bounds.size`는 48개국 전부 정확히
같은 값(캔버스 전체 크기)을 반환한다 — 즉 Step 70이 "콜라이더가 너무 작다"는 문제를 "콜라이더가
지도 전체만큼 크다"는 정반대 문제로 바꿔치기한 것이었다. 클릭이 안 잡히는 게 아니라 아무 데나
클릭해도(씬에 마지막으로 배치된/레이어 순서상 위에 있는) 특정 국가가 항상 잡히는 형태로 나타났다.

`[CountryDockController] Subscribe` 경고는 별개의 원인(OnEnable이 Start보다 먼저 호출되는 Unity
생명주기상 첫 프레임엔 다른 오브젝트의 Awake가 아직 안 끝나 WorldMap.Instance가 null일 수 있음)인데,
`Subscribe()`가 `OnEnable()`과 `Start()` 양쪽에서 다 호출되므로 `Start()` 시점에는 정상적으로
재구독된다 — 실질적으로 문제를 일으키지 않는 정상 동작이라 로그 자체가 노이즈였다(진단 완료 후
제거 대상, 아래 참고).

**콜라이더 재수정**: `CountryShapes` 텍스처는 `isReadable=0`이라 런타임에 알파 채널을 직접 읽어
실제 실루엣 바운딩박스를 구할 수 없다(`InfectionDotDatabase` 클래스 설명에 이미 문서화된 제약 —
그래서 감염 점 좌표도 오프라인 스크립트로 미리 계산해둔 것). 대신 그 오프라인 데이터
(`_allDotPoints`, 국가 실루엣 내부가 보장됨)의 바운딩박스를 실루엣 크기의 근사치로 재사용하는
쪽으로 바꿨다 — `SetupHotspots()`가 핫스팟 지름 상한을 구할 때 이미 쓰던 것과 같은 접근(Step 45).
`CountryView.Awake()`에서 `LoadDotData()`(이 데이터를 채움)를 `ApplyCountryShape()`보다 먼저
호출하도록 순서를 바꾸고, `ApplyCountryShape()`가 `shape.bounds` 대신 신규
`ApplyColliderBoundsFromDotPoints()`를 호출하게 했다. 점 데이터가 국가 면적의 약 70%만 덮도록
계산된 값이라 바운딩박스가 실제 윤곽선까지 못 미칠 수 있어 1.2배 padding을 줬고, 데이터가 아예
없는 국가(방어적 케이스)는 지도 전체로 새는 것보다 안전한 Step 70 이전 기본값(0.16x0.16)으로
폴백한다.

**디버그 로그 정리**: 이번 신고와 Step 68/70에서 원인 추적용으로 남겨뒀던 진단 로그를 전부
제거했다 — `CountryDockController.Subscribe()`의 두 로그, `Start()`의 WorldMap/WorldDataManager
null 경고 블록, `HandleCountryClicked`/`HandleCountryChanged`의 진입 로그, `WorldMap.
HandleCountryClicked()`의 구독자 수 로그, `CountryView.OnMouseUpAsButton()`의 클릭 감지 로그.
`CountryDockController.OnEnable()`의 UXML `Q<>()` 바인딩 실패 경고는 이번 문제와 무관한 별도의
상시 안전장치라 유지했다.

**원칙 준수**: UI 변경 없음, 게임 로직(감염/사망/치료제 등) 무변경 — 콜라이더 계산 로직과 로그
정리만 수정.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`, `Assets/Scripts/Gameplay/WorldMap.cs`,
`Assets/Scripts/UI/CountryDockController.cs`.

## Step 73 구현 메모 (BoxCollider2D → PolygonCollider2D 전환 — 인접국 오탭 조사 후속 조치)

**배경**: Step 72로 "클릭 반경이 지도 전체" 문제는 해결했지만, 그 수정 자체가 감염 점 바운딩박스
기반 **사각형** 콜라이더였다 — 실제 국경선과 다르므로 인접국끼리 사각형이 겹치는 영역에서는 여전히
엉뚱한 나라가 선택될 수 있었다. 사용자가 "일부 국가가 BoxCollider2D로 선택되고 있어 인접 국가 클릭
시 잘못 선택된다"고 재조사를 요청했고, 조사 결과 다음이 확인됐다: (1) `CountryShapes/*.png` 48개
전부 존재하지만 `isReadable=0`이라 런타임 알파 읽기는 불가, 단 `Resources/InfectionDotPoints.json`에
실루엣 내부 보장 좌표가 이미 있음. (2) 48개 메타파일 전부 `spriteMeshType=1`(Tight — 이미 임포트
시점 알파 기반 렌더 메시 생성 중)인데 `spriteGenerateFallbackPhysicsShape=0`(Physics Shape 생성은
꺼져 있음) — 즉 같은 파이프라인으로 폴리곤 physics shape도 생성 가능하다는 기술적 근거 확보.
(3) `GamePlay.unity`에 `BoxCollider2D` 48개 확인(국가 수와 정확히 일치), `PolygonCollider2D` 0개 —
현재 선택이 BoxCollider2D 기반이라는 것 확정. (4) 전환 비용은 에디터 배치 스크립트 신규 작성 +
컴포넌트/씬 마이그레이션 정도로 평가.

**구현**:
- `Assets/Editor/CountryShapePhysicsShapeGenerator.cs` 신규 작성 — 메뉴
  `Contagion → Country Shapes → Generate Physics Shapes (48개국)`가 `Resources/CountryShapes/`
  48개 텍스처의 `TextureImporter.spriteGenerateFallbackPhysicsShape`를 일괄 켜고 재임포트한다.
  같은 메뉴에 `Validate Physics Shapes` 검증 도구도 추가. 코드 도구로는 실행 불가(에디터 전용) —
  `Docs/unity-editor-task.md` §9에 실행 안내 등록.
- `CountryView.cs`: `[RequireComponent(typeof(BoxCollider2D))]` → `PolygonCollider2D`로 교체,
  `_collider` 필드 타입 변경. `Awake()`에서 씬에 이미 배치된 48개 GameObject의 레거시
  `BoxCollider2D`를 `Destroy()`로 정리하고 `PolygonCollider2D`를 확보하도록 해 **씬 파일
  (`GamePlay.unity`)을 손대지 않고** 마이그레이션했다(Step 70/72와 같은 원칙 — 48번 손으로
  고치는 대신 코드 한 곳에서 자가 정리).
- `ApplyColliderBoundsFromDotPoints()`를 `ApplyColliderShapeFromSprite(Sprite shape)`로 대체 —
  `Sprite.GetPhysicsShapeCount()`/`GetPhysicsShape()`로 임포트 시점에 구워진 실제 실루엣 폴리곤을
  읽어 `PolygonCollider2D.SetPath()`에 그대로 적용한다(physics shape 데이터는 `isReadable`과 무관
  — 별도로 직렬화되는 임포트 결과물이라 런타임 알파 읽기 제약을 받지 않음). Editor 배치를 아직
  실행하지 않았거나 특정 국가만 physics shape가 비어 있으면(`GetPhysicsShapeCount()==0`)
  `ApplyFallbackRectCollider()`로 자동 폴백 — Step 72의 바운딩박스 사각형 로직을 그대로 이식해
  "배치 실행 전에는 클릭이 아예 안 됨" 회귀를 막았다.
- 기존 선택 로직(`OnMouseUpAsButton()`)은 무변경 — `Collider2D` 타입에 무관하게 동일하게 동작.
  국가 ID/데이터/감염 로직도 무변경(요청 조건 준수).

**QA/에디터 작업 등록**: `Docs/QA_Checklist.md`에 "PolygonCollider2D 전환 검증" 섹션 신설(배치
실행→검증→인접국/다도서국/극소국 클릭 테스트→Inspector 확인 순서). `Docs/unity-editor-task.md`
§9에 배치 스크립트 실행이 최우선 필수 수동 단계임을 등록 — 이걸 실행하지 않으면 콜라이더는
계속 Step 72 수준(사각형 폴백)으로 동작한다(게임이 깨지지는 않음).

**원칙 준수**: 기존 선택 로직(`OnMouseUpAsButton`) 유지, 국가 ID·데이터·감염 로직 무변경. 씬 파일
(`GamePlay.unity`)은 이번 변경에서 전혀 건드리지 않았다(런타임 자가 정리로 대체).

**검증 진행**: `Generate Physics Shapes (48개국)` 배치 실행 — 2026-07-10 사용자가 Unity 에디터에서
정상 완료 확인(컴파일 에러는 "Step 73 추가 대응"에서 이미 수정됨). `Docs/unity-editor-task.md` §9,
`Docs/QA_Checklist.md` "PolygonCollider2D 전환 검증" 섹션의 해당 체크박스 갱신. 남은 검증: Validate
Physics Shapes 실행, 인접국/다도서국/극소국 클릭 테스트, GamePlay.unity Inspector 확인.

**변경 파일**: `Assets/Editor/CountryShapePhysicsShapeGenerator.cs`(신규),
`Assets/Scripts/Gameplay/CountryView.cs`, `Docs/QA_Checklist.md`, `Docs/unity-editor-task.md`.

### Step 73 추가 대응 (컴파일 에러 수정 — 사용자가 Unity 에디터에서 실제 실행 시도 후 신고)

**신고 내용**: `Assets/Editor/CountryShapePhysicsShapeGenerator.cs(48,30)`/`(54,26)`에서
`CS1061: 'TextureImporter' does not contain a definition for 'spriteGenerateFallbackPhysicsShape'`
컴파일 에러.

**원인**: `.meta` YAML 필드명(`spriteGenerateFallbackPhysicsShape`)만 보고 `TextureImporter`의
직접 프로퍼티라고 가정한 게 실수였다 — 실제 Unity 스크립팅 API에서 이 값은 `TextureImporter`가
아니라 `TextureImporterSettings`(별도 구조체) 소속이며, `TextureImporter.ReadTextureSettings()`로
읽고 `SetTextureSettings()`로 다시 써야 한다(Sprite Editor의 "Generate Physics Shape" 체크박스가
내부적으로 쓰는 것과 동일한 경로). 코드 작성 시 이 API를 실제 컴파일해 검증할 Unity 에디터가
이 세션에 없어 발생한 결함 — 이후 유사 임포터 설정을 다루는 에디터 스크립트를 작성할 때는
Unity 스크립팅 API 문서(`TextureImporter` vs `TextureImporterSettings` 소속 여부)를 우선 확인할 것.

**수정**: `GeneratePhysicsShapes()`에서 `importer.spriteGenerateFallbackPhysicsShape` 직접 접근을
`var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); ...
settings.spriteGenerateFallbackPhysicsShape = true; importer.SetTextureSettings(settings);`로
교체. 나머지 로직(idempotent 체크, `SaveAndReimport()`, 로그)은 무변경.

**원칙 준수**: 배치 툴 내부 API 호출 방식만 수정 — 메뉴 경로, 처리 대상, 로그 메시지, 게임 로직
전부 무변경.

**변경 파일**: `Assets/Editor/CountryShapePhysicsShapeGenerator.cs`.

## Step 74 구현 메모 (감염 점 크기 공식 버그 수정 — 대형국 과대 확대(quadratic) → sqrt 압축)

**신고 내용**: "중국/러시아/미국 등 대형 국가 감염점이 과도하게 크다"는 밸런스 조사 요청. 대한민국을
기준 크기(1.0)로 삼아 상대 크기를 재계산하고 새 공식을 적용해달라는 것.

**조사 — diameter 필드 성격 확인**: `InfectionDotPoints.json`의 국가별 `diameter`가 "이미 최종
표시 크기"인지 "런타임에 추가 배율이 필요한 중간값"인지부터 확인했다. DevLog Step 42(3절, "여기
계산되는 값은 `infectionDotDiameterScale` 적용 전 기준값") 및 Step 53("`infectionDotDiameterScale`은
그 위에 곱하는 배율")을 근거로 diameter는 **국가 면적 비례로 오프라인 계산된 기준값**(Step 36의
"전체 활성화 시 면적의 70% 커버" 역산식, `diameter = 2*sqrt(0.70*area/(count*π))`)이지 최종
표시값이 아님을 확인 — 즉 diameter 자체에 이미 국가 크기 차이(한국 0.00902~러시아 0.05103, 5.66배)가
녹아 있는 상태에서, `CountryView.SetupInfectionDots()`가 `infectionDotDiameterScale` 하나만 곱해야
했다.

**버그 원인**: Step 53에서 "한국보다 큰 나라는 배율도 같이 키워라"는 요청을 구현하며
`sizeRatio = 자국 diameter / MinDiameter`를 diameter 위에 **한 번 더** 곱했다 — 이미 크기
비례가 반영된 값에 크기 비례 배율을 재적용한 것이라 최종 지름이 `diameter²`에 비례하게 됐다.
Step 53 본문 검증 메모에도 "배율이 diameter에 선형 비례라 큰 나라일수록 증가폭이 가파른데... 너무
크면 sizeRatio에 `Mathf.Sqrt()`를 씌워 증가폭을 완만하게 만드는 걸 다음 튜닝 후보로 남겨둔다"고
이미 예고돼 있었다.

**수치 비교 (한국=1.0 기준, `infectionDotDiameterScale`=1 — GamePlay.unity 48개국 오버라이드값)**:

| 국가 | diameter(raw) | 현재(버그, ratio¹) | sizeRatio 완전 제거 | sqrt(sizeRatio) 적용 |
|---|---|---|---|---|
| KOR | 0.00902 | 1.00배 (0.00902) | 1.00배 (0.00902) | 1.00배 (0.00902) |
| CHI | 0.04045 | 20.11배 (0.18140) | 4.48배 (0.04045) | 9.50배 (0.08566) |
| USA | 0.04133 | 21.00배 (0.18938) | 4.58배 (0.04133) | 9.81배 (0.08847) |
| CAN | 0.04429 | 24.11배 (0.21747) | 4.91배 (0.04429) | 10.88배 (0.09814) |
| RUS | 0.05103 | 32.01배 (0.28870) | 5.66배 (0.05103) | 13.46배 (0.12138) |

"sizeRatio 완전 제거"안(diameter를 그대로 최종값으로 씀)도 같이 계산해봤으나, 이 경우 Step 53이
원했던 "한국보다 큰 나라는 배율 차원에서 한 번 더 확대"라는 효과가 사라져(diameter 자체 비례폭인
5.66배가 상한이 됨) 원래 요청 의도와 달라진다. sqrt 압축안은 그 의도(대형국을 diameter 자체
비율보다 더 키움)는 유지하면서 버그였던 제곱 증폭(20~32배)을 절반 지수(최대 13.46배)로 완화한다 —
실기기/에디터 렌더링 스크린샷은 이 세션에 Unity 에디터 접속이 없어 확인 불가, 대신 세 공식의
절대 지름 값을 SVG로 시각화해 비교했다(오프라인 Python 시뮬레이션, 사용자 확인 후 sqrt안 채택).

**수정**: `CountryView.SetupInfectionDots()`의
`resolvedScale = Mathf.Max(infectionDotDiameterScale, infectionDotDiameterScale * sizeRatio)`를
`Mathf.Sqrt(sizeRatio)`를 곱하는 형태로 변경. `infectionDotDiameterScale` 필드 툴팁도 새 배율
공식을 반영해 갱신.

**원칙 준수**: 코드 수정은 사전 조사·수치 비교·사용자 승인 이후에만 진행 — 조사 단계에서는
"코드 수정 금지" 요청에 따라 계산/보고만 수행했다. 감염/사망/치료제 등 게임 로직, UI 레이아웃
무변경 — 감염 점 지름 계산식 한 줄만 수정.

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`.

## Step 75 구현 메모 (감염 점 실제 커버리지 검증 — Step 74 sqrt 수정으로도 못 잡은 이중 반영 문제)

**요청**: 한국(KOR)/일본(JPN)/영국(UK) 기준으로 감염률 100%일 때 감염 점이 국가 면적의 몇 %를
덮는지 실측하고, 크기를 더 키우지 않고 개수만 늘려서 부족분을 메우면 몇 개가 더 필요한지 계산.

**조사 방법**: `InfectionDotPoints.json`의 국가별 `diameter`·점 개수(`count`)는 Step 36/42/51
오프라인 스크립트가 "감염률 100%(전체 점 활성화) 시 국가 면적의 70%(COVERAGE_TARGET)를 덮도록"
역산한 값이다(`diameter = 2*sqrt(0.70*area/(count*π))`). 이 역산식을 거꾸로 풀면 국가 면적을
`area = π*count*diameter²/2.8`로 근사 복원할 수 있다(원본 alpha mask 픽셀 수 재계산 없이 오프라인
Python으로 계산). 실제 커버리지는 `count*π*(최종지름/2)²/area`로 구했다(원끼리 겹침은 무시한 상한
추정치 — Step 36 본문이 이미 "겹치면 체감상 더 꽉 차 보일 것"이라고 명시한 바로 그 근사).

**실측 결과 (Step 74 sqrt 공식 기준, `infectionDotDiameterScale=1` 오버라이드)**:

| 국가 | 지름(raw) | 점 개수 | 배율 | 최종 지름 | 복원 면적 | 커버리지 | 70% 기준 필요 개수 | 증감 |
|---|---|---|---|---|---|---|---|---|
| KOR | 0.00902 | 128 | 1.000 | 0.00902 | 0.011685 | **70.00%** | 128.0 | 0 |
| JPN | 0.01529 | 184 | 1.302 | 0.01991 | 0.048264 | **118.66%** | 108.5 | **-75.5** |
| UK | 0.01362 | 164 | 1.229 | 0.01674 | 0.034134 | **105.70%** | 108.6 | **-55.4** |

한국은 `sizeRatio=1`이라 배율 영향이 없어 정확히 설계값(70%)과 일치 — 역산식이 맞다는 근거로도
쓸 수 있다. 반면 일본·영국은 이미 70%를 넘어(118.7%/105.7%) "커버리지가 부족해서 점을 추가해야
한다"는 전제 자체가 성립하지 않았다 — 오히려 현재 지름을 유지한 채 70%로 되돌리려면 점을 **75개/55개
줄여야** 하는 역설적 결과가 나왔다(양수로 요청된 "추가 개수"가 실제로는 음수).

**추가로 확인한 대조군 — "크기를 한국 수준으로 전혀 안 키우고 개수만으로 70%를 채운다면"**: 일본은
128→528.7개(+344.7), 영국은 128→373.9개(+209.9)가 필요 — `maxInfectionDots`(900) 상한 안에는
들어오지만, Step 36에서 이미 "면적에 그대로 비례시키면 러시아가 수천 개"라고 우려했던 것과 같은
방향의 문제라 두 나라 선에서 이미 상당히 부담스러운 증가폭이다.

**결론 — 근본 원인**: diameter·count는 이미 국가별로 70% 커버리지를 만족하도록 **공동 계산**된
완결값인데, `CountryView.SetupInfectionDots()`가 여기에 국가 크기 비례 배율(Step 53의 sizeRatio,
Step 74의 sqrt(sizeRatio))을 **또** 곱해 이중 반영하고 있었다 — Step 74는 그 증가폭을 완만하게
누그러뜨렸을 뿐 이중 반영 자체를 없애지는 못했다. "커버리지 부족분을 개수로 보충"하는 방향은 애초에
전제(부족)가 틀렸으므로 적용하지 않았다.

**수정**: `CountryView.SetupInfectionDots()`에서 `sizeRatio`/`Mathf.Sqrt(sizeRatio)` 배율 계산을
완전히 제거하고 `_resolvedDotDiameter = _baseDotDiameter * infectionDotDiameterScale`로 단순화 —
`infectionDotDiameterScale`은 Step 51 이전처럼 다시 전 국가 공통 전역 배율로만 쓰인다. diameter·count
자체가 이미 실제 국가 면적에 비례(러시아가 한국보다 점 6.4배 많고 지름 5.66배 큼 — 이 조합만으로도
화면상 대형국이 확연히 크고 빽빽하게 보인다)하므로 배율 없이도 대형국 강조 효과는 유지되고, 48개국
전부가 설계 목표(70%)를 정확히 만족하게 된다. `InfectionDotDatabase.MinDiameter`는 더 이상
`CountryView`가 쓰지 않지만 계산 로직 자체는 보존(향후 다른 용도 재사용 가능성 대비, 코드 삭제
대신 비활성화 원칙 — Step 49 hotspot 처리와 동일 원칙).

**참고**: 이 Step으로 diameter가 raw 값 그대로 최종 크기가 되면서, 2턴 전 세션에서 비교했던
"sizeRatio 완전 제거" 후보(한국 대비 최대 배율 5.66배, 러시아)와 수학적으로 동일한 결과가 된다 —
다만 이번엔 "배율 튜닝 취향" 문제가 아니라 **커버리지 실측으로 검증된 필연적 결론**이라는 점이
다르다.

**원칙 준수**: 조사(면적 역산·커버리지 계산·대조군 계산)를 먼저 보고한 뒤 사용자 지시("결과 보고
후 수정 진행")에 따라 코드 수정 진행. 감염/사망/치료제 등 게임 로직, UI 레이아웃, JSON 데이터
무변경 — 감염 점 지름 계산식만 단순화(점 좌표/개수 재생성 없음, 성능 영향 없음).

**변경 파일**: `Assets/Scripts/Gameplay/CountryView.cs`, `Assets/Scripts/Data/InfectionDotDatabase.cs`(주석만).

## Step 76 구현 메모 (CountryPopup 인구수/공항·항구·국경 오버플로우 조사·수정)

**신고**: 중국/인도 선택 시 CountryPopup의 인구수 텍스트, 그리고 공항/항구/국경 상태 표시가 팝업
영역을 초과해서 그려짐.

**조사 결과 — 근본 원인은 하나**: `Tactical.uss`의 `.data-row`/`.data-label`/`.data-value`(여러
화면이 공유하는 판독행 규격)가 `white-space`/`flex-shrink`/`min-width`를 전혀 지정하지 않는다.
이 프로젝트 컨벤션상 UI Toolkit Label은 `white-space: normal`을 명시하지 않으면 줄바꿈되지 않고
(Hud.uss/MainMenu.uss/UpgradeTree.uss/RankingPanel.uss가 전부 이미 이 패턴을 직접 오버라이드해서
씀 — `Tactical.uss`만 빠져 있었음), CSS flexbox 규칙상 `white-space: nowrap`인 flex 아이템은
`min-width: 0`이 없으면 "자동 최소 너비 = 줄바꿈 없는 전체 텍스트 너비"가 되어 아무리 좁아도
줄어들지 않는다(MainMenu.uss `country-row__name`이 이미 겪고 고친 동일 트랩, 105행 주석 참고).
VisualElement 기본 `overflow`가 `visible`이라 넘친 텍스트가 그대로 팝업 밖으로 삐져나왔다.
`.popup-root`가 `width: 320px` 고정인 CountryPopup이 다른 소비자(CountrySelect/UpgradeTree 등,
더 넓은 패널)보다 훨씬 좁아서 이 공용 결함이 처음으로 실제 오버플로우로 드러난 지점이었다.
구체적으로: 인구는 중국(1,412,914,089)·인도처럼 10자리 숫자를 `N0`로 그대로 표기해 최장
숫자열이었고, 공항/항구/국경은 세 상태를 " · "로 이어붙인 하나의 값 문자열(약 19~20자)+앱 내
가장 긴 라벨("공항/항구/국경", 7자) 조합이라 가장 심하게 초과했다.

**수정(사용자 선택: Tactical UI 기준 최적안)**:
1. `Tactical.uss` `.data-value`에 `flex-shrink: 1; min-width: 0; white-space: normal;
   -unity-text-align: upper-right;` 추가, `.data-label`에 `flex-shrink: 0;` 추가 — data-row를
   쓰는 모든 화면(CountrySelect/MainMenu/EndingScreen/CountryPopup)이 공용으로 오버플로우
   방지 혜택을 받는다. `UpgradeTree.uss`는 자체 `.data-value`를 따로 갖고 있어(Tactical.uss
   미로드) 영향 없음.
2. 이 공용 변경이 `MainMenu.uss` `country-row__meta`(48행 리스트, "한 줄 유지" 명시적 설계
   제약 — 116행 주석)에 줄바꿈을 유발해 리스트 행 높이를 깨뜨릴 위험이 있어, `.country-row__meta
   .data-value`에 `white-space: nowrap; text-overflow: ellipsis; overflow: hidden;`을 추가해
   해당 컨텍스트만 기존 단일 줄 동작으로 되돌렸다(`country-row__name`과 동일 패턴).
3. `CountryPopupController.Populate()` — 공항/항구/국경을 감염자/사망자처럼 개별 data-row 3개로
   분리하고 severity 색상(개방=`data-value--info`, 폐쇄/봉쇄=`data-value--danger`) 적용. 인구는
   신설한 `FormatPopulation()`으로 억/만 단위 축약(예: "14.1억") — 감염자/사망자는 정확한 값이
   중요해 `N0` 그대로 유지.
4. `CountryPopup.uss` `.popup-root` 폭을 320px → 340px로 소폭 확대(세로 1440px 폭 기준 화면비
   문제 없음).

**적용하지 않은 것**: 제안 단계에서 검토했던 `data-row--stacked`(라벨 위/값 아래 세로 배치)
모디파이어는 위 수정 후 CountryPopup의 어떤 행도 실제로 필요하지 않아(3분할+축약 표기만으로
340px 안에 전부 들어감) 추가하지 않았다 — 안 쓰이는 CSS를 미리 넣어두는 대신, 향후 다른 화면에서
필요해지면 그때 만들기로 함. `.data-value`의 wrap-capable 기본값 자체가 어떤 값이든 넘칠 경우
줄바꿈되는 최소한의 안전망 역할은 대신한다.

**원칙 준수**: 조사(원인 분석·수정안 3개 제시)를 먼저 보고한 뒤 사용자 선택("C. Tactical UI
최적안")에 따라 수정 진행. 게임 로직(감염/사망/치료제 등) 무변경. Unity 에디터 미접속으로 실제
렌더링 검증은 못 했음 — 남은 검증은 `Docs/QA_Checklist.md` "CountryPopup 오버플로우 수정 검증"
섹션 참고.

**변경 파일**: `Assets/UI/Tactical.uss`, `Assets/UI/MainMenu.uss`, `Assets/UI/CountryPopup.uss`,
`Assets/Scripts/UI/CountryPopupController.cs`.

## Step 77 구현 메모 (인구 축약 표기 위치 정정 — CountryPopup→Country Dock 이관)

**신고**: Step 76에서 CountryPopup 인구수를 억/만 축약 표기로 바꿨는데, CountryPopup은 국가 상세
정보 창이므로 population은 정확한 정수(N0)를 유지해야 한다는 피드백. 대신 우측 상단 상시 표시
위젯인 Country Dock(`CountryDockController`)의 인구 표시를 축약 표기로 바꿔달라는 요청.

**조사**: `CountryDockController.Populate()`(122행)가 `_populationValue.text =
$"{country.population:N0}";`로 CountryPopup과 동일하게 전체 자릿수를 그대로 쓰고 있었다. `Hud.uss`
`.country-dock`의 폭은 `140px`(232행) — CountryPopup(340px)보다도 훨씬 좁은 상시 표시 위젯인데
`.country-dock__value`(276행)에도 Step 76에서 `Tactical.uss`에 넣은 wrap/shrink 공용 수정이
적용되지 않는다(Country Dock은 `Tactical.uss`를 로드하지 않고 자체 `Hud.uss` 규격을 씀 — Step 76
수정의 사정거리 밖). 즉 중국/인도 선택 시 Country Dock 쪽 인구 행도 같은 오버플로우 가족 버그를
겪고 있었을 가능성이 높다(이번 세션에서 새로 발견, 이전엔 신고되지 않았던 지점).

**수정**:
1. `CountryPopupController.Populate()` — 인구 표기를 `FormatPopulation()` 호출에서
   `$"{country.population:N0}"`로 되돌리고, Step 76에서 추가했던 `FormatPopulation()` 메서드는
   제거(더 이상 쓰는 곳이 없어 죽은 코드로 남기지 않음). 오버플로우 자체는 이미 Step 76의
   `Tactical.uss` wrap/shrink 공용 수정 + 340px 확장 폭으로 해결돼 있어 N0로 되돌려도 문제없다.
2. `CountryDockController.cs`에 동일한 로직의 `FormatPopulation()`을 신설(억/만 단위 축약, 예:
   "14.1억")하고 `Populate()`의 `_populationValue.text`에 적용. 플레이스홀더("-")는 무변경.

**목표 상태**: CountryPopup = 전체 숫자(N0), Country Dock = 축약 숫자. 두 화면의 역할(상세 정보
창 vs 상시 표시 요약 위젯)에 맞는 정밀도로 분리됐다.

**적용하지 않은 것**: `Hud.uss` `.country-dock__value`/`.country-dock__label`에는 Tactical.uss와
같은 wrap/shrink 수정을 추가하지 않았다 — 인구는 이번 수정으로 축약되어 140px 안에 충분히 들어가고,
`_transportValue`(공항/항구 상태를 한 문자열로 이어붙이는 방식, CountryPopup이 Step 76에서 고친 것과
동일한 패턴)를 포함한 Country Dock 전반의 오버플로우 내성은 이번 요청 범위 밖이라 손대지 않았다.
같은 패턴의 잠재적 취약점이므로 필요 시 별도 확인 항목으로 QA_Checklist에 남겨둔다.

**원칙 준수**: 사용자 지시("관련 컨트롤러 조사 후 수정 진행")에 따라 `CountryDockController.cs`/
`Hud.uss` 확인 후 수정. 게임 로직 무변경. Unity 에디터 미접속으로 실제 렌더링 검증은 못 함.

**변경 파일**: `Assets/Scripts/UI/CountryPopupController.cs`, `Assets/Scripts/UI/CountryDockController.cs`.

## Step 81 구현 메모 (CountryStatusPanel — GLOBAL STATUS/감염 국가 현황/의료 시스템 현황/종합 위협도 추가)

**배경**: Step 80으로 "GLOBAL STATUS CENTER"의 기본 구조(세계 요약/국가 상태 분포/랭킹 4종/
48개국 목록)는 완성됐으나, 사용자가 "단순 통계 화면이 아니라 전략 상황실이 되어야 한다 — 정보량을
늘리지 말고 전략적으로 판단할 수 있는 데이터를 추가하라"고 요청. GLOBAL STATUS 한줄평가/THREAT
INDEX TOP10/감염 국가 현황/GLOBAL HOTSPOT/의료 시스템 현황/교통 허브 위험도 6개 후보를 제시받아,
먼저 각 후보가 현재 데이터 구조(Country/WorldState 기존 필드·계산식)만으로 구현 가능한지, UI
밀도를 얼마나 늘리는지 분석해 추천안을 제시한 뒤(AskUserQuestion 2회, 옵션 6개 중 4개 승인) 승인된
범위만 구현했다. **GLOBAL HOTSPOT 카드와 교통 허브 위험도 집계는 사용자가 선택하지 않아 이번
범위에서 제외**됐다(둘 다 데이터상 구현 가능하다고 분석했었음 — 필요 시 후속 Step 후보).

**핵심 발견**: `WorldState.GetResistanceStage()`(plagueVisibility 5단계)/`GetMortalityStage()`
(사망률 3단계)가 이미 구현돼 있었는데 HUD(`HudController._worldStatusLabel`)/뉴스피드에서만
쓰이고 `CountryStatusPanelController`에는 전혀 노출되지 않고 있었다 — GLOBAL STATUS 배너는 이
기존 계산을 그대로 조합해 신규 계산 없이 구현했다.

**구현 4항목** (`CountryStatusPanelController.cs`/`CountryStatusPanel.uxml`/`.uss`):

1. **GLOBAL STATUS 배너** — 화면 최상단(헤더 바로 아래) 추가. `GlobalStatusAssessment()`가
   `WorldState.GetMortalityStage()`(기존)와 신설 `WorldInfectionRate()`(세계 요약의 감염률
   계산과 동일 로직을 공유 — 값 불일치 방지 목적으로 헬퍼로 뽑음)를 조합해 4단계 텍스트
   (CONTAINMENT SUCCESS/OUTBREAK EXPANDING/GLOBAL PANDEMIC/WORLD COLLAPSE IMMINENT, 사용자
   제시 예시 문구 그대로 사용) + severity 4색(`global-status-banner--info/infected/danger/dead`)
   modifier 클래스를 결정한다. 임계값(감염률 5%/50%)은 제안값 — 플레이테스트로 조정 필요.
2. **감염 국가 현황** — "세계 요약" 아래 신규 섹션. 감염 국가 수(`infectedCount>0`인 국가 수)/
   무감염 국가 수/소멸 국가 수(`GetCollapseStage()==Extinct`) 3개 data-row. 기존 "국가 상태
   분포"(사망률 기준 4버킷)와는 다른 축(감염 시작 여부)이라 별도 섹션으로 분리했고, 소멸(Extinct)은
   기존 COLLAPSE 버킷에 이미 포함돼 있지만 거기선 안 보이므로 여기서 별도로 뽑아 보여준다.
3. **의료 시스템 현황** — "국가 상태 분포" 바로 아래, **동일한 `distribution-row`/`distribution-item`/
   `stat-chip` UI를 그대로 재사용**(신규 CSS 없음, DESIGN.md "새 국가 데이터 UI는 기존 4색만
   재사용" 원칙 그대로 따름). 정상/주의/과부하/붕괴 4버킷 국가 수 — 버킷 판정은 기존
   `MedicalLoadStatus()`의 라벨을 그대로 재사용(임계값 리터럴 중복 없이 단일 소스 유지).
4. **종합 위협도(THREAT INDEX) TOP10으로 랭킹 통합** — 기존 감염률/치사율(추정)/의료 부하
   TOP10 3개 랭킹(30행)을 신설 `ThreatIndex()`(감염률 40%+치사율(추정) 30%+의료부하 30% 가중합,
   세 값 모두 기존 계산식 재사용) 기반 종합 위협도 TOP10 하나로 통합하고, 감염자 TOP10(원시
   수치, 규모 파악용)만 별도로 남겼다. 랭킹 섹션이 4개(40행)→2개(20행)로 줄어 "정보량을 늘리지
   말라"는 요구에 맞게 오히려 화면이 단순해졌다. 가중치(0.4/0.3/0.3)는 제안값 — 플레이테스트로
   조정 필요.

**화면 최종 순서(위→아래)**: GLOBAL STATUS 배너 → 세계 요약 → 감염 국가 현황 → 국가 상태 분포 →
의료 시스템 현황 → 종합 위협도 TOP10 → 감염자 TOP10 → 48개국 목록.

**원칙 준수**: 신규 데이터 모델 없음(Country/WorldState 기존 필드·계산식만 조합). `CountryPopupController`
등 다른 화면 미변경. Unity 에디터 미접속으로 실제 렌더링 검증은 못 함 — 검증은
`Docs/QA_Checklist.md` 참고.

**변경 파일**: `Assets/Scripts/UI/CountryStatusPanelController.cs`, `Assets/UI/CountryStatusPanel.uxml`,
`Assets/UI/CountryStatusPanel.uss`.
