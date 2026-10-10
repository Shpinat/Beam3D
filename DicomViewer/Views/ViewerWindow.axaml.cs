using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DicomViewer.ViewModels;
using System;
using Avalonia;

namespace DicomViewer.Views;

public partial class ViewerWindow : Window
{
    private bool _isPanning = false;
    private Point _lastMousePosition;

    public ViewerWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is ViewerViewModel vm)
        {
            vm.OpenMprWindowAction = series =>
            {
                var mprVm = new MprViewModel();
                var window = new MprWindow { DataContext = mprVm };
                window.Show();
                _ = mprVm.InitializeAsync(series);
            };

            vm.Open3DWindowAction = series =>
            {
                var volVm = new Volume3DViewModel();
                var window = new Volume3DWindow { DataContext = volVm };
                window.Show();
                _ = volVm.InitializeAsync(series);
            };
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastMousePosition = e.GetPosition(this);
            e.Pointer.Capture((IInputElement)sender);
        }
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (_isPanning)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastMousePosition;

            var image = this.FindControl<Image>("MainImage");
            if (image != null && image.RenderTransform is TransformGroup group)
            {
                foreach(var transform in group.Children)
                {
                    if (transform is TranslateTransform translateTransform)
                    {
                        translateTransform.X += delta.X;
                        translateTransform.Y += delta.Y;
                    }
                }
            }

            _lastMousePosition = currentPosition;
        }
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }
    }

    private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        var image = this.FindControl<Image>("MainImage");
        if (image != null && image.RenderTransform is TransformGroup group)
        {
            foreach(var transform in group.Children)
            {
                if (transform is ScaleTransform scaleTransform)
                {
                    double zoomFactor = 1.1;
                    if (e.Delta.Y > 0)
                    {
                        scaleTransform.ScaleX *= zoomFactor;
                        scaleTransform.ScaleY *= zoomFactor;
                    }
                    else
                    {
                        scaleTransform.ScaleX /= zoomFactor;
                        scaleTransform.ScaleY /= zoomFactor;
                    }
                }
            }
        }
    }
}
