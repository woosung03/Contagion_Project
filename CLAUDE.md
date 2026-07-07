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
- [x] 글로벌 교통망 구축(Step 29~30-3) — 항공/해운 허브 30개(각 15개) 신규 구현, 유닛이 지도 위를 이동하며 도착 시 감염 전파(기존 추상 확률 롤 대체), 국가 18→48개/업그레이드 트리 27→45노드 확장, 해운 경유점을 `world_base.png` 육지 마스크 기반 A* 길찾기로 계산. Step 30-3에서 경유점 키 순서 버그 10쌍 수정 + 배/비행기 스프라이트 실루엣·색상 분리(십자형/파랑 vs 납작한 선체/초록)
- [x] 교통망 미세조정(Step 30-4) — SEA_JEA(제벨알리) 허브 오프셋이 요르단/이스라엘 인접 내륙에 찍혀 있던 버그 발견·정정(페르시아만 연안) + 관련 항로 3개 재검증
- [x] 교통망 재검증+유닛 감소(Step 30-5) — 감염 보유(빨간) 유닛 가시성을 위해 활성 유닛 수(24~180→10~70)/스프라이트 크기(pixelsPerUnit 70→150) 축소 + 26개 해운 항로 전수 재검증(허브별 "가장 가까운 진짜 바다 지점" sea anchor 도입) — 로스앤젤레스항(SEA_LAX)이 메인 대양과 단절된 웅덩이에 있던 버그 발견·수정 포함. 사용자가 실제 플레이로 최종 확인 완료("문제 없는 것 같아")
- [x] UI 디자인 시스템 도입(Step 31) — `C:\Game\codebase` 위키(uss-variables/uss-tss) 패턴 참조해 공통 `Theme.uss`(:root 커스텀 프로퍼티) 신규 작성, 7개 화면에 흩어져 있던 색상·간격·폰트크기·모서리반경 하드코딩을 var(--토큰)으로 통일
- [x] 항공 허브 절대좌표화(Step 32) — "비행기 경로 출발/도착 지점이 공항이여야 하는데 어긋난다" 지적 반영, 48개국 앵커 대 실제 위경도 최소제곱 회귀로 좌표 변환식을 구해 15개 공항을 국가 앵커 상대 오프셋 대신 실제 위경도 기반 절대 좌표로 교체
- [x] 교통망 실플레이 피드백 반영(Step 33) — HND 좌표 30px 보정(육지 위로), 유닛 수 5~35/스폰 최대 3으로 재하향, carrier 판정을 boolean→감염 비율 비례 확률(`carrierChanceScale`)로 변경

---

## 최근 작업 (Step 34~39)

| Step | 내용 | 파일 |
|------|------|------|
| 34 | 인구 스케일을 "실제 인구/1000" → "실제 인구 수 그대로"로 변경. Worldometer(UN 2026 전망, 위키피디아와 동일 출처 계열) 기준으로 48개국 population 전수 갱신 + 파일 말단 truncation 버그(YEM 마지막 항목이 `- I`에서 끊김) 발견·수정. population 절대값에 비례하는 공식만 1000배 재조정(`cureStartChancePerInfected`/`PerDeath`, `whoMeetingMinDeathCount`) — 비율 기반 공식(확산/붕괴/visibility 등)과 절대 인원수 기반 시드 값(`seedInfectedAmount` 등)은 스케일 불변이라 그대로 둠 | `Assets/New Folder/CountryDatabase.asset`, `Managers/SimulationManager.cs`, `Managers/EventManager.cs` |
| 35 | "감염률 색상 얼룩만으로는 시각 피드백이 약하다" 지적 반영 — 국가 실루엣 알파마스크에서 오프라인 Python(farthest-point sampling)으로 국가당 점 24개 좌표를 미리 뽑아 `Assets/Resources/InfectionDotPoints.json`으로 저장(런타임엔 `CountryShapes` 텍스처가 `isReadable=0`이라 못 읽음 — world_base.png A* 계산 때와 같은 이유로 오프라인 사전계산 방식 재사용). `CountryView.Awake()`가 이 좌표로 점 오브젝트(최대 16개, 공유 원형 스프라이트)를 만들어두고, `UpdateVisual()`이 `Ceil(infectionRatio * 점개수)`만큼만 활성화 — 감염자 1명만 있어도 즉시 점 1개가 나타나 색상 얼룩보다 빠른 피드백을 준다. 씬 파일은 안 건드림(전부 코드로 생성) | `Assets/Resources/InfectionDotPoints.json`(신규), `Data/InfectionDotDatabase.cs`(신규), `Gameplay/CountryView.cs` |
| 36 | Step 35 실플레이 이전 피드백 3건 반영: (1) "감염 점이 규칙적" — farthest-point sampling의 균일한 퍼짐이 오히려 격자처럼 보였던 게 원인, 주요 도시 좌표(Step 32 회귀식 재사용) 주변 가우시안 지터 클러스터 + rural 배경점으로 교체해 자연스러운 뭉침으로 변경. (2) "점 수가 적음" — 고정 24개(활성 최대 16개) → 국가 면적(알파 픽셀 수) 기반 14~90개로 국가별 자동 산출. (3) "100%에도 땅이 안 채워짐" — 점 지름도 국가별로 "전체 활성화 시 면적의 70%를 덮도록" 역산해 국가마다 다르게 적용(러시아 0.1085유닛 vs 한국 0.0186유닛). 가중 라운드로빈으로 도시/rural 그룹을 인터리브해 낮은 감염률에서도 "도시 위주+약간의 지방" 비례가 유지되게 함 | `Assets/Resources/InfectionDotPoints.json`(재생성), `Data/InfectionDotDatabase.cs`, `Gameplay/CountryView.cs` |
| 37 | 비행기/배 그래픽을 절차적 도형(십자형/선체) → 실제 이모지로 교체. 채팅 위젯으로 후보 이모지를 보여주고 사용자가 직접 선택(✈️ 여객기, 🚢 화물선). Unity가 색 이모지를 못 그려서 `emoji-datasource-twitter`(Twemoji) PNG를 오프라인에서 그대로 복사해 `Assets/Resources/TransportIcons/`에 추가 — **주의: 이 그래픽은 CC-BY 4.0이라 상용 배포 시 저작자 표시가 필요할 수 있음**(Docs/DevLog.md Step 37 참고). carrier/idle 색 구분은 이모지 본체 틴트 대신 뒤에 까는 halo 원으로 이동, 회전은 이모지 기본 방향(45도/180도) 보정 추가. 리소스 로드 실패 시 기존 절차적 도형으로 자동 폴백 | `Assets/Resources/TransportIcons/plane.png`, `ship.png`(신규), `Gameplay/TransportUnit.cs` |
| 38 | "halo 하이라이트가 너무 큼" 피드백 반영 — halo(반투명 원) 제거하고 색상 윤곽선으로 교체. RGB를 흰색으로 고정한 실루엣 PNG(`plane_outline.png`/`ship_outline.png`, 신규)를 carrier/idle 색으로 물들여 8방향으로 몇 픽셀씩 어긋나게 겹쳐 그리는 고전적인 스프라이트 아웃라인 기법(셰이더 없이 구현) — 원본 색과 곱해지지 않아 깨끗한 단색 테두리가 나온다. 오프라인 Python 합성으로 알고리즘 미리 확인(비행기 주황/배 빨강 테두리 정상 확인). 이모지 본체 크기도 `iconScale`(0.8)로 축소 | `Assets/Resources/TransportIcons/plane_outline.png`, `ship_outline.png`(신규), `Gameplay/TransportUnit.cs` |
| 39 | 사용자가 Twemoji 🚢 대신 직접 준비한 고해상도 화물선 이미지("Ship 1.png", Multiple 스프라이트 모드, pixelsPerUnit=2500)를 배 그래픽으로 교체. `GetSeaSprite()`가 이를 최우선 시도(LoadAll 기반 `LoadFirstSprite` 헬퍼로 서브 스프라이트 추출, CountryView.ApplyCountryShape()와 동일 패턴), 실패 시 기존 ship.png(Twemoji)→절차적 선체 순서로 폴백. 대응하는 흰색 실루엣 `Ship 1_outline.png`도 오프라인 Python으로 생성(PCA로 방향 확인, 기존 180도 기준각 그대로 유효). 작업 중 윤곽선 두께가 `outlineThicknessPx / sprite.pixelsPerUnit`로 계산돼 스프라이트마다 PPU가 다르면(260 vs 2500) 두께가 실제로 크게 달라지는 버그를 발견 — 월드 유닛 고정값(`outlineThicknessWorldUnits`)으로 교체. 사용자가 이미 조정해뒀다고 밝힌 `iconScale`은 건드리지 않음 | `Assets/Resources/TransportIcons/Ship 1.png`, `Ship 1_outline.png`(신규, 사용자 제공+생성), `Gameplay/TransportUnit.cs` |

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
  14. **(Step 39 신규, 실플레이 필요)** 배 그래픽을 사용자 제공 "Ship 1.png"(고해상도)로 교체 — `TransportUnit`
      인스펙터에서 `iconScale`(사용자가 이미 조정함, 그대로 둠)과 새로 추가된 `outlineThicknessWorldUnits`
      (기존 픽셀 기준 값에서 월드 유닛 고정값으로 바뀜, 기본 0.012)가 새 배 이미지에 실제로 잘 맞는지
      확인 필요 — 특히 (a) 배 크기가 비행기 대비 과하게 크거나 작지 않은지, (b) 윤곽선 두께가 새 이미지
      해상도에서도 기존 이모지 배와 비슷한 두께로 보이는지, (c) 이동 방향(180도 기준각)이 실제로 맞는지.
      상세 내용은 `Docs/DevLog.md` Step 39 참고.

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
