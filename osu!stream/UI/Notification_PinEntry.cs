using OpenTK;
using OpenTK.Graphics;
using osum.Graphics.Sprites;
using osum.Helpers;

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
        }
    }
}
