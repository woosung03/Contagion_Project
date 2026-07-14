# Color System v2 설계 보고서

> 이 문서는 설계 보고서(제안)다. `DESIGN.md`/USS/코드는 이 문서와 함께 수정하지 않았다.
> 실제 반영은 별도 작업으로 진행한다.

---

## 0. 배경 요약

최근 색상 감사 결론:

- `DESIGN.md`의 Surface / Border / Severity / Brand 체계 자체는 실패하지 않았다 — 유지한다.
- 진짜 문제는 **Button System의 부재**다.
- Accent Color(민트, `--color-accent-glow`)가 구조물(테두리·코너컷·격자)에만 쓰이고 행동
  유도(버튼)에는 거의 쓰이지 않는다.
- 그 결과 Unity 기본 회색 버튼이 일부 화면에 그대로 노출되고, 전체 UI가 "산업용 프로그램"
  인상을 준다.

## 1. 코드 근거 조사

주장을 검증하기 위해 실제 USS를 확인했다. 세 가지가 확인됐다.

**① 미정의 배경색 버튼 3곳** — `background-color`를 지정하지 않아 Unity 기본 회색 버튼이
그대로 노출된다.

| 클래스 | 파일 | 상태 |
|---|---|---|
| `.popup-close` | `CountryPopup.uss` | width/height/padding만 있음, 배경·테두리 없음 |
| `.status-close-btn` | `CountryStatusPanel.uss` | 동일 |
| `.mainmenu-back` | `MainMenu.uss` | height/margin만 있음, 배경·테두리 없음 |

**② 고아 토큰(orphan token)** — `Theme.uss`에 `--color-button-secondary: rgb(70, 70, 90)`가
선언돼 있으나 `DESIGN.md`의 `colors:` 소스오브트루스 목록에는 없다. 사용처도
`Hud.uss .tab-button--secondary` 단 한 곳뿐이다. "버튼 색"이 시스템이 아니라 애드혹으로 한 번
끼워 넣어진 흔적이다.

**③ 이미 한 번 터진 문제** — `EndingScreen.uss`에 다음 코드 주석이 그대로 남아 있다:

> "버그 리포트: 기존엔 width/height/margin만 지정하고 배경·테두리·폰트가 없어 Unity 기본
> 런타임 버튼(둥근 회색)이 그대로 노출돼 Tactical Design System과 어긋났다."

즉 같은 유형의 버그가 `EndingScreen`에서 이미 한 번 발생했고, `.ending-button`을
`accent-glow` 테두리 + `bg-panel` 배경으로 개별 땜질했다. `.mainmenu-next`(브랜드 그린 채움),
`.popup-footer-button--confirm`(accent-glow-soft 채움), `.ending-button--revive`(프리미엄
퍼플 채움)까지 합치면 **버튼마다 색을 각자 다르게 손으로 지정**하고 있다 — 공용 컴포넌트가
없다는 뜻이다. 이번 감사 결론과 정확히 일치한다.

결론: 문제는 "색이 부족해서"가 아니라 **"버튼이라는 컴포넌트 자체가 시스템으로 존재하지
않아서"**다. 색을 하나 새로 만드는 것보다 우선, 그 색을 담을 Button System의 구조가 먼저
필요하다.

---

## 2. 두 개의 레이어로 분리

| Layer | 목적 | 대상 |
|---|---|---|
| **Layer 1 — UI Color System** | 사용자 행동 유도 | 버튼, 선택 상태, 탭, 네비게이션, 드롭다운, 대륙 접기/펼치기 헤더 |
| **Layer 2 — Game Semantic Color System** | 게임 의미 전달 | 뉴스, 이벤트, 알림, 국가 상태, 감염 상황, 치료제 진행 |

두 레이어는 색상값을 공유하지 않는다. 이게 핵심이다 — 지금 문제의 절반은 "액션 색이 없다"지만
나머지 절반은 "만약 액션 색을 severity 계열(빨강/파랑)로 잘못 고르면 새로운 축돌이 생긴다"는
잠재 리스크다. 아래 분석은 이 전제 위에서 진행한다.

---

## 3. Button System 색 후보 비교

### 3-1. Mint 기반 (A안 — Mint = 구조 + 버튼)

**장점**
- 학습 비용 0. 이미 코너컷/테두리/격자가 전부 민트라 추가 색 도입 없이 바로 통일감이 생긴다.
- "이 게임 하면 떠오르는 색"이라는 아이덴티티가 강화된다.

**단점**
- **의미 과부하(semantic overload)**가 가장 큰 문제다. 지금 화면에서 민트는 이미 패널
  테두리·코너컷·격자선·연구 가능 노드 테두리까지 뒤덮고 있다. 여기에 버튼 채움까지 민트를
  쓰면 "이게 눌러야 하는 버튼인지 그냥 패널 장식인지"가 구별되지 않는다. 전경(액션)과
  배경(구조)이 같은 색이 되는 순간 위계가 무너진다.
- DEFCON은 실제로 이 방식(단색 그린 CRT)에 가깝지만, DEFCON은 버튼 자체가 거의 없는
  지도·텍스트 중심 UI다. 이 프로젝트는 버튼·탭·드롭다운·아코디언이 훨씬 많은 밀도 높은
  모바일 UI라 같은 논리를 그대로 옮기기 어렵다.
- 장시간 플레이 관점에서도 불리하다 — "모든 게 강조되면 강조는 사라진다." 화면 전체가
  민트 헤어라인으로 뒤덮인 상태에서 버튼까지 민트면, 정작 지금 눌러야 할 지점을 찾기 위해
  사용자가 매번 형태(사각형인지, 텍스트인지)로 판별해야 한다 — 색으로 즉시 판별되지 않는다.

### 3-2. Amber 기반 (B안 — Mint = 구조, Amber = 버튼)

**장점**
- `DESIGN.md`가 이미 선언한 "색상 축 분리" 원칙과 정확히 일치한다. 구조축(민트)과 액션축
  (앰버)이 물리적으로 분리되므로 어떤 화면에서도 즉시 "여기를 누르면 된다"가 형태 판별
  없이 색만으로 인지된다.
- 이 프로젝트에는 이미 `--color-brand-gold`(rgb 255,210,90 — 앰버/골드 계열)가 "선택 강조/
  화폐성 텍스트"용으로 존재하고 검증돼 있다. 완전히 새로운 색상군이 아니라 **이미 사용자가
  익숙한 색조의 확장**이라 도입 저항이 적다.
- Frostpunk의 미학과 정확히 겹친다 — 차갑고 어두운 생존 환경(강철·서리 톤) 속에서 유일하게
  따뜻한 색(호박빛 불빛)이 "여기에 생존이 걸려 있다/여기를 조작하라"는 신호로 쓰인다. 이
  게임도 "차가운 전술 콘솔(다크+민트 헤어라인)" 속에서 "지금 조작 가능한 지점"을 따뜻한
  색으로 분리하는 것이 톤에 맞는다.
- 색상환에서 민트(청록)와 앰버(주황)는 서로 보색에 가까워 다크 배경 위에서 대비가 가장
  크다 — 모바일 소형 화면·짧은 시선 이동에서 버튼 식별 속도가 가장 빠르다.
- Red/Blue(게임 의미색)와도 색상 거리가 멀어 "이 버튼을 누르면 병원체에 좋은 건가 인류에
  좋은 건가"라는 오해석이 생기지 않는다 — 순수하게 "조작 신호"로만 읽힌다.

**단점 / 리스크**
- `--color-brand-gold`와 색상 거리가 가깝다. 버튼 앰버를 골드와 동일 톤으로 두면 "이게
  버튼인지 자원/선택 표시인지" 다시 혼동될 수 있다 → 버튼용 앰버는 골드보다 채도를 낮추고
  주황 쪽으로 더 이동시켜(예: `rgb(255, 160, 70)` 대) 골드와 육안으로 구별되게 분리해야
  한다.
- "브랜드 악센트 3색 고정, 신규 추가 금지" 규칙과 문면상 충돌하는 것처럼 보인다. 하지만
  이 규칙은 **Layer 2(게임 의미/자원) 축**에 대한 것이고, Action Color는 Layer 1(UI 행동)
  이라는 별도 축이므로 "브랜드 색을 늘리는 것"이 아니라 "누락돼 있던 5번째 축을 새로 정의
  하는 것"이다. §7에서 이 구분을 `DESIGN.md`에 명문화하는 것을 제안한다.

### 3-3. Red 기반 버튼

**장점**
- 일반적인 액션 게임 UI에서 강한 행동 유도력을 가진다(긴급성 신호).

**단점 — 이 프로젝트에서는 채택 불가**
- Red는 이미 이 프로젝트에서 강한 의미를 선점하고 있다(`status-dead`, 그리고 이번 재정의로
  "전염병에게 유리한 사건"). 버튼에 Red를 쓰면 "이 버튼을 누르면 위험한 일이 생기나?"라는
  잘못된 인지 부하가 즉시 발생한다. `DESIGN.md`가 이미 못박은 "Severity 색을 UI 크롬에
  가져다 쓰지 않는다" 규칙과 정면으로 충돌한다.
- 단, **예외가 하나 있다** — Danger Button(파괴적 확인 액션: 게임 포기, 삭제, 되돌릴 수 없는
  선택)에는 오히려 Red 계열이 의미상 정확하다. 이 경우는 "충돌"이 아니라 "일치"다. 아래
  Button System 설계에서 Danger Button 전용으로만 한정한다.

### 3-4. 결론

**B안(Mint = 구조, Amber = 버튼)을 채택한다.** Mint 단일축 확장(A안)은 의미 과부하로
장시간 플레이에 불리하고, Red 기반은 기존 Severity 축과 정면충돌한다. Amber는 구조축과
분리되면서도 이미 존재하는 Gold 계열의 자연스러운 확장이라 도입 비용이 가장 낮다.

---

## 4. 70 : 20 : 10 평가

### 현재 상태

| 비중 | 실제 색 | 문제 |
|---|---|---|
| ~85~90% | 다크 캔버스/패널(Base) | 정상 |
| ~10~15% | 민트 구조색 + Severity 4색 + 브랜드 3색 | Secondary 역할은 하고 있음 |
| ~0% | Action Color | **사실상 없음.** `.mainmenu-next`가 브랜드 그린을 버튼에 차용한 게
유일한 사례이고, 이마저 "자원색을 버튼에 재사용"한 것이라 축 정의상 올바르지 않다. |

Action Color가 0%에 가깝다는 것 자체가 "산업용 프로그램 인상"의 근본 원인이다. 사람 눈은
70:20:10 중 10%(Action)이 있어야 "여기가 인터랙티브 화면"이라고 인지한다. 지금은 사실상
90:10:0이라 화면 전체가 "읽는 화면"으로만 보인다.

### 제안 상태

| 비중 | 색 | 용도 |
|---|---|---|
| 70% Base | `--color-bg-root/panel/panel-strong` 등 (그대로 유지) | 캔버스/패널 |
| 20% Secondary | 민트 구조색(Layer 1 비-액션 부분) + Severity/브랜드(Layer 2) | 구조 헤어라인, 코너컷, 데이터 값 색 |
| 10% Action | Amber 단일색(Layer 1 액션 부분) | Primary Button, 선택된 탭, 활성 아코디언 헤더, 강조 CTA |

Secondary(20%)의 상한을 지키는 게 중요하다 — 지금처럼 민트 헤어라인을 화면 전체 패널마다
반복하는 밀도를 더 늘리지 않고, 새로 확보할 10%는 오직 Amber에만 배정한다.

---

## 5. Layer 1 — UI Color System 설계

새 토큰(제안, 미적용):

| 토큰 | 값(제안) | 용도 |
|---|---|---|
| `--color-action` | `rgb(255, 160, 70)` | Primary Button 배경, 활성 탭 밑줄, 선택된 아코디언 헤더 |
| `--color-action-soft` | `rgba(255, 160, 70, 0.16)` | Secondary Button 배경, hover 배경 |
| `--color-action-text-on-fill` | `rgb(35, 20, 10)` | Action 배경 위 텍스트(어두운 색으로 대비 확보 — `.mainmenu-next`가 이미 쓰는 패턴과 동일) |
| `--color-action-danger` | `rgb(220, 90, 90)`(`status-dead`와 동일 값 재사용) | Danger Button 전용 |

`--color-button-secondary`(고아 토큰)는 이 체계로 흡수하거나 폐기 대상으로 `DESIGN.md`에
명시한다.

기존 `--color-brand-gold`는 **버튼 fill 용도로는 더 이상 쓰지 않는다.** 지금처럼
"선택 강조/화폐성 텍스트"로만 계속 쓴다(`tree-node--maxed`, `border-selected` 등 기존 사용처는
유지 — 그건 버튼이 아니라 상태 표시라 축돌이 없다).

## 6. Layer 2 — Game Semantic Color System 설계

핵심 발견: **이 레이어에 필요한 색은 이미 대부분 존재한다.** 새로 만들 필요가 없고, 기존
Severity 4색의 "국가 데이터 전용"이라는 좁은 정의를 "게임 사건 전반"으로 넓히는 재정의가
필요할 뿐이다.

| 재정의 | 기존 토큰 | 새 의미(전염병 플레이어 관점) |
|---|---|---|
| Red | `--color-status-dead`(rgb 220,90,90) | 전염병에게 유리 — 신규 국가 감염, 감염자/사망자 증가, 전파 성공, 내성 획득 |
| Blue | `--color-status-info`(rgb 140,210,255) | 인류에게 유리 — 치료제 개발, 의료 수준 향상, 국경 봉쇄, 감염 감소 |
| Orange | `--color-status-infected`(rgb 255,170,90) | 주의 — 의료 과부하, 국가 불안정, 경계 단계 |
| Gray | `--color-text-tertiary`/`--color-text-secondary` | 중립 정보 — 일반 뉴스, 통계, 안내 |

`--color-status-danger`(rgb 255,140,120)는 국가 상세 팝업 등 기존 용도(위험 수치 강조)로
그대로 남긴다.

**중요 원칙(재천명)**: Red/Blue/Orange/Gray는 절대 버튼에 쓰지 않는다. Amber(Layer 1)와
색상 거리가 이미 멀기 때문에 두 레이어가 물리적으로 겹칠 일은 없지만, 이후 새 화면을 만들
때도 이 경계를 명시적으로 지킨다.

---

## 7. Button System 설계

### Primary Button
가장 강한 CTA(다음 단계, 확인, 발원 국가 선택 등 화면당 1개 내외).

```
배경: --color-action
텍스트: --color-action-text-on-fill, bold
테두리: 1px --color-action (배경과 동색 또는 살짝 밝게)
radius: 0 (Tactical 원칙 유지)
```

### Secondary Button
보조 액션(취소, 뒤로가기, 닫기 등 Primary와 나란히 놓이는 경우).

```
배경: --color-bg-panel (또는 transparent)
텍스트: --color-action
테두리: 1px --color-action
radius: 0
```

`tab-button--secondary`(현재 회색 `--color-button-secondary`)를 이 정의로 교체하는 것을
포함한다.

### Danger Button
파괴적/되돌릴 수 없는 확인(게임 포기, 리셋 등).

```
배경: --color-action-danger
텍스트: --color-text-primary(백색), bold
테두리: 1px --color-action-danger
radius: 0
```

### 상태 매트릭스

| 상태 | 규칙 |
|---|---|
| Default | 위 표의 배경/테두리/텍스트 |
| Hover | (모바일은 호버 없음, 에디터/PC 확인용) 배경 밝기 +8~10% 또는 테두리를 `--color-action`보다 한 단계 밝은 톤으로 |
| Pressed(`:active`) | 기존 전역 규칙 그대로 승계 — `scale: 0.96 0.96`, **배경색은 건드리지 않는다**(`Theme.uss`의 기존 `Button:active` 규칙과 완전히 호환) |
| Disabled | 기존 전역 규칙 그대로 승계 — `opacity: 0.5`(`Theme.uss`의 기존 `Button:disabled`와 완전히 호환) |

Pressed/Disabled는 이미 `Theme.uss`에 전역으로 정의돼 있어 **손댈 필요가 없다** — Button
System은 Default 배경/테두리/텍스트 3종(Primary/Secondary/Danger)만 새로 정의하면 기존
인터랙션 규칙과 자동으로 맞물린다. 이번 설계가 기존 시스템과 충돌하지 않는 지점이다.

## 8. Accent Color를 구조물에 유지할지, 버튼까지 확장할지

**결론: 구조물(테두리·코너컷)에 유지한다. 버튼으로 확장하지 않는다.**

이유:
1. 위 3-1 분석대로, 민트를 버튼까지 확장하면 구조와 액션이 같은 색이 되어 위계가
   무너진다.
2. 현재 `tactical-panel`/`corner-cut`/`data-row`가 이미 화면 전체에서 민트 헤어라인을
   반복 사용 중이다 — 이 반복 밀도 자체가 "이 게임의 톤"을 만드는 시그니처이므로, 여기에
   새로운 의미(버튼)를 얹으면 오히려 기존 시그니처의 신뢰도가 떨어진다.
3. Amber를 별도로 두면 "민트 = 이 화면이 전술 콘솔이다(분위기)", "앰버 = 지금 이걸
   누를 수 있다(행동)"로 역할이 완전히 나뉘어 두 요소가 서로를 방해하지 않고 각자 강화된다.

---

## 9. DESIGN.md 수정 방향 (제안 — 이번 작업 범위 아님)

실제 반영 시 다음 절을 개정한다:

1. **`colors:` frontmatter**에 `action`/`action-soft`/`action-text-on-fill`/`action-danger`
   4개 토큰 추가. `--color-button-secondary`는 흡수 또는 폐기 명시.
2. **"브랜드 악센트 3색 고정" 규칙 문구 개정** — "브랜드 악센트(Layer 2, 자원/게임 의미) 3색
   고정 + Action Color(Layer 1, UI 행동) 1색은 별도 축"으로 명확화. 3색 고정 원칙 자체는
   깨지 않는다(Action Color는 브랜드 축이 아니라 새 축이므로).
3. **Status Semantics 표(4축 표)에 5번째 축 추가** — "UI Action | action/action-soft/
   action-danger | 버튼/탭/선택 등 행동 유도 | Layer 2(게임 의미)와 색상 공유 금지".
4. **Component Library에 Button 계열 신설** — `primary-button`/`secondary-button`/
   `danger-button` 3종 정의, 기존 `tab-button`/`mainmenu-next`/`ending-button`/
   `popup-footer-button` 등 산발적 버튼 클래스를 이 공용 컴포넌트로 통합하는 리팩터링
   경로 명시.
5. **Interaction Rules는 그대로 유지** — 이미 "배경색은 건드리지 않는다" 원칙이 있어 신규
   Button System과 충돌하지 않는다.

## 10. 실제 적용 우선순위

| 순위 | 대상 | 근거 |
|---|---|---|
| 1 | Button System 정의(토큰 + Primary/Secondary/Danger 공용 클래스) | 나머지 모든 적용의 전제 |
| 2 | `popup-close` / `status-close-btn` / `mainmenu-back` | §1 조사에서 확인된 실제 회색 버튼 노출 지점 — 가장 눈에 띄는 결함 |
| 3 | HUD(`tab-button`, `tab-button--secondary`) | 고아 토큰 `--color-button-secondary` 정리 겸 상시 노출 영역 |
| 4 | Research Database(연구 시작/취소 버튼) | 아직 미구현 상태라 처음부터 새 Button System으로 구현 가능 — 가장 깨끗한 적용 지점 |
| 5 | CountryStatusPanel 대륙 접기/펼치기 헤더 | 현재 버튼 요소인지 별도 위젯인지 코드 확인 필요(이번 조사 범위 밖) — 적용 전 실제 구현 확인 선행 |

---

## 11. 최종 결론

**Contagion Project에 가장 적합한 Action Color는 Amber(주황 계열, 제안값
`rgb(255, 160, 70)`)다.**

이유를 한 문장으로 요약하면: 이 프로젝트는 이미 "차가운 전술 콘솔(다크 배경 + 민트
헤어라인)"이라는 톤을 완성해 뒀고, 여기에 필요한 건 그 톤을 더 강화하는 색이 아니라
**그 톤과 대비되어 "지금 조작 가능한 지점"만 즉시 튀어 보이게 하는 색**이다. Mint를 버튼까지
확장하면 이미 포화된 구조색 위에 액션을 얹는 셈이라 식별성이 떨어지고, Red는 이미 이
게임에서 "전염병에게 유리한 사건"이라는 강한 의미를 선점하고 있어 버튼에 쓰면 오해를
낳는다. Amber는 ① 구조색과 색상환상 대비가 가장 크고, ② 게임 의미색(Red/Blue/Orange)과도
겹치지 않으며, ③ 이미 존재하는 Gold 계열의 톤을 자연스럽게 확장한 것이라 도입 비용이
가장 낮고, ④ Frostpunk류 "차가운 생존 환경 속 따뜻한 조작 신호"라는 이 장르의 검증된
관례와도 맞아떨어진다. Red는 Danger Button이라는 좁은 예외에서만, 그것도 게임 의미
Red(전염병 유리)와는 값을 공유하되 문맥(버튼 vs 뉴스피드)으로 구분해 사용한다.
