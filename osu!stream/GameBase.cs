#if iOS || ANDROID
using OpenTK.Graphics.ES11;
#if iOS
using Foundation;
using ObjCRuntime;
using OpenGLES;
#endif

using TextureTarget = OpenTK.Graphics.ES11.All;
using TextureParameterName = OpenTK.Graphics.ES11.All;
using EnableCap = OpenTK.Graphics.ES11.All;
using ArrayCap = OpenTK.Graphics.ES11.All;
using BlendingFactorSrc = OpenTK.Graphics.ES11.All;
using BlendingFactorDest = OpenTK.Graphics.ES11.All;
using PixelStoreParameter = OpenTK.Graphics.ES11.All;
using VertexPointerType = OpenTK.Graphics.ES11.All;
using ColorPointerType = OpenTK.Graphics.ES11.All;
using ClearBufferMask = OpenTK.Graphics.ES11.All;
using TexCoordPointerType = OpenTK.Graphics.ES11.All;
using BeginMode = OpenTK.Graphics.ES11.All;
using DepthFunction = OpenTK.Graphics.ES11.All;
using MatrixMode = OpenTK.Graphics.ES11.All;
using PixelInternalFormat = OpenTK.Graphics.ES11.All;
using PixelFormat = OpenTK.Graphics.ES11.All;
using PixelType = OpenTK.Graphics.ES11.All;
using ShaderType = OpenTK.Graphics.ES11.All;
using VertexAttribPointerType = OpenTK.Graphics.ES11.All;
using ProgramParameter = OpenTK.Graphics.ES11.All;
using ShaderParameter = OpenTK.Graphics.ES11.All;
#if iOS
using CoreGraphics;
using UIKit;
#endif
#else
using OpenTK.Graphics.OpenGL;
#endif
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using osum.AssetManager;
using osum.Audio;
using osum.GameModes;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Input;
using osum.Localisation;
using osum.Support;
using osum.Support.Desktop;
using osum.UI;

namespace osum
{
    public abstract partial class GameBase
    {
        public static GameBase Instance;

        public static pTexture WhitePixel;
        public static Random   Random = new Random();

        /// <summary>
        /// use for input handling, sprites etc.
        /// </summary>
        internal static Vector2 BaseSizeFixedWidth = new Vector2(640, 640 / 1.5f);

        internal static Vector2 BaseSize = new Vector2(640, 640 / 1.5f);
        internal static Vector2 GamefieldBaseSize = new Vector2(512, 384);

        //calculations and internally, all textures are at 960-width-compatible sizes.
        internal const float BASE_SPRITE_RES = 960;

        internal static float SpriteResolution;

        /// <summary>
        /// Ratio of sprite size compared to their default habitat (SpriteResolution)
        /// </summary>
        internal static float SpriteToBaseRatio;

        internal static float SpriteToNativeRatio;

        internal static float ScaleFactor = 1;
        internal static Size NativeSize;

        public static pConfigManager Config;

        /// <summary>
        /// The ratio of actual-pixel window size in relation to the base resolution used internally.
        /// </summary>
        internal static float BaseToNativeRatio;

        /// <summary>
        /// The ratio of actual-pixel window size in relation to the base resolution used internally.
        /// Includes realignment for the range of widths where sprite ratio does not change.
        /// </summary>
        internal static float BaseToNativeRatioAligned;

        internal static Vector2 GamefieldOffsetVector1;

        internal static readonly NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

        public static Scheduler Scheduler = new Scheduler();

        internal virtual bool DisableDimming { get; set; }

        /// <summary>
        /// A list of components which get updated every frame.
        /// </summary>
        public static List<IUpdateable> Components = new List<IUpdateable>();

        /// <summary>
        /// Top-level sprite manager. Draws above everything else.
        /// </summary>
        internal static SpriteManager MainSpriteManager = new SpriteManager();

        //false for tablets etc.
        internal static bool IsHandheld = true;

        //true for iphone 3g etc.
        internal static bool IsSlowDevice = false;

        private readonly OsuMode startupMode;

        internal SerialPort   _cardReaderPort;
        private Thread       _cardReaderThread;
        private BinaryReader _cardReaderBinaryReader;
        public static double LastFrameTime, LastFrameTimeSync, LastUpdateTime;

        public LightingManager _lightingManager;

        private pText updateTimeText, drawTimeText;

        internal pSpriteText creditTimeText;
        private  bool        _oldAuthState = ArcadeUserData.HasAuth;

        public GameBase(OsuMode mode = OsuMode.Unknown)
        {
            startupMode = mode;
            Instance = this;

            CrashHandler.Initialize();

            //initialise config before everything, because it may be used in Initialize() override.
            Config = new pConfigManager(Instance.PathConfig + "osum.cfg");

            Clock.USER_OFFSET = Config.GetValue("offset", 0);
            Clock.UniversalOffsetInput = Config.GetValue("InputOffset", 0);
            Clock.UniversalOffsetMp3 = Config.GetValue("MP3Offset", 50);
            Clock.UniversalOffsetM4A = Config.GetValue("M4AOffset", -20);
        }

        internal static Vector2 BaseSizeHalf => new Vector2(BaseSizeFixedWidth.X / 2, BaseSizeFixedWidth.Y / 2);

        internal static Vector2 GamefieldToStandard(Vector2 vec)
        {
            return (vec + GamefieldOffsetVector1) * (BASE_SPRITE_RES / SpriteResolution);
        }

        internal static Vector2 StandardToGamefield(Vector2 vec)
        {
            //base position is mapped using constant-width 640
            //firstly we need to map this back over variable width
            //*then* remove the offset.
            return vec / (BASE_SPRITE_RES / SpriteResolution) - GamefieldOffsetVector1;
        }

        /// <summary>
        /// MainLoop runs, starts the main loop and calls Initialize when ready.
        /// </summary>
        public abstract void Run();


        public static event VoidDelegate OnScreenLayoutChanged;

        private bool flipView;

        public bool FlipView
        {
            get => flipView;

            set
            {
                if (flipView == value) return;

                flipView = value;
                Instance.SetViewport();
            }
        }

        public virtual void SetViewport()
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            GL.Viewport(0, 0, NativeSize.Width, NativeSize.Height);
            GL.Ortho(0, NativeSize.Width, NativeSize.Height, 0, -1, 1);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
        }

        /// <summary>
        /// Whether we are running on a device the destroys the limits of how wide a screen can be.
        /// </summary>
        public static bool IsSuperWide;

        public virtual void UpdateSpriteResolution()
        {
            float res = 1136;

            float aspectRatio = (float)NativeSize.Width / NativeSize.Height;

            if (aspectRatio > 1.775f)
            {
                res *= aspectRatio / 1.775f;
                IsSuperWide = true;
            }
            else
            {
                IsSuperWide = false;
            }

            SpriteResolution = (int)res;
        }

        /// <summary>
        /// Setup viewport and projection matrix. Should be called after a resolution/orientation change.
        /// </summary>
        public virtual void SetupScreen()
        {
            float aspectRatio = (float)NativeSize.Width / NativeSize.Height;

            //Setup window...
            BaseSizeFixedWidth.Y = BaseSizeFixedWidth.X / aspectRatio;

            GL.Disable(EnableCap.DepthTest);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.Disable(EnableCap.Lighting);
            GL.DepthMask(false);

            SetViewport();

            BaseToNativeRatio = NativeSize.Width / BaseSizeFixedWidth.X;

            int oldResolution = SpriteSheetResolution;

            //define any available sprite sheets here.
            if (NativeSize.Width < 720)
                SpriteSheetResolution = 480;
            else if (NativeSize.Width < 1280)
                SpriteSheetResolution = 960;
            else
                SpriteSheetResolution = 1920;

            //if we are switching to a new sprite sheet (resizing window on PC) let's refresh our textures.
            if (SpriteSheetResolution != oldResolution && oldResolution > 0)
                TextureManager.ReloadAll();

            UpdateSpriteResolution();

            InputToFixedWidthAlign = BASE_SPRITE_RES / SpriteResolution;

            BaseToNativeRatioAligned = BaseToNativeRatio * InputToFixedWidthAlign;

            SpriteToBaseRatio = BaseSizeFixedWidth.X / BASE_SPRITE_RES;
            SpriteToBaseRatioAligned = BaseSizeFixedWidth.X / SpriteResolution;

            BaseSize = new Vector2((NativeSize.Width / BaseToNativeRatioAligned), (NativeSize.Height / BaseToNativeRatioAligned));

            GamefieldOffsetVector1 = new Vector2((BaseSize.X - GamefieldBaseSize.X) / 2,
                Math.Max(31.5f, (BaseSize.Y - GamefieldBaseSize.Y) / 2));

            SpriteToNativeRatio = NativeSize.Width / SpriteResolution;
            //1024x = 1024/1024 = 1
            //960x  = 960/960   = 1
            //480x  = 480/960   = 0.5

#if FULL_DEBUG
            Console.WriteLine("Base Resolution is " + BaseSize + " (fixed: " + BaseSizeFixedWidth + ")");
            Console.WriteLine("Sprite Resolution is " + SpriteResolution + " with SpriteSheet " + SpriteSheetResolution);
            Console.WriteLine("Sprite multiplier is " + SpriteToBaseRatio + " or aligned at " + SpriteToBaseRatioAligned);
#endif

            TriggerLayoutChanged();
        }

        /// <summary>
        /// As per Apple recommendations, we should pre-warm any mode changes before actually displaying to avoid stuttering.
        /// </summary>
        public void Warmup()
        {
            SpriteManager.TexturesEnabled = true;

            SpriteManager.AlphaBlend = false;

            SpriteManager.SetBlending(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.SetBlending(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.AlphaBlend = true;

            SpriteManager.SetBlending(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.SetBlending(BlendingFactorSrc.One, BlendingFactorDest.One);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);

            SpriteManager.SetBlending(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.DrawArrays(BeginMode.TriangleFan, 0, 0);
        }

        /// <summary>
        /// This is where the magic happens.
        /// </summary>
        public virtual void Initialize()
        {
            SetupScreen();

            Warmup();

            InitializeAssetManager();

            TextureManager.Initialize();

            InputManager.Initialize();

            InitializeInput();
            InitializeInactivity();

            if (InputManager.RegisteredSources.Count == 0)
                throw new Exception("No input sources registered");

            BackgroundAudioPlayer music = InitializeBackgroundAudio();
            if (music == null)
                throw new Exception("No background audio manager registered");

            Clock.AudioTimeSource = music;
            Components.Add(music);

            SoundEffectPlayer effect = InitializeSoundEffects();
            if (effect == null)
                throw new Exception("No sound effect player registered");

            Components.Add(effect);

            AudioEngine.Initialize(effect, music);

            WhitePixel = pTexture.FromRawBytes(new byte[]{255, 255, 255, 255}, 1, 1);

            ArcadeUserData.Initialize();


#if !RELEASE
            DebugOverlay.Update();
#endif

#if false
            //benchmark

            string path = SongSelectMode.BeatmapPath + "/Aperture Science Psychoacoustics Laboratory - Want You Gone (Larto).osz2";
            Console.WriteLine(path);

            Player.Beatmap = new osum.GameplayElements.Beatmaps.Beatmap(path);
            Player.Difficulty = osum.GameplayElements.Difficulty.Expert;
            Player.Autoplay = true;

            Director.ChangeMode(OsuMode.Play, null);
#elif false
            //results screen testing

            Player.Beatmap = new GameplayElements.Beatmaps.Beatmap("Beatmaps/Lix - Phantom Ensemble -Ark Trance mix- (Dyaems).osf2");
            Player.Difficulty = GameplayElements.Difficulty.Normal;


            Results.RankableScore = new GameplayElements.Scoring.Score()
            {
                count100 = 55,
                count50 = 128,
                count300 = 387,
                countMiss = 0,
                date = DateTime.Now,
                spinnerBonusScore = 1500,
                comboBonusScore = 578420,
                hitScore = 100000 - 578420,
                maxCombo = 198
            };

            Director.ChangeMode(OsuMode.Results, null);
#else
            //Load the main menu initially.
#if MONO && DEBUG
            if (Director.PendingOsuMode == OsuMode.Unknown)
                Director.ChangeMode(startupMode != OsuMode.Unknown ? startupMode : OsuMode.MainMenu, null);
#else
            Director.ChangeMode(OsuMode.MainMenu, null);
#endif
#endif

            Clock.Start();

            this.drawTimeText = new pText("frame: 0.0ms (0fps)", 10, new Vector2(0, 0), 1.0f, true, Color4.White);
            this.updateTimeText = new pText("update: 0.0ms (0ups)", 10, new Vector2(0, 12), 1.0f, true, Color4.White);

            #if DEBUG
            MainSpriteManager.Add(this.drawTimeText);
            #endif
//MainSpriteManager.Add(this.updateTimeText);

            if (ArcadeUserData.CreditType == CreditType.Time) {
                creditTimeText            = new pSpriteText("00.00", "default", -2, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2(10, 10), 0.9f, true, Color4.White);
                this.creditTimeText.Alpha = 0;

                MainSpriteManager.Add(this.creditTimeText);
            }

            string setSerialPort = Config.GetValue("CardReaderPort", "COM5");

            if (SerialPort.GetPortNames().Contains(setSerialPort)) {
                this._cardReaderPort             = new SerialPort(setSerialPort, 115200);
                this._cardReaderPort.Handshake   = Handshake.None;
                //this._cardReaderPort.ReadTimeout = 1000;
                this._cardReaderPort.RtsEnable   = true;
                this._cardReaderPort.DtrEnable   = true;
                this._cardReaderPort.Parity = Parity.None;
                this._cardReaderPort.StopBits = StopBits.One;
                this._cardReaderPort.DataBits = 8;
                this._cardReaderPort.Open();

                _cardReaderThread = new Thread(CardReaderThread);
                _cardReaderThread.Start();

                _cardReaderBinaryReader = new BinaryReader(this._cardReaderPort.BaseStream);
                Console.WriteLine("Created Card Reader port on " + this._cardReaderPort.PortName);
            }

            string lightingEnabled = Config.GetValue("LightingEnabled", "false");

            if (lightingEnabled == "true") {
                Console.WriteLine("Attempting to enable LEDs");

                string ledCount = Config.GetValue("LightingLedCount", "160");

                bool conversionSuccessful = int.TryParse(ledCount, out int ledCountInt);

                if (conversionSuccessful) {
                    _lightingManager = new LightingManager(this._cardReaderPort, ledCountInt);

                    Console.WriteLine("LightingManager enabled");
                } else {
                    Console.WriteLine($"Error converting `{ledCount}` to a number. LEDs disabled.");
                }
            }
        }

        public virtual string DeviceIdentifier => "1234567890123456789012345678901234567890";

        /// <summary>
        /// Initializes the sound effects engine.
        /// </summary>
        protected virtual SoundEffectPlayer InitializeSoundEffects()
        {
            //currently openAL implementation is used across the board.
            return new SoundEffectPlayerOpenAL();
        }

        /// <summary>
        /// Initializes the background audio playback engine.
        /// </summary>
        protected abstract BackgroundAudioPlayer InitializeBackgroundAudio();

        /// <summary>
        /// Initializes the input management subsystem.
        /// </summary>
        protected abstract void InitializeInput();

        /// <summary>
        /// Initializes the AssetManager.
        /// Assets are skins, hitsounds, textures that come with the game.
        /// These are, depending on the platform, located in the executable itself.
        /// Maps are not included as assets to prevent oversized executables.
        /// </summary>
        /// <returns></returns>
        protected virtual NativeAssetManager InitializeAssetManager()
        {
            return new NativeAssetManager();
        }

        //Sets up all variables after the user is logged in
        //also creates the display on the main menu that the user is logged in
        //with all sorts of stats
        private void EstablishUser(string dataString, string submitToken) {
            ArcadeUserData.ResetLogin();

            _cardLoadingSpinnerActive = false;

            this._cardLoadingNotification?.Dismiss(true);
            this._pinEntryNotification?.Dismiss(true);
            this._newOrLinkToExistingNotification?.Dismiss(true);

            string[] splitDataString = dataString.Split('|');

            if (splitDataString.Length != 4) {
                this.loginFailed();
                return;
            }

            bool parsedUserId = ulong.TryParse(splitDataString[0], out ArcadeUserData.UserId);

            if (!parsedUserId) {
                this.loginFailed();
                return;
            }

            bool parsedStreams = double.TryParse(splitDataString[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out ArcadeUserData.StatStreams);

            if (!parsedStreams) {
                this.loginFailed();
                return;
            }

            ArcadeUserData.Username = splitDataString[1];

            Notify(new Notification($"Welcome {ArcadeUserData.Username}!", "Authentication successful!", NotificationStyle.Brief));

            ArcadeUserData.HasAuth     = true;
            ArcadeUserData.IsGuest     = false;
            ArcadeUserData.SubmitToken = submitToken;

            this._loginProcessOccuring     = false;
            this._cardLoadingSpinnerActive = false;
        }

        /// <summary>
        /// Main update cycle
        /// </summary>
        /// <returns>true if a draw should occur</returns>
        public bool Update(double delta)
        {
            this._sixtyFrameAccumulator += delta;

            if (this._sixtyFrameAccumulator > ((1000.0 / 60.0) / 1000.0)) {
                this._lightingManager?.Update();
                this._sixtyFrameAccumulator = 0;
            }

            if (this._oldAuthState != ArcadeUserData.HasAuth) {
                this._oldAuthState = ArcadeUserData.HasAuth;

                if (ArcadeUserData.HasAuth) {
                    Instance.creditTimeText?.FadeInFromZero(1500);
                } else {
                    Instance.creditTimeText?.FadeOut(1500);
                }
            }

            Clock.Update(false);

            UpdateNotifications();

            Scheduler.Update();

            UpdateInactivity(delta);

#if !RELEASE
            DebugOverlay.Update();
#endif

#if FULLER_DEBUG
            DebugOverlay.AddLine("GC: 0:" + GC.CollectionCount(0) + " 1:" + GC.CollectionCount(1) + " 2:" + GC.CollectionCount(2));
            DebugOverlay.AddLine("Window Size: " + NativeSize.Width + "x" + NativeSize.Height + " Sprite Resolution: " + SpriteResolution);
#endif

            TextureManager.Update();

            MainSpriteManager.Update();

            if (Director.Update())
                InputManager.Update();

            Components.ForEach(c => c.Update());

            ActiveNotification?.Update();

            if (ArcadeUserData.CreditType == CreditType.Time) {
                string newCreditTime = ArcadeUserData.GetFormattedRemainingTime();

                if (creditTimeText.Text != newCreditTime) {
                    creditTimeText.Text = newCreditTime;
                }
            }

            return true;
        }

        private double _deltaAccumulator;
        private int    _frameAccumulator;

        public double _sixtyFrameAccumulator;

        /// <summary>
        /// Main draw cycle.
        /// </summary>
        public void Draw(double delta) {

            #if RELEASE
            this._deltaAccumulator += delta;
            this._frameAccumulator += 1;

            if (this._deltaAccumulator >= 1.0f) {
                this._deltaAccumulator = 0;

                drawTimeText.Text = $"frames: {Math.Round(1000.0 / (double)_frameAccumulator, 2)}ms ({_frameAccumulator}fps)";

                this._frameAccumulator = 0;
            }

            #endif
            SpriteManager.Reset();

            if (Director.ActiveTransition == null || !Director.ActiveTransition.SkipScreenClear)
                //todo: Does clearing DEPTH as well here add a performance overhead?
                GL.Clear(Constants.COLOR_DEPTH_BUFFER_BIT);

            Director.Draw();

            //drawTimeText.Text = $"frame: {LastFrameTime}ms ({Math.Round(1000.0f / LastFrameTime, 2)}fps)\nwith sync: {LastFrameTimeSync}ms ({Math.Round(1000.0f / LastFrameTimeSync, 2)}fps)";
            // drawTimeText.Text = $"frame: {Math.Round(LastFrameTimeSync, 1)}ms ({Math.Round(1000.0f / LastFrameTimeSync)}fps)";
            #if DEBUG
            drawTimeText.Text = $"frames: {Math.Round(LastFrameTime, 1)}ms ({Math.Round(1000.0f / LastFrameTime)}fps)";
            #endif
            //updateTimeText.Text = $"update: {LastUpdateTime}ms ({Math.Round(1000.0f / LastUpdateTime, 2)}fps)";

            MainSpriteManager.Draw();
        }

        private static pDrawable loadingText;
        private static pDrawable loadingCircle;

        private static bool showLoadingOverlay;

        public static bool ShowLoadingOverlay
        {
            get => showLoadingOverlay;
            set
            {
                if (value == showLoadingOverlay) return;

                showLoadingOverlay = value;

                if (showLoadingOverlay)
                {
                    loadingText = new pText(LocalisationManager.GetString(OsuString.Loading), 30, new Vector2(0, -25), 0.999f, true, Color4.LightGray)
                    {
                        DimImmune = true,
                        Origin = OriginTypes.Centre,
                        Field = FieldTypes.StandardSnapCentre,
                        Clocking = ClockTypes.Game,
                        Bold = true
                    };

                    MainSpriteManager.Add(loadingText);
                    loadingText.FadeInFromZero(300);

                    loadingCircle = new pSprite(TextureManager.Load(OsuTexture.songselect_audio_preview), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Game, new Vector2(0, 25), 0.999f, true, Color4.White)
                    {
                        ExactCoordinates = false,
                        DimImmune = true
                    };
                    loadingCircle.Transform(new TransformationF(TransformationType.Rotation, 0, MathHelper.Pi * 2, Clock.Time, Clock.Time + 1500) { Looping = true });
                    MainSpriteManager.Add(loadingCircle);
                    loadingCircle.FadeInFromZero(300);
                }
                else
                {
                    loadingText.FadeOut(100);
                    loadingText.AlwaysDraw = false;

                    loadingCircle.Transformations.Clear();
                    loadingCircle.FadeOut(100);
                    loadingCircle.AlwaysDraw = false;
                }
            }
        }


        private static bool globallyDisableInput;

        public static bool GloballyDisableInput
        {
            get => globallyDisableInput;
            set
            {
                if (value == globallyDisableInput)
                    return;

                globallyDisableInput = value;
                ShowLoadingOverlay = globallyDisableInput;
            }
        }


        public static bool ThrottleExecution;

        public static void TriggerLayoutChanged()
        {
            OnScreenLayoutChanged?.Invoke();
        }

        internal static Notification ActiveNotification;
        internal static Queue<Notification> NotificationQueue = new Queue<Notification>();

        internal static int SpriteSheetResolution;
        public static float InputToFixedWidthAlign;
        public static float SpriteToBaseRatioAligned;

        public static bool Mapper;

        internal static int SuperWidePadding => IsSuperWide ? 30 : 0;

        internal static void Notify(string simple, BoolDelegate action = null)
        {
            Notify(new Notification(LocalisationManager.GetString(OsuString.Alert), simple, NotificationStyle.Okay, action));
        }

        internal static void Notify(Notification notification)
        {
            NotificationQueue.Enqueue(notification);
        }

        private void UpdateNotifications()
        {
            if (ActiveNotification != null && ActiveNotification.Dismissed)
                ActiveNotification = null;

            if (NotificationQueue.Count > 0 && ActiveNotification == null && !globallyDisableInput)
            {
                ActiveNotification = NotificationQueue.Dequeue();
                ActiveNotification.Display();
                //use the main sprite manager to handle input before anything else.
                MainSpriteManager.Add(ActiveNotification);
            }
        }

        public virtual Thread RunInBackground(VoidDelegate task)
        {
            Thread t = new Thread((ThreadStart)delegate { task(); })
            {
                Priority = ThreadPriority.Highest,
                IsBackground = true
            };
            t.Start();
            return t;
        }

        public virtual void ShowWebView(string url, string title = "", StringBoolDelegate checkFinished = null)
        {
            OpenUrl(url);
        }

        public virtual void OpenUrl(string url)
        {
            //Process.Start(url);
        }

        public virtual string PathConfig => string.Empty;
    }
}
