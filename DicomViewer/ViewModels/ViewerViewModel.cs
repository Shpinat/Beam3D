using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FellowOakDicom;
using DicomViewer.Models;
using DicomViewer.Services;
using System;

namespace DicomViewer.ViewModels;

public partial class ViewerViewModel : ViewModelBase
{
    private readonly ImageProcessingService _imageProcessingService;
    private DicomSeriesModel _currentSeries;
    private List<DicomFile> _dicomFiles = new();

    [ObservableProperty]
    private WriteableBitmap? _displayImage;

    [ObservableProperty]
    private int _sliceIndex;

    [ObservableProperty]
    private int _maxSliceIndex;

    [ObservableProperty]
    private double _windowWidth = 400;

    [ObservableProperty]
    private double _windowCenter = 40;

    [ObservableProperty]
    private string? _patientInfo;

    public Action<DicomSeriesModel> OpenMprWindowAction;
    public Action<DicomSeriesModel> Open3DWindowAction;

    public ViewerViewModel()
    {
        _imageProcessingService = new ImageProcessingService();
    }

    public void Initialize(DicomSeriesModel series)
    {
        _currentSeries = series;
        _dicomFiles.Clear();

        if (series.FilePaths.Count > 0)
        {
            MaxSliceIndex = series.FilePaths.Count - 1;

            // Load all files (for a real app, you might lazy load)
            foreach(var path in series.FilePaths)
            {
                _dicomFiles.Add(DicomFile.Open(path));
            }

            // Extract window/level from first slice
            var firstDataset = _dicomFiles[0].Dataset;
            if (firstDataset.Contains(DicomTag.WindowWidth))
                WindowWidth = firstDataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 400.0);
            if (firstDataset.Contains(DicomTag.WindowCenter))
                WindowCenter = firstDataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 40.0);

            PatientInfo = $"Name: {series.PatientName}\nStudy: {series.StudyDescription}\nSeries: {series.SeriesDescription}";

            SliceIndex = 0;
            UpdateImage();
        }
    }

    partial void OnSliceIndexChanged(int value)
    {
        UpdateImage();
    }

    partial void OnWindowWidthChanged(double value)
    {
        UpdateImage();
    }

    partial void OnWindowCenterChanged(double value)
    {
        UpdateImage();
    }

    private void UpdateImage()
    {
        if (_dicomFiles.Count == 0 || SliceIndex < 0 || SliceIndex >= _dicomFiles.Count)
            return;

        var file = _dicomFiles[SliceIndex];
        DisplayImage = _imageProcessingService.RenderDicomImage(file, WindowWidth, WindowCenter);
    }

    [RelayCommand]
    private void OpenMpr()
    {
        OpenMprWindowAction?.Invoke(_currentSeries);
    }

    [RelayCommand]
    private void Open3D()
    {
        Open3DWindowAction?.Invoke(_currentSeries);
    }
}
