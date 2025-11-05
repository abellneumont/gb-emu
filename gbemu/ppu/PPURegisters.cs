using System;

namespace gbemu.ppu
{
    internal class PPURegisters
    {
        private readonly Device _device;

        internal PPURegisters(Device device)
        {
            _device = device;

            if (_device.device_type == DeviceType.CGB)
            {
                IsBackgroundEnabled = true;
            }
        }

        internal byte ScrollX { get; set; }

        internal byte ScrollY { get; set; }

        internal byte WindowX { get; set; }

        internal byte WindowY { get; set; }

        internal byte BackgroundPaletteData { get; set; }

        internal byte ObjectPaletteData0 { get; set; }

        internal byte ObjectPaletteData1 { get; set; }

        internal byte LYRegister { get; set; }

        private byte _lyCompare;
        internal byte LYCompare
        {
            get => _lyCompare;
            set
            {
                _lyCompare = value;

                UpdateStatIRQSignal();
            }
        }

        internal Grayscale GetColorFromNumberPalette(int colorNumber, byte paletteData) =>
            colorNumber switch
            {
                0 => (Grayscale)(paletteData & 0x3),
                1 => (Grayscale)((paletteData >> 2) & 0x3),
                2 => (Grayscale)((paletteData >> 4) & 0x3),
                3 => (Grayscale)((paletteData >> 6) & 0x3),
                _ => throw new ArgumentOutOfRangeException()
            };

        internal bool IsLcdOn { get; private set; }
        internal int WindowTileMapOffset { get; private set; }
        internal bool IsWindowEnabled { get; private set; }
        internal int BackgroundAndWindowTilesetOffset { get; private set; }
        internal bool UsingSignedByteForTileData { get; private set; }
        internal int BackgroundTileMapOffset { get; private set; }
        internal bool LargeSprites { get; private set; }
        internal bool AreSpritesEnabled { get; private set; }
        internal bool IsBackgroundEnabled { get; private set; }
        internal bool IsCgbSpriteMasterPriorityOn { get; private set; }

        private byte _lcdControlRegister;
        internal byte LCDControlRegister
        {
            get => _lcdControlRegister;
            set
            {
                _lcdControlRegister = value;
                IsLcdOn = (value & 0x80) == 0x80;
                WindowTileMapOffset = (value & 0x40) == 0x40 ? 0x9C00 : 0x9800;
                IsWindowEnabled = (value & 0x20) == 0x20;
                BackgroundAndWindowTilesetOffset = (value & 0x10) == 0x10 ? 0x8000 : 0x8800;
                UsingSignedByteForTileData = BackgroundAndWindowTilesetOffset == 0x8800;
                BackgroundTileMapOffset = (value & 0x8) == 0x8 ? 0x9C00 : 0x9800;
                LargeSprites = (value & 0x4) == 0x4;
                AreSpritesEnabled = (value & 0x2) == 0x2;
                IsBackgroundEnabled = (value & 0x1) == 0x1;

                if (!IsLcdOn)
                {
                    _device.ppu.TurnLCDOff();

                    LYRegister = 0x0;
                    StatMode = StateMode.H_BLANK_PERIOD;
                    _statIRQSignal = false;
                }
                else
                {
                    UpdateStatIRQSignal();
                }
            }
        }

        private byte _statRegister = 0x80;
        private bool _statIRQSignal;

        internal byte StatRegister
        {
            get => _statRegister;
            set
            {
                var s = value | 0x80;
                s &= 0xF8;
                _statRegister = (byte)s;
                IsLYLCCheckEnabled = (value & 0x40) == 0x40;
                Mode2OAMCheckEnabled = (value & 0x20) == 0x20;
                Mode1VBlankCheckEnabled = (value & 0x10) == 0x10;
                Mode0HBlankCheckEnabled = (value & 0x8) == 0x8;

                UpdateStatIRQSignal();
            }
        }

        private void UpdateStatIRQSignal()
        {
            if (!IsLcdOn) return;
            var oldStatIRQSignal = _statIRQSignal;

            CoincidenceFlag = _lyCompare == LYRegister;

            _statIRQSignal = (IsLYLCCheckEnabled && LYRegister == _lyCompare) ||
                             (StatMode == StateMode.H_BLANK_PERIOD && Mode0HBlankCheckEnabled) ||
                             (StatMode == StateMode.OAM_RAM_PERIOD && Mode2OAMCheckEnabled) ||
                             (StatMode == StateMode.V_BLANK_PERIOD && (Mode1VBlankCheckEnabled || Mode2OAMCheckEnabled));

            if (!oldStatIRQSignal && _statIRQSignal)
            {
                _device.interrupt_registers.RequestInterrupt(interrupts.Interrupt.LCD_STATE);
            }
        }

        internal bool IsLYLCCheckEnabled { get; private set; }

        internal bool Mode2OAMCheckEnabled { get; private set; }

        internal bool Mode1VBlankCheckEnabled { get; private set; }

        internal bool Mode0HBlankCheckEnabled { get; private set; }

        private bool _coincidenceFlag;
        internal bool CoincidenceFlag
        {
            get => _coincidenceFlag;
            private set
            {
                _coincidenceFlag = value;
                if (value)
                {
                    _statRegister |= 0x4;
                }
                else
                {
                    _statRegister &= 0xFB;
                }
            }
        }

        private StateMode _statMode;
        internal StateMode StatMode
        {
            get => _statMode;
            set
            {
                _statMode = value;
                _statRegister = (byte)((_statRegister & 0xFC) | (int)value);

                UpdateStatIRQSignal();
            }
        }
    }
}
