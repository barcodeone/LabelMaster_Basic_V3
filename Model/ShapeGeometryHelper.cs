using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BarcodePrint.Model
{
    public static class ShapeGeometryHelper
    {
        /// <summary>
        /// 안드로이드(Label_Master) 버전과 100% 동일한 17종 다채로운 도형 지오메트리 생성
        /// </summary>
        public static Geometry CreateShapeGeometry(string shapeType, double w, double h, double strokeThickness = 0, double offsetX = 0, double offsetY = 0)
        {
            if (w <= 0 || h <= 0) return new RectangleGeometry(new Rect(offsetX, offsetY, Math.Max(1, w), Math.Max(1, h)));

            // 선 두께(strokeThickness)의 절반만큼 안쪽으로 인셋하여,
            // Stroke가 Bounding Box 바깥으로 삐져나가 상하좌우가 잘리거나 두께가 불균일해지는 현상 완벽 방지
            double half = Math.Max(0, strokeThickness) / 2.0;
            double sw = (w > strokeThickness) ? (w - strokeThickness) : 1.0;
            double sh = (h > strokeThickness) ? (h - strokeThickness) : 1.0;
            double x = offsetX + (w > strokeThickness ? half : 0);
            double y = offsetY + (h > strokeThickness ? half : 0);

            string type = shapeType != null ? shapeType.ToUpper() : "RECTANGLE";
            double cx = x + sw / 2.0;
            double cy = y + sh / 2.0;

            var pathGeo = new PathGeometry();
            var figure = new PathFigure();

            switch (type)
            {
                case "ROUNDED_RECT":
                    double r = Math.Min(sw, sh) * 0.18;
                    return new RectangleGeometry(new Rect(x, y, sw, sh), r, r);

                case "CIRCLE":
                case "ELLIPSE":
                    return new EllipseGeometry(new Rect(x, y, sw, sh));

                case "TRIANGLE":
                    figure.StartPoint = new Point(cx, y);
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + sh), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "TRIANGLE_INVERTED":
                    figure.StartPoint = new Point(x, y);
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y), true));
                    figure.Segments.Add(new LineSegment(new Point(cx, y + sh), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "DIAMOND":
                    figure.StartPoint = new Point(cx, y);
                    figure.Segments.Add(new LineSegment(new Point(x + sw, cy), true));
                    figure.Segments.Add(new LineSegment(new Point(cx, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(x, cy), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "PENTAGON": // 5각형
                    for (int i = 0; i < 5; i++)
                    {
                        double angle = i * 2 * Math.PI / 5 - Math.PI / 2;
                        double px = cx + (sw / 2.0) * Math.Cos(angle);
                        double py = cy + (sh / 2.0) * Math.Sin(angle);
                        if (i == 0) figure.StartPoint = new Point(px, py);
                        else figure.Segments.Add(new LineSegment(new Point(px, py), true));
                    }
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "HEXAGON": // 6각형
                    figure.StartPoint = new Point(cx, y);
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y + sh * 0.25), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y + sh * 0.75), true));
                    figure.Segments.Add(new LineSegment(new Point(cx, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + sh * 0.75), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + sh * 0.25), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "OCTAGON": // 8각형
                    double cutX = sw * 0.293;
                    double cutY = sh * 0.293;
                    figure.StartPoint = new Point(x + cutX, y);
                    figure.Segments.Add(new LineSegment(new Point(x + sw - cutX, y), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y + cutY), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y + sh - cutY), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw - cutX, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(x + cutX, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + sh - cutY), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + cutY), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "STAR": // 5각 별
                    double rx = sw / 2.0;
                    double ry = sh / 2.0;
                    double rix = rx * 0.42;
                    double riy = ry * 0.42;
                    for (int i = 0; i < 10; i++)
                    {
                        double angle = i * Math.PI / 5 - Math.PI / 2;
                        double rX = (i % 2 == 0) ? rx : rix;
                        double rY = (i % 2 == 0) ? ry : riy;
                        double px = cx + rX * Math.Cos(angle);
                        double py = cy + rY * Math.Sin(angle);
                        if (i == 0) figure.StartPoint = new Point(px, py);
                        else figure.Segments.Add(new LineSegment(new Point(px, py), true));
                    }
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "HEART": // 하트
                    figure.StartPoint = new Point(cx, y + sh * 0.82);
                    figure.Segments.Add(new BezierSegment(
                        new Point(x - sw * 0.1, y + sh * 0.45),
                        new Point(x, y - sh * 0.05),
                        new Point(cx, y + sh * 0.28), true));
                    figure.Segments.Add(new BezierSegment(
                        new Point(x + sw, y - sh * 0.05),
                        new Point(x + sw * 1.1, y + sh * 0.45),
                        new Point(cx, y + sh * 0.82), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "CLOUD": // 구름
                    figure.StartPoint = new Point(x + sw * 0.2, y + sh * 0.7);
                    figure.Segments.Add(new QuadraticBezierSegment(new Point(x, y + sh * 0.5), new Point(x + sw * 0.25, y + sh * 0.3), true));
                    figure.Segments.Add(new QuadraticBezierSegment(new Point(x + sw * 0.35, y), new Point(x + sw * 0.6, y + sh * 0.15), true));
                    figure.Segments.Add(new QuadraticBezierSegment(new Point(x + sw * 0.8, y), new Point(x + sw * 0.85, y + sh * 0.35), true));
                    figure.Segments.Add(new QuadraticBezierSegment(new Point(x + sw, y + sh * 0.4), new Point(x + sw, y + sh * 0.65), true));
                    figure.Segments.Add(new QuadraticBezierSegment(new Point(x + sw, y + sh * 0.85), new Point(x + sw * 0.75, y + sh * 0.85), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.2, y + sh * 0.85), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "SPEECH_BUBBLE": // 말풍선
                    double bubbleRadius = Math.Min(sw, sh) * 0.15;
                    double bodyH = sh * 0.75;
                    var bubbleRect = new RectangleGeometry(new Rect(x, y, sw, bodyH), bubbleRadius, bubbleRadius);
                    var bubbleTail = new PathGeometry();
                    var tailFig = new PathFigure
                    {
                        StartPoint = new Point(x + sw * 0.3, y + bodyH),
                        IsClosed = true
                    };
                    tailFig.Segments.Add(new LineSegment(new Point(x + sw * 0.2, y + sh), true));
                    tailFig.Segments.Add(new LineSegment(new Point(x + sw * 0.5, y + bodyH), true));
                    bubbleTail.Figures.Add(tailFig);
                    return new CombinedGeometry(GeometryCombineMode.Union, bubbleRect, bubbleTail);

                case "ARROW_RIGHT": // 오른쪽 화살표
                    figure.StartPoint = new Point(x, y + sh * 0.32);
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.58, y + sh * 0.32), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.58, y + sh * 0.08), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, cy), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.58, y + sh * 0.92), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.58, y + sh * 0.68), true));
                    figure.Segments.Add(new LineSegment(new Point(x, y + sh * 0.68), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "ARROW_LEFT": // 왼쪽 화살표
                    figure.StartPoint = new Point(x + sw, y + sh * 0.32);
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.42, y + sh * 0.32), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.42, y + sh * 0.08), true));
                    figure.Segments.Add(new LineSegment(new Point(x, cy), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.42, y + sh * 0.92), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.42, y + sh * 0.68), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, y + sh * 0.68), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "ARROW_BOTH": // 양방향 화살표
                    figure.StartPoint = new Point(x + sw * 0.25, y + sh * 0.1);
                    figure.Segments.Add(new LineSegment(new Point(x, cy), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.25, y + sh * 0.9), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.25, y + sh * 0.65), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.75, y + sh * 0.65), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.75, y + sh * 0.9), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, cy), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.75, y + sh * 0.1), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.75, y + sh * 0.35), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw * 0.25, y + sh * 0.35), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                case "CROSS": // 십자가 / 플러스
                    double barW = sw * 0.32;
                    double barH = sh * 0.32;
                    double leftX = x + (sw - barW) / 2.0;
                    double rightX = x + (sw + barW) / 2.0;
                    double topY = y + (sh - barH) / 2.0;
                    double bottomY = y + (sh + barH) / 2.0;
                    figure.StartPoint = new Point(leftX, y);
                    figure.Segments.Add(new LineSegment(new Point(rightX, y), true));
                    figure.Segments.Add(new LineSegment(new Point(rightX, topY), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, topY), true));
                    figure.Segments.Add(new LineSegment(new Point(x + sw, bottomY), true));
                    figure.Segments.Add(new LineSegment(new Point(rightX, bottomY), true));
                    figure.Segments.Add(new LineSegment(new Point(rightX, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(leftX, y + sh), true));
                    figure.Segments.Add(new LineSegment(new Point(leftX, bottomY), true));
                    figure.Segments.Add(new LineSegment(new Point(x, bottomY), true));
                    figure.Segments.Add(new LineSegment(new Point(x, topY), true));
                    figure.Segments.Add(new LineSegment(new Point(leftX, topY), true));
                    figure.IsClosed = true;
                    pathGeo.Figures.Add(figure);
                    return pathGeo;

                default: // RECTANGLE
                    return new RectangleGeometry(new Rect(x, y, sw, sh));
            }
        }

        /// <summary>
        /// 안드로이드(Label_Master) 버전과 100% 동일한 7종 선 스타일 지오메트리 및 비주얼 생성
        /// </summary>
        public static Geometry CreateLineGeometry(string lineStyle, double w, double h, double lineWidth = 1.0, double offsetX = 0, double offsetY = 0)
        {
            string style = lineStyle != null ? lineStyle.ToUpper() : "SOLID";
            double midY = offsetY + h / 2.0;

            if (style == "WAVY" || style == "WAVE")
            {
                var waveGeo = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(offsetX, midY) };
                double waveLen = Math.Max(12.0, w / Math.Max(2.0, Math.Round(w / 16.0)));
                double amp = Math.Min(h / 2.0 - 1.0, Math.Max(3.0, lineWidth * 2.0));

                double curX = offsetX;
                while (curX < offsetX + w)
                {
                    double nextX = Math.Min(offsetX + w, curX + waveLen);
                    double halfX = curX + (nextX - curX) / 2.0;
                    figure.Segments.Add(new BezierSegment(
                        new Point(curX + (halfX - curX) / 2.0, midY - amp),
                        new Point(curX + (halfX - curX) * 1.5, midY + amp),
                        new Point(nextX, midY), true));
                    curX = nextX;
                }
                waveGeo.Figures.Add(figure);
                return waveGeo;
            }
            else if (style == "ZIGZAG")
            {
                var zigGeo = new PathGeometry();
                var figure = new PathFigure { StartPoint = new Point(offsetX, midY) };
                double step = Math.Max(8.0, w / Math.Max(2.0, Math.Round(w / 12.0)));
                double amp = Math.Min(h / 2.0 - 1.0, Math.Max(3.0, lineWidth * 2.0));

                double curX = offsetX;
                bool up = true;
                while (curX < offsetX + w)
                {
                    double nextX = Math.Min(offsetX + w, curX + step / 2.0);
                    double targetY = (curX + step / 2.0 >= offsetX + w) ? midY : (up ? midY - amp : midY + amp);
                    figure.Segments.Add(new LineSegment(new Point(nextX, targetY), true));
                    curX = nextX;
                    up = !up;
                }
                zigGeo.Figures.Add(figure);
                return zigGeo;
            }
            else if (style == "DOUBLE")
            {
                var doubleGeo = new GeometryGroup();
                double gap = Math.Max(2.0, lineWidth * 1.6);
                double line1Y = Math.Max(offsetY, midY - gap / 2.0);
                double line2Y = Math.Min(offsetY + h, midY + gap / 2.0);
                doubleGeo.Children.Add(new LineGeometry(new Point(offsetX, line1Y), new Point(offsetX + w, line1Y)));
                doubleGeo.Children.Add(new LineGeometry(new Point(offsetX, line2Y), new Point(offsetX + w, line2Y)));
                return doubleGeo;
            }
            else
            {
                // SOLID, DASHED, DOTTED, DASH_DOT
                return new LineGeometry(new Point(offsetX, midY), new Point(offsetX + w, midY));
            }
        }

        public static DoubleCollection GetStrokeDashArray(string lineStyle)
        {
            string style = lineStyle != null ? lineStyle.ToUpper() : "SOLID";
            switch (style)
            {
                case "DASHED":
                    return new DoubleCollection { 4, 2 };
                case "DOTTED":
                    return new DoubleCollection { 1, 1.5 };
                case "DASH_DOT":
                    return new DoubleCollection { 4, 2, 1, 2 };
                default:
                    return null;
            }
        }
    }
}
