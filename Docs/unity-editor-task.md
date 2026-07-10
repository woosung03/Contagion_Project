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
