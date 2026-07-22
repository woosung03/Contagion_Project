# World Simulation System 설계 — Contagion Simulation Engine

**목적**: Contagion Project의 세계가 매 Tick마다 스스로 계산되고 변화하는 Simulation Engine을
설계한다. 이 문서는 이후 구현의 Source of Truth다. 코드는 작성하지 않는다 — 순수 게임 디자인·
시스템 설계 문서. `EvolutionSystem2.0_PlagueIncDesignAnalysis.md`/`WorldEventSystem_Design.md`
(선행 두 문서)를 구현하기 위한 **Simulation Layer**를 설계한다.

**조사 방법**: Plague Inc./Frostpunk/RimWorld/Stellaris/Crusader Kings III는 앞선 두 문서에서
이미 이벤트 관점으로 조사했으므로, 이번엔 **계산 구조(어떻게 세계를 계산하는가)** 관점으로
재조사했다. Dwarf Fortress/Oxygen Not Included, Factorio/Victoria 3, Workers & Resources는
신규 조사했다. 동시에 Contagion Project의 실제 코드(`SimulationManager.cs`/`WorldState.cs`/
`Country.cs`/`HumanResistanceManager.cs`/`EventManager.cs`/`GameManager.cs`)를 직접 읽고
이 문서 전체의 근거로 삼았다 — "기존 구조를 최대한 활용" 원칙에 따라, 모든 제안은 실제 필드명·
메서드명을 인용해 현재 구조와의 연결점을 명시한다.

---

## 가장 중요한 철학

```
World Simulation
      ↓
World State 변화
      ↓
World Event 발생
      ↓
Evolution Opportunity 생성
      ↓
Player Decision
      ↓
World 변화
      ↓
다음 Tick
```

이 구조는 절대 역행하지 않는다. Event가 State를 만드는 게 아니라 State가 Event를 만든다 —
`EventManager.HandleTick()`이 이미 이 원칙을 부분적으로 구현하고 있다(`TryTrigger`의
`conditionMet` 인자가 항상 `WorldState`/`Country` 필드 조건이지 순수 난수가 아니다).

---

## 1. Simulation 철학

10개 게임 조사에서 세 가지 철학이 스펙트럼 양 끝과 중간에 위치한다.

- **완전한 충실도(Dwarf Fortress)**: 모든 걸 개별적으로 시뮬레이션한다. 대가는 성능 — 실제
  병목은 경로탐색이 아니라 **개체 간 쌍대(pairwise) 체크**(O(개체수²))라는 게 확인됐다.
- **단순 규칙의 착시(Oxygen Not Included)**: Klei 개발자가 직접 "복잡해 보이는 건 대부분 착시다,
  실제로 돌아가는 시뮬레이션은 거의 없다"고 밝혔다 — 상호 연결된 **단순 규칙**이 깊은 시뮬레이션
  처럼 느껴지게 한다.
- **결정론적 정합성(Factorio)**: 모든 클라이언트가 매 틱 비트 단위로 동일한 결과를 내야 하므로,
  "거의 맞음"이 아니라 "완전히 같음"을 요구한다. 이게 부동소수점 비결정성조차 버그로 취급하게
  만든다.

**Contagion Project가 택할 위치**: ONI에 가깝다. Contagion은 이미 `SimulationManager.RunTick()`이
"단순 공식(사망 = 감염자×치사율×중증도×(1-의료수준))의 조합"으로 설계돼 있고, 이건 옳은 선택이다 —
48개국 규모에서 DF식 완전 충실도는 과설계다. 다만 **결정론적 재현성**(Factorio의 교훈)은
디버깅·QA를 위해 부분적으로 도입한다: 동일 시드로 동일 초기 상태에서 실행하면 동일 결과가
나와야 한다는 원칙을 최소한 지킨다(§ "Deterministic Rule" 참고). Vic3/RimWorld/DF 공통의
**빈도 계층화**(모든 걸 매 틱 계산하지 않는다)도 Contagion 규모에 맞게 축소 적용한다.

---

## 2. Simulation Tick — 현재 구조 (as-is, 코드 직접 확인)

`SimulationManager.RunTick()`의 실제 순서:

1. 국가별 루프 시작 — 각 국가의 `preTickInfected`(틱 시작 시점 스냅샷) 확보
2. **사망 계산**: `preTickInfected × lethality × severity × (1-HealthLevel)` (스냅샷 기준)
3. **국내 신규 감염 계산**: `preTickInfected × (infectivity+보너스) × globalSpreadFactor × 난이도배율
   × (1-effectiveHealthLevel) × climateModifier × ... × densityMultiplier` (**사망 계산과 동일한
   `preTickInfected` 스냅샷 사용** — 사망과 신규감염이 서로의 이번 틱 결과에 영향받지 않고
   **동시 계산**된다)
4. `country.deadCount`/`country.infectedCount` 갱신, 마일스톤 체크, `NotifyCountryChanged`
5. 국가별 루프 종료 후 → `SpreadBetweenCountries()`(육상 국경 전파, Animal 경로 우회)
6. 치료제 연구 시작 확률 판정 → 시작됐으면 `cureProgress` 갱신(펀딩 합산 × visibility 보너스 ×
   난이도 배율 - 약물내성 감쇠)
7. 박멸 판정(`cureProgress>=1`) → `EradicatePathogen()`
8. `plagueVisibility` 갱신
9. `currentDay++`, `RecalculateWorldTotals()`
10. `GameManager.EvaluatePhase()` (페이즈 전환 판정)
11. **`OnTickCompleted` 발행** → 구독자 실행(순서 미보장, 아래 §5 참고)
12. `EvaluateEndConditions()`(승패 판정)

**중요한 기존 설계 결정(이미 올바름)**: 3번의 사망/신규감염 동시 계산은 Stellaris가 4.0에서
"순차 계산이 먼저 처리되는 종을 유리하게 만드는" 버그를 고치기 위해 **나중에** 도입한 "동시
계산" 패턴과 정확히 같은 원리다. Contagion은 이미 이걸 하고 있다 — 유지해야 한다.

**중요한 기존 설계 결함(§5에서 상세)**: `HumanResistanceManager.HandleTick()`과
`EventManager.HandleTick()`이 **둘 다** `OnTickCompleted`를 구독한다. Unity는 서로 다른
컴포넌트의 구독 순서를 보장하지 않으므로, 같은 틱 안에서 정책 갱신(봉쇄 등)과 이벤트 판정
중 어느 쪽이 먼저 실행되는지가 **결정론적이지 않다.** `HumanResistanceManager`가 이미
`OnPolicyApplied`라는 2차 이벤트를 만들어 `BottleneckAnalyzer`용으로 이 문제를 해결한 전례가
있는데, `EventManager`는 아직 그 2차 이벤트를 쓰지 않는다.

---

## 3. World State — 필드 전수 정의

### 3.1 현재 필드 (`WorldState.cs`, 코드 확인)

| 필드 | 타입 | 갱신 위치 |
|---|---|---|
| `totalPopulation` | long | `RecalculateWorldTotals()` |
| `infectedCount` | long | 상동 |
| `deadCount` | long | 상동 |
| `cureProgress` | float(0~1) | `RunTick()` 6번 |
| `plagueVisibility` | float(0~1) | `RunTick()` 8번 |
| `dnaPoints` | int | `UpgradeManager`/버블 획득 |
| `currentDay` | int | `RunTick()` 9번 |
| `cureResearchStarted` | bool | `RunTick()` 6번(1회 전환) |
| `hasEverBeenInfected` | bool | `RecalculateWorldTotals()` |

### 3.2 신규 필요 필드 — `WorldEventSystem_Design.md` 갭 분석

직전 문서의 Event Category 17개 중 **9개는 지금 대응하는 `WorldState`/`Country` 필드가 전혀
없다.** 구현 우선순위(§12)에 직결되는 발견이다.

| Category | 현재 지원 상태 | 부족한 필드 |
|---|---|---|
| MED(의료) | 지원(`cureProgress`) | — |
| POL(정치) | 지원(`Country.isBorderClosed` 등) | — |
| SOC(사회) | 부분 지원(`GetCollapseStage()`) | `governmentStability` 세분화 계기판 없음 |
| SCI(과학) | 지원(`cureProgress`) | — |
| CLM(기후) | 정적 지원(`Country.climate`) | 계절/동적 변화 없음(고정값) |
| **ECO(경제)** | 미지원 | `Country.economyIndex`류 없음, `healthFunding`이 대리 중 |
| **REL(종교)** | 미지원 | 신뢰도/저항 계기판 없음 |
| **DIS(자연재해)** | 임시 지원(EventManager 하드코딩) | 국가별 재해 이력 누적 없음 |
| **INT(국제기구)** | 미지원 | 다국 동시 판정 인프라 없음(팬아웃 메커니즘 자체가 없음) |
| **MIL(군사)** | 부분(governmentStability로 대리) | 계엄 등 전용 상태 없음 |
| **NEWS(언론)** | 미지원 | `plagueVisibility` 하나가 전부 대리 중 |
| **CUL(문화)** | 미지원 | 없음 |
| **TEC(기술)** | 미지원 | `Country.developmentLevel`이 대리 중 |
| TRN(교통) | 지원(`isAirportOpen`/`isPortOpen`/route 리스트) | 그래프 전파 확률 필드 없음 |
| **FOD(식량)** | 미지원 | 없음 |
| **NRG(에너지)** | 미지원 | 없음 |
| **REF(난민)** | 미지원 | 없음 |

**설계 원칙**: 새 필드는 최소 추가한다 — ONI 철학("복잡함은 단순 규칙의 착시")에 따라, ECO/REL/
NEWS/CUL/TEC/FOD/NRG/REF 8개 미지원 카테고리 전부에 전용 필드를 만들지 않는다. 대신 `Country`에
범용 압력 계기판 하나(`Country.socialPressure`, 0~1)를 신설해 SOC/ECO/FOD/NRG/REF가 공유하고,
`WorldState`에 `internationalTension`(0~1) 하나를 신설해 INT/MIL/NEWS가 공유한다 — Vic3의
"pop 병합" 교훈(엔티티 수를 줄이는 게 개별 최적화보다 낫다)을 상태 필드 설계에도 적용한 것이다.

---

## 4. Country State — 필드 전수 정의

### 4.1 현재 필드 (`Country.cs`, 코드 확인)

| 필드/프로퍼티 | 종류 | 비고 |
|---|---|---|
| `id`/`name` | 정적 | — |
| `population` | 정적(게임 중 불변) | — |
| `infectedCount`/`deadCount` | 동적 | 매 틱 갱신 |
| `climate`/`developmentLevel` | 정적 | — |
| `isAirportOpen`/`isPortOpen`/`isBorderClosed` | 동적 플래그 | `HumanResistanceManager` 갱신 |
| `healthFunding` | 동적(0~1) | 매 틱 재계산(캐시 아님) |
| `governmentStability` | 동적(0~1) | 서서히 감소만 함(현재 회복 로직 없음) |
| `healthFundingCap` | 동적(영구 감소만) | 자연재해가 깎음 |
| `neighborCountryIds`/`airRouteCountryIds`/`seaRouteCountryIds` | 정적 그래프 | — |
| `LivingPopulation`/`SusceptibleCount` | 계산 프로퍼티(캐시 없음) | — |
| `HealthLevel`/`ResearchMultiplier` | 계산 프로퍼티(enum 스위치) | — |
| `GetPopulationDensityTier()`/`DensityMultiplier` | 계산 메서드(캐시 없음) | population 불변이라 캐싱 이득 낮음(주석에 이미 명시) |
| `GetCollapseStage()` | 계산 메서드(캐시 없음) | 매 접근마다 재계산 |

### 4.2 신규 필요 필드

| 신규 필드 | 목적 | 근거 |
|---|---|---|
| `Country.socialPressure` (0~1) | SOC/ECO/FOD/NRG/REF 공유 압력 계기판 | §3.2 설계 원칙 |
| `Country.isDiscovered` (bool) | 국가별 발각 여부(현재는 `plagueVisibility` 하나로 전역 판정만 가능 — Civ VI Emergency/Plague Inc. Discovered처럼 국가별 개별 발각이 §5 팬아웃 구조에 필요) | `WorldEventSystem_Design.md` §7.1 POL-01 |
| `Country.governmentStability` 회복 로직 | 현재 감소만 함 — Frostpunk/Civ VI 사례처럼 "회복 가능한 계기판"이어야 Public Order 역주행(`WorldEventSystem_Design.md` §5.2) 재현 가능 | 신규 로직(필드 자체는 기존) |
| `Country.eventHistory` (List<string>, 최근 N개 이벤트 ID) | 체인 이벤트(§4a 사다리, §4c 팬아웃) 판정에 "이 국가에서 이미 무슨 일이 있었는가"가 필요 | `WorldEventSystem_Design.md` §9 체인 예시 다수 |

---

## 5. Dependency Graph

실제 코드에서 확인된 의존 관계를 그래프로 표기한다(`←`는 "이 값이 바뀌면 좌변도 바뀐다"):

```
[국가별, 매 틱]
preTickInfected(스냅샷)
  ├─→ newDeaths          (동시 계산, 서로 미참조)
  └─→ newInfected        (동시 계산, 서로 미참조)
        ↓
infectedCount, deadCount (틱 종료 시점 갱신)
        ↓
totalInfectionRatio(국가 평균) ─→ GameManager.EvaluatePhase()
        ↓
[국가 간, 국가별 루프 종료 후]
SpreadBetweenCountries() ← neighborCountryIds, isBorderClosed(양쪽)
        ↓
[World, 국가 루프 전체 종료 후]
cureProgress ← Σ(healthFunding × ResearchMultiplier) × (1+plagueVisibility×0.5) × 난이도배율
        ↓
plagueVisibility ← infectedRatio(World) × severity × visibilityGainRate
        ↓
[OnTickCompleted 이후 — 순서 미보장 구간]
HumanResistanceManager.ApplyPolicy() ← plagueVisibility, GetCollapseStage()
  → isAirportOpen/isBorderClosed/isPortOpen, healthFunding, governmentStability
        ↕ (순서 미보장!)
EventManager.HandleTick() ← plagueVisibility, cureProgress, deadCount 등
  → country.infectedCount 직접 수정 가능(자연재해 등), healthFundingCap 영구 감소
```

**이 그래프가 알려주는 것**: `EventManager`의 일부 이벤트(`ApplyNaturalDisaster`)가
`country.infectedCount`를 직접 수정하는데, 이게 `HumanResistanceManager.ApplyPolicy()`보다
먼저 실행되면 그 틱의 봉쇄 판정에 영향을 주고, 나중에 실행되면 다음 틱까지 반영이 지연된다 —
**같은 입력에서 다른 결과가 나올 수 있다는 뜻**이고, 이는 Factorio가 "결정론 위반"으로 분류할
종류의 문제다. §9에서 이 순서를 명시적으로 고정하는 것을 최우선 제안으로 다룬다.

---

## 6. Simulation Layer

7개 레이어로 분리한다(기존 클래스에 최대한 매핑):

| Layer | 담당 | 현재 대응 클래스 | 실행 빈도 |
|---|---|---|---|
| 1. Disease | 병원체 고유 수치(읽기 전용) | `Pathogen` | 참조만, 계산 없음 |
| 2. Country | 국가별 감염/사망 계산 | `SimulationManager.RunTick()` 국가 루프 | 매 틱 |
| 3. Society | 국가별 사회 압력(`socialPressure`, 신규) | 신규 — `HumanResistanceManager` 확장 | 매 틱 또는 저빈도(§11) |
| 4. Government | 국가별 정책 반응(봉쇄/펀딩) | `HumanResistanceManager.ApplyPolicy()` | 매 틱 |
| 5. World | 전역 집계(총 인구/치료제/visibility) | `WorldDataManager.RecalculateWorldTotals()`, `RunTick()` 6~8번 | 매 틱 |
| 6. Events | 조건부 이벤트 판정/발동 | `EventManager.HandleTick()` | 매 틱(단, §11에서 세분화 제안) |
| 7. Evolution | 노드 잠금/해금 재평가 | `UpgradeManager` | 이벤트 발생 시에만(상시 아님) |

**레이어 간 원칙**: 상위 레이어(5→6→7)는 하위 레이어(1→2→3→4) 결과를 **읽기만** 하고, 하위
레이어는 상위 레이어를 참조하지 않는다 — 단방향 데이터 흐름(§ "Data Flow"). 현재
`EventManager`가 `country.infectedCount`(Layer 2 소관)를 직접 쓰는 것은 이 원칙 위반이다 —
§9에서 대안(간접 수정 큐)을 제안한다.

---

## 7. Event Evaluation

**현재**: `EventManager.HandleTick()`이 매 틱, 7개 이벤트 전부를 확률+쿨다운으로 검사한다(48개국
규모에서는 부담 없음).

**언제 평가해야 하는가 — 3가지 원칙**:
1. **World 레벨 이벤트**(MED/SCI/INT 등 전역 조건)는 매 틱 평가해도 무방 — 조건 자체가
   `WorldState` 몇 개 필드 비교뿐이라 비용이 극히 낮다(DF의 "매 틱 vs 10틱마다" 구분에서
   "매 틱" 쪽에 해당하는 가벼운 작업).
2. **Country 레벨 이벤트**(48개국 × 17개 카테고리 조건)는 매 틱 전수 검사하면 O(국가수×카테고리)
   = 최대 816회/틱 조건 평가 — 아직 부담은 아니지만(§11 참고), RimWorld의 `TickRare`(250틱마다)
   패턴을 본떠 **국가별 "재평가 필요" 더티 플래그**(해당 국가의 `infectedCount`/`deadCount`/
   `governmentStability`가 전 틱 대비 변했을 때만 true)를 도입해 불필요한 재평가를 건너뛴다.
3. **그래프 전파형(TRN 카테고리, §4d)**은 트리거 국가의 이웃만 평가한다(전 세계 48개국 전수
   순회 아님) — Pandemic Outbreak의 "연결된 도시만" 원칙 그대로.

---

## 8. Evolution Opportunity 생성

Event Evaluation(§7) 직후, Player Decision 이전 시점에 평가한다(철학 파이프라인의 정확한 위치).
`EvolutionSystem2.0_PlagueIncDesignAnalysis.md` §9의 A형(게이트)/B형(비용 연동)/C형(자동 해금)
3분류를 그대로 재사용한다:

- **A형/B형**: `UpgradeManager`가 매 틱이 아니라 **`OnPolicyApplied`/이벤트 발동 직후에만**
  전체 트리 재평가(잠금 사유 재계산) — 노드 45개를 매 틱 스캔할 필요 없음(Layer 7이 "이벤트
  발생 시에만" 실행되는 이유).
- **C형(자동 해금)**: 이벤트 발동 함수 자체가 `UpgradeManager.TryUnlock()`을 직접 호출 —
  기존 `OnNodeUnlocked` 이벤트 체계를 그대로 재사용.

---

## 9. Contagion Simulation Engine 제안

### 9.1 최우선 수정 — `OnTickCompleted` 구독 순서 결정론화

`HumanResistanceManager`와 `EventManager`가 같은 이벤트를 구독해 순서가 미보장인 문제(§2)를
고친다. `HumanResistanceManager`가 이미 만들어둔 `OnPolicyApplied` 패턴을 그대로 확장한다 —
`EventManager`가 `OnTickCompleted`가 아니라 `HumanResistanceManager.OnPolicyApplied`를
구독하도록 바꾸면, "정책 갱신 → 이벤트 판정" 순서가 코드 구조로 강제된다(Victoria 3의 "의존
대상을 명시적으로 선언하는 틱 태스크" 패턴과 동일한 원리 — 순서를 실행 우연에 맡기지 않고
이벤트 체인 자체로 강제).

### 9.2 Layer 3(Society) 신설

`HumanResistanceManager` 내부에 `socialPressure` 계산을 추가한다 — 기존 클래스를 확장하는
것이지 새 매니저를 만들지 않는다("새로운 시스템을 만들기보다 기존 구조를 최대한 활용" 원칙).

### 9.3 EventManager의 간접 수정 큐 도입

`EventManager`가 `country.infectedCount` 등을 직접 수정하는 대신, "이번 틱에 적용할 변경"을
큐에 쌓고 **다음 틱 시작 시점**에 일괄 적용하도록 바꾼다 — Factorio가 인서터/벨트 상호작용에서
"전역 순서를 강제하지 않되, 각 시스템이 자기 완결적인 규칙을 갖게" 한 것과 같은 원리: 이벤트는
"이번 틱 결과에 끼어들지" 않고 "다음 틱의 입력을 바꾼다"는 규칙이 명확해지면 §5의 순서 문제가
근본적으로 사라진다.

### 9.4 Deterministic Rule — 랜덤 사용 원칙

`StochasticRound()`(이미 존재 — 확률적 반올림)처럼, 신규 랜덤 사용은 전부 다음 원칙을 따른다:
**State가 이미 결과를 강하게 결정할 때는 랜덤을 쓰지 않는다.** 예: `MED-01`(병상 부족)의 발동
여부는 `deadCount` 임계값이 결정하고, 랜덤은 오직 "이 틱에 발동하는가"의 확률적 타이밍에만
쓴다(현재 `EventManager.TryTrigger`가 이미 이 원칙을 따르고 있다 — 유지). State가 동률에 가까울
때만(예: 어느 국가가 다음 재해 대상인지, 후보가 여럿일 때) 랜덤을 최종 선택자로 쓴다(현재
`ApplyNaturalDisaster`의 `candidates[Random.Range(...)]` 패턴 — 이미 올바르다).

---

## 10. Tick Pipeline (최종 제안)

```
World Tick 시작
  ↓
[Layer 1] Disease — Pathogen 값 참조(계산 없음)
  ↓
[Layer 2] Country — 국가별 사망/신규감염 동시 계산(기존 유지)
         → SpreadBetweenCountries (그래프 전파, 기존 유지)
  ↓
[Layer 3] Society — 국가별 socialPressure 갱신 (신규)
  ↓
[Layer 4] Government — HumanResistanceManager.ApplyPolicy
         (봉쇄 사다리, 펀딩, governmentStability 증감)
  ↓
[Layer 5] World — RecalculateWorldTotals, cureProgress, plagueVisibility,
         currentDay++, GameManager.EvaluatePhase
  ↓
  OnPolicyApplied 발행
  ↓
[Layer 6] Events — EventManager (OnPolicyApplied 구독, 순서 보장됨)
         → 변경사항은 "다음 틱 적용 큐"에 적재(§9.3)
  ↓
[Layer 7] Evolution — Event가 실제로 발동했을 때만 UpgradeManager 재평가
         (Evolution Opportunity 생성, §8)
  ↓
Player Decision (다음 실제 프레임들 동안, 틱과 비동기)
  ↓
EvaluateEndConditions (승패 판정)
  ↓
다음 Tick (적용 큐 반영이 여기서 실제로 State에 반영됨)
```

**기존 대비 변경점 요약**: (1) `OnTickCompleted` 단일 이벤트를 구독하던 두 매니저를
`OnPolicyApplied` 체인으로 직렬화, (2) Society 레이어 신설, (3) Event의 State 수정을 다음 틱
적용으로 지연, (4) Evolution 재평가를 "이벤트 발동 시에만"으로 제한(매 틱 전체 스캔 금지).

---

## 11. Performance 전략

Contagion은 48개국 고정 규모라 DF/Factorio 수준의 대규모 최적화는 불필요하지만, 10개 게임
조사에서 나온 원칙 중 **규모와 무관하게 적용 가능한 것들**을 선별 적용한다.

1. **엔티티 수 자체를 줄인다(Victoria 3 pop 병합 원리)** — Contagion은 국가 48개가 고정이라
   "병합"할 대상이 없다. 대신 이 교훈은 §3.2의 "신규 필드를 8개 카테고리마다 따로 안 만들고
   `socialPressure` 하나로 묶는다"는 결정에 이미 반영했다.
2. **실제 병목은 예상 밖의 곳에 있다(DF 교훈)** — DF의 진짜 병목은 경로탐색이 아니라 개체 간
   쌍대 체크였다. Contagion에서 이에 대응하는 위험 지점은 **TRN 카테고리 그래프 전파**
   (`WorldEventSystem_Design.md` §4d) — 48개국이 서로의 이웃 봉쇄 확률에 영향을 주는 구조라,
   구현을 잘못하면 O(국가수²)가 될 수 있다. 반드시 `neighborCountryIds`/`airRouteCountryIds`
   그래프를 따라 **연결된 국가만** 순회하도록 제한한다(이미 `SpreadBetweenCountries()`가 이
   원칙을 따르고 있다 — TRN 신규 구현도 이 패턴을 그대로 복제).
3. **더티 플래그로 불필요한 재계산 생략(DF/Factorio 공통)** — `GetCollapseStage()`/
   `DensityMultiplier`는 현재 매 접근마다 재계산되는데, 48개국 규모에서는 문제없다(주석에
   이미 명시된 판단, 유지). 다만 §7에서 제안한 "국가별 재평가 필요 더티 플래그"는 이벤트
   조건 검사(48국 × 17카테고리)에는 적용할 가치가 있다 — 이쪽은 순수 계산이 아니라 조건 분기가
   많아 비용이 다른 성격이기 때문.
4. **동시 계산으로 순서 편향 제거(Stellaris pop growth 교훈)** — 이미 §2에서 확인했듯
   Contagion은 이미 이 원칙을 지키고 있다. 신규 코드(Society/Government 레이어)를 작성할 때도
   "먼저 계산된 국가가 유리해지는" 순차 구조를 피한다.
5. **결정론은 필요한 곳에만(Factorio 교훈, 단 전면 적용 아님)** — Contagion은 멀티플레이가
   없으므로 Factorio 수준의 엄격한 lockstep 결정론은 불필요하다. 다만 QA 재현성을 위해 "같은
   시드로 같은 초기 조건 실행 시 같은 결과"라는 약한 결정론은 유지 가치가 있다 — 신규 랜덤
   사용처는 전부 `UnityEngine.Random`(전역 시드 하나) 통해서만 굴리고, 별도 `System.Random`
   인스턴스를 산발적으로 만들지 않는다(현재 코드가 이미 이 원칙을 지키고 있음 — 유지).

---

## 12. 구현 우선순위

`EvolutionSystem2.0`/`WorldEventSystem_Design.md`의 우선순위 체계와 정합시킨다.

### Tier 0 — 버그 수정 (신규 기능 아님, 최우선)
1. **`OnTickCompleted` 이중 구독 결정론화**(§9.1) — 지금 당장도 재현 안 되는 버그를 만들 수
   있는 실제 결함이라, 신규 시스템보다 먼저 고친다.
2. **`EventManager`의 직접 State 수정 → 큐 방식 전환**(§9.3) — 위 수정과 세트로 처리.

### Tier 1 — Layer 신설 없이 가능
3. **Society 레이어를 `HumanResistanceManager` 확장으로 신설**(§9.2) — 새 매니저 없이 기존
   클래스 확장.
4. **`Country.socialPressure`/`isDiscovered`/`eventHistory` 필드 추가**(§4.2) — 데이터만
   추가, 로직은 이후 단계.

### Tier 2 — World Event System과 연동
5. **국가별 더티 플래그 이벤트 재평가**(§7) — `WorldEventSystem_Design.md`의 102개 이벤트를
   실제로 얹기 전에 이 최적화 골격부터 마련.
6. **`WorldState.internationalTension` 신설** — INT/MIL/NEWS 카테고리 착수 전제 조건.

### Tier 3 — Evolution System과 연동
7. **Evolution 재평가를 "이벤트 발동 시에만"으로 제한**(§8, §10 Layer 7) — `LockReason()`
   "관측 필요" 잠금 사유(`EvolutionSystem2.0` §9-D41)와 동시 착수 권장.

### Tier 4 — 장기
8. `governmentStability` 회복 로직(§4.2) — Public Order 역주행 재현, MED/POL/SOC 사다리가
   안정화된 후 착수.
9. Layer 1~7 전체가 자리 잡은 뒤에야 `WorldEventSystem_Design.md`의 나머지 카테고리(CUL/REL/
   TEC/FOD/NRG/REF)를 순차 도입 — 이미 그 문서 §10 Tier 4와 동일한 결론.

---

**참고 문서**: `Docs/Archive/EvolutionSystem2.0_PlagueIncDesignAnalysis.md`,
`Docs/Archive/WorldEventSystem_Design.md`(이 문서가 구현 대상으로 삼는 Event 목록의 출처),
`Docs/GameDesignDocument.md`(4절 시뮬레이션 로직 원본), 실제 코드
`Assets/Scripts/Managers/SimulationManager.cs`/`HumanResistanceManager.cs`/`EventManager.cs`/
`GameManager.cs`, `Assets/Scripts/Data/WorldState.cs`/`Country.cs`(이 문서 전체의 as-is 근거).
