using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ODProxl.ViewModels.Pages;
using System;

namespace ODProxl;

public partial class AnnotationPage : UserControl
{
    #region 縮放相關欄位
    private ScaleTransform? _scaleTransform;
    private ScrollViewer? _scrollViewer;
    private Border? _zoomContainer;
    private double _zoomFactor = 1.0;
    #endregion

    #region 重繪計時器
    private DispatcherTimer? _redrawTimer;
    private bool _needsRedraw;
    #endregion

    #region 建構子與初始化
    public AnnotationPage()
    {
        InitializeComponent();
        _redrawTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _redrawTimer.Tick += (_, _) =>
        {
            if (_needsRedraw && DataContext is AnnotationPageViewModel vm)
            {
                vm.RequestRedraw();
                _needsRedraw = false;
            }
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    #endregion

    #region DataContext 變更處理
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not AnnotationPageViewModel vm) return;

        _scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");
        _zoomContainer = this.FindControl<Border>("ZoomContainer");
        _scaleTransform = null;
        if (_zoomContainer?.RenderTransform is ScaleTransform st)
            _scaleTransform = st;

        var image = this.FindControl<Image>("ImageViewer");
        var canvas = this.FindControl<Canvas>("DrawingCanvas");
        if (image != null && canvas != null)
            vm.SetControls(image, canvas);

        vm.RequestResetZoom += ResetZoom;
    }
    #endregion

    #region 縮放功能
    private void ResetZoom()
    {
        _zoomFactor = 1.0;
        if (_scaleTransform != null)
            _scaleTransform.ScaleX = _scaleTransform.ScaleY = 1.0;
        _scrollViewer?.Offset = new Vector(0, 0);
        if (DataContext is AnnotationPageViewModel vm)
            vm.ZoomLevel = 1.0;
    }

    private void UpdateZoom(double delta, Point? mousePos = null)
    {
        if (_scaleTransform == null || _scrollViewer == null) return;
        var oldZoom = _zoomFactor;
        _zoomFactor = Math.Clamp(_zoomFactor + delta, 0.1, 10.0);
        _scaleTransform.ScaleX = _scaleTransform.ScaleY = _zoomFactor;

        if (mousePos.HasValue)
        {
            var oldOffset = _scrollViewer.Offset;
            var mouseOld = new Point(mousePos.Value.X / oldZoom, mousePos.Value.Y / oldZoom);
            var mouseNew = new Point(mousePos.Value.X / _zoomFactor, mousePos.Value.Y / _zoomFactor);
            _scrollViewer.Offset = new Vector(
                oldOffset.X + (mouseNew.X - mouseOld.X) * _zoomFactor,
                oldOffset.Y + (mouseNew.Y - mouseOld.Y) * _zoomFactor);
        }

        if (DataContext is AnnotationPageViewModel vm)
        {
            vm.ZoomLevel = _zoomFactor;
            vm.RequestRedraw();
        }
    }
    #endregion

    #region 座標轉換輔助方法
    private Point GetImagePixelPosition(PointerEventArgs e)
        => e.GetPosition(this.FindControl<Canvas>("DrawingCanvas"));
    #endregion

    #region 畫布事件處理
    private void DrawingCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        UpdateZoom(e.Delta.Y > 0 ? 0.1 : -0.1, e.GetPosition(_zoomContainer));
        e.Handled = true;
    }

    private void DrawingCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not AnnotationPageViewModel vm) return;
        var pos = GetImagePixelPosition(e);
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            vm.OnPointerPressedRight(pos);
        else
            vm.OnPointerPressed(pos);
    }

    private void DrawingCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (DataContext is not AnnotationPageViewModel vm) return;
        var pos = GetImagePixelPosition(e);
        vm.OnPointerMoved(pos);
        _needsRedraw = true;
        _redrawTimer?.Start();
    }

    private void DrawingCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not AnnotationPageViewModel vm) return;
        vm.OnPointerReleased(GetImagePixelPosition(e));
    }

    private void DrawingCanvas_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is AnnotationPageViewModel vm)
        {
            vm.FinishPolygonIfPossible();
            e.Handled = true;
        }
    }

    private void DrawingCanvas_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AnnotationPageViewModel vm)
            vm.FinishPolygonIfPossible();
    }
    #endregion
}