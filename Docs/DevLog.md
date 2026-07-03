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
