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
