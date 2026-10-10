using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DicomViewer.Models;
using DicomViewer.Services;
using System;
using System.Linq;

namespace DicomViewer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DicomLoaderService _loaderService;

    [ObservableProperty]
    private ObservableCollection<DicomSeriesModel> _seriesList = new();

    [ObservableProperty]
    private DicomSeriesModel _selectedSeries;

    // Action to handle window opening request from VM
    public Action<DicomSeriesModel> OpenViewerWindowAction;

    public MainViewModel()
    {
        _loaderService = new DicomLoaderService();
    }

    [RelayCommand]
    private void OpenViewer(DicomSeriesModel series)
    {
        if (series != null)
        {
            OpenViewerWindowAction?.Invoke(series);
        }
    }

    public async Task LoadFilesAsync(string[] filePaths)
    {
        var loadedSeries = await _loaderService.LoadFilesAsync(filePaths);
        foreach (var series in loadedSeries)
        {
            // If same series exists, replace it, else add
            var existing = SeriesList.FirstOrDefault(s => s.SeriesInstanceUID == series.SeriesInstanceUID);
            if(existing != null)
            {
                SeriesList.Remove(existing);
            }
            SeriesList.Add(series);
        }
    }
}
