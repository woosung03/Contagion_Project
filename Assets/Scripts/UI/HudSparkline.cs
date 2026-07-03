using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// HUD 스탯(감염자/사망자/치료제)이 텍스트로만 표시돼 가시성이 떨어진다는 피드백으로 추가한
    /// 인라인 스파크라인 그래프. UI Toolkit엔 내장 차트 위젯이 없어서 UpgradeTreeView의 선행조건
    /// 연결선과 같은 방식(Painter2D 직접 그리기)을 재사용했다.
    ///
    /// 최근 N개 샘플(기본 하루 1개, <see cref="MaxSamples"/>일치)을 그 구간의 최솟값~최댓값으로
    /// 자동 스케일링해서 그린다 — 감염자 수처럼 절대값이 수백만까지 커져도 그래프 높이를 항상 꽉
    /// 채워서 추세(늘고 있는지 줄고 있는지)가 한눈에 보이게 하는 게 목적이라, 절대 수치 축은 굳이
    /// 표시하지 않는다(정확한 수치는 옆의 텍스트 라벨이 이미 담당).
    /// </summary>
    public class HudSparkline
    {
        private const int MaxSamples = 200;

        private readonly VisualElement _target;
        private readonly Color _lineColor;
        private readonly Color _fillColor;
        private readonly List<float> _samples = new List<float>();

        public HudSparkline(VisualElement target, Color lineColor)
        {
            _target = target;
            _lineColor = lineColor;
            _fillColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0.2f);
            _target.generateVisualContent += Draw;
        }

        /// <summary>새 샘플을 추가하고 다시 그리도록 요청한다. WorldState가 바뀔 때마다(매 틱=하루) 호출.</summary>
        public void AddSample(float value)
        {
            _samples.Add(value);
            if (_samples.Count > MaxSamples)
                _samples.RemoveAt(0);
            _target.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext mgc)
        {
            if (_samples.Count < 2) return;

            float width = _target.contentRect.width;
            float height = _target.contentRect.height;
            if (width <= 0f || height <= 0f || float.IsNaN(width) || float.IsNaN(height)) return;

            float min = _samples[0];
            float max = _samples[0];
            foreach (var s in _samples)
            {
                if (s < min) min = s;
                if (s > max) max = s;
            }
            float range = Mathf.Max(max - min, 0.0001f);
            int count = _samples.Count;

            Vector2 PointAt(int i)
            {
                float x = width * i / (count - 1);
                float t = (_samples[i] - min) / range;
                float y = height - t * height;
                return new Vector2(x, y);
            }

            var painter = mgc.painter2D;

            // 은은한 채움 영역 — 추세를 더 눈에 띄게.
            painter.fillColor = _fillColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, height));
            for (int i = 0; i < count; i++)
                painter.LineTo(PointAt(i));
            painter.LineTo(new Vector2(width, height));
            painter.ClosePath();
            painter.Fill();

            // 선.
            painter.strokeColor = _lineColor;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.MoveTo(PointAt(0));
            for (int i = 1; i < count; i++)
                painter.LineTo(PointAt(i));
            painter.Stroke();
        }
    }
}
