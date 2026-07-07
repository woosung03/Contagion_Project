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
- **타겟 화면**: 세로(Portrait) 고정, 갤럭시 S25 울트라 기준(1440×3120, 19.5:9). `PanelSettings.referenceResolution`/
  `ProjectSettings.defaultScreenOrientation`/`GamePlay.unity`의 Main Camera가 전부 이 비율 기준으로 맞춰져 있음.
  새 UI 화면/레이아웃 작업 시 가로 모드는 고려하지 않아도 됨(회전 잠김).
- 국가 48개(`Assets/New Folder/CountryDatabase.asset`), 병원체 6종, 업그레이드 트리 45노드(`DefaultUpgradeTreeFactory.cs`).
  인구는 Step 34부터 **실제 인구 수 그대로**(스케일 없음) — 이전엔 실제 인구/1000이었음.

---

## 완료된 주요 시스템 (Step 1~43)

각 시스템의 상세 구현 배경·설계 결정 이유·버그 진단 과정은 `Docs/DevLog.md`에서 Step 번호로 검색.

- [x] 핵심 데이터/시뮬레이션 — `Pathogen`/`Country`/`WorldState`(Data), 틱 기반 감염·사망·치료제 진행 (`SimulationManager`), `WorldDataManager`, `GameManager`(페이즈/난이도/일시정지)
- [x] 세계 지도 — `WorldMap`+`CountryView`(국가별 색상/클릭/실제 실루엣), DNA 버블 스포너+오브젝트 풀링(`BubbleSpawner`, `ObjectPool<T>`)
- [x] 게임플레이 시스템 — `UpgradeManager`(DNA 트리), 인류 저항 AI(`HumanResistanceManager`), `EventManager`(뉴스피드 이벤트 6종 + 나무위키 백로그 반영: 붕괴단계/난이도 확산보정/처형·폭격 이벤트/자금 상한선/국경 폐쇄 순차화/플레이버 이벤트)
- [x] UI Toolkit 전체 — HUD(스파크라인 그래프 포함)/업그레이드 트리(좌표+연결선 시각화)/국가 상태 패널/뉴스피드/엔딩/랭킹 패널
- [x] 데이터화 — ScriptableObject(`CountryDatabase`/`PathogenDefinition`/`UpgradeTreeDatabase`) + `GameDataBootstrapper`가 씬 시작 시 주입
- [x] 플랫폼 연동 — 앱인토스 보상형 광고(`GameAds`), 랭킹(게임센터 리더보드로 대체), 저장 시스템(로컬 폴백 + AIT Storage 훅)
- [x] 화면 플로우 — `MainMenu`(병원체 선택)/`CountrySelect`(발원 국가 선택)/`GamePlay` + 재시작 루프 안정화(DontDestroyOnLoad 매니저 전체 `ResetForNewGame()`)
- [x] 모바일 타겟팅 — 세로 화면 고정, SafeArea 적용, 국가 지리적 재배치(경도/위도 기반) + 좌우 드래그 스크롤
- [x] 모바일 UI 가시성 폴리싱(Step 25) — PanelSettings 참조 해상도 정정, 세계 지도 화면비 버그 수정, SafeAreaApplier 7개 화면 적용, 폰트 크기 상향
- [x] UI 세부 폴리싱(Step 26) — 뉴스피드 영역 확대 + 업그레이드 트리 3분할 창을 버튼+화살표 페이징으로 통합, 노드 크기/간격 축소
- [x] 세계 지도 재배치(Step 27) — 국가 위치를 경도/위도 기반 그리드로 재배치 + 국가 크기 면적 비례화
- [x] 세계 지도 UX 개선(Step 28/28-2/28-3) — 지도를 화면보다 넓게 배치 + 좌우 드래그 스크롤, 국가 클릭 팝업 제거 → "국가현황" 스크롤 리스트 패널로 대체
- [x] 글로벌 교통망 구축(Step 29~30-3) — 항공/해운 허브 30개(각 15개) 신규 구현, 유닛이 지도 위를 이동하며 도착 시 감염 전파(기존 추상 확률 롤 대체), 국가 18→48개/업그레이드 트리 27→45노드 확장, 해운 경유점을 `world_base.png` 육지 마스크 기반 A* 길찾기로 계산. Step 30-3에서 경유점 키 순서 버그 10쌍 수정 + 배/비행기 스프라이트 실루엣·색상 분리(십자형/파랑 vs 납작한 선체/초록)
- [x] 교통망 미세조정(Step 30-4) — SEA_JEA(제벨알리) 허브 오프셋이 요르단/이스라엘 인접 내륙에 찍혀 있던 버그 발견·정정(페르시아만 연안) + 관련 항로 3개 재검증
- [x] 교통망 재검증+유닛 감소(Step 30-5) — 감염 보유(빨간) 유닛 가시성을 위해 활성 유닛 수(24~180→10~70)/스프라이트 크기(pixelsPerUnit 70→150) 축소 + 26개 해운 항로 전수 재검증(허브별 "가장 가까운 진짜 바다 지점" sea anchor 도입) — 로스앤젤레스항(SEA_LAX)이 메인 대양과 단절된 웅덩이에 있던 버그 발견·수정 포함. 사용자가 실제 플레이로 최종 확인 완료("문제 없는 것 같아")
- [x] UI 디자인 시스템 도입(Step 31) — `C:\Game\codebase` 위키(uss-variables/uss-tss) 패턴 참조해 공통 `Theme.uss`(:root 커스텀 프로퍼티) 신규 작성, 7개 화면에 흩어져 있던 색상·간격·폰트크기·모서리반경 하드코딩을 var(--토큰)으로 통일
- [x] 항공 허브 절대좌표화(Step 32) — "비행기 경로 출발/도착 지점이 공항이여야 하는데 어긋난다" 지적 반영, 48개국 앵커 대 실제 위경도 최소제곱 회귀로 좌표 변환식을 구해 15개 공항을 국가 앵커 상대 오프셋 대신 실제 위경도 기반 절대 좌표로 교체
- [x] 교통망 실플레이 피드백 반영(Step 33) — HND 좌표 30px 보정(육지 위로), 유닛 수 5~35/스폰 최대 3으로 재하향, carrier 판정을 boolean→감염 비율 비례 확률(`carrierChanceScale`)로 변경
- [x] 인구 스케일 변경(Step 34) — "실제 인구/1000" → "실제 인구 수 그대로"로 변경, Worldometer(UN 2026 전망) 기준 48개국 population 전수 갱신. population 절대값 비례 공식(`cureStartChancePerInfected`/`PerDeath`, `whoMeetingMinDeathCount`)만 1000배 재조정
- [x] 국가별 감염 점(dot) 오버레이 도입(Step 35) — "감염률 색상 얼룩만으로는 시각 피드백이 약하다" 지적 반영, 국가 실루엣 알파마스크에서 오프라인 Python(farthest-point sampling)으로 국가당 점 24개 좌표를 미리 뽑아 `Assets/Resources/InfectionDotPoints.json`으로 저장, `CountryView`가 감염 비율에 비례해 점을 하나씩 활성화(최대 16개)
- [x] 감염 점 오버레이 고도화(Step 36) — farthest-point sampling(규칙적으로 보임) → 주요 도시 좌표 주변 가우시안 지터 클러스터 + rural 배경점 방식으로 교체, 점 개수(14~90)/지름을 국가 면적(알파 픽셀 수) 기반으로 국가별 자동 산출(전체 활성화 시 면적의 약 70% 커버)
- [x] 교통 유닛 그래픽 이모지화(Step 37~40) — 비행기/배를 절차적 도형에서 실제 이모지(Twemoji ✈️/🚢, 이후 사용자 제공 고해상도 배 이미지 "Ship 1.png"로 교체)로 전환, carrier/idle 표시를 halo→색상 윤곽선 방식으로 변경(스프라이트 아웃라인 기법), `TransportIcons/` 리소스 손상·누락 복구 및 스프라이트 슬라이스 재정의, `iconScale`을 TransportManager 인스펙터로 이전. **주의**: 그래픽 출처가 Twemoji(CC-BY 4.0)라 배포 전 저작자 표시 검토 필요
- [x] 감염 점 크기/개수 1차 조정(Step 41) — 지름 배율 1→0.5, 상한 16→32(씬 48개 인스턴스 직렬화 값도 동일 반영, 스크립트 기본값이 무시되고 있던 문제 확인·수정)
- [x] DNA 버블 스폰 버그 수정 + 감염 점 크기/개수 2차 조정(Step 42) — 국가 크기와 무관한 고정 반경(0.5유닛)으로 DNA 버블을 흩뿌려 작은 나라에서 국경 밖으로 벗어나던 버그를, 국가 실루엣 내부가 보장된 감염 점 좌표를 재사용하는 방식(`GetRandomDnaSpawnWorldPosition`/`DnaSpawnScatterRadius`)으로 수정. 감염 점 지름 배율 0.5→0.35, 개수는 한국 32개 기준으로 나머지 47개국 면적 비례 재산출(러시아 최대 206개)
- [x] 감염 점 개수 3차 조정(Step 43) — "빈 공간이 많음" 피드백으로 개수를 한 번 더 2배(한국 64개, 러시아 412개, 총합 6380개)로 재생성, 상한 220→450
- [x] 감염 핫스팟 오버레이 도입~폐지(Step 44~47, 49) — 국가당 개별 점(최대 412개, 합계 6380개) 방식을 소수의 큰 원형 "핫스팟"(최대 10개, 합계 약 348개)으로 재설계(44), 국가/이웃나라 침범 방지 상한 추가(45), 레이어 순서 재정비(46), 방사형 그라디언트 텍스처로 재차 개선(47)까지 네 차례 튜닝했지만, "레이어로 나눠서 구분하는 게 너무 힘들다"는 피드백으로 Step 49에서 `hotspotsEnabled` 토글(기본 false)을 추가해 오버레이 자체를 껐다(코드는 삭제 없이 보존). **Step 50에서 재확인**: 원했던 건 색상 얼룩만 남기는 게 아니라 핫스팟(큰 원) 없이 Step 41~43의 "개별 점" 방식으로 돌아가는 것이었음 — 아래 Step 50 참고.
- [x] 교통 레이어 대비 문제 진단·수정(Step 48) — "배/비행기/노선이 감염 점보다 아래 레이어에 있는 것 같다" 신고를 받고 sortingOrder를 전수 재검토했으나 코드상으로는 이미 정상(교통이 핫스팟보다 위)이었음 — 실제 원인은 색상 대비: 노선 알파 0.18→0.55(+두께 0.02→0.032), 감염 배(`SeaCarrierColor`)가 핫스팟 빨강과 사실상 같은 색이던 것을 마젠타(1,0.15,0.75,1)로 교체. 핫스팟이 Step 49에서 기본 꺼지면서 이 대비 수정의 원래 용도(핫스팟과의 구분)는 당장 안 쓰이지만, 노선/carrier 색상 개선 자체는 유효해 되돌리지 않음
- [x] 핫스팟 오버레이 끄기(Step 49) — "레이어로 나눠서 구분하는 거 너무 힘든데 이전 버전으로 돌아갈까" 요청으로 `CountryView.hotspotsEnabled`(기본 false) 토글을 추가해 `SetupHotspots()`/`UpdateHotspots()` 호출을 조건부로 만듦(코드는 삭제 없이 보존, true로 되돌리면 즉시 복원 가능). 감염 점 좌표/지름 로딩(`LoadDotData()`)은 DNA 버블 스폰이 재사용하므로 이 토글과 무관하게 항상 실행되도록 분리
- [x] 개별 감염 점 방식 복원(Step 50) — Step 49 직후 "감염 점 안 보이는데" 재신고 확인 결과, 원했던 건 색상 얼룩만 남기는 게 아니라 핫스팟(큰 원) 없이 Step 41~43 시절 "국가당 여러 개의 작은 점" 방식이었음. `SetupInfectionDots()`/`UpdateInfectionDots()`를 복원하고 `dotsEnabled`(기본 true) 토글 추가
- [x] 감염 점 중앙집중화 제안 검토 → 보류, 크기/개수 2배 대체 적용(Step 51) — "상위 프리팹으로 중앙 조정 + 면적 비례 확대" 제안을 받았으나 국가별 면적 비례는 이미 구현돼 있었고, 진짜 중앙집중식 구조 변경은 에디터 미접속 리스크가 있어 사용자가 제시한 대안(크기 2배, 개수 2배)을 그대로 적용. `infectionDotDiameterScale` 0.35→0.7, `InfectionDotPoints.json` 점 개수 6380→12760개, `maxInfectionDots` 450→900

---

## 최근 작업 (Step 52~56)

| Step | 내용 | 파일 |
|------|------|------|
| 52 | "감염 점 하이라이트 있는데 이거 뭐임" 질문 — AskUserQuestion으로 확인해보니 Step 47에서 핫스팟용으로 도입한 SmoothStep 방사형 그라디언트 스프라이트(반지름 전체에 걸쳐 페이드)를 Step 50에서 개별 점에도 그대로 재사용하면서 "또렷한 점"이 아니라 "부드럽게 번지는 글로우/백광"처럼 보이고 있었음. 사용자가 "더 또렷한 점으로 변경"을 선택 — `GetSharedInfectionDotSprite()`를 중심 60%는 완전 불투명한 코어로 유지하고 바깥 40% 구간에서만 SmoothStep 페이드하도록 수정(Step47 이전의 완전 하드엣지로는 되돌리지 않음, 안티에일리어싱된 가장자리는 유지). 이 스프라이트는 핫스팟과 공유(static 캐시)라 나중에 `hotspotsEnabled`를 다시 켜면 핫스팟도 더 또렷한 코어를 쓰게 되고, 그만큼 Step45~48이 우려했던 "가리는 문제"가 살짝 다시 커질 수 있음을 코드 주석에 남겨둠 | `Gameplay/CountryView.cs` |
| 53 | "감염 점 최소 크기 0.5로 고정하고, 한국보다 큰 나라는 크기를 확대"하라는 요청 — Step 51까지의 `infectionDotDiameterScale`은 전 국가 공통 배율(0.7)이라 국가별 diameter(이미 면적에 비례해 다름 — 러시아 0.051 vs 한국 0.009)에 그대로 곱해질 뿐, "한국보다 큰 나라를 추가로 더 키우는" 효과는 없었음. `InfectionDotDatabase`에 로드된 48개국 중 최소 diameter(현재 한국)를 추적하는 `MinDiameter` 정적 프로퍼티를 추가(국가 id를 하드코딩하지 않고 매 로드 시 자동 산출), `CountryView.SetupInfectionDots()`에서 `resolvedScale = Max(infectionDotDiameterScale, infectionDotDiameterScale * (자국diameter/MinDiameter))`로 국가별 배율을 계산하도록 변경 — 한국은 ratio=1이라 정확히 바닥값(0.5)을 쓰고, 더 큰 나라는 그 비율만큼 배율 자체가 커진다(러시아는 diameter 비율 5.66배 → 배율도 0.5→2.83으로 커져 최종 지름이 기존 대비 약 4배). `infectionDotDiameterScale` 기본값도 0.7→0.5로 낮춤(이제 "바닥값" 의미이므로). **씬 파일 주의**: Step 41에서 겪었던 것과 같은 문제로, `GamePlay.unity`의 48개 CountryView 인스턴스가 이 필드를 `1`로 직렬화 저장해 스크립트 기본값을 무시하고 있었음 — 코드 변경만으로는 반영 안 돼서 씬 YAML의 48개 값을 전부 `1`→`0.5`로 직접 치환 | `Data/InfectionDotDatabase.cs`, `Gameplay/CountryView.cs`, `Assets/Scenes/GamePlay.unity` |
| 54 | 두 가지 신고 동시 처리. (1) "`[CountryView] SDN 목표색 갱신 — 감염률=0%...` 이 부류 디버그 로그 지워줘" — `CountryView.UpdateVisual()`의 매 틱 국가별 목표색 로그(48개국 × 밴드 변화마다)를 제거, 관련 밴드 추적 필드는 남겨둠(당장 미사용). (2) **"게임 시작 버튼 누르면 광고 보고 부활/다시 시작 창이 바로 나와서 플레이가 안 된다"** — `WorldState.IsPathogenEradicated`(패배 조건)가 `infectedCount <= 0`만으로 판정하는데, 이 조건은 "치료제 완성으로 병원체가 박멸된 상태"와 "발원 감염이 아직 안 심어진 새 게임 막 시작 직후"를 구분 못 함(둘 다 감염자 0명) — 코드 추적 결과 발원국 시딩(`GameDataBootstrapper.SeedStartingInfection()`)은 정상 순서로 호출되고 있어 근본 원인을 100% 재현하진 못했지만, 이 판정 자체가 시점과 무관하게 항상 참일 수 있는 구조적 결함이라 `cureResearchStarted`와 같은 패턴으로 `hasEverBeenInfected` 게이트를 추가 — 감염이 실제로 한 번이라도 기록된 뒤에만 그 후 0이 되는 걸 "박멸"로 인정하도록 수정. `WorldDataManager.RecalculateWorldTotals()`에서 자동 세팅(발원 시딩도 이 메서드를 호출하므로 별도 배선 불필요), `LoadState()`/`WorldState.Reset()`에도 반영. **재발 시 확인할 것**: 그래도 재현되면 [FLOW] 로그(`BeginGame —`, `SeedStartingInfection`)를 확인해 `startingCountryId`가 실제로 비어있는지 볼 것 | `Gameplay/CountryView.cs`, `Data/WorldState.cs`, `Managers/WorldDataManager.cs` |
| 55 | "`[HumanResistanceManager] Peru(PER) 붕괴 단계 변경` 이 부류 디버그 로그 지워줘" — Step 54와 같은 종류의 요청. `HumanResistanceManager`의 국가별 개별 붕괴 단계(`CountryCollapseStage`) 변경 로그를 제거했다(48개국이 각자 붕괴 단계가 바뀔 때마다 콘솔에 로그를 남겨 스팸이 심했음) — funding 배율 계산에 쓰이는 `_lastCollapseStage` 갱신 로직 자체는 그대로 유지하고 로그 호출만 제거. 이어서 Step 54의 "게임 시작 버튼 누르면 즉시 패배 화면" 수정 직후 사용자가 "발원 국가가 안 나온다"고 재신고 — AskUserQuestion으로 확인해보니 실제로는 "CountrySelect(국가 선택) 화면 자체가 안 뜬다"는 증상이었다. Step 54에서 건드린 파일들은 MainMenu→CountrySelect 전환 코드를 전혀 건드리지 않았고 문법도 재확인했지만 원인을 못 찾아, 사용자에게 Console의 `[FLOW][UIManager] HandlePathogenConfirmed`/`[FLOW][CountrySelectController] OnEnable`/`Show()` 로그를 요청해둔 상태 — 회신 대기 중 | `Managers/HumanResistanceManager.cs` |
| 56 | Step 55에서 요청한 콘솔 로그를 받아 확인한 결과 **`GamePlay.unity` 씬 파일 자체가 파일 끝부분에서 중간에 잘려 있었다**(진짜 파일 손상 — 코드 문제 아님). 콘솔의 `The referenced script on this Behaviour (Game Object 'SceneUICoordinator') is missing!` + `[FLOW][CountrySelectController] 뒤로 버튼 클릭됨 ... OnBackRequested 구독자 수=0` 두 줄이 결정적 단서였음 — `UIManager`가 이 GameObject의 세 번째 컴포넌트인데, 씬 파일이 정확히 그 컴포넌트 바로 앞(`m_PrefabAsset: {fileID: 0}` 다음 줄 `m_GameObject:`가 `m_Gam`에서 끊김, 뒤에 아무 바이트도 없음)에서 잘려 있어 Unity가 이 컴포넌트를 통째로 파싱하지 못하고 있었다. 그 결과 `UIManager.Start()`/`OnEnable()`이 아예 실행되지 않아 (a) MainMenu를 띄우고 CountrySelect를 숨기는 로직이 안 돌고, (b) `CountrySelectController`의 이벤트(`OnBackRequested`/`OnCountryConfirmed`)에 UIManager가 구독을 못 해 버튼을 눌러도 아무 반응이 없고, (c) `Show()`(따라서 `RebuildList()`)가 한 번도 호출된 적이 없어 국가 목록이 항상 빈 채로 남아있었던 것 — 사용자가 본 "발원 국가를 선택 타이틀/뒤로/시작 버튼만 나오고 목록이 없다"는 증상과 정확히 일치. 잘려나간 건 `UIManager` 컴포넌트 뒷부분뿐 아니라 `SymptomTreeUI`/`AbilityTreeUI`(업그레이드 트리 탭 2개)와 `CountryStatusPanelController` GameObject 전체가 파일에서 통째로 사라진 상태였다(grep으로 전수 확인). `git log`의 마지막 커밋(HEAD) 버전에서 이 구간(UIManager 컴포넌트~파일 끝, 총 508줄)을 가져와 이어붙여 복구 — 이 구간은 UI 패널 배선 전용이라 Step 41/51/53에서 건드린 `infectionDotDiameterScale`(국가 오브젝트 쪽에 있음) 등 최근 수치 변경과 전혀 겹치지 않아 HEAD 버전을 그대로 써도 안전함을 사전에 확인(grep으로 해당 구간에 `infectionDotDiameterScale`이 하나도 없음을 검증). 복구 후 fileID 중복 없음, `UpgradeTreeView`가 3개(전파/증상/능력), `CountryStatusPanelController`가 1개로 정상 개수인 것까지 확인. **파손 원인은 확정 못 함** — 내가 지난 세션에 한 `sed` 편집(Step 53, 48개 `infectionDotDiameterScale` 값 치환)은 이 구간을 전혀 건드리지 않고 당시 즉시 검증도 정상이었어서, 유니티 에디터 쪽에서 저장이 중간에 끊겼을 가능성이 더 높아 보이지만 100% 확신은 어려움 | `Assets/Scenes/GamePlay.unity` |

---

## 다음에 할 일 (TODO)

- Step 30-5까지의 교통망 수정(유닛 수/크기, 26개 해운 항로, 배·비행기 구분)과 Step 33(HND 좌표
  보정/유닛 빈도 재하향/carrier 확률화)은 사용자 피드백 기반으로 반영 완료. 다음 세션 확인 사항:
  1. 프로젝트 열어 컴파일 에러 없는지 확인 (정적 리뷰는 완료됐지만 실제 컴파일러 확인 아직 안 함)
  2. `GamePlay` 씬 재생 → 콘솔에서 `[TransportManager] 허브 30개 좌표 해석 완료` 로그 확인
  3. (Step 34로 대체됨 — 아래 참고) 인구 스케일이 "실제 인구/1000"에서 "실제 인구 수 그대로"로 바뀌었으니,
     DNA 버블 빈도/치료제 시작 확률/교통망 감염 확률 밸런스를 실플레이로 재확인할 것
  4. (Step 30-5에서 발견, 당장 안 고치기로 함) 산투스항(SEA_SAN, 브라질)은 가장 가까운 바다 지점까지
     거리가 다른 허브보다 좀 더 멀다(약 116px, 다른 허브는 대개 10~40px) — 화면 전체 대비로는 미미하니
     눈에 띄게 어색할 때만 오프셋 개별 조정할 것.
  5. **(Step 32/33 확인 완료, 조치는 안 함)** 두바이(DXB)·싱가포르(SIN) 항공 허브는 실제 위경도 절대
     좌표로 두니 대표 국가(SAU/MAS) 실루엣 밖에 찍힌다(픽셀 확인 완료 — UAE/싱가포르가 애초에 그
     대표국 영토가 아니므로 지리적으로는 오히려 정확한 결과). 게임 로직(isAirportOpen 등)은 여전히
     대표국 기준이라 문제 없지만, 지도 위에서 시각적으로 어색하면(대표국 색칠 영역과 동떨어져 보이면)
     그때 개별 좌표를 대표국 실루엣 쪽으로 당길 것.
  6. **(Step 33 신규, 실기기 확인 필요)** `carrierChanceScale`(기본 25)은 감염 비율 4%부터 사실상
     항상 carrier가 되도록 잡은 값인데, 실제 플레이에서 "너무 늦게 빨간 유닛이 나온다"거나 반대로
     "여전히 너무 이르다"고 느껴지면 이 배율(`TransportManager` 인스펙터)을 조정할 것 — 낮출수록
     carrier가 더 늦게(더 감염이 퍼져야) 나오고, 높일수록 더 일찍 나온다.
  7. **(Step 34 신규, 실플레이 필요)** 인구 스케일 제거(실제 인구 수 그대로)로 포화까지 걸리는 틱 수가
     로그 스케일로 늘어난다(대략 log(1000)/log(1+틱당 성장률)만큼 추가). `cureStartChancePerInfected`/
     `PerDeath`(`SimulationManager`)와 `whoMeetingMinDeathCount`(`EventManager`)는 1000배로 재조정했지만,
     혹시 놓친 절대값 기반 임계값이 더 있을 수 있으니 실플레이로 "치료제/이벤트 타이밍이 이전과 비슷한지"
     확인할 것. 상세 내용은 `Docs/DevLog.md` Step 34 참고.
  8. **(Step 36으로 대체됨 — 아래 참고)** 국가별 감염 점(dot) 오버레이는 Step 35(고정 24개, farthest-point
     sampling) → Step 36(국가 면적 기반 14~90개 + 도시 클러스터링)으로 두 번 개선됐다. 실제 렌더링은
     여전히 못 봤음.
  9. **(Step 36 신규, 실플레이 필요)** 감염 점이 이제 국가별로 개수(14~90)·지름이 다 다르고, 주요 도시
     주변에 뭉쳐 나타나도록 바뀌었다 — `CountryView` 인스펙터의 `infectionDotDiameterScale`(기본 1,
     전체 크기 배율)/`dotTransitionSpeed`(6)/`dotSortingOrder`(50)를 실플레이로 보고 튜닝할 것. 특히
     확인할 것: (a) 감염률 100%일 때 정말 "꽉 찬" 느낌인지(설계상 면적의 70%를 덮도록 계산함),
     (b) 도시 클러스터가 자연스러운 뭉침으로 보이는지 아니면 여전히 어색한지, (c) 러시아처럼 가로로
     아주 긴 나라에서 클러스터 반경(바운딩박스 대각선의 3.5%)이 적절한지. 상세 내용은 `Docs/DevLog.md`
     Step 36 참고.
  10. **(Step 37 완료)** 비행기/배 그래픽을 사용자가 고른 이모지(✈️/🚢)로 교체 완료.
  11. **(Step 37→38로 대체됨)** halo(반투명 원)로 carrier/idle을 표시하던 방식은 "너무 크다"는 피드백을
      받아 Step 38에서 색상 윤곽선 방식으로 교체했다 — halo 관련 튜닝 항목은 더 이상 유효하지 않음.
  12. **(Step 37, 배포 전 확인 필요 — 법률 자문 권장)** 이모지 그래픽 출처가 Twemoji(`emoji-datasource-twitter`
      npm 패키지에서 PNG만 추출)라 **CC-BY 4.0 라이선스** — 코드(MIT)와 별개로 그래픽 자체는 저작자 표시가
      필요할 수 있다. 앱인토스 등에 실제 배포하기 전에 오픈소스 고지/크레딧 화면에 Twemoji 저작자 표시를
      넣을지 검토할 것. (Step 38에서 추가한 `plane_outline.png`/`ship_outline.png`도 같은 소스에서 만든
      파생물이라 동일하게 적용됨.)
  13. **(Step 38 신규, 실플레이 필요)** 이동 방향 회전(`AirSpriteBaseFacingDeg=45`/`SeaSpriteBaseFacingDeg=180`)은
      여전히 PCA/육안 추정값이라 실제로 맞는지 확인 필요. 색상 윤곽선(`outlineThicknessWorldUnits` 기본
      0.012, 8방향)이 halo보다 자연스러운지, 이모지 축소(`iconScale`)가 적당한지 실플레이로 확인하고
      `TransportUnit` 인스펙터에서 바로 조정할 것. 오프라인 Python 합성 미리보기로 알고리즘은 확인했지만
      실제 Unity 렌더링은 아직 못 봤음 — 상세 내용은 `Docs/DevLog.md` Step 38 참고.
  14. **(Step 39→40으로 위치 이전)** 배 그래픽을 사용자 제공 "Ship 1.png"(고해상도)로 교체.
      `outlineThicknessWorldUnits`(월드 유닛 고정값, 기본 0.012)가 실제로 잘 맞는지, 이동 방향(180도
      기준각)이 맞는지는 여전히 실플레이 확인 필요. `iconScale`은 Step 40에서 `TransportUnit`→
      `TransportManager` 인스펙터로 위치가 옮겨졌다(아래 참고).
  15. **(Step 40 신규, 사용자 확인 필요 — 최우선)** "이미지 적용이 안 되는 것 같다" 신고로 점검하다가
      `TransportIcons/` 폴더에서 실제 파일 손상/누락을 발견했다: `plane.png`가 64×64에서 32×32로 깨져
      있었고(헤더만 조작된 형태 — 원본 픽셀 데이터 자체는 살아있어 Twemoji 원본으로 안전하게 복구),
      `ship.png`/`ship_outline.png`는 프로젝트에서 아예 사라져 있었다(Twemoji 원본으로 복구). 이 둘은
      공개 이모지 데이터라 복구에 데이터 손실이 없다. **문제는 "Ship 1.png"(사용자가 직접 제공한 배
      그래픽)** — 현재 117×64로 줄어들어 있고 `.meta`의 스프라이트 슬라이스 정의도 비어있는 상태였다.
      세션 중 이 파일의 `.meta` 내용이 여러 번 읽을 때마다 달라져 있어서, 사용자가 Unity 에디터에서
      Sprite Editor로 이 파일을 직접 편집하던 중이었을 가능성이 높다고 보고 **일부러 건드리지 않고
      그대로 뒀다** — 다음 세션에서 (a) 사용자가 의도한 최종 크기/크롭이 무엇인지, (b) 지금 파일이
      맞는 상태인지 아니면 원본을 다시 넣어야 하는지 확인할 것. 그 외 구조적으로, 본체·윤곽선 스프라이트
      한 쌍의 텍스처 크기/Pixels Per Unit이 서로 어긋나면(사용자가 Pixels Per Unit으로 이미지 크기를
      조정할 때 본체만 바꾸고 윤곽선은 안 바꾸는 경우 등) 윤곽선이 안 보이거나 과하게 커 보이는 버그가
      있었는데, 윤곽선의 로컬 스케일을 본체의 실제 월드 크기에 맞춰 매번 재계산하도록 고쳐 이제는
      텍스처 크기/PPU가 서로 달라도 항상 시각적으로 맞는다 — 실제 Unity 렌더링으로 확인 필요. **이미지
      크기 조정은 이제 `TransportManager` 인스펙터의 `iconScale` 필드에서 한다** — 예전엔 `TransportUnit`
      필드였는데, 이 컴포넌트는 프리팹 없이 코드로만 생성돼(AddComponent) 인스펙터에서 값을 바꿔도
      Play 종료 시 사라지는 문제가 있어 씬에 실제로 존재하는 `TransportManager`(`carrierChanceScale`과
      같은 위치)로 옮겼다. 상세 내용은 `Docs/DevLog.md` Step 40 참고.
  16. **(Step 40 후속, 사용자가 "Ship 1 이미지를 써달라"고 확정)** "Ship 1.png"는 최종적으로 117×64
      크기로 확정된 상태(사용자가 그 상태로 계속 두기로 함). 남아있던 진짜 문제는 `.meta`의
      `spriteMode: 2`(Multiple)인데 `spriteSheet.sprites`가 빈 배열(`[]`)이라 슬라이스가 아예 정의돼
      있지 않았던 것 — `LoadFirstSprite`(LoadAll 기반)로도 서브 스프라이트를 못 찾아 계속 폴백(Twemoji
      ship.png)으로 떨어지고 있었다. 실제 이미지 전체(0,0,117,64)를 덮는 슬라이스 "Ship 1_0"을 직접
      정의해 넣어서 해결 — 다음 Unity 실행 시 이 슬라이스로 재임포트되면 "Ship 1"이 정상적으로 로드될
      것. `Ship 1_outline.png`(1188×648)는 손대지 않았다 — 종횡비(1.833 vs 1.828)가 거의 같아서 Step 40의
      윤곽선 자동 스케일 매칭 로직이 크기 차이를 알아서 보정해준다. 실제 Unity 렌더링으로 최종 확인 필요.
  17. **(Step 44로 완전히 대체됨 — 아래 참고)** 감염 점 크기/개수를 Step 41/42/43에 걸쳐 세 번 늘렸던
      "국가당 개별 점 GameObject" 방식(최종 국가당 최대 412개, 합계 6380개) 자체를 Step 44에서 폐기하고
      "국가당 소수의 큰 핫스팟" 방식으로 아키텍처를 바꿨다. 이 항목들(구 17~19번)은 더 이상 유효하지 않음.
  18. **(Step 42 신규, 여전히 유효 — 실플레이 필요)** DNA 버블 스폰 로직(국가별 실루엣 내부 좌표 재사용)은
      Step 44에서도 그대로 유지된다(핫스팟 GameObject가 아니라 원본 좌표 배열에서 직접 샘플링하도록
      내부만 바뀜, 공개 API는 동일). 실제 렌더링으로 (a) 버블이 국가 실루엣 안에서 스폰되는지(특히
      한국·소형 섬나라 위주), (b) 다른 시각 요소와 안 겹치는지 확인할 것.
  19. **(Step 44~47로 대체됨 — Step 49로 완전히 무효화, 아래 참고)** 핫스팟 개수/크기/상한/텍스처를
      네 차례에 걸쳐 튜닝했던 항목들(구 19~22번)은 Step 49에서 핫스팟 오버레이 자체를 껐기 때문에 더
      이상 유효하지 않다. 코드(`hotspotSizeBoost` 등 인스펙터 필드)는 그대로 남아있어 나중에
      `hotspotsEnabled`를 다시 켜면 이 튜닝값들이 그대로 되살아난다 — 재활성화할 때 이 항목들을
      다시 참고할 것. 상세 튜닝 이력은 `Docs/DevLog.md` Step 44~47 참고.
  20. **(Step 48 신규, 최우선 — 실플레이 필요)** "배/비행기/노선이 감염 점보다 아래 레이어에 있는 것
      같다" 신고로 sortingOrder를 다시 전수 검토했으나 **코드상으로는 이미 정상**(교통이 핫스팟보다 위)
      이었다 — 대신 색상 대비 문제 두 가지를 찾아 고쳤다: 노선 알파 0.18→0.55(+두께 0.02→0.032),
      감염 배(`SeaCarrierColor`)가 핫스팟 빨강과 사실상 같은 색이던 것을 마젠타로 교체. **Step 49에서
      핫스팟 자체가 기본 꺼졌으니 지금은 이 수정이 핫스팟과의 대비 문제를 해결하는 용도로는 당장
      쓰일 일이 없지만, 노선/carrier 색상 자체는 여전히 유효한 개선이라 되돌리지 않았다.** 확인할 것:
      (a) 배/비행기/노선이 잘 보이는지, (b) 노선이 너무 진해져서 지도가 산만해 보이진 않는지(문제면
      `TransportManager.airRouteColor`/`seaRouteColor`의 알파를 0.55에서 다시 낮출 것), (c) 마젠타가
      "감염된 배"라는 의미로 직관적으로 읽히는지. 상세 내용은 `Docs/DevLog.md` Step 48 참고.
  21. **(Step 49로 시작, Step 50으로 정정됨)** "레이어로 나눠서 구분하는 게 너무 힘들다, 하이라이트
      없는 이전 버전으로 돌아가자"는 요청으로 Step 49에서 핫스팟 오버레이를 끄고 국가 색상 얼룩만
      남겼는데(`hotspotsEnabled=false`), 바로 다음에 "감염 점 안 보이는데"라는 재신고를 받고 확인해보니
      실제로 원했던 건 색상 얼룩만 남기는 게 아니라 **핫스팟(Step44의 큰 원) 없이 Step 41~43의 "개별
      점" 방식**이었다 — 아래 22번 참고. `hotspotsEnabled`는 여전히 false로 남아있고(핫스팟 자체는
      여전히 꺼진 상태), 대신 Step 50에서 `dotsEnabled`(기본 true)로 개별 점 시스템을 되살렸다.
  22. **(Step 50 신규, 최우선 — 실플레이 필요)** Step 49 직후 "감염 점 안 보이는데 뭔 문제여" 신고로
      AskUserQuestion을 통해 확인한 결과를 반영해, Step 44에서 대체됐던 개별 점 GameObject 생성 로직
      (`SetupInfectionDots()`/`UpdateInfectionDots()`)을 git 이력(Step 36 시점 커밋)에서 복원하고 현재
      아키텍처(Step 46 레이어 체계, Step 47 그라디언트 스프라이트, `LoadDotData()` 분리)에 맞게 재조립
      했다. 확인할 것: (a) 이제 실제로 국가별로 여러 개의 작은 빨간 점이 감염률에 비례해 나타나는지,
      (b) `dotSortingOrder=10`(핫스팟이 쓰던 자리 — 국가 오버레이(20)보다 아래)이 예전 값(50, 국가
      오버레이보다 위)과 달라서 점이 국가 색상 얼룩에 가려 안 보이지는 않는지(문제면 `dotSortingOrder`를
      국가 오버레이(20)보다 높은 값으로 올릴 것 — 이번엔 개별 점이라 핫스팟만큼 크게 가리지 않을 걸로
      예상되지만 실제 렌더링 확인 전까지는 모름), (c) 크기/개수 느낌은 Step 51에서 한 차례 더 조정됐으니
      아래 23번 참고. 핫스팟을 다시 켜고 싶으면 `hotspotsEnabled=true`(개별 점과 동시에 켜져도 서로
      충돌하지 않음, 둘 다 독립적인 GameObject라 같이 표시될 뿐임 — 다만 그러면 Step 45~48이 우려했던
      레이어링 복잡도가 다시 생길 수 있음). 상세 내용은 `Docs/DevLog.md` Step 50 참고.
  23. **(Step 51 신규, 최우선 — 실플레이 필요)** "상위 프리팹으로 중앙 조정 + 국가 면적 비례 확대"
      제안은 (a) 면적 비례는 이미 구현돼 있고(각국 `diameter`가 InfectionDotDatabase에 면적 기반으로
      미리 계산됨), (b) 진짜 중앙집중식 매니저 구조로 바꾸는 건 새 씬 배선이 필요해 에디터 미접속
      리스크가 있어서 보류하고, 사용자가 제시한 대안(크기 2배 + 개수 2배)을 그대로 적용했다.
      `infectionDotDiameterScale` 0.35→0.7, `InfectionDotPoints.json` 재생성으로 국가별 점 개수 2배
      (총 6380→12760, 러시아 412→824), `maxInfectionDots` 450→900(새 최대치가 안 잘리도록). 새 점은
      기존 점 각각의 이웃에 그 국가 `diameter` 기준 가우시안 지터로 배치하고 `CountryShapes/{id}.png`
      알파마스크로 실루엣 내부인지 매번 검증했다(48개국 중 2개 점만 검증 실패해 아주 작은 고정 오프셋
      폴백 사용). 확인할 것: (a) 크기/개수가 실제로 2배로 체감되는지, (b) 특히 러시아·캐나다처럼 이미
      점이 많던 큰 나라에서 점끼리 너무 빽빽하게 겹쳐 뭉개진 얼룩처럼 보이지 않는지(문제면
      `infectionDotDiameterScale`을 0.7보다 낮추는 게 개수를 다시 줄이는 것보다 먼저 시도할 조정),
      (c) 새로 추가된 점들이 실제로 국가 실루엣 안에 있는지(오프라인 알파마스크 검증은 했지만 Unity
      렌더링으로는 미확인). 상세 내용은 `Docs/DevLog.md` Step 51 참고.
  24. **(Step 52 신규, 최우선 — 실플레이 필요)** "감염 점 하이라이트 있는데 이거 뭐임" 질문으로 확인해보니
      Step 47의 그라디언트 스프라이트(반지름 전체에 걸쳐 페이드)가 개별 점(작음)에는 "또렷한 점"이 아니라
      "부드러운 글로우/백광"처럼 보이고 있었다. `GetSharedInfectionDotSprite()`를 중심 60% 완전 불투명
      코어 + 바깥 40%만 페이드로 수정했다. 확인할 것: (a) 이제 점이 "또렷한 빨간 점 + 살짝 부드러운
      가장자리"로 보이는지(여전히 너무 번져 보이면 `coreFraction`(현재 0.6, 코드에 하드코딩)을 더
      올릴 것 — 1.0에 가까울수록 Step47 이전의 완전 하드엣지에 가까워짐), (b) 코어를 키운 만큼 점 하나가
      "완전히 가리는" 면적도 다시 조금 늘었는데(Step47이 줄이려던 바로 그 문제), 개별 점은 크기가 작아
      체감이 안 될 걸로 예상되지만 실제로 다른 요소를 가리진 않는지. **주의**: 이 스프라이트는 static
      캐시로 핫스팟(Step44~48)과 공유돼서, 나중에 `hotspotsEnabled=true`로 되돌리면 핫스팟도 이 더
      또렷한 코어를 쓰게 되고 Step45~48이 줄이려 애썼던 "가리는 문제"가 다시 커질 수 있다 — 그때는
      핫스팟 전용으로 스프라이트를 분리하는 걸 고려할 것. 상세 내용은 `Docs/DevLog.md` Step 52 참고.
  25. **(Step 53 신규, 최우선 — 실플레이 필요)** "감염 점 최소 크기 0.5로 고정, 한국보다 큰 나라는 크기를
      확대"하라는 요청으로 `infectionDotDiameterScale`(기존엔 전 국가 공통 배율)의 역할을 "가장 작은
      나라(현재 한국)에 적용되는 바닥값"으로 바꾸고, 자국 diameter가 `InfectionDotDatabase.MinDiameter`
      (한국 diameter)보다 큰 비율만큼 배율 자체를 키우도록 `CountryView.SetupInfectionDots()`를 수정했다
      (한국은 정확히 0.5, 러시아는 원본 diameter가 한국의 약 5.66배라 배율도 0.5→2.83까지 커져 최종
      지름이 기존 대비 약 4배). 확인할 것: (a) 한국 등 작은 나라의 점이 실제로 눈에 띄게 "최소 크기"로
      보이는지, (b) 러시아·캐나다·미국처럼 원래도 diameter가 크던 나라들이 이제 지나치게 커져서 점끼리
      겹쳐 뭉개진 얼룩처럼 보이지는 않는지(문제면 이 비율 계산 자체에 `Mathf.Sqrt()`를 씌워 증가폭을
      완만하게 만들 것 — 지금은 선형 비례라 diameter 차이가 그대로 배율 차이로 반영됨), (c) `GamePlay.unity`
      의 48개 CountryView 인스턴스에 직접 패치한 `infectionDotDiameterScale: 0.5` 값이 실제로 이 새
      계산식과 함께 의도대로 작동하는지(코드 기본값과 씬 직렬화 값이 둘 다 0.5로 일치함은 확인함). 상세
      내용은 `Docs/DevLog.md` Step 53 참고.
  26. **(Step 54 신규, 최우선 — 실플레이 필요)** "게임 시작 버튼 누르면 즉시 패배 화면이 뜬다" 버그를
      `WorldState.hasEverBeenInfected` 게이트로 고쳤다. 코드 추적으로는 발원 감염 시딩
      (`GameDataBootstrapper.SeedStartingInfection()`)이 `SetPaused(false)`보다 항상 먼저 실행되는 정상
      순서라 이 버그가 나오는 정확한 경로를 100% 재현하지는 못했다 — 다만 `IsPathogenEradicated`가
      "감염자 0명"만으로 판정하는 게 시딩 타이밍과 무관하게 시점을 착각할 수 있는 구조적 결함이라
      방어적으로 게이트를 씌웠다. 확인할 것: (a) 게임 시작 버튼을 눌렀을 때 정상적으로 플레이가
      시작되는지, (b) 만약 **여전히 재현되면** Console에서 `[FLOW][GameDataBootstrapper] BeginGame —`과
      그 직후 `SeedStartingInfection` 관련 경고(`startingCountryId '...'를 찾을 수 없습니다` 등) 로그를
      확인해 실제 원인(예: countryId 전달 안 됨, 국가 조회 실패)을 찾을 것 — 이번 수정은 증상(즉시 패배
      화면)은 막지만, 만약 시딩 자체가 실제로 실패하고 있다면 그 근본 원인은 아직 안 고쳐진 것일 수
      있다. 상세 내용은 `Docs/DevLog.md` Step 54 참고.
  27. **(Step 56에서 해결됨)** "발원 국가가 안 나온다"(CountrySelect 화면 자체가 안 뜸) 문제의 실제
      원인은 코드가 아니라 `GamePlay.unity` 씬 파일 자체의 손상(파일 끝부분이 중간에 잘려 `UIManager`
      컴포넌트 + `SymptomTreeUI`/`AbilityTreeUI`/`CountryStatusPanelController` 전체가 파싱 불가능한
      상태)이었음. git HEAD에서 해당 구간(508줄)을 가져와 복구 완료 — 상세 내용은 아래 28번과
      `Docs/DevLog.md` Step 56 참고.
  28. **(Step 56 신규, 최우선 — 다음 세션 시작하자마자 확인)** `GamePlay.unity` 손상을 복구했지만 (a) 이
      복구가 Unity 에디터로 실제로 열었을 때 정상 로드되는지(YAML 문법상으로는 앞뒤 경계가 정확히
      맞물리는 것을 확인했지만 Unity 자체 파서로 열어본 적은 없음), (b) MainMenu → 병원체 선택 → "다음" →
      CountrySelect 화면에 실제로 국가 목록이 뜨는지, (c) "시작" 버튼을 눌렀을 때 정상적으로 게임이
      시작되는지(Step 54의 `hasEverBeenInfected` 수정과 합쳐 이번엔 정말 플레이가 되는지) 실제 플레이로
      확인 필요. **파손 원인이 확정되지 않았다는 점도 중요** — 내가 지난 세션에 한 `sed` 스크립트 편집
      (Step 53, 48개 `infectionDotDiameterScale` 값 치환)은 이 손상 구간을 전혀 건드리지 않았고 당시
      즉시 검증도 정상이어서 원인일 가능성은 낮아 보이지만, 100% 배제는 못 한다. 재발 방지를 위해:
      (1) 이 프로젝트는 git 저장소인데 `GamePlay.unity`를 포함한 수많은 파일이 여러 Step에 걸쳐
      **커밋되지 않은 채** 누적돼 있다(`git status`로 미추적/미커밋 변경사항 다수 확인) — 씬처럼 텍스트가
      아닌(사실상 텍스트지만 손으로 diff 보기 힘든) 대용량 파일은 한 번 손상되면 이번처럼 git 히스토리가
      유일한 복구 수단이 되므로, 사용자가 Unity 에디터에서 안정적으로 저장을 확인할 때마다 주기적으로
      커밋해두는 걸 권장할 것. (2) 이번 손상이 유니티 저장 중단(예: 에디터 크래시, 디스크 쓰기 중
      끊김) 때문이라면, 다음에 또 "뭔가 이상하다"는 신고가 들어오면 이번처럼 파일 끝부분(`tail -c 200`)이
      멀쩡한 문장으로 끝나는지부터 확인하는 걸 진단 절차에 추가할 것.

### 씬/에셋 배선 필요 (코드만으로는 안 되는 작업)

- `MainMenu`/`CountrySelect`/`GamePlay` 씬 분리 (현재 씬 하나에서 UIDocument 패널 on/off로 대체 중)
- `DnaBubble` 프리팹 제작(SpriteRenderer + CircleCollider2D) 후 `BubbleSpawner.bubblePrefab`에 연결
- 앱인토스 SDK 설치(`Packages/manifest.json`) — **한 번 시도했다가 되돌림**: git 의존성 추가 시 pnpm
  lockfile 충돌(`ERR_PNPM_LOCKFILE_CONFIG_MISMATCH`) 발생, 게임 마지막 단계로 미룸. 재시도 시 사람이
  터미널에서 `pnpm install --no-frozen-lockfile` 수동 실행 필요.
- `AudioManager` 오브젝트 생성 + 효과음 에셋 준비/연결 (에셋은 사람이 준비/임포트해야 함)
- (선택) MainMenu의 DNA+5 보상형 광고 버튼 — `GameAds.Rewarded` 재사용해서 붙이기만 하면 됨

---

## 튜닝 포인트 (설계 문서 미정의 계수 — 밸런싱 시 여기부터 조정)

설계 문서 4절 공식에는 `spreadFactor`, `climateModifier`, `severityFactor`, `researchMultiplier`,
`drugResistanceReduction` 등 정의되지 않은 계수가 여러 개 있다:

- `climateModifier` = `Pathogen.GetEnvironmentResistance(country.climate)` (0~1, 미설정 시 기본 1=중립)
- `countryHealthLevel`/`healthcareCapacity` = `Country.HealthLevel` (developmentLevel → Low 0.2 / Mid 0.5 / High 0.8)
- `researchMultiplier` = `Country.ResearchMultiplier` (Low 0.2 / Mid 0.8 / High 1.5)
- `severityFactor` = `Pathogen.severity` 그대로 사용
- `drugResistanceReduction` = `pathogen.drugResistance * drugResistanceCoefficient`(SimulationManager 인스펙터 값)
- `spreadFactor`, 국가 간 육상 전파 확률(`landBorderSpreadChance`), DNA 마일스톤 간격, 저항 단계별 봉쇄
  임계값은 전부 `SimulationManager`/`HumanResistanceManager` 인스펙터 노출 값 — 플레이테스트로 조정.
- 항공/해운 전파는 이제 `TransportManager` 인스펙터(`airArrivalInfectionChance`/`seaArrivalInfectionChance`/
  `airSeedAmount`/`seaSeedAmount`/`infectionVisibilityScale` 등)에서 조정 — `SimulationManager`의
  `airRouteSpreadChance`/`seaRouteSpreadChance`는 [미사용]으로 남겨둔 필드이니 헷갈리지 말 것.
- 국가 수가 48개까지 늘어난 만큼 `SimulationManager`의 `Cure Progress Coefficient`(기본 0.002)가 다시
  커 보일 수 있음 — 치료제가 너무 빨리 100%를 찍으면 이 값부터 낮출 것.

---

## 규칙

- 이 프로젝트는 개발 대상이다 (참조 전용인 `C:\Game\codebase`와 다름). 자유롭게 수정한다.
- 새 Step을 진행할 때마다 위 "최근 작업" 표에 **한 줄 요약만** 추가하고, 표가 5~6개 Step을 넘어가면
  가장 오래된 Step을 "완료된 주요 시스템" 체크리스트로 압축 이동한다. 상세 구현 배경·버그 진단
  과정·설계 결정 이유는 `Docs/DevLog.md`에 적어서 이 파일이 계속 가볍게 유지되도록 한다.
