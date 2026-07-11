# Research Database 런타임 시스템 설계 (Runtime Systems Architecture)

`ResearchDatabase_Design.md`(철학·효과 설계 원칙) / `UpgradeTree_ResearchDatabase_NodeMapping.md`
(45개 노드 전수 매핑, Stat 7·Mechanic 5·Hybrid 33 분류) / `UpgradeTree_FinalDecision.md`(현재
구현 확정판)가 전제다. 세 문서 모두 "이 연구가 무엇을 의미하는가"를 다뤘고, 실제로 **그 효과가
`SimulationManager`/`Country`/`Pathogen`/`WorldState` 중 어디에 어떻게 연결되는가**는 아직
아무도 답하지 않았다 — 이 문서가 그 질문에 답한다.

**순수 설계 문서다. 코드/UXML/구현 없음.** 필드명·메서드명은 현재 코드(`Assets/Scripts/Data`,
`Assets/Scripts/Managers`)를 근거로 정확히 인용하되, 실제 선언·구현은 전부 별도 세션의 범위다.
향후 연구가 45개에서 100개 이상으로 늘어나도 매니저 쪽 코드가 노드 개수에 비례해 복잡해지지
않는 구조를 만드는 것이 이 문서의 핵심 목표다.

---

## 0. 문서 성격과 범위

이 문서는 게임 디자인 문서가 아니라 **아키텍처 문서**다. `ResearchDatabase_Design.md`가 "이
연구는 무엇을 상징하는가"를 다뤘다면, 이 문서는 "그 상징을 실제로 계산에 반영하려면 어느
클래스의 어느 메서드가 무엇을 읽어야 하는가"를 다룬다. 두 문서는 서로 대체하지 않고 겹치지도
않는다 — 이 문서는 `NodeMapping.md`가 이미 확정한 45개 노드의 이름·선행조건·비용을 전혀
재검토하지 않고, 오직 "그 노드들의 `effects`가 런타임에서 어떤 자료구조로 흘러가는가"만 다룬다.

---

## 1. 현재 시뮬레이션 구조 분석

### 1.1 데이터 계층

| 클래스 | 역할 | 연구와 관련된 필드 |
|---|---|---|
| `WorldState` | 전 세계 공용 상태(감염자/사망자/치료제 진행률/발각도/DNA/경과일) | 직접 연구 대상 필드 없음 — `cureProgress`/`plagueVisibility`는 매 틱 계산 결과일 뿐 |
| `Country` | 국가별 상태(인구/감염자/사망자/기후/개발수준/봉쇄 3종/연구기여도/정부안정성) | `isAirportOpen`/`isPortOpen`/`isBorderClosed`/`governmentStability`/`healthFunding` — 전부 `HumanResistanceManager`가 매 틱 갱신하는 **정책 상태**, 연구가 직접 쓰는 필드 아님 |
| `Pathogen` | 병원체 스탯(감염력/중증도/치사율/약물내성) + `environmentResistance` 리스트 + `transmissionRoutes` | `infectivity`/`severity`/`lethality`/`drugResistance` 4개가 현재 유일한 연구 반영 지점 |
| `UpgradeNode` | 연구 노드(id/category/cost/prerequisites/`effects: List<UpgradeEffectEntry>`) | `effects`의 `statName`이 위 4개 필드 이름과 문자열로 매칭됨 |

핵심 관찰: **연구가 건드릴 수 있는 것은 현재 `Pathogen`의 4개 float 필드뿐이다.** `Country`의
정책 상태, `WorldState`의 세계 상태, `Pathogen.environmentResistance`(이미 존재하는데도) 중
어느 것도 연구 해금과 연결돼 있지 않다.

### 1.2 매니저 계층 (책임 분리)

| 매니저 | 책임 | 연구 시스템과의 현재 관계 |
|---|---|---|
| `SimulationManager` | 틱 루프. 국내 감염/사망 계산, 육상 국가간 전파, 치료제 진행, 발각도 갱신, 종료조건 판정 | `pathogen.infectivity/severity/lethality/drugResistance/environmentResistance`를 매 틱 읽음 — 4개 스탯은 이미 소비 중, `environmentResistance`는 읽기만 하고 아무도 안 씀 |
| `UpgradeManager` | DNA 관리, 노드 해금, `ApplyEffectsToPathogen()`으로 노드 `effects`를 `Pathogen`에 반영 | **연구 효과의 유일한 "쓰기" 지점** — 이 문서가 확장할 핵심 지점 |
| `HumanResistanceManager` | `plagueVisibility`/국가별 붕괴 단계에 따라 `isAirportOpen`/`isPortOpen`/`isBorderClosed`/`healthFunding`/`governmentStability` 갱신 | 연구와 전혀 연결 안 됨 — Mechanic Unlock류(격리 내성, 봉쇄 지연) 대부분이 이 매니저를 건드려야 함 |
| `TransportManager` | 항공/해운 유닛의 실제 이동·도착 판정, 도착 시 감염 이식. 경로 개폐는 `isAirportOpen`/`isPortOpen` 양쪽 국가 AND로 판정(431~433행) | 연구와 전혀 연결 안 됨 — "항구 폐쇄돼도 감염 유지"류 연구는 이 매니저의 경로 판정 지점을 반드시 거쳐야 함 |
| `EventManager` | `OnTickCompleted` 구독, 조건+확률+쿨다운(`TryTrigger`)으로 뉴스 이벤트 발동 | 연구와 무관하지만, **패턴 자체는 Event Modifier 설계에 그대로 재사용 가능**(§2.1, §8) |
| `GameManager` | 페이즈/난이도 배율 | 난이도 배율과 연구 배율이 같은 계산식(`RunTick()`의 `cureIncrease`, `newInfected`)에 곱해지므로 상호작용 지점으로만 남음, 별도 연결 불필요 |

### 1.3 현재 연구→시뮬레이션 연결 지점과 공백

```
UpgradeNode.effects (statName, amount)
        │
        ▼  UpgradeManager.ApplyEffectsToPathogen()  ← 유일한 쓰기 지점
        │
        ▼
Pathogen.infectivity / severity / lethality / drugResistance   ← 4개 필드만 존재
        │
        ▼  SimulationManager.RunTick()이 매 틱 읽음   ← 유일한 읽기 지점(사실상)
```

`NodeMapping.md`가 확정한 45개 노드의 73%(Hybrid)·11%(Mechanic)는 이 파이프라인 밖에 있는
효과를 요구한다 — 국경/항구/공항 게이트 우회, 재감염 풀, 치료제 역전, 격리 누수, 기후 저항
등은 전부 `Pathogen`의 4개 필드로 표현 불가능하다. 이 문서의 §2~§8은 이 파이프라인을
"4개 필드 가산"에서 "네 가지 효과 타입을 각자의 소비처로 흘려보내는 구조"로 넓히는 설계다.

---

## 2. 연구 효과 분류 체계

### 2.1 네 가지 효과 타입 정의

| 타입 | 정의 | 무엇을 바꾸는가 | `NodeMapping.md` 실사례 |
|---|---|---|---|
| **Stat Modifier** | 병원체의 정적 수치를 가산/감산 | `Pathogen`의 float 필드 값 자체 | 감염력/치사율/중증도/약물내성 4종(기존 그대로) |
| **Simulation Modifier** | 시뮬레이션이 계산하는 "경로"·"게이트" 자체를 바꾼다 | 국내/국가간 전파가 어떤 조건에서 발생하는지 — 기존에 계산 안 하던 경로를 계산하게 하거나, 기존 게이트(봉쇄/폐쇄)를 무력화·완화 | 조류 매개 전파(고립국 자연감염), 에어로졸 광역 부유(국경봉쇄 우회), 설치류 매개 확산(육상봉쇄 우회) |
| **State Modifier** | 국가/세계의 "상태가 파생되는 계산식"을 바꾼다 — 원본 상태값(`isBorderClosed` 등)은 안 건드리고, 그 상태가 만들어내는 실효값에 보정을 가함 | `HealthLevel`의 실효치, `climateModifier`, `governmentStability` 상승 속도 등 | 수혈 전파(의료부하 임계 배가), 내한성(기후 페널티 제거), 숙주 내 잠행(정부안정성 상승 지연) |
| **Event Modifier** | 조건 충족 시 확률적으로 "한 번" 발동하는 규칙 — 기존 `EventManager.TryTrigger` 패턴과 동일 | `WorldState.cureProgress`를 역행시키거나, 박멸된 국가에 재감염 풀을 되살리는 등 단조 증가/감소를 깨는 변화 | 항원 변이(백신 무력화 이벤트), 세포벽 강화(재감염 풀 복귀) |

### 2.2 타입별 소비 매니저 매핑

| 효과 타입 | 쓰기(집계) | 읽기(소비) |
|---|---|---|
| Stat Modifier | `UpgradeManager.ApplyEffectsToPathogen()` (기존 그대로) | `SimulationManager.RunTick()` 전체 계산식(기존 그대로) |
| Simulation Modifier | `UpgradeManager` (노드 해금 시 `Pathogen`의 능력 집합에 등록) | `SimulationManager.SpreadBetweenCountries()`/`TrySpreadRoute()`, `TransportManager`의 경로 개폐 판정(431~433행) |
| State Modifier | `UpgradeManager` (동일) | `SimulationManager.RunTick()`의 사망/감염 계산식 내부(§7), `HumanResistanceManager.ApplyPolicy()` |
| Event Modifier | `UpgradeManager` (동일, 다만 "발동 조건"만 등록하고 실제 굴림은 별도 루프) | 신규 루프 또는 `EventManager` 확장(§8) |

### 2.3 복합 효과(Hybrid)

`ResearchDatabase_Design.md` §10.3과 `NodeMapping.md` 통계(Hybrid 73%)가 이미 못박았듯,
한 연구는 보통 Stat + (Simulation|State|Event) 중 하나를 동시에 가진다. 따라서 `UpgradeNode`
한 개의 `effects`는 위 네 타입의 항목을 **여러 개 섞어서 가질 수 있어야 한다** — 이는 지금
`effects: List<UpgradeEffectEntry>`가 이미 리스트라는 점과 방향이 같다. 즉 데이터 모델 확장은
"새 필드 하나 추가"가 아니라 "리스트가 담을 수 있는 항목 종류를 4가지로 넓히는" 방향이 된다
(구체적 자료구조는 §8에서 다룬다).

---

## 3. unlockedFlags 설계

### 3.1 표현 형태 후보 검토

| 형태 | 장점 | 단점 |
|---|---|---|
| (a) 자유 문자열 리스트(`List<string>`) | 신규 플래그 추가 시 코드 재컴파일 불필요, 데이터만 늘리면 됨 | 오타/중복에 취약 — 소비 코드가 매직 스트링을 직접 비교해야 함 |
| (b) `[Flags]` 열거형 비트마스크 | 컴파일 타임 안전, 조회 연산이 비트 AND 하나로 끝남 | 노드 100개 이상 확장 목표와 정면 충돌 — enum은 32/64비트가 물리적 한계, 신규 플래그마다 enum 재컴파일 필요 |
| (c) `HashSet<string>` + 중앙 상수 정의 | (a)의 유연성 유지 + 상수 클래스를 양쪽(데이터 정의·소비 코드)이 함께 참조해 오타를 컴파일 타임에 잡음 | 상수 클래스 유지관리 비용(신규 플래그 추가 시 상수 1줄 추가를 잊으면 안 됨) — 다만 이는 (b)의 enum 유지관리 비용보다 훨씬 낮음 |

### 3.2 권장안

**(c) `HashSet<string>` + 중앙 상수 정의**를 권장한다. 이유는 두 가지다.

1. **확장성**: 목표(45→100+)를 만족하는 유일한 형태다. (b)는 애초에 상한이 있고, (a)는 상한은
   없지만 안전장치가 전혀 없다.
2. **기존 패턴과의 일관성**: 지금 `UpgradeEffectEntry.statName`도 이미 (a)와 동일한 "문자열
   키 + switch 소비" 패턴이다(`ApplyEffectsToPathogen`의 `case "infectivity":` 등). 중앙
   상수 클래스를 추가하는 것은 기존 패턴을 깨지 않으면서 안전성만 보강하는 최소 변경이다.

다만 (c)만으로는 부족하다 — `NodeMapping.md`의 실제 사례(§6 표)를 보면 거의 전부 "낮은
확률로", "50% 감산" 같은 **강도(magnitude)** 를 동반한다. 순수 on/off 플래그로는 "국경봉쇄가
완전히 무력화되는지, 5%만 새는지"를 표현할 수 없다. 따라서 `unlockedFlags`는 실제로는 두
계층으로 나뉜다.

- **규칙 집합**(`HashSet<string>`) — 어떤 규칙이 열려 있는가.
- **강도 사전**(`Dictionary<string, float>`) — 그 규칙이 얼마나 강하게 작동하는가. 키는
  규칙 이름과 동일한 문자열을 재사용해 두 자료구조가 항상 짝을 이루게 한다.

이 두 계층 모두 `Pathogen`에 귀속시킨다(`Country`나 `WorldState`가 아니라) — "국경을 얼마나
무시할 수 있는가"는 병원체의 능력이지 국가의 상태가 아니기 때문이다(§6.1에서 원칙을 더
자세히 다룬다).

### 3.3 플래그 카탈로그 예시

요청된 5개 예시(`BirdTransmission`/`RodentSpread`/`OceanTransmission`/
`QuarantineResistance`/`GlobalSpread`)를 `NodeMapping.md`의 실제 대응 노드와 함께 정리한다.
이는 "이렇게 명명하라"는 확정안이 아니라 **강도 파라미터가 왜 필요한지 보여주는 예시**다.

| 플래그 이름(예시) | 대응 노드(`NodeMapping.md`) | 강도 파라미터 의미 |
|---|---|---|
| `BirdTransmission` | 조류 매개 전파(`trans_contact1` 재설계) | 항공/해운 허브 없는 고립국에 매 틱 발생하는 자연감염 "확률" |
| `RodentSpread` | 설치류 매개 확산(`trans_insect1` 재설계) | 국경 봉쇄 상태에서도 육상 인접국 전파가 유지되는 "확률"(완전 유지=1.0이 아니라 원래 확률의 일부만 잔존) |
| `OceanTransmission` | 해상 전파(`trans_water2` 재설계) | 해안(항구 보유) 인접국 간 감염 확률에 곱해지는 "가중치" |
| `QuarantineResistance` | 격리 내성(`abl_resist2` 재설계) | 격리된 인구 집단 내 전파가 유지되는 "잔존율"(격리 효과 100%에서 차감되는 비율) |
| `GlobalSpread` | 전지구적 전파망(`trans_global`) | 항공/항구/국경 봉쇄 페널티 전체에 곱해지는 "감산율"(다른 여러 플래그의 최종 합류점 — 개별 플래그보다 이 값이 우선 적용되는 예외 처리 필요, §7.2) |

**중요한 설계 함정**: 위 표의 마지막 행(`GlobalSpread`)처럼, 일부 플래그는 다른 플래그들의
"상위 합류"다. 이런 경우 단순히 두 값을 곱하면(개별 플래그 감산 × GlobalSpread 감산) 과도한
중첩 감산이 발생할 수 있다 — `NodeMapping.md`의 `trans_advanced1`(50% 감산)→`trans_global`
(100% 감산) 관계가 정확히 이 사례다. 강도 사전을 단순 곱셈으로 합성하지 말고, **같은 "게이트
범주"(예: 봉쇄 무력화)에 속하는 플래그들은 `Max()`로 합성**하는 것을 원칙으로 권장한다 —
`trans_global`이 있으면 `trans_advanced1`의 50%는 자동으로 무시되고 100%만 적용되는 식이다.
이 원칙이 없으면 노드가 늘어날수록 조합 폭발(중첩 곱셈으로 의도치 않게 100% 초과 무력화)이
발생한다.

---

## 4. medicalBurdenModifier 설계

### 4.1 필요한 데이터

현재 `Country.HealthLevel`은 `developmentLevel`에서 정적으로 유도되는 값(High=0.8/Mid=0.5/
Low=0.2)이다 — "감염이 폭증하면 의료 시스템이 무너져 실효 의료 수준이 떨어진다"는 동적 요소가
없다. `medicalBurdenModifier`가 채워야 할 공백은 정확히 이것이다: **국가의 명목 의료 수준과
실제로 그 틱에 발휘되는 의료 수준 사이의 간극**.

### 4.2 국가별인가 병원체별인가

둘 다 필요하지만 역할이 다르다.

- **병원체별(정적, 연구로 누적)**: `Pathogen.medicalBurdenModifier` — "이 병원체가 의료
  시스템에 얼마나 부담을 주는 유전적 특성을 가졌는가"의 기초 계수. §11의 수혈 전파·오염
  혈액 유통망·발열·폐렴·호흡 부전·패혈증(`NodeMapping.md` §6, 6건)이 이 값을 누적시킨다.
  연구가 해금될 때마다 가산되므로 기존 4개 스탯과 동일한 패턴(Stat Modifier의 5번째 필드에
  가깝다).
- **국가별(동적, 매 틱 계산)**: 실제 "이번 틱에 이 국가의 의료 부하가 얼마인가"는 국가마다
  감염 비율이 다르므로 국가별로 다르게 나온다. 이 값은 **저장하지 않고 매 틱 계산**하는
  것을 권장한다 — `Country`에 새 필드를 추가하지 않고, `SimulationManager.RunTick()`의
  국가 루프 안에서 지역 변수로만 존재하는 파생값으로 둔다. 이미 `preTickInfected` 같은
  스냅샷 변수를 그 루프 안에서 쓰고 있어 기존 패턴과 정확히 일치한다.

### 4.3 계산식 초안

방향성만 제시한다(정확한 계수는 밸런스 리뷰 대상, `NodeMapping.md` §7과 동일한 caveat).

```
국가별 의료 부하(medicalLoad)
  = (country.infectedCount / country.LivingPopulation)   -- 감염 비율
  × pathogen.severity                                      -- 병원체 중증도
  × pathogen.medicalBurdenModifier                         -- 연구로 누적된 병원체 특성치
```

이 값을 기존 `RunTick()`의 `healthcareCapacity`/`effectiveHealthLevel` 항에 곱해 넣는다 —
즉 "명목 의료 수준"에서 "의료 부하로 깎인 실효 의료 수준"으로 대체하는 지점을 추가한다.
`medicalLoad`가 임계값(예: 0.5)을 넘으면 "의료 붕괴" 상태로 간주해, 그 조건에 의존하는
연구(수혈 전파·오염 혈액 유통망의 "임계값 초과 시 효과 배가")가 이 국면에서 조회하는
불리언(`medicalLoad > threshold`)도 같은 계산에서 파생시킨다 — 별도 필드 없이 계산값
하나로 스탯 보정과 조건 판정 두 가지를 동시에 지원한다.

### 4.4 기존 UI 계산식과의 통합 필요성

`CountryPopupController`(DevLog Step 78)가 이미 "의료 시스템 부담 4단계"를 자체 계산식으로
독자 구현했고, `CountryStatusPanelController`(Step 80)도 "이 계산식을 독립 복제해 재사용"한
전례가 있다 — 즉 현재 의료 부하 계산 로직이 **UI 레이어에 이미 두 벌 존재**한다. 이번에
`medicalBurdenModifier`를 `SimulationManager`(시뮬레이션 레이어)에 세 번째로 추가하면 세
계산식이 서로 다른 시점에 따로 값을 낼 위험이 있다. **권장: `medicalBurdenModifier` 도입
시점에 UI 두 곳의 계산식도 같은 소스(§4.3 공식)를 참조하도록 통합** — 이는 이 문서의 범위를
넘는 리팩터링이지만, §9(MVP 우선순위) 3단계 착수 시 반드시 함께 검토해야 할 부수 작업으로
명시해둔다.

---

## 5. environmentResistance 활용 구조

### 5.1 현재 상태(이미 존재하는 소비처)

`Pathogen.environmentResistance`(`List<ClimateResistanceEntry>`)와
`GetEnvironmentResistance(climate)`는 이미 존재하고, `SimulationManager.RunTick()`의
`climateModifier = pathogen.GetEnvironmentResistance(country.climate)`가 이미 `newInfected`
계산식에 곱해지고 있다. `FinalDecision.md` §3이 이미 진단했듯 **공백은 소비처가 아니라
공급처다** — 어떤 노드도 이 리스트에 값을 써넣지 않는다.

### 5.2 연구 연결 방식

`UpgradeEffectEntry.statName`에 `"climateResistance:Cold"`처럼 콜론으로 기후를 인코딩하는
방식은 지양한다 — 문자열 파싱이 오타에 취약하고(`§3.2`가 이미 짚은 매직 스트링 문제가
그대로 재발), `ClimateType`이 이미 강타입 enum인데 이를 문자열로 우회하는 것은 일관성이
없다. 대신 `UpgradeEffectEntry`와 **병렬적인 신규 리스트**(기후 enum + 보정치 float 쌍)를
`UpgradeNode`에 하나 더 두는 것을 권장한다 — `Pathogen.environmentResistance`가 이미
`ClimateResistanceEntry`라는 (enum, float) 쌍 구조이므로, 노드 쪽도 동일한 형태의 리스트를
가지면 "해금 시 이 리스트를 그대로 `Pathogen.environmentResistance`에 가산 반영"하는 로직이
`GetEnvironmentResistance()`를 전혀 수정하지 않고 끝난다 — §9의 2단계가 "이미 있는 함수에
값을 채워 넣기만 하면 되는" 최저 비용 항목인 이유가 여기 있다.

적용 방식은 가산이 아니라 **상한 clamp**다 — `GetEnvironmentResistance()`가 미등록 기후에
대해 1(중립, 페널티 없음)을 반환하므로, "페널티 제거" 연구(내한성/내건성/전천후 적응)는
해당 기후 항목의 값을 1로 올리는 것으로 표현한다(현재 페널티 값이 얼마든 상한을 뚫지 않고
1을 넘지 않게 `Mathf.Min(1f, ...)` 방향으로 처리 — 정확한 산술은 구현 세션에서 확정).

---

## 6. 국가 상태 시스템 연결 구조

### 6.1 원칙 — 정책 상태(Country)와 병원체 능력(Pathogen)의 분리

`Country.isAirportOpen`/`isPortOpen`/`isBorderClosed`/`governmentStability`는
`HumanResistanceManager`가 "인류가 이번 틱에 어떤 정책을 폈는가"를 매 틱 새로 계산해서
덮어쓰는 값이다. 만약 연구 효과가 이 값을 직접 조작한다면(예: 국경 무력화 연구가
`country.isBorderClosed = false`를 강제) 다음 틱에 `HumanResistanceManager.ApplyPolicy()`가
조건을 다시 평가해 원래대로 되돌려버린다 — **원인(정책 판단)과 결과(정책이 실제로 얼마나
유효한가)가 같은 필드에 뒤섞여 있으면 두 시스템이 서로를 덮어쓰는 경쟬 상태가 된다.**

원칙: **`Country`의 정책 상태 필드는 절대 연구 효과가 직접 쓰지 않는다.** 대신 그 상태값을
"읽어서 사용하는" 소비 코드 쪽에서, 원래 상태값과 함께 `Pathogen`의 능력(§3의 플래그·강도)을
같이 조회해 최종 판정을 내린다. 즉 `Country.isBorderClosed == true`라는 사실 자체는 그대로
유지되고, "그럼에도 불구하고 병원체가 얼마나 뚫는가"만 병원체 쪽 능력으로 별도 판정한다.

### 6.2 연결 지점별 확장 설계

| 정책 상태 | 현재 소비 지점 | 확장 방식 |
|---|---|---|
| `isBorderClosed` | `SimulationManager.SpreadBetweenCountries()`의 `routeOpen` 델리게이트(양국 모두 열려 있어야 true) | `routeOpen`이 false를 반환해도, 곧바로 전파를 막지 않고 `RodentSpread`류 플래그의 강도(§3.3)만큼 별도 확률을 한 번 더 굴리는 "우회 판정"을 추가 |
| `isAirportOpen`/`isPortOpen` | `TransportManager`의 경로 개폐 판정(431~433행, 양국 AND) | 위와 동일한 원칙 — 판정이 false여도 우회 확률 굴림을 거치게 하는 지점을 `TransportManager`에도 동일하게 추가해야 한다(§1.2가 지적한 공백) |
| `governmentStability` | `HumanResistanceManager.ApplyPolicy()`의 세계 붕괴 단계 감쇠, `ApplySequentialClosure()`의 봉쇄 임계값 판정 | 원래 갱신식(감소/상승 폭)에 `Pathogen`의 State Modifier 강도를 곱하는 항을 추가 — 예: "숙주 내 잠행"류 연구가 있으면 `governmentStability` 상승 속도 자체가 느려짐 |
| `healthFunding` | `HumanResistanceManager.ApplyPolicy()`가 `developmentLevel`·붕괴단계로 계산 | 현재 스코프에서 연구가 직접 건드릴 필요는 확인되지 않음(§11 카탈로그 어느 노드도 연구 기여도 자체를 겨냥하지 않음) — 확장 지점으로 예비만 해둔다 |

이 표의 핵심은 **"게이트가 닫혀 있다"는 사실과 "그래도 얼마나 새는가"라는 판정을 물리적으로
분리된 두 단계로 유지**하는 것이다. 이렇게 하면 `HumanResistanceManager`는 여전히 `Pathogen`
을 전혀 몰라도 되고(정책 로직 그대로 유지), 병원체의 우회 능력은 `SimulationManager`/
`TransportManager`의 게이트 판정 직후에만 끼어든다 — 책임이 섞이지 않는다.

---

## 7. Simulation Tick 처리 흐름

### 7.1 현재 틱 파이프라인

`SimulationManager.RunTick()` 기준(코루틴 `TickLoop()`이 매 틱 호출):

1. 국가 루프 진입, `preTickInfected` 스냅샷
2. 사망자 계산(`newDeaths` — 치사율×중증도×(1-의료수준))
3. 국내 감염 계산(`newInfected` — 감염력×확산계수×난이도배율×(1-실효의료수준)×기후보정)
4. 국가 상태 갱신(`deadCount`/`infectedCount` 반영), 마일스톤 체크
5. 국가 루프 종료 후 `SpreadBetweenCountries()`(육상 국경 전파)
6. 치료제 연구 시작 판정(미시작 시) → 시작됐으면 `cureProgress` 갱신
7. `cureProgress >= 1`이면 `EradicatePathogen()`
8. `plagueVisibility` 갱신
9. `currentDay` 증가, `WorldDataManager.RecalculateWorldTotals()`
10. `GameManager.EvaluatePhase()` 호출
11. `OnTickCompleted` 발행 — `HumanResistanceManager`/`EventManager`가 이어서 처리
12. 종료조건(`EvaluateEndConditions`) 판정

`TransportManager`는 별도 코루틴으로 독립 이동하며, 유닛 도착 시점에 별도로 감염을 이식한다
(같은 "틱"은 아니지만 같은 day 스케일에서 함께 굴러가는 병렬 시스템으로 취급).

### 7.2 연구 효과 적용 순서 제안

| 순서 | 파이프라인 단계 | 적용되는 효과 타입 | 비고 |
|---|---|---|---|
| 0 | (틱 시작 전, 노드 해금 시점 1회) | Stat Modifier | 해금 즉시 `Pathogen` 필드에 반영돼 있으므로 틱 시작 시점엔 이미 최신값 — 매 틱 재계산 불필요 |
| 1 | 사망자 계산(2번) | State Modifier(`medicalBurdenModifier`) | `medicalLoad`를 이 시점에 1회 계산해 지역 변수로 두고, 사망·감염 계산 양쪽에서 재사용(§4.2 원칙과 동일 — `preTickInfected` 스냅샷과 같은 패턴) |
| 2 | 국내 감염 계산(3번) | State Modifier(`environmentResistance`, 이미 Stat과 동일하게 사전 반영됨), Simulation Modifier 중 "국내 유지력" 계열 | `climateModifier`는 이미 §5 방식대로 사전 반영, 국내 유지력 보정(예: 비말 핵 잔류의 공항폐쇄국 페널티 완화)은 이 계산식에 곱항 추가 |
| 3 | 국가간 전파(5번) | Simulation Modifier의 게이트 우회류 전부 | §6.2 표의 우회 판정이 전부 이 단계 — `SpreadBetweenCountries()`와 `TransportManager` 도착 판정 양쪽 |
| 4 | 치료제 진행(6번) 직후 | Event Modifier 중 치료제 역전류(항원 변이) | `cureProgress` 갱신 직후 조건(임계값 0.5 초과 등) 체크 후 확률 판정 |
| 5 | 발각도 갱신(8번) | State Modifier 중 발각도 지연류(무증상 잠복 등 일부는 Stat, 나머지는 계수) | `visibilityGainRate`에 곱하는 보정 — Stat인 `severity` 감소와는 별개 경로 |
| 6 | `OnTickCompleted` 이후(11번) | State Modifier 중 정책 갱신 관련(§6.2의 `governmentStability` 행) | `HumanResistanceManager.ApplyPolicy()` 내부에서 처리 — 틱 파이프라인 본체가 아니라 후처리 구독자이므로 순서상 가장 마지막 |
| 7 | (별도 확률 루프, `EventManager`와 유사한 독립 타이밍) | Event Modifier 중 재감염 풀류(세포벽 강화) | 기존 `EventManager.TryTrigger` 패턴 재사용(§8) — 굳이 `RunTick()` 본체에 끼워 넣지 않고 독립 구독자로 둔다 |

순서가 중요한 이유는 두 가지다. 첫째, `medicalLoad`(1번)는 사망·감염 계산 둘 다 필요로 하므로
두 번 따로 계산하면 값이 미세하게 어긋날 여지(부동소수 순서 의존)가 생긴다 — 반드시 국가 루프
최초 진입 시 1회만 계산해 공유해야 한다. 둘째, 게이트 우회 판정(3번)은 반드시
`SpreadBetweenCountries()`가 원래의 `routeOpen` 판정을 마친 **직후**에 붙어야 한다 — 우회
판정을 원래 판정보다 먼저 굴리면 "닫혀 있어도 원래 열려 있던 것처럼" 로직이 꼬인다.

---

## 8. Research Effect Architecture — if문 vs 데이터 기반

### 8.1 if문 나열 방식의 한계

현재 `ApplyEffectsToPathogen()`은 `switch (effect.statName)`으로 4개 case를 나열하는
방식이다. 이 패턴을 그대로 4개 효과 타입 전체로 확장하면, 새 연구 하나가 추가될 때마다
`SimulationManager`/`HumanResistanceManager`/`TransportManager` 각각에 "이 노드가
해금됐으면..." 식의 조건문이 새로 생긴다. 이는 두 가지 문제를 만든다.

1. **역방향 의존**: 원래 `SimulationManager`는 `Pathogen`의 필드값만 알면 됐는데(노드 id를
   전혀 몰라도 됨), if문 나열 방식에서는 시뮬레이션 매니저가 "어느 노드가 이 효과를
   냈는가"까지 알아야 하는 경우가 생기기 쉽다 — 계층이 거꾸로 얽힌다.
2. **선형 증가하지 않는 유지보수 비용**: `NodeMapping.md` §6이 이미 "3단계(신규 불리언
   시스템)가 리스크 가장 높다"고 지적한 이유가 정확히 이것이다 — 45개에서 100개로 늘면
   조건문도 같이 늘고, 조건문끼리 상호작용(§3.3의 `GlobalSpread` 합류 문제 같은)까지
   고려하면 조합 복잡도가 노드 수보다 빠르게 증가한다.

### 8.2 데이터 기반 방식

권장 방향: **조건 분기를 소비처가 아니라 집계 시점(노드 해금 시)으로 옮긴다.**
`UpgradeManager`는 지금도 "해금 시 `Pathogen`을 갱신하는 유일한 지점"이라는 성질을 갖고
있다(§1.3) — 이 성질을 유지한 채로, 갱신 대상을 4개 float 필드에서 "`Pathogen`이 노출하는
조회 API 전체"(4개 스탯 + `environmentResistance` + `medicalBurdenModifier` + 플래그·강도
사전)로 넓힌다. 소비 매니저(`SimulationManager`/`TransportManager`/`HumanResistanceManager`)
쪽 코드는 "노드 id로 분기"하지 않고 "`Pathogen`에게 값을 물어본다"는 형태만 유지한다 — 이미
`pathogen.infectivity`를 읽는 것과 동일한 형태로, `pathogen.HasFlag("RodentSpread")`나
`pathogen.MedicalBurdenModifier` 같은 조회를 추가하는 정도의 확장이다.

### 8.3 권장 하이브리드 원칙

100% 범용 데이터 기반(예: 임의의 계산식을 문자열로 인코딩해 런타임에 해석하는 표현식
인터프리터)은 이 프로젝트 규모(45→100 목표)에는 과설계다 — 검증·디버깅 비용이 오히려
커진다. 대신 다음 원칙으로 절충한다.

- **효과 타입은 §2.1의 4종으로 고정한다.** 새 타입을 추가하지 않는 한(즉 시뮬레이션을
  담당하는 매니저가 `SimulationManager`/`HumanResistanceManager`/`TransportManager`/
  `EventManager` 4개로 안정적으로 유지되는 한), 신규 연구 추가는 **새 조건문이 아니라 이미
  존재하는 4개 카테고리 중 하나에 새 데이터 항목을 추가하는 것**으로 끝난다.
- **"매니저는 노드 id를 몰라야 한다"는 경계를 지킨다.** `SimulationManager`는 지금
  `pathogen.infectivity`가 어느 노드에서 왔는지 전혀 모른다 — 이 원칙을 나머지 3개 타입에도
  동일하게 적용한다. "어느 노드가 이 플래그를 심었는가"는 오직 `UpgradeManager`(그리고
  `UpgradeNode.effects` 데이터 자체)만 안다.
- **쓰기는 `UpgradeManager` 하나, 읽기는 시뮬레이션 4개 매니저** — 이 경계 하나만 지키면
  노드 개수가 늘어도 매니저 쪽 코드 복잡도는 "조회 API의 개수"에 비례하지, "노드의 개수"에
  비례하지 않는다(§10에서 최종 아키텍처로 재확인).

---

## 9. MVP 구현 우선순위

`ResearchDatabase_Design.md` §19-4가 권장한 순서(환경 저항 재활용 → 의료 시스템 부담 스탯 →
시기 게이팅)와 `NodeMapping.md` §6의 필드별 의존 연구 수를 함께 반영해 4단계로 정리한다.

| 단계 | 항목 | 구현 표면적 | 실현되는 연구 수(`NodeMapping.md` 기준) | 비고 |
|---|---|---|---|---|
| **1단계** | 기존 시스템 재사용 | 신규 코드 없음 — 노드 서술 문구만 교체(`ResearchDatabase_Design.md` §16 1단계와 동일) | 21번(약물 내성) 등 "효과는 이미 있고 서술만 바꾸면 되는" 사례 | 리스크 없음, 즉시 착수 가능 |
| **2단계** | `environmentResistance` 소비(§5) | `UpgradeNode`에 (기후,보정치) 병렬 리스트 1개 추가 + `ApplyEffectsToPathogen`에 case 1개 | 인수공통 전파(Humid 가중)·내한성·전천후 적응 3건 | 소비 함수(`GetEnvironmentResistance`)가 이미 있어 구현 비용 대비 효과 최고 |
| **3단계** | `medicalBurdenModifier`(§4) | `Pathogen`에 float 필드 1개 + `RunTick()` 사망/감염 계산식에 곱항 1개 + (권장) UI 계산식 통합 | 수혈 전파·오염 혈액 유통망·발열·폐렴·호흡 부전·패혈증 6건 | `NodeMapping.md` §6 기준 실제 의존 연구 수가 가장 많음(우선순위 재검토 여지가 있었던 항목, §19-4는 2순위·이 매핑은 최다 소비) |
| **4단계** | `unlockedFlags`(§3) + Event Modifier(§2.1) | `Pathogen`에 `HashSet<string>` + `Dictionary<string,float>` 추가, `SimulationManager`/`TransportManager`/`HumanResistanceManager` 세 곳 모두에 우회 판정 지점 추가(§6.2), `EventManager`류 확률 루프 확장 | 조류 매개 전파·설치류 매개 확산·세포벽 강화·격리 내성·검사 회피 등 다수 | 표면적이 가장 넓고 `GlobalSpread`류 합류 처리(§3.3)까지 고려해야 해 리스크 최고 — 검사 회피(UI 왜곡)는 이 단계 안에서도 가장 나중 |

우선순위가 구현 비용 오름차순인 동시에, 각 단계가 그 이전 단계의 데이터 모델을 깨지 않고
순수 추가만 한다는 점을 확인했다 — 2단계는 1단계 위에, 3단계는 1·2단계 위에, 4단계는
1·2·3단계 위에 쌓이며 어느 단계도 이전 단계를 재작업하지 않는다.

---

## 10. 최종 권장 아키텍처

### 10.1 `Pathogen`을 "정적 스탯 보관함"에서 "병원체 능력 조회 인터페이스"로

현재 `Pathogen`은 4개 float 필드의 묶음이다. 이 문서가 권장하는 최종 형태는 다음 5개
구성요소를 가진 응집된 조회 대상이다.

1. 4개 Stat 필드(기존 그대로) — `infectivity`/`severity`/`lethality`/`drugResistance`
2. `environmentResistance`(기존 필드, §5 방식으로 노드가 값을 채우게 됨)
3. `medicalBurdenModifier`(§4, 3단계 신규)
4. 능력 플래그 집합 + 강도 사전(§3, 4단계 신규)
5. (선택, Event Modifier 발동 조건 등록용) 게이팅 조건 목록 — `minCureProgress`/
   `minVisibility` 등, `NodeMapping.md` §6이 "시기 게이팅"으로 이미 언급한 항목

### 10.2 쓰기·읽기 경계

```
쓰기 (유일한 지점)              읽기 (소비 지점 4개, 서로 독립)
─────────────────              ─────────────────────────────
UpgradeManager                  SimulationManager   (틱 계산 전체)
  .TryUnlock()                  TransportManager     (경로 판정)
  → ApplyEffectsToPathogen()    HumanResistanceManager (정책 갱신)
    (4타입 전부 여기서 집계)     EventManager/신규 루프 (확률 이벤트)
```

이 경계 — **쓰기는 `UpgradeManager` 하나, 읽기는 시뮬레이션 매니저 4개** — 가 지켜지는 한
노드가 45개에서 100개, 200개로 늘어나도 소비 매니저 쪽 코드는 "조회 API의 개수"에만
비례해서 커지고 "노드의 개수"에는 비례하지 않는다. 이것이 이 문서가 요구받은 "유지보수
가능한 구조"의 실질적인 정의다.

### 10.3 단계별 로드맵 요약

| 단계 | 데이터 모델 변경 | 이전 단계 재작업 여부 |
|---|---|---|
| 1단계 | 없음 | - |
| 2단계 | `UpgradeNode`에 기후 보정 리스트 추가 | 없음 |
| 3단계 | `Pathogen`에 `medicalBurdenModifier` 추가 | 없음(단, UI 계산식 통합은 권장 부수 작업) |
| 4단계 | `Pathogen`에 플래그 집합·강도 사전 추가, 3개 매니저에 우회 판정 지점 추가 | 없음 |

이 문서는 설계안이며 위 항목 중 어느 것도 아직 구현되지 않았다. 다음 세션에서 구현에
착수한다면 §9의 순서(2→3→4단계)를 따르고, 각 단계 착수 전 `NodeMapping.md` §8의 미결정
항목(플래그 정확한 타입, `medicalBurdenModifier` 계수, DNA 비용 프리미엄 여부)을 먼저
확정해야 한다.

---

## 11. 남은 질문 / 다음 세션 결정 필요 항목

1. **§3.3의 `GlobalSpread`류 합류 처리 — `Max()` 합성 원칙을 실제로 어디까지 적용할지.**
   "같은 게이트 범주"의 경계를 정의하는 구체적 규칙(예: 국경/항구/공항을 하나의 범주로
   묶을지, 개별로 유지할지)이 아직 없다.
2. **§4.4의 UI 계산식 통합 범위.** `CountryPopupController`/`CountryStatusPanelController`
   두 곳의 기존 의료 부하 계산식을 `medicalBurdenModifier` 도입과 동시에 리팩터링할지,
   아니면 3단계 이후 별도 세션으로 미룰지 결정 필요.
3. **§10.1의 5번(게이팅 조건 목록)이 이 문서 범위인가, `NodeMapping.md`가 이미 다룬
   "시기 게이팅"과 중복인가.** 항원 변이 연구 하나만 이 필드에 의존하므로(`NodeMapping.md`
   §6), 4단계와 통합해서 설계할지 별도 소단계로 분리할지는 실제 착수 시점에 재검토한다.
4. **§8.3 원칙(4개 효과 타입 고정)이 깨지는 경우의 대응.** 만약 향후 연구 카탈로그가 4종으로
   설명 안 되는 새로운 성격의 효과를 요구하면(예: UI 자체를 바꾸는 효과 — 검사 회피가 이미
   그 조짐), 그때는 타입을 5종으로 늘리는 것이 맞는지, 기존 4종 중 하나로 우겨넣는 것이
   맞는지 판단 기준이 아직 없다.

이 문서는 설계안이며 위 항목 중 어느 것도 아직 구현되지 않았다.
