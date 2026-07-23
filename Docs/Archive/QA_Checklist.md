# Contagion Project — QA 체크리스트

"확인해볼 것" 형태의 테스트/검증 항목을 모아두는 문서. CLAUDE.md에는 이런 항목을 두지 않고 전부
여기로 옮긴다. 항목이 확인 완료되면 체크(`[x]`) 후 근거(확인 날짜/방법)를 한 줄 남기고, 완전히
불필요해지면 삭제한다. 각 항목의 배경(왜 이 확인이 필요한지)은 `Docs/DevLog.md`의 해당 Step 참고.

---

## 세션 시작 시 확인 (미해결)

- [ ] 5분 프로토타입 밸런스 실측 검증 (근거: DevLog Step 93) — `cureProgressCoefficient`/
      `visibilityGainRate`/`globalSpreadFactor` 조정이 정적 코드 분석 기반 역산이라 Unity
      Editor 플레이테스트로 검증되지 않았다.
  - [ ] 아무 행동도 하지 않는 플레이어 기준 Day 240(4분) 전후 치료제 100% → Game Over가
        발생하는지 확인 (병원체는 Virus 기준으로 계산했음 — 다른 병원체도 확인 권장)
  - [ ] 일반적인 플레이(DNA 수집 + 업그레이드 구매) 기준 Day 300(5분) 전후 승패가 결정되는지 확인
  - [ ] 감염 시작 → DNA 획득 → 업그레이드 선택 → 국가 대응 시작 → 감염 확산 가속 → 치료제 개발
        압박 → 승리/패배 전체 루프가 한 판 안에서 자연스럽게 발생하는지 확인
  - [ ] 목표(4~6분)에서 벗어나면 `cureProgressCoefficient`부터 재조정(DevLog Step 93 계산 근거 참고)

- [ ] CountryStatusPanel v5 Hero Stats/ADVANCED DIAGNOSTICS 검증 (근거: DevLog Step 89) — Unity
      에디터 미접속으로 작성돼 실기기/에디터 검증 전부 미완료.
  - [ ] GLOBAL STATUS 배너 바로 아래에 INFECTED/DEATHS/CURE/EXTINCT 2x2 Hero Stats 타일이
        보이고, 각 값이 실제 WorldState 값(감염자 수/사망자 수/치료제 진행률/소멸 국가 수)과
        일치하는지 확인
  - [ ] Hero Stats 타일이 2열×2행으로 정렬되고(타일 폭 48%, 줄바꿈 정상), 숫자가 라벨보다
        눈에 띄게 크게(24px) 보이는지 육안 확인
  - [ ] WORLD OVERVIEW에서 "총 감염자"/"총 사망자"/"무감염 국가 수" 행이 더 이상 보이지 않고,
        세계 인구/세계 감염률/치사율(추정)/경과 일수/감염 국가/소멸 국가/population-bar는
        그대로 남아있는지 확인
  - [ ] ADVANCED DIAGNOSTICS 헤더가 기본 접힘(▶) 상태로 시작하는지, 클릭 시 펼쳐지며(▼) 국가
        상태 분포(SAFE/WARNING/DANGER/COLLAPSE)와 의료 시스템 현황(정상/주의/과부하/붕괴)이
        정상 표시되는지 확인
  - [ ] ADVANCED DIAGNOSTICS를 펼친 상태로 두고 틱이 진행돼도(감염 확산) 각 버킷 수치가 정상
        갱신되는지, 48개국 목록 대륙 아코디언의 펼침 상태와 서로 간섭하지 않는지 확인
  - [ ] Hero Stats 값 색상(감염=infected색, 사망=dead색, 치료제=info색)이 severity 4색 체계와
        일치하고 새로운 색이 추가되지 않았는지 육안 확인
- [ ] CountryStatusPanel 대륙별 접기/펼치기 아코디언 검증 (근거: DevLog Step 86) — Unity 에디터
      미접속으로 작성돼 실기기/에디터 검증 전부 미완료.
  - [ ] GLOBAL STATUS CENTER를 열면 ASIA 섹션만 펼쳐져 있고 나머지 5개 대륙(EUROPE/NORTH
        AMERICA/SOUTH AMERICA/AFRICA/OCEANIA)은 접혀 있는지 확인
  - [ ] 대륙 헤더를 클릭할 때마다 그 대륙만 펼침↔접힘이 토글되고(다른 대륙은 영향 없음), 화살표가
        ▼(펼침)/▶(접힘)로 정확히 바뀌는지 확인
  - [ ] 각 대륙 헤더의 국가 수 표기(예: "ASIA (19)")가 실제 하위 국가 행 개수와 일치하는지, 6개
        대륙 합계가 48인지 확인
  - [ ] 접힌 대륙의 국가 행이 실제로 화면에서 사라지고(레이아웃 공간도 차지하지 않고) ScrollView
        높이가 그만큼 줄어드는지 확인
  - [ ] 국가 행 클릭 시 기존과 동일하게 CountryPopup이 뜨는지(대륙 헤더 클릭과 국가 행 클릭이
        서로 간섭하지 않는지) 확인
  - [ ] 대륙 몇 개를 펼침/접힘으로 바꾼 뒤 국가 행을 클릭해 CountryPopup을 열고 닫아도 대륙
        펼침 상태가 그대로 유지되는지 확인
  - [ ] 감염 확산 중 특정 국가의 상태가 바뀔 때(`OnCountryChanged`) 해당 국가가 속한 대륙 섹션이
        접혀 있어도 프레임 드랍 없이 정상 갱신되는지(안 보이는 행도 라벨 텍스트는 갱신됨) 확인
  - [ ] 대륙 헤더에 severity 색(감염/사망/위험/정보)이 아니라 구조용 발광색만 쓰였는지 육안 확인
        (DESIGN.md Severity Colors Don't 규칙 준수 여부)
- [ ] CountryStatusPanel 리스트→상세 팝업 드릴다운 검증 (근거: DevLog Step 85) — Unity 에디터
      미접속으로 작성돼 실기기/에디터 검증 전부 미완료.
  - [ ] GLOBAL STATUS CENTER를 연 상태에서 48개국 목록 중 아무 행이나 클릭하면 CountryPopup이
        해당 국가 데이터로 정상 표시되는지 확인
  - [ ] 팝업이 뜬 상태에서 CountryStatusPanel(뒤에 깔린 화면)이 시각적으로 가려지지 않고 팝업이
        항상 위에 렌더링되는지 확인 (`CountryPopupUI` sortingOrder를 1→2로 올려 수정)
  - [ ] 팝업의 ✕(닫기) 버튼이 CountryStatusPanel에 클릭을 가로채이지 않고 정상적으로 반응해
        팝업만 닫히는지, 이때 CountryStatusPanel은 계속 열려 있는지 확인
  - [ ] 팝업을 닫은 뒤 다른 행을 연달아 클릭해도(같은 국가 재클릭 포함) 데이터가 매번 올바르게
        갱신되는지 확인
  - [ ] 지도를 직접 클릭했을 때의 기존 동작(CountryPopup 표시 + GlobalStatus가 열려 있었다면
        자동으로 닫힘)이 이번 변경으로 깨지지 않았는지 함께 확인
- [ ] Research Database UI Shell 검증 (근거: DevLog Step 82) — Unity 에디터 미접속으로 작성돼
      실기기/에디터 검증 전부 미완료.
  - [ ] 업그레이드 버튼 클릭 시 화면이 정상 표시되는지, 콘솔 에러/경고 0건인지 확인
  - [ ] 상단 탭 3개(전파/증상/적응) 클릭 시 실제로 다른 카테고리 화면으로 전환되는지, 전환 후에도
        DNA 표시·광고 보너스 버튼이 정상 동작하는지 확인
  - [ ] 각 탭에서 갈래 섹션 헤더(예: "공기 계열")와 그 아래 연구 항목 행이 세로 스크롤로 전부
        도달 가능한지, 좌측 accent-bar 색이 카테고리색과 일치하는지 확인
  - [ ] 잠김/연구 가능/진행 중/완료 4개 상태 색상이 리스트 행에서 구분되는지, 잠긴 행의
        "선행: OOO" 텍스트가 잘리지 않고 보이는지 확인
  - [ ] 연구 항목 행 클릭 시 하단 상세 패널에 이름/설명/비용/상태가 채워지는지, 선택된 행에
        `research-row--selected` 강조가 적용되는지 확인
  - [ ] "연구 시작" 버튼이 항상 비활성화 상태로 보이는지(Commit 1은 데이터 미연결이라 의도된
        동작 — Commit 2에서 활성화 로직 추가 예정) 확인
  - [ ] 480px~1440px 폭 범위에서 탭 버튼/리스트 행 터치 영역이 48px 이상 확보되는지 확인
- [ ] CountryStatusPanel Console 에러 확인 — `CountryStatusPanel.uss (line 67): Unsupported
      selector format` 에러가 더 이상 안 뜨는지(주석 안에 의도치 않은 `*/`가 섞여 스타일시트
      전체가 파싱 실패했던 문제, 수정 완료) 최우선으로 확인. 이 에러가 사라져야 그동안의 배경
      불투명도/인셋/오버플로우 수정이 비로소 실제로 적용된다 (근거: DevLog Step 80 추가 대응 4)
- [ ] CountryStatusPanel 열린 상태에서 국가 클릭 시 자동으로 닫히는지, 그 직후 CountryPopup의
      ✕ 버튼이 정상적으로 반응하는지(클릭 시 팝업이 닫히는지) 확인 (근거: DevLog Step 80 추가
      대응 3)
- [ ] CountryStatusPanel 오버플로우 수정 검증 — `status-scroll`에 `flex-basis: 0`을 추가한
      뒤 UI Builder(또는 Play 모드)에서 "국가 상태 분포"~"48개국 목록"까지 전 구간이 전부
      불투명한 배경(`--color-bg-panel-strong`, 완전 불투명) 위에서 보이는지, checkerboard
      (투명) 영역이 더 이상 없는지 확인. 콘텐츠가 패널 높이보다 길면 `status-root`의
      `overflow: hidden`으로 스크롤 없이 잘리는 게 아니라 `status-scroll` 내부에서 정상
      스크롤되는지도 함께 확인 (근거: DevLog Step 80 추가 대응 2 — 진짜 원인은 배경 불투명도가
      아니라 flex-basis 누락으로 인한 세로 오버플로우였음)
- [ ] CountryStatusPanel(GLOBAL STATUS CENTER) 화면 크기 확인 — `top: 8%`로 확장한 패널이
      1440×3120 세로 화면에서 실제로 잘리지 않는지, 하단 `bottom: 12.5%` 아래로 Hud
      tabs-row가 계속 노출·클릭 가능한지 확인 (근거: DevLog Step 80)
- [ ] CountryStatusPanel의 `population-bar`가 HUD와 별도인 이 화면 전용 `UIDocument` 인스턴스에서
      정상 렌더링/갱신되는지(같은 이름 `population-bar-healthy` 등을 HUD와 별개로 재사용) 확인
      (근거: DevLog Step 80)
- [ ] CountryStatusPanel 국가 상태 분포(SAFE/WARNING/DANGER/COLLAPSE) 4버킷 합계가 항상 48이
      되는지, 감염 진행에 따라 버킷 분포가 합리적으로 이동하는지 확인 (근거: DevLog Step 80)
- [ ] CountryStatusPanel 랭킹 2종(종합 위협도/감염자 각 TOP 10)이 다른 국가의 값 변화만으로도
      틱마다(`OnWorldStateChanged`) 갱신되는지, 순서가 실제 수치와 일치하는지 확인 (근거: DevLog
      Step 80/81 — Step 81에서 감염률/치사율/의료부하 3개 랭킹이 종합 위협도 하나로 통합됨)
- [ ] CountryStatusPanel 48개국 목록 좌측 accent bar 색이 `GetCollapseStage()` 변화에 따라
      실시간으로(국가 개별 `OnCountryChanged`) 바뀌는지, 기존 O(1) 행 캐싱이 깨지지 않았는지
      (패널을 오래 열어둬도 프레임 드랍이 재발하지 않는지) 확인 (근거: DevLog Step 80)
- [ ] CountryStatusPanel GLOBAL STATUS 배너 — 게임 진행에 따라 CONTAINMENT SUCCESS →
      OUTBREAK EXPANDING → GLOBAL PANDEMIC → WORLD COLLAPSE IMMINENT 순으로 실제로 전환되는지,
      각 단계의 색(info/infected/danger/dead)이 텍스트+테두리에 함께 적용되는지 확인 (근거:
      DevLog Step 81 — 임계값(감염률 5%/50%)이 제안값이라 실제 플레이 곡선에 맞는지도 함께 확인)
- [ ] CountryStatusPanel 감염 국가 현황(감염/무감염/소멸 국가 수)이 "국가 상태 분포" 4버킷 합계
      (48)와 별도로 항상 정합적인지(감염 국가+무감염 국가=48, 소멸 국가 수가 COLLAPSE 버킷 수를
      넘지 않는지) 확인 (근거: DevLog Step 81)
- [ ] CountryStatusPanel 의료 시스템 현황(정상/주의/과부하/붕괴) 4버킷 합계가 항상 48이 되는지,
      "국가 상태 분포"와 다른 축이라 두 분포의 버킷 분류가 서로 달라도(예: 사망률은 낮지만 의료
      부하는 높은 국가) 정상으로 보이는지 확인 (근거: DevLog Step 81)
- [ ] CountryStatusPanel 종합 위협도(THREAT INDEX) TOP10 순위가 감염률/치사율/의료부하 3요소를
      실제로 반영하는지(단순 감염자 TOP10과 순서가 달라야 정상) 확인 (근거: DevLog Step 81 — 가중치
      0.4/0.3/0.3이 제안값이라 플레이테스트로 체감 위험도와 맞는지 함께 확인)
- [ ] CountryPopup 도넛 차트 렌더링 확인 — `CountryDonutChart.cs`가 `Painter2D.Arc()`/`Angle`
      API로 그린 3개 부채꼴(건강/감염/사망)이 실제로 컴파일·렌더링되는지(Unity 에디터 미접속으로
      미검증), 가운데 구멍 오버레이 색(`CountryDonutChart.HoleColor`)이 `CountryPopup.uss`
      `.popup-root` 배경색(`--color-bg-panel`)과 시각적으로 자연스럽게 맞물리는지 확인 (근거:
      DevLog Step 78)
- [ ] CountryPopup 도넛 범례 3줄(건강/감염/사망) 텍스트가 340px 폭 안에서 줄바꿈 없이 들어가는지,
      값이 도넛 조각 색상과 정확히 같은 순서로 매핑되는지(건강=파랑/감염=주황/사망=빨강) 확인
      (근거: DevLog Step 78)
- [ ] CountryPopup 신규 data-row(감염률/치사율(추정)/의료 시스템 상태) 값이 실제 감염 진행 중인
      국가에서 합리적으로 나오는지 확인 (근거: DevLog Step 78)
- [ ] CountryPopup "감염자 순위"/"사망자 순위"/"감염률 순위" 3행이 각각 올바른 국가에서 다른
      순번을 보여주는지, 표시 중인 국가가 아닌 **다른** 국가의 값 변화만으로도 순위가 틱마다
      갱신되는지(`OnWorldStateChanged` 구독 확인) 확인 (근거: DevLog Step 78/79)
- [ ] CountryPopup "이동 통제" 섹션 캡션(`modal-section-caption`)과 공항/항구/국경 3행의 좌측
      accent bar(`data-row--open`/`data-row--closed`, 3px)가 실제로 보이는지, 기존 `data-row`
      하단 구분선과 시각적으로 충돌(겹침/어색한 여백)하지 않는지 확인 (근거: DevLog Step 79)
- [ ] CountryPopup 위치 상향(`top: 30%`→`16%`) 후 상단 SafeArea/코너컷과 겹치지 않는지, 패널
      전체가 화면 안에 들어오는지 확인 (근거: DevLog Step 79 추가 대응)
- [ ] CountryPopup 패널 총 길이 확인 — 도넛+범례+행 6개(생존자 수/감염률/치사율/의료 시스템 상태/
      순위 3행)+"이동 통제" 캡션까지 추가로 길어진 만큼 1440×3120 세로 화면 `top: 30%` 기준 하단이
      잘리지 않는지, 잘리면 `modal-rows`를 `ScrollView`로 교체하는 안전장치 검토 (근거: DevLog
      Step 78/79)
- [ ] CountryPopup 오버플로우 수정 검증 — 중국(CHI)/인도(IND) 선택 시 인구수(N0 전체 숫자,
      Step 77로 축약 표기에서 되돌림)와 공항/항구/국경 3행이 340px 팝업 폭 안에서 잘리거나
      넘치지 않는지, 다른 46개국도 정상 표시되는지 확인. `country-row__meta`(MainMenu 48행
      리스트)도 인구 큰 국가(중국/인도) 행이 여전히 한 줄로 말줄임되는지(줄바꿈으로 행 높이가
      깨지지 않는지) 같이 확인 (근거: DevLog Step 76)
- [ ] GamePlay.unity 정상 로드 + MainMenu→CountrySelect→게임 시작 플로우 확인 (근거: DevLog Step 56)
- [ ] "시작 버튼 즉시 패배" 재현 여부 확인 — 재현 시 `[FLOW][GameDataBootstrapper] BeginGame`/
      `SeedStartingInfection` 로그로 `startingCountryId` 전달 확인 (근거: DevLog Step 54)
- [ ] CountrySelect/MainMenu 상세 패널 텍스트 3줄이 안 잘리는지, 리스트 스크롤 확인 (근거: DevLog Step 57)
- [ ] HUD 3줄 레이아웃에서 큰 숫자·위협 단계 텍스트 표시 시 옆 요소 안 밀리는지 확인 (근거: DevLog Step 58)
- [ ] CountrySelect 48개국 국기 전부 표시, 콘솔 경고 없는지 확인 (근거: DevLog Step 59)
- [ ] 신규 허브 16개 위치, 신규 항로 15개 육지 관통 여부, 신규 국가 교통 유닛 동작 확인 (근거: DevLog Step 60)
- [ ] 국가현황 패널 연 상태로 감염 확산 중 프레임 드랍 해소 확인(프로파일러로 틱당 UI 재빌드 없는지)
      확인 (근거: DevLog Step 61)
- [ ] UpgradeTree LOCKED/AVAILABLE/ACTIVE/MAXED 4색 실제 데이터로 전부 나타나는지(특히 MAXED —
      갈래 끝 노드 해금 시) 확인 (근거: DevLog Step 62)
- [ ] UpgradeTree 헤더 2줄(영문 LAB 캡션 + 한글 라벨)이 좁은 폭에서 안 잘리는지 확인 (근거: DevLog Step 62)

## PolygonCollider2D 전환 검증 (근거: DevLog Step 73)

Step 72까지는 BoxCollider2D(사각형)였고, Step 73에서 국가 실루엣 폴리곤 자체를 콜라이더로 쓰도록
바꿨다. Editor 배치 스크립트(`Assets/Editor/CountryShapePhysicsShapeGenerator.cs`) 실행이 선행
조건이므로, 아래 순서대로 확인할 것.

- [x] Unity 에디터 메뉴 `Contagion → Country Shapes → Generate Physics Shapes (48개국)` 실행 —
      콘솔에 "48개 중 48개 신규 활성화"(최초 1회 실행 기준) 로그 확인. **2026-07-10 사용자 실행
      확인 — 정상 완료.**
- [ ] 이어서 `Contagion → Country Shapes → Validate Physics Shapes (48개국)` 실행 — "physics shape
      있음 48개 / 없음 0개" 확인. 0개가 아니면 어떤 국가가 실패했는지 경고 로그로 확인 후 재조사.
- [ ] Play 모드 진입 후 콘솔에 `[CountryView] {id} — InfectionDotPoints.json에 좌표가 없어...`
      경고가 없는지(있으면 그 국가는 폴백 사각형 콜라이더로 동작 중이라는 뜻).
- [ ] 국경을 맞댄 인접국 쌍(예: 한국/일본, 프랑스/독일, 인도/파키스탄)을 실기기에서 경계선
      가까이 탭해 의도한 국가가 선택되는지 — 이번 조사·수정의 원래 신고 대상.
- [ ] 다도서국(인도네시아, 필리핀 등 여러 섬으로 나뉜 나라)에서 각 섬을 탭해도 정상 선택되는지 —
      PolygonCollider2D의 멀티패스(pathCount>1) 처리 확인.
- [ ] 국가 크기가 극단적으로 작은 나라(예: 베네룩스권 인접국)에서 클릭 반경이 너무 좁아 아예
      안 잡히는 회귀가 없는지.
- [ ] `GamePlay.unity`를 에디터에서 열어 48개 국가 GameObject 중 임의로 몇 개를 선택했을 때
      Inspector에 `Polygon Collider 2D`만 보이고 `Box Collider 2D`가 남아있지 않은지 확인 —
      `CountryView.Awake()`가 Play 모드 진입 시 레거시 BoxCollider2D를 자동 제거하므로, Edit 모드
      상태의 씬 파일 자체에는 여전히 예전 BoxCollider2D가 남아있는 게 정상이다(씬 파일은 의도적으로
      건드리지 않음). Play 모드에서 Hierarchy로 확인할 것.
- [ ] 감염 점/DNA 버블/색상 오버레이 등 콜라이더와 무관한 기존 기능이 이번 변경으로 회귀하지
      않았는지(코드상 무관하지만 실측 확인 권장).

## 감염 점 오버레이 확인 (`dotsEnabled=true`, `hotspotsEnabled=false`)

- [ ] 감염률 비례 표시 여부, 국가 색상 오버레이에 가리지 않는지
- [ ] 러시아·캐나다 등 대형 국가에서 점 뭉개짐 여부 → `infectionDotDiameterScale` 조정
- [ ] 점 코어 또렷함 vs 다른 요소 침범 여부 → `coreFraction` 조정
- [ ] (근거: DevLog Step 75) 감염 점 크기 배율을 완전히 제거하고 `diameter`를 그대로 최종 크기로
      쓰도록 단순화한 결과(한국 대비 최대 약 5.66배, 러시아 — Step 74의 sqrt 완화안 13.5배보다 더
      작음)가 실제 렌더링에서도 대형국(중국/미국/캐나다/러시아) 강조 효과가 충분히 느껴지는지 확인.
      오프라인 계산상 48개국 모두 감염률 100% 시 설계 목표(면적의 약 70%)를 만족하도록 복원했지만,
      점 배치가 랜덤 지터라 실제 겹침 정도에 따라 체감 커버리지가 계산값과 다를 수 있다(Step 36 참고).
      너무 밋밋하게 느껴지면 `infectionDotDiameterScale`(현재 GamePlay.unity 오버라이드 1) 상향 검토 —
      단, 국가별 추가 배율을 다시 도입할 경우 이번에 발견된 "커버리지 70% 초과" 문제가 재발하지
      않는지 반드시 재계산할 것.

## 그 외 실플레이 확인

- [ ] DNA 버블이 소형 국가 실루엣 안에서 스폰되는지
- [ ] 교통 노선/carrier 색상 대비가 과하지 않은지
- [ ] `Ship 1.png` 슬라이스/윤곽선 스케일 정상 로드 확인
- [ ] 이동 방향 회전값(비행기 45°/배 180°)·윤곽선 두께 확인
- [ ] 인구 스케일 변경 후 치료제/이벤트 타이밍 체감 확인
- [ ] `carrierChanceScale`(기본 25) 타이밍 적절성 확인

---

*최초 생성: 2026-07-09, CLAUDE.md 문서 분리 규칙 적용으로 "현재 TODO"의 확인성 항목 전체 이동.*
