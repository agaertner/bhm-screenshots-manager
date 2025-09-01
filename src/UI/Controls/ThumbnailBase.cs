using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.IO;

namespace Nekres.Screenshot_Manager.UI.Controls
{
    public class ThumbnailBase : Container
    {
        private static BitmapFont _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);

        private AsyncTexture2D _texture;
        public AsyncTexture2D Texture {
            get => _texture;
            set {
                _texture?.Dispose();
                SetProperty(ref _texture, value);
            }
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value ?? string.Empty);
        }

        public ThumbnailBase(AsyncTexture2D texture, string fileName)
        {
            _texture = texture;
            FileName = fileName;
        }

        protected override void DisposeControl()
        {
            _texture?.Dispose();
            base.DisposeControl();
        }

        public bool TryLoadImage(out Texture2D texture) {
            try {
                using (var stream = File.OpenRead(FileName)) {
                    using (var ctx = GameService.Graphics.LendGraphicsDeviceContext()) {
                        texture = Texture2D.FromStream(ctx.GraphicsDevice, stream);
                    }
                }
                return true;
            } catch (Exception ex) {
                ScreenshotManagerModule.Logger.Warn(ex, $"Failed to copy image to clipboard: {FileName}"); 
                texture = null; 
                return false;
            }
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Darken background
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black);
            if (_texture.HasTexture)
            {
                spriteBatch.DrawOnCtrl(this, _texture.Texture, _texture.Texture.Bounds.ScaleTo(bounds, 1, true));
                spriteBatch.DrawStringOnCtrl(this, 
                    Path.GetExtension(this.FileName).TrimStart('.').ToUpperInvariant(), _font, 
                    new Rectangle(bounds.X + 10, bounds.Y + 3, bounds.Width - 20, bounds.Height - 6), 
                    new Color(Color.Gray, 0.5f), false, false, 1, HorizontalAlignment.Left, VerticalAlignment.Top);
            }
            else
                LoadingSpinnerUtil.DrawLoadingSpinner(this, spriteBatch, bounds);
        }
    }
}
