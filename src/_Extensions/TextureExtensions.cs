using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using System.Drawing.Imaging;
namespace Nekres.Screenshot_Manager
{
    public static class TextureExtensions
    {
        public static Bitmap ToBitmap(this Texture2D texture) {
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }

            int width  = texture.Width;
            int height = texture.Height;

            // Get pixel data from the texture (ARGB format).
            var pixelData = new Microsoft.Xna.Framework.Color[width * height];
            texture.GetData(pixelData);

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            // Lock the bitmap bits.
            var bmpData = bitmap.LockBits(
                                          new Rectangle(0, 0, width, height),
                                          ImageLockMode.WriteOnly,
                                          PixelFormat.Format32bppArgb);
            unsafe {
                byte* dstPtr = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++) {
                    byte* row = dstPtr + (y * bmpData.Stride);
                    for (int x = 0; x < width; x++) {
                        var color = pixelData[y * width + x];
                        // XNA Color is in RGBA, but GDI+ expects BGRA
                        row[x * 4 + 0] = color.B;
                        row[x * 4 + 1] = color.G;
                        row[x * 4 + 2] = color.R;
                        row[x * 4 + 3] = color.A;
                    }
                }
            }
            bitmap.UnlockBits(bmpData);
            return bitmap;
        }
    }
}
