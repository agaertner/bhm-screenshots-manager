using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Nekres.Screenshot_Manager
{
    internal static class BitmapExtensions
    {
        public static Bitmap Fit(this Bitmap source, Size size)
        {
            if (source.Size.Equals(size)) {
                return source;
            }

            float scale = Math.Min(size.Width / source.Width, size.Height / source.Height);

            int newHeight = Convert.ToInt32(source.Width * scale);
            int newWidth = Convert.ToInt32(source.Height * scale);
            var newBitmap = new Bitmap(newWidth, newHeight);
            using (var gfx = Graphics.FromImage(newBitmap))
            {
                gfx.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gfx.SmoothingMode = SmoothingMode.HighQuality;
                gfx.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gfx.CompositingQuality = CompositingQuality.HighQuality;
                gfx.Clear(Color.Transparent);
                gfx.DrawImage(source, 0, 0, newWidth, newHeight);
                gfx.Flush();
                gfx.Save();
            }
            source.Dispose();
            return newBitmap;
        }

        public static async Task SaveOnNetworkShare(this Image image, string fileName, ImageFormat imageFormat) {
            if (image == null) throw new ArgumentNullException(nameof(image));
            try {
                using var lMemoryStream = new MemoryStream();
                image.Save(lMemoryStream, imageFormat);

                using var lFileStream = new FileStream(fileName, FileMode.Create);
                lMemoryStream.Position = 0;

                await lMemoryStream.CopyToAsync(lFileStream);
            } catch (Exception ex) {
                ScreenshotManagerModule.Logger.Warn(ex, ex.Message);
            }
        }
        public static void SaveToClipboard(this Image image, ImageFormat imageFormat) {
            try {
                var dataObject = new System.Windows.Forms.DataObject();
                dataObject.SetData(System.Windows.Forms.DataFormats.Bitmap, true, image);
                using (var stream = new MemoryStream()) {
                    image.Save(stream, imageFormat);
                    stream.Position = 0;
                    dataObject.SetData(imageFormat.ToString(), false, stream);
                }
                System.Windows.Forms.Clipboard.SetDataObject(dataObject, true);
            } catch (Exception ex) {
                ScreenshotManagerModule.Logger.Warn(ex, ex.Message);
            }
        }

        public static Bitmap CompressToTargetSize(this Bitmap bitmap, long maxBytes) {
            if (bitmap   == null) throw new ArgumentNullException(nameof(bitmap));
            if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));

            var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                                            .First(e => e.FormatID == ImageFormat.Jpeg.Guid);

            int    minQ     = 1;
            int    maxQ     = 100;
            byte[] bestData = null;

            while (minQ <= maxQ) {
                int       q             = (minQ + maxQ) / 2;
                using var ms            = new MemoryStream();
                var       encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, q);
                bitmap.Save(ms, jpegEncoder, encoderParams);

                if (ms.Length > maxBytes) {
                    maxQ = q - 1; // too big, reduce quality
                } else {
                    bestData = ms.ToArray(); // fits, try higher quality
                    minQ     = q + 1;
                }
            }

            if (bestData == null)
                throw new Exception("Cannot compress bitmap below target size");

            using var resultStream = new MemoryStream(bestData);
            return new Bitmap(resultStream);
        }
    }
}
