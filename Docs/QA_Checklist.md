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
- [ ] Country Dock이 국가 탭 시 정상적으로 데이터로 채워지는지 확인 — 원인은 48개국
      CountryView의 BoxCollider2D 크기가 0.16×0.16으로 고정돼 클릭 자체가 씹히던 것으로 특정,
      `ApplyCountryShape()`에서 스프라이트 로드 시 콜라이더 size/offset을 재계산하도록 수정
      완료. 콘솔에서 `[CountryView] OnMouseUpAsButton` → `[WorldMap] HandleCountryClicked` →
      `[CountryDockController] HandleCountryClicked 진입`까지 로그 체인이 찍히고
      `_shownCountryId`가 클릭한 국가로 채워지는지가 성공 기준 (근거: DevLog Step 70)
- [ ] 위 항목 확인 후, 국가 탭 시 Country Dock과 CountryPopup(모달)이 동시에 뜨는지 확인 —
      클릭이 막혀있던 동안 가려져 있던 증상일 수 있음. 동시에 뜨면 어느 쪽을 남길지 결정 필요
      (근거: DevLog Step 67, CLAUDE.md TODO)

## 감염 점 오버레이 확인 (`dotsEnabled=true`, `hotspotsEnabled=false`)

- [ ] 감염률 비례 표시 여부, 국가 색상 오버레이에 가리지 않는지
- [ ] 러시아·캐나다 등 대형 국가에서 점 뭉개짐 여부 → `infectionDotDiameterScale` 조정
- [ ] 점 코어 또렷함 vs 다른 요소 침범 여부 → `coreFraction` 조정

## 그 외 실플레이 확인

- [ ] DNA 버블이 소형 국가 실루엣 안에서 스폰되는지
- [ ] 교통 노선/carrier 색상 대비가 과하지 않은지
- [ ] `Ship 1.png` 슬라이스/윤곽선 스케일 정상 로드 확인
- [ ] 이동 방향 회전값(비행기 45°/배 180°)·윤곽선 두께 확인
- [ ] 인구 스케일 변경 후 치료제/이벤트 타이밍 체감 확인
- [ ] `carrierChanceScale`(기본 25) 타이밍 적절성 확인

---

*최초 생성: 2026-07-09, CLAUDE.md 문서 분리 규칙 적용으로 "현재 TODO"의 확인성 항목 전체 이동.*
