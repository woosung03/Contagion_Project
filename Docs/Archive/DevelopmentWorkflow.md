# Development Workflow

AI 개발 파이프라인 기반 프로젝트의 문서 운영 규칙

---

# 목적

프로젝트 문서는 역할별로 분리한다.

CLAUDE.md를 프로젝트 위키로 사용하지 않는다.

Claude가 세션 시작 시 최소한의 정보만 읽고 현재 상태를 빠르게 파악할 수 있도록 유지한다.

---

# 문서 역할

## CLAUDE.md

현재 프로젝트 상태와 다음 작업만 기록한다.

포함 가능:

- 현재 구현된 시스템
- 현재 진행 중인 작업
- 현재 우선순위 TODO
- 프로젝트 전역 규칙
- 작업에 필요한 핵심 제약사항

포함 금지:

- 구현 과정
- 버그 분석
- 설계 논의
- 시행착오
- 튜닝 수치
- 플레이테스트 목록
- 배포 체크리스트
- Unity 에디터 작업 목록
- 세부 구현 설명

질문 기준:

> 이 정보가 현재 상태 또는 다음 작업인가?

YES → CLAUDE.md

NO → 다른 문서

---

## DevLog.md

개발 과정 기록 문서

기록 대상:

- Step별 작업 내역
- 구현 과정
- 설계 변경 이유
- 버그 원인 분석
- 디버깅 기록
- 시행착오
- 성능 최적화 과정
- 문제 해결 과정

질문 기준:

> 왜 이렇게 구현되었는가?

YES → DevLog.md

---

## GameDesignDocument.md

게임 설계 문서

기록 대상:

- 게임 규칙
- 감염 공식
- 치료제 공식
- 밸런스 설계
- 경제 시스템
- 자원 시스템
- AI 설계
- 튜닝 파라미터

질문 기준:

> 게임이 어떻게 동작하는가?

YES → GameDesignDocument.md

---

## UI_Design.md

UI 설계 문서

기록 대상:

- Design Token
- 색상 체계
- Typography
- Layout 규칙
- UI 패턴
- 공통 컴포넌트
- Tactical Design System

질문 기준:

> UI를 어떻게 만들어야 하는가?

YES → UI_Design.md

---

## QA_Checklist.md

검증 문서

기록 대상:

- 기능 확인
- 플레이테스트
- 텍스트 잘림 확인
- 버튼 동작 확인
- 성능 검증
- 프레임 드랍 확인
- UI 검증

질문 기준:

> 이게 제대로 동작하는지 확인해야 하는가?

YES → QA_Checklist.md

---

## Release_Checklist.md

배포 문서

기록 대상:

- 저작권 확인
- 라이선스 확인
- 스토어 등록
- SDK 최종 검증
- 출시 전 점검

질문 기준:

> 배포 전에 확인해야 하는가?

YES → Release_Checklist.md

---

## unity-editor-task.md

Unity 에디터 작업 문서

기록 대상:

- 프리팹 연결
- SerializedField 연결
- UIDocument 연결
- Scene 배선
- Build Settings 수정
- Unity Editor 수동 작업

질문 기준:

> 코드 수정이 아니라 Unity Editor에서 해야 하는 작업인가?

YES → unity-editor-task.md

---

# 문서 분류 규칙

새 정보를 기록하기 전에 반드시 판단한다.

질문:

> 이 정보가 현재 상태 또는 다음 작업인가?

YES

→ CLAUDE.md

NO

→ 적절한 문서로 이동

---

# CLAUDE.md 유지 규칙

작업 완료 후:

1. 현재 상태 갱신
2. 완료된 TODO 제거
3. 새 TODO 추가
4. 구현 과정은 DevLog.md 기록
5. QA 항목은 QA_Checklist.md 기록
6. 배포 항목은 Release_Checklist.md 기록
7. Unity Editor 작업은 unity-editor-task.md 기록

---

# CLAUDE.md 크기 관리

목표:

- 권장 100줄 이하
- 최대 150줄

150줄을 초과하면 정리 대상으로 판단한다.

정리 우선순위:

1. 구현 과정 → DevLog.md
2. 게임 설계 → GameDesignDocument.md
3. UI 설계 → UI_Design.md
4. QA 항목 → QA_Checklist.md
5. 배포 항목 → Release_Checklist.md
6. 에디터 작업 → unity-editor-task.md

---

# 원칙

코드는 Source of Truth다.

구현 세부사항은 코드에서 확인한다.

CLAUDE.md는 브리핑 문서다.

작업 이력은 Git과 DevLog.md가 담당한다.

설계는 GameDesignDocument.md가 담당한다.

검증은 QA_Checklist.md가 담당한다.

CLAUDE.md는 현재 상태와 다음 작업만 유지한다.