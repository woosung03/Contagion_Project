# 프로젝트 구조 참고

코드/에셋을 어디에 두고 어떻게 찾는지에 대한 참고 문서. 새 스크립트나 UI 에셋을 추가할 때,
또는 기존 코드 위치를 찾을 때만 열어본다. (CLAUDE.md에서 이동 — 폴더/코드 구조 설명은
CLAUDE.md에 두지 않는다.)

---

## 엔진 / 렌더 파이프라인

- Unity 6000.3.10f1 (URP 템플릿)
- 렌더 파이프라인: URP (프로젝트 기본 템플릿 그대로 사용)

## 코드 네임스페이스

- `Contagion.Data`
- `Contagion.Managers`
- `Contagion.Gameplay`
- `Contagion.UI`
- `Contagion.Ads`
- `Contagion.Ranking`
- `Contagion.Utils`

## 폴더 위치

| 종류 | 경로 |
|---|---|
| 스크립트 | `Assets/Scripts/{Data, Managers, Gameplay, UI, Ads, Ranking, Utils}` |
| UI 에셋 (UI Toolkit) | `Assets/UI/*.uxml`, `Assets/UI/*.uss` |
| 국가 데이터 | `Assets/New Folder/CountryDatabase.asset` |

## 데이터 규모

- 국가 48개, 병원체 6종, 업그레이드 트리 45노드 (`DefaultUpgradeTreeFactory.cs`)
- ScriptableObject 기반: `CountryDatabase` / `PathogenDefinition` / `UpgradeTreeDatabase`,
  `GameDataBootstrapper`가 씬 시작 시 주입
