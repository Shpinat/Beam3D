using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using DicomViewer.Models;

namespace DicomViewer.Services
{
    public class DicomLoaderService
    {
        public async Task<List<DicomSeriesModel>> LoadFilesAsync(IEnumerable<string> filePaths)
        {
            var seriesDict = new Dictionary<string, DicomSeriesModel>();

            foreach (var path in filePaths)
            {
                try
                {
                    var file = await DicomFile.OpenAsync(path);
                    var dataset = file.Dataset;

                    string seriesUID = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);

                    if (!seriesDict.ContainsKey(seriesUID))
                    {
                        seriesDict[seriesUID] = new DicomSeriesModel
                        {
                            PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, "Unknown Patient"),
                            StudyDescription = dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, "Unknown Study"),
                            SeriesDescription = dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription, "Unknown Series"),
                            SeriesInstanceUID = seriesUID
                        };
                    }
                    seriesDict[seriesUID].FilePaths.Add(path);
                }
                catch (Exception)
                {
                    // Ignore non-DICOM files or errors
                }
            }

            // Sort files in each series
            foreach (var series in seriesDict.Values)
            {
                series.FilePaths = SortDicomFiles(series.FilePaths);
            }

            return seriesDict.Values.ToList();
        }

        private List<string> SortDicomFiles(List<string> filePaths)
        {
            var fileInfos = new List<(string Path, double Position, int InstanceNumber)>();

            foreach (var path in filePaths)
            {
                try
                {
                    var file = DicomFile.Open(path);
                    var dataset = file.Dataset;

                    int instanceNumber = dataset.GetSingleValueOrDefault(DicomTag.InstanceNumber, 0);

                    double position = 0;
                    if (dataset.Contains(DicomTag.ImagePositionPatient))
                    {
                        var positionValues = dataset.GetValues<double>(DicomTag.ImagePositionPatient);
                        if (positionValues != null && positionValues.Length == 3)
                        {
                            // Typically sort by Z position for axial slices
                            position = positionValues[2];
                        }
                    }

                    fileInfos.Add((path, position, instanceNumber));
                }
                catch
                {
                    fileInfos.Add((path, 0, 0));
                }
            }

            // Sort primarily by InstanceNumber, then by ImagePosition
            var sortedInfos = fileInfos
                .OrderBy(x => x.InstanceNumber)
                .ThenBy(x => x.Position)
                .Select(x => x.Path)
                .ToList();

            return sortedInfos;
        }
    }
}
