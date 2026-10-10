using System.Collections.Generic;

namespace DicomViewer.Models
{
    public class DicomSeriesModel
    {
        public string PatientName { get; set; } = string.Empty;
        public string StudyDescription { get; set; } = string.Empty;
        public string SeriesDescription { get; set; } = string.Empty;
        public string SeriesInstanceUID { get; set; } = string.Empty;

        public List<string> FilePaths { get; set; } = new();

        public override string ToString()
        {
            return $"{PatientName} - {StudyDescription} - {SeriesDescription} ({FilePaths.Count} images)";
        }
    }
}
