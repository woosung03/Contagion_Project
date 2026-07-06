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
  인구는 실제 인구/1000 스케일.

---

## 완료된 주요 시스템 (Step 1~24)

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
- [x] 글로벌 교통망 구축(Step 29~30-2) — 항공/해운 허브 30개(각 15개) 신규 구현, 유닛이 지도 위를 이동하며 도착 시 감염 전파(기존 추상 확률 롤 대체), 국가 18→48개/업그레이드 트리 27→45노드 확장, 해운 경유점을 `world_base.png` 육지 마스크 기반 A* 길찾기로 계산

---

## 최근 작업 (Step 30-3~30-5)

| Step | 내용 | 파일 |
|------|------|------|
| 30-3 | **실제 플레이 첫 확인 피드백 반영**: (1) `BuildSeaWaypoints()` 키 10쌍이 `PairKey()` canonical 순서와 반대로 저장돼 조회 실패 → 해당 항로가 직선으로 대체돼 대륙 관통. 키 순서 전부 교정 + 누락된 SEA_PKL\|SEA_SHA 쌍 추가(25→26쌍). (2) 배/비행기 스프라이트 구분 안 되던 문제 — 비행기는 십자형(1.6:1)/파랑 계열, 배는 납작한 선체(4.4:1)/초록 계열로 실루엣·색상 분리 | `Data/DefaultTransportHubFactory.cs`, `Gameplay/TransportUnit.cs` |
| 30-4 | "사우디-아랍 쪽 배가 사우디 위를 지나다닌다" 재지적 — SEA_JEA(제벨알리) 허브 오프셋이 실제로는 요르단/이스라엘 인접 내륙에 찍혀 있던 버그 발견. 오프셋을 페르시아만 연안으로 정정 + JEA 관련 3개 항로 재계산·재검증 | `Data/DefaultTransportHubFactory.cs` |
| 30-5 | 감염 보유(빨간) 유닛 가시성을 위해 활성 유닛 수(24~180→10~70)/스프라이트 크기(pixelsPerUnit 70→150) 축소 + **26개 해운 항로 전수 재검증**(허브별 "가장 가까운 진짜 바다 지점" sea anchor 도입) — 로스앤젤레스항(SEA_LAX)이 메인 대양과 단절된 웅덩이에 있던 버그 발견·수정 포함. **사용자가 실제 플레이로 최종 확인 완료("문제 없는 것 같아")** | `Managers/TransportManager.cs`, `Gameplay/TransportUnit.cs`, `Data/DefaultTransportHubFactory.cs` |

---

## 다음에 할 일 (TODO)

- Step 30-5까지의 교통망 수정(유닛 수/크기, 26개 해운 항로, 배·비행기 구분)은 사용자가 실제
  플레이로 확인 완료. 다음 세션 확인 사항:
  1. 프로젝트 열어 컴파일 에러 없는지 확인 (정적 리뷰는 완료됐지만 실제 컴파일러 확인 아직 안 함)
  2. `GamePlay` 씬 재생 → 콘솔에서 `[TransportManager] 허브 30개 좌표 해석 완료` 로그 확인
  3. 새 인구 스케일(실제 인구/1000) 기준 DNA 버블 빈도/치료제 시작 확률/교통망 감염 확률 밸런스 재확인
  4. (Step 30-5에서 발견, 당장 안 고치기로 함) 산투스항(SEA_SAN, 브라질)은 가장 가까운 바다 지점까지
     거리가 다른 허브보다 좀 더 멀다(약 116px, 다른 허브는 대개 10~40px) — 화면 전체 대비로는 미미하니
     눈에 띄게 어색할 때만 오프셋 개별 조정할 것.
  5. (기존에 이미 알려진, 이번 수정과 무관한 별개 한계) 미국 내 여러 항공 허브(ATL/DFW 등)가 여전히
     미국 국가 중심점 기준 작은 오프셋으로 정의돼 있어 실제 도시 위치가 아니라 지도 위 한 지점에
     뭉쳐 보임(해운 허브 LAX/LGB는 Step 30-5에서 위치를 조정했지만 항공 허브는 그대로임) — 필요하면
     각 허브에 국가 앵커 대신 직접 좌표를 부여하는 별도 작업으로 처리.

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
