# World Event System 설계 — 살아있는 세계 시뮬레이션

**목적**: Contagion Project의 세계가 스스로 변화하고, 플레이어는 그 변화를 관찰해 진화(Evolution)로
적응하는 구조를 설계한다. Event는 랜덤 팝업이 아니라 **World Simulation의 결과물**이며, 다른
Event를 낳고 World State를 바꾸고 새로운 Evolution Opportunity를 만들어야 한다. 코드는 작성하지
않는다 — 순수 게임 디자인 문서. `Docs/Archive/EvolutionSystem2.0_PlagueIncDesignAnalysis.md`(직전
문서, Plague Inc. 심층 분석 + Evolution 아이디어 52개)와 짝을 이루는 문서다 — 이 문서의 Evolution
Opportunity 열은 가능한 곳에서 그 문서의 아이디어 번호를 직접 인용한다.

**조사 방법**: Plague Inc.는 직전 문서에서 이미 심층 조사했으므로 재사용한다. Frostpunk·RimWorld,
Stellaris·Civilization, Crusader Kings·Pandemic(보드게임) 3개 그룹으로 나눠 병렬 조사했다 —
공식 위키/개발자 Dev Diary 1순위, Reddit·Steam Community·BoardGameGeek 커뮤니티 소스로 보강.

---

## 1. World Event 철학

7개 게임을 관통하는 단 하나의 공통 결론: **좋은 Event 시스템은 "무슨 일이 일어났는가"를 알려주는
게 아니라 "왜 지금 이 일이 일어날 수밖에 없었는가"를 플레이어가 스스로 재구성할 수 있게 한다.**
이걸 달성하는 방식은 게임마다 다르지만 원리는 셋으로 수렴한다.

1. **Event 자격은 항상 누적된 상태가 결정한다, 순수 난수가 아니다.** RimWorld는 부(富)를 위협
   포인트로 환산하고, Stellaris는 앤솔러지 사이트 완료·기술 보유·연도 경과를 크라이시스 발동
   조건으로 삼으며, Frostpunk는 Discontent/Hope 두 계기판이 같은 "문제"를 다른 위기(Protest vs
   Riot)로 분기시킨다. "무작위처럼 보이는 것"은 실제로는 **현재 세계 상태가 이미 열어둔 선택지
   중에서의 추첨**일 뿐이다.
2. **결정이 이벤트를 만들지, 이벤트가 결정을 강요하는 게 다가 아니다.** Frostpunk의 Book of Laws는
   이 관계를 뒤집는다 — 플레이어가 법을 통과시키는 행위 자체가 후속 이벤트를 생성한다. Plague
   Inc.도 마찬가지다 — 치사율을 올리는 "결정"이 치료제 가속이라는 "이벤트"를 만든다. 좋은 Event는
   플레이어가 "불운"이 아니라 "내가 자초했다"고 느끼게 한다.
3. **체인은 최소 4가지 서로 다른 구조를 가질 수 있다** — (a) Frostpunk형 **명명된 사다리**
   (Problem→Protest/Riot, 계기판 2개로 분기), (b) RimWorld형 **독립 시스템의 우발적 충돌**
   (부상→기분→멘탈브레이크, 누구도 "체인"을 설계하지 않았는데 발생), (c) Crusader Kings형
   **팬아웃**(군주 사망 하나 → 봉신 N명이 각자 독립적으로 반란 이벤트를 생성), (d) Pandemic형
   **그래프 전파**(도시 하나의 발병이 연결된 모든 도시로 동시에 번지는 카운터 공유형 연쇄).
   Contagion Project는 이미 국가 간 교통망 그래프가 있으므로 (d)가 구조적으로 가장 잘 맞고,
   국가별 정부 대응은 (a)/(c) 혼합이 적합하다 — 3절/4절에서 구체화한다.

---

## 2. Event Category (게임별 비교)

| 게임 | 카테고리 구분 방식 | 특징 |
|---|---|---|
| Plague Inc. | 정부 대응(교통/치안) vs 뉴스(전염병 결과 서술) vs 재해 | 대응은 우선순위 번호로 순서 고정 |
| Frostpunk | 자원(식량/난방) vs 사회(불만/희망) vs 기후(화이트아웃) | Book of Laws가 사회 카테고리를 영구화 |
| RimWorld | 위협/기회/중립 3버킷, 그 안에 세부 유형 다수 | 부/시간/적응 3요인이 버킷 내 확률을 조정 |
| Stellaris | 탐사(이상현상) vs 상황(진행형) vs 위기(엔드게임) vs 외교(은하공동체) | "일회성/진행형/영속" 3계층 구조 |
| Civilization | 재해(지형 기반) vs 비상사태(투표) vs 도시국가(관계) vs 종교(압력) | 각 카테고리가 서로 다른 트리거 철학 사용 |
| Crusader Kings | 인물(성격) vs 음모 vs 승계 vs 지역 Struggle vs 역병 | 캐릭터 스케일 vs 지역 스케일이 분리 |
| Pandemic | Infection(꾸준한 배경) vs Epidemic(스파이크) vs Outbreak(연쇄) | 3종이 서로 다른 카드/자원 풀을 사용 |

**Contagion Project 채택 카테고리 — 17개** (§7.1에서 상세)는 이 비교표에서 "무엇이 트리거인가"가
서로 겹치지 않도록 설계한다: 의료/과학은 치료제 시계(Plague Inc. Cure), 정치/군사/사회는 정부
대응 사다리(Plague Inc. Government Actions + Frostpunk Book of Laws), 국제기구는 집단 결의안
(Stellaris 은하공동체 + Civ 비상사태 투표), 교통/자연재해/기후는 그래프 전파(Pandemic Outbreak +
Civ Black Death 타일 전파), 문화/종교/언론/기술/경제/식량/에너지/난민은 부차 압력원으로
분류한다.

---

## 3. Event Trigger (트리거 패턴 종합)

관찰된 트리거는 5가지 패턴으로 분류된다 — Contagion 설계 시 이 5가지 중 하나로 모든 이벤트를
명시적으로 태깅한다.

| 패턴 | 예시 | Contagion 대응 필드 |
|---|---|---|
| **임계값형(Threshold)** — 누적 상태가 특정 수치를 넘는 순간 발동 | Plague Inc. 사망률 10%/50%, Civ VI 기후변화 임계값 | `WorldState.deadCount/totalPopulation`, `Country.healthFunding` |
| **예산 소모형(Budget)** — 상태가 "위협 포인트"로 환산되고 그 포인트를 확률적으로 소모 | RimWorld 부→습격 포인트, Stellaris 크라이시스 5년마다 재굴림 | `WorldState.plagueVisibility` × 난이도 배율 |
| **쿨다운형(Cooldown)** — 최소 간격을 두고 재발 방지 | RimWorld Cassandra 최소 2일, 기존 `EventManager.TryTrigger` | 이미 구현됨(`EventManager`) |
| **투표/집단형(Ratification)** — 상태 변화가 "제안"만 만들고 집단 행동이 확정 | Stellaris 은하공동체, Civ 비상사태 특별회기 | 다국가 동시 조건(예: 3개국 이상 봉쇄) 충족 시 자동 확정(플레이어 투표 주체 없음이므로 단순화) |
| **성격/맥락형(Contextual)** — 동일 트리거라도 주체(국가) 속성에 따라 다르게 반응 | Crusader Kings 캐릭터 성격 가중, Civ 종교/문화 압력 | `Country.developmentLevel`/`climate`/`governmentStability` |

---

## 4. Event Chain (체인 구조 종합)

앞서 정의한 4가지 체인 구조를 Contagion에 적용할 층위를 명시한다.

- **(a) 명명된 사다리** — 국가 단위 정부 대응에 적용. `Country.GetCollapseStage()`의 기존 6단계
  (평시→붕괴→무질서→무정부근접→무정부→소멸)를 Frostpunk의 Problem→Protest/Riot처럼
  "사망률"과 "치료제 투자 실패" 두 계기판으로 분기시킨다(§7.2).
- **(b) 우발적 충돌** — 국가 간 상호작용에 적용. 예: 식량 이벤트(경제 카테고리)가 독립적으로
  사회 이벤트(치안 불안)를 촉발하는 걸 "설계"하지 않고, 두 시스템이 같은 `governmentStability`
  값을 공유하게만 해서 자연히 충돌하게 한다(RimWorld 방식).
- **(c) 팬아웃** — 세계적 사건(치료제 25/50/75/95% 돌파, WHO 긴급회의)에 적용. 하나의 세계
  이벤트가 발동하면 조건을 만족하는 **모든 국가**에서 독립적으로 각자의 후속 이벤트가 동시
  판정된다.
- **(d) 그래프 전파** — 교통망(항공/해운 허브)에 적용. 국경 폐쇄·공항 폐쇄가 Pandemic의 Outbreak처럼
  **연결된 국가로 전파**되게 한다 — 한 허브 국가가 봉쇄되면 그 허브에 연결된 다른 국가들의
  봉쇄 확률이 일시적으로 상승(Pandemic Intensify 스텝의 "핫존이 계속 핫하다" 원리 재현).

---

## 5. World State 변화 (변화 유형 종합)

| 변화 유형 | 예시 | Contagion 적용 |
|---|---|---|
| **계기판(Meter) 증감** | Frostpunk Discontent/Hope, Stellaris Situation 진행 바 | 기존 `plagueVisibility`/`cureProgress`에 이벤트가 직접 가감 |
| **플래그/상태 전환** | Civ "발각(Discovered)" 스위치, Plague Inc. Public Order 단계 | `Country.GetCollapseStage()` 승격, 신규 `isDiscovered`류 플래그 |
| **그래프/맵 편집** | Pandemic Legacy의 영구 도로 차단, CK Struggle의 지역 룰 재정의 | 교통망 허브의 임시/영구 비활성화 |
| **가용 옵션 풀 변경** | Pandemic Legacy 펀딩 레벨이 다음 세션 카드 풀 결정, Stellaris 은하공동체 결의안 | 이벤트 발생 이력이 이후 `EventManager` 이벤트 풀 자체를 바꿈(예: 한 번 실패한 봉쇄는 재시도 시 강화) |
| **영구 수정자(Permanent Modifier)** | Civ 화산 토양, CK3 개발도 영구 감소 | `Country`에 이벤트 이력 기반 영구 보정치 누적 |

---

## 6. Evolution Opportunity 생성 (일반화)

7개 게임 전부에서 "위기가 곧 새 도구"라는 패턴이 반복된다 — Frostpunk Book of Laws(위기 →
새 법), Civ Black Death(재앙 → Flagellant 유닛), CK3 흑사병(역병 → Hospice/Physician 대응),
Plague Inc.(봉쇄 → 구체적 우회 진화). 공통 원칙: **Evolution Opportunity는 이벤트의 부작용이
아니라 이벤트의 존재 이유다.** Contagion에서는 모든 신규 Event가 반드시 다음 3가지 중 최소
하나를 만족해야 한다 — ①직전 문서(EvolutionSystem2.0) §9의 특정 아이디어를 해금, ②`UpgradeNode`
잠금 사유를 "관측 필요"에서 "구매 가능"으로 전환, ③기존 노드의 비용을 일시 할인.

---

## 7. Contagion Project Event System 제안

### 7.1 Event Category — 17개

| 코드 | 카테고리 | 핵심 압력원 | 대응 계기 |
|---|---|---|---|
| MED | 의료 | 치료제 시계 | `cureProgress` |
| POL | 정치 | 정부 대응 사다리 | `Country.governmentStability` |
| ECO | 경제 | 자금/자원 배분 | `Country.healthFunding` |
| SOC | 사회 | 치안/공포 | `GetCollapseStage()` |
| REL | 종교 | 과학 불신/순응 | `developmentLevel` |
| CLM | 기후/환경 | 배경 승수(비발표) | `Country.climate` |
| DIS | 자연재해 | 외생적 충격 | 무작위 + 지형 |
| INT | 국제기구 | 집단 결의(팬아웃) | 다국 동시 조건 |
| SCI | 과학/연구 | 치료제 반대편 시계 | `cureProgress` 역행 |
| MIL | 군사 | 강제력 집행 | `governmentStability` 붕괴 |
| MED2(NEWS) | 언론 | 발각/공포 증폭 | `plagueVisibility` |
| CUL | 문화 | 초광역 전파 기회 | 주기적(달력) |
| TEC | 기술 | 감시/역감시 | `developmentLevel` |
| TRN | 교통 | 그래프 전파(핵심) | `isAirportOpen`/`isPortOpen`/`isBorderClosed` |
| FOD | 식량 | 사회 불안 촉매 | `healthFunding` 저하 |
| NRG | 에너지 | 인프라 붕괴 | `GetCollapseStage()` 고단계 |
| REF | 난민/이주 | 국가 간 전파 경로 | 인접국 붕괴 |

### 7.2 Trigger 설계 원칙

기존 `EventManager.TryTrigger()`(조건+확률+쿨다운) 패턴을 그대로 재사용하되, 조건 평가 대상을
"World 전역"과 "국가별" 두 스코프로 나눈다. 국가별 이벤트는 §4(a) 사다리 구조를 따른다 — 각
국가가 `deadCount`(치사율 압력)와 `healthFunding` 저하(경제 압력) 두 계기판을 각각 가지며, 어느
쪽이 먼저 임계값을 넘는지에 따라 SOC(폭동형) 또는 MIL(계엄형) 중 다른 후속 이벤트가 갈린다.

### 7.3 Chain 설계 원칙

TRN(교통) 카테고리는 반드시 §4(d) 그래프 전파를 적용한다 — 허브 국가 하나의 봉쇄가 인접 허브의
봉쇄 확률을 한시적으로 올린다. INT(국제기구) 카테고리는 §4(c) 팬아웃을 적용한다 — 세계 단일
임계값 통과 시 조건을 만족하는 모든 국가에서 각자 판정.

### 7.4 종료 조건 설계 원칙

세 유형으로 통일한다 — ①**자연 소멸**(원인 상태가 조건 아래로 회복, 예: 치료제 실패 시
봉쇄 일부 해제), ②**대체**(상위 단계 이벤트가 발동하며 이전 단계를 흡수·종료, 예: 계엄령이
통행금지를 대체), ③**영구화**(국가 소멸처럼 되돌릴 수 없는 종료 — 후속 이벤트 풀에서 그 국가
영구 제외).

---

## 8. Event 목록 (102개)

카테고리당 6개, 17개 카테고리. **ID 형식**: `[카테고리]-[번호]`. Evolution Opportunity 열의
`§9-N`은 `EvolutionSystem2.0_PlagueIncDesignAnalysis.md` §9의 아이디어 번호를 가리킨다.

### MED — 의료 (치료제 시계)

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| MED-01 | 병상 부족 선언 | 국가별 `deadCount` 급증(단기 기울기) | `healthFunding` 상한 일시 감소 | §9-C29(치료제 요구치 조작 능력) 조건 완화 | SOC-01(의료 붕괴 폭동) | `deadCount` 기울기 완화 시 |
| MED-02 | 응급의료소 배치 | 국가 최초 봉쇄(POL-02) 직후 | 그 국가 `deadCount` 증가율 일시 억제 | §9-B14(응급의료소 대응 능력) 해금 | MED-03 | 배치 후 N틱 |
| MED-03 | 안전장치 해제 | 국가 `deadCount/population>=0.1` | `cureProgress` 가속 배율 상승 | — | MED-04 | 비가역(영구) |
| MED-04 | 인체실험 착수 | `deadCount/population>=0.5` | `cureProgress` 재가속 | §9-B15 해금 | SCI-05 | 비가역 |
| MED-05 | 백신 임상 시작 | `cureProgress>=0.5` | 신규 대응 노드 카테고리 개방 | §9-B26(백신 임상 실패 유도) | SCI-01 | 백신 성공/실패로 대체 |
| MED-06 | 의료 인력 이탈 | `governmentStability` 저하 + `healthFunding` 저하 동시 | 그 국가 치료제 기여도 일시 0 | — | ECO-03 | 안정성 회복 시 |

### POL — 정치 (정부 대응 사다리)

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| POL-01 | 발각(Discovered) | `plagueVisibility` 최저 단계 이탈 | 전역 1차 대응(통행금지급) 동시 개방(팬아웃) | §9-C37(최초 발각 대응 능력, 자동 해금) | 전체 POL/MIL/SOC 카테고리 개방 | 비가역(영구, 전역 스위치) |
| POL-02 | 국가별 1차 봉쇄 | 국가 `deadCount` 임계값 | `isAirportOpen=false` | §9-A1(하늘길 우회) | POL-03(국경) | Public Order 역주행 시 재개방 가능 |
| POL-03 | 국경 폐쇄 | POL-02 이후 추가 임계값 | `isBorderClosed=true` | §9-A1/§9-C27 | POL-04(항구) | 상동 |
| POL-04 | 항구 폐쇄 | POL-03 이후 추가 임계값(가장 늦음) | `isPortOpen=false` | §9-A2(해상 소독 저항) | TRN-01(핫존 전파) | 상동 |
| POL-05 | 정권 교체 | `governmentStability` 장기 저하 | 그 국가 대응 정책 초기화(일시 완화) | — | POL-06 | 발동 즉시(원샷) |
| POL-06 | 국경 재개방 | 치료제 국지적 성공 또는 정권 교체 후 안정화 | `isBorderClosed=false` 복귀 | §9-E48(재유입 창) | TRN-02 | 재봉쇄 조건 재충족 시 종료 |

### ECO — 경제

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| ECO-01 | 시장 공포 매도 | `plagueVisibility` 상승 + 다국 동시 봉쇄 | 저개발국 `healthFunding` 추가 하락 | — | FOD-01 | 발동 후 원샷 |
| ECO-02 | 의료 예산 긴급 편성 | `deadCount` 임계값 | 해당국 `healthFunding` 임시 상승(역설적 치료제 가속) | §9-C29 조건 확인 | MED-03 | N틱 후 |
| ECO-03 | 노동력 붕괴 | 감염률 고수준 지속 | `healthFunding` 지속 하락 | — | FOD-02 | 감염률 하락 시 |
| ECO-04 | 국제 원조 도착 | INT 카테고리 결의안 통과 | 대상국 `healthFunding` 회복 | — | POL-06 | 원샷 |
| ECO-05 | 암시장 활성화 | `isBorderClosed=true` 국가 다수 | 해당국 통제 우회 확률 소폭 상승 | §9-C39(선제 자금원 차단) 대구도 | REF-01 | 봉쇄 해제 시 |
| ECO-06 | 제약회사 특허 경쟁 | `cureProgress>=0.75` | 치료제 배포 속도 국가별 격차 발생(부국 우선) | — | MED-05 | 백신 배포 완료 시 |

### SOC — 사회

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| SOC-01 | 의료 붕괴 폭동 | MED-01 + `governmentStability` 저하 | `GetCollapseStage()` 1단계 상승 | — | SOC-02 | 안정화 시 |
| SOC-02 | 사재기 확산 | ECO-01 이후 | `healthFunding` 추가 압박 | — | FOD-03 | N틱 후 |
| SOC-03 | 대규모 시위 | `GetCollapseStage()` "무질서" 진입 | 대응 강도 다음 단계로 강제 승격 | — | MIL-01 | 진압 또는 정권교체 |
| SOC-04 | 공포 확산(패닉) | `plagueVisibility` 급상승 | 해당국 전파 확률 소폭 하락(자발적 거리두기) | §9-D41(관측 필요 UI) 사례 | NEWS-01 | 발각 안정화 시 |
| SOC-05 | 연대/자원봉사 확산 | `deadCount` 낮고 `governmentStability` 높음 | `healthFunding` 소폭 상승 | — | — | 위협 재상승 시 |
| SOC-06 | 무정부 상태 진입 | `GetCollapseStage()` 최종 전단계 | 치료제 연구 그 국가 한정 정지 | §9-C32(무정부 국가 활용 능력) | ECO-06/REF-02 | 소멸 또는 회복 |

### REL — 종교

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| REL-01 | 종교 집회 강행 | `developmentLevel=Low` + 통제 약화 | 해당국 전파 확률 상승 | §9-A11(국제 행사 편승)과 동형 | CUL-01 | 집회 종료 후 |
| REL-02 | 신앙 치유 신뢰 확산 | `healthFunding` 부족 지속 | 공식 의료 신뢰도 하락(치료제 배포 저항) | — | MED-06 | 치료제 성공 사례 확산 시 |
| REL-03 | 종교 지도자 발병 | 고위험군 감염 무작위 | 그 국가 `plagueVisibility` 급상승 | — | POL-02 | 원샷 |
| REL-04 | 순례 행렬 | 주기적(달력) | 다국가 동시 감염 위험 상승(팬아웃) | §9-A11 | TRN-03 | 행사 종료 |
| REL-05 | 과학-신앙 화해 선언 | `cureProgress` 고수준 + REL-02 발생 이력 | 치료제 수용도 회복 | — | MED-05 | — |
| REL-06 | 금식/자가격리 문화 확산 | `deadCount` 임계값 | 자발적 전파 억제(플레이어 불리) | — | SOC-05 | — |

### CLM — 기후/환경

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| CLM-01 | 폭염 | 계절/기후 배경값(비공개 승수) | `climate=hot` 국가 전파 계수 임시 변화 | §9-C30(환경 강화 조기 해금) | — | 계절 종료 |
| CLM-02 | 한파 | 상동 | `climate=cold` 국가 전파 계수 임시 변화 | §9-C30 | — | 계절 종료 |
| CLM-03 | 우기 | 상동 | 수인성 전파 유리 | §9-A9(기후 무관 전파) 반대급부 | TRN-04 | 계절 종료 |
| CLM-04 | 가뭄 | 장기 기후 누적 | `healthFunding` 저하(농업 기반 국가) | — | FOD-04 | — |
| CLM-05 | 대기오염 심화 | 산업국가 지속 | 호흡기 계열 증상 발각 지연(역설적 유리) | — | NEWS-02 | — |
| CLM-06 | 이상기후 공식 선언 | CLM 이벤트 3종 이상 누적 발생 | 전역 "기후위기" 플래그, INT 카테고리 결의안 트리거 | — | INT-01 | 비가역 |

### DIS — 자연재해

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| DIS-01 | 지진 | 무작위(지형 가중) | 해당국 `GetCollapseStage()` 강제 1단계 상승(플레이어 무관) | — | SOC-01 | 원샷 |
| DIS-02 | 홍수 | 무작위(하천 인접국 가중) | 이재민 발생 → REF 카테고리 트리거 | — | REF-03 | 원샷 |
| DIS-03 | 태풍/허리케인 | 계절 + 무작위 | 해당국 교통망 허브 일시 비활성화 | — | TRN-05 | N틱 후 자동 복구 |
| DIS-04 | 산불 | 건조 기후 + 무작위 | `healthFunding` 저하, 호흡기 취약군 증가 | — | MED-01 | 진화 후 |
| DIS-05 | 화산 폭발 | 무작위(저확률) | 인접국 교통 그래프 일시 단절(그래프 전파 예외 케이스) | — | ECO-01 | N틱 후 |
| DIS-06 | 복합재해(재난 중첩) | DIS 이벤트 2종 이상 동시 | 해당국 `GetCollapseStage()` 2단계 강제 상승 | §9-E49(다국 동시 붕괴 시너지)와 연동 | SOC-06 | 원샷 |

### INT — 국제기구 (팬아웃)

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| INT-01 | WHO 긴급회의(기존 이벤트 확장) | `plagueVisibility` 임계값 | 전역 대응 강화(팬아웃, 전국가 동시 판정) | §9-A12(긴급회의 역이용) | 전체 POL 사다리 가속 | 원샷이나 재발 가능(쿨다운) |
| INT-02 | 국제공조 강화(기존 이벤트 확장) | INT-01 이후 | 전역 국경 감시 강화 | §9-C34(국제공조 대응 능력) | POL-03 전역 가속 | N틱 지속 |
| INT-03 | 글로벌 펀드 조성 | `deadCount` 세계 총합 임계값 | 저개발국 전체 `healthFunding` 상승(팬아웃) | — | MED-01 | 원샷 |
| INT-04 | 여행 경보 격상 | 3개국 이상 동시 POL-03 | 전역 TRN 카테고리 확률 조정 | — | TRN 전역 | 경보 해제 시 |
| INT-05 | 국제 제재/고립 | 특정국 대응 실패 반복 | 그 국가만 국제 원조 대상 제외 | — | ECO-05 | 정권교체 시 |
| INT-06 | 팬데믹 공식 선언 | `plagueVisibility` 최고 단계 | 전역 모든 카테고리 이벤트 확률 상향 보정(마스터 스위치) | §9-D46(이벤트 카테고리 확장)의 실제 적용 사례 | 전체 카테고리 | 비가역 |

### SCI — 과학/연구

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| SCI-01 | 백신 임상 성공(기존 이벤트) | `cureProgress` 고수준 | `cureProgress` 급진전 | §9-C35(백신 임상 성공 후 대응, 유일한 순수 방어형) | MED-05 | 원샷 |
| SCI-02 | 항원 검출 기술 발전 | `developmentLevel=High` 국가 연구 누적 | `plagueVisibility` 판정 민감도 상승(발각 쉬워짐) | — | POL-01 가속 | — |
| SCI-03 | 유전체 분석 실패 | 병원체 측 대응(§9-B18 은신 콤보 등) 발동 이력 | `cureProgress` 요구치 상승(분모 증가) | 8.2절 "치료제 요구치" 재현 사례 | — | — |
| SCI-04 | 국제 연구 데이터 공유 | INT-02 발동 중 | `cureProgress` 속도 전역 상승 | — | MED-04 | INT-02 종료 시 |
| SCI-05 | 임상 부작용 스캔들 | MED-04 이후 무작위 | 백신 수용도 하락(REL-02와 합류 가능) | — | REL-02 | 원샷 |
| SCI-06 | 오픈소스 치료 프로토콜 | `cureProgress>=0.9` | 저개발국 치료제 접근성 급상승 | — | MED-05 | — |

### MIL — 군사

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| MIL-01 | 계엄령 선포 | SOC-03 이후 | 그 국가 감염 확산 강한 억제 + `governmentStability` 추가 하락 | §9-C31(격리 내성 이벤트 연동) | SOC-06 | 안정화 또는 무정부 진입 |
| MIL-02 | 군 병력 방역 투입 | MED-01 + `governmentStability` 보통 이상 | `healthFunding` 임시 대체(의료 인력 부족 보완) | — | MED-02 | N틱 후 |
| MIL-03 | 국경 무력 봉쇄 | POL-03 이후 밀입국 시도 감지 | TRN 카테고리 우회 확률 하락 | — | ECO-05 | 긴장 완화 시 |
| MIL-04 | 생물무기 의혹 제기 | `plagueVisibility` 최고 단계 + 특정 국가 지목 | 국제 갈등 이벤트 트리거(외교 긴장) | — | INT-05 | 조사 종료 시 |
| MIL-05 | 격리소 강제 수용 | `deadCount` 임계값 | 해당 지역 확산 억제하나 `governmentStability` 급락 | — | SOC-03 | — |
| MIL-06 | 계엄 해제 | 정권교체(POL-05) 또는 안정화 | MIL-01 효과 롤백 | — | POL-06 | — |

### NEWS — 언론

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| NEWS-01 | 공포 조장 보도 | SOC-04 이후 | `plagueVisibility` 추가 상승 | — | POL-01 가속 | 원샷 |
| NEWS-02 | 오보/가짜뉴스 확산 | `developmentLevel` 무관, 무작위 | 대응 정책 일시 오작동(효과 약화) | — | SOC-02 | N틱 후 정정 |
| NEWS-03 | 탐사 보도(정부 은폐 폭로) | `governmentStability` 저하 중 무작위 | 정권 신뢰도 급락 | — | POL-05 | 원샷 |
| NEWS-04 | 영웅 서사(의료진 미담) | MED-02/MIL-02 이후 | `governmentStability` 소폭 회복 | — | SOC-05 | 원샷 |
| NEWS-05 | 해외 상황 보도 | 인접국 POL 이벤트 발생 | 자국 `plagueVisibility` 간접 상승(선제 경계) | — | POL-02 | — |
| NEWS-06 | 언론 통제 | MIL-01 이후 권위주의 성향국 | `plagueVisibility` 상승 억제(발각 지연, 병원체 유리) | — | REL-03 | 정권 교체 시 |

### CUL — 문화

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| CUL-01 | 국제 스포츠 행사(기존 제안 §4 재사용) | 주기적(달력) | 참가국 전역 전파 기회 상승(팬아웃) | §9-A11 | TRN-03 | 행사 종료 |
| CUL-02 | 대규모 콘서트/축제 | 무작위(문화 강국 가중) | 해당국 국지적 전파 급상승 | — | POL-02 | 원샷 |
| CUL-03 | 명절/귀성 이동 | 주기적(달력) | 국내 이동 급증(교통망 활용도 상승) | — | TRN-06 | 명절 종료 |
| CUL-04 | 문화 행사 전면 취소 | `plagueVisibility` 상승 이후 | CUL 카테고리 확률 전역 하락 | — | ECO-03(경기 침체) | 안정화 시 해제 |
| CUL-05 | 온라인 문화 전환 | CUL-04 이후 | 물리적 전파 기회 감소, `TEC` 카테고리 활성화 | — | TEC-01 | — |
| CUL-06 | 국가 애도 기간 선포 | `deadCount` 임계값 | `governmentStability` 소폭 회복(결속) | — | SOC-05 | 기간 종료 |

### TEC — 기술

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| TEC-01 | 접촉 추적 앱 도입 | `developmentLevel=High` + POL-01 이후 | `plagueVisibility` 판정 민감도 상승 | — | SCI-02 | — |
| TEC-02 | 원격의료 확산 | MED-01 이후 고개발국 | `healthFunding` 효율 상승 | — | MED-02 | — |
| TEC-03 | 감시 기술 반발 | TEC-01 이후 무작위 | `governmentStability` 소폭 하락 | — | SOC-03 | — |
| TEC-04 | 신속진단키트 보급 | `cureProgress` 중간 단계 | 발각 속도 상승, 대신 조기 대응 정확도 향상 | — | POL-02 | — |
| TEC-05 | 사이버 공격(의료 시스템) | 무작위(저확률) | `healthFunding` 일시 마비 | — | MED-06 | N틱 후 복구 |
| TEC-06 | 소셜미디어 확산 알고리즘 | NEWS-02 이후 | 오보 효과 증폭(체인 강화) | — | NEWS-03 | — |

### TRN — 교통 (그래프 전파 핵심)

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| TRN-01 | 허브 국가 봉쇄 전파(그래프) | POL-04 발생국이 교통망 허브 | 인접 허브국 봉쇄 확률 일시 상승(Pandemic Intensify 원리) | §9-A2/§9-A3 | TRN-02 | 원 허브 재개방 시 |
| TRN-02 | 검역 강화 | TRN-01 이후 인접국 | 해당 경로 전파 확률 하락 | §9-A2 | POL-03 | N틱 후 |
| TRN-03 | 임시 항로 개설(우회로) | CUL-01/REL-04 행사 종료 후 | 특정 두 국가 간 임시 고빈도 연결 | §9-A11 | POL-02 | 행사 종료 |
| TRN-04 | 해상 물류 지연 | CLM-03 우기 + POL-04 동시 | 수인성 전파 효율 변화(양방향 가능) | §9-A2 | ECO-01 | — |
| TRN-05 | 재해로 인한 교통 마비 | DIS-03 태풍 등 | 해당국 허브 일시 완전 비활성 | — | ECO-01 | 자동 복구(N틱) |
| TRN-06 | 국내 대이동(명절) | CUL-03 | 국내 지역 간 전파 가속(국가 내부 세분화가 없다면 국가 자체 전파력 임시 상승으로 대체) | — | POL-02 | — |

### FOD — 식량

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| FOD-01 | 식량 가격 폭등 | ECO-01 이후 | `governmentStability` 하락 | — | SOC-01 | — |
| FOD-02 | 농업 노동력 부족 | ECO-03 이후 | `healthFunding` 추가 압박(장기) | — | FOD-04 | 감염률 하락 시 |
| FOD-03 | 배급제 도입 | SOC-02 이후 | `governmentStability` 단기 하락, 장기 안정 | — | SOC-05 | — |
| FOD-04 | 기근 임박 경보 | FOD-02 + CLM-04 동시 | `GetCollapseStage()` 상승 압력 | — | REF-03 | 국제 원조(ECO-04) 도착 시 |
| FOD-05 | 국제 식량 원조 | INT-03 이후 | 대상국 `healthFunding` 회복 | — | POL-06 | 원샷 |
| FOD-06 | 사재기 폭동 | FOD-01 + SOC 카테고리 이미 발동 중 | `GetCollapseStage()` 강제 1단계 상승 | — | MIL-05 | — |

### NRG — 에너지

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| NRG-01 | 전력망 과부하 | `GetCollapseStage()` 고단계 | `healthFunding` 효율 하락(병원 가동률 저하) | — | MED-06 | 안정화 시 |
| NRG-02 | 에너지 배급 | NRG-01 이후 | 사회 전반 `governmentStability` 하락 | — | SOC-03 | — |
| NRG-03 | 대체 에너지 긴급 도입 | NRG-01 장기화 | `healthFunding` 서서히 회복 | — | MED-02 | — |
| NRG-04 | 정전 사태 | NRG-01 무대응 지속 | 의료 시설 일시 마비(MED-06과 합류) | — | MIL-02 | N틱 후 복구 |
| NRG-05 | 연료 공급망 붕괴 | TRN-01/TRN-05 동시 다발 | 교통망 전역 효율 하락 | — | TRN 전역 | — |
| NRG-06 | 에너지 국제 협력 | INT-03 이후 | NRG 카테고리 위험 전역 완화 | — | NRG-03 | — |

### REF — 난민/이주

| ID | 이벤트명 | Trigger | World Effect | Evolution Opportunity | Chain→ | 종료조건 |
|---|---|---|---|---|---|---|
| REF-01 | 국경 통제 우회 이주 | ECO-05 이후 | 인접국 저확률 감염 유입(봉쇄 우회) | §9-C40(재감염 풀 부활)과 유사 원리 | POL-02(인접국) | — |
| REF-02 | 대규모 탈출 행렬 | SOC-06 무정부 진입 | 인접 3개국 이상으로 확산 위험(팬아웃) | §9-E49 | TRN-01 | — |
| REF-03 | 재해 이재민 발생 | DIS-02/FOD-04 | 국내 인구 이동, 지역 간 전파 가속 | — | REF-04 | — |
| REF-04 | 난민 수용소 형성 | REF-02/REF-03 이후 | 밀집도 상승으로 국지적 전파 급상승 | — | MED-01 | 수용소 해체 시 |
| REF-05 | 국제 난민기구 개입 | REF-04 + INT 카테고리 활성 | 수용소 내 확산 억제(대응책 강화) | — | MED-02 | — |
| REF-06 | 이주 노동자 귀환 | ECO 카테고리 회복 이후 | 해외 유입 경로 재활성화 | — | TRN-03 | — |

---

## 9. Event Chain 예시 (32개)

각 체인은 §4에서 정의한 4가지 구조(a 사다리/b 우발적 충돌/c 팬아웃/d 그래프 전파) 중 하나를
명시한다.

1. **[a] 발각→봉쇄 사다리**: POL-01 → POL-02(공항) → POL-03(국경) → POL-04(항구)
2. **[d] 항구 봉쇄의 그래프 전파**: POL-04 → TRN-01(인접 허브국 확률 상승) → TRN-02(검역 강화) → POL-03(인접국)
3. **[a] 의료 붕괴 사다리**: MED-01 → SOC-01(폭동) → MIL-01(계엄령) → SOC-06(무정부 진입)
4. **[b] 경제-사회 우발적 충돌**: ECO-01(시장 공포) → FOD-01(식량 가격 폭등) → SOC-01(폭동, ECO/FOD 두 경로가 동일 governmentStability를 압박해 우연히 충돌)
5. **[c] 치료제 임계값 팬아웃**: MED-03(안전장치 해제, 세계 사망률 10%) → 전 국가 동시 `cureProgress` 배율 판정 → 각국 독립적으로 MED-04 여부 결정
6. **[a] 정치 회복 사다리**: POL-05(정권 교체) → POL-06(국경 재개방) → TRN-03(임시 항로) → REF-06(이주 노동자 귀환)
7. **[b] 종교-의료 우발적 충돌**: REL-02(신앙 치유 신뢰) → MED-06(의료 인력 이탈, 무관해 보이지만 동일 healthFunding 경로로 충돌) → SCI-05(임상 스캔들)
8. **[d] 문화 행사의 팬아웃형 전파**: CUL-01(국제 스포츠 행사) → 참가국 전체 동시 TRN-03(임시 항로) 개설 → 각국 독립적으로 POL-02 판정
9. **[a] 군사 대응 사다리**: SOC-03(시위) → MIL-01(계엄령) → MIL-06(해제, 정권교체 시)
10. **[c] WHO 긴급회의 팬아웃**: INT-01 → 전 국가 동시 POL 사다리 가속 판정 → 일부 국가는 즉시 POL-02, 일부는 미달
11. **[b] 기후-식량 우발적 충돌**: CLM-04(가뭄) → FOD-02(농업 노동력 부족, 별개 원인) → FOD-04(기근 임박, 두 경로 합류)
12. **[a] 에너지 붕괴 사다리**: NRG-01(전력망 과부하) → NRG-02(배급) → NRG-04(정전) → MED-06(의료 마비)
13. **[d] 재난 연쇄 전파**: DIS-05(화산) → 인접국 교통 그래프 단절(TRN-05류) → ECO-01(해당 권역 시장 공포)
14. **[c] 팬데믹 공식 선언 팬아웃**: INT-06 → 전 카테고리 이벤트 확률 동시 상향 → 각 국가 개별적으로 어떤 카테고리가 먼저 터질지 판정
15. **[a] 난민 발생 사다리**: SOC-06(무정부) → REF-02(대탈출) → REF-04(수용소) → MED-01(수용소발 의료 붕괴)
16. **[b] 기술-언론 우발적 충돌**: TEC-01(추적 앱) → TEC-03(감시 반발) → NEWS-03(탐사 보도) — 감시 기술이 의도치 않게 언론 스캔들 촉발
17. **[d] 국내 이동 전파**: CUL-03(명절) → TRN-06(대이동) → 국가 자체 전파력 상승 → NEWS-05(해외 보도, 인접국 경계)
18. **[a] 과학 저항 사다리**: SCI-01(백신 성공) → SCI-05(부작용 스캔들) → REL-02(신앙 치유 신뢰 확산) → MED-06(의료 신뢰 붕괴)
19. **[c] 국제 제재 팬아웃**: INT-05(제재) → 해당국 ECO-05(암시장) 판정 + 별도로 REF-01(우회 이주) 판정 동시 진행
20. **[b] 문화-교통 우발적 충돌**: CUL-05(온라인 문화 전환) → TEC 카테고리 활성화(무관해 보이는 연결) → TEC-01 조기 도입 유도
21. **[a] 식량 위기 사다리**: FOD-01 → FOD-03(배급제) → FOD-06(사재기 폭동) → MIL-05(강제 수용)
22. **[d] 다중 허브 동시 봉쇄**: 3개 이상 허브국에서 독립적으로 POL-04 발생 → TRN-01이 각 쌍마다 그래프 전파 판정 → 전역 TRN 카테고리 확률 재조정(TRN-05 NRG-05 연쇄)
23. **[a] 에너지-군사 사다리**: NRG-01 → NRG-04(정전) → MIL-02(군 투입, 의료 대체) → 안정화 또는 MIL-01(불안 시 계엄)
24. **[c] 이상기후 선언 팬아웃**: CLM-06 → INT 카테고리 결의안(INT-03류) 전역 트리거 → 각국 `healthFunding` 개별 조정
25. **[b] 종교-문화 우발적 충돌**: REL-01(종교 집회) → CUL-01(국제 행사)과 동일 시기 중첩 → 두 경로가 같은 국가에서 합산되어 예상보다 강한 TRN-03 발생
26. **[a] 언론 통제 사다리**: MIL-01(계엄) → NEWS-06(언론 통제) → REL-03(지도자 발병, 은폐 실패) → NEWS-03(탐사 보도로 반전)
27. **[d] 재해 기반 이재민 그래프 전파**: DIS-02(홍수) → REF-03(이재민) → REF-04(수용소) → 인접국으로 REF-01류 유출
28. **[c] 오픈소스 프로토콜 팬아웃**: SCI-06(`cureProgress>=0.9`) → 전 저개발국 동시 MED-05 접근성 판정
29. **[a] 기술 감시 사다리**: TEC-01 → TEC-04(신속진단키트) → SCI-02(검출 기술 발전) → POL-01 가속(발각 민감도 상승 누적)
30. **[b] 에너지-식량 우발적 충돌**: NRG-02(에너지 배급) → FOD-02(농업 노동력, 별개 원인이나 동일 governmentStability 경로) → FOD-04(합류)
31. **[a] 국제 원조 사다리**: FOD-04(기근 임박) → INT-03(글로벌 펀드) → FOD-05(원조 도착) → ECO-04(회복)
32. **[d] 팬데믹 선언 이후 전역 그래프 재편**: INT-06 → 전 허브 국가 TRN 확률 동시 재계산(다대다 그래프 전파) → 이후 모든 TRN 이벤트가 상시 상향 배율 적용(영구 종료조건 없음, §7.4 "대체" 유형)

---

## 10. 구현 우선순위

Plague Inc. 분석 문서(§8.2)의 "실행 결과가 이미 있는 계획 문서는 코드가 정본"이라는 원칙에 따라,
아래는 실행 순서 제안이지 즉시 착수 지시가 아니다.

### Tier 1 — 기존 구조 확장만으로 가능 (`EventManager`/`HumanResistanceManager` 소폭 확장)
1. **POL 카테고리 사다리화**(§4a) — `HumanResistanceManager.CloseBorders()`를 3단계로 분리.
   가장 파급력이 크고 `PlagueIncReference.md`(삭제됨, §5)가 이미 방향을 제안했던 항목이라
   재작업 비용이 낮다.
2. **MED-03/MED-04(치료제 가속 사다리)** — `WorldState.cureProgress` 가속 배율에 `deadCount`
   비율을 연동. 기존 `RunTick()` 로직에 조건문 하나 추가 수준.
3. **INT-01/INT-02 기존 이벤트를 팬아웃 구조로 전환**(§4c) — 이미 존재하는 이벤트이므로
   "전 국가 동시 판정" 로직만 추가하면 됨.

### Tier 2 — 신규 소규모 시스템 필요
4. **TRN 카테고리 그래프 전파**(§4d) — 기존 교통망 허브 데이터 구조를 재사용해 "인접 허브
   확률 상승" 로직 신규 구현.
5. **`Country.GetCollapseStage()` 승격** — 표시용에서 `ApplyPolicy()`의 실제 입력으로 승격
   (Evolution System 2.0 문서 §8.1과 동일 제안).
6. **MED/SOC/MIL 국가별 사다리(§4a) 10종 내외 우선 구현** — 나머지 카테고리보다 플레이 체감이
   가장 큼(전체 32개 체인 예시 중 §9-1/§9-3/§9-9/§9-21이 이 조합).

### Tier 3 — Evolution System 2.0과의 통합 필요(선행 시스템 요구)
7. **Evolution Opportunity 연동**(§6, §8 전체) — `ResearchPopupController.LockReason()`에
   "관측 필요" 3번째 잠금 사유 추가(§9-D41)가 선행돼야 함 — CLAUDE.md 현재 TODO 항목과 직결.
8. **콤보 감지 매니저**(§9-D44) — MED/SOC/MIL 사다리가 안정화된 후에 착수(의존성 있음).

### Tier 4 — 장기/실험적
9. CUL/REL/TEC/NRG/REF 카테고리(문화·종교·기술·에너지·난민) — 압력원으로서 가치는 있으나
   Tier 1~3 대비 플레이 체감 대비 구현 비용이 높음. 코어 루프(POL/MED/SOC/TRN)가 먼저
   자리 잡은 뒤 순차 도입 권장.
10. Mutation 시스템(Evolution System 2.0 §8.4, §9-D42) — World Event System과 독립적으로
    작동 가능하므로 아무 때나 병행 착수 가능, 우선순위상 급하지 않음.

---

**참고 문서**: `Docs/Archive/EvolutionSystem2.0_PlagueIncDesignAnalysis.md`(짝 문서, Evolution
아이디어 52개), `Docs/Archive/ResearchDatabase_RuntimeSystems.md`(Event Modifier 노드 타입
기존 설계), `Docs/Archive/unity-editor-task.md` §10(BottleneckAnalyzer/ResearchRecommender —
이 문서의 이벤트 발생이 향후 이 두 컴포넌트의 입력으로 연동될 수 있음, 별도 검토 필요).
