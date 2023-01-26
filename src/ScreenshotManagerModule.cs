using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nekres.Screenshot_Manager.Core;
using Nekres.Screenshot_Manager.Properties;
using Nekres.Screenshot_Manager.UI.Models;
using Nekres.Screenshot_Manager.UI.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using Blish_HUD.Extended;
using Nekres.Screenshot_Manager.UI.Controls;

namespace Nekres.Screenshot_Manager
{
    public enum ImageFormatExt {
        Jpeg,
        Png,
        Bmp
    }

    [Export(typeof(Module))]
    public class ScreenshotManagerModule : Module
    {
        internal static readonly Logger Logger = Logger.GetLogger(typeof(ScreenshotManagerModule));

        internal static ScreenshotManagerModule ModuleInstance;

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        [ImportingConstructor]
        public ScreenshotManagerModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(
            moduleParameters)
        {
            ModuleInstance = this;
        }

        #region Settings

        internal SettingEntry<KeyBinding> ScreenshotNormalBinding;
        //internal SettingEntry<KeyBinding> ScreenshotStereoscopicBinding;

        internal SettingEntry<bool> MuteSound;
        internal SettingEntry<bool> DisableNotification;
        internal SettingEntry<bool> SendToRecycleBin;
        internal SettingEntry<bool> HideCornerIcon;
        internal SettingEntry<List<string>> Favorites;
        internal SettingEntry<ImageFormatExt> Format;
        #endregion

        private Texture2D _icon64;
        //private Texture2D _icon128;

        public SoundEffect ScreenShotSfx { get; private set; }

        private SoundEffect[] _deleteSfx;
        public SoundEffect DeleteSfx => _deleteSfx[RandomUtil.GetRandom(0, 1)];

        private CornerIcon _moduleCornerIcon;
        private WindowTab _moduleTab;
        private FileWatcherFactory _fileWatcherFactory;

        public const int FILE_TIME_OUT_MILLISECONDS = 10000;

        protected override void DefineSettings(SettingCollection settings)
        {
            var generalOptions = settings.AddSubCollection("general", true, false, () => Resources.General_Options);
            HideCornerIcon = generalOptions.DefineSetting("hideCornerIcon", false,
                () => Resources.Hide_Corner_Icon,
                () => Resources.Disables_the_corner_icon_in_the_navigation_menu_);

            var soundOptions = settings.AddSubCollection("audio", true, false, () => Resources.Sound_Options);

            MuteSound = soundOptions.DefineSetting("muteSound", false, 
                () => Resources.Mute_Screenshot_Sound,
                () => Resources.Mutes_the_sound_alert_when_a_new_screenshot_has_been_captured_);

            DisableNotification = soundOptions.DefineSetting("disableNotification", false,
                () => Resources.Disable_Screenshot_Notification,
                () => Resources.Disables_the_notification_when_a_new_screenshot_has_been_captured_);

            var screenshotOptions = settings.AddSubCollection("capture", true, false, () => Resources.Screenshot_Options);
            ScreenshotNormalBinding = screenshotOptions.DefineSetting("normalKey", new KeyBinding(Keys.F10),
                () => Resources.Normal, 
                () => Resources.Take_a_normal_screenshot_);
            /*ScreenshotStereoscopicBinding = capture.DefineSetting("stereoscopicKey", new KeyBinding(Keys.None),
                () => Resources.Stereoscopic, 
                () => Resources.Take_a_stereoscopic_screenshot_);*/
            Format = screenshotOptions.DefineSetting("imageFormat", ImageFormatExt.Png,
                () => Resources.Image_Format,
                () => Resources.Choose_your_preferred_image_format_);
            SendToRecycleBin = screenshotOptions.DefineSetting("sendToRecycleBin", true,
                () => Resources.Delete_sends_to_Recycle_Bin,
                () => Resources.By_default__screenshots_are_sent_to_the_Recycle_Bin_so_that_they_can_be_recovered_if_needed__nWhen_this_feature_is_disabled__deleted_screenshots_are_removed_from_the_hard_disk_and_their_space_is_marked_as_overwriteable_);

            var selfManagedSettings = settings.AddSubCollection("ManagedSettings", false, false);
            Favorites = selfManagedSettings.DefineSetting("favorites", new List<string>());
        }

        protected override void Initialize()
        {
            _fileWatcherFactory = new FileWatcherFactory();
            LoadTextures();

            _moduleTab = GameService.Overlay.BlishHudWindow.AddTab(Name, _icon64,
                () => new ScreenshotManagerView(new ScreenshotManagerModel(_fileWatcherFactory)));

            CreateOrDisposeCornerIcon(HideCornerIcon.Value);
        }

        public void CreateOrDisposeCornerIcon(bool dispose)
        {
            if (dispose)
            {
                _moduleCornerIcon?.Dispose();
                return;
            }

            _moduleCornerIcon = new CornerIcon
            {
                IconName = Name,
                Icon = _icon64,
                Priority = Name.GetHashCode()
            };
            _moduleCornerIcon.Click += ModuleCornerIconClicked;
        }

        private void OnHideCornerIconSettingChanged(object o, ValueChangedEventArgs<bool> e) => CreateOrDisposeCornerIcon(e.NewValue);

        private void LoadTextures()
        {
            ScreenShotSfx = ContentsManager.GetSound(@"audio\screenshot.wav");
            _deleteSfx = new[]
            {
                ContentsManager.GetSound(@"audio\crumbling-paper-1.wav"),
                ContentsManager.GetSound(@"audio\crumbling-paper-2.wav")
            };

            _icon64 = ContentsManager.GetTexture("screenshots_icon_64x64.png");
            //_icon128 = ContentsManager.GetTexture("screenshots_icon_128x128.png");
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            ScreenshotNormalBinding.Value.Enabled = true;
            HideCornerIcon.SettingChanged += OnHideCornerIconSettingChanged;
            ScreenshotNormalBinding.Value.Activated += OnScreenshotNormalBindingActivated;
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        protected override void Update(GameTime gameTime)
        {
        }

        private void OnScreenshotNormalBindingActivated(object o, EventArgs e) {
            System.Drawing.Imaging.ImageFormat format;
            string ext;

            switch (this.Format.Value) {
                case ImageFormatExt.Jpeg:
                    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    ext = "jpg";
                    break;
                case ImageFormatExt.Png:
                    format = System.Drawing.Imaging.ImageFormat.Png;
                    ext = "png";
                    break;
                case ImageFormatExt.Bmp:
                    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    ext = "bmp";
                    break;
                default: return;
            }

            //new Regex(@"^gw(?<count>[0-9][0-9][0-9]+)\.(png|jpg|bmp)");

            var name = FileUtil.IndexedFilename(Path.Combine(DirectoryUtil.ScreensPath, "gw"), ext);

            WindowUtil.GetInnerBounds(GameService.GameIntegration.Gw2Instance.Gw2WindowHandle, out var bounds);

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height)) {
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    g.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), System.Drawing.Point.Empty, new Size(bounds.Size.X, bounds.Size.Y));
                }
                bitmap.Save(name, format);
            }
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            ResponsiveThumbnail.DisposeTextures();

            HideCornerIcon.SettingChanged -= OnHideCornerIconSettingChanged;
            ScreenshotNormalBinding.Value.Activated -= OnScreenshotNormalBindingActivated;
            _fileWatcherFactory.Dispose();
            if (_moduleCornerIcon != null) {
                _moduleCornerIcon.Click -= ModuleCornerIconClicked;
                _moduleCornerIcon.Dispose();
            }
            GameService.Overlay.BlishHudWindow.RemoveTab(_moduleTab);
            foreach (var sfx in _deleteSfx) sfx?.Dispose();
            ScreenShotSfx?.Dispose();
            // All static members must be manually unset
            ModuleInstance = null;
        }

        private void ModuleCornerIconClicked(object o, MouseEventArgs e)
        {
            if (GameService.Overlay.BlishHudWindow.Visible)
            {
                GameService.Overlay.BlishHudWindow.Hide();
                return;
            }
            GameService.Overlay.BlishHudWindow.Show();
            GameService.Overlay.BlishHudWindow.Navigate(new ScreenshotManagerView(new ScreenshotManagerModel(_fileWatcherFactory)));
        }
    }
}