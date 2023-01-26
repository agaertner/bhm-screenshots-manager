﻿using System;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nekres.Screenshot_Manager.UI.Controls
{
    internal sealed class ConfirmationPrompt : Container
    {
        private AsyncTexture2D _bgTexture;
        private static ConfirmationPrompt _singleton;

        private static BitmapFont _font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size24, ContentService.FontStyle.Regular);

        private Rectangle _confirmButtonBounds;
        private Rectangle _cancelButtonBounds;
        private Rectangle _challengeTextBoxBounds;

        private StandardButton _confirmButton;
        private StandardButton _cancelButton;
        private TextBox _challengeTextBox;

        private readonly Action<bool> _callback;
        private readonly string _text;
        private readonly string _confirmButtonText;
        private readonly string _cancelButtonButtonText;
        private readonly string _challengeText;

        private ConfirmationPrompt(Action<bool> callback, string text, string confirmButtonText, string cancelButtonText, string challengeText)
        {
            _callback = callback;
            _text = text;
            _confirmButtonText = confirmButtonText;
            _cancelButtonButtonText = cancelButtonText;
            _challengeText = challengeText;
            this.ZIndex = 999;
            GameService.Input.Keyboard.KeyPressed += OnKeyPressed;

            this.LoadTextures();
        }

        private void LoadTextures() {
            _bgTexture = GameService.Content.DatAssetCache.GetTextureFromAssetId(156003);
        }

        public static void ShowPrompt(Action<bool> callback, string text, string confirmButtonText = "Confirm", string cancelButtonText = "Cancel", string challengeText = "")
        {
            if (_singleton != null) {
                return;
            }

            _singleton = new ConfirmationPrompt(callback, text, confirmButtonText, cancelButtonText, challengeText)
            {
                Parent = Graphics.SpriteScreen,
                Location = Point.Zero,
                Size = Graphics.SpriteScreen.Size
            };
            _singleton.Show();
        }

        private void Confirm() {
            if (_confirmButton == null || !_confirmButton.Enabled) {
                return;
            }
            GameService.Input.Keyboard.KeyPressed -= OnKeyPressed;
            GameService.Content.PlaySoundEffectByName("button-click");
            _singleton = null;
            _callback(true);
            this.Dispose();
        }

        private void Cancel() {
            GameService.Input.Keyboard.KeyPressed -= OnKeyPressed;
            GameService.Content.PlaySoundEffectByName("button-click");
            _singleton = null;
            _callback(false);
            this.Dispose();
        }

        private void OnKeyPressed(object o, KeyboardEventArgs e) {
            switch (e.Key) {
                case Keys.Enter:
                    this.Confirm();
                    break;
                case Keys.Escape:
                    this.Cancel();
                    break;
                default: return;
            }
        }

        protected override void DisposeControl() {
            _singleton = null;
            _bgTexture?.Dispose();
            GameService.Input.Keyboard.KeyPressed -= OnKeyPressed;
            base.DisposeControl();
        }

        private void CreateButtons()
        {
            if (_confirmButton == null)
            {
                _confirmButton = new StandardButton
                {
                    Parent = this,
                    Text = _confirmButtonText,
                    Size = _confirmButtonBounds.Size,
                    Location = _confirmButtonBounds.Location,
                    Enabled = string.IsNullOrEmpty(_challengeText)
                };
                _confirmButton.Click += (_, _) =>
                {
                    this.Confirm();
                };
            }

            if (_cancelButton == null)
            {
                _cancelButton = new StandardButton
                {
                    Parent = this,
                    Text = _cancelButtonButtonText,
                    Size = _cancelButtonBounds.Size,
                    Location = _cancelButtonBounds.Location
                };
                _cancelButton.Click += (_, _) =>
                {
                    this.Cancel();
                };
            }
        }

        private void CreateTextInput()
        {
            if (_challengeTextBox != null) return;
            _challengeTextBox = new TextBox
            {
                Parent = this,
                Size = _challengeTextBoxBounds.Size,
                Location = _challengeTextBoxBounds.Location,
                Font = _font,
                Focused = true
            };
            _challengeTextBox.TextChanged += (o, _) =>
            {
                _confirmButton.Enabled = ((TextBox)o).Text.Equals(_challengeText);
            };
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintBeforeChildren(spriteBatch, bounds);

            var textSize = _font.MeasureString(_text);

            // Darken background outside container
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.8f);

            // Calculate background bounds
            var bgTextureSize = new Point((int)textSize.Width + 12, (int)textSize.Height + (!string.IsNullOrEmpty(_challengeText) ? 125 : 60));
            var bgTexturePos = new Point((bounds.Width - bgTextureSize.X) / 2, (bounds.Height - bgTextureSize.Y) / 2);
            var bgBounds = new Rectangle(bgTexturePos, bgTextureSize);

            // Draw border
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bgBounds.X - 1, bgBounds.Y - 1, bgBounds.Width + 1, 1), Color.Black);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bgBounds.X - 1, bgBounds.Y - 1, 1, bgBounds.Height + 1), Color.Black);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bgBounds.X + bgBounds.Width, bgBounds.Y, 1, bgBounds.Height + 1), Color.Black);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bgBounds.X, bgBounds.Y + bgBounds.Height, bgBounds.Width, 1), Color.Black);

            // Draw Background
            spriteBatch.DrawOnCtrl(this, _bgTexture, bgBounds, new Rectangle(29, 23, 942, 942), Color.White);

            // Draw text
            spriteBatch.DrawStringOnCtrl(this, _text, _font, new Rectangle(bgBounds.X + 6, bgBounds.Y + 5, bgBounds.Width - 11, bgBounds.Height), Color.White, true, HorizontalAlignment.Left, VerticalAlignment.Top);

            // Set button bounds
            _confirmButtonBounds = new Rectangle(bgBounds.Left + 5, bgBounds.Bottom - 50, 100, 45);
            _cancelButtonBounds = new Rectangle(_confirmButtonBounds.Right + 10, _confirmButtonBounds.Y, 100, 45);

            if (!string.IsNullOrEmpty(_challengeText))
            {
                _challengeTextBoxBounds = new Rectangle(_confirmButtonBounds.X, _confirmButtonBounds.Y - 55, bgBounds.Width - 10, 45);
                this.CreateTextInput();
            }

            this.CreateButtons();
        }
    }
}
