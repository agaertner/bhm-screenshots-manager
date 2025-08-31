using System;
using Microsoft.Xna.Framework;

namespace Nekres.Screenshot_Manager
{
    internal static class RectangleExtensions
    {

        public static Rectangle ScaleTo(this Rectangle source, Rectangle bounds, float scaleFactor = 1.0f, bool center = false) {
            if (source.Equals(bounds) || source.Width == 0 || source.Height == 0) {
                return source;
            }
            float scale = Math.Min(bounds.Width / (float)source.Width, bounds.Height / (float)source.Height);
            scale = Math.Min(scale * scaleFactor, 1.0f);
            int newWidth  = (int)(source.Width  * scale);
            int newHeight = (int)(source.Height * scale);
            var scaledSrc = new Rectangle(source.X, source.Y, newWidth, newHeight);
            return center ? scaledSrc.Center(bounds) : scaledSrc;
        }

        public static Rectangle Center(this Rectangle source, Rectangle bounds) {
            if (source.Equals(bounds)) {
                return source;
            }
            int newX = (bounds.Width - source.Width) / 2;
            int newY = (bounds.Height - source.Height) / 2;
            return new Rectangle(newX, newY, source.Width, source.Height);
        }
    }
}
