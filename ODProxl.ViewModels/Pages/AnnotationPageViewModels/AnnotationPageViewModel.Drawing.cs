using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace ODProxl.ViewModels.Pages.AnnotationPageViewModels
{
    public partial class AnnotationPageViewModel
    {
        private Point _startPoint;
        private bool _isDragging;
        private Point _currentRectEnd;
        private List<Point> _currentPolygonPoints = new();
        private Point? _tempMovePoint;

        private readonly Dictionary<Annotation, Shape> _annotationElements = new();
        private Shape? _tempShape;

        public void OnPointerPressed(Point imagePixelPos)
        {
            if (!IsPolygonMode)
            {
                _startPoint = _currentRectEnd = imagePixelPos;
                _isDragging = true;
            }
            else
            {
                _currentPolygonPoints.Add(imagePixelPos);
                PolygonPointCount = _currentPolygonPoints.Count;
                _tempMovePoint = imagePixelPos;
            }
            RedrawAllAnnotations();
        }

        public void OnPointerPressedRight(Point imagePixelPos)
        {
            if (IsPolygonMode)
            {
                if (_currentPolygonPoints.Count >= 3)
                    FinishCurrentPolygon();
                else if (_currentPolygonPoints.Count > 0)
                {
                    _currentPolygonPoints.RemoveAt(_currentPolygonPoints.Count - 1);
                    PolygonPointCount = _currentPolygonPoints.Count;
                }
            }
            else
            {
                _isDragging = false;
            }
            RedrawAllAnnotations();
        }

        public void OnPointerMoved(Point imagePixelPos)
        {
            MousePositionText = $"X: {imagePixelPos.X:F1} Y: {imagePixelPos.Y:F1}";
            if (!IsPolygonMode && _isDragging)
                _currentRectEnd = imagePixelPos;
            else if (IsPolygonMode && _currentPolygonPoints.Count > 0)
                _tempMovePoint = imagePixelPos;
        }

        public void OnPointerReleased(Point imagePixelPos)
        {
            if (!IsPolygonMode && _isDragging)
            {
                _currentRectEnd = imagePixelPos;
                AddRectangleAnnotation();
                _isDragging = false;
            }
            RedrawAllAnnotations();
        }

        private void AddRectangleAnnotation()
        {
            if (_startPoint == default || _currentRectEnd == default || SelectedClass == null) return;
            var ann = new Annotation
            {
                Points = new List<Point> { _startPoint, _currentRectEnd },
                IsPolygon = false,
                ClassId = SelectedClass.Id,
                ClassName = SelectedClass.Name
            };
            Annotations.Add(ann);
        }

        private void FinishCurrentPolygon()
        {
            if (_currentPolygonPoints.Count < 3 || SelectedClass == null) return;
            var ann = new Annotation
            {
                Points = new List<Point>(_currentPolygonPoints),
                IsPolygon = true,
                ClassId = SelectedClass.Id,
                ClassName = SelectedClass.Name
            };
            Annotations.Add(ann);
            _currentPolygonPoints.Clear();
            PolygonPointCount = 0;
            _tempMovePoint = null;
        }

        public void FinishPolygonIfPossible()
        {
            if (IsPolygonMode && _currentPolygonPoints.Count >= 3 && SelectedClass != null)
                FinishCurrentPolygon();
        }

        private void CancelCurrentPolygon()
        {
            _currentPolygonPoints.Clear();
            PolygonPointCount = 0;
            _tempMovePoint = null;
            RedrawAllAnnotations();
        }

        public void RedrawAllAnnotations()
        {
            if (_canvas == null) return;
            var currentAnnotations = new HashSet<Annotation>(Annotations);
            foreach (var kv in _annotationElements.ToList())
            {
                if (!currentAnnotations.Contains(kv.Key))
                {
                    _canvas.Children.Remove(kv.Value);
                    _annotationElements.Remove(kv.Key);
                }
            }
            foreach (var ann in Annotations)
            {
                if (!_annotationElements.TryGetValue(ann, out var shape))
                {
                    shape = CreateShapeForAnnotation(ann);
                    _annotationElements[ann] = shape;
                    _canvas.Children.Add(shape);
                }
                else
                {
                    UpdateShape(shape, ann);
                }
            }
            if (_tempShape != null)
                _canvas.Children.Remove(_tempShape);
            _tempShape = CreateTempShape();
            if (_tempShape != null)
                _canvas.Children.Add(_tempShape);
        }

        private Shape CreateShapeForAnnotation(Annotation ann)
        {
            if (ann.IsPolygon && ann.Points.Count >= 3)
            {
                return new Polygon
                {
                    Points = new Points(ann.Points),
                    Stroke = Brushes.Red,
                    StrokeThickness = 3.5,
                    Fill = new SolidColorBrush(Colors.Red, 0.06),
                    StrokeJoin = PenLineJoin.Round
                };
            }
            if (!ann.IsPolygon && ann.Points.Count == 2)
            {
                var p1 = ann.Points[0];
                var p2 = ann.Points[1];
                var rect = new Rectangle
                {
                    Width = Math.Abs(p2.X - p1.X),
                    Height = Math.Abs(p2.Y - p1.Y),
                    Stroke = Brushes.Blue,
                    StrokeThickness = 3.5,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(rect, Math.Min(p1.X, p2.X));
                Canvas.SetTop(rect, Math.Min(p1.Y, p2.Y));
                return rect;
            }
            return new Rectangle();
        }

        private void UpdateShape(Shape shape, Annotation ann)
        {
            if (shape is Polygon poly && ann.IsPolygon)
                poly.Points = new Points(ann.Points);
        }

        private Shape? CreateTempShape()
        {
            if (IsPolygonMode && _currentPolygonPoints.Count > 0)
            {
                var points = new List<Point>(_currentPolygonPoints);
                if (_tempMovePoint.HasValue) points.Add(_tempMovePoint.Value);
                if (points.Count >= 2)
                    return new Polygon
                    {
                        Points = new Points(points),
                        Stroke = Brushes.Orange,
                        StrokeThickness = 3.5,
                        Fill = new SolidColorBrush(Colors.Orange, 0.08),
                        StrokeDashArray = new AvaloniaList<double> { 5, 3 }
                    };
            }
            if (!IsPolygonMode && _isDragging && _currentRectEnd != default)
            {
                var p1 = _startPoint;
                var p2 = _currentRectEnd;
                var rect = new Rectangle
                {
                    Width = Math.Abs(p2.X - p1.X),
                    Height = Math.Abs(p2.Y - p1.Y),
                    Stroke = Brushes.Lime,
                    StrokeThickness = 3.5,
                    StrokeDashArray = new AvaloniaList<double> { 4, 2 },
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(rect, Math.Min(p1.X, p2.X));
                Canvas.SetTop(rect, Math.Min(p1.Y, p2.Y));
                return rect;
            }
            return null;
        }

        public class LocalClass
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class Annotation
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public bool IsPolygon { get; set; }
            public List<Point> Points { get; set; } = new();
            public string DisplayText => IsPolygon ? $"多邊形 [{ClassName}]" : $"矩形 [{ClassName}]";
        }
    }
}
