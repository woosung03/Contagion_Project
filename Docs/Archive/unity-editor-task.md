# Unity 에디터 작업 목록 — Hud v2 (프로토타입 레이아웃 이식)

이번 패스는 Hud.uxml/Hud.uss를 HTML 프로토타입 v2(Resource Strip / Event Dock / Country Dock /
지도 중심 / Action Strip) 구조로 전면 재구성했고, 이를 실제로 동작시키기 위해 C#도 3개 파일
수정/추가했다: `CountryView.cs`(클릭 트리거 복원), `CountryDockController.cs`(신규),
`NewsFeedController.cs`(이벤트 심각도 점), `HudController.cs`(위협 상태 강조 클래스 1줄).
코드 도구로는 여기까지만 가능하고, 아래는 **Unity 에디터에서 사람이 반드시 해야 하는 작업**이다.

## 1. 최우선 — 씬 배선 (이거 안 하면 Country Dock이 절대 안 움직인다)

- [ ] **`CountryDockController.cs`를 GamePlay 씬의 Hud UIDocument GameObject에 Add Component로
      추가할 것.** `HudController`/`NewsFeedController`가 이미 붙어있는 바로 그 GameObject다.
      코드 파일만 만들어서는 스크립트가 아무 데도 붙어있지 않아 country-dock이 영원히
      플레이스홀더("국가를 선택하세요")로만 남는다 — 반드시 확인.
- [ ] 위 작업 후 Console에 `country-dock-name` 등 6개 라벨 쿼리 관련 에러가 없는지 확인
      (Hud.uxml에 해당 이름 요소들을 이미 넣어뒀으니 정상이면 에러 없어야 함).

## 2. 국가 클릭 트리거 복원 관련 (중요 리스크)

- [ ] `CountryView.cs`에 `OnMouseUpAsButton()`을 다시 추가해 국가 탭 시 `WorldMap.HandleCountryClicked`가
      호출되게 했다. **이건 Step 28-2에서 "국가가 촘촘해서 탭 정확도가 낮다"는 이유로 의도적으로
      제거됐던 기능이다.** 되살렸으니 같은 문제가 재발할 수 있다 — 실기기에서 작은 국가(예: 베네룩스,
      한반도 주변)를 반복 탭해보고 원하는 국가가 잘 선택되는지, 아니면 자꾸 엉뚱한 이웃 국가가
      선택되는지 확인할 것.
- [ ] 오탭이 잦으면: (a) `CountryView`의 `BoxCollider2D` 크기를 시각적 스프라이트보다 살짝
      넉넉하게(예: 1.1~1.3배) 키우는 걸 검토, (b) 그래도 부족하면 country-dock을 "탭 선택"이 아니라
      "국가현황 리스트에서 행을 눌러 채우는" 방식으로 트리거를 바꾸는 대안도 고려(리스트는 이미
      탭 정확도 문제가 없는 UI이므로).
- [ ] 지도 드래그(좌우 스크롤) 중에 국가가 잘못 선택되지 않는지 확인 —
      `WorldMapCameraController.WasDragging` 가드를 넣어뒀지만 실제 손맛은 실기기 확인 필요.

## 3. 레이아웃/스타일 확인

- [ ] resource-strip(최상단, 일차/DNA/잠복기/위협단계) 한 줄에 텍스트가 잘리지 않고 다 들어가는지.
- [ ] event-dock(좌상단)과 country-dock(우상단)이 겹치지 않는지, 실기기(1440x3120 참조 해상도)
      기준으로 두 도킹 패널이 지도를 과하게 가리지 않는지.
- [ ] event-dock 안 이벤트 항목에 심각도 점(빨강/파랑/회색)이 색상별로 정상 표시되는지 — Positive/
      Negative/Flavor 이벤트를 각 1건씩 실제로 발생시켜(자연 발생 대기 또는 디버그) 확인.
- [ ] country-dock: 아무 국가도 안 눌렀을 때 "국가를 선택하세요" + "-"만 보이는지, 국가를 누르면
      실제 값(인구/감염률/사망률/의료수준/국경)으로 바뀌는지, 다른 국가를 다시 누르면 갱신되는지,
      틱이 지나면서 선택된 국가의 감염률 등이 실시간으로 갱신되는지.
- [ ] graph-panel(감염자/사망자/치료제 그래프, 52px)과 action-strip(하단 3버튼)이 정상 표시되는지 —
      이 둘은 이전 패스에서 이미 검증한 구조라 회귀 위험은 낮음.
- [ ] world-status-label — 평시엔 자리 차지 없이 사라지고, 위협 단계 진입 시 강조 배경이 붙는지.

## 4. 회귀 테스트

- [ ] MainMenu → CountrySelect → GamePlay 진입 플로우 정상 동작.
- [ ] 업그레이드(전파/증상/능력 3탭 페이징)/국가현황(전체 목록 패널)/랭킹 버튼 정상 동작 —
      action-strip으로 이름만 바뀌었을 뿐 UIManager/HudController 이벤트 발행 경로는 안 건드렸다.
- [ ] 재시작 루프(`SceneManager.LoadScene` + 매니저 `ResetForNewGame()`) 정상 동작.
- [ ] DNA 버블 스폰, 감염 점/색상 오버레이 등 지도 관련 기존 기능이 country-dock 도입과 무관하게
      정상 동작하는지(같은 지도 오브젝트를 다루므로 회귀 확인 권장).

## 5. 알아두면 좋은 것 (당장 처리 안 해도 됨)

- `CountryPopupController.cs`/`CountryPopup.uxml`은 여전히 미사용(죽은 코드) 상태로 남아있다.
  이번에 country-dock이 사실상 그 역할(국가 클릭 시 정보 표시)을 대체했으므로, 이 파일들을
  완전히 정리(삭제 또는 명시적 비활성화)할지는 별도로 판단할 것 — 이번 패스에서는 건드리지 않았다.
- Country Dock은 폭이 좁아 항목을 인구/감염률/사망률/의료수준/국경 5개로 압축했다(기후 등은 생략).
  필요하면 `CountryDockController.Populate()`에 항목을 추가하면 된다 — Country.cs에 climate,
  governmentStability, GetCollapseStage() 등 아직 안 쓴 필드가 더 있다.
- 이벤트 로그 접기/펼치기 토글은 이전 패스에서 검토했었지만 이번엔 프로토타입 레이아웃 우선
  원칙에 따라 제외했다 — 필요하면 이후 별도 요청으로 다시 추가 가능(event-dock 헤더에 토글
  버튼 한 줄 추가 + NewsFeedController에 display 토글 로직 추가로 재현 가능).

## 6. SF 전술 디스플레이 시각 언어 패스 (신규, C# 변경 없음 — Hud.uxml/Hud.uss/Theme.uss만 수정)

이번 패스는 레이아웃/기능은 그대로 두고 시각 언어만 바꿨다(발광 헤어라인, 코너 브래킷, 내부
격자선, DNA 그린 액센트, 색상 칩, 그래프 격자, WORLD STATUS 프레임, Country Dock 판독행 스타일).
전부 UXML/USS 정적 마크업이라 C# 쿼리 경로는 전혀 안 건드렸지만, 실기기/에디터에서 아래를
확인할 것:

- [ ] `corner-cut` 4개(코너 브래킷)가 event-dock/country-dock 네 귀퉁이에서 살짝 삐져나온
      대각선 발광 틱으로 보이는지 — `rotate` 트랜스폼이 실제 화면에서 의도한 대로(45°) 렌더링
      되는지, 혹시 위치가 어긋나 보이면 `Hud.uss`의 `.corner-cut--tl/tr/bl/br` 좌표(top/left/right/
      bottom 5px, -4px)를 조정할 것.
- [ ] `world-status-frame`(평시에도 항상 보이는 얇은 테두리 프레임)이 resource-strip 우측 끝에서
      너무 크거나 작지 않은지, "STATUS" 캡션이 겹치거나 잘리지 않는지.
- [ ] `stat-chip`(DNA/잠복기 라벨 앞 6x6 색상 사각 칩)이 라벨과 수직 정렬이 맞는지.
- [ ] country-dock 각 행(country-dock__row) 아래 옅은 격자선이 "판독행"처럼 자연스러운지, 너무
      진하게 보이면 `--color-grid-line`의 alpha(현재 0.12)를 낮출 것.
- [ ] graph-panel의 3개 그래프에 25%/50%/75% 격자선이 스파크라인(HudSparkline)을 가리지 않는지 —
      HudSparkline이 그리는 실제 그래프 라인이 격자선보다 위 레이어에 그려지는지(트리 순서상
      `infected-graph` 등이 gridline/baseline보다 뒤에 선언돼 있어 위에 그려질 것으로 예상되나
      실제 확인 필요).
- [ ] tab-button(업그레이드/국가현황)에 준 발광 테두리가 버튼 배경색과 잘 어울리는지, 랭킹
      버튼(보조, 중립 테두리)과 시각적 우선순위 차이가 잘 느껴지는지.
- [ ] 전체적으로 초록 액센트가 과해서 산만하지 않은지 — 과하면 `Theme.uss`의
      `--color-accent-glow-soft` alpha(현재 0.35)만 낮추면 전체적으로 톤 다운됨.

## 7. 커밋

- [ ] 위 항목이 전부 확인되면 CLAUDE.md 규칙대로 git 커밋. CountryDockController를 씬에 붙이는
      작업은 씬 파일(.unity) 변경을 동반하므로 특히 저장 확인 후 커밋할 것(Step 56 파일 손상
      재발 방지 차원에서 중요).
- [ ] CLAUDE.md "최근 작업" 표에 이번 Step을 1줄로 추가하고, 상세 배경(왜 country-dock을
      되살렸는지, 국가 클릭 트리거 재도입의 리스크 등)은 DevLog.md에 옮겨 기록.

## 8. 대기 중인 씬/에셋 배선 작업 (CLAUDE.md TODO에서 이동, 2026-07-09)

Hud v2 패스와 무관한, 별도로 대기 중인 에디터 수동 작업 목록.

- [ ] `MainMenu`/`CountrySelect`/`GamePlay` 씬 분리 (현재 씬 하나에서 UIDocument 패널 on/off로 대체 중)
- [ ] `DnaBubble` 프리팹 제작 후 `BubbleSpawner.bubblePrefab`에 연결
- [ ] 앱인토스 SDK 설치 — pnpm lockfile 충돌로 보류 중, 재시도 시 `pnpm install --no-frozen-lockfile` 수동 실행 필요
- [ ] `AudioManager` 오브젝트 생성 + 효과음 에셋 준비/연결
- [ ] (선택, 낮은 우선순위) MainMenu DNA+5 보상형 광고 버튼 — `GameAds.Rewarded` 재사용

## 9. PolygonCollider2D 전환 — Physics Shape 배치 생성 (Step 73, 신규)

국가 선택 정확도 문제(인접국 오탭)의 근본 원인은 콜라이더가 실제 국경선이 아니라 사각형
(BoxCollider2D)이었던 것이었다. `CountryView.cs`는 이제 `PolygonCollider2D` + 국가 실루엣
polygon physics shape 기반으로 바뀌었지만, 그 physics shape 데이터는 **Unity 에디터에서 텍스처를
재임포트해야만** 생성된다 — 코드 도구로는 실행 불가능한 유일한 수동 단계다.

- [x] **(필수, 최우선) 상단 메뉴 `Contagion → Country Shapes → Generate Physics Shapes (48개국)`
      실행.** `Assets/Editor/CountryShapePhysicsShapeGenerator.cs`가 `Resources/CountryShapes/`의
      48개 PNG 전부 `TextureImporter.spriteGenerateFallbackPhysicsShape`를 켜고 재임포트한다.
      **2026-07-10 사용자 실행 확인 — 정상 완료.**
- [ ] 이어서 `Contagion → Country Shapes → Validate Physics Shapes (48개국)` 실행해 48개국 전부
      physics shape가 채워졌는지 확인(콘솔 로그로 "physics shape 있음 48개 / 없음 0개" 확인).
- [ ] 재임포트로 텍스처 관련 씬/프리팹 참조가 깨지지 않았는지(스프라이트 GUID는 그대로라 깨질
      가능성은 낮지만) GamePlay 씬을 열어 지도가 정상 렌더링되는지 눈으로 확인.
- [ ] Play 모드로 실기기/에디터 게임 뷰에서 실제 클릭 테스트 — 상세 체크리스트는
      `Docs/QA_Checklist.md`의 "PolygonCollider2D 전환 검증" 섹션 참고.
- [ ] 확인 완료 후 git 커밋(신규 파일 `Assets/Editor/CountryShapePhysicsShapeGenerator.cs` +
      48개 `CountryShapes/*.png.meta` 변경분 포함, `.unity` 씬 파일은 이번 변경에서 건드리지
      않았으므로 diff에 나오면 안 됨 — 나오면 실수로 저장된 것이니 되돌릴 것).

## 10. BottleneckAnalyzer / ResearchRecommender 씬 배선 (Phase 1 검증, 신규)

업그레이드 시스템 2.0 Phase 1(`BottleneckAnalyzer.cs`/`ResearchRecommender.cs`,
`HumanResistanceManager.OnPolicyApplied` 이벤트 체인) 코드는 전부 작성·컴파일 확인
완료됐지만, 두 컴포넌트 다 스크립트만 있고 씬의 어떤 GameObject에도 붙어있지 않다 —
Add Component 하지 않으면 `Instance`가 계속 null이라 아무 것도 동작하지 않는다.
UI/UpgradeTree는 이번 패스에서 연결하지 않으므로(Phase 2 대기), 동작 확인은 전부
Console 로그로만 한다.

### 씬 배선 절차

- [ ] **`BottleneckAnalyzer.cs`를 GamePlay 씬의 "Managers" GameObject(`SimulationManager`/
      `HumanResistanceManager`/`UpgradeManager`/`WorldDataManager` 등이 이미 붙어있는 바로
      그 오브젝트)에 Add Component로 추가할 것.** 새 GameObject를 만들지 않는다 — 기존
      매니저들과 같은 오브젝트, 같은 DontDestroyOnLoad 라이프사이클을 공유해야
      `GameDataBootstrapper.ResetPersistentManagersForNewGame()`이 정상 동작한다.
- [ ] **`ResearchRecommender.cs`도 같은 GameObject에 Add Component.**
- [ ] 두 컴포넌트 Inspector에 노출된 튜닝 필드(`historyWindowTicks`, `spreadStallRatio`,
      `lowLethalityThreshold`, `visibilitySurgeThreshold`, `cureUrgentEtaTicks`,
      `maxRecommendations` 등)는 기본값 그대로 두고 1차 검증을 시작한다 — 조정은 아래
      검증 결과를 본 뒤 판단.
- [ ] Play 진입 직후 Console에 두 스크립트 관련 NullReferenceException/
      MissingComponentException이 없는지 확인(있으면 컴포넌트 부착 위치나 순서 문제).

### Play Mode 검증 절차

- [ ] Console 검색창에 `[BottleneckAnalyzer]` / `[ResearchRecommender]`로 필터링해서 로그만
      걸러 볼 것 — 다른 매니저 로그와 섞여 있다.
- [ ] `[BottleneckAnalyzer]` 로그는 병목 **유형이 바뀔 때만** 찍힌다(같은 유형이 계속 유지되는
      동안은 조용함) — 로그가 안 뜬다고 무조건 이상 동작은 아니다, 병목이 없는(`None`) 상태가
      계속 유지 중일 수 있다.
- [ ] `[ResearchRecommender]` 로그는 `[BottleneckAnalyzer]` 로그 직후 항상 1개씩 따라와야
      한다(둘이 같은 이벤트 체인) — 하나만 뜨고 하나는 안 뜨면 구독이 끊긴 것.
- [ ] 병목 로그가 뜬 시점에 기존 화면(CountryStatusPanel/HUD)을 직접 봐서 그 판정이 실제
      상황과 맞아떨어지는지 육안 대조하고, 로그의 `Evidence` 문자열을 근거로 기록.
- [ ] 4개 플레이 테스트 시나리오(확산 위주/치사율 위주/치료제 방치/DNA 과소비 — 대화 기록의
      "Phase 1 검증 계획" 참고)를 각각 최소 1회 진행하며 해당 병목이 뜨는지/안 뜨는지 확인.
- [ ] 오탐(예상과 다른 병목이 뜨거나, 떠야 할 병목이 안 뜬 경우) 발견 시 그 틱의 `Evidence`
      문자열과 함께 기록 — 임계값(Inspector 필드) 조정 대상 후보.

### 이번 패스에서 하지 않은 것(참고)

- `ResearchRecommender` 결과를 UpgradeTree UI에 연결하는 작업 — Phase 2.
- A1/A2/A3/B1/D1/D3/C2(정보성 병목) — Phase 1.5.

## 11. UpgradeTree 최종 검증 (캔버스 승격 + ResearchPopup 개편, Commit 1~4)

UpgradeTree를 Painter2D 캔버스로 승격하고 ResearchPopup을 비용/효과/주의/구매 버튼/고급
정보 순서로 재구성하는 코드 작업은 전부 완료됐다(정적 분석·컴파일 확인까지). 아래는 Unity
Editor Play Mode 실행이 필요해 코드 도구로는 확인할 수 없는 항목이다 — DevLog.md
"Commit 1~5 — UpgradeTree 캔버스 승격" 항목 참고.

### 트리 캔버스

- [ ] 전파/증상/적응 3탭 각각 진입 시 캔버스(노드 15개)가 정상 표시되는지.
- [ ] 노드 간 연결선(`TreePathElement`, Painter2D)이 표시되는지 — 해금된 경로는 밝고 굵게,
      미해금 경로는 흐리고 얇게 구분되는지.
- [ ] 노드를 탭하면 연결선에 클릭이 막히지 않고 정상적으로 `ResearchPopup`이 열리는지
      (`pickingMode: Ignore` 검증).
- [ ] 합류 노드(선행 2개)와 최종 진화 노드가 일반 노드와 시각적으로 구분되는지(테두리 굵기/색).
- [ ] 세로 스크롤이 정상 동작하는지(캔버스가 화면보다 큰 경우).

### ResearchPopup 4갈래 버튼 상태

- [ ] **구매 가능**: `"N DNA · 연구하기"` 표시 + 버튼 활성.
- [ ] **DNA 부족**: `"DNA 부족"` 표시 + 버튼 비활성.
- [ ] **잠김**(선행조건 미충족): `"선행 연구 필요"` 표시 + 버튼 비활성.
- [ ] **이미 연구 완료**: `"연구 완료"` 표시 + 버튼 비활성.
- [ ] 구매 성공 시: DNA 차감 → 팝업 닫힘 → 트리 캔버스가 즉시 갱신(경로가 밝아짐)되는지
      (`UpgradeManager.OnNodeUnlocked` → `HandleNodeUnlockedForCanvas` 경로).

### ResearchPopup 정보 표시

- [ ] 효과 목록 — 값이 0인 스탯이 실제로 숨겨지는지, 음수 효과(예: 패혈증 계열의 감염성
      감소)도 부호 그대로 표시되는지.
- [ ] 주의 목록 — severity/lethality가 **증가**하는 노드에서만 뜨는지, **감소**하는 노드
      (은신 계열 등)에서는 안 뜨는지.
- [ ] 효과·주의 둘 다 없을 때 해당 섹션이 완전히 숨겨지는지.
- [ ] 고급 정보 `Foldout`이 기본 접힘 상태로 열리는지, 펼쳤을 때 감염성/심각성/치사율/
      약물저항 4종 막대가 현재값/적용후값으로 정상 표시되는지(이 노드가 안 건드리는 스탯도
      포함해 항상 4개 다 보이는지).
- [ ] 팝업을 닫고 다른 노드를 열었을 때 `Foldout`이 다시 접힘 상태로 초기화되는지(이전 노드의
      펼침 상태가 남지 않는지).

### 공통

- [ ] Play 모드 진입~퇴장까지 Console에 Warning/Error가 없는지(일반 Log도 최소 상태 유지 —
      기존 세션 원칙).
- [ ] `NullReferenceException` 없이 탭 전환·노드 클릭·구매·팝업 닫기를 반복해도 안정적인지.

## 12. SafeAreaApplier 누락 씬 배선 (Layout Architecture Refactoring, 2026-07-21)

Responsive Layout Audit(같은 날짜)에서 `GamePlay.unity`를 직접 확인한 결과, `SafeAreaApplier`
컴포넌트가 `hud-root`/`mainmenu-root`(×2)/`upgrade-root`(×3, 전파·증상·적응 페이지별)/
`ending-root`에는 붙어 있지만 **`status-root`(CountryStatusPanel)와 `ranking-root`(RankingPanel)
GameObject에는 하나도 없다** — 이 두 화면만 노치/펀치홀/제스처 바 안전영역을 전혀 보정받지
못하는 상태다. `.unity` 씬 파일은 지금 Unity Editor가 열려 있는 상태에서 코드 도구로 직접
편집하면 에디터가 나중에 저장할 때 덮어써 사라질 위험이 있어(재발 방지 차원, Step 56 파일
손상 사례 참고), 아래는 반드시 에디터에서 직접 Add Component 할 것.

- [ ] **CountryStatusPanel의 UIDocument GameObject**(`CountryStatusPanelController`가 이미
      붙어있는 바로 그 오브젝트)에 `SafeAreaApplier` Add Component. `Target Class`에
      `status-root` 입력, `Nav Bar Reserve Top`은 기존 `upgrade-root` 항목들과 동일하게 `48`로
      맞출 것(다른 화면과 통일).
- [ ] **RankingPanel의 UIDocument GameObject**(`RankingPanelController`가 붙어있는 오브젝트)에도
      동일하게 `SafeAreaApplier` Add Component, `Target Class`는 `ranking-root`, `Nav Bar
      Reserve Top`은 `48`.
- [ ] 이번 리팩터로 `status-root`/`ranking-root`는 `position:absolute; top:%/bottom:%`가
      아니라 순수 flex-column(자식: top-spacer/실제 패널 박스/bottom-spacer)으로 바뀌었다 —
      `SafeAreaApplier`가 이 root에 적용하는 상하 padding은 `hud-root`가 이미 쓰는 것과 동일한
      방식으로 top/bottom spacer 영역을 안전영역만큼 자동으로 늘려준다. Add Component 후
      Play Mode에서 실기기/노치 프리뷰(`previewInEditor` 옵션)로 상단 텍스트/버튼이 노치에
      가려지지 않는지 확인할 것.
- [ ] 세 화면(Upgrade/CountryStatus/Ranking) 모두 Add Component 완료 후, Console에
      `SafeAreaApplier` 관련 경고/에러가 없는지 확인.
