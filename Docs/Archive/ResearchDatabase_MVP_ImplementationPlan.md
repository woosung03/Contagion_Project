# Research Database MVP 구현 계획서

**순수 구현 계획 문서. 코드/UXML/USS 작성 없음.** 아래 5개 문서가 이미 확정한 설계를 실제
작업 단위로 쪼갠다 — 이 문서 자체는 설계를 재검토하지 않는다.

- `Docs/ResearchDatabase_Design.md` — 철학·효과 설계 원칙(§16 마이그레이션 전략, §19 최종 권장안)
- `Docs/UpgradeTree_ResearchDatabase_Investigation.md` — 트리→리스트 전환 결정, 재사용 범위(§9)
- `Docs/UpgradeTree_ResearchDatabase_ScreenDesign.md` — 화면 레이아웃 확정안(탭/섹션/행/상세패널)
- `Docs/ResearchDatabase_RuntimeSystems.md` — 효과 4분류·MVP 우선순위(§9)·최종 아키텍처(§10)
- `Docs/UpgradeTree_ResearchDatabase_NodeMapping.md` — 45개 노드 전수 매핑(이름/효과/타입/분류)

코드 근거는 실제 파일을 직접 확인했다: `Assets/Scripts/Data/UpgradeNode.cs`,
`Assets/Scripts/Data/DefaultUpgradeTreeFactory.cs`, `Assets/Scripts/Managers/UpgradeManager.cs`,
`Assets/Scripts/UI/UpgradeTreeView.cs`(551줄), `Assets/UI/UpgradeTree.uxml`/`.uss`.

---

## 0. MVP 범위 확정 (이 계획의 전제)

사용자 목표 "최소 위험으로 현재 UpgradeTree를 Research Database로 전환"은 다섯 문서가 이미
합의한 **`ResearchDatabase_Design.md` §16 1단계**와 정확히 일치한다. 이 계획서는 그 1단계만
실행 범위로 잡는다.

**포함 (이 MVP의 실제 작업 범위)**

1. 화면 레이어 전환 — 절대좌표 캔버스(`RebuildTree()`/`DrawConnections()`) → 세로 스크롤
   리스트(`ScrollView` + 갈래 섹션 헤더 + 행). `ScreenDesign.md` 전체 반영.
2. 카테고리 좌우 화살표 → 상단 고정 탭 3개.
3. 카테고리 표시명 "능력" → "적응" (enum `Ability`는 그대로, 표시 문자열만 교체).
4. 45개 노드의 표시명(`NodeDisplayNames`) 교체 — `NodeMapping.md`의 "신규 이름" 열 반영.
5. 잠금 사유 인라인 표시("잠김 · 선행: OOO"), DNA 부족과 선행 미충족 구분 표시.
6. CTA 라벨 교체("구매" → "연구 시작 (N DNA)"), 완료 라벨("이미 해금됨" → "연구 완료").
7. 상세 패널에 노드별 한 줄 설명("설명" 행) 추가 — **단, §0.1 원칙에 따라 텍스트 내용을 제한**.

**제외 (후속 Phase로 명시 이연, 이번 커밋 범위 아님)**

- `environmentResistance` 소비 (Runtime Systems §9 2단계) — 내한성/내건성/전천후 적응 등.
- `Pathogen.medicalBurdenModifier` 신규 필드 (3단계) — 수혈 전파 등 6개 연구가 의존.
- `unlockedFlags`/강도 사전/Event Modifier (4단계) — 조류 매개 전파, 국경 무력화류 전부.
- `minCureProgress`/`minVisibility` 시기 게이팅, 연구 소요시간 모델, 선행조건 OR 표현.
- `Docs/GameDesignDocument.md` §7.2 와이어프레임 갱신(트리 전제 문구 정정) — 별도 문서 커밋.
- `NodeMapping.md`의 "신규 효과" 열에 적힌 Mechanic/Hybrid 수치 변경 자체 — **DNA 비용·`effects`
  수치는 1개도 바꾸지 않는다.** (`NodeMapping.md` §7이 이미 이 원칙을 못박아 뒀다.)

### 0.1 "서술-효과 불일치" 방지 원칙 (이 계획서가 추가하는 유일한 판단)

`NodeMapping.md`의 "신규 이름"과 "신규 효과" 열은 함께 묶여 있지만, "신규 효과" 중 실제로
지금 코드가 낼 수 있는 것은 기존 4개 스탯(`infectivity`/`severity`/`lethality`/
`drugResistance`) 가감뿐이다. 예를 들어 `trans_contact1`(신규 이름 "조류 매개 전파")의
"신규 효과"에 적힌 "고립국 자연 감염 발생"은 4단계(`unlockedFlags`) 없이는 실제로 작동하지
않는다. 화면에 "이 연구를 하면 고립국도 감염된다"고 써놓고 실제로는 아무 일도 안 일어나면
플레이어 신뢰가 깨진다.

**따라서 이번 MVP의 노드 설명 문구는 두 갈래로 나눠 작성한다.**

- **효과가 이미 존재하는 노드** (`NodeMapping.md`의 "이미 로직 존재, 서술만 변경" 사례 —
  예: `abl_hardening1`/약물 내성, `drugResistanceReduction` 공식으로 이미 치료제 진행을
  저해하는 로직이 있음) → 설명 문구에 그 규칙을 그대로 서술해도 된다.
- **효과가 아직 없는 노드**(4단계 미착수 상태의 Mechanic/Hybrid 대부분) → 설명 문구는
  "정체성 서사"까지만 쓰고 "지금 이 연구가 실제로 무엇을 바꾸는지"에 대한 구체적 규칙
  문장은 넣지 않는다. 대신 스탯 변화(`infectivity +0.05` 등)는 기존처럼 `data-row`로
  그대로 노출한다. (예: "조류 매개 전파 — 철새의 계절 이동 경로를 따라 병원체가 퍼진다"까지만
  쓰고, "고립국에도 감염이 발생한다"는 문장은 4단계 구현 시점에 추가한다.)
- 이 구분 작업물(45개 노드 × "지금 쓸 수 있는 문구" 확정 리스트)은 §1 Step 1의 산출물이다.

---

## 1. 구현 순서

1. **데이터 준비 (코드 변경 없음)** — 45개 노드의 신규 표시명·설명 문구·갈래(브랜치) 소속을
   확정한 표를 만든다. `NodeMapping.md`를 원본으로 하되 §0.1 원칙에 따라 설명 문구를
   필터링한다. 이 표가 이후 모든 Step의 유일한 입력.
2. **UXML 구조 변경** — `Assets/UI/UpgradeTree.uxml`에서 카테고리 좌우 화살표 마크업을 탭 바
   3개로 교체하고, 캔버스용 루트를 `ScrollView` 콘텐츠 컨테이너로 정리한다(상세 패널
   `detail-panel` 구조는 그대로 둔다).
3. **USS 추가/정리** — `Assets/UI/UpgradeTree.uss`에 리스트 행(`research-row`)·섹션 헤더
   (`section-caption`) 스타일 추가, 기존 `tree-node`/절대좌표 관련 셀렉터 제거 대상 표시만
   해둔다(실제 삭제는 Step 5 코드 정리와 함께).
4. **`UpgradeTreeView.cs` — 표시 데이터 계층 확장** — `NodeDisplayNames` 옆에 `NodeDescriptions`
   (id → 설명 문자열), `NodeBranch`(id → 갈래 라벨) 두 개의 신규 `static readonly Dictionary`를
   Step 1 산출물로 채운다. 이 시점까지는 기존 렌더링 로직을 안 건드리므로 빌드는 여전히
   기존 트리 화면으로 동작해야 한다(중간 검증 지점).
5. **`UpgradeTreeView.cs` — 렌더링 로직 교체** — `RebuildTree()`의 절대좌표 계산과
   `DrawConnections()`를 제거하고, `nodes`를 `NodeBranch` 기준으로 그룹핑해 섹션 헤더 +
   리스트 행을 순서대로 `Add()`하는 방식으로 교체한다. `CreateNodeElement()`는
   `CreateResearchRow()`로 개명하되 내부의 `DetermineState()`/`GetEffectiveCost()`/
   `DisplayName()`/`_codeByNodeId` 조회는 그대로 재사용한다.
6. **잠금 사유 인라인 표시 로직 추가** — `LockReason(UpgradeNode node)` 헬퍼 신규 추가(선행
   미충족 시 "선행: OOO, OOO" 텍스트 합성, 3개 이상이면 "선행: N개 항목"으로 축약). DNA
   부족 케이스는 우측 비용 텍스트에 경고 톤 클래스만 추가.
7. **탭 전환 로직 정리** — 기존 `OnPrevRequested`/`OnNextRequested` 이벤트 배선을 탭 3개
   클릭 이벤트로 교체(3-GameObject 구조 유지 여부는 §6 리스크에서 판단, 유지 시 이 Step은
   이벤트명만 그대로 두고 호출부만 탭 클릭으로 바꾸는 최소 변경).
8. **상세 패널 문구 정리** — `SelectNode()`에 `NodeDescriptions` 조회로 "설명" `data-row`를
   효과 목록보다 먼저 추가. CTA 라벨 텍스트만 교체(`AddDetailRow`/버튼 텍스트 대입부).
9. **QA 체크리스트 반영** — `Docs/QA_Checklist.md`에 리스트 전환 검증 항목 추가(§8에서 상세).
10. **실기기/에디터 검증** — Unity 에디터 접속 후 렌더링·스크롤·탭 전환·잠금 사유 표시
    확인(에디터 미접속 상태로 작성된 계획이므로 최종 검증은 별도 세션).

---

## 2. 파일별 수정 목록

| 파일 | 수정 내용 | 비고 |
|---|---|---|
| `Assets/Scripts/UI/UpgradeTreeView.cs` | `RebuildTree()`/`DrawConnections()` 제거 후 리스트 렌더링으로 교체, `CreateNodeElement()`→`CreateResearchRow()` 개명, `NodeDescriptions`/`NodeBranch` 딕셔너리 신규, `LockReason()` 신규, 탭 클릭 배선, `SelectNode()`에 설명 행 추가, CTA 라벨 텍스트 교체 | 데이터 모델 미참조, View 레이어 내부 변경만 |
| `Assets/UI/UpgradeTree.uxml` | 좌우 화살표 마크업 → 탭 바 3개, 캔버스 루트 → `ScrollView` 콘텐츠 컨테이너 정리 | `detail-panel` 구조 무변경 |
| `Assets/UI/UpgradeTree.uss` | `research-row`/`research-row--locked`/`section-caption` 등 신규 클래스 추가, `tree-node`의 절대좌표(`position: absolute` 계열)·connection 관련 셀렉터 제거 | `DESIGN.md` 기존 토큰(`data-row`, `stat-chip`, accent-bar) 재사용, 신규 색상 축 추가 없음 |
| `Docs/QA_Checklist.md` | "리스트 전환 검증" 섹션 신규 추가(§8) | 검증 전용 문서, 코드 아님 |
| `Docs/UI_Design.md` | §7(트리 화면 설계)에 "Step N부로 리스트 화면으로 대체됨, 아래는 이전 설계로 보존" 각주만 추가 | 삭제하지 않고 이력 보존(CLAUDE.md Git 우선 원칙) |
| `Docs/GameDesignDocument.md` | §7.2 와이어프레임 "노드 트리 스크롤 뷰" 문구 정정(트리 전제 → 리스트 전제) | 이 MVP와 별도 커밋(§7 커밋 분해 참고) |
| `CLAUDE.md` | "현재 구현된 시스템" 목록에 한 줄 추가, "현재 TODO"의 UI 디자인 시스템 적용 항목 갱신 | 프로젝트 규칙상 완료 즉시 갱신(작업 완료 후, 이 계획서 실행과 별개 시점) |

**변경하지 않는 파일** (명시적으로 손대지 않음): `Assets/Scripts/Data/UpgradeNode.cs`,
`Assets/Scripts/Data/UpgradeManager.cs`(→ 경로 오타 정정: `Assets/Scripts/Managers/UpgradeManager.cs`),
`Assets/Scripts/Data/DefaultUpgradeTreeFactory.cs`, `Assets/Scripts/Data/UpgradeTreeDatabase.cs`,
`Assets/Scripts/Data/Pathogen.cs`, `Assets/Scripts/Managers/SimulationManager.cs`,
`Assets/Scripts/Managers/HumanResistanceManager.cs`, `Assets/Scripts/Managers/TransportManager.cs`.

---

## 3. 신규 클래스 목록

**신규 "클래스"는 없다.** MVP 원칙(§0)상 데이터 모델에 신규 타입을 추가하지 않으므로, 전부
`UpgradeTreeView.cs` 내부의 신규 static 필드/메서드다.

| 이름 | 형태 | 위치 | 역할 |
|---|---|---|---|
| `NodeDescriptions` | `static readonly Dictionary<string, string>` | `UpgradeTreeView.cs`, `NodeDisplayNames` 옆 | id → 설명 한 줄(§0.1 필터링된 문구) |
| `NodeBranch` | `static readonly Dictionary<string, string>` | `UpgradeTreeView.cs` | id → 갈래 라벨("공기 계열" 등) — `DefaultUpgradeTreeFactory.cs`의 기존 주석(`// --- 표준형 (기침 계열) ---` 등)을 근거로 45개 전부 명시적으로 하드코딩. x좌표 기반 추정은 하지 않는다(합류 노드가 tier에 따라 같은 x열을 공유해 오판 위험) |
| `CreateResearchRow(UpgradeNode)` | `private VisualElement` 메서드 | `UpgradeTreeView.cs` | 기존 `CreateNodeElement()` 대체 — 내부적으로 `DetermineState()`/`GetEffectiveCost()`/`DisplayName()` 그대로 호출 |
| `CreateBranchSectionHeader(string branchLabel, UpgradeCategory)` | `private VisualElement` 메서드 | `UpgradeTreeView.cs` | 갈래 섹션 헤더 생성, 카테고리색 accent-bar 포함 |
| `LockReason(UpgradeNode)` | `private string` 메서드 | `UpgradeTreeView.cs` | "선행: OOO" 또는 "선행: N개 항목" 텍스트 합성 |

---

## 4. 삭제 가능한 코드

| 대상 | 위치 | 비고 |
|---|---|---|
| `RebuildTree()`의 절대좌표 계산 블록 전체 | `UpgradeTreeView.cs` 224~278행(캔버스 크기 계산, `_xOffset`, `connectionsLayer` 생성·`generateVisualContent` 구독) | `ScrollView.contentContainer`에 순서대로 `Add()`만 하면 대체됨(`ScreenDesign.md` §10) |
| `DrawConnections()` 전체 | `UpgradeTreeView.cs` 360행~ (Painter2D 꺾은선 벡터 계산, 포트 마커) | 연결선 자체가 사라지므로 통째로 제거 |
| `CreateNodeElement()`의 절대좌표 스타일 대입부 | `UpgradeTreeView.cs` 317~321행(`box.style.position = Position.Absolute`, `left`/`top`/`width`/`height` 고정값 대입) | 리스트 행은 Flex 기본 흐름 사용, 이 대입문들만 제거하고 나머지 로직(상태 클래스, 코드/이름 텍스트 조립)은 유지 |
| `Assets/UI/UpgradeTree.uss`의 `tree-node` 절대좌표/connection 관련 셀렉터 | USS 파일 내 해당 규칙 | 정확한 셀렉터명은 파일 직접 확인 후 정리(이번 조사에서 USS 본문은 미열람 — Step 3에서 실제 셀렉터 목록 확정) |
| `NodeWidth`/`NodeHeight`(140/60 또는 110/50) 상수 | `UpgradeTreeView.cs` 상단 상수 선언부 | 리스트 행은 고정 폭/높이 대신 `min-height: 48px`(터치 타겟) + 가변 폭으로 대체 |

**삭제하지 않는 것**: `_codeByNodeId`, `NodeDisplayNames`, `DetermineState()`, `StateCaption()`,
`CategoryLabel()`/`CategoryPrefix()`/`CategoryCaption()`, `SelectNode()`, `AddDetailRow()`,
`EffectStatLabel()` — 전부 §5에서 재사용.

---

## 5. 기존 코드 재사용 범위

| 구성요소 | 재사용률 | 근거 |
|---|---|---|
| `UpgradeNode`/`UpgradeManager`/`DefaultUpgradeTreeFactory`(데이터 모델·해금 로직 전체) | 100% | §0 MVP 범위 원칙 — 데이터 계층 완전 무변경 |
| `UpgradeManager.GetEffectiveCost()`/`CanUnlock()`/`TryUnlock()`/`ApplyEffectsToPathogen()` | 100% | 코드 확인 완료(`Managers/UpgradeManager.cs` 72~150행) — 4개 스탯 가감 로직 자체는 손대지 않음 |
| `DetermineState()`/`StateCaption()` | 100% | 리스트 행 상태 칩에 그대로 씀(`UpgradeTreeView.cs` 283~305행) |
| `_codeByNodeId`/`CategoryPrefix()` | 100% | 리스트 행 좌측 코드(`TRANS-004` 등) 표시에 그대로 씀 |
| `NodeDisplayNames` | 값만 교체, 구조 재사용 | 딕셔너리 자체(56행~)는 그대로, 45개 값만 `NodeMapping.md` "신규 이름" 열로 교체 |
| `SelectNode()`/`AddDetailRow()`/`EffectStatLabel()` | 100% | 이미 리스트형 상세 패널이라 트리든 리스트든 무관하게 동작(414~478행) |
| `OnPrevRequested`/`OnNextRequested` + 3-GameObject 구조 | 조건부 재사용 | §6 리스크에서 결정 — 유지 시 이벤트명 그대로, 호출부만 탭 클릭으로 교체 |
| `DESIGN.md` 공용 컴포넌트(`data-row`, `stat-chip`, accent-bar) | 100% | 다른 5개 화면에서 이미 검증된 패턴, 신규 컴포넌트 도입 안 함(`ScreenDesign.md` §10이 `ListView` 도입도 이미 기각) |

**결론**: 데이터 계층 100%, UI 로직(상태 판정·비용·상세 패널) 100%, 표시 텍스트(딕셔너리 값)만
교체, 렌더링 방식(절대좌표→Flex 리스트)만 교체. 이는 `Investigation.md` §9와 `ScreenDesign.md`
§10이 이미 평가한 재사용률과 정확히 일치한다.

---

## 6. 예상 리스크

1. **서술-효과 불일치 (신규 리스크, §0.1에서 원칙으로 완화)** — Step 1 산출물(설명 문구 표)
   검수 없이 `NodeMapping.md`의 "신규 효과" 문구를 그대로 옮기면 미구현 메커닉을 약속하는
   꼴이 된다. 완화책: Step 1 산출물을 별도로 리뷰(이 문서 §0.1 기준 재확인)한 뒤에만
   Step 4로 진행.
2. **갈래(브랜치) 매핑 이중 유지보수** — `NodeBranch` 딕셔너리가 `NodeDisplayNames`와 별개로
   45개 항목을 또 유지해야 한다. 노드 추가/삭제 시 두 딕셔너리를 동시에 갱신하지 않으면
   불일치(표시명은 있는데 소속 갈래가 없어 어느 섹션에도 안 뜨는 노드) 발생 가능. 완화책:
   두 딕셔너리 키 집합이 동일한지 검증하는 에디터 전용 자가진단(Step 10 검증 항목에 추가,
   `Docs/DevLog.md` Step 68~69의 "자가진단 경고 로그" 관행과 동일 패턴).
3. **3-GameObject 구조 처리 방식 미결정** — `TransmissionTreeUI`/`SymptomTreeUI`/`AbilityTreeUI`
   3개 독립 GameObject/UIDocument 구조를 유지한 채 탭으로 전환할지, 하나의 화면에서 리스트
   콘텐츠만 갈아끼우는 구조로 합칠지 결정되지 않았다(`ScreenDesign.md` §2가 이미 "구현
   관점에서 별도 판단"으로 남겨둠). **이 계획은 유지를 권장**한다 — 기존 씬 배선을 안
   건드리고(`Docs/unity-editor-task.md` 영향 없음) 탭 클릭 시 세 UIDocument의 표시 여부만
   토글하면 되므로 구조 변경보다 리스크가 낮다. 다만 최종 결정은 Unity 에디터 씬 확인 후.
4. **OR 선행조건 표시 이슈 — 이번 조사로 해소됨** — `ScreenDesign.md` §7이 "현재 45개 노드가
   실제로 OR 조건을 쓰는지 확인 필요"로 남겨뒀던 항목을 `DefaultUpgradeTreeFactory.cs` 원본
   확인으로 해소했다: `prerequisites`는 전부 다중 항목이 있어도 AND로만 쓰인다(예:
   `trans_global`은 `trans_advanced1`과 `trans_advanced2` 둘 다 요구). OR 조건 표시 로직은
   설계할 필요가 없다 — 리스크 낮음, 별도 확인 불필요.
5. **`ScrollView` sticky header 미지원 가능성** — `ScreenDesign.md` §9가 이미 "1단계에서는
   생략, 실기기 테스트 후 재판단"으로 결론 내렸다. 이 계획도 동일하게 sticky 헤더는
   구현하지 않는다(Step 3/5 범위 밖) — 리스크 아님, 범위 확정 사항.
6. **USS 셀렉터 제거 범위 미확정** — 이번 조사에서 `UpgradeTree.uss` 파일 본문을 직접 열람하지
   않았다. Step 3 착수 시 실제 셀렉터 목록을 먼저 확인해야 §4의 "삭제 가능한 코드" 표가
   정확해진다 — 코드 착수 전 첫 번째 확인 작업으로 명시.
7. **실기기 미검증** — Unity 에디터 미접속 상태로 작성된 계획이라 리스트 스크롤/탭 전환/터치
   영역이 실제로 480px~1440px 폭에서 의도대로 나오는지 검증 안 됨 — Step 10, §8 완료조건에
   명시적으로 게이트.
8. **"능력→적응" 명칭 전수 교체 누락** — `CategoryLabel()`/`CategoryCaption()`뿐 아니라
   UXML/USS에 하드코딩된 "능력"/"ABILITY LAB" 문자열이 있다면(현재 조사에서 미확인) 일부만
   바뀌고 일부는 그대로 남는 불일치가 생길 수 있다 — Step 3/4 착수 시 프로젝트 전체
   문자열 검색(`grep "능력\|ABILITY LAB"`)으로 누락 여부 확인 필요.

---

## 7. 커밋 단위 작업 분해

각 커밋은 독립적으로 되돌릴 수 있도록 레이어별로 분리한다(§9 롤백 전략과 직결).

1. **커밋 1 — 데이터 준비 (문서만)**: 45개 노드 신규 이름·설명·갈래 매핑 확정 표를 이
   문서 부록 또는 별도 워크시트로 산출. 코드 변경 없음.
2. **커밋 2 — USS 셀렉터 확인 + 신규 스타일 추가**: `UpgradeTree.uss`에 `research-row`류
   신규 클래스 추가(기존 `tree-node` 셀렉터는 아직 유지, 병행 기간).
3. **커밋 3 — UXML 구조 변경**: 탭 바 마크업 교체, `ScrollView` 콘텐츠 컨테이너 정리.
   이 시점에서는 아직 C# 쪽이 옛 구조를 채우므로 화면이 깨질 수 있음(같은 PR 내에서
   커밋 4와 함께 머지, 개별 커밋은 리뷰 단위로만 분리).
4. **커밋 4 — `UpgradeTreeView.cs` 표시 데이터 계층 확장**: `NodeDescriptions`/`NodeBranch`
   딕셔너리 추가만(아직 미사용 상태라 빌드/동작에 영향 없음 — 안전한 중간 지점).
5. **커밋 5 — 렌더링 로직 교체**: `RebuildTree()`/`DrawConnections()` 제거, 리스트 렌더링
   전환. 이 커밋부터 화면이 실제로 리스트로 보임 — 가장 리스크가 큰 단일 커밋.
6. **커밋 6 — 잠금 사유·CTA 라벨·상세 패널 설명 행**: §1 Step 6/8 반영.
7. **커밋 7 — 탭 전환 배선 정리**: 좌우 화살표 완전 제거, 탭 클릭 이벤트로 대체.
8. **커밋 8 — 구 코드/구 USS 셀렉터 정리**: 커밋 2~7이 안정화된 뒤 병행 유지하던 `tree-node`
   절대좌표 셀렉터·`NodeWidth`/`NodeHeight` 상수 등 최종 제거.
9. **커밋 9 — QA_Checklist 갱신**: §8 검증 항목 추가(코드 아님).
10. **커밋 10 (별도) — GameDesignDocument.md §7.2 정정**: 이 MVP와 별도 시점에 처리 가능
    (문서 정합성 작업, 의존성 없음).

---

## 8. Phase별 완료 조건

| Phase | 범위(커밋) | 완료 조건 |
|---|---|---|
| **Phase A — 데이터 준비** | 커밋 1 | 45개 노드 전부에 대해 신규 이름·설명(§0.1 필터링 완료)·갈래 라벨이 표로 확정됨. `NodeMapping.md`와 대조해 빠진 노드 0개 |
| **Phase B — 화면 골격 전환** | 커밋 2~5 | 3개 카테고리 전부 리스트로 렌더링됨. 카테고리당 15개 노드 전부 스크롤로 도달 가능. 절대좌표 관련 콘솔 경고/에러 0건. 기존 잠김/해금가능/진행중/완료 4상태 색상이 리스트 행에서도 동일하게 구분됨 |
| **Phase C — 상호작용 폴리싱** | 커밋 6~7 | 잠긴 행 전부 선행 사유 텍스트 노출(3개 이상은 축약). DNA 부족과 선행 미충족이 시각적으로 구분됨. CTA 라벨 전부 "연구 시작"류로 통일. 탭 3개로 카테고리 전환 가능, 화살표 UI 잔존 0건 |
| **Phase D — 정리·검증** | 커밋 8~9 | 구 코드(`DrawConnections`, 절대좌표 스타일 대입) 0줄 잔존. `dotnet build`/Unity 콘솔 경고 0건. `QA_Checklist.md` 신규 항목 전부 실기기 통과. §6 리스크 항목(2·3·6·8번) 전부 확인 완료 |

---

## 9. 롤백 전략

- **데이터 모델 무변경이 곧 최대 안전망**이다 — `UpgradeNode`/`UpgradeManager`/
  `DefaultUpgradeTreeFactory`를 전혀 안 건드리므로, 최악의 경우(리스트 화면이 실기기에서
  심각한 문제를 일으킴)에도 `UpgradeTreeView.cs`/`UpgradeTree.uxml`/`UpgradeTree.uss` 3개
  파일만 이전 커밋으로 `git revert`하면 게임 저장 데이터·해금 상태·DNA 로직에는 어떤
  영향도 없다.
- **커밋을 레이어별로 분리한 이유(§7)가 곧 롤백 단위다** — 예를 들어 Phase C(잠금 사유
  표시)에서 문제가 생겨도 Phase B(리스트 골격)까지는 되돌리지 않고 커밋 6~7만 revert하면
  된다.
- **병행 유지 기간을 의도적으로 둔다** — 커밋 2~7 동안 구 USS 셀렉터(`tree-node` 절대좌표
  계열)를 즉시 삭제하지 않고 커밋 8에서야 정리하는 이유가 이것이다 — 중간 커밋 어느
  시점에서 롤백이 필요해도 구 스타일이 아직 남아있어 화면이 완전히 깨지지 않는다.
  (다만 커밋 5 자체는 되돌리기 전용 단일 커밋이라 그 커밋만 revert하면 트리 화면으로
  바로 복귀 가능 — 구 렌더링 코드가 커밋 8 전까지는 git 히스토리에만 있고 워킹 트리에는
  없으므로, 완전 복귀가 필요하면 커밋 5~7을 한 번에 revert)
- **별도 feature flag/토글은 두지 않는다** — 화면 하나(UpgradeTree)만 교체되고 되돌리기
  쉬운 작업 규모라 플래그 도입 자체가 과설계(Runtime Systems §8.3의 "과설계 회피" 원칙과
  같은 판단).
- **병합 게이트**: Phase D(§8) 완료 조건 — 특히 `QA_Checklist.md` 실기기 검증 통과 — 를
  만족하기 전에는 `main`/배포 브랜치에 병합하지 않는다.

---

## 10. 최종 구현 로드맵

```
Phase 1 (이 문서 범위, MVP)
  A. 데이터 준비 → B. 화면 골격 전환 → C. 상호작용 폴리싱 → D. 정리·검증
  = "트리 캔버스 → 리스트" 완료, 데이터 모델 0변경, DNA/해금 로직 0변경
       │
       ▼  (Runtime Systems.md §9 순서를 그대로 계승, 이 문서와 독립적으로 착수 가능)
Phase 2 — environmentResistance 소비
  `UpgradeNode`에 (기후, 보정치) 병렬 리스트 추가 + `ApplyEffectsToPathogen` case 1개
  실현 연구: 인수공통 전파(Humid 가중)·내한성·전천후 적응 (3건)
       │
       ▼
Phase 3 — medicalBurdenModifier
  `Pathogen`에 float 필드 1개 + `RunTick()` 계산식에 곱항 1개 + UI 계산식 통합 검토
  실현 연구: 수혈 전파·오염 혈액 유통망·발열·폐렴·호흡 부전·패혈증 (6건, 최다 수요)
       │
       ▼
Phase 4 — unlockedFlags + Event Modifier
  `Pathogen`에 HashSet<string> + Dictionary<string,float>, 3개 매니저에 우회 판정 지점 추가
  실현 연구: 조류 매개 전파·설치류 매개 확산·세포벽 강화·격리 내성·검사 회피 등 (리스크 최고)
       │
       ▼
Phase 5 (장기, 미확정) — 연구 소요시간 모델, 시기 게이팅 전면 도입, DNA 비용 프리미엄 재조정
```

**이 계획서가 실제로 착수하는 것은 Phase 1뿐이다.** Phase 2~4는 `Docs/ResearchDatabase_RuntimeSystems.md`
§9·§10에 이미 설계돼 있으며, Phase 1 완료(§8 Phase D 통과) 후 별도 계획서(각 Phase당 1개
권장 — Phase 2/3/4는 매니저 코드를 직접 건드리므로 이 MVP보다 리스크가 높아 각각 독립적인
구현 계획·롤백 전략이 필요)를 다시 작성하는 것을 권장한다.

**다음 세션 결정 필요 항목 (이 계획서 실행 전 확정할 것)**

1. §1 Step 1의 노드별 설명 문구 확정 표 — 이 문서가 만들지 않고 다음 세션 산출물로 남김.
2. §6-3 3-GameObject 구조 유지 여부 — Unity 에디터 씬 확인 후 최종 결정.
3. §6-6 `UpgradeTree.uss` 실제 셀렉터 목록 확인 — Step 3 착수 전 선행 작업.
4. §2의 `Docs/UI_Design.md`/`Docs/GameDesignDocument.md` 각주 반영 시점 — Phase 1 커밋과
   같은 PR로 갈지, 별도 문서 전용 커밋으로 분리할지(§7 커밋 10 권장안 참고).

이 문서는 구현 계획서이며 위 항목 중 어느 것도 아직 구현되지 않았다.
