using OpenTK;
using OpenTK.Graphics;
using osum.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;
using osum.Localisation;

namespace osum.UI {
    public partial class Notification {
        private void CreatePinEntry() {
            pinEntryText = new pText("_ _ _ _", 24, new Vector2(150, 60), Vector2.Zero, 1, true, Color4.White, true)
            {
                Field     = FieldTypes.StandardSnapCentre,
                Origin    = OriginTypes.Centre,
                Clocking  = ClockTypes.Game,
                DimImmune = true
            };

            renderRow(200, 240,      "123");
            renderRow(200, 240 + 32, "456");
            renderRow(200, 240 + 64, "789");
            renderRow(200, 240 + 96, "0");

            Add(pinEntryText);

            pDrawable additiveButton = null;

            this.cancelButton = new pSprite(TextureManager.Load(OsuTexture.notification_button_no), new Vector2(100, 130))
            {
                Field           = FieldTypes.StandardSnapCentre,
                Origin          = OriginTypes.Centre,
                Clocking        = ClockTypes.Game,
                DimImmune       = true,
                DrawDepth       = 0.99f,
                HandleClickOnUp = true
            };

            this.cancelButton.OnHover += delegate { additiveButton = this.cancelButton.AdditiveFlash(10000, 0.4f); };

            this.cancelButton.OnHoverLost += delegate
            {
                additiveButton?.FadeOut(100);
            };

            this.cancelButton.OnClick += delegate {
                this.EnteredInput = "not me";
                InputEntryComplete?.Invoke(this);
            };

            Add(this.cancelButton);

            this.noText = new pText("That's not me!!", 24, new Vector2(100, 130), Vector2.Zero, 1, true, Color4.White, true)
            {
                Field     = FieldTypes.StandardSnapCentre,
                Origin    = OriginTypes.Centre,
                Clocking  = ClockTypes.Game,
                DimImmune = true
            };

            Add(this.noText);
        }
    }
}
