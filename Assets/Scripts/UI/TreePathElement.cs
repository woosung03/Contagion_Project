using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Contagion.UI
{
    /// <summary>
    /// Painter2D로 트리 경로/합류선을 그리는 커스텀 엘리먼트 — UpgradeTreeView 캔버스 스파이크
    /// (Docs/DevLog.md 참고: UI Toolkit에 네이티브 라인 프리미티브가 없어 generateVisualContent +
    /// Painter2D로 직접 그린다). 좌표 계산·색상 결정은 전부 호출부(UpgradeTreeView)가 담당하고,
    /// 이 클래스는 주어진 세그먼트를 그대로 그리기만 한다.
    /// </summary>
    public class TreePathElement : VisualElement
    {
        public readonly struct LineSegment
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;
            public readonly Color Color;
            public readonly float Width;

            public LineSegment(Vector2 start, Vector2 end, Color color, float width)
            {
                Start = start;
                End = end;
                Color = color;
                Width = width;
            }
        }

        private readonly List<LineSegment> _segments = new List<LineSegment>();

        public TreePathElement()
        {
            // 선 레이어가 그 위/아래 노드의 클릭을 가로채면 안 된다(캔버스 스파이크 위험 요소 #2).
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetSegments(IReadOnlyList<LineSegment> segments)
        {
            _segments.Clear();
            _segments.AddRange(segments);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_segments.Count == 0) return;

            var painter = mgc.painter2D;
            foreach (var seg in _segments)
            {
                painter.strokeColor = seg.Color;
                painter.lineWidth = seg.Width;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(seg.Start);
                painter.LineTo(seg.End);
                painter.Stroke();
            }
        }
    }
}
