using CommunityToolkit.Mvvm.ComponentModel;
using DicomViewer.Models;
using DicomViewer.Services;
using System.Threading.Tasks;

namespace DicomViewer.ViewModels;

public partial class Volume3DViewModel : ViewModelBase
{
    private readonly ImageProcessingService _imageProcessingService;
    private readonly MarchingCubesService _marchingCubesService;

    [ObservableProperty]
    private VolumeData _volume;

    [ObservableProperty]
    private string _status = "Loading volume...";

    [ObservableProperty]
    private HelixToolkit.Geometry.MeshGeometry3D _volumeModelGeometry;

    public Volume3DViewModel()
    {
        _imageProcessingService = new ImageProcessingService();
        _marchingCubesService = new MarchingCubesService();
    }

    public async Task InitializeAsync(DicomSeriesModel series)
    {
        Volume = await _imageProcessingService.BuildVolumeAsync(series.FilePaths);

        if (Volume != null)
        {
            Status = $"Volume loaded: {Volume.Width}x{Volume.Height}x{Volume.Depth}. Generating mesh...";

            // Extract the isosurface mesh from the volume data for bones (e.g. HU 300)
            var mesh = await Task.Run(() => _marchingCubesService.GenerateMesh(Volume, 300));

            VolumeModelGeometry = (HelixToolkit.Geometry.MeshGeometry3D)mesh;

            Status = $"Mesh generated. Triangles: {mesh.TriangleIndices.Count / 3}";
        }
        else
        {
            Status = "Failed to load volume";
        }
    }
}
