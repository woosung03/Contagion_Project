# Contagion Project — 문서 구조 정리 분석 보고서

**작성일**: 2026-07-14
**범위**: `CLAUDE.md`(프로젝트 루트) + `Docs/` 폴더 내 .md 27개 = 총 28개 문서
**주의**: 이 보고서는 분석 전용이다. 어떤 파일도 이동·삭제·수정하지 않았다.

---

## 0. 조사 방법

전체 28개 문서를 직접 읽고(DevLog.md는 4,117줄이라 헤더/최근 Step 위주로 발췌 확인 + 서브에이전트로
"제안이 실제 코드에 반영됐는지"를 `DefaultUpgradeTreeFactory.cs`/`UpgradeTreeView.cs` 등 실제 코드와
대조 검증), 각 문서가 (1) 지금도 유효한 정본인지 (2) 이미 실행 완료되어 이력으로만 남을 문서인지
(3) 다른 문서와 내용이 겹치는지를 판단했다.

---

## 1. 문서별 분석

### 1.1 핵심 운영 문서

| 문서 | 목적 | 유효성 | 최신성 | 중복 | 필요성 |
|---|---|---|---|---|---|
| `CLAUDE.md` | 세션 시작 시 로드되는 현재 상태+TODO 브리핑 | 유효 | 2026-07-12, 최신 | `DevelopmentWorkflow.md`와 "문서 분류 규칙" 중복 | 필수 |
| `Docs/GameDesignDocument.md` | 게임 규칙/공식/밸런스/튜닝 원본 스펙 | 유효 | 2026-07-09 이후 안정, 16절에 튜닝 포인트 계속 추가됨 | 없음 | 필수 |
| `Docs/DevLog.md` | Step별 구현 배경·설계 변경 이유 아카이브 | 유효, 살아있는 이력 | Step 84까지(2026-07-12) | 없음(태생이 이력 문서) | 필수 |
| `Docs/DESIGN.md` | 디자인 시스템 정본(토큰/컬러/컴포넌트/Do·Don't) — **Source of Truth** | 유효 | 2026-07-10, `UI_Redesign_Plan.md` §9 제안이 아직 미반영 | `UI_Design.md`가 화면 배치만 다루도록 역할 분리되어 있어 중복 없음 | 필수 |
| `Docs/UI_Design.md` | 화면별 배치/우선순위/실행계획(§16까지 Step 80 기준 갱신) | 유효, 살아있는 실행 문서 | 최신(Step 80 반영) | 과거 토큰 제안 내용은 이미 DESIGN.md로 이관 완료(문서 자체에 명시) | 필수 |
| `Docs/Architecture.md` | 네임스페이스/폴더구조/엔진 정보 | 유효 | 짧고 최신 | 없음 | 필수 |
| `Docs/QA_Checklist.md` | 검증/플레이테스트 체크리스트 | 유효, 미해결 항목 다수 | 최신 | 없음 | 필수 |
| `Docs/Release_Checklist.md` | 배포 전 점검(저작권/SDK/스토어) | 유효(대부분 미착수) | 최신 | 없음 | 필수 |
| `Docs/unity-editor-task.md` | Unity 에디터 수동 작업 목록(씬 배선 등) | 유효, 대기 작업 존재 | 최신 | 없음 | 필수 |

### 1.2 Research Database / UpgradeTree — 현재도 능동적으로 참조되는 문서

| 문서 | 목적 | 유효성 | 최신성 | 비고 |
|---|---|---|---|---|
| `Docs/ResearchDatabase_V2_ImplementationPlan.md` | Research Database v2(브랜치 보드+리스트+팝업) 구현 커밋 단위 계획 | 유효, 현재 진행 중(커밋 7 나머지) | CLAUDE.md TODO가 §1/§4를 직접 근거로 지목 | **필수** — 지금 당장의 다음 작업 근거 |
| `Docs/ResearchDatabase_V2_UI_StructureDesign.md` | v2 브랜치 현황판/팝업 상세 구조 확정안 | 유효 | Implementation Plan의 전제 문서 | **필수** |
| `Docs/ResearchDatabase_RuntimeSystems.md` | 연구 효과를 시뮬레이션에 연결하는 아키텍처(Phase 2~4) | 유효, 아직 미착수 | CLAUDE.md TODO가 "§9 순서를 따르라"고 명시 | **필수** — 향후 작업 설계도 |
| `Docs/UpgradeTree_ResearchDatabase_NodeMapping.md` | 45개 노드 전수 매핑(이름/효과/타입) | 유효, 일부만 반영(표시명 리네이밍은 미반영) | CLAUDE.md TODO가 "NodeMapping.md §8 잔여 항목" 직접 지목 | **필수** — Phase 2+ 근거 자료 |
| `Docs/UpgradeTree_FinalDecision.md` | UpgradeTree 45노드 cost/effects 최종 수치안 | **이미 코드에 100% 반영 완료**(검증됨: `DefaultUpgradeTreeFactory.cs` 수치 일치, 코드 주석에 §번호까지 인용) | 구현 완료, 그러나 **DevLog에 Step으로 기록되지 않음** | 참조 가치는 있으나 "결정 문서"가 아니라 "완료된 스냅샷" — §2에서 상세 |
| `Docs/ResearchDatabase_Design.md` | Research Database 철학·효과 설계 원칙(849줄) | 유효, 철학적 기반 문서 | NodeMapping/RuntimeSystems가 이 문서를 전제로 삼음 | 통합 후보 — §2에서 상세 |

### 1.3 완료된 조사 · 제안 · 리뷰 문서 (Archive 후보)

아래 문서들은 전부 자기 서문에 **"조사 전용 문서"/"순수 설계 문서, 코드 작성 금지"/"승인 대기, 코드 미수정"** 이라고 명시하고 있고, 실제로 결론이 후속 문서(주로 `_FinalDecision.md`, `_V2_ImplementationPlan.md` 등)에 흡수되어 실행되었거나, 실행되지 않은 채 방치되어 있다.

| 문서 | 성격 | 결론이 흡수된 곳 | 현재 상태 |
|---|---|---|---|
| `Docs/UI_UX_Review.md` | 스크린샷 4장 기반 일회성 진단(A/B/C 분류) | `UI_Redesign_Plan.md`의 근거 | CLAUDE.md 현재 TODO에 전혀 언급 없음 — 사실상 미실행 상태로 방치 |
| `Docs/UI_Redesign_Plan.md` | 위 리뷰를 근거로 한 실행 설계안(지도 다크화/버튼 시스템 등 8항목, 우선순위표까지 있음) | 미실행 | 코드 작성 금지 문서였고 실제 착수 기록 없음 — **아직 유효한 미착수 제안서** |
| `Docs/UpgradeTree_Redesign_Investigation.md` | 원작 대조 조사("정답 트리 없는 구조") | `UpgradeTree_MVP_ChangeProposal.md` → `_FinalDecision.md` | 결론 100% 흡수, 구현 완료 |
| `Docs/UpgradeTree_MVP_ChangeProposal.md` | 45노드 수치 변경 제안(승인 대기로 명시) | `_FinalDecision.md` | 결론 100% 흡수, 구현 완료 |
| `Docs/UpgradeTree_MVP_BalanceReview.md` | 제안 수치의 지배관계(dominance) 검증 | `_FinalDecision.md` | 결론 100% 흡수, 구현 완료 |
| `Docs/UpgradeTree_ResearchDatabase_Investigation.md` | "트리 캔버스 vs 리스트" 상위 구조 조사 — Research Database 전환의 시발점 | `_ScreenDesign.md` → `ResearchDatabase_Design.md` 등 후속 전체 | 결론(리스트 전환) 채택되어 구현 완료 |
| `Docs/UpgradeTree_ResearchDatabase_ScreenDesign.md` | Research Database 화면 레이아웃 1차 확정안(인라인 리스트) | `ResearchDatabase_MobileNav_Investigation.md`→`UI_FinalReview.md`→`V2_UI_StructureDesign.md`로 **재설계되어 대체됨** | **완전히 superseded** — 지금 화면은 이 문서가 아니라 V2 문서 기준으로 구현됨 |
| `Docs/ResearchDatabase_MVP_ImplementationPlan.md` | Research Database v1(Commit 1~5) 구현 계획 | `V2_ImplementationPlan.md`가 본문에 "이번 문서로 대체" 명시 | Commit 1(더미 UI 셸)만 실행되고 즉시 v2로 전환됨 |
| `Docs/ResearchDatabase_MobileNav_Investigation.md` | 모바일 정보 밀도 재설계 5안 비교(A~E) | `UI_FinalReview.md`로 압축(C/D/E) | 결론 흡수, 구현 완료 |
| `Docs/ResearchDatabase_UI_FinalReview.md` | C/D/E × 상세표시 6조합 비교, "E+팝업" 최종 채택 | `V2_UI_StructureDesign.md`가 이 결정을 그대로 이어받음 | 결론 흡수, 구현 완료 |
| `Docs/CountryStatus_Dashboard_Investigation.md` | CountryPopup 대시보드 확장 조사(도넛차트/순위/이동통제 등) | CLAUDE.md "완료된 시스템"의 "CountryPopup 국가 대시보드 확장" 항목 | 결론 100% 구현 완료(DevLog Step 78-79) |
| `Docs/PlagueIncReference.md` | Plague Inc. 원작 대조 및 백로그 4건 | 문서 자체가 "**2026-07-03 Step 16에서 4번 항목까지 전부 반영 완료 — 이 문서의 백로그는 더 이상 남아있지 않다**"고 명시 | **완전 종료**, 남은 작업 없음 |

### 1.4 중복/저가치 문서

| 문서 | 문제 |
|---|---|
| `Docs/DevelopmentWorkflow.md` | "문서 역할별 분리 규칙"을 다루는 문서인데, 이 규칙은 이미 `CLAUDE.md` 하단 "프로젝트 규칙" 섹션에 거의 동일한 표(문서 분류 규칙 + CLAUDE.md 유지 규칙)로 들어가 있다. 두 문서를 대조한 결과 **DevelopmentWorkflow.md의 내용이 CLAUDE.md에 이미 100% 포함**되어 있고, 신규 정보가 없다. 두 곳에 같은 규칙이 있으면 향후 규칙이 바뀔 때 한쪽만 갱신되어 어긋날 위험이 있다. |

---

## 2. 중복 클러스터 심층 분석 (요청 영역)

### 2.1 UI_Design / UI_Redesign / UI_UX

세 문서는 **역할이 이미 문서 자체 서문으로 명확히 분리**되어 있어, 이름이 비슷해도 실질적인 내용 중복은
없다.

- `UI_Design.md` — **정본, 살아있는 실행 문서.** "화면별 배치/우선순위/실행계획"만 다루고, 토큰/색상은
  전부 `DESIGN.md`로 이미 이관했다고 문서 서두에 명시. §16 실행계획이 Step 80까지 갱신되며 계속
  쓰이고 있다.
- `UI_UX_Review.md` — **일회성 진단.** 스크린샷 4장을 놓고 "지금 이 상태가 컨셉에 부합하는가"만 진단한
  결과물. 코드/문서 수정 없음이 원칙이라 명시. 결론(A/B/C 항목)은 `UI_Redesign_Plan.md`가 이어받았다.
- `UI_Redesign_Plan.md` — **미실행 제안서.** 위 리뷰의 A/B 항목을 실행 가능한 설계안(지도 다크화,
  버튼 시스템 3등급, DESIGN.md 보강 항목 등)으로 옮긴 문서. 우선순위 1~8위까지 정해놨지만, 현재
  `CLAUDE.md` TODO 어디에도 이 계획 착수가 언급되지 않는다 — **결론: 독립적으로 존재해야 한다.
  통합 대상이 아니라, "다음에 착수할 백로그"로 보존해야 하는 문서다.**

**판단**: 세 문서는 통합하면 오히려 "정본/일회성 진단/미착수 제안"이라는 서로 다른 성격이 뒤섞여
혼란만 커진다. `UI_Design.md`는 A(핵심 유지), `UI_UX_Review.md`는 결론이 이미 다른 문서로
흡수된 완료 이력이라 C(Archive), `UI_Redesign_Plan.md`는 **아직 유효한 미착수 제안서**라 Archive로
옮기더라도 "차기 작업 후보"라는 태그를 달아 쉽게 찾을 수 있게 해야 한다(§4 참고).

### 2.2 UpgradeTree 관련 (5개 문서)

`Redesign_Investigation.md` → `MVP_ChangeProposal.md` → `MVP_BalanceReview.md` → `FinalDecision.md`는
**순서대로 조사 → 제안 → 검증 → 확정**의 선형 파이프라인이며, 서로 겹치는 게 아니라 각 단계가 앞
단계를 소비하고 좁혀나가는 구조다. 코드 대조 결과 **`FinalDecision.md`의 수치가 이미
`DefaultUpgradeTreeFactory.cs`에 전부 반영되어 있다** — 즉 파이프라인은 완결됐다.

문제는 `FinalDecision.md`가 "구현 확정판"이라는 이름과 달리, 실제 반영 시점이 DevLog에 별도 Step으로
기록되지 않았다는 점이다(Research Database 대형 작업에 묻혀 들어감). `DevelopmentWorkflow.md`/
`CLAUDE.md` 규칙상 "구현 배경/설계 변경 이유"는 DevLog가 담당해야 하는데, 지금은 그 역할을
`FinalDecision.md`가 대신 하고 있다.

`UpgradeTree_ResearchDatabase_Investigation.md`는 이름은 UpgradeTree 계열이지만 실질적으로는
ResearchDatabase 파이프라인의 시작점이라 §2.3에서 함께 다룬다.

**판단**: 앞 3개(`Redesign_Investigation`/`MVP_ChangeProposal`/`MVP_BalanceReview`)는 결론이
`FinalDecision.md` 하나로 완전히 흡수됐으므로 독립 보존할 실익이 낮다 → Archive(C). `FinalDecision.md`는
당장 삭제하기엔 "왜 지금 수치가 이런가"를 설명하는 유일한 문서라 완전한 대체재가 없다 → **DevLog.md에
정식 Step으로 내용을 옮겨 적은 뒤에 Archive로 보내는 것을 권장**(B, 통합 후보).

### 2.3 ResearchDatabase 관련 (UpgradeTree_ResearchDatabase_* 포함 9개 문서)

파이프라인이 두 갈래로 나뉜다.

1. **구조 결정 갈래**: `UpgradeTree_ResearchDatabase_Investigation.md`(트리 vs 리스트) →
   `UpgradeTree_ResearchDatabase_ScreenDesign.md`(1차 레이아웃, 인라인 리스트) →
   `ResearchDatabase_MobileNav_Investigation.md`(밀도 문제 발견, 5안 비교) →
   `ResearchDatabase_UI_FinalReview.md`(E+팝업 최종 채택) →
   `ResearchDatabase_V2_UI_StructureDesign.md`(현재 정본) →
   `ResearchDatabase_V2_ImplementationPlan.md`(현재 진행 중인 구현 계획).
   **`ScreenDesign.md`는 이 체인 안에서 완전히 superseded** — 지금 화면은 이 문서가 아니라
   V2 문서 기준으로 만들어졌다(DevLog Step 83이 "기준 문서를 V2로 전환"이라고 명시).

2. **내용/철학 갈래**: `ResearchDatabase_Design.md`(연구 시스템 철학) →
   `UpgradeTree_ResearchDatabase_NodeMapping.md`(45노드 전수 매핑) →
   `ResearchDatabase_RuntimeSystems.md`(효과를 시뮬레이션에 연결하는 아키텍처). 이 셋은 서로 다른
   레이어(철학/데이터/아키텍처)를 다뤄 중복이 아니며, **NodeMapping과 RuntimeSystems는 CLAUDE.md
   TODO의 Phase 2~4 항목이 직접 지목하는 살아있는 참조 문서**라 지금 당장 유지해야 한다.

3. `ResearchDatabase_MVP_ImplementationPlan.md`(v1 구현 계획)는 `V2_ImplementationPlan.md`
   본문이 "이번 문서로 대체"라고 명시적으로 폐기 선언을 했다 — Commit 1만 반영되고 즉시 v2로
   전환됐다.

**판단**: 구조 결정 갈래의 중간 산출물(`ScreenDesign`/`MobileNav_Investigation`/`UI_FinalReview`)과
`MVP_ImplementationPlan`은 전부 Archive(C) — 최종 결론이 `V2_UI_StructureDesign`/
`V2_ImplementationPlan` 두 문서로 이미 수렴했다. 철학/데이터/아키텍처 갈래(`Design`/`NodeMapping`/
`RuntimeSystems`)는 지금도 능동적으로 참조되는 문서라 유지해야 한다 — 다만 `ResearchDatabase_Design.md`는
분량이 크고(849줄) 이미 승인된 자산 위에 다시 설계를 얹는 성격이라, 장기적으로는
`GameDesignDocument.md`의 "연구/업그레이드 시스템" 절로 요약 편입하고 상세본은 참조 링크로 돌리는
통합을 고려할 만하다(B, 지금 당장 급하지 않음).

---

## 3. 카테고리별 분류표

### [A] 반드시 유지해야 하는 핵심 문서 (14개)

| 문서 | 사유 |
|---|---|
| `CLAUDE.md` | 세션 시작 브리핑 |
| `Docs/GameDesignDocument.md` | 게임 설계 원본 |
| `Docs/DevLog.md` | 구현 이력 아카이브 |
| `Docs/DESIGN.md` | 디자인 시스템 정본 |
| `Docs/UI_Design.md` | 화면 설계 정본, 실행 중 |
| `Docs/Architecture.md` | 코드 구조 참고 |
| `Docs/QA_Checklist.md` | 검증 체크리스트 |
| `Docs/Release_Checklist.md` | 배포 체크리스트 |
| `Docs/unity-editor-task.md` | 에디터 수동 작업 대기열 |
| `Docs/ResearchDatabase_V2_ImplementationPlan.md` | 현재 진행 중인 작업의 직접 근거 |
| `Docs/ResearchDatabase_V2_UI_StructureDesign.md` | v2 구조 정본 |
| `Docs/ResearchDatabase_RuntimeSystems.md` | Phase 2~4 TODO 근거 |
| `Docs/UpgradeTree_ResearchDatabase_NodeMapping.md` | Phase 2~4 TODO 근거 |
| `Docs/PlagueIncReference.md`* | *유효성 자체는 종료됐으나 별도 사유로 §1.3/§3 C군에 재배치(아래 참고) |

> `PlagueIncReference.md`는 위 표에 실수로 두 번 나오지 않도록, 최종 분류는 **C군**으로 확정한다
> (문서 스스로 "백로그 종료"를 선언했기 때문). A군은 위 표에서 이 항목을 제외한 **13개**가 맞다.

### [B] 유지하되 통합 후보 (2개)

| 문서 | 통합 방향 |
|---|---|
| `Docs/UpgradeTree_FinalDecision.md` | 내용을 `DevLog.md`에 정식 Step으로 옮겨 적어 "구현 배경" 기록 공백을 메운 뒤, 원본은 Archive로 이동 |
| `Docs/ResearchDatabase_Design.md` | 장기적으로 `GameDesignDocument.md`의 연구 시스템 절로 요약 편입 검토(지금 당장 급하지 않음, 규모가 커서 신중히 진행) |

### [C] Archive로 이동 가능한 문서 (12개)

| 문서 | 사유 |
|---|---|
| `Docs/UI_UX_Review.md` | 일회성 스크린샷 진단, 결론이 Redesign_Plan에 흡수 |
| `Docs/UI_Redesign_Plan.md` | 미착수 제안서(단, "차기 백로그"로 태그 권장) |
| `Docs/UpgradeTree_Redesign_Investigation.md` | 결론이 FinalDecision에 흡수, 구현 완료 |
| `Docs/UpgradeTree_MVP_ChangeProposal.md` | 결론이 FinalDecision에 흡수, 구현 완료 |
| `Docs/UpgradeTree_MVP_BalanceReview.md` | 결론이 FinalDecision에 흡수, 구현 완료 |
| `Docs/UpgradeTree_ResearchDatabase_Investigation.md` | 결론이 후속 설계 문서 전체에 흡수, 구현 완료 |
| `Docs/UpgradeTree_ResearchDatabase_ScreenDesign.md` | V2 문서로 완전히 superseded |
| `Docs/ResearchDatabase_MVP_ImplementationPlan.md` | V2_ImplementationPlan이 명시적으로 대체 선언 |
| `Docs/ResearchDatabase_MobileNav_Investigation.md` | 결론이 UI_FinalReview에 흡수 |
| `Docs/ResearchDatabase_UI_FinalReview.md` | 결론이 V2_UI_StructureDesign에 흡수 |
| `Docs/CountryStatus_Dashboard_Investigation.md` | 제안 100% 구현 완료(Step 78-79) |
| `Docs/PlagueIncReference.md` | 문서 스스로 "백로그 종료" 선언 |

### [D] 삭제 후보 (1개, 실제 삭제는 하지 않음 — 제안만)

| 문서 | 사유 |
|---|---|
| `Docs/DevelopmentWorkflow.md` | 내용이 `CLAUDE.md` "프로젝트 규칙" 섹션과 100% 중복. 신규 정보 없음. 두 곳에 같은 규칙을 두면 향후 규칙 변경 시 불일치 위험 |

---

## 4. 추천 문서 구조

```
Contagion_Project/
├─ CLAUDE.md
└─ Docs/
   ├─ DESIGN.md                                   [A]
   ├─ UI_Design.md                                 [A]
   ├─ GameDesignDocument.md                        [A]
   ├─ Architecture.md                              [A]
   ├─ DevLog.md                                     [A] (UpgradeTree_FinalDecision 내용 흡수 후 갱신)
   ├─ QA_Checklist.md                              [A]
   ├─ Release_Checklist.md                         [A]
   ├─ unity-editor-task.md                         [A]
   ├─ ResearchDatabase_Design.md                    [B, 장기 통합 후보]
   ├─ ResearchDatabase_RuntimeSystems.md            [A]
   ├─ ResearchDatabase_V2_ImplementationPlan.md     [A]
   ├─ ResearchDatabase_V2_UI_StructureDesign.md     [A]
   ├─ UpgradeTree_ResearchDatabase_NodeMapping.md   [A]
   │
   └─ Archive/
      ├─ UI_UX_Review.md
      ├─ UI_Redesign_Plan.md              ← "차기 백로그" 표시 권장
      ├─ UpgradeTree_Redesign_Investigation.md
      ├─ UpgradeTree_MVP_ChangeProposal.md
      ├─ UpgradeTree_MVP_BalanceReview.md
      ├─ UpgradeTree_FinalDecision.md      ← DevLog 이관 후 이동
      ├─ UpgradeTree_ResearchDatabase_Investigation.md
      ├─ UpgradeTree_ResearchDatabase_ScreenDesign.md
      ├─ ResearchDatabase_MVP_ImplementationPlan.md
      ├─ ResearchDatabase_MobileNav_Investigation.md
      ├─ ResearchDatabase_UI_FinalReview.md
      ├─ CountryStatus_Dashboard_Investigation.md
      └─ PlagueIncReference.md

(DevelopmentWorkflow.md — 삭제 후보, 최종 확인 후 처리. 구조도에서 제외)
```

`CLAUDE.md`의 "세션 시작 시 읽는 순서" 목록(1~10번)은 이 재배치로 인해 **전혀 바뀌지 않는다** — 원래도
Archive 대상 12개 문서는 그 목록에 없었다(전부 필요할 때만 찾아보는 개별 조사 문서였음). 목록에 있는
`Docs/ResearchDatabase_RuntimeSystems.md`, `NodeMapping.md` 등은 A군 그대로 유지되므로 이 순서
목록의 참조 경로도 무변경이다.

---

## 5. 문서 정리 실행 계획

**주의**: 아래는 실행 순서 제안이며, 이번 세션에서는 실행하지 않는다. 다음에 "정리를 실제로 진행해줘"
라는 지시가 있을 때 이 순서를 따르면 된다.

1. **정리 전 커밋**: 이동/삭제 전 현재 상태를 git commit(또는 스냅샷)으로 고정 — 실수 시 되돌릴 지점 확보.
2. **DevLog 보강**: `UpgradeTree_FinalDecision.md`의 내용(최종 수치, 변경 근거)을 `DevLog.md`에
   정식 Step으로 옮겨 적는다. 현재 이 반영 내역이 DevLog 어디에도 Step으로 기록되지 않은 문서화
   공백을 먼저 메운다 — 이 작업이 끝나야 원본을 Archive로 보내도 이력이 끊기지 않는다.
3. **참조 경로 사전 점검**: 이동 대상 12개 문서를 다른 문서(`QA_Checklist.md`가
   `CountryStatus_Dashboard_Investigation.md` 2.6절을 인용하는 것처럼)가 상대 경로로 참조하고
   있는지 전수 검색(grep)한다. 발견되면 이동 후 참조 문구에 `Archive/` 경로를 반영할지, 아니면
   "이미 종료된 조사이므로 참조 자체를 삭제할지"를 판단한다.
4. **`Docs/Archive/` 폴더 생성 후 C군 12개 이동**: §4 구조도 그대로 이동. `UI_Redesign_Plan.md`는
   파일 상단에 "미착수 — 차기 작업 후보"라는 한 줄을 추가해 Archive 안에서도 구분되게 한다(선택
   사항이지만 권장).
5. **`DevelopmentWorkflow.md` 처리**: `CLAUDE.md`와의 중복을 사용자에게 최종 확인 후 삭제(또는
   원치 않으면 Archive로 이동). 이 문서만 유일하게 "삭제"가 걸려있는 항목이라 별도 확인 절차를
   권장한다.
6. **사후 검증**: `CLAUDE.md` "세션 시작 시 읽는 순서" 및 TODO 섹션이 가리키는 모든 파일 경로가
   실제로 존재하는지(이동으로 깨진 링크가 없는지) 재확인.
7. **최종 커밋**: 정리 결과를 커밋하고, `CLAUDE.md`에 "문서 정리 완료(Archive 12개 이동, 1개 삭제)"
   한 줄을 남긴다(단, 이 자체가 "구현 과정" 서술이 되지 않도록 결과만 한 줄로 — 상세 사유는 이
   보고서 또는 DevLog에 남기는 것이 프로젝트 규칙에 맞다).

---

## 요약

- 총 28개 문서 중 **13개는 지금도 활성 상태로 반드시 유지**, **2개는 유지하되 통합이 필요**,
  **12개는 결론이 이미 다른 문서로 흡수되었거나(구현 완료) 자체적으로 종료를 선언한 문서**라
  Archive로 옮겨도 안전, **1개는 상위 문서와 완전히 중복**되어 삭제를 검토할 만하다.
- UI_Design/UI_UX_Review/UI_Redesign_Plan, UpgradeTree 5문서, ResearchDatabase 9문서 모두
  "조사 → 제안 → 검증 → 확정" 파이프라인 구조를 따르고 있어, **최종 확정 문서만 남기고 중간
  산출물을 Archive로 보내는 것이 안전하다** — 단, `UpgradeTree_FinalDecision.md`처럼 확정 문서가
  DevLog의 역할(구현 이력)을 대신하고 있는 경우는 이관 작업이 선행되어야 한다.
- 실제 파일 이동/삭제는 이번 세션에서 수행하지 않았다.
