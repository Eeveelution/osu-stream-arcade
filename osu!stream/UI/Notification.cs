using System;
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
    public partial class Notification : SpriteManager
    {
        public bool Dismissed;
        internal BoolDelegate Action;

        private NotificationStyle Style;

        internal pSprite okayButton;
        private pSprite cancelButton;

        internal pText titleText, descriptionText, pinEntryText, yesText, noText, hiddenText;

        internal string TextToHide;

        public delegate void InputEntryCompletedDelegate(Notification sender);

        public event InputEntryCompletedDelegate InputEntryComplete;

        public Notification(string title, string description, NotificationStyle style, BoolDelegate action = null)
        {
            Clocking = ClockTypes.Game;

            pSprite back = new pSprite(TextureManager.Load(OsuTexture.notification_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre, ClockTypes.Game, Vector2.Zero, 0.98f, true, Color4.White)
            {
                DimImmune = true,
                Scale     = (style == NotificationStyle.TextInput || style == NotificationStyle.PinEntry) ? new Vector2(1, 1.2f) : Vector2.One
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

                    this.yesText = new pText(LocalisationManager.GetString(OsuString.Yes), 24, new Vector2(-140, button_height), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(this.yesText);
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

                    this.noText = new pText(LocalisationManager.GetString(OsuString.No), 24, new Vector2(140, button_height), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field = FieldTypes.StandardSnapCentre,
                        Origin = OriginTypes.Centre,
                        Clocking = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(this.noText);
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
                    this.CreatePinEntry();

                    break;
                }
                case NotificationStyle.Brief: {
                    GameBase.Scheduler.Add(() => {
                        this.Dismiss(true);
                    }, 1500);
                    break;
                }
                case NotificationStyle.HiddenText: {
                    hiddenText = new pText("click to reveal", 24, new Vector2(0, button_height - 48), Vector2.Zero, 1, true, Color4.White, true)
                    {
                        Field     = FieldTypes.StandardSnapCentre,
                        Origin    = OriginTypes.Centre,
                        Clocking  = ClockTypes.Game,
                        DimImmune = true
                    };

                    this.hiddenText.OnHover += HiddenTextOnOnHover;
                    this.hiddenText.OnHoverLost += HiddenTextOnOnHoverLost;

                    Add(hiddenText);

                    pDrawable additiveButton = null;

                    okayButton = new pSprite(TextureManager.Load(OsuTexture.notification_button_ok), new Vector2(0, button_height))
                    {
                        Field           = FieldTypes.StandardSnapCentre,
                        Origin          = OriginTypes.Centre,
                        Clocking        = ClockTypes.Game,
                        DimImmune       = true,
                        DrawDepth       = 0.99f,
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
                        Field     = FieldTypes.StandardSnapCentre,
                        Origin    = OriginTypes.Centre,
                        Clocking  = ClockTypes.Game,
                        DimImmune = true
                    };

                    Add(okayText);

                    break;
                }
                case NotificationStyle.TextInput: {
                    this.CreateTextInput();
                    break;
                }
            }
        }
        private void HiddenTextOnOnHover(object sender, EventArgs e) {
            this.hiddenText.Text = this.TextToHide;
        }
        private void HiddenTextOnOnHoverLost(object sender, EventArgs e) {
            this.hiddenText.Text = "click to reveal";
        }

        internal string EnteredInput = "";
        private  int    enteredDigits = 0;

        internal void Dismiss(bool completed, bool invokeAction = true)
        {
            AudioEngine.PlaySample(OsuSamples.ButtonTap);

            GameBase.Scheduler.Add(delegate
            {
                if (invokeAction && this.Action != null) {
                    Action.Invoke(completed);
                }
                Dismissed = true;
                AlwaysDraw = false;
            }, 300);

            FadeOut(300);
            ScaleTo(0.95f, 300, EasingTypes.Out);
            RotateTo(0.05f, 300, EasingTypes.Out);

            switch (this.Style) {
                case NotificationStyle.HiddenText:
                    this.hiddenText.OnHover -= this.HiddenTextOnOnHover;
                    this.hiddenText.OnHoverLost -= this.HiddenTextOnOnHoverLost;
                    break;
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
        Brief,
        HiddenText,
        TextInput
    }
}