# HUD 재설계 분석 보고서

> 코드/USS/DESIGN.md는 수정하지 않았다. 이 문서는 분석 전용이며, 실제 수정은 별도 작업으로 진행한다.
> 근거 파일: `Assets/UI/Hud.uxml`, `Hud.uss`, `Theme.uss`, `Tactical.uss`, `CountryPopup.uss`,
> `ResearchPopup.uss`, `MainMenu.uss`(`.mainmenu-back`), `Docs/DESIGN.md`.

---

## 1. 상단 HUD (`resource-strip`) 구조 분석

`Hud.uss:14-22`

```
.resource-strip {
    height: 34px;
    background-color: var(--color-bg-scrim-soft);      /* rgba(0,0,0,0.55) */
    border-bottom-width: 1px;
    border-bottom-color: var(--color-accent-glow-soft); /* rgba(150,255,185,0.35) */
}
```

- **배경**: `--color-bg-scrim-soft` = 검정 55% 알파. 즉 지도가 **45% 그대로 비친다.**
- **Border**: 하단 1px만. 좌/우/상단 테두리 없음.
- **Corner Cut**: 없음. `resource-strip`은 `corner-cut` 클래스를 전혀 쓰지 않는다.
- **Transparency**: DESIGN.md의 Surface Hierarchy 표(`DESIGN.md` "Surface Hierarchy" 절)에서
  `resource-strip`은 명시적으로 **Tier 1 "Scrim bar"** — "떠있는 패널이 아니라 화면에 고정된
  크롬 바"로 분류돼 있다. 즉 현재 반투명 상태는 **버그가 아니라 설계 의도**다. 문제는 이
  의도 자체가 사용자가 원하는 "지도 위에 뜬 콘솔" 느낌과 맞지 않는다는 데 있다.

## 2. 하단 HUD 구조 분석

세 블록으로 나뉜다 (`Hud.uss:303-471`).

| 블록 | 배경 | Border | 역할 |
|---|---|---|---|
| `population-bar` | `--color-bg-scrim-strong` (검정 75%) | 없음 | 통계 요약 막대 |
| `graph-panel` | `--color-bg-scrim-strong` (검정 75%) | 상단 1px `--color-grid-line`(민트 12%) | 감염/사망/치료제 그래프 3개 |
| `action-strip` | `--color-bg-scrim-strong` (검정 75%) | 상단 1px `--color-accent-glow-soft`(민트 35%) | 버튼 3개 |

- 세 블록이 **같은 배경색·같은 알파값**을 공유하며 경계선도 얇은 상단 1px뿐이라, 실제로는
  하나의 큰 반투명 검은 블록처럼 보인다. 좌/우/하단 테두리는 어디에도 없다.
- Corner Cut: 이 세 블록 모두 없음. DESIGN.md "Corner Cut — Don't" 규칙("화면 루트/전체폭
  크롬 바에는 붙이지 않는다")과 일치하는 의도적 생략이다 — 즉 하단 HUD도 Tier 1 Scrim bar로
  설계됐다.
- `action-strip`은 75% 알파 검정이라 상단 HUD보다는 지도와 분리되지만, **버튼 자체가 그 위에서
  더 밝은 색(후술 4절)으로 채워져 있어 "바"보다 "버튼"이 시각적으로 더 도드라지는 역전**이
  발생한다.

## 3. 지도-HUD 색상 충돌 원인

원인은 색상 팔레트 충돌이 아니라 **알파(투명도) 설계**다.

- Tactical Design System은 5단계 Surface Hierarchy를 쓴다(`DESIGN.md` "Surface Hierarchy"):
  0 Canvas → **1 Scrim bar**(resource-strip/population-bar/graph-panel/action-strip) →
  2 Docked panel(event-dock/country-dock/CountryPopup/ResearchPopup) → 3 Card/row → 4 Selected.
- HUD 상/하단은 처음부터 **Tier 1(반투명 크롬 바)**로 설계됐다. Tier 1의 정의 자체가 "지도
  위에 완전히 분리된 패널"이 아니라 "지도가 비쳐 보이는 얇은 바"다.
- 반면 사용자가 만족한 CountryPopup/ResearchPopup/event-dock/country-dock은 전부 **Tier 2**
  (`--color-bg-panel`, 알파 0.95 — 거의 불투명)이고, `tactical-panel`의 **4면 테두리**를
  온전히 두른다.
- 즉 "HUD가 지도와 한 덩어리로 보인다"는 인상은, 상/하단 HUD가 설계상 **Tier 1에 머물러
  있는데 사용자는 그것을 Tier 2급 분리감으로 기대**하기 때문에 발생한다. 55%/75% 알파는
  "장식성 오버레이"로는 적절해도, 사용자가 원하는 "떠 있는 콘솔" 느낌을 주기엔 지도 색이
  너무 많이 비친다 — 특히 상단(55%)이 하단(75%)보다 더 심하다.
- 부차 요인: 상/하단 모두 좌우 테두리가 없고 코너컷도 없어, 지도와 맞닿는 경계가 "얇은
  가로선 하나"뿐이다. Tier 2 패널들이 4면 테두리 + 코너컷으로 윤곽을 완전히 닫는 것과
  대조적이다.

## 4. 버튼 분석 — 왜 "색칠된 사각형"으로 보이는가

`Hud.uss:456-471`

```
.tab-button {
    border-color: var(--color-accent-glow);
    background-color: var(--color-accent-glow-soft);   /* rgba(150,255,185,0.35) 민트 채움 */
}
.tab-button--secondary {
    background-color: var(--color-button-secondary);   /* rgb(70,70,90) 무채색 채움 */
    border-color: var(--color-border);                  /* rgba(255,255,255,0.15) — 거의 안 보임 */
}
```

- `tab-button`(업그레이드/국가현황)은 **채움색이 배경보다 밝은 민트**다. 어두운 배경 위에
  놓인 밝은 블록은 시선을 가장 먼저 끌지만, 그 형태가 "테두리+글자"가 아니라 "면적"이기
  때문에 버튼이라기보다 색칠된 도형으로 지각된다.
- `tab-button--secondary`(랭킹)는 더 심각하다 — 채움색이 회보라 계열 flat color고, 테두리는
  `--color-border`(흰색 15% 알파)라 거의 안 보인다. 사실상 "테두리 없는 색 블록"이다.
- 참고로 DESIGN.md "Button System > Primary Button" 정의는 정확히
  `background-color: --color-accent-glow-soft` + `border: 1px --color-accent-glow`다 — 즉
  `tab-button`은 **스펙 위반이 아니라 Primary Button 스펙을 문자 그대로 따른 결과**다. 문제는
  Primary Button 스펙 자체가 "채움형"이라, 폭 전체를 채우는 대형 버튼 3개가 나란히 반복될 때
  "장식 프레임"이 아니라 "칠해진 판"으로 읽힌다는 데 있다. ResearchPopup의
  `popup-footer-button--confirm`도 같은 스펙(accent-glow-soft 채움)을 쓰지만 문제로
  지적되지 않았다 — 이유는 5절 참고.

## 5. 성공 사례(✕ 버튼 3종) 대비 분석

| | CountryPopup ✕ / ResearchPopup ✕ / CountrySelect 뒤로 | HUD 3버튼 |
|---|---|---|
| 배경 | `--color-bg-panel`(거의 불투명 검정) | `--color-accent-glow-soft`(민트 채움) / `--color-button-secondary`(회보라 채움) |
| 내부 | 어둡다 → 배경과 동화 | 밝다 → 배경과 분리, 형태가 "판"으로 지각 |
| 테두리 | 1px `--color-accent-glow`(선명한 민트) 4면 전체 | `tab-button`은 4면 있으나 채움 위라 존재감 약함 / `--secondary`는 테두리가 사실상 안 보임 |
| 소속 맥락 | Tier 2 불투명 패널(CountryPopup/ResearchPopup) 또는 캔버스(CountrySelect) 위 — 배경 자체가 이미 분리돼 있음 | Tier 1 반투명 스크림 바 위 — 배경부터가 지도와 섞여 있음 |
| 개수/면적 | 화면당 1개, 작은 사이즈(28px 정사각 또는 46px 단일 바) | 3개가 나란히, 폭 전체를 채우는 대형 반복 |

세 버튼이 공유하는 공통 공식은 정확히 `DESIGN.md`의 **Secondary Button** 스펙이다:

```
background-color: --color-bg-panel;
border: 1px --color-accent-glow;
border-radius: 0;
color: --color-text-primary;
```

즉 사용자가 "만족스럽다"고 느낀 것은 **어두운 내부 + 밝은 윤곽선**이라는 조합이지,
Primary Button(민트 채움) 조합이 아니다. `popup-footer-button--confirm`(ResearchPopup의
Primary 채움 버튼)이 문제로 지적되지 않은 이유도 이 표로 설명된다 — ① 이미 불투명 Tier 2
패널 내부에 있고, ② 옆의 "취소"(Secondary, 어두운 채움) 버튼과 나란히 있어 "둘 중 하나만
강조됐다"는 상대적 대비로 읽히며, ③ 작은 footer 버튼 하나일 뿐 화면을 가로지르는 대형
반복 요소가 아니다. HUD의 3버튼은 이 세 조건을 모두 반대로 갖는다 — 배경부터 반투명이고,
3개가 전부 같은 방식(민트 또는 회보라 채움)으로 강조돼 있어 "무엇이 강조됐는지" 신호가
없고, 폭 전체를 채우는 대형 반복 요소다.

**결론**: 문제는 버튼 컴포넌트 자체(Button System)가 아니라, ① HUD가 Primary Button 스펙을
쓴 것 자체가 이 맥락(반투명 바 위, 대형 반복)에 맞지 않았고 ② `--secondary` 변형은 애초에
DESIGN.md의 Secondary Button 스펙(어두운 배경+민트 테두리)과 다른 임의의 회색 채움
(`--color-button-secondary`)을 쓰고 있다는 것이다.

---

## 설계 목표 재확인

HUD는 "지도 위에 떠 있는 Tactical Control Console"이어야 한다. 현재는 상/하단 모두
Tier 1 Scrim bar로 설계되어 있어 이 목표와 구조적으로 어긋난다.

## 6. 검토한 개선 방향

### A안 — 상/하단 HUD를 Tactical Panel 계열로 승격

`resource-strip`/`action-strip` 등을 Tier 1(스크림 바)에서 Tier 2(`tactical-panel`,
`--color-bg-panel` 95% 불투명 + 4면 `accent-glow-soft` 테두리)로 승격.

- 장점: event-dock/country-dock/CountryPopup과 동일한 "닫힌 패널" 언어로 통일되어 사용자가
  이미 검증한 분리감을 상/하단에도 그대로 재현. 코너컷 부착도 가능해져 시그니처 요소 일관성 상승.
- 트레이드오프: `DESIGN.md` Corner Cut "Don't" 규칙("화면 루트/전체폭 바에는 코너컷을 붙이지
  않는다")과 배치되므로, 코너컷 없이 4면 테두리 + 불투명 배경만 적용하는 절충이 필요.
  또한 Tier 1을 Tier 2로 승격하면 DESIGN.md의 Surface Hierarchy 표 자체를 갱신해야 함
  (문서 수정은 이번 범위 밖이므로 별도 커밋 필요).
- 지도 노출 면적이 상/하단에서 줄어드는 것은 의도된 트레이드오프 — "지도가 주인공"이 아니라
  "지도 위의 콘솔"이 목표이므로 방향과 일치.

### B안 — HUD 버튼을 Secondary Button 스펙으로 통일

`tab-button`/`tab-button--secondary`의 채움색을 제거하고 성공 사례 3종과 동일한 공식
(`background: --color-bg-panel`, `border: 1px --color-accent-glow`, `radius: 0`,
`color: --color-text-primary`)으로 통일.

- 장점: 코드 변경 범위가 가장 작다(색상 3개 값 교체 수준). 이미 실기기에서 검증된 조합을
  그대로 재사용하므로 리스크가 낮다. "주 액션 vs 보조 액션" 구분이 필요하면 테두리 두께
  (`--stroke-hairline` vs `--stroke-active`)나 좌측 accent-bar로 대체 가능 —
  DESIGN.md Stroke System이 이미 "진행 상태=2px" 의미를 정의해 두었으므로 재사용 가능.
- 트레이드오프: 현재 `tab-button`(발광 테두리)과 `--secondary`(중립 테두리)로 구분하던
  "주 액션/보조 액션" 구분이 채움색 없이는 약해질 수 있음 — 테두리 두께나 accent-bar 같은
  대체 강조 수단이 함께 필요.

### C안 — 기타 대안

1. **하이브리드**: A안(패널 승격)과 B안(버튼 스펙 통일)을 동시에 적용. 근본 원인이 ①
   배경 알파, ② 버튼 채움색 두 가지이므로, 하나만 고치면 나머지 하나가 여전히 "지도와
   섞임"/"색칠된 사각형" 인상을 남길 가능성이 있다. 두 원인이 서로 다른 레이어(패널
   배경 vs 버튼 크롬)에 있어 충돌 없이 함께 적용 가능.
2. **점진안**: 우선 action-strip(하단 버튼 바)만 Tier 2로 승격 + 버튼을 Secondary 스펙으로
   교체 (문제 2·3·4·5가 집중된 영역), resource-strip(상단)은 후속 작업으로 분리. 사용자가
   "하단 버튼 3개가 버튼처럼 안 보인다"를 별도 항목으로 명시했으므로 우선순위가 가장 높음.
3. **비추천**: 버튼에만 그림자/그라디언트를 추가해 "떠 보이게" 하는 방식 — USS가
   `box-shadow`를 지원하지 않고(DESIGN.md "USS 기술적 제약"), Tactical Design System의
   "Hairline-first, No shadow" 원칙에 정면으로 위배되므로 제외.

## 산출물 요약

1. **현재 문제 원인**: 상/하단 HUD가 Tactical Design System의 Tier 1(반투명 Scrim bar)로
   설계되어 있고, 하단 3버튼은 DESIGN.md의 Primary Button 스펙(민트 채움)을 대형·반복
   맥락에 그대로 적용한 것, 그리고 랭킹 버튼은 Secondary Button 스펙과 다른 임의의 회색
   채움을 쓰고 있다.
2. **HUD-지도 색 섞임 이유**: `resource-strip`은 알파 0.55, 하단 3블록은 알파 0.75인
   반투명 배경이라 지도가 그대로 비친다 — Tier 2 패널(0.95 불투명 + 4면 테두리)과 달리
   "닫힌 패널"로 지각되지 않는다.
3. **버튼이 버튼처럼 안 보이는 이유**: 채움색(민트/회보라)이 배경보다 밝아 "면"으로
   지각되고, 3개가 동일한 강조 방식으로 나란히 반복되며, 반투명 배경 위에 있어 테두리의
   윤곽 역할이 약화된다.
4. **CountryPopup 등 성공 이유**: 어두운 불투명 배경(Tier 2) + 민트 테두리 4면(Secondary
   Button 스펙) + 소수·소형 인스턴스 + 이미 분리된 패널 맥락 위에 있다는 4가지 조건을
   모두 만족한다.
5. **HUD 재설계안**: A안(패널 Tier 승격) + B안(버튼 Secondary 스펙 통일)을 함께 적용하는
   하이브리드가 근본 원인 두 가지를 모두 해소한다.
6. **우선 적용 순위**: (1) 하단 `action-strip` 버튼 3종을 Secondary Button 스펙으로 교체 —
   가장 적은 변경으로 가장 명확히 지적된 문제 해결. (2) 하단 3블록(population-bar/
   graph-panel/action-strip) 배경을 Tier 2급 불투명도로 상향 + 4면 테두리 부여. (3) 상단
   `resource-strip`도 동일 원칙 적용. (4) 필요 시 DESIGN.md Surface Hierarchy 표에
   HUD 전용 Tier 승격을 문서화(별도 작업).

## 최종 결론

**현재 HUD에서 가장 큰 시각적 문제는, 상/하단 HUD가 "지도 위의 불투명 콘솔 패널"이 아니라
알파값 0.55~0.75의 반투명 스크림 바로 설계되어 있고, 그 위에 놓인 하단 3버튼마저 어두운
내부 없이 밝은 색으로 통째로 채워져 있다는 것 — 즉 배경과 버튼 모두 "테두리로 감싼 어두운
공간"이 아니라 "칠해진 면"으로 지각된다는 한 가지 원인으로 요약된다.** 성공한 3개 버튼이
공유하는 유일한 공통점(어두운 배경+민트 테두리, Tier 2 불투명 패널 맥락)이 HUD 상/하단
전체에는 아직 적용되지 않은 상태다.
