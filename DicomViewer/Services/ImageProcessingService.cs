using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using DicomViewer.Models;

namespace DicomViewer.Services
{
    public class ImageProcessingService
    {
        public WriteableBitmap RenderDicomImage(DicomFile dicomFile, double windowWidth, double windowCenter)
        {
            var image = new DicomImage(dicomFile.Dataset);
            image.WindowWidth = windowWidth;
            image.WindowCenter = windowCenter;

            using (var iImage = image.RenderImage())
            {
                var sharpImage = iImage.As<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Bgra32>>();
                if (sharpImage == null) return null;

                int width = image.Width;
                int height = image.Height;

                byte[] bgraPixels = new byte[width * height * 4];
                sharpImage.CopyPixelDataTo(bgraPixels);

                var writeableBitmap = new WriteableBitmap(
                    new PixelSize(width, height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);

                using (var lockedBitmap = writeableBitmap.Lock())
                {
                    Marshal.Copy(bgraPixels, 0, lockedBitmap.Address, bgraPixels.Length);
                }

                return writeableBitmap;
            }
        }

        public WriteableBitmap CreateBitmapFromPixels(byte[] rgbaPixels, int width, int height)
        {
            var writeableBitmap = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using (var lockedBitmap = writeableBitmap.Lock())
            {
                Marshal.Copy(rgbaPixels, 0, lockedBitmap.Address, rgbaPixels.Length);
            }

            return writeableBitmap;
        }

        public async Task<VolumeData> BuildVolumeAsync(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0) return null;

            int depth = filePaths.Count;
            int width = 0;
            int height = 0;
            short[] volumeData = null;

            for (int z = 0; z < depth; z++)
            {
                var file = await Task.Run(() => DicomFile.Open(filePaths[z]));

                var pixelData = DicomPixelData.Create(file.Dataset);

                if (volumeData == null)
                {
                    width = pixelData.Width;
                    height = pixelData.Height;
                    volumeData = new short[width * height * depth];
                }

                if (pixelData.BitDepth.BitsAllocated == 16)
                {
                     var data = pixelData.GetFrame(0).Data;
                     short[] slicePixels = new short[data.Length / 2];
                     Buffer.BlockCopy(data, 0, slicePixels, 0, data.Length);

                     double intercept = file.Dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
                     double slope = file.Dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);

                     int sliceOffset = z * width * height;

                     for(int i = 0; i < slicePixels.Length; i++)
                     {
                         short val = (short)(slicePixels[i] * slope + intercept);
                         volumeData[sliceOffset + i] = val;
                     }
                }
            }

            if(width == 0 || height == 0) return null;

            return new VolumeData(width, height, depth) { Data = volumeData };
        }
    }
}
