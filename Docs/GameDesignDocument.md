# 전염병 주식회사 클론 게임 - AI 파이프라인 설계 문서
**Unity 모바일 (iOS / Android) 인앱토스 타겟**

---

## 1. 게임 개요

| 항목 | 내용 |
|------|------|
| 장르 | 전략 시뮬레이션 |
| 엔진 | Unity (모바일 최적화) |
| 플랫폼 | iOS / Android |
| 수익 모델 | 광고 (배너 광고 + 보상형 광고) |
| 핵심 루프 | 전염병 진화 → 전파 → 인류 저항 → 인류 전멸 or 치료제 완성 |
| 승리 조건 | 전 세계 인구 전멸 |
| 패배 조건 | 감염자 0명 + 생존자 존재 (치료제 완성 후 박멸) |

---

## 2. 핵심 게임플레이 루프

```
[게임 시작]
    ↓
[병원체 종류 선택] → 세균 / 바이러스 / 곰팡이 / 기생충 / 나노바이러스 등
    ↓
[발원 국가 선택] → 지도에서 터치
    ↓
[실시간 시뮬레이션 진행]
    │
    ├─ DNA 포인트 수집 (감염자/사망자 발생 시 팝업 탭 → 수집)
    │
    ├─ 트리 업그레이드
    │   ├─ 감염 경로 (공기/수질/접촉/동물/곤충/혈액 등)
    │   ├─ 증상 (기침/구토/신부전/출혈열/장기부전 등)
    │   └─ 능력 (환경 내성/약물 내성/치료제 저항 등)
    │
    ├─ 인류 저항 진행 (자동)
    │   ├─ 의료 연구 진행률 (%)
    │   ├─ 국가별 방역 조치 (국경 봉쇄/격리/검역)
    │   └─ 치료제 개발 속도 증가
    │
    └─ 뉴스 피드 (세계 상황 텍스트 이벤트)
    
[엔딩]
    ├─ 승리: 인류 전멸 스코어 계산 (날짜 / 바이오하자드 점수)
    └─ 패배: 게임 오버 화면
```

---

## 3. 핵심 데이터 구조

### 3.1 WorldState (전역 세계 상태)
```
WorldState
├── totalPopulation: long          // 전 세계 총 인구
├── infectedCount: long            // 현재 감염자 수
├── deadCount: long                // 누적 사망자 수
├── cureProgress: float (0~1)      // 치료제 개발 진행률
├── plagueVisibility: float (0~1)  // 전염병 노출도 (높을수록 저항 강화)
├── dnaPoints: int                 // 플레이어 보유 DNA 포인트
└── currentDay: int                // 경과 일수
```

### 3.2 Country (국가)
```
Country
├── id: string
├── name: string
├── population: long
├── infectedCount: long
├── deadCount: long
├── climate: enum (Arid/Temperate/Cold/Humid)
├── developmentLevel: enum (Low/Mid/High)  // 의료 수준
├── isAirportOpen: bool
├── isPortOpen: bool
├── isBorderClosed: bool
├── healthFunding: float           // 치료제 기여도
└── governmentStability: float     // 0=무정부, 1=안정
```

### 3.3 Pathogen (병원체)
```
Pathogen
├── type: enum (Bacteria/Virus/Fungus/Parasite/Nano/Prion)
├── infectivity: float             // 감염력
├── severity: float                // 중증도 (높을수록 가시성↑)
├── lethality: float               // 치사율
├── drugResistance: float
├── environmentResistance: float[]  // [Cold, Hot, Humid, Arid]
└── transmissionRoutes: List<TransmissionRoute>
```

### 3.4 UpgradeTree (업그레이드 트리)
```
UpgradeNode
├── id: string
├── category: enum (Transmission/Symptom/Ability)
├── cost: int                      // DNA 포인트 비용
├── prerequisites: List<string>    // 선행 노드 id
├── isUnlocked: bool
└── effects: Dictionary<string, float>  // stat 변화량
```

---

## 4. 시뮬레이션 로직

### 4.1 감염 전파 계산 (매 틱)
```
[국내 전파]
newInfected = (infectedCount × infectivity × spreadFactor)
             × (1 - countryHealthLevel)
             × climateModifier

[국가 간 전파]
- 항공: 항공 노선 존재 + Airport Open → 확률적 감염자 이동
- 해운: 항구 존재 + Port Open → 확률적 감염자 이동
- 육상 국경: 인접 국가 감염 시 border 상태에 따라 이동
```

### 4.2 사망자 계산
```
newDeaths = infectedCount × lethality × severityFactor
            × (1 - healthcareCapacity)
```

### 4.3 치료제 개발 속도
```
cureIncreasePerTick = Σ(국가별 healthFunding × researchMultiplier)
                      × (1 + plagueVisibility × 0.5)
                      - drugResistanceReduction
```

### 4.4 DNA 포인트 획득
```
- 감염자 수 임계값 돌파 시 버블 팝업 생성 (플레이어 탭으로 수집)
- 사망자 발생 시 추가 버블
- 특정 이벤트 트리거 시 보너스 DNA
```

---

## 5. 인류 저항 시스템 (AI)

```
저항 단계 (plagueVisibility 기준)

0.0 ~ 0.2  | 인식 없음 → 정상 생활
0.2 ~ 0.4  | 질병 보도 → 마스크 착용, 손씻기 캠페인
0.4 ~ 0.6  | 공중보건 비상사태 → 격리 시작, 연구 가속
0.6 ~ 0.8  | 국가 비상사태 → 국경 봉쇄, 항공/항구 폐쇄
0.8 ~ 1.0  | 세계 붕괴 → 무정부 상태, 치료제 연구 감속
```

국가별 방역 행동 트리거:
- 선진국 (High): 빠른 봉쇄, 높은 연구 기여도
- 개발도상국 (Mid): 느린 봉쇄, 낮은 연구 기여도
- 저개발국 (Low): 봉쇄 없음, 연구 기여도 거의 없음

---

## 6. 전염병 종류 (콘텐츠 / 인앱토스 연계)

| 종류 | 특징 | 해금 방식 |
|------|------|----------|
| 세균 (Bacteria) | 기본, 모든 환경 적응 가능 | 기본 제공 |
| 바이러스 (Virus) | 돌연변이 랜덤 발생, 빠른 확산 | 기본 제공 |
| 곰팡이 (Fungus) | 초기 전파 느림, 포자 특수 스킬 | 기본 제공 |
| 기생충 (Parasite) | 무증상 전파, 치사율 낮음 | IAP or 해금 |
| 나노바이러스 (Nano) | 치료제가 자동 개발됨, 고난이도 | IAP or 해금 |
| 생물무기 (Bio-Weapon) | 치사율 최고, 컨트롤 필수 | IAP |
| 좀비 바이러스 (Necroa) | 좀비 전투 시스템 추가 | IAP |
| 뇌신경 기생충 (Neurax) | 행동 조작, 다른 승리 조건 | IAP |

---

## 7. UI 구조

### 7.1 메인 게임 화면
```
┌─────────────────────────────┐
│  [뉴스 피드 텍스트 스크롤]        │  ← 상단
│                             │
│     [세계 지도]                │  ← 중앙 메인
│   (국가별 색상 감염 표시)        │
│   (버블 팝업 DNA 수집)          │
│                             │
│  [하단 HUD]                   │
│  감염자 | 사망자 | 치료제%       │
│  [전파] [증상] [능력] 탭 버튼    │
└─────────────────────────────┘
```

### 7.2 업그레이드 화면 (탭 전환)
```
┌────────────────────────────┐
│  DNA 포인트: [XXX]           │
│                            │
│  [노드 트리 스크롤 뷰]          │
│  ○─○─○  (감염 경로)          │
│     └─○  (증상)             │
│        └─○ (능력)           │
│                            │
│  [선택 노드 설명 + 구매 버튼]   │
└────────────────────────────┘
```

### 7.3 국가 정보 팝업 (지도 터치 시)
```
국가명 / 인구 / 감염자 / 사망자
의료 수준 / 기후 / 방역 상태
항공·항구·국경 상태 아이콘
```

---

## 8. 이벤트 시스템

게임 중 텍스트 뉴스로 표시되는 랜덤/조건부 이벤트:

```
이벤트 타입
├── 긍정 이벤트 (병원체 유리)
│   ├── 자연재해 (홍수/지진) → 감염 가속
│   ├── 정치 불안 → 방역 약화
│   └── 의료 파업 → 치료제 연구 감속
│
└── 부정 이벤트 (인류 유리)
    ├── WHO 긴급 회의 → 치료제 가속
    ├── 국제 공조 → 봉쇄 강화
    └── 백신 임상 성공 → 치료제 진행률 점프
```

이벤트 트리거 조건: 특정 감염률 / 사망자 수 / 치료제 진행률 임계값 도달

---

## 9. 난이도 시스템

| 난이도 | 설명 |
|--------|------|
| Casual | 치료제 속도 느림, 방역 약함 |
| Normal | 기본값 |
| Brutal | 치료제 빠름, 선진국 즉시 봉쇄 |
| Mega Brutal | 극한 방역, 섬 국가 사실상 불가침 |

---

## 10. 게임 진행 단계 (페이즈)

### Phase 1 - 잠복기 (Incubation)
- 무증상 전파 구간
- 플레이어 전략: 감염 경로 먼저 올리고, 증상은 억제
- 인류: 전염병 미인식

### Phase 2 - 확산기 (Spread)
- 전 세계 대부분 국가 감염
- 인류: 연구 시작, 방역 강화
- 플레이어 전략: DNA 포인트 대량 수집 후 치사율 올리기

### Phase 3 - 결정기 (Endgame)
- 치료제 vs 치사율 레이스
- 미감염 국가 (뉴질랜드, 아이슬란드 등) 처리가 핵심
- 플레이어 전략: 약물 내성 / 환경 내성 강화

---

## 11. 점수 시스템 (바이오하자드 점수)

```
기본 점수: 생존일 / 클리어 날짜 기반
보너스 요소:
  - 사용한 업그레이드 수 (효율성)
  - 난이도 배율
  - 특수 조건 달성 (뉴스 언급 없이 클리어 등)
바이오하자드 별점: 1~3개
```

---

## 12. Unity 구현 구조 (씬 / 매니저)

```
Scenes
├── MainMenu         ← 타이틀, 병원체 선택
├── CountrySelect    ← 발원지 선택 (지도 터치)
└── GamePlay         ← 메인 게임

Core Managers (DontDestroyOnLoad)
├── GameManager          ← 게임 상태, 페이즈 관리
├── SimulationManager    ← 틱 기반 감염/사망 계산
├── WorldDataManager     ← 국가 데이터, WorldState
├── UpgradeManager       ← DNA 포인트, 트리 상태
├── EventManager         ← 랜덤 이벤트 발생
├── UIManager            ← HUD, 팝업 제어
└── SaveManager          ← 게임 저장/로드

Game Objects
├── WorldMap             ← 국가 클릭, 색상 업데이트
├── BubbleSpawner        ← DNA 수집 버블 생성
├── NewsFeed             ← 상단 뉴스 텍스트 스크롤
└── UpgradeTreeView      ← 노드 UI 렌더링
```

---

## 13. 광고 수익 모델 설계 (보상형 광고 전용)

앱인토스 정책상 광고는 사용자가 예상 가능한 시점에만 노출 가능. 보상형 광고(Rewarded Ad)만 사용.

| 노출 시점 | 보상 내용 | 조건 |
|-----------|----------|------|
| 업그레이드 화면에서 선택 시청 | DNA 포인트 +10 | 플레이어가 버튼 직접 탭 |
| 게임 오버 후 부활 선택 시 | 현재 진행 상태로 재개 | 패배 직후 1회 한정 |
| 메인 메뉴에서 선택 시청 | DNA 포인트 +5 (다음 판 시작 전) | 무제한 |

- 광고는 반드시 **사전 로딩** 후 재생 (앱인토스 정책)
- 광고 재생 중 게임 BGM 일시정지 → 광고 종료 후 자동 재생 (앱인토스 정책)
- 보상은 광고를 끝까지 시청한 경우에만 지급

## 14. 랭킹 시스템

### 랭킹 기준 지표
```
Score = (바이오하자드 점수) × (난이도 배율) × (클리어 속도 보너스)

바이오하자드 점수 구성:
  - 클리어 날짜 (빠를수록 고점)
  - 사용 업그레이드 수 (적을수록 효율 보너스)
  - 병원체 종류별 기본 배율

난이도 배율:
  Casual × 1.0 / Normal × 1.5 / Brutal × 2.0 / Mega Brutal × 3.0
```

### 랭킹 구조
```
랭킹 종류
├── 전체 랭킹      - 모든 병원체 / 모든 난이도 통합
├── 병원체별 랭킹  - 세균 / 바이러스 / 곰팡이 각각
└── 주간 랭킹      - 매주 초기화, 상위권 보상
```

### 앱인토스 연동
- 앱인토스 사용자 식별키(userHashKey)로 유저 식별
- 랭킹 데이터는 서버(Firebase Firestore 또는 Supabase) 저장
- WebSocket 사용 시 `wss://` 암호화 연결 필수 (앱인토스 정책)

### Unity 구현
```
RankingManager
├── SubmitScore(score, plagueType, difficulty)  // 클리어 시 점수 전송
├── FetchRankings(type, page)                   // 랭킹 조회
├── GetMyRank()                                 // 내 순위 조회
└── RankingUI                                   // 랭킹 팝업 화면
```

---

## 14. AI 파이프라인 구현 순서 (권장)

```
Step 1  WorldState / Country / Pathogen 데이터 클래스 정의
Step 2  SimulationManager - 감염/사망 틱 계산 로직
Step 3  WorldMap - 국가 데이터 바인딩 + 색상 표시
Step 4  UpgradeManager - DNA 포인트 + 트리 노드
Step 5  BubbleSpawner - DNA 수집 팝업
Step 6  인류 저항 AI - 방역/치료제 자동 진행
Step 7  EventManager - 뉴스 피드 + 이벤트
Step 8  UI 전체 연결 (HUD, 팝업, 엔딩)
Step 9  ScriptableObject로 병원체/국가 데이터화
Step 10 보상형 광고 SDK 연동 (앱인토스 AdMob)
Step 11 랭킹 서버 연동 (Firebase / Supabase) + RankingManager
Step 12 랭킹 UI (전체 / 병원체별 / 주간) + 내 순위 표시
Step 13 저장 시스템 (앱인토스 userHashKey 기반)
```

---

## 15. 주요 참고 포인트

- 원작은 **유니티 엔진** 개발 (동일 스택)
- 시뮬레이션은 **실시간이지만 틱 기반** (Update마다 처리 X → InvokeRepeating 또는 코루틴)
- 국가 지도는 **2D 폴리곤 또는 스프라이트** 방식 권장
- DNA 버블은 **오브젝트 풀링** 필수 (성능)
- 모바일 특성상 **세션 저장 필수** (백그라운드 종료 대응)

---

## 16. 튜닝 포인트 (실제 구현값 참고, CLAUDE.md에서 이동)

밸런스 수치를 조정할 때 시작점으로 참고하는 실제 구현 파라미터 목록. 값 자체의 변경 히스토리나
조정 이유는 `Docs/DevLog.md`에 있다.

- `climateModifier` = `Pathogen.GetEnvironmentResistance(climate)`
- `healthcareCapacity` = `Country.HealthLevel` (Low 0.2 / Mid 0.5 / High 0.8)
- `researchMultiplier` = `Country.ResearchMultiplier` (Low 0.2 / Mid 0.8 / High 1.5)
- `severityFactor` = `Pathogen.severity`
- `drugResistanceReduction` = `pathogen.drugResistance × drugResistanceCoefficient` (SimulationManager)
- `spreadFactor`/`landBorderSpreadChance`/DNA 마일스톤 간격/저항 단계 임계값 — SimulationManager·
  HumanResistanceManager 인스펙터 값, 플레이테스트로 조정
- 항공/해운 전파는 `TransportManager` 인스펙터 값 사용 (`SimulationManager`의
  `airRouteSpreadChance`/`seaRouteSpreadChance`는 미사용 필드)
- 인구 스케일링 없음 — `totalPopulation` 등 국가별 인구는 실제 인구 수를 그대로 사용
  (밸런스 조정 시 축소 스케일 적용 여부 논의 필요, CLAUDE.md에서 이동)
- `Cure Progress Coefficient`(기본 0.002) — 국가 48개 기준 재조정 필요 시 여기부터
