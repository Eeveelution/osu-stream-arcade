using osum.GameModes;
using System;
using System.IO.Ports;
using OpenTK.Graphics;
using osum.Audio;
using osum.GameModes.MainMenu;

namespace osum.Support.Desktop {
    public class LightingColour
    {
        public byte R;
        public byte G;
        public byte B;

        internal void FromColor4(Color4 colour)
        {
            R = (byte)Math.Round(colour.R * 255);
            G = (byte)Math.Round(colour.G * 255);
            B = (byte)Math.Round(colour.B * 255);
        }

        internal void AddColor4(Color4 colour, bool cap = true)
        {
            if (cap)
            {
                byte newR = (byte)Math.Round(colour.R * 255);
                byte newG = (byte)Math.Round(colour.G * 255);
                byte newB = (byte)Math.Round(colour.B * 255);

                if (newR > R) R = (byte)Math.Min(255, R + (byte)Math.Round(colour.R * 255));
                if (newG > G) G = (byte)Math.Min(255, G + (byte)Math.Round(colour.G * 255));
                if (newB > B) B = (byte)Math.Min(255, B + (byte)Math.Round(colour.B * 255));
            }
            else
            {
                R = (byte)Math.Min(255, R + (byte)Math.Round(colour.R * 255));
                G = (byte)Math.Min(255, G + (byte)Math.Round(colour.G * 255));
                B = (byte)Math.Min(255, B + (byte)Math.Round(colour.B * 255));
            }
        }
    }

    public class LightingManager : GameComponent {
        private SerialPort _port;

        private int   _ledCount, _currentLight, _spacingCurrent;
        private float _intensity, _diminish;

        private LightingColour[] _colors;
        private byte[]           _buffer;

        public static LightingManager Instance;
        public bool _useVolume;

        public LightingManager(SerialPort port, int ledCount, float intensity = 1.0f, float diminish = 0.98f) {
            if (port == null) {
                Console.WriteLine("COM Port not defined, LightingManager deactivated.");
                return;
            }

            _port = port;

            _ledCount      = ledCount;
            _intensity     = intensity;
            _diminish      = diminish;

            _colors = new LightingColour[_ledCount];
            _buffer = new byte[_ledCount * 3];

            for (int i = 0; i != _ledCount; i++) {
                _colors[i] = new LightingColour {
                    R = 1, G = 1, B = 1,
                };
            }

            Instance = this;
        }

        public override void Update() {
            Console.WriteLine("sdkhfbdsjhdf");
            if (_port == null || !_port.IsOpen) {
                return;
            }

            float power = this._useVolume ? AudioEngine.Music.CurrentPower : 0;

            byte color = (byte)Math.Max(1, (power - 0.3f) / 0.7f * (255 * _intensity));

            MainMenu m = Director.CurrentMode as MainMenu;

            float r = 0, g = 0, b = 0;

            if (m != null) {
                switch (m.lastExplode) {
                    case 0:
                        r = 152 / 255f;
                        g = 110 / 255f;
                        b = 201 / 255f;
                        break;
                    case 1:
                        r = 247 / 255f;
                        g = 74  / 255f;
                        b = 189 / 255f;
                        break;
                    case 2:
                        r = 255 / 255f;
                        g = 175 / 255f;
                        b = 142 / 255f;
                        break;
                }
            }
            else
            {
                r = (float)GameBase.Random.NextDouble();
                g = (float)GameBase.Random.NextDouble();
                b = (float)GameBase.Random.NextDouble();
            }

            if (color > 1) {
                if (color > 128) this._currentLight = GameBase.Random.Next(this._ledCount);

                _colors[this._currentLight].R = (byte)(color * r);
                _colors[this._currentLight].G = (byte)(color * g);
                _colors[this._currentLight].B = (byte)(color * b);
            }

            int i = 0;
            foreach (LightingColour c in _colors) {
                if (c.R > 1) c.R = (byte)(c.R * _diminish);
                if (c.G > 1) c.G = (byte)(c.G * _diminish);
                if (c.B > 1) c.B = (byte)(c.B * _diminish);

                _buffer[i * 3]     = c.B;
                _buffer[i * 3 + 1] = c.G;
                _buffer[i * 3 + 2] = c.R;

                i++;
            }

            _port.Write(_buffer, 0, _buffer.Length);
        }

        public void Blind(Color4 colour)  {
            if (_colors == null) {
                return;
            }

            foreach (LightingColour c in _colors) {
                c.FromColor4(colour);
            }
        }

        internal void Add(Color4 colour, int spacingInterval = 0) {
            if (this._colors == null) {
                return;
            }

            bool reverse = spacingInterval < 0;

            if (reverse) {
                spacingInterval = Math.Abs(spacingInterval);
            }

            int i = _spacingCurrent;

            foreach (LightingColour c in this._colors) {
                if (i % spacingInterval == 0) {
                    c.AddColor4(colour);
                }

                i = (i + 1) % this._colors.Length;
            }

            _spacingCurrent = (_spacingCurrent + this._colors.Length - (reverse ? -1 : 1)) % this._colors.Length;
        }

        public void UseVolume(bool enable) {
            this._useVolume = enable;
        }
    }
}
