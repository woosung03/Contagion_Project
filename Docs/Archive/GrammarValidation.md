# Grammar Validation

> **역할**: `DesignGrammarExtraction.md`에서 도출한 비율 규칙이 Legacy UI를 실제로 예측할 수
> 있는지 검증한 기록(맹검 예측 → 실측 대조). `MASTER_DESIGN.md`가 채택한 규칙과 기각한 규칙의
> 근거다 — 규칙 자체는 항상 `MASTER_DESIGN.md`를 따른다.

## 검증 방법

Grammar(Base=16 + 비율)만으로 값을 먼저 계산(맹검 예측) → git에서 Legacy 실제값 재확인 → 오차
기준으로 등급 분류(A=오차 0% / B=±2px 또는 ±2% 이내 / C=격자는 부합하나 별도 규칙 / D=격자
자체를 벗어난 예외).

## 핵심 검증 결과

**맹검 예측이 적중한 항목(진짜 검증력 있는 A등급)**:
- `radius-sm`(예측 3px = 실제 3px), `radius-lg`(예측 8px = 실제 8px) — 사전에 값을 보지 않고
  예측했는데 정확히 일치.
- CountryPopup `popup-title`(Title=24px), `popup-row`(Subtitle=18px) — 정확히 일치.
- 주력 버튼(48px) — CountryStatusPanel/RankingPanel/UpgradeTree/MainMenu 5곳에서 전부 일치.

**Grammar를 벗어난 항목(C/D등급)**:
- **Button 예외 2건**: `ending-button`(56px = Base×3.5, 확정 액션 전용 별도 배수),
  `popup-close`(36px = Base×2.25, 아이콘 버튼 전용 별도 배수). 둘 다 격자 자체는 지키므로
  "역할별 하위 Tier"로 편입 가능(C등급) → `MASTER_DESIGN.md` 7절 Button Hierarchy 3-Tier
  체계의 근거가 됨.
- **Typography 홀수px 잔재(D등급, 5건)**: `mainmenu-subtitle`(19px), `pathogen-card__name`
  (21px), `pathogen-card__stats`(17px), `detail-panel__desc`(17px, MainMenu·UpgradeTree
  공통), `tree-category-label`(19px). 전부 2px 원자 격자조차 벗어난 완전한 예외 —
  `MASTER_DESIGN.md` 9절 Legacy Exclusions의 근거.
- **Panel side inset(D등급)**: CountryStatusPanel(5%)과 RankingPanel(9%) 사이에 공통 배수
  관계가 없음. RankingPanel은 bottom inset 자체가 없음(콘텐츠 기반) — "Panel Composition은
  공통 Grammar가 아니다"라는 결론의 직접 근거.

## Grammar 재현율

| 카테고리 | 재현율 |
|---|---:|
| Border/Radius | 100% |
| Button(주력 규칙) | 83% |
| Graph | 100%(정의 항목, 검증력 낮음) |
| Spacing(격자 기준) | 50%(격자는 맞으나 명명 토큰 밖 하드코딩 다수) |
| Typography(정의 항목 제외) | 43% |
| Panel Composition | 50%(개별 화면마다 다름) |
| **Overall** | **약 68%** |

## 최종 결론(당시 채택)

**"Grammar 구조는 맞지만 Base 또는 Scale 수정 필요"** — Base=16과 2px 원자 격자라는 뼈대는
채택하되, ① Typography는 토큰화된 값만 대상으로 스코프를 좁히고, ② Button은 역할별 3단계
하위 배수를 추가하고, ③ Panel Composition은 Grammar 대상에서 제외한다는 결론. 이 결론이 그대로
`MASTER_DESIGN.md`의 최종 구조(Typography 6+2 Role, Button 3-Tier, Panel 독립 설계 원칙)로
반영됐다.
