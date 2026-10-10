namespace DicomViewer.Models
{
    public class VolumeData
    {
        public short[] Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }

        public double SpacingX { get; set; } = 1.0;
        public double SpacingY { get; set; } = 1.0;
        public double SpacingZ { get; set; } = 1.0;

        public VolumeData(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            Data = new short[width * height * depth];
        }
    }
}
