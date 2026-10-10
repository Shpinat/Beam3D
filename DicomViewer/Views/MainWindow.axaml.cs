using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DicomViewer.ViewModels;
using System.Linq;

namespace DicomViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // This is needed for binding to #MainWindowRoot in XAML if we set x:Name
        this.SetValue(NameProperty, "MainWindowRoot");
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.OpenViewerWindowAction = series =>
            {
                var viewerVm = new ViewerViewModel();
                viewerVm.Initialize(series);

                var viewerWindow = new ViewerWindow
                {
                    DataContext = viewerVm
                };
                viewerWindow.Show();
            };
        }
    }

    private async void OnLoadFilesClicked(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select DICOM Files",
            AllowMultiple = true
        });

        if (files.Count >= 1 && DataContext is MainViewModel vm)
        {
            var paths = files.Select(f => f.Path.LocalPath).ToArray();
            await vm.LoadFilesAsync(paths);
        }
    }
}
