using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nekres.Screenshot_Manager
{
    internal static class BitmapExtensions
    {
        public static Bitmap Fit(this Bitmap source, Size size)
        {
            if (source.Size.Equals(size))
                return source;

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
            try {
                using var lMemoryStream = new MemoryStream();
                image.Save(lMemoryStream, imageFormat);

                using var lFileStream = new FileStream(fileName, FileMode.Create);
                lMemoryStream.Position = 0;

                await lMemoryStream.CopyToAsync(lFileStream);
            } catch (Exception ex) when (ex is ExternalException or UnauthorizedAccessException or IOException) {
                ScreenshotManagerModule.Logger.Warn(ex, ex.Message);
            }
        }
    }
}
