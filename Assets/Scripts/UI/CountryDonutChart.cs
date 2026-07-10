using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// 국가 감염 상태(건강/감염/사망) 3분할 도넛 차트. Docs/CountryStatus_Dashboard_Investigation.md
    /// 3절 "감염 상태 원그래프" 1차 구현분 — UI Toolkit엔 내장 차트 위젯이 없어 <see cref="HudSparkline"/>과
    /// 동일한 방식(VisualElement.generateVisualContent + Painter2D 직접 그리기)을 재사용했다.
    ///
    /// 실제 도넛(고리) 모양은 Painter2D가 "내부 반지름을 뺀 부채꼴" 경로를 직접 지원하지 않아,
    /// 1) 3개 부채꼴(파이 조각)을 먼저 꽉 채워 그리고 2) 그 위에 패널 배경색과 같은 원을 덮어
    /// 가운데를 "뚫는" 방식으로 구현한다(오버레이 트릭). CountryPopup.popup-root의 배경색
    /// (--color-bg-panel, rgba(15,15,25,0.95))과 정확히 같은 색을 <see cref="HoleColor"/>로 써야
    /// 이 트릭이 자연스럽게 보인다 — 배경색이 바뀌면 이 상수도 같이 바꿔야 한다.
    ///
    /// Unity 에디터 미접속으로 실제 렌더링 확인은 못 했다(이 프로젝트의 다른 Painter2D 사용처와
    /// 같은 제약, Docs/DevLog.md Step 75 참고) — 남은 검증은 Docs/QA_Checklist.md에 추가할 것.
    /// </summary>
    public class CountryDonutChart
    {
        /// <summary>CountryPopup.uss .popup-root 배경색과 반드시 일치시켜야 하는 "구멍" 색.</summary>
        public static readonly Color HoleColor = new Color(15f / 255f, 15f / 255f, 25f / 255f, 0.95f);

        private readonly VisualElement _target;
        private readonly Color _healthyColor;
        private readonly Color _infectedColor;
        private readonly Color _deadColor;

        private float _healthy;
        private float _infected;
        private float _dead;

        public CountryDonutChart(VisualElement target, Color healthyColor, Color infectedColor, Color deadColor)
        {
            _target = target;
            _healthyColor = healthyColor;
            _infectedColor = infectedColor;
            _deadColor = deadColor;
            _target.generateVisualContent += Draw;
        }

        /// <summary>세 값(건강/감염/사망)을 갱신하고 다시 그리도록 요청한다. 값 자체는 비율 계산에만
        /// 쓰이므로 인구수 스케일(long) 그대로 넘겨도 된다.</summary>
        public void SetValues(long healthy, long infected, long dead)
        {
            _healthy = Mathf.Max(0f, healthy);
            _infected = Mathf.Max(0f, infected);
            _dead = Mathf.Max(0f, dead);
            _target.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext mgc)
        {
            float total = _healthy + _infected + _dead;
            if (total <= 0f) return;

            float width = _target.contentRect.width;
            float height = _target.contentRect.height;
            if (width <= 0f || height <= 0f || float.IsNaN(width) || float.IsNaN(height)) return;

            var center = new Vector2(width / 2f, height / 2f);
            float outerRadius = Mathf.Min(width, height) / 2f;
            float innerRadius = outerRadius * 0.55f; // 도넛 두께 45% — HUD 스파크라인과 마찬가지로 고정 비율

            var painter = mgc.painter2D;

            float healthyDeg = 360f * (_healthy / total);
            float infectedDeg = 360f * (_infected / total);
            // 사망 조각은 나머지 전체를 채운다(부동소수 오차로 3개 각도 합이 360을 못 채워 빈 틈이
            // 생기는 것을 방지 — 조각 3개 중 마지막은 항상 "시작각 -> 360"으로 닫는다).
            DrawSlice(painter, center, outerRadius, 0f, healthyDeg, _healthyColor);
            DrawSlice(painter, center, outerRadius, healthyDeg, healthyDeg + infectedDeg, _infectedColor);
            DrawSlice(painter, center, outerRadius, healthyDeg + infectedDeg, 360f, _deadColor);

            // 가운데를 패널 배경색 원으로 덮어 "도넛" 모양을 만든다.
            painter.fillColor = HoleColor;
            painter.BeginPath();
            painter.Arc(center, innerRadius, new Angle(0f, AngleUnit.Degree), new Angle(360f, AngleUnit.Degree));
            painter.ClosePath();
            painter.Fill();
        }

        private static void DrawSlice(Painter2D painter, Vector2 center, float radius, float startDeg, float endDeg, Color color)
        {
            if (endDeg - startDeg <= 0.05f) return; // 값이 0인 조각은 그리지 않음(불필요한 겹침 방지)

            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.Arc(center, radius, new Angle(startDeg, AngleUnit.Degree), new Angle(endDeg, AngleUnit.Degree));
            painter.LineTo(center);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
