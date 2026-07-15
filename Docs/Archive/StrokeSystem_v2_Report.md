---
title: Tactical Stroke System v2 — 설계 보고서
status: 제안(코드/USS/DESIGN.md 미반영)
scope: Stroke(테두리 두께) 시스템만. Color System은 이번 범위 아님.
---

> 이 문서는 **보고서**다. `DESIGN.md`, `Theme.uss`, 화면 `.uss`, 코드는 이 보고서 기준으로
> 아직 수정되지 않았다. 9절의 "DESIGN.md 수정안"은 승인 후 붙여넣을 초안이며, 이 보고서
> 자체가 최종 문서가 아니다.

## 0. 결론 먼저

가설("민트색 선이 얇아서 안 보인다")은 틀렸다. `PanelSettings.asset` 실측 결과 스케일
팩터가 정확히 **3.0배**(아래 1절)이므로 1px 테두리는 실기기에서 이미 3px로 렌더링된다 —
"안 보일 정도로 얇음"은 애초에 발생할 수 없는 현상이었다. 실제 문제는 코드베이스 실측
결과 **동일한 두께 값(2px, 3px)이 서로 다른 3~4개의 의미로 중복 배정**되어 있다는 것,
그리고 **코너컷(2px)과 패널 테두리(1px)의 관계가 설계되지 않고 우연히 결정**되어 있다는
것이다. 두 문제 모두 "두께=의미" 규칙이 존재하지 않아서 생긴 결과이므로, 제안된 Stroke
System v2(Hairline/Active/Selected/Accent Bar + Corner Cut 파생 규칙)는 타당하다 —
다만 사용자가 제시한 원안 중 "Corner Cut = 2px"는 독립 토큰이 아니라 **Hairline의 2배
파생값**으로 재정의해야 근거가 선다. 최종 철학 문장은 12절.

---

## 1. Border Audit 재검토 — 렌더링 배율 실측

`Assets/UI Toolkit/PanelSettings.asset`을 직접 확인했다.

| 필드 | 값 |
|---|---|
| `m_ScaleMode` | `2` (Scale With Screen Size) |
| `m_ReferenceResolution` | `480 × 1040` |
| `m_ScreenMatchMode` / `m_Match` | `0` (Match Width) |
| 타겟 기기(CLAUDE.md 명시) | 갤럭시 S25 울트라, `1440 × 3120` |

`1440 ÷ 480 = 3.0` — 폭 기준으로 정확히 3배 스케일이다(세로 고정 화면이라 Match Width가
전 화면에 그대로 적용된다). 즉 이 프로젝트에서 **USS 1px = 실기기 3px**는 근사치가 아니라
PanelSettings 설정값으로부터 나오는 정확한 배율이다. 사용자가 제시한 실측 결론("1px가
약 3px 수준으로 렌더링된다")과 일치하며, 근거를 특정 파일·필드로 확인했다.

파생 배율표(설계 시 이 표를 기준으로 두께 차이를 판단할 것):

| USS 값 | 실기기 렌더링 | 인접 값과의 실기기 차이 |
|---|---|---|
| 1px | 3px | — |
| 2px | 6px | 1px 대비 +3px |
| 3px | 9px | 2px 대비 +3px |
| 4px | 12px | 3px 대비 +3px |

인접 단계 간 실기기 차이가 항상 3px로 균등하다 — 즉 1/2/3/4px 4단계는 실기기에서
"3px씩 균등하게 굵어지는 계단"으로 보인다. 이는 우연이 아니라 스케일이 정수배(3.0)이기
때문이며, 4단계 체계가 시각적으로 성립할 물리적 근거가 된다(단계가 등간격이 아니면
일부 구간만 도드라지고 일부는 안 보이는 문제가 생길 수 있었는데, 여기선 그렇지 않다).

**정정할 가설**: "얇아서 안 보인다"가 아니라 "굵기가 무작위로 재사용돼 있어서, 굵어져도
아무 의미가 없다"가 정확한 진단이다. 두께 자체의 가시성 문제는 이번 조사에서 확인되지
않았다.

---

## 2. 코드베이스 실측 — 현재 두께 사용 현황

`Assets/UI/*.uss` 전체에서 `border-width` 계열 선언을 전수 조사했다. 아래는 원본 그대로의
실측 표다(추정 아님, 파일:라인 명시).

### 2.1 — 1px (구조/기본 테두리)

| 클래스 | 파일:라인 | 용도 |
|---|---|---|
| `.tactical-panel` | `Tactical.uss:11` | 모든 패널 기본 테두리 |
| `.tactical-panel__header` (bottom) | `Tactical.uss:19` | 헤더 구분선 |
| `.data-row` (bottom) | `Tactical.uss:51` | 판독 행 구분선 |
| `.research-row` (전체) | `UpgradeTree.uss:243` | 연구 항목 행 기본 |
| `.branch-row` (전체) | `UpgradeTree.uss:96` | 브랜치 보드 행 기본 |
| `.status-row` 계열, `.tree-node`, `.country-row` 등 다수 | `Theme.uss:143-150` | hover 트랜지션 대상(폭 자체는 각 컴포넌트가 정의) |

→ 여기까지는 일관됨. "구조/윤곽"이라는 의미로 1px가 예외 없이 쓰인다.

### 2.2 — 2px (세 가지 서로 다른 의미로 중복 사용)

| 클래스 | 파일:라인 | 실제 의미 |
|---|---|---|
| `.research-row--active` | `UpgradeTree.uss:299` | **진행 중 상태**(DNA초록 색+배경 틴트 동반) |
| `.research-row--maxed` | `UpgradeTree.uss:306` | **완료 상태**(골드 색+배경 틴트 동반) |
| `.country-row` | `MainMenu.uss:68` | **선택 여부와 무관한 기본(idle) 테두리** — 상태도 진행도 아님 |
| `.branch-row--selected` | `UpgradeTree.uss:129` | **선택 커서**(카테고리 탭 선택) |
| `.corner-cut` (height) | `Tactical.uss:35` | 데코 틱 두께(테두리가 아니라 배경색 사각형) |

`research-row--active/maxed`는 "상태(지속적 진행도)", `country-row` 기본값은 "그냥 기본
껍데기", `branch-row--selected`는 "선택(일시적 커서)" — **의미가 완전히 다른 세 개념이
같은 2px를 쓴다.** 특히 `branch-row--selected`(2px, 선택)와 `research-row--active`(2px,
진행 상태)는 **같은 화면(Research Database v2 — 브랜치 보드 + 연구 목록이 한 화면에 공존)**
에서 동시에 보이므로 실사용자가 "이 굵기가 뭘 뜻하는지" 학습할 수 없는 정확한 사례다.

### 2.3 — 3px (선택 강조 vs 개방/폐쇄 토글 — 축이 다른데 값을 공유)

| 클래스 | 파일:라인 | 실제 의미 |
|---|---|---|
| `.pathogen-card--selected` | `MainMenu.uss:46` | 선택 커서 |
| `.country-row--selected` | `MainMenu.uss:83` | 선택 커서 |
| `.research-row--selected` | `UpgradeTree.uss:312` | 선택 커서 |
| `.data-row--open` / `.data-row--closed` (left만) | `Tactical.uss:123, 129` | **공항/항구/국경 개방·폐쇄 이분 상태** — 선택이 아니라 지속 상태, 게다가 전체 테두리가 아니라 좌측 한 줄만 |

"선택 커서" 세 컴포넌트는 3px로 통일돼 있어 실제로는 **일관성이 있다**(아래 3.1에서
근거 있는 패턴으로 인정). 문제는 `data-row--open/closed`가 "선택"이 아닌 "지속 상태"인데도
같은 3px를 좌측 한 줄에만 가져다 썼다는 점 — 이 값은 2.2의 "Active(지속 상태) = 2px"
패턴도 아니고, 2.4의 "Accent Bar = 4px" 패턴도 아닌 **어디에도 속하지 않는 독자적인 3px
값**이다. 세 번째 축(선택도 진행도 아닌, 좌측 바 형태의 이분 상태)이 실제로 존재하는데
토큰 체계가 없어서 임의의 숫자를 가져다 쓴 사례로 보인다.

### 2.4 — 4px (카테고리/식별 accent-bar — 유일하게 완전히 일관된 값)

| 클래스 | 파일:라인 |
|---|---|
| `.accent-bar-row` (공용) | `Tactical.uss:98` |
| `.tree-node` (border-left) | `UpgradeTree.uss:226` (DESIGN.md 명시) |
| `.country-row` (border-left) | `MainMenu.uss:73` |
| `.status-row` (border-left) | `CountryStatusPanel.uss:196` |
| `.section-caption` (border-left) | `UpgradeTree.uss:226` 부근 |
| `.research-row` (border-left) | `UpgradeTree.uss:244` |

6개 컴포넌트 전부 좌측 4px로 **완전히 일관됨.** 이것은 우연이 아니라 `accent-bar-row`라는
공용 클래스가 실제로 존재하고 재사용되기 때문이다 — **"공용 토큰/클래스가 있으면 두께
의미가 지켜지고, 없으면 무너진다"**는 이번 감사의 핵심 증거다. Stroke System v2가 해야
할 일은 정확히 이 패턴(4px accent-bar)을 1px/2px/3px에도 똑같이 적용하는 것이다.

### 2.5 — Corner Cut과 Panel Border의 관계

`.tactical-panel`(1px, `Tactical.uss:11`)과 `.corner-cut`(2px, `Tactical.uss:35`)은
**서로 다른 두 규칙에서 독립적으로 하드코딩**되어 있다 — 코드 어디에도 "코너컷은 패널
테두리의 N배"라는 관계식이 없다. 우연히 2:1 비율이 된 것이고, 이 비율이 의도인지 실수인지
판단할 근거가 코드에 없다. 아래 5절에서 이걸 "버그"로 볼지 "의도된 브래킷 강조"로 볼지
따로 평가한다.

---

## 3. Stroke System v2 타당성 평가

사용자가 제시한 원안:

```
Hairline    = 1px  (구조)
Active      = 2px  (진행)
Selected    = 3px  (선택)
Accent Bar  = 4px  (강조)
Corner Cut  = 2px
```

### 3.1 — 이미 검증된 부분 (코드가 이미 이 규칙을 따르고 있음)

- **Hairline=1px(구조)**: `tactical-panel`, `data-row`, `research-row`/`branch-row` 기본
  테두리 전부 1px. 실측상 예외 없음. **그대로 채택 가능.**
- **Active=2px(진행/지속 상태)**: `research-row--active`/`--maxed`(및 DESIGN.md에 이미
  문서화된 `tree-node--active`/`--maxed`)가 정확히 이 의미로 2px+상태색+배경 틴트를
  쓰고 있다. 이건 **새로 만드는 규칙이 아니라 이미 존재하는 올바른 패턴을 공식화**하는
  것이다.
- **Selected=3px(선택 커서)**: `pathogen-card--selected`, `country-row--selected`,
  `research-row--selected` 3곳이 이미 3px로 일치한다. 이것도 기존 패턴 공식화.
- **Accent Bar=4px**: 2.4절에서 확인했듯 6개 컴포넌트가 이미 완전히 일치. 원안 그대로
  채택.

즉 원안 4단계 중 **3단계(Hairline/Active/Selected/Accent Bar 중 Active·Selected·
Accent Bar)는 이미 코드베이스 다수가 암묵적으로 따르고 있는 값**이고, 토큰화는 이를
"우연한 일치"에서 "강제되는 규칙"으로 승격하는 작업이다. 새 시각 언어를 발명하는 게
아니라 이미 맞는 것을 표준으로 박제하고, 어긋난 소수(2.2/2.3의 예외들)를 거기 맞추는
작업이라는 뜻 — 리스크가 낮은 리팩터링이다.

### 3.2 — 어긋나는 부분 (그대로 채택하면 안 되는 지점)

- **`country-row` 기본 테두리(2px)**: "Active=2px" 의미를 침범한다. `country-row`는
  선택되지 않은 기본 상태에서 진행/활성 상태가 아니므로 Hairline(1px) 취급이 맞다 —
  다만 `country-row`는 이미 `border-left-width:4px` accent-bar를 겸하고 있어서
  (2.4절) 전체 테두리를 1px로 낮추면 4px 좌측 바와 대비가 더 뚜렷해지는 효과도 있다.
  이 부분은 **국가 목록 48행이라는 반복 리스트 특성상 의도적으로 살짝 굵게 잡았을
  가능성**도 있어(DevLog에 별도 근거 기록 없음), 단순 "버그"로 단정하지 않고 "의미
  재배정이 필요한 항목"으로 분류한다.
- **`branch-row--selected`(2px)**: Active 의미(2px)와 Selected 의미(3px)가 같은 화면
  안에서 충돌한다. 이건 2.2절에서 짚은 대로 **실제 버그에 가깝다** — 다른 두 "선택"
  컴포넌트가 전부 3px인데 이것만 2px다.
- **`data-row--open`/`--closed`(3px, 좌측만)**: "선택(3px)"도 "진행(2px)"도 "카테고리
  accent(4px)"도 아닌 제4의 의미(이분 지속 상태, 좌측 바)인데 값만 3px를 빌려 썼다.
  원안 4단계에 이 의미를 위한 자리가 없다 — **원안을 그대로 쓰면 이 케이스가 여전히
  붕 뜬다.** 7절에서 5번째 개념(Sub-accent)을 추가 제안하는 이유다.

### 3.3 — Corner Cut을 독립 토큰(2px)으로 볼지 여부

**독립 토큰으로 두는 것은 권장하지 않는다.** 이유:

1. `border-width`와 `width/height`(코너컷은 배경색 사각형)는 서로 다른 USS 속성이라
   같은 숫자 체계로 묶일 필연성이 없다 — 우연히 2px라는 값이 겹쳤을 뿐이다.
2. 2.5절에서 확인했듯 지금은 "코너컷=패널 테두리의 2배"라는 관계가 코드에 전혀
   선언돼 있지 않다 — 두 값이 각자 따로 바뀌면 비율이 깨질 수 있다.
3. 대신 **"Corner Cut = Hairline × 2"라는 파생 규칙**으로 문서화하면, 나중에
   Hairline 자체가 바뀌어도(예: 다른 레퍼런스 해상도로 프로젝트를 포팅할 때) 관계가
   유지된다는 설계 의도가 명시적으로 남는다.

이 판단은 5절(DEFCON/Frostpunk 관점)과 직접 연결된다.

---

## 4. DEFCON / Frostpunk / Military Command UI 관점 평가

- **DEFCON**: 레이더/지도 프레임은 항상 얇은 단색 헤어라인이고, 강조는 굵기가 아니라
  점멸·색상·텍스트 라벨로 처리한다. 프레임 자체의 두께 위계는 거의 쓰지 않는 편.
- **Frostpunk**: 패널 프레임은 얇게 유지하고, 코너/모서리 브래킷이 프레임보다 확실히
  굵거나 밝게 처리되어 "이 틀 안이 상호작용 가능 영역"임을 알리는 언어를 쓴다. 즉
  **코너가 프레임보다 강조되는 것 자체는 이 장르에서 흔한 관습**이다.
- **일반 Military Command UI 관용구**: 조준경/HUD 프레임에서 코너 브래킷(corner
  bracket)이 연결선보다 굵은 경우가 많다 — 시선을 코너로 먼저 모으고 그 다음 프레임
  경계를 인지시키는 목적이다.

이 관점에서 보면 **"코너가 강하고 패널이 흐릿하다"는 현상 자체는 장르 관습에서 벗어난
게 아니다.** 문제는 그 관계가 "의도"인지 "우연"인지가 코드/문서 어디에도 없다는 것 —
지금 상태로는 다음에 패널 테두리 색이나 두께를 조정하는 사람이 코너컷과의 비율을 전혀
신경 쓸 이유가 없다(연결돼 있다는 사실 자체를 모른다). 그래서 5절 결론과 마찬가지로:
**"코너 > 프레임" 관계 자체는 유지하되, 우연이 아니라 명시적 파생 규칙으로 승격**하는
쪽을 권장한다. Corner Cut을 Hairline과 동일한 독립 1px으로 낮추는 것은 이 장르의
"브래킷이 시선을 끄는" 관용구를 오히려 약화시키므로 **권장하지 않는다.**

---

## 5. 모바일 가독성 평가

1절 실측 배율표(3px 등간격) 기준:

- 1px(3px 실기기) vs 2px(6px 실기기): 차이 3px — 갤럭시 S25 울트라 화면 밀도(약
  500ppi대)에서 인접 판독 요소 간 3px 차이는 근거리(30~40cm) 시야에서 식별 가능한
  수준이다(일반적으로 이런 밀도에서 사람 눈의 최소 식별 간격은 대략 1px대 수준이므로
  3px 차는 여유 있게 식별된다). 다만 이건 일반적인 시각 인지 기준의 추정이며, 실기기
  스크린샷으로 실측 검증하는 QA 항목으로 남겨야 한다(6절 유지보수 항목 참고).
- 4단계(1/2/3/4px)가 전부 3px 간격으로 균등하다는 점은 오히려 **모바일에서 유리한
  조건**이다 — 어느 인접 단계도 압도적으로 작거나 커서 "안 보이는 단계"가 생기지 않는다.
- 위험 요소는 두께가 아니라 **색상 대비**다. `--color-border`(중립, alpha 0.15)와
  `--color-accent-glow-soft`(구조용, alpha 0.35)처럼 저채도·저알파 색 위에서는 3px
  차이보다 알파 값 차이가 가독성에 더 크게 기여할 수 있다 — 두께 체계만 고쳐도 색상
  대비가 낮은 조합(예: 어두운 배경 위 `--color-border`)에서는 개선 체감이 작을 수
  있음을 감안해야 한다. (Color System은 이번 범위 밖이므로 결론만 남긴다.)

---

## 6. 장기 유지보수 관점 평가

- **2.4절의 교훈이 핵심이다**: `accent-bar-row`처럼 **공용 클래스 하나로 강제된 값은
  100% 지켜지고, 각 화면 `.uss`에 개별 선언된 값은 반드시 어긋난다.** 지금 2px/3px
  불일치 사례(`country-row`, `branch-row--selected`, `data-row--open/closed`)는
  전부 **화면 로컬 `.uss`에 하드코딩된 숫자**이지 공용 토큰을 참조한 게 아니다.
  → 토큰만 새로 정의하고 화면 `.uss`가 여전히 리터럴 숫자를 쓰면 6개월 뒤 같은
  감사를 또 해야 한다. **토큰 정의 자체보다 "리터럴 숫자 금지" 규칙(Lint 성격의
  Do/Don't 문구)이 장기적으로 더 중요하다.**
- **DevLog 선례**: `country-row`는 이미 한 번 `border-left-width` 캐스케이드 버그를
  겪었다(DevLog 기록, MainMenu.uss/CountrySelect.uss 로드 순서 문제 — 2.4절 표의
  4px accent-bar가 한때 무력화됐던 사건). 같은 컴포넌트가 두께 체계에서도 예외로
  나온 것은 우연이 아니라 **이 컴포넌트가 구조적으로 "한 rule 안에 여러 축(기본
  테두리+accent-bar+개발수준색)을 욱여넣는" 설계라서 축이 늘어날 때마다 충돌이
  재발하는 패턴**으로 보인다. 8~9절 토큰 설계에서 이 컴포넌트를 우선순위 1번으로
  둔 이유.
- **컴포넌트 확장성**: `data-row--open/--closed`처럼 "선택도 진행도 아닌 좌측 바
  이분 상태"가 이미 하나 존재한다는 것은, 앞으로 비슷한 이분 상태(예: 국경 봉쇄
  여부 `isBorderClosed`를 다른 화면에서도 시각화할 가능성, CLAUDE.md에 언급된
  이동 통제 섹션 확장)가 더 생길 수 있다는 신호다. 지금 4단계 체계에 이 축을 위한
  자리를 만들어 두지 않으면, 다음 기능에서 또 임의의 숫자가 추가될 것이다.

---

## 7. Stroke System v2 최종안

### 7.1 — Stroke Semantic Table

| 의미(Semantic) | 두께(USS) | 실기기 두께 | 적용 대상(전체 vs 좌측) | 근거 |
|---|---|---|---|---|
| **Hairline** — 구조 | 1px | 3px | 전체 | 패널/행의 기본 윤곽. 상태·선택과 무관하게 항상 존재하는 최소 단위 |
| **Active** — 진행/지속 상태 | 2px | 6px | 전체 + 상태색 + 배경 틴트 | `research-row`/`tree-node`의 `--active`/`--maxed`가 이미 이 조합(두께+색+배경)을 세트로 쓰고 있음 — 두께 단독이 아니라 항상 색·배경과 동반 |
| **Selected** — 선택 커서 | 3px | 9px | 전체 + `--color-border-selected` | `pathogen-card`/`country-row`/`research-row`의 `--selected` 3종이 이미 일치 |
| **Accent Bar** — 카테고리/식별 | 4px | 12px | 좌측만 | 이미 6개 컴포넌트가 일치(2.4절) — 원안 그대로 |
| **Sub-Accent** — 이분 지속 상태 *(신규 제안)* | 2px | 6px | 좌측만 | `data-row--open/--closed`를 위한 자리. Active와 같은 두께(2px = "상태")를 쓰되 적용 범위(좌측만)로 Accent Bar와 구분 |

Sub-Accent를 새로 제안하는 이유: `data-row--open/--closed`는 의미상 "지속 상태"이므로
Active(2px)와 같은 개념 축에 속해야 하고, 형태상 "좌측 바"이므로 Accent Bar와 같은
적용 방식이어야 한다. 기존 3px(선택 전용 두께)를 재사용하는 대신 **2px+좌측 적용**으로
옮기면, "3px=선택"이라는 규칙을 깨지 않으면서 이 컴포넌트도 규칙 안에 들어온다.

### 7.2 — Corner Cut 파생 규칙 (독립 토큰이 아님)

```
corner-cut thickness = Hairline × 2
```

지금 값(2px)은 그대로 유지하되(4절 결론 — 장르 관습상 "코너 > 프레임"은 유지할 가치가
있음), **"왜 2px인가"에 대한 근거를 Hairline의 2배 파생값이라고 명시**한다. USS는
`calc()`를 지원하지 않으므로(코드베이스 위키 `css-to-uss-support.md` 확인 필요 —
DESIGN.md도 이미 이 제약을 여러 번 언급함) 실제 값은 여전히 `2px` 리터럴로 선언해야
하지만, 주석과 문서에 "Hairline 값이 바뀌면 이 값도 같이 검토"라는 관계를 남긴다.

---

## 8. Theme.uss 토큰 설계 제안

**필요하다고 판단한다.** 근거: 6절에서 확인했듯 공용 토큰이 없는 값(2px/3px)만 어긋났고,
공용 클래스가 있는 값(4px)은 지켜졌다. 두께도 색상과 동일하게 `:root` 커스텀 프로퍼티로
승격해야 같은 보호를 받는다.

```css
:root {
    /* Stroke System v2 — 두께=의미. 화면별 .uss에서 border-width 리터럴 숫자 직접
       선언 금지, 항상 이 토큰을 참조할 것. */
    --stroke-hairline: 1px;      /* 구조 — 기본 테두리, data-row 구분선 */
    --stroke-active: 2px;        /* 진행/지속 상태 — 항상 상태색과 동반 */
    --stroke-selected: 3px;      /* 선택 커서 — border-selected 색과 세트 */
    --stroke-accent-bar: 4px;    /* 카테고리 accent-bar, 좌측 전용 */
    --stroke-sub-accent: 2px;    /* 이분 지속 상태 accent-bar, 좌측 전용(data-row--open 등) */
    --stroke-corner-cut: 2px;    /* 코너컷 틱 두께 — Hairline×2 파생값, 독립 개념 아님 */
}
```

`--stroke-active`와 `--stroke-sub-accent`가 값(2px)은 같지만 **토큰을 분리**한 이유는
값이 같다고 개념까지 같은 게 아니기 때문이다 — 나중에 둘 중 하나만 조정해야 하는
상황(예: Sub-Accent만 3px로 올려야 하는 디자인 변경)이 왔을 때 값을 공유하는 토큰
하나였다면 다른 쪽까지 영향을 준다. 토큰 이름 자체가 "의미"를 담아야 한다는 원칙에
따른 결정.

---

## 9. DESIGN.md 수정안 초안 — "Stroke System" 신규 섹션

> 아래는 **Spacing System과 Surface Hierarchy 사이**에 삽입할 수준으로 작성한 초안이다.
> 기존 문서 톤(원칙 나열 → 표 → Do/Don't)을 그대로 따랐다. 실제 반영은 사용자 승인
> 후 별도 작업으로 진행.

```markdown
## Stroke System

**두께는 장식이 아니라 의미다.** 이 시스템은 `border-width`를 시각적 강조 스케일이
아니라 **상태를 나타내는 고정 어휘**로 다룬다. 굵기를 임의로 조정하지 않고, 아래 5단계
중 하나를 선택한다.

> **PanelSettings 배율 참고**: `Assets/UI Toolkit/PanelSettings.asset`은
> Scale With Screen Size + Reference Resolution `480×1040` + Match Width로
> 설정되어 있다. 타겟 기기(갤럭시 S25 울트라, 1440×3120) 기준 실제 렌더링 배율은
> **정확히 3.0배**다 — USS 1px는 실기기 3px로 그려진다. "두께가 안 보인다"는
> 렌더링 문제가 아니라 항상 **의미 중복 문제**로 먼저 의심할 것.

### Stroke Semantic Table

| 토큰 | 두께 | 의미 | 적용 범위 |
|---|---|---|---|
| `--stroke-hairline` | 1px | 구조 — 모든 패널/행의 기본 윤곽 | 전체 |
| `--stroke-active` | 2px | 진행/지속 상태(항상 상태색+배경 틴트 동반) | 전체 |
| `--stroke-selected` | 3px | 선택 커서(항상 `--color-border-selected` 동반) | 전체 |
| `--stroke-accent-bar` | 4px | 카테고리/식별 accent | 좌측만 |
| `--stroke-sub-accent` | 2px | 이분 지속 상태(개방/폐쇄 등) accent | 좌측만 |

`--stroke-corner-cut`(2px)은 별도 개념이 아니라 `--stroke-hairline`의 2배 파생값이다
(USS `calc()` 미지원으로 리터럴 선언하되, Hairline이 바뀌면 함께 검토한다).

### Do
- 화면 전용 `.uss`에 `border-width` 리터럴 숫자를 직접 쓰지 않는다 — 항상 위 5개
  토큰 중 하나를 참조한다.
- Active/Selected는 두께 단독으로 쓰지 않는다 — 항상 상태색(또는 `border-selected`)과
  세트로 적용한다. 두께만 바뀌고 색이 그대로면 의미가 전달되지 않는다.
- 새 컴포넌트가 "선택도 진행도 아닌 좌측 바 이분 상태"에 해당하면 `--stroke-sub-accent`를
  쓴다 — 3px(Selected)를 빌려 쓰지 않는다.

### Don't
- 같은 화면에서 두 개의 다른 개념에 같은 두께를 쓰지 않는다(과거 사례: 브랜치 보드
  선택=2px, 연구 항목 진행 상태=2px가 같은 화면에서 충돌).
- 코너컷 두께를 패널 테두리와 무관하게 임의로 조정하지 않는다 — 항상 Hairline의 2배를
  유지한다.
- 리스트 반복 행(48개국 등)의 기본(비선택) 상태에 Active(2px)를 쓰지 않는다 — 기본
  상태는 항상 Hairline(1px)이고, accent-bar(4px, 좌측)로만 식별 정보를 더한다.
```

---

## 10. 현재 코드베이스 불일치 항목 정리

| 항목 | 파일:라인 | 현재 값 | 문제 | 제안 |
|---|---|---|---|---|
| `.country-row` 기본 테두리 | `MainMenu.uss:68` | 2px(전체) | Active(2px) 의미를 비활성 기본 상태가 침범 | Hairline(1px)로 하향, 좌측 accent-bar(4px)는 유지 |
| `.branch-row--selected` | `UpgradeTree.uss:129` | 2px | 같은 화면의 `research-row--active`(2px)와 의미 충돌, 다른 Selected 사례(3px)와도 불일치 | Selected(3px)로 상향해 `pathogen-card`/`country-row`/`research-row`와 통일 |
| `.data-row--open` / `.data-row--closed` | `Tactical.uss:123,129` | 3px(좌측) | Selected 두께(3px)를 좌측 accent 형태로 오용 — 축 자체가 없음 | 신설 `--stroke-sub-accent`(2px, 좌측)로 이동 |
| `.corner-cut` vs `.tactical-panel` | `Tactical.uss:35` vs `:11` | 2px vs 1px, 관계 미문서화 | 우연한 2:1 비율 — 의도인지 실수인지 코드에 근거 없음 | 값은 유지, "Hairline×2 파생값"으로 명문화(9절 반영) |
| 화면별 `.uss`의 리터럴 `border-width` 다수 | `MainMenu.uss`, `UpgradeTree.uss` 전반 | 1/2/3px 리터럴 혼재 | 토큰 미사용 — 6절의 "공용 클래스만 지켜진다" 교훈과 직결 | 8절 토큰으로 전면 치환(코드 작업은 별도 승인 후) |

이 표의 항목은 **보고 목적**이며, 이번 세션에서는 코드/USS/DESIGN.md 어느 것도 수정하지
않았다.

---

## 11. 실제 적용 우선순위

사용자가 제시한 순서(① Stroke Token 도입 → ② Selectable Card 통일 → ③ Button System
→ ④ Color System)를 실측 결과로 검증한 결과, **순서 자체는 타당하다.** 근거를 덧붙이면:

1. **Stroke Token 도입** — 8~9절 토큰을 `Theme.uss`에 추가. 다른 모든 작업의 전제
   조건이므로 최우선. 위험도 낮음(기존에 이미 대다수가 따르던 값을 토큰화할 뿐).
2. **Selectable Card 통일** — `pathogen-card`/`country-row`/`branch-row`/`research-row`
   4개 "선택 가능 카드" 계열이 Selected(3px)를 공유하도록 정리. `branch-row--selected`
   버그(10절)가 여기서 해결된다. Stroke Token이 먼저 있어야 리터럴 치환이 아니라
   토큰 참조로 고칠 수 있으므로 2순위가 맞다.
3. **Button System** — 이번 조사 범위 밖이라 실측하지 않았으나, `Interaction Rules`
   절(DESIGN.md)이 이미 버튼 테두리색·:active 규칙을 일부 다루고 있어 Stroke Token이
   생기면 자연스럽게 버튼에도 적용할 여지가 크다. 3순위 타당.
4. **Color System** — 이번 보고서에서 의도적으로 보류한 영역. 5절에서 짚었듯 두께
   체계를 고쳐도 저알파 색상 조합에서는 개선 체감이 제한적일 수 있으므로, 두께 정리가
   끝난 뒤 색상 대비를 별도로 감사하는 순서가 합리적이다.

---

## 12. 최종 결론

**Contagion Project Tactical UI의 Stroke 철학**:

> 두께는 굵기가 아니라 **어휘**다 — 1px는 항상 "구조"라고 말하고, 2px는 항상 "진행
> 중이거나 지속되는 상태"라고 말하고, 3px는 항상 "지금 선택된 것"이라고 말하고, 4px는
> 항상 "이것이 무슨 카테고리인지"라고 말한다. 같은 굵기가 두 가지를 동시에 말하는
> 순간, 이 시스템은 전술 UI이기를 멈춘다.
