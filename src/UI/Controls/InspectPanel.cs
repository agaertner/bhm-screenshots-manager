using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Blish_HUD.Input;
using MonoGame.Extended.BitmapFonts;
using Blish_HUD.Extended;

namespace Nekres.Screenshot_Manager.UI.Controls
{
    internal sealed class InspectPanel : Panel
    {
        private const int BORDER_WIDTH = 10;

        private static readonly BitmapFont     _font        = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);
        private readonly        AsyncTexture2D _texture;
        private                 Rectangle      _textureBounds;
        private                 Rectangle      _borderBounds;
        private readonly        string         _label;

        public InspectPanel(AsyncTexture2D texture, string label)
        {
            _texture        = texture;
            _label          = label;
            Parent          = GameService.Graphics.SpriteScreen;
            Size            = GameService.Graphics.SpriteScreen.Size;
            ZIndex          = 120;
            ShowTint        = true;

            _texture.TextureSwapped += OnTextureSwapped;
            GameService.Graphics.SpriteScreen.Resized += OnSpriteScreenResized;
        }

        private void OnSpriteScreenResized(object sender, ResizedEventArgs e) {
            Size = e.CurrentSize;
            if (_texture.HasTexture) {
                _textureBounds = _texture.Texture.Bounds.ScaleTo(this.LocalBounds, 0.8f, true);
                _borderBounds  = new Rectangle(_textureBounds.X, _textureBounds.Y, _textureBounds.Width + BORDER_WIDTH, _textureBounds.Height + BORDER_WIDTH);
            }
        }

        protected override void OnClick(MouseEventArgs e)
        {
            base.OnClick(e);
            this.Dispose();
        }

        protected override void DisposeControl()
        {
            GameService.Graphics.SpriteScreen.Resized -= OnSpriteScreenResized;
            _texture.Dispose();
            base.DisposeControl();
        }

        public void OnTextureSwapped(object o, EventArgs e)
        {
            _textureBounds = _texture.Texture.Bounds.ScaleTo(this.LocalBounds, 0.8f, true);
            _borderBounds  = new Rectangle(_textureBounds.X, _textureBounds.Y, _textureBounds.Width + BORDER_WIDTH, _textureBounds.Height + BORDER_WIDTH);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintBeforeChildren(spriteBatch, bounds);

            // Darken background outside container
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.5f);

            if (_texture.HasTexture) {
                spriteBatch.DrawOnCtrl(this, _texture.Texture, _textureBounds);
                spriteBatch.DrawRectangleOnCtrl(this, _borderBounds, BORDER_WIDTH, Color.Black);
            } else {
                LoadingSpinnerUtil.DrawLoadingSpinner(this,spriteBatch, bounds);
            }
            spriteBatch.DrawStringOnCtrl(this, $"\u201c{_label}\u201d", _font, new Rectangle(0, HEADER_HEIGHT, this.Width, HEADER_HEIGHT), Color.White, false, true, 3, HorizontalAlignment.Center);
        }
    }
}
