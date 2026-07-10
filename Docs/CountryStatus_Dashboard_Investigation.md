# Country Status 화면 확장 조사 — "국가 대시보드" 설계안

조사 전용 문서. 코드 수정 없음. 대상 화면은 `CountryPopupController`(국가 클릭 시 뜨는 상세 모달,
`TacticalModalController` 상속, `CountryPopup.uxml/.uss`) — 현재 인구/감염자/사망자/의료 수준/
기후/공항/항구/국경 8행을 표시 중이다. (`CountryStatusPanelController`는 48개국 전체 목록 스크롤
화면이라 별개 — 이번 확장 대상이 아님)

---

## 1. 현재 사용 가능한 데이터 조사

### 1.1 `Country` (Contagion.Data) — 국가별 원본 필드

| 필드/프로퍼티 | 내용 |
|---|---|
| `population`, `infectedCount`, `deadCount` | 원본 카운터 (long) |
| `climate`, `developmentLevel` | 기후/개발 수준 enum |
| `isAirportOpen`, `isPortOpen`, `isBorderClosed` | 봉쇄 상태 bool 3종 |
| `healthFunding`, `healthFundingCap`, `governmentStability` | 치료제 기여도/상한/정부 안정성 (0~1) |
| `neighborCountryIds`/`airRouteCountryIds`/`seaRouteCountryIds` | 연결 그래프 (id 리스트) |
| `LivingPopulation` | population - deadCount (계산) |
| `SusceptibleCount` | LivingPopulation - infectedCount, 즉 **건강 인구** (계산) |
| `HealthLevel` | developmentLevel → 0.8/0.5/0.2 (계산) |
| `ResearchMultiplier` | developmentLevel → 1.5/0.8/0.2 (계산) |
| `GetCollapseStage()` | 사망률 기준 6단계(Normal~Extinct) (계산) |

### 1.2 `WorldState` / `Pathogen` — 전역 데이터 (국가 화면에서 참조만 가능)

`WorldState`: totalPopulation/infectedCount/deadCount(세계 합계), cureProgress, plagueVisibility,
currentDay, `GetResistanceStage()`(5단계), `GetMortalityStage()`(4단계).
`Pathogen`: infectivity/severity/lethality/drugResistance, 기후별 환경저항.

### 1.3 매니저에서 가져올 수 있는 데이터

| 소스 | 제공 데이터 | 비고 |
|---|---|---|
| `WorldDataManager.Countries` | 48개국 전체 리스트(`IReadOnlyList<Country>`) | **순위 계산의 유일한 필요 조건 — 이미 있음** |
| `WorldDataManager.OnCountryChanged` | 국가 값 변경 시 이벤트 | 팝업이 이미 구독 중 |
| `SimulationManager.OnInfectionMilestone/OnDeathMilestone` | 국가별 마일스톤 이벤트 | 현재 BubbleSpawner 전용, 팝업 미구독 |
| `HumanResistanceManager` | 공항/국경/항구 폐쇄 로직, 붕괴단계별 funding 배율 | **폐쇄 발생을 Debug.Log로만 남김 — 구조화된 이벤트/이력 없음** |
| `EventManager.OnNewsEvent` | 자연재해/정치불안 등 국가 타겟 이벤트 | `NewsEvent`는 category/text/day만 있고 **countryId 필드가 없음** — 텍스트에 국가명이 들어갈 뿐 |
| `HudSparkline` (UI 코드) | Painter2D로 직접 그래프를 그리는 기존 선례 | UI Toolkit에 내장 차트가 없다는 문제의 **답이 이미 코드베이스에 있음** |
| `TacticalModalController.AddRow()` | data-row 헬퍼 (라벨+값, severity 클래스 지원) | 신규 항목도 동일 패턴으로 붙일 수 있음 |

**결론**: 원그래프/통계/의료상태/순위 4개는 **지금 있는 데이터만으로 충분**하다. "최근 국가
이벤트"만 구조화된 이력이 없어 신규 시스템이 필요하다.

---

## 2. 신규 표시 항목 평가

### 2.1 감염 상태 원그래프 (건강/감염/사망)

- **데이터**: `SusceptibleCount`(건강) / `infectedCount`(감염) / `deadCount`(사망) — population 3분할, 전부 기존 필드.
- **UI Toolkit 구현 난이도**: 중. UI Toolkit에는 내장 차트 위젯이 없지만, `HudSparkline.cs`가
  이미 `VisualElement.generateVisualContent` + `MeshGenerationContext.painter2D`로 커스텀
  그래프를 그리는 선례다. 원그래프는 `Painter2D.Arc()`로 3개 섹터를 그리면 되고, 분량/난이도는
  HudSparkline과 비슷한 수준(약 60~90줄의 신규 클래스)이다.
- **성능**: `MarkDirtyRepaint()`는 값이 바뀔 때만 호출(HudSparkline과 동일 패턴) — 팝업은 이미
  "보이는 동안만 해당 국가 변경 시 재계산" 구조라 매 틱 48개국 전체를 다시 그리는 기존 국가현황
  패널의 최적화 이력(O(48)→O(1))과 같은 함정에 빠지지 않는다. 영향 미미.
- **평가**: 의미 있음(직관적 비율 파악) / 구현 난이도 낮음 / 모바일 적합(정사각 컴팩트) / Plague
  Inc 스타일과 부합. **즉시 구현 가능.**

### 2.2 감염 통계 (감염률/치사율/생존자 수/건강 인구 수)

| 지표 | 공식 | 데이터 상태 |
|---|---|---|
| 감염률 | `infectedCount / LivingPopulation` | 이미 CountryDock/국가현황 패널에서 쓰는 공식 그대로 |
| 건강 인구 수 | `SusceptibleCount` | 기존 필드 |
| 생존자 수 | `LivingPopulation` (또는 "비감염 생존자"로 SusceptibleCount와 구분) | 기존 필드 — 단, 이 게임엔 "감염 후 회복" 개념이 없어(치료제 100% 완성 시 세계 단위로 일괄 박멸) "생존자"는 "죽지 않은 사람" 의미로 한정해야 함 |
| 치사율(CFR) | 후보 A: `deadCount / population`(전체 인구 대비, 근사) / 후보 B: `deadCount / (deadCount + infectedCount)`(현재 스냅샷 기준 근사 CFR) | **정확한 역학적 치사율(누적 감염자 대비 사망자)은 현재 데이터로 불가능** — `SimulationManager`가 "누적 감염 경험 인구"를 별도로 카운트하지 않고 `infectedCount`를 늘었다 줄었다 하는 현재값으로만 관리하기 때문. 후보 A/B는 근사치이며 라벨에 "추정" 표기 권장 |
- **평가**: 감염률/건강 인구/생존자는 즉시 구현 가능. 치사율은 근사식으로 즉시 구현하거나,
  정확도가 중요하면 `Country`에 `cumulativeInfectedCount`(단조 증가 카운터) 필드를 추가하고
  `SimulationManager.RunTick()`에서 `newInfected`만큼 더해주는 소규모 확장이 필요(신규 시스템은
  아니고 기존 클래스에 필드 1개 + 한 줄 추가 수준).

### 2.3 의료 시스템 상태 (정상/주의/과부하/붕괴)

- 기존 `GetCollapseStage()`는 **사망률** 기준 사회 붕괴 단계(무질서/무정부 등)라 의료 시스템
  "과부하" 개념과는 축이 다르다. 새 계산식 제안:
  `load = infectionRatio * (1f - HealthLevel)` (감염 비율이 높고 의료 수준이 낮을수록 부하 증가)
  - 정상: load < 0.1 / 주의: 0.1~0.3 / 과부하: 0.3~0.6 / 붕괴: ≥ 0.6 (임계값은 플레이테스트로 조정)
- **데이터**: `infectedCount`, `LivingPopulation`, `HealthLevel` 모두 기존 필드 — **신규 데이터
  불필요, 계산식만 추가**(UI 컨트롤러 내부 순수 함수, `Country.cs` 변경 없이도 가능).
- **평가**: 의미 있음(플레이어가 "이 나라는 이제 의료가 무너진다"를 직관적으로 인지) / 난이도 낮음
  (data-row 1줄 + severity 색상) / **즉시 구현 가능.**
- 주의: 기존 `GetCollapseStage()`(무질서/무정부 등) 라벨과 이름이 겹치지 않게 "의료 부하" 등으로
  구분해서 표기해야 두 축이 헷갈리지 않는다(DESIGN.md도 severity와 노드상태 축을 분리하라고 명시).

### 2.4 국가 순위 (감염자/사망자/감염률, 48개국 기준)

- **데이터**: `WorldDataManager.Instance.Countries`가 48개국 전체를 이미 들고 있다 — 국가현황
  패널이 정확히 이 리스트를 순회 중이므로 접근 경로 검증됨.
- **정렬 비용**: 48개 항목 LINQ `OrderByDescending` 1회 = 사실상 무시 가능한 비용(수십~수백
  마이크로초 이하). 매 틱 계산할 필요 없이 **팝업이 열릴 때/표시 중인 국가 값이 바뀔 때만**
  계산하면 되므로(이미 그런 이벤트 구독 구조), 국가현황 패널이 겪었던 "매 틱 전체 재계산" 함정과
  무관.
- **구현**: 3개 지표 각각 `Countries.OrderByDescending(c => 지표).ToList().FindIndex(c => c.id == 현재국가) + 1` 형태의 순수 함수. 화면에는 "감염자 12위/48" 같은 한 줄이면 충분.
- **평가**: 즉시 구현 가능. 다만 3개 지표를 다 보여주면 3줄이 늘어나므로 화면 밀도 고려 필요(2.6 참고).

### 2.5 최근 국가 이벤트 (공항 폐쇄/국경 폐쇄/의료 수준 변화)

- **현재 시스템 조사 결과**: 구조화된 "국가별 이벤트 이력"이 **없다**.
  - `HumanResistanceManager.ApplySequentialClosure()`가 공항/국경/항구 상태를 바꿀 때 `Debug.Log`만
    남기고 구독 가능한 이벤트나 이력 리스트를 두지 않는다.
  - `EventManager.OnNewsEvent`는 국가를 타겟으로 하는 이벤트(자연재해/처형 등)가 있지만 `NewsEvent`
    구조체에 `countryId` 필드가 없어 텍스트 문자열에서 국가명을 파싱해야 하는데, 이는 다국어/이름
    중복 리스크가 있어 신뢰할 수 없는 방법이다.
  - `NewsFeedController`가 전역 뉴스 30건을 캡(ring buffer)해서 보여주는 선례는 있으나 국가별
    필터링 기능은 없다.
- **구현 비용 추정**(신규 시스템, 코드 수정 시 필요할 작업 — 지금은 조사만):
  1. `NewsEvent`에 `string countryId` 필드 추가(nullable, 국가 비타겟 이벤트는 null).
  2. `EventManager`의 국가 타겟 이벤트 6종 중 자연재해/정치불안/처형 3종은 이미 대상 `country`
     변수를 들고 있으므로 `RaiseNews` 호출에 country.id만 얹으면 됨(낮은 비용).
  3. `HumanResistanceManager`에 `OnCountryFlagChanged(Country, string flagName, bool newValue)` 같은
     이벤트를 추가해 공항/국경/항구 변화 시 발행(`Debug.Log` 호출부 옆에 한 줄 추가).
  4. 팝업 쪽에 국가별 최근 N건(예: 5건) 캡 리스트 — `NewsFeedController.AddEntry`의 ring-buffer
     패턴 재사용, 국가 id로 필터링.
  - 기존 이벤트/델리게이트 패턴이 이미 코드베이스 전역에 있어(위 표 참고) 처음부터 설계하는 건
    아니지만, **Country.cs/EventManager.cs/HumanResistanceManager.cs 3개 파일에 걸친 변경**이라
    다른 4개 항목(전부 UI 레이어 단독 작업)보다는 확실히 비용이 크다.
- **평가**: "추가 시스템 필요"로 분류. 의미는 크다(공항이 왜 닫혔는지 맥락 제공)지만 후순위 권장.

### 2.6 화면 밀도 — 5개 기준 평가 요약

| 항목 | 플레이어 의미 | 기존 데이터로 가능 | UI 복잡도 | 모바일 세로 적합 | Plague Inc 스타일 |
|---|---|---|---|---|---|
| 원그래프 | 높음 | O | 중 | O | O |
| 감염 통계 | 높음 | O(치사율은 근사) | 낮음 | O | O |
| 의료 시스템 상태 | 높음 | O(계산식 신규) | 낮음 | O | O |
| 국가 순위 | 중~높음 | O | 낮음 | O(1줄 요약 시) | O |
| 최근 이벤트 | 높음 | **X (신규 필요)** | 중 | O | O |

`CountryPopup.popup-root`는 `width: 340px`, `top: 30%`로 고정 폭에 세로 방향 auto-height라(최대
높이 제한 없음) 항목을 추가할수록 패널이 길어진다. 1440×3120 세로 화면 기준 top 30%(≈936px) 아래로
약 2000px 여유가 있어 당장 담기는 문제없지만, 5개를 전부 넣으면 8행(기존) + 원그래프 + 6~8행(신규)
+ 이벤트 로그로 상당히 길어지므로 아래 3안에서는 시각 밀도를 낮추는 배치를 제안한다.

---

## 3. 즉시 구현 가능 (기존 데이터 + 계산만으로 충분)

| 항목 | 필요 데이터 | 구현 난이도 | 예상 UI |
|---|---|---|---|
| 감염 상태 원그래프 | SusceptibleCount/infectedCount/deadCount | 중 (Painter2D 신규 클래스, HudSparkline 패턴 재사용) | 지름 90~110px 도넛 + 우측 3줄 범례(색상 점+수치) |
| 감염률/건강 인구/생존자 수 | infectedCount, SusceptibleCount, LivingPopulation | 낮음 (data-row 3줄) | 기존 `AddRow()` 패턴 그대로 |
| 치사율(근사) | deadCount, infectedCount, population | 낮음 (근사식 + "추정" 라벨) | data-row 1줄, 값 옆에 회색 각주 |
| 의료 시스템 부하 | infectedCount, LivingPopulation, HealthLevel | 낮음 (계산식 + 4단계 severity 색) | data-row 1줄 (정상=info, 주의=중립, 과부하/붕괴=danger) |
| 국가 순위 | WorldDataManager.Countries (48개) | 낮음 (LINQ 정렬, 표시 시점에만 계산) | data-row 1~3줄 ("감염자 12위/48" 등) |

## 4. 추가 시스템 필요

| 항목 | 필요한 신규 데이터 | 구현 비용 |
|---|---|---|
| 최근 국가 이벤트 (공항/국경 폐쇄, 의료 수준 변화) | `NewsEvent.countryId` 필드, `HumanResistanceManager`의 국가별 플래그 변경 이벤트, 국가별 이력 ring-buffer | 중 — 3개 파일(EventManager/HumanResistanceManager/신규 팝업 로직) 걸친 변경, 기존 이벤트 패턴 재사용 가능해 설계 난이도 자체는 낮음 |
| 정확한 누적 치사율(CFR) | `Country.cumulativeInfectedCount`(단조 증가) 필드 + SimulationManager 한 줄 | 낮음 — 위 근사치로 즉시 대체 가능하므로 필수는 아님 |

---

## 5. 최종 추천안 — Country Status(팝업) 레이아웃 시안

기존 8행을 없애지 않고 아래 순서로 재배치·보강한다(굵은 구분선은 시각적 그룹, 실제 UI는
`data-row`/신규 도넛 엘리먼트 조합):

```
[헤더] 국가명                                    ✕
──────────────────────────────────────────
[도넛 차트 100px]   건강  ██ 1,204만 (72%)
                      감염  ██   382만 (23%)
                      사망  ██    83만 ( 5%)
──────────────────────────────────────────
인구            14,120,000
감염률           23.1%
치사율(추정)      3.4%
의료 시스템 상태   ⚠ 과부하        (severity 색)
──────────────────────────────────────────
의료 수준         선진국
기후             온대
붕괴 단계         무질서           (기존 CountryStatusPanel 라벨 재사용)
──────────────────────────────────────────
세계 순위         감염자 12위 · 사망자 8위 · 감염률 19위  (48개국 중)
──────────────────────────────────────────
공항             폐쇄  (danger)
항구             개방  (info)
국경             봉쇄  (danger)
──────────────────────────────────────────
[최근 이벤트 — 추가 시스템 구현 후]
· 12일차 공항 폐쇄
· 9일차 자연재해로 감염 급증
· 5일차 국경 봉쇄
```

설계 의도:
1. 도넛 차트를 최상단에 둬 "이 나라 지금 상태"를 0.5초 안에 파악하게 하고, 그 아래 텍스트 통계는
   차트의 수치적 뒷받침 역할로 배치(Plague Inc 통계창과 동일한 정보 위계).
2. "의료 시스템 상태"를 기존 "의료 수준"(정적 국력 지표) 바로 위, 감염 통계 블록 끝에 둬서 "지금
   무너지고 있는지"를 강조 — 정적 스펙(의료 수준/기후)과 동적 위기(의료 부하)를 시각적으로 분리.
3. "세계 순위"는 한 줄 요약(3개 지표를 콤마로 압축)으로 넣어 패널 길이를 아끼고, 필요하면 탭해서
   펼치는 방식(추후 UX 개선 후보)도 가능.
4. 최근 이벤트는 신규 시스템이 필요하므로 별도 섹션으로 맨 아래 배치 — 1단계 구현(도넛/통계/의료
   부하/순위)과 2단계 구현(이벤트 로그)을 분리해서 진행할 수 있도록 레이아웃도 그에 맞춰 확장 가능한
   구조로 제안.
5. 패널이 길어지는 문제는 화면 여유(top 30% 아래 ~2000px)로 당장은 괜찮지만, 실기기 테스트에서
   넘칠 경우 `modal-rows`를 `ScrollView`로 교체하는 안전장치를 QA 체크리스트에 추가 권장.

### 구현 순서 제안 (착수 시)
1. 감염 상태 원그래프 (독립적, 시각 임팩트 가장 큼)
2. 감염 통계 3줄 + 치사율(근사) — 원그래프와 세트로 묶어서 한 번에
3. 의료 시스템 부하 계산식 + 색상
4. 국가 순위 한 줄 요약
5. (별도 작업으로 분리) 최근 국가 이벤트 — EventManager/HumanResistanceManager 확장 선행 필요
