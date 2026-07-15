# Button System Audit — Contagion Project Tactical UI

> 조사 전용 문서. 코드/USS/UXML 수정 없음. `Docs/DESIGN.md` "Button System"절(Primary/
> Secondary/Danger 3종 + Action Color Amber 예약 토큰)을 기준으로 실제 코드베이스를
> 대조했다.

**조사 범위**: `Hud.uxml`/`MainMenu.uxml`/`CountrySelect.uxml`/`CountryStatusPanel.uxml`/
`CountryPopup.uxml`/`ResearchPopup.uxml`/`UpgradeTree.uxml`/`RankingPanel.uxml`/
`EndingScreen.uxml` 9개 화면의 `<ui:Button>` 전수 조사 + 대응하는 `.uss`(`Theme.uss`/
`Tactical.uss` 포함 11개) 스타일 정의 대조 + `UpgradeTreeView.cs`에서 `buy-button`의
런타임 배선 상태 확인. 런타임에 `new Button(...)`으로 생성되는 코드는 코드베이스 전체에
없음을 확인했다 — `pathogen-card`/`country-row`/`status-row`/`research-row`/`branch-row`
등 리스트 행은 전부 `VisualElement` + 클릭 매니퓰레이터이며 `Button` 컴포넌트가 아니다.
DESIGN.md도 이들을 "브리핑룸 톤 메뉴류"로 명시적으로 예외 처리하고 있으므로, 이번 Button
System 감사 대상에서 제외한다(별도 트랙).

---

## 1. 버튼 인벤토리 (전수 21개)

`<ui:Button>` 태그로 존재하는 인스턴스만 집계. 화면 하나에 같은 클래스가 2회 쓰이는
경우(`mainmenu-next`)도 인스턴스별로 별도 행 처리했다.

| # | 화면 | name | 표시 텍스트 | 클래스 |
|---|---|---|---|---|
| 1 | HUD | `tab-upgrade` | 업그레이드 | `tab-button` |
| 2 | HUD | `tab-country-status` | 국가현황 | `tab-button` |
| 3 | HUD | `ranking-button` | 랭킹 | `tab-button tab-button--secondary` |
| 4 | MainMenu | `next-button` | 다음 | `mainmenu-next` |
| 5 | CountrySelect | `back-button` | 뒤로 | `mainmenu-back` |
| 6 | CountrySelect | `start-button` | 시작 | `mainmenu-next` |
| 7 | CountryStatusPanel | `close-button` | ✕ | `status-close-btn` |
| 8 | CountryPopup | `modal-close` | ✕ | `popup-close` |
| 9 | ResearchPopup | `modal-close` | ✕ | `popup-close` |
| 10 | ResearchPopup | `research-popup-confirm-button` | 연구 시작 | `popup-footer-button popup-footer-button--confirm` |
| 11 | ResearchPopup | `research-popup-cancel-button` | 취소 | `popup-footer-button` |
| 12 | UpgradeTree | `tab-transmission-button` | 전파 | `tab-button`(로컬, HUD와 동명이의) |
| 13 | UpgradeTree | `tab-symptom-button` | 증상 | `tab-button` |
| 14 | UpgradeTree | `tab-ability-button` | 적응 | `tab-button` |
| 15 | UpgradeTree | `ad-bonus-button` | 광고 보고 DNA +10 | `upgrade-header__ad-bonus` |
| 16 | UpgradeTree | `close-button` | 닫기 | `upgrade-header__close` |
| 17 | UpgradeTree | `buy-button` | 연구 시작 | `detail-panel__buy` (레거시, 아래 4절 참고) |
| 18 | RankingPanel | `open-leaderboard-button` | 토스 리더보드 열기 | `popup-footer-button popup-footer-button--confirm` |
| 19 | RankingPanel | `close-button` | 닫기 | `popup-footer-button` |
| 20 | EndingScreen | `revive-button` | 광고 보고 부활 | `ending-button ending-button--revive` |
| 21 | EndingScreen | `restart-button` | 다시 시작 | `ending-button` |

주의: `#1/#2/#12~14`처럼 이름이 같은 `tab-button` 클래스가 **서로 다른 파일(`Hud.uss`
vs `UpgradeTree.uss`)에 독립적으로 정의**되어 있다. 같은 UXML 문서 안에서만 스타일시트가
로드되므로 실제 충돌은 없지만, 두 `tab-button`이 완전히 다른 시각 언어(HUD는 박스+테두리,
UpgradeTree는 밑줄 탭)를 쓰고 있어 "탭 버튼"이라는 이름 하나에 두 개의 컴포넌트 계약이
공존한다.

---

## 2. 버튼 역할 분류 (Primary / Secondary / Danger)

### Primary (8개) — 확정/진행 액션

| name | 화면 | 근거 |
|---|---|---|
| `tab-upgrade` | HUD | 핵심 게임플레이 진입점(Hud.uss 주석: "주 액션") |
| `tab-country-status` | HUD | 핵심 게임플레이 진입점(위와 동일 근거) |
| `next-button` | MainMenu | 다음 단계(병원체 확정) 진행 |
| `start-button` | CountrySelect | 게임 시작 확정 |
| `research-popup-confirm-button` | ResearchPopup | 연구 확정(`--confirm` 변형) |
| `open-leaderboard-button` | RankingPanel | 화면의 유일한 CTA(`--confirm` 변형) |
| `ad-bonus-button` | UpgradeTree | 확정 액션이나 프리미엄(광고) 하위 유형 |
| `revive-button` | EndingScreen | 확정 액션이나 프리미엄(광고) 하위 유형 |

### Secondary (12개) — 중립/취소/닫기/뒤로가기

| name | 화면 | 비고 |
|---|---|---|
| `ranking-button` | HUD | 화면 comment상 "보조 액션"으로 이미 의도됨 |
| `back-button` | CountrySelect | 실기기 검증 완료 |
| `close-button` | CountryStatusPanel | `popup-close`와 동일 톤 |
| `modal-close`(CountryPopup) | CountryPopup | 실기기 검증 완료 |
| `modal-close`(ResearchPopup) | ResearchPopup | 실기기 검증 완료 |
| `research-popup-cancel-button` | ResearchPopup | |
| `close-button` | RankingPanel | |
| `restart-button` | EndingScreen | |
| `close-button` | UpgradeTree | **미검증 — 3절 참고** |
| `tab-transmission/symptom/ability-button` (3개) | UpgradeTree | 4절에서 별도 아키타입으로 재분류 제안 |

### Danger (0개) — 코드베이스에 인스턴스 없음

DESIGN.md가 이미 "예약 정의"로 명시한 대로, 9개 화면 어디에도 파괴적 확인 액션이 없다.
가장 유력한 향후 후보는 HUD의 "게임 포기"(진행 중 게임 종료) 또는 향후 설정 화면의
"저장 데이터 삭제"이지만 현재 두 기능 자체가 미구현이므로 버튼도 없다. **신규 버튼
없이는 Danger 스타일을 적용할 대상이 없다** — 이번 조사에서 새로 만들 필요는 없음.

---

## 3. 현재 문제점

### 3-1. Unity 기본 스타일 잔존 — 7개 인스턴스 (가장 심각)

CLAUDE.md 현재 상태에 "Unity 기본 회색 버튼 제거 완료"라고 기록되어 있으나, 실제로는
**HUD와 UpgradeTree에 미수정 잔존 버튼이 남아 있다.** DESIGN.md 자체가 "background-color/
border/border-radius를 반드시 명시적으로 지정"을 요구하는데, 아래는 그 셋 중 하나 이상이
빠진 경우다.

| name | 클래스 | 빠진 속성 | 심각도 |
|---|---|---|---|
| `close-button`(UpgradeTree) | `upgrade-header__close` | background-color, border, border-radius **전부** | 최고 — `width:80px` 한 줄뿐, Unity 기본 회색 라운드 버튼 그대로 노출 |
| `buy-button` | `detail-panel__buy` | background-color, border, border-radius **전부** | 최고 — 4절 참고, 레거시 이슈와 중복 |
| `tab-upgrade` | `tab-button`(Hud.uss) | background-color, border-radius | 높음 — HUD 최우선 진입점 2개 중 하나 |
| `tab-country-status` | `tab-button`(Hud.uss) | background-color, border-radius | 높음 — 위와 동일 |
| `ranking-button` | `tab-button--secondary` | border-radius | 중간 — 배경/테두리는 있으나 모서리만 둥긂 |
| `next-button` | `mainmenu-next` | border-width/color, border-radius | 중간 — 배경은 브랜드그린이나 테두리·모서리 기본값 |
| `start-button` | `mainmenu-next`(재사용) | 위와 동일 | 중간 — 같은 클래스라 동시 발생 |
| `ad-bonus-button` | `upgrade-header__ad-bonus` | border-width/color, border-radius | 중간 — 배경은 프리미엄퍼플이나 테두리·모서리 기본값 |

즉 "Stroke System 적용 완료 / Unity 기본 회색 버튼 제거 완료"라는 현재 상태 기록은
**CountryPopup ✕ / ResearchPopup ✕ / CountrySelect 뒤로가기 등 실기기 테스트를 진행한
3곳에 한정된 사실**이고, HUD 액션스트립(가장 자주 보이는 화면)과 UpgradeTree 헤더는
검증에서 빠진 채 방치되어 있다. 특히 HUD의 `tab-upgrade`/`tab-country-status`는 이
게임에서 가장 많이 클릭되는 두 버튼일 가능성이 높아 우선순위가 가장 높다.

### 3-2. Secondary 토큰 분기 — `--color-button-secondary` vs `--color-bg-panel`

DESIGN.md의 Secondary 정의는 `background-color: --color-bg-panel` + `border: 1px
accent-glow`다. 그러나 `ranking-button`(`tab-button--secondary`)은 별도 토큰
`--color-button-secondary`(`rgb(70,70,90)`, Theme.uss 60행)와 중립 `--color-border`를
쓴다 — 이름은 "Secondary"인데 실제 색 조합이 공식 Secondary 스펙과 다른 **제3의 변형**이
하나 더 존재하는 셈이다. 둘 다 "Secondary"라는 이름을 쓰면 앞으로 새 버튼을 추가할 때
어느 쪽을 참조해야 할지 기준이 모호해진다.

### 3-3. `tab-button`이라는 이름 아래 서로 다른 3개 컴포넌트가 공존

- HUD `tab-button` — 박스형(테두리+배경), 화면 전환용 대형 네비게이션.
- HUD `tab-button--secondary` — 위 변형, 중립색.
- UpgradeTree `tab-button`/`tab-button--active` — 밑줄형(배경 투명, 하단 보더만),
  카테고리 필터용 소형 탭.

세 가지 모두 "탭"이라는 상호작용 의미는 같지만 시각 언어가 완전히 다르다. Primary/
Secondary/Danger 3종 분류에 억지로 끼워 맞추면 왜곡되므로, 4절에서 별도 처리를 제안한다.

### 3-4. `--confirm` 수정자가 있는 버튼 중 의미가 다른 두 그룹이 섞여 있음

`popup-footer-button--confirm`은 현재 "연구 시작"(상태를 확정 소비하는 액션)과 "토스
리더보드 열기"(외부 화면으로 이동하는 액션)에 동시에 쓰인다. 시각적으로는 문제없이
동일한 CTA 톤을 공유하지만, 전자는 게임 자원(DNA)을 소비하는 반면 후자는 소비가 없는
순수 네비게이션이다 — Amber 후보 논의(5절)에서 이 구분이 의미를 가진다.

---

## 4. `buy-button`(UpgradeTree) — 레거시 상태 별도 확인 필요

`UpgradeTree.uxml` 53~59행 주석은 "`UpgradeTreeView.SelectDummyItem()`이 여전히
`_buyButton.SetEnabled()`/`text`를 무조건 호출하기 때문에 지금 지우면 NRE가 난다"고
설명하지만, 실제 `UpgradeTreeView.cs`를 확인한 결과 `_buyButton`은 323행에서 **선언·조회만
되고 이후 `SetEnabled`/`text` 등 어떤 곳에서도 참조되지 않는다.** 즉 주석이 가리키는
위험(NRE)은 이미 코드가 바뀌면서 해소된 것으로 보이며, `buy-button`은 현재:

1. 화면에 항상 렌더링되어 있고(`display:none` 처리 없음),
2. 스타일이 전무해 Unity 기본 회색 버튼으로 보이고,
3. 클릭해도 아무 로직도 실행되지 않는(죽은) 상태다.

이 버튼은 "스타일만 고쳐서 될" 문제가 아니라 **주석의 전제(코드 참조 여부)부터 재확인 후
제거 여부를 결정해야 하는 항목**이다. CLAUDE.md TODO의 "커밋 9(구 코드 정리, buy-button
UXML 제거 등)"가 이미 이 제거를 계획하고 있으므로, Button System 색상 통일 작업과
별개로 먼저 커밋 9 범위에서 처리하는 편이 낫다 — 스타일을 입혀봐야 곧 지워질 코드에
공수를 쓰게 된다.

---

## 5. Action Color(Amber) 적용 후보 조사 — 적용 금지, 후보만 제시

DESIGN.md는 Amber를 "향후 Primary Button의 CTA 강조색으로 `--color-accent-glow` 대신(또는
함께) 도입할 후보"로 예약해 두었다. 현재 모든 Primary 버튼은 구조용 발광색(`accent-glow`)
또는 브랜드색(DNA그린/프리미엄퍼플)을 CTA 배경으로 쓰고 있어, "패널 테두리 색"과 "버튼
CTA 색"이 시각적으로 구분되지 않는다는 문제가 있다. Amber 도입 시 아래를 우선 후보로
제안한다(코드 변경 없이 제안만):

| 후보 | 현재 색 | Amber 전환 시 기대 효과 |
|---|---|---|
| `research-popup-confirm-button`("연구 시작") | `accent-glow-soft` | 패널 테두리(accent-glow)와 CTA 배경이 같은 색 축이라 버튼이 "테두리의 연장"처럼 보임 — Amber로 분리하면 클릭 대상이 더 도드라짐 |
| `next-button`/`start-button`("다음"/"시작") | `brand-dna`(그린) | DNA그린은 "자원" 의미로 이미 고정 축인데 "진행 CTA"에도 재사용되고 있어 두 의미가 겹침(DESIGN.md 색상 축 분리 원칙과 긴장) — Amber는 이 CTA 의미만 전담 가능 |
| `open-leaderboard-button`("토스 리더보드 열기") | `accent-glow-soft` | 3-4절에서 지적한 "외부 이동" 성격의 CTA — 자원 소비형 확정 액션(연구 시작)과 시각적으로 분리하고 싶다면 Amber가 이 그룹만 담당하는 방법이 있음 |

반대로 Amber를 적용하지 **말아야** 할 대상: `ad-bonus-button`/`revive-button`(이미
프리미엄퍼플이 "광고 보상" 의미로 고정 배정되어 있어 Amber로 바꾸면 오히려 축이 겹침).
Danger 후보가 아직 없으므로 Amber-vs-Danger 혼동 리스크는 현재 없음.

---

## 6. 수정 대상 목록 (우선순위 순)

1. **`upgrade-header__close`**(UpgradeTree 닫기) — 스타일 전무, 최우선.
2. **`buy-button`**(UpgradeTree, `detail-panel__buy`) — 스타일보다 먼저 참조 여부 재확인 → 제거 or 스타일링 결정.
3. **HUD `.tab-button`**(`tab-upgrade`/`tab-country-status`) — background-color/border-radius 추가.
4. **HUD `.tab-button--secondary`**(`ranking-button`) — border-radius 추가 + `--color-button-secondary` 토큰을 공식 Secondary 스펙(`--color-bg-panel`)과 통일할지 결정.
5. **`.mainmenu-next`**(MainMenu `next-button` + CountrySelect `start-button` 동시 영향) — border-width/color, border-radius 추가.
6. **`.upgrade-header__ad-bonus`**(`ad-bonus-button`) — border-width/color, border-radius 추가.
7. (선택) UpgradeTree 로컬 `.tab-button`을 HUD `.tab-button`과 다른 이름(`.category-tab` 등)으로 리네이밍 — 3-3절의 네이밍 충돌 해소, 시각 변경 없음.

## 7. 실제 수정 계획 (제안, 미실행)

- **1단계 — 최우선 버그 수정(시각 변경 최소)**: 위 우선순위 1~6 항목에 대해 이미 검증된
  Secondary/Primary 톤(`border: 1px accent-glow`, `border-radius: 0`, 배경은 기존
  배경색 유지)을 그대로 얹는다. 배경색을 새로 정하지 않고 테두리/반경만 채우는 것이라
  기존 색 의미(그린/퍼플/중립)를 건드리지 않는다. `buy-button`은 이 단계에서 제외하고
  별도 트랙(아래)으로 뺀다.
- **2단계 — `buy-button` 처리**: `UpgradeTreeView.cs`에서 `_buyButton` 참조 여부를
  다시 확인해 주석을 최신화하고, CLAUDE.md TODO의 커밋 9(구 코드 정리) 범위에서
  제거하거나 `display: none`으로 우선 숨긴다.
- **3단계 — Secondary 토큰 통합 여부 결정**: `--color-button-secondary`를 없애고
  `tab-button--secondary`도 공식 Secondary 스펙(`--color-bg-panel`)으로 흡수할지,
  아니면 "HUD 액션스트립 전용 3차 변형"으로 DESIGN.md에 정식 등재할지 결정 — 결정
  전까지는 시각 변경하지 않는다.
- **4단계 — 탭 아키타입 분리**: UpgradeTree 로컬 `.tab-button`을 리네이밍해 HUD
  `.tab-button`과의 네이밍 충돌만 해소(순수 리팩터링, 시각 변경 없음).
- **5단계(별도 작업, 이번 범위 아님) — Amber 도입**: 5절의 3개 후보를 대상으로
  Theme.uss에 `--color-action-amber` 토큰을 추가하고 DESIGN.md Status Semantics 표에
  "Action" 축을 정식 추가한 뒤에만 적용. 이번 작업에서는 절대 적용하지 않는다.

---

## 결론 — 표준화 지정

| 분류 | 대상 버튼 |
|---|---|
| **Primary** | `tab-upgrade`, `tab-country-status`(HUD) · `next-button`(MainMenu) · `start-button`(CountrySelect) · `research-popup-confirm-button`(ResearchPopup) · `open-leaderboard-button`(RankingPanel) · `ad-bonus-button`(UpgradeTree, 프리미엄 변형) · `revive-button`(EndingScreen, 프리미엄 변형) |
| **Secondary** | `ranking-button`(HUD) · `back-button`(CountrySelect) · `close-button`(CountryStatusPanel) · `modal-close`(CountryPopup) · `modal-close`(ResearchPopup) · `research-popup-cancel-button`(ResearchPopup) · `close-button`(RankingPanel) · `restart-button`(EndingScreen) · `close-button`(UpgradeTree, 스타일 보수 필요) |
| **Danger** | 없음(예약 상태 유지, 신규 기능 생기기 전까지 지정 대상 없음) |
| **미분류(별도 아키타입 제안)** | `tab-transmission/symptom/ability-button`(UpgradeTree 카테고리 필터 탭) — Primary/Secondary가 아니라 "Tab/Segment" 4번째 컴포넌트로 DESIGN.md에 별도 등재 권장 |
| **레거시/제거 검토** | `buy-button`(UpgradeTree) — 스타일링 대상에서 제외, 참조 여부 재확인 후 제거 |

가장 시급한 실무 조치는 **HUD의 `tab-upgrade`/`tab-country-status`**와 **UpgradeTree의
`close-button`**이다 — 셋 다 Unity 기본 회색 버튼이 그대로 남아 있고, 앞의 둘은 게임에서
가장 자주 눌리는 버튼이라 "Stroke System 적용 완료"라는 현재 기록과 실제 화면 사이
괴리가 가장 크게 드러나는 지점이다.
