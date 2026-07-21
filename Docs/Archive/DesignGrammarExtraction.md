# Design Grammar Extraction

> **역할**: Legacy 실측값(`LegacyMasterAnalysis.md`)에서 비례 규칙을 수학적으로 역산한 기록.
> `MASTER_DESIGN.md` 3~7절 수치의 산출 과정이다 — 규칙 자체는 항상 `MASTER_DESIGN.md`를 따른다.

## Base 확정

`Body(--font-size-body) = 16px`를 Base로 설정. 결정적 근거: **참조 캔버스(480×1040) 자체가
Base의 정수배**(폭 = Base×30, 높이 = Base×65) — 우연이 아니라 Legacy 디자인이 실제로 16px
격자 위에서 설계됐다는 증거.

원자 단위는 **Base/8 = 2px** — 폰트의 13px(xs) 단 하나를 제외한 모든 실측값이 이 배수.

## Typography Scale (Base=16 배수)

| 토큰 | px | Base 대비 |
|---|---|---|
| xs | 13 | ×0.8125(격자 유일 예외) |
| sm | 14 | ×0.875(7/8) |
| body | 16 | ×1.0 |
| md | 18 | ×1.125(9/8) |
| lg | 20 | ×1.25(5/4) |
| xl | 24 | ×1.5(3/2) |
| xxl | 26 | ×1.625(13/8) |
| display | 32 | ×2.0 |
| hero | 40 | ×2.5(5/2) |

## Spacing Scale (Base=16 배수, 0.25 등차)

`xs=×0.25(4) sm=×0.5(8) md=×0.75(12) lg=×1.0(16, Base와 수렴) xl=×1.25(20) xxl=×1.5(24)`

## Component Scale

| 컴포넌트 | px | Base 대비 |
|---|---|---|
| Button Height(주력) | 48 | ×3.0 |
| Graph Height | 34 | ×2.125(17/8) |
| Graph Width | 104 | ×6.5 |
| Card Border | 2 | ×0.125(1/8) |
| Card Radius(md) | 6 | ×0.375(3/8) |
| Flag Icon | 36×26 | ×2.25 / ×1.625 |

## Border Scale

`기본 2px(×0.125) / 선택 강조 3px(×0.1875)` — 2단계 체계.

## Panel Composition

`CountryStatusPanel bottom inset 12.5% = 정확히 1/8` — 화면(65 Base) 격자와 맞물려 우연히
깔끔한 분수로 떨어짐. 단, 이 규칙이 다른 Panel(RankingPanel)에도 적용되는지는
`GrammarValidation.md`에서 별도 검증 — **검증 결과 적용되지 않음(D등급)**, 즉 Panel Composition은
공통 Base 배수 법칙이 아니라 화면별 독립 결정이라는 결론으로 이어짐(`MASTER_DESIGN.md` 6절 반영).

## Information Density

Content Area(지도) : Chrome(News+Bottom Bar) = 71.9% : 28.1% ≈ 2.56 : 1

## Vertical Rhythm

명시적 line-height 토큰 없음 — margin이 그 역할을 대신함. Spacing Scale(0.25 스텝, 4px 격자)보다
더 촘촘한 **2px 원자 격자**(하드코딩된 6/10/14px 등)가 실제 사용 사례에서 다수 발견됨 — 공식
토큰은 4px 격자만 명명했지만, 실제 사용된 격자는 그보다 두 배 촘촘한 2px 격자였다는 뜻.
