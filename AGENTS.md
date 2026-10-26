# Agent Collaboration & Development Guidelines

## 1. Collaboration Roles
- **Antigravity (Gemini)**: Responsible for overall architecture design, cross-platform alignment, complex business logic, multi-file data flow analysis, and logical reasoning for deep bug fixes.
- **Cursor (Composer/Tab)**: Responsible for visual UI/UX layout and styling in WinForms/WPF Designer, fixing real-time typos and syntax errors, and fast code completion within the current block.
- **Always respond in Korean**. Keep code blocks, variable names, and technical terms in English.

## 2. Autonomous Execution & Self-Debugging Protocol
[목표]
요청받은 기능 추가 또는 버그 수정

[실행 프로토콜]
1. 계획 및 코드 작성: 요구사항에 맞게 코드를 수정한다.
2. 자동 검증: 수정한 뒤 반드시 터미널 명령(`msbuild BarcodePrint.sln` 또는 관련 빌드 명령어)을 직접 실행해서 결과를 확인한다.
3. 자가 디버깅 루프:
   - 빌드나 컴파일 중 에러/경고가 발생하면 사용자에게 묻지 말고 오류 로그를 스스로 분석해서 코드를 자체 수정한다.
   - 모든 빌드가 성공(Exit Code 0)할 때까지 수정 및 빌드 검증 과정을 자율 반복한다.
4. 완료 보고: 최종 빌드가 완전히 통과했을 때만 수정 결과와 변경된 파일 목록을 보고한다.

# Autonomous Execution Rule
- 기능 구현이나 버그 수정 요청을 받으면 코드 수정 후 터미널 빌드를 즉시 실행할 것.
- 빌드 실패 시 컴파일 오류 로그를 스스로 파싱하고 해결할 때까지 코드를 다시 수정할 것.
- 빌드가 완전히 성공하기 전에는 작업을 완료 처리하지 말 것.
