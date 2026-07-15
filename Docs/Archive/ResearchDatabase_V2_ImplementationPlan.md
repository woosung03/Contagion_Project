# Research Database v2 구현 계획서 (브랜치 현황판 + 연구 리스트 + Research Popup)

**순수 구현 계획 문서. 코드/UXML/USS 작성 없음.** `ResearchDatabase_V2_UI_StructureDesign.md`(구조
설계 확정안 E+팝업)와 `ResearchDatabase_MVP_ImplementationPlan.md`(구 인라인 리스트 전제 Commit
2 계획, 이번 문서로 대체)를 실제 작업 단위로 쪼갠다. 설계 자체는 재검토하지 않는다.

코드 근거는 이번 세션에 실제 파일을 직접 확인했다: `Assets/Scripts/UI/UpgradeTreeView.cs`(551줄,
Commit 1 상태 — 더미 데이터),`Assets/Scripts/Managers/UpgradeManager.cs`,
`Assets/Scripts/Data/UpgradeNode.cs`, `Assets/Scripts/UI/TacticalModalController.cs`,
`Assets/Scripts/UI/CountryPopupController.cs`, `Assets/UI/UpgradeTree.uxml`/`.uss`,
`Assets/UI/CountryPopup.uxml`, `Assets/Scripts/Managers/UIManager.cs`.

---

## 0. 이번 계획이 채택한 전제 (V2 문서 §7 결정 필요 사항 중 코드 근거로 해소된 것)

`ResearchDatabase_V2_UI_StructureDesign.md` §7이 "다음 세션 결정 필요"로 남긴 5개 항목 중 2개는
이번 코드 조사로 답이 나왔다.

1. **Commit 순서 — A(통합 진행) 채택.** 브랜치 진행률 집계(`n/m`)와 실제 `UpgradeManager.Tree`
   연결이 어차피 같은 "노드→브랜치" 그룹핑 테이블을 필요로 한다. 따로 하면 그룹핑 코드를 두 번
   만지게 된다 — V2 문서 §5.3이 이미 권장한 대로 이번 계획은 구조 변경과 데이터 연결을 하나의
   커밋 시퀀스로 합친다.
2. **팝업 오픈 책임 소재 — `UIManager` 중앙 코디네이터 채택.** `UIManager.cs`를 직접 확인한 결과
   "탭 3개 중 하나가 눌리면 해당 `UpgradeTreeView`를 찾아 보여준다"(`OnCategoryRequested`
   구독, 74~79행/188~197행)가 정확히 이 패턴으로 이미 구현돼 있다. `CountryPopupController`가
   `WorldMap.OnCountryClicked`에 직접 구독하는 방식(CLAUDE.md가 "죽은 코드인 줄 알았는데
   살아있었다"는 혼란의 원인으로 이미 지목한 그 패턴)을 반복하지 않고, 이미 검증된 중앙 구독
   패턴을 그대로 확장한다 — 3개 `UpgradeTreeView`의 `OnResearchItemSelected`를 `UIManager`가
   전부 구독해 공유 `ResearchPopupController` 하나를 연다.

남은 3개 항목(브랜치 선택 기억 정책 / `detail-rows` 문단 텍스트 허용 범위 / 4브랜치 확장
시나리오)은 §7에 기본값과 함께 남겨둔다 — 구현을 막지 않는 선에서 진행하되 확정은 아니다.

---

## 1. 구현 순서

### A. 데이터 준비 (문서만, 코드 없음)

1. `UpgradeTree_ResearchDatabase_NodeMapping.md`의 "신규 이름" 열을 원본으로, 45개 노드 전부에
   대해 (표시명 / 설명 문구 / 브랜치 라벨) 3열 확정표를 만든다. 설명 문구는
   `ResearchDatabase_MVP_ImplementationPlan.md` §0.1 원칙(효과가 이미 있는 노드만 규칙 문장
   포함, 나머지는 정체성 서사까지만)을 그대로 적용 — 이 원칙은 구조가 바뀌어도 유효하다.
   브랜치 라벨은 `DefaultUpgradeTreeFactory.cs`의 기존 주석(`// --- 표준형 (기침 계열) ---` 등,
   실제 파일에서 확인 완료)을 근거로 45개 전부 명시적으로 배정 — x좌표 추정 금지(합류 노드가
   tier에 따라 같은 열을 공유해 오판 위험, MVP 계획 §3과 동일 이유).
2. 4브랜치 진행률 집계 규칙 확정: 브랜치당 `해금 수 / 전체 수`, 통합 브랜치(4번째)는 선행
   3브랜치 전부가 `prerequisites` 조건을 만족하기 전까지 UI상 "잠김"으로 표시(실제 unlock
   가능 여부는 `UpgradeManager.CanUnlock()`이 이미 판정하므로 신규 로직 아님 — 표시 문구만
   결정).

### B. 화면 골격 — UXML/USS

3. `UpgradeTree.uxml`: `node-scroll` 위에 `branch-board`(4행 고정) 컨테이너 1개 추가. 기존
   `detail-panel`/`detail-rows` 구조는 무변경, `buy-button`만 제거(연구 시작 버튼은 팝업으로
   완전히 이관 — V2 문서 §5.2).
4. `UpgradeTree.uss`: `branch-board`/`branch-row`/`branch-row__label`/`branch-row__progress`/
   `branch-row__progress-fill`/`branch-row--selected`/`branch-row--locked` 신규 클래스 추가.
   `Hud.uss`의 `population-bar__segment`(flex-grow 비율 막대) 기법을 2세그먼트로 재사용,
   기존 `research-row`/`section-caption`/`detail-panel`은 무변경.
5. `ResearchPopup.uxml` 신규 — `CountryPopup.uxml` 구조를 그대로 복제(`modal-root`+코너컷
   4개+헤더+`modal-rows`+`modal-footer`)하고 설명 문단 `Label` 1개만 추가.
6. `ResearchPopup.uss` 신규 — 설명 텍스트 스타일 + `modal-footer` 2버튼 가로 배치(균등폭,
   `modal-footer`의 첫 실사용 사례 — 현재 어떤 화면도 이 컨테이너를 안 씀, `CountryPopup`은
   닫기를 헤더 `popup-close`로 처리).

### C. 코드 — 표시 데이터 계층 (안전한 중간 지점)

7. `UpgradeTreeView.cs`: `NodeDisplayNames` 45개 값을 Step 1 산출물로 교체, `NodeDescriptions`/
   `NodeBranch` 딕셔너리 신규 추가. 이 시점까지는 아직 아무 데서도 참조하지 않으므로 빌드/동작에
   영향 없음(옛 MVP 계획 커밋 4와 동일한 안전판 전략).

### D. 코드 — 렌더링 로직 교체 (브랜치 보드 + 리스트)

8. `BuildDummyBranches()`/`DummyResearchItem`/`DummyBranch` 제거, `UpgradeManager.Tree`를
   `NodeBranch` 기준으로 그룹핑하는 `BuildBranches()`로 교체. `_codeByNodeId`/
   `DetermineState(UpgradeNode)`(이미 있는 메서드, 지금은 미사용)를 실제로 연결.
9. `BuildBranchBoard()` 신규 — 4브랜치 `n/m` 집계 + `branch-row` 렌더링.
10. `SelectBranch(branch)` 신규 — 활성 브랜치 상태 저장, 기본값은 "미완료 항목이 있는 첫
    브랜치"(§7 기본값).
11. `BuildResearchList()` 변경 — 전체 브랜치 순회 대신 활성 브랜치 하나만 렌더링.
12. `BuildBranchSummary()` 신규 — 하단 `detail-panel`을 2행(브랜치명+진행률, 다음 추천 연구)으로
    채움. `SelectDummyItem()`의 상세 채움 로직을 대체(그 메서드 자체는 Step 17에서 삭제).

### E. 코드 — 팝업 + 코디네이터

13. `ResearchPopupController.cs` 신규 — `TacticalModalController` 상속(`CountryPopupController`와
    동일 패턴), `Show(UpgradeNode node)` 오버로드로 이름/설명/효과(쉼표 병기)/비용/선행조건
    (`AddSectionCaption("선행 조건")`+반복 `AddRow`)/상태를 채움. `modal-footer`에 상태별로
    "닫기"만 또는 "닫기"+"연구 시작(N DNA)" 버튼 구성(V2 문서 §4.2 상태별 표 그대로).
14. `UpgradeTreeView.cs`: 항목 행 클릭 콜백을 `SelectDummyItem(item)` 대신 신규 이벤트
    `OnResearchItemSelected(UpgradeNode)` 발행으로 교체.
15. `UIManager.cs`: `researchPopupController` 필드 추가, 3개 `UpgradeTreeView`의
    `OnResearchItemSelected`를 전부 구독(§0의 채택 근거)해 `ResearchPopupController.Show()`
    호출. 팝업의 "연구 시작" 성공 콜백(`UpgradeManager.OnNodeUnlocked` 재사용 가능 — 신규
    이벤트 불필요, 이미 존재)을 구독해 현재 활성 `UpgradeTreeView`의 `BuildBranchBoard()`/
    `BuildBranchSummary()`를 다시 호출하도록 배선.

### F. 폴리싱

16. `LockReason(UpgradeNode)` 헬퍼 추가 — 선행 미충족 시 "선행: OOO"(3개 이상은 "N개 항목"으로
    축약) 텍스트 합성. `research-row` 상태줄과 팝업 선행조건 섹션 양쪽에서 재사용.
17. CTA/상태 배지 최종 문구 정리 — "연구 시작 (N DNA)"/"DNA 부족"(비활성)/"선행 조건 필요"
    (비활성)/"연구 완료"(버튼 자체 미노출) 4갈래, `UpgradeManager.CanUnlock()`/DNA 잔량 조회로
    판정(신규 로직 아님).

### G. 정리 · 문서 반영

18. 구 코드 삭제 — `SelectDummyItem`/`DummyResearchItem`/`DummyBranch`/`BuildDummyBranches`
    전체, UXML의 `buy-button` 요소.
19. `DESIGN.md` 반영 — `branch-board`/`branch-row` 컴포넌트 항목, Modal Footer 사용 규칙,
    Do/Don't 2건(V2 문서 §6 문구 그대로).
20. `QA_Checklist.md` — 브랜치 보드 진행률 정확성/팝업 상태별 버튼/선행조건 텍스트/DNA 부족
    표시 검증 항목 추가.
21. `Docs/unity-editor-task.md` — 신규 `ResearchPopupUI` GameObject(UIDocument +
    `ResearchPopupController`, `PanelSettings` 연결) 배선 항목 추가, `UIManager` 인스펙터에
    `researchPopupController` 참조 연결 항목 추가.
22. `CLAUDE.md` — 완료 시스템 목록 1줄 추가, TODO의 Research Database 관련 항목 정리(작업 완료
    후, 프로젝트 규칙에 따름).
23. Unity 에디터 실기기 검증 — 브랜치 보드 렌더링/팝업 오픈-클로즈/선행조건 표시/DNA 부족 상태
    확인(에디터 미접속 상태로 작성된 계획이라 최종 검증은 별도 세션).

---

## 2. 파일별 수정 목록

| 파일 | 수정 내용 | 관련 Step |
|---|---|---|
| `Assets/Scripts/UI/UpgradeTreeView.cs` | `NodeDisplayNames` 값 교체, `NodeDescriptions`/`NodeBranch` 신규, `BuildDummyBranches` 계열 삭제 → `BuildBranches()`/`BuildBranchBoard()`/`SelectBranch()`/`BuildBranchSummary()` 신규, `BuildResearchList()` 활성 브랜치 전용으로 축소, `LockReason()` 신규, `OnResearchItemSelected` 이벤트 신규 | 1, 7~12, 14, 16~18 |
| `Assets/UI/UpgradeTree.uxml` | `branch-board` 컨테이너 추가, `buy-button` 제거 | 3, 18 |
| `Assets/UI/UpgradeTree.uss` | `branch-board`류 신규 클래스 추가(`population-bar__segment` 기법 재사용) | 4 |
| `Assets/Scripts/Managers/UIManager.cs` | `researchPopupController` 필드 추가, 3개 `UpgradeTreeView.OnResearchItemSelected` 구독 → 팝업 오픈, `UpgradeManager.OnNodeUnlocked` 구독 → 활성 뷰 갱신 | 15 |
| `Docs/DESIGN.md` | `branch-board` 컴포넌트, Modal Footer 규칙, Do/Don't 2건 | 19 |
| `Docs/QA_Checklist.md` | 브랜치 보드·팝업 검증 항목 신규 섹션 | 20 |
| `Docs/unity-editor-task.md` | `ResearchPopupUI` 씬 배선, `UIManager` 참조 연결 항목 | 21 |
| `CLAUDE.md` | 완료 목록/TODO 갱신(작업 완료 후) | 22 |

**변경하지 않는 파일**: `Assets/Scripts/Data/UpgradeNode.cs`,
`Assets/Scripts/Managers/UpgradeManager.cs`(호출만 늘어남, 내부 로직 무변경),
`Assets/Scripts/Data/DefaultUpgradeTreeFactory.cs`, `Assets/Scripts/UI/TacticalModalController.cs`
(상속만, 베이스 자체는 `AddSectionCaption()`/`AddRow(rowClass)`를 이미 지원해 무변경),
`Assets/Scripts/UI/CountryPopupController.cs`/`CountryPopup.uxml`/`.uss`(별개 화면).

---

## 3. 신규 생성 파일 목록

| 파일 | 역할 | 참조 원본 |
|---|---|---|
| `Assets/UI/ResearchPopup.uxml` | Research Popup 뼈대 — `modal-root`+코너컷 4개+헤더+설명 `Label`+`modal-rows`+`modal-footer` | `CountryPopup.uxml` 복제 |
| `Assets/UI/ResearchPopup.uss` | 설명 텍스트 스타일 + `modal-footer` 2버튼 가로 배치 | `CountryPopup.uss`의 `popup-*` 패턴 |
| `Assets/Scripts/UI/ResearchPopupController.cs` | `TacticalModalController` 상속, `Show(UpgradeNode)`로 4-1 표 항목 채움, "연구 시작" 클릭 시 `UpgradeManager.TryUnlock()` 호출 | `CountryPopupController.cs`의 "베이스 상속 + Show() 오버라이드" 패턴 |

**씬 작업(코드 아님, `unity-editor-task.md`로 이관)**: `CountryPopupUI`와 동일하게 화면 전체에
공유되는 팝업 GameObject 1개(`ResearchPopupUI`) 신규 배치 — 카테고리별 3개 `UIDocument`와 별개로,
어느 카테고리 화면에서 항목을 탭해도 이 팝업 하나가 뜬다.

---

## 4. 커밋 단위 분할 계획

각 커밋은 레이어별로 분리해 독립적으로 되돌릴 수 있게 한다(`ResearchDatabase_MVP_
ImplementationPlan.md` §9 롤백 전략과 동일 원칙 — 데이터 모델(`UpgradeNode`/`UpgradeManager`/
`DefaultUpgradeTreeFactory`) 무변경이 최대 안전망).

1. **커밋 1 — 데이터 준비(문서만)**: 45개 노드 (표시명/설명/브랜치 라벨) 확정표 산출. 코드 변경
   없음. (§1 Step 1~2)
2. **커밋 2 — UXML/USS 골격**: `UpgradeTree.uxml`에 `branch-board` 컨테이너 추가 + `buy-button`
   제거, `UpgradeTree.uss` 신규 클래스 추가. 이 시점 C#은 미변경이라 컨테이너는 빈 채로 남음
   (화면이 깨지지 않는 안전한 중간 상태). (Step 3~4)
3. **커밋 3 — ResearchPopup UXML/USS 신규**: 아직 어느 컨트롤러도 참조하지 않아 완전히 독립적,
   되돌리기 가장 쉬운 커밋. (Step 5~6)
4. **커밋 4 — 표시 데이터 계층**: `NodeDisplayNames` 교체 + `NodeDescriptions`/`NodeBranch`
   추가(미사용 상태, 빌드 영향 없음 — 안전한 중간 지점). (Step 7)
5. **커밋 5 — 브랜치 보드 + 리스트 렌더링 교체**: 더미 데이터 제거, `BuildBranches()`/
   `BuildBranchBoard()`/`SelectBranch()`/`BuildResearchList()`(활성 브랜치 전용)/
   `BuildBranchSummary()` 구현. **가장 리스크가 큰 단일 커밋** — 화면이 실제로 브랜치 보드로
   전환되는 지점. (Step 8~12)
6. **커밋 6 — `ResearchPopupController` 신규 + 이벤트 배선**: 컨트롤러 구현,
   `UpgradeTreeView.OnResearchItemSelected` 이벤트 발행 추가. 아직 `UIManager`가 구독 안 해
   컴파일만 통과하고 팝업은 실제로 안 뜸(안전). (Step 13~14)
7. **커밋 7 — `UIManager` 코디네이터 배선**: 3개 뷰 구독 → 팝업 오픈, `OnNodeUnlocked` 구독 →
   활성 뷰 갱신. **이 커밋부터 팝업이 실제로 동작**. (Step 15)
8. **커밋 8 — 잠금 사유·CTA 폴리싱**: `LockReason()`, 버튼 라벨 4갈래 최종 정리. (Step 16~17)
9. **커밋 9 — 구 코드 정리**: `Dummy*` 전체 삭제, 미사용 참조 정리. (Step 18)
10. **커밋 10 — 문서 갱신**: `DESIGN.md`/`QA_Checklist.md`/`unity-editor-task.md`/`CLAUDE.md`.
    코드 아님, 다른 커밋과 의존성 없어 병렬 작업 가능. (Step 19~22)
11. **커밋 11(별도, 사람이 직접 수행) — Unity 에디터 씬 배선 + 실기기 검증**: `unity-editor-task.md`
    항목 실행 후 QA 체크리스트 통과 확인. (Step 23)

**병합 게이트**: 커밋 5(브랜치 보드 전환)와 커밋 7(팝업 동작)이 이 시퀀스에서 가장 위험도가
높다 — 각각 단독으로 문제가 생겨도 그 커밋만 revert하면 직전 안전한 중간 상태로 복귀 가능하다.
커밋 2~4가 "병행 유지 기간"(구 더미 렌더링이 아직 화면에 남아있는 상태)을 의도적으로 두는 것도
동일한 이유다.

---

## 5. 다음 세션 확인 권장 (구현을 막지는 않음)

- **브랜치 선택 기억 정책**: 이 계획은 기본값으로 "카테고리 재진입 시 항상 미완료 첫 브랜치로
  초기화"를 채택(§10 `SelectBranch` 기본값). 마지막 선택 기억이 필요하면 필드 하나 추가로 확장
  가능 — 이번 범위에 포함하지 않음.
- **`detail-rows` 문단 텍스트 허용 범위**: 이 계획은 V2 문서의 해석(1회성 팝업 설명 문단은
  `DESIGN.md`의 "문단형 설명 텍스트로 되돌리지 않는다" 규칙 예외)을 그대로 따른다 — `DESIGN.md`
  갱신(커밋 10) 시 이 해석을 명문화한다.
- **4브랜치를 넘는 확장 시나리오**: 이번 범위 밖(V2 문서 §7-5와 동일).
- **DNA 비용 프리미엄 / 항원 변이 확률 밸런스**: `NodeMapping.md` §8과 동일하게 범위 밖, 별도
  밸런스 리뷰 세션 필요.

이 문서는 구현 계획서이며 위 어느 Step도 아직 구현되지 않았다.
