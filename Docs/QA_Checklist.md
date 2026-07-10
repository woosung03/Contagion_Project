# Contagion Project — QA 체크리스트

"확인해볼 것" 형태의 테스트/검증 항목을 모아두는 문서. CLAUDE.md에는 이런 항목을 두지 않고 전부
여기로 옮긴다. 항목이 확인 완료되면 체크(`[x]`) 후 근거(확인 날짜/방법)를 한 줄 남기고, 완전히
불필요해지면 삭제한다. 각 항목의 배경(왜 이 확인이 필요한지)은 `Docs/DevLog.md`의 해당 Step 참고.

---

## 세션 시작 시 확인 (미해결)

- [ ] GamePlay.unity 정상 로드 + MainMenu→CountrySelect→게임 시작 플로우 확인 (근거: DevLog Step 56)
- [ ] "시작 버튼 즉시 패배" 재현 여부 확인 — 재현 시 `[FLOW][GameDataBootstrapper] BeginGame`/
      `SeedStartingInfection` 로그로 `startingCountryId` 전달 확인 (근거: DevLog Step 54)
- [ ] CountrySelect/MainMenu 상세 패널 텍스트 3줄이 안 잘리는지, 리스트 스크롤 확인 (근거: DevLog Step 57)
- [ ] HUD 3줄 레이아웃에서 큰 숫자·위협 단계 텍스트 표시 시 옆 요소 안 밀리는지 확인 (근거: DevLog Step 58)
- [ ] CountrySelect 48개국 국기 전부 표시, 콘솔 경고 없는지 확인 (근거: DevLog Step 59)
- [ ] 신규 허브 16개 위치, 신규 항로 15개 육지 관통 여부, 신규 국가 교통 유닛 동작 확인 (근거: DevLog Step 60)
- [ ] 국가현황 패널 연 상태로 감염 확산 중 프레임 드랍 해소 확인(프로파일러로 틱당 UI 재빌드 없는지),
      Country Dock 신규 행(상태/항공·항구) 줄바꿈·색상 충돌 없는지 확인 (근거: DevLog Step 61)
- [ ] UpgradeTree 노드 4줄(code/이름/상태/비용) 78px 높이 안에서 안 잘리는지, 특히 두 단어 한글
      표시명("다발성 장기부전 II") 줄바꿈 시 확인 (근거: DevLog Step 62)
- [ ] UpgradeTree LOCKED/AVAILABLE/ACTIVE/MAXED 4색 실제 데이터로 전부 나타나는지(특히 MAXED —
      갈래 끝 노드 해금 시) 확인 (근거: DevLog Step 62)
- [ ] UpgradeTree detail-panel 코너컷이 모서리에 올바르게 붙는지(position:relative 적용 확인),
      연결선 꺾은선/포트 마커가 3개 카테고리 전부에서 겹침 없이 그려지는지 확인 (근거: DevLog Step 62)
- [ ] UpgradeTree 헤더 2줄(영문 LAB 캡션 + 한글 라벨)이 좁은 폭에서 안 잘리는지 확인 (근거: DevLog Step 62)
- [ ] Country Dock이 국가 탭 시 정상적으로 데이터로 채워지는지 확인 — Step 70/72/73을 거쳐
      콜라이더 관련 원인은 전부 특정·수정 완료(현재는 PolygonCollider2D 기반, 아래 "PolygonCollider2D
      전환 검증" 섹션 참고). `_shownCountryId`가 클릭한 국가로 채워지는지가 성공 기준
      (근거: DevLog Step 70/72/73)
- [ ] 위 항목 확인 후, 국가 탭 시 Country Dock과 CountryPopup(모달)이 동시에 뜨는지 확인 —
      클릭이 막혀있던 동안 가려져 있던 증상일 수 있음. 동시에 뜨면 어느 쪽을 남길지 결정 필요
      (근거: DevLog Step 67, CLAUDE.md TODO)

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
