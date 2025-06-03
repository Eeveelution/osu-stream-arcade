using System;
using OpenTK;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.SongSelect;
using osum.Graphics;
using osum.Graphics.Renderers;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Localisation;
using osum.UI;

#if iOS
using Accounts;
using Foundation;
using osum.Support.iPhone;
#endif

namespace osum.GameModes.Options
{
    public class Options : GameMode
    {
        private BackButton s_ButtonBack;

        private readonly SpriteManagerDraggable smd = new SpriteManagerDraggable
        {
            Scrollbar = true,

        };

        private SliderControl soundEffectSlider;
        private SliderControl universalOffsetSlider;
        private SliderControl inputOffsetSlider;
        private SliderControl m4aOffsetSlider;
        private SliderControl mp3OffsetSlider;

        private readonly SpriteManager topMostSpriteManager = new SpriteManager();

        internal static float ScrollPosition;

        public override void Initialize()
        {
            s_Header = new pSprite(TextureManager.Load(OsuTexture.options_header), new Vector2(0, 0));
            s_Header.OnClick += delegate { };
            topMostSpriteManager.Add(s_Header);

            pDrawable background =
                new pSprite(TextureManager.Load(OsuTexture.songselect_background), FieldTypes.StandardSnapCentre, OriginTypes.Centre,
                    ClockTypes.Mode, Vector2.Zero, 0, true, new Color4(56, 56, 56, 255));
            background.AlphaBlend = false;
            spriteManager.Add(background);

            s_ButtonBack = new BackButton(delegate { Director.ChangeMode(OsuMode.MainMenu); }, Director.LastOsuMode == OsuMode.MainMenu);
            smd.AddNonDraggable(s_ButtonBack);

            if (MainMenu.MainMenu.InitializeBgm())
                AudioEngine.Music.Play();

            const int header_x_offset = 60;

            float button_x_offset = GameBase.BaseSize.X / 2;

            int vPos = 70;

            pText text = new pText(LocalisationManager.GetString(OsuString.About), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 90;

            pButton button = new pButton(LocalisationManager.GetString(OsuString.Credits), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { Director.ChangeMode(OsuMode.Credits); });
            smd.Add(button);

            vPos += 70;

            button = new pButton(LocalisationManager.GetString(OsuString.OnlineHelp), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { GameBase.Instance.ShowWebView("https://www.osustream.com/help/", "Online Help"); });

            smd.Add(button);

            vPos += 60;

            text = new pText(LocalisationManager.GetString(OsuString.DifficultySettings), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 90;

            buttonFingerGuides = new pButton(LocalisationManager.GetString(OsuString.UseFingerGuides), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayFingerGuideDialog(); });
            smd.Add(buttonFingerGuides);

            vPos += 70;

            buttonEasyMode = new pButton(LocalisationManager.GetString(OsuString.DefaultToEasyMode), new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { DisplayEasyModeDialog(); });
            smd.Add(buttonEasyMode);

            vPos += 60;

            text = new pText(LocalisationManager.GetString(OsuString.Audio), 36, new Vector2(header_x_offset, vPos), 1, true, Color4.White) { Bold = true, TextShadow = true };
            smd.Add(text);

            vPos += 80;

            soundEffectSlider = new SliderControl(LocalisationManager.GetString(OsuString.EffectVolume), AudioEngine.Effect.Volume, new Vector2(button_x_offset - 30, vPos),
                delegate(float v)
                {
                    AudioEngine.Effect.Volume = v;
                    if (Clock.ModeTime / 200 != lastEffectSound)
                    {
                        lastEffectSound = Clock.ModeTime / 200;
                        switch (lastEffectSound % 4)
                        {
                            case 0:
                                AudioEngine.PlaySample(OsuSamples.HitNormal);
                                break;
                            case 1:
                            case 3:
                                AudioEngine.PlaySample(OsuSamples.HitWhistle);
                                break;
                            case 2:
                                AudioEngine.PlaySample(OsuSamples.HitFinish);
                                break;
                        }
                    }
                });
            smd.Add(soundEffectSlider);

            vPos += 60;

            soundEffectSlider = new SliderControl(LocalisationManager.GetString(OsuString.MusicVolume), AudioEngine.Music.MaxVolume, new Vector2(button_x_offset - 30, vPos),
                delegate(float v) { AudioEngine.Music.MaxVolume = v; });
            smd.Add(soundEffectSlider);

            vPos += 60;

            const int offset_range = 32;

            universalOffsetSlider = new SliderControl(LocalisationManager.GetString(OsuString.UniversalOffset), (float)(Clock.USER_OFFSET + offset_range) / (offset_range * 2), new Vector2(button_x_offset - 30, vPos),
                delegate(float v)
                {
                    GameBase.Config.SetValue("offset", (Clock.USER_OFFSET = (int)((v - 0.5f) * offset_range * 2)));
                    if (universalOffsetSlider != null) //will be null on first run.
                        universalOffsetSlider.Text.Text = Clock.USER_OFFSET + "ms";
                });
            smd.Add(universalOffsetSlider);

            vPos += 50;

            text               = new pText(LocalisationManager.GetString(OsuString.UniversalOffsetDetails) + "\n\nExcept for the Input and everything else offsets\npeppys code comment says that higher numbers mean earlier\non iOS and Android input offset is 16ms\n", 18, new Vector2(0, vPos), 1, true, Color4.LightGray) { TextShadow = true };
            text.Field         = FieldTypes.StandardSnapTopCentre;
            text.Origin        = OriginTypes.TopCentre;
            text.TextAlignment = TextAlignment.Centre;
            text.MeasureText(); //force a measure as this is the last sprite to be added to the draggable area (need height to be precalculated)
            text.TextBounds.X = 600;
            smd.Add(text);

            vPos += (int)text.MeasureText().Y - 30;

            const int newOffsetRanges = 64;

            this.inputOffsetSlider = new SliderControl("Input Offset", (float)(Clock.UniversalOffsetInput + newOffsetRanges) / (newOffsetRanges * 2), new Vector2(button_x_offset - 30, vPos), delegate(float v) {
                float actualValue = ((v - 0.5f) * newOffsetRanges * 2);
                Clock.UniversalOffsetInput = (int)actualValue;

                GameBase.Config.SetValue("InputOffset", Clock.UniversalOffsetInput);

                if (inputOffsetSlider != null) //will be null on first run.
                    inputOffsetSlider.Text.Text = Clock.UniversalOffsetInput + "ms";

            });
            smd.Add(inputOffsetSlider);

            vPos += 60;

            this.m4aOffsetSlider = new SliderControl("*.m4a Audio Offset", (float)(Clock.UniversalOffsetM4A + newOffsetRanges) / (newOffsetRanges * 2), new Vector2(button_x_offset - 30, vPos), delegate(float v) {
                float actualValue = ((v - 0.5f) * newOffsetRanges * 2);
                Clock.UniversalOffsetM4A = (int)actualValue;

                GameBase.Config.SetValue("M4AOffset", Clock.UniversalOffsetM4A);

                if (m4aOffsetSlider != null) //will be null on first run.
                    m4aOffsetSlider.Text.Text = Clock.UniversalOffsetM4A + "ms";
            });
            smd.Add(m4aOffsetSlider);

            vPos += 60;

            this.mp3OffsetSlider = new SliderControl("*.mp3 Audio Offset", (float)(Clock.UniversalOffsetMp3 + newOffsetRanges) / (newOffsetRanges * 2), new Vector2(button_x_offset - 30, vPos), delegate(float v) {
                float actualValue = ((v - 0.5f) * newOffsetRanges * 2);
                Clock.UniversalOffsetMp3 = (int) actualValue;

                GameBase.Config.SetValue("MP3Offset", Clock.UniversalOffsetMp3);

                if (mp3OffsetSlider != null) //will be null on first run.
                    mp3OffsetSlider.Text.Text = Clock.UniversalOffsetMp3 + "ms";
            });

            smd.Add(mp3OffsetSlider);

            vPos += 40;
            vPos += 40;

            pButton abc = new pButton("Log in abc", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue,
                                         delegate {
                                             GameBase.Instance.ArcadeStartLoginProcess("abcabcabcabcabcabcabcabc", "abcabcabcabcabcabcabcabc", "abcabcabcabcabcabcabcabc");
                                         });

            smd.Add(abc);

            vPos += 40;

            pButton def = new pButton("Log in def", new Vector2(button_x_offset, vPos), new Vector2(280, 50), Color4.SkyBlue, delegate { GameBase.Instance.ArcadeStartLoginProcess("defdefdefdefdefdefdefdef", "defdefdefdefdefdefdefdef", "defdefdefdefdefdefdefdef"); });

            smd.Add(def);

            UpdateButtons();

            smd.SetMaxHeight(vPos + 250);
            smd.ScrollTo(ScrollPosition);

        }

        private int lastEffectSound;
        private pButton buttonFingerGuides;
        private pButton buttonEasyMode;
        private pSprite s_Header;

        internal static void DisplayFingerGuideDialog()
        {
            Notification notification = new Notification(LocalisationManager.GetString(OsuString.UseFingerGuides), LocalisationManager.GetString(OsuString.UseGuideFingers_Explanation),
                NotificationStyle.YesNo,
                delegate(bool yes)
                {
                    GameBase.Config.SetValue(@"GuideFingers", yes);

                    if (Director.CurrentMode is Options o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }

        private void UpdateButtons()
        {
            buttonEasyMode.SetStatus(GameBase.Config.GetValue(@"EasyMode", false));
            buttonFingerGuides.SetStatus(GameBase.Config.GetValue(@"GuideFingers", false));
        }

        internal static void DisplayEasyModeDialog()
        {
            Notification notification = new Notification(LocalisationManager.GetString(OsuString.DefaultToEasyMode), LocalisationManager.GetString(OsuString.DefaultToEasyMode_Explanation),
                NotificationStyle.YesNo,
                delegate(bool yes)
                {
                    GameBase.Config.SetValue(@"EasyMode", yes);

                    if (Director.CurrentMode is Options o) o.UpdateButtons();
                });
            GameBase.Notify(notification);
        }

        public override void Dispose()
        {
            ScrollPosition = smd.ScrollPosition;

            GameBase.Config.SetValue("VolumeEffect", (int)(AudioEngine.Effect.Volume * 100));
            GameBase.Config.SetValue("VolumeMusic", (int)(AudioEngine.Music.MaxVolume * 100));
            GameBase.Config.SaveConfig();

            topMostSpriteManager.Dispose();

            smd.Dispose();
            base.Dispose();
        }

        public override bool Draw()
        {
            base.Draw();
            smd.Draw();
            topMostSpriteManager.Draw();
            return true;
        }

        public override void Update()
        {
            s_Header.Position.Y = Math.Min(0, -smd.ScrollPercentage * 20);

            smd.Update();
            base.Update();
            topMostSpriteManager.Update();
        }
    }
}