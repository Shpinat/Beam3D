using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using DicomViewer.Models;
using DicomViewer.Services;
using System;
using System.Runtime.InteropServices;

namespace DicomViewer.ViewModels;

public partial class MprViewModel : ViewModelBase
{
    private readonly ImageProcessingService _imageProcessingService;
    private VolumeData _volume;

    [ObservableProperty]
    private WriteableBitmap? _axialImage;
    [ObservableProperty]
    private WriteableBitmap? _coronalImage;
    [ObservableProperty]
    private WriteableBitmap? _sagittalImage;

    [ObservableProperty] private int _axialIndex;
    [ObservableProperty] private int _coronalIndex;
    [ObservableProperty] private int _sagittalIndex;

    [ObservableProperty] private int _maxAxialIndex;
    [ObservableProperty] private int _maxCoronalIndex;
    [ObservableProperty] private int _maxSagittalIndex;

    [ObservableProperty] private double _windowWidth = 400;
    [ObservableProperty] private double _windowCenter = 40;

    public MprViewModel()
    {
        _imageProcessingService = new ImageProcessingService();
    }

    public async Task InitializeAsync(DicomSeriesModel series)
    {
        _volume = await _imageProcessingService.BuildVolumeAsync(series.FilePaths);
        if (_volume == null) return;

        MaxAxialIndex = _volume.Depth - 1;
        MaxCoronalIndex = _volume.Height - 1;
        MaxSagittalIndex = _volume.Width - 1;

        AxialIndex = _volume.Depth / 2;
        CoronalIndex = _volume.Height / 2;
        SagittalIndex = _volume.Width / 2;

        AxialImage = new WriteableBitmap(new PixelSize(_volume.Width, _volume.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        CoronalImage = new WriteableBitmap(new PixelSize(_volume.Width, _volume.Depth), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        SagittalImage = new WriteableBitmap(new PixelSize(_volume.Height, _volume.Depth), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

        UpdateAxial();
        UpdateCoronal();
        UpdateSagittal();
    }

    partial void OnAxialIndexChanged(int value) => UpdateAxial();
    partial void OnCoronalIndexChanged(int value) => UpdateCoronal();
    partial void OnSagittalIndexChanged(int value) => UpdateSagittal();
    partial void OnWindowWidthChanged(double value) { UpdateAxial(); UpdateCoronal(); UpdateSagittal(); }
    partial void OnWindowCenterChanged(double value) { UpdateAxial(); UpdateCoronal(); UpdateSagittal(); }

    private void UpdateAxial()
    {
        if (_volume == null || AxialIndex < 0 || AxialIndex >= _volume.Depth || AxialImage == null) return;
        int w = _volume.Width;
        int h = _volume.Height;

        using (var locked = AxialImage.Lock())
        {
            unsafe
            {
                int z = AxialIndex;
                int sliceOffset = z * w * h;
                uint* ptr = (uint*)locked.Address;

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        short val = _volume.Data[sliceOffset + y * w + x];
                        byte color = ApplyWindowLevel(val);
                        // BGRA
                        uint pixel = (uint)(255 << 24 | color << 16 | color << 8 | color);
                        ptr[y * w + x] = pixel;
                    }
                }
            }
        }

        // Trigger a visual update by raising the property changed event
        OnPropertyChanged(nameof(AxialImage));
    }

    private void UpdateCoronal()
    {
        if (_volume == null || CoronalIndex < 0 || CoronalIndex >= _volume.Height || CoronalImage == null) return;
        int w = _volume.Width;
        int d = _volume.Depth;

        using (var locked = CoronalImage.Lock())
        {
            unsafe
            {
                int y = CoronalIndex;
                uint* ptr = (uint*)locked.Address;

                for (int z = 0; z < d; z++)
                {
                    int sliceOffset = z * w * _volume.Height;
                    int displayY = d - 1 - z;
                    for (int x = 0; x < w; x++)
                    {
                        short val = _volume.Data[sliceOffset + y * w + x];
                        byte color = ApplyWindowLevel(val);
                        uint pixel = (uint)(255 << 24 | color << 16 | color << 8 | color);
                        ptr[displayY * w + x] = pixel;
                    }
                }
            }
        }
        OnPropertyChanged(nameof(CoronalImage));
    }

    private void UpdateSagittal()
    {
        if (_volume == null || SagittalIndex < 0 || SagittalIndex >= _volume.Width || SagittalImage == null) return;
        int h = _volume.Height;
        int d = _volume.Depth;

        using (var locked = SagittalImage.Lock())
        {
            unsafe
            {
                int x = SagittalIndex;
                uint* ptr = (uint*)locked.Address;

                for (int z = 0; z < d; z++)
                {
                    int sliceOffset = z * _volume.Width * h;
                    int displayY = d - 1 - z;
                    for (int y = 0; y < h; y++)
                    {
                        short val = _volume.Data[sliceOffset + y * _volume.Width + x];
                        byte color = ApplyWindowLevel(val);
                        uint pixel = (uint)(255 << 24 | color << 16 | color << 8 | color);
                        ptr[displayY * h + y] = pixel;
                    }
                }
            }
        }
        OnPropertyChanged(nameof(SagittalImage));
    }

    private byte ApplyWindowLevel(short hu)
    {
        double min = WindowCenter - WindowWidth / 2.0;
        double max = WindowCenter + WindowWidth / 2.0;

        if (hu <= min) return 0;
        if (hu >= max) return 255;

        return (byte)((hu - min) / WindowWidth * 255.0);
    }
}
