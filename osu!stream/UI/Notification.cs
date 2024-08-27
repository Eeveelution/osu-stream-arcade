using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using osum.Audio;
using osum.Graphics;
using osum.Graphics.Renderers;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Input;
using osum.Localisation;
using osum.Support.Desktop;

namespace osum.UI
{
    public class Notification : SpriteManager
    {
        public bool Dismissed;
        private readonly BoolDelegate Action;

        private NotificationStyle Style;

        internal pSprite okayButton;
        private pSprite cancelButton;

        internal pText titleText, descriptionText, pinEntryText;

        public delegate void PinEntryCompletedDelegate(Notification sender);

        public event PinEntryCompletedDelegate PinEntryComplete;

        public Notification(string title, string description, NotificationStyle style, BoolDelegate action = null)
        {
            Clocking = ClockTypes.Game;

            pSprite back = new pSprite(TextureManager.Load(OsuTexture.notification_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Game, Vector2.Zero, 0.98f, true, Color4.White)
            {
                DimImmune = true
            };

            titleText = new pText(title, 36, new Vector2(0, -130), new Vector2(600 * GameBase.SpriteToBaseRatio, 0), 1, true, Color4.White, true)
            {
                Field = FieldTypes.StandardSnapCentre,
                Origin = OriginTypes.Centre,
                TextAlignment = TextAlignment.Centre,
                Clocking = ClockTypes.Game,
                DimImmune = true
            };

            descriptionText = new pText(description, 24, new Vector2(0, -90), new Vector2(600 * GameBase.SpriteToBaseRatio, 0), 1, true, Color4.White, false)
            {
                Field = FieldTypes.StandardSnapCentre,
                Origin = OriginTypes.TopCentre,
                TextAlignment = TextAlignment.Centre,
                Clocking = ClockTypes.Game,
                DimImmune = true
            };

            Alpha = 0;
            DrawDepth = 1;

            Action = action;


            Style = style;

            AddControls(style);

            Add(back);
            Add(descriptionText);
            Add(titleText);
        }

        private void AddControls(NotificationStyle style)
        {
            const int button_height = 95;

            switch (style)
            {
                case NotificationStyle.Okay:
                {
                    pDrawable additiveButton = null;

                    okayButton = new pSprite(TextureManager.Load(OsuTexture.notification_button_ok), new Vector2(0, button_height))
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true,
                        DrawDepth = 0.99f,
                        HandleClickOnUp = true
                    };
                    okayButton.OnHover += delegate { additiveButton = okayButton.AdditiveFlash(10000, 0.4f); };

                    okayButton.OnHoverLost += delegate
                    {
                        additiveButton?.FadeOut(100);
                    };

                    okayButton.OnClick += delegate { this.Dismiss(true); };

                    Add(okayButton);

                    pText okayText = new pText(LocalisationManager.GetString(OsuString.Okay), 24, new Vector2(0, button_height), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(okayText);
                }
                    break;
                case NotificationStyle.YesNo:
                {
                    pDrawable additiveButton = null;

                    okayButton = new pSprite(TextureManager.Load(OsuTexture.notification_button_yes), new Vector2(-140, button_height))
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true,
                        DrawDepth = 0.99f,
                        HandleClickOnUp = true
                    };

                    okayButton.OnHover += delegate { additiveButton = okayButton.AdditiveFlash(10000, 0.4f); };

                    okayButton.OnHoverLost += delegate
                    {
                        additiveButton?.FadeOut(100);
                    };

                    okayButton.OnClick += delegate { this.Dismiss(true); };

                    Add(okayButton);

                    pText okayText = new pText(LocalisationManager.GetString(OsuString.Yes), 24, new Vector2(-140, button_height), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(okayText);
                }
                {
                    pDrawable additiveButton = null;

                    cancelButton = new pSprite(TextureManager.Load(OsuTexture.notification_button_no), new Vector2(140, button_height))
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true,
                        DrawDepth = 0.99f,
                        HandleClickOnUp = true
                    };
                    cancelButton.OnHover += delegate { additiveButton = cancelButton.AdditiveFlash(10000, 0.4f); };

                    cancelButton.OnHoverLost += delegate
                    {
                        additiveButton?.FadeOut(100);
                    };

                    cancelButton.OnClick += delegate { this.Dismiss(false); };

                    Add(cancelButton);

                    pText cancelText = new pText(LocalisationManager.GetString(OsuString.No), 24, new Vector2(140, button_height), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(cancelText);
                }
                    break;
                case NotificationStyle.Loading: {
                    okayButton = new pSprite(TextureManager.Load(OsuTexture.songselect_audio_preview), new Vector2(0, button_height))
                    {
                        Field           = FieldTypes.StandardSnapCentre,
                        Origin          = OriginTypes.Centre,
                        Clocking        = ClockTypes.Game,
                        DimImmune       = true,
                        DrawDepth       = 0.99f,
                        HandleClickOnUp = true
                    };

                    okayButton.Transform(new TransformationF(TransformationType.Rotation, 0, MathHelper.Pi * 2, Clock.ModeTime, Clock.ModeTime + 2000) { Looping = true });

                    Add(okayButton);
                    break;
                }
                case NotificationStyle.PinEntry: {
                    GameWindowDesktop.Instance.Keyboard.KeyDown += pinEntryHandler;

                    pinEntryText = new pText("_ _ _ _", 24, new Vector2(0, button_height), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field     = FieldTypes.StandardSnapCentre,
                        Origin    = OriginTypes.Centre,
                        Clocking  = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(pinEntryText);

                    break;
                }
                case NotificationStyle.Brief: {
                    GameBase.Scheduler.Add(() => {
                        this.Dismiss(true);
                    }, 1500);
                    break;
                }
            }
        }

        internal string EnteredPin = "";
        private  int    enteredDigits = 0;

        private void pinEntryHandler(object sender, KeyboardKeyEventArgs e) {
            switch (e.Key) {
                case Key.Number0:
                case Key.Keypad0:
                    this.EnteredPin += "0";
                    break;
                case Key.Number1:
                case Key.Keypad1:
                    this.EnteredPin += "1";
                    break;
                case Key.Number2:
                case Key.Keypad2:
                    this.EnteredPin += "2";
                    break;
                case Key.Number3:
                case Key.Keypad3:
                    this.EnteredPin += "3";
                    break;
                case Key.Number4:
                case Key.Keypad4:
                    this.EnteredPin += "4";
                    break;
                case Key.Number5:
                case Key.Keypad5:
                    this.EnteredPin += "5";
                    break;
                case Key.Number6:
                case Key.Keypad6:
                    this.EnteredPin += "6";
                    break;
                case Key.Number7:
                case Key.Keypad7:
                    this.EnteredPin += "7";
                    break;
                case Key.Number8:
                case Key.Keypad8:
                    this.EnteredPin += "8";
                    break;
                case Key.Number9:
                case Key.Keypad9:
                    this.EnteredPin += "9";
                    break;
            }

            this.enteredDigits++;

            switch (this.enteredDigits) {
                case 0:
                    this.pinEntryText.Text = "_ _ _ _";
                    break;
                case 1:
                    this.pinEntryText.Text = "X _ _ _";
                    break;
                case 2:
                    this.pinEntryText.Text = "X X _ _";
                    break;
                case 3:
                    this.pinEntryText.Text = "X X X _";
                    break;
                case 4:
                    this.pinEntryText.Text = "X X X X";

                    if (PinEntryComplete != null) {
                        PinEntryComplete(this);
                    }
                    break;
            }
        }

        internal void Dismiss(bool completed)
        {
            AudioEngine.PlaySample(OsuSamples.ButtonTap);

            GameBase.Scheduler.Add(delegate
            {
                Action?.Invoke(completed);
                Dismissed = true;
                AlwaysDraw = false;
            }, 300);

            FadeOut(300);
            ScaleTo(0.95f, 300, EasingTypes.Out);
            RotateTo(0.05f, 300, EasingTypes.Out);

            if (this.Style == NotificationStyle.PinEntry) {
                GameWindowDesktop.Instance.Keyboard.KeyDown -= pinEntryHandler;
            }
        }

        internal virtual void Display()
        {
            Transformation bounce = new TransformationBounce(Clock.Time, Clock.Time + 800, 1, 0.1f, 8);
            Transform(bounce);

            FadeIn(200);

            AudioEngine.PlaySample(OsuSamples.Notify);
        }
    }

    public enum NotificationStyle
    {
        Okay,
        YesNo,
        Loading,
        PinEntry,
        Brief
    }
}