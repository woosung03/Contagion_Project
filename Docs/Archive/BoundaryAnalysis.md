# Boundary Analysis

> **역할**: Master Commit(`cfcc0e4`)의 8개 화면 중 어디가 Master Design을 대표하고 어디가 Legacy
> 잔재인지 화면 단위로 분류한 기록. `MASTER_DESIGN.md` 9절 Legacy Exclusions의 직접 근거다 —
> 규칙 자체는 항상 `MASTER_DESIGN.md`를 따른다.

## 화면별 평가

| 화면 | Grammar 순도 | 등급 | 비고 |
|---|---:|---|---|
| CountryStatusPanel | 95% | **A** | Panel Composition(1/8 bottom inset) 규칙의 원천 |
| CountryPopup | 95% | **A** | Typography 전부 토큰, radius 맹검 예측 2건 적중 |
| Hud | 95% | **A** | Typography 전부 토큰 참조, Graph/Button 정의 원천 |
| RankingPanel | 90% | **A** | Typography/Spacing/Button/Radius 전부 클린 |
| EndingScreen | 90% | **B** | Typography/Spacing 100% 토큰 참조, 버튼만 별도 배수(×3.5) |
| UpgradeTree | 78% | **B** | 구조는 클린, "설명 텍스트" 계열에 홀수px 잔재 2건 |
| CountrySelect | 70% | **C** | MainMenu.uss 공유로 인한 잔재(subtitle/detail-panel) |
| MainMenu | 62% | **C** | 카드 이름/스탯/부제 등 6개 중 4개가 D등급 하드코딩 |

D등급(화면 전체가 Legacy 잔재)으로 분류된 화면은 없음.

## Legacy 잔재 목록

**MainMenu**: `mainmenu-subtitle`(19px), `pathogen-card__name`(21px), `pathogen-card__stats`
(17px), `detail-panel__title`(22px), `detail-panel__desc`(17px), `mainmenu-back`(46px, 주력
버튼과 2px 차이). 카드/디테일 패널이라는 "리스트 프로토타입" 계열에 집중돼 있고, 같은 파일의
`mainmenu-title`/`country-row__name`/`mainmenu-next`는 전부 토큰을 정확히 참조 — 한 파일
안에 신규 설계 레이어와 구 하드코딩 레이어가 뚜렷이 공존.

**CountrySelect**: 위 MainMenu 잔재 중 `mainmenu-subtitle`/`detail-panel__title`/
`detail-panel__desc`/`mainmenu-back`을 스타일시트 공유로 그대로 물려받음. `country-row`
자체는 잔재 없음.

**UpgradeTree**: `tree-category-label`(19px, MainMenu subtitle과 동일 값), `detail-panel__desc`
(17px, MainMenu와 동일 클래스명·동일 문제). "설명(description) 텍스트" 역할이 프로젝트
전체에서 한 번도 제대로 토큰화된 적이 없었다는 방증.

## Master Design 구성 요소(당시 판정)

- **Master Design**: Hud, CountryPopup, CountryStatusPanel, RankingPanel
- **Master 기반(정리 후 편입 가능)**: EndingScreen, UpgradeTree
- **Legacy(복원 기준 사용 금지)**: MainMenu, CountrySelect

이 판정이 그대로 `MASTER_DESIGN.md` 9절 Legacy Exclusions 표로 반영됐다.
