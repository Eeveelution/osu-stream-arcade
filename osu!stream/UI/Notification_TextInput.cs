using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Localisation;

namespace osum.UI {
    public partial class Notification {
        private string  currentTextBuffer = "";
        private pText   currentTextBufferDisplay;

        private pSprite okButtonBackground;
        private pSprite okButtonText;

        private List<pSprite> buttonBackgrounds = new List<pSprite>();
        private List<pText> buttonTexts = new List<pText>();

        private bool _shiftEnabled = false;

        private const string numberRow = "1234567890_";
        private const string firstRow  = "qwertyuiop";
        private const string secondRow = "asdfghjkl";
        private const string thirdRow  = "zxcvbnm";

        private const float bgSize         = 35;
        private const float offset         = (bgSize / 2.0f);
        private const float borderXEnlarge = 6;

        private void renderRow(int rowXOffset, int rowYOffset, string row) {
            for (int i = 0; i != row.Length; i++) {
                int xOffset = rowXOffset + (i * 32);

                char c = row[i];

                //pText text = new pText(c.ToString(), 20, new Vector2(xOffset - 5.75f, rowYOffset - 5), Vector2.Zero, 1.1f, true, Color4.White, true) {
                pText text = new pText(c.ToString(), 20, new Vector2(0, 0), Vector2.Zero, 1.1f, true, Color4.White, true) {
                    Field     = FieldTypes.Standard,
                    Origin    = OriginTypes.Centre,
                    Clocking  = ClockTypes.Game,
                    DimImmune = true
                };

                pSprite bg = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2(xOffset - offset, rowYOffset - offset), 1.05f, true, new Color4(100, 100, 100, 255)) {
                    Scale     = new Vector2(bgSize, bgSize),
                    DimImmune = true,
                };

                text.Position = (bg.Position + bg.Scale / 2.0f) - new Vector2(6, 8);

                pSprite bgBorder = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2((xOffset - offset) - 2, (rowYOffset - offset) - 2), 1f, true, Color4.White) {
                    Scale     = new Vector2(bgSize + borderXEnlarge, bgSize + 6),
                    DimImmune = true,
                    Tag = c.ToString()
                };

                bgBorder.OnClick += (sender, args) => {
                    this.currentTextBuffer = this.currentTextBuffer.TrimEnd('_') + (sender as pSprite).Tag as string + "_" ;

                    if (this.Style == NotificationStyle.PinEntry) {
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
                                break;
                        }

                        if (this.enteredDigits == 4) {
                            this.EnteredInput = this.currentTextBuffer.TrimEnd('_');
                            this.InputEntryComplete?.Invoke(this);
                        }
                    }

                    bg.FlashColour(Color4.White, 200);

                    if(this.currentTextBufferDisplay != null)
                        currentTextBufferDisplay.Text = this.currentTextBuffer;
                };

                Add(text);
                Add(bg);
                Add(bgBorder);

                this.buttonBackgrounds.Add(bgBorder);
                this.buttonTexts.Add(text);
            }
        }

        private void CreateTextInput() {
            this.titleText.Position.Y -= 40;
            this.descriptionText.Position.Y -= 40;

            const int baseX = 200;
            const int baseY = 165;

            const int numberRowXOffset = baseX;
            const int numberRowYOffset = baseY;

            renderRow(numberRowXOffset, numberRowYOffset, numberRow);

            const int firstRowXOffset = numberRowXOffset + 5;
            const int firstRowYOffset = numberRowYOffset + 32;

            renderRow(firstRowXOffset, firstRowYOffset, firstRow);

            const int secondRowXOffset = firstRowXOffset + 5;
            const int secondRowYOffset = firstRowYOffset + 32;

            renderRow(secondRowXOffset, secondRowYOffset, secondRow);

            const int thirdRowXOffset = secondRowXOffset + 15;
            const int thirdRowYOffset = secondRowYOffset + 32;

            renderRow(thirdRowXOffset, thirdRowYOffset, thirdRow);

            //Shift key
            pText shiftText = new pText("shift", 22, new Vector2(464, thirdRowYOffset - 7), Vector2.Zero, 1.1f, true, Color4.White, true) {
                Field     = FieldTypes.Standard,
                Origin    = OriginTypes.Centre,
                Clocking  = ClockTypes.Game,
                DimImmune = true
            };

            pSprite shiftBg = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2(449 - offset, thirdRowYOffset - offset), 1.05f, true, new Color4(100, 100, 100, 255)) {
                Scale     = new Vector2(100, bgSize),
                DimImmune = true,
            };

            pSprite shiftBgBorder = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2((449 - offset) - 2, (thirdRowYOffset - offset) - 2), 1f, true, Color4.White) {
                Scale     = new Vector2(100+ borderXEnlarge, bgSize + 6),
                DimImmune = true,
            };

            shiftBgBorder.OnClick += (sender, args) => {
                if (_shiftEnabled) {
                    _shiftEnabled = false;
                    foreach (pSprite button in this.buttonBackgrounds) {
                        button.Tag = (button.Tag as string).ToLower();
                    }

                    foreach (pText t in this.buttonTexts) {
                        if(char.IsLetter(t.Text[0]))
                            t.Position.Y -= 3;
                        t.Text       =  t.Text.ToLower();
                    }
                } else {
                    _shiftEnabled = true;
                    foreach (pSprite button in this.buttonBackgrounds) {
                        button.Tag = (button.Tag as string).ToUpper();
                    }

                    foreach (pText t in this.buttonTexts) {
                        if(char.IsLetter(t.Text[0]))
                            t.Position.Y += 3;
                        t.Text       =  t.Text.ToUpper();
                    }
                }

                shiftBg.FlashColour(Color4.White, 200);
            };

            Add(shiftText);
            Add(shiftBg);
            Add(shiftBgBorder);

            //backspace Key
            pText backspaceText = new pText("Bsp.", 22, new Vector2(562, numberRowYOffset - 5), Vector2.Zero, 1.1f, true, Color4.White, true) {
                Field     = FieldTypes.Standard,
                Origin    = OriginTypes.Centre,
                Clocking  = ClockTypes.Game,
                DimImmune = true
            };

            pSprite backspaceBg = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2(552 - offset, numberRowYOffset - offset), 1.05f, true, new Color4(100, 100, 100, 255)) {
                Scale     = new Vector2(88, bgSize),
                DimImmune = true,
            };

            pSprite backspaceBgBorder = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2((552 - offset) - 2, (numberRowYOffset - offset) - 2), 1f, true, Color4.White) {
                Scale     = new Vector2(88+ borderXEnlarge, bgSize + 6),
                DimImmune = true,
            };

            backspaceBgBorder.OnClick += (sender, args) => {
                backspaceBg.FlashColour(Color4.White, 200);

                if (this.currentTextBuffer == "") {
                    return;
                }

                this.currentTextBuffer = this.currentTextBuffer.TrimEnd('_');
                this.currentTextBuffer = this.currentTextBuffer.Substring(0, this.currentTextBuffer.Length - 1);
                this.currentTextBuffer = this.currentTextBuffer + "_";


                currentTextBufferDisplay.Text     = this.currentTextBuffer;
            };

            Add(backspaceText);
            Add(backspaceBg);
            Add(backspaceBgBorder);

            //Space
            pSprite spaceBg = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2(250 - offset, (thirdRowYOffset + 32) - offset), 1.05f, true, new Color4(100, 100, 100, 255)) {
                Scale     = new Vector2(247, bgSize),
                DimImmune = true,
            };

            pSprite spaceBgBorder = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2((250 - offset) - 2, ((thirdRowYOffset + 32) - offset) - 2), 1f, true, Color4.White) {
                Scale     = new Vector2(247+ borderXEnlarge, bgSize + 6),
                DimImmune = true,
            };

            spaceBgBorder.OnClick += (sender, args) => {
                this.currentTextBuffer = this.currentTextBuffer.TrimEnd('_');
                this.currentTextBuffer += " _";

                currentTextBufferDisplay.Text     = currentTextBuffer;

                spaceBg.FlashColour(Color4.White, 200);
            };

            Add(spaceBg);
            Add(spaceBgBorder);

            //Text display field
            pSprite borderBottom = new pSprite(GameBase.WhitePixel, FieldTypes.Standard, OriginTypes.TopLeft, ClockTypes.Game, new Vector2(125, 355), 1f, true, Color4.White) {
                Scale = new Vector2(400, 2), DimImmune = true
            };

            currentTextBufferDisplay = new pText("_", 24, new Vector2(125, 325), 1f, true, Color4.White) {
                Field = FieldTypes.Standard,
                DimImmune = true
            };

            Add(borderBottom);
            Add(currentTextBufferDisplay);

            pDrawable additiveButton = null;

            this.okButtonBackground = new pSprite(TextureManager.Load(OsuTexture.notification_button_yes), new Vector2(515, 340)) {
                Field           = FieldTypes.Standard,
                Origin          = OriginTypes.Centre,
                Clocking        = ClockTypes.Game,
                DimImmune       = true,
                DrawDepth       = 0.99f,
                HandleClickOnUp = true,
                Scale = new Vector2(0.75f, 0.75f)
            };

            okButtonBackground.OnHover += delegate {
                additiveButton = okButtonBackground.AdditiveFlash(10000, 0.4f);
            };

            okButtonBackground.OnHoverLost += delegate {
                additiveButton.FadeOut(100);
            };

            okButtonBackground.OnClick += delegate {
                this.EnteredInput = this.currentTextBuffer.TrimEnd('_');
                InputEntryComplete?.Invoke(this);
            };

            Add(this.okButtonBackground);

            pText okayText = new pText(LocalisationManager.GetString(OsuString.Okay), 24, new Vector2(480, 322), Vector2.Zero, 1, true, Color4.White, true)
            {
                Field     = FieldTypes.Standard,
                Origin    = OriginTypes.TopLeft,
                Clocking  = ClockTypes.Game,
                DimImmune = true
            };

            Add(okayText);
        }
    }
}
