using System;

namespace gbemu.ppu
{
    internal class PPURegisters
    {
        private readonly Device device;

        internal PPURegisters(Device device)
        {
            this.device = device;

            if (this.device.device_type == DeviceType.CGB)
            {
                background_enabled = true;
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

        private byte ly_compare;
        internal byte LYCompare
        {
            get => ly_compare;
            set
            {
                ly_compare = value;

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

        internal bool lcd_on { get; private set; }
        internal int window_tile_offset { get; private set; }
        internal bool window_enabled { get; private set; }
        internal int background_and_window_tile_offset { get; private set; }
        internal bool signed_tile_data { get; private set; }
        internal int background_tile_offset { get; private set; }
        internal bool large_sprites { get; private set; }
        internal bool sprites_enabled { get; private set; }
        internal bool background_enabled { get; private set; }
        internal bool IsCgbSpriteMasterPriorityOn { get; private set; }

        private byte lcd_control_register;
        internal byte LCDControlRegister
        {
            get => lcd_control_register;
            set
            {
                lcd_control_register = value;
                lcd_on = (value & 0x80) == 0x80;
                window_tile_offset = (value & 0x40) == 0x40 ? 0x9C00 : 0x9800;
                window_enabled = (value & 0x20) == 0x20;
                background_and_window_tile_offset = (value & 0x10) == 0x10 ? 0x8000 : 0x8800;
                signed_tile_data = background_and_window_tile_offset == 0x8800;
                background_tile_offset = (value & 0x8) == 0x8 ? 0x9C00 : 0x9800;
                large_sprites = (value & 0x4) == 0x4;
                sprites_enabled = (value & 0x2) == 0x2;
                background_enabled = (value & 0x1) == 0x1;

                if (!lcd_on)
                {
                    device.ppu.TurnLCDOff();

                    LYRegister = 0x0;
                    StatMode = StateMode.H_BLANK_PERIOD;
                    state_irq_signal = false;
                }
                else
                {
                    UpdateStatIRQSignal();
                }
            }
        }

        private byte state_register = 0x80;
        private bool state_irq_signal;

        internal byte StateRegister
        {
            get => state_register;
            set
            {
                var s = value | 0x80;
                s &= 0xF8;
                state_register = (byte)s;
                IsLYLCCheckEnabled = (value & 0x40) == 0x40;
                Mode2OAMCheckEnabled = (value & 0x20) == 0x20;
                Mode1VBlankCheckEnabled = (value & 0x10) == 0x10;
                Mode0HBlankCheckEnabled = (value & 0x8) == 0x8;

                UpdateStatIRQSignal();
            }
        }

        private void UpdateStatIRQSignal()
        {
            if (!lcd_on) return;

            var old_irq_signal = state_irq_signal;

            CoincidenceFlag = ly_compare == LYRegister;

            state_irq_signal = (IsLYLCCheckEnabled && LYRegister == ly_compare) ||
                             (StatMode == StateMode.H_BLANK_PERIOD && Mode0HBlankCheckEnabled) ||
                             (StatMode == StateMode.OAM_RAM_PERIOD && Mode2OAMCheckEnabled) ||
                             (StatMode == StateMode.V_BLANK_PERIOD && (Mode1VBlankCheckEnabled || Mode2OAMCheckEnabled));

            if (!old_irq_signal && state_irq_signal)
            {
                device.interrupt_registers.RequestInterrupt(interrupts.Interrupt.LCD_STATE);
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
                    state_register |= 0x4;
                }
                else
                {
                    state_register &= 0xFB;
                }
            }
        }

        private StateMode state_mode;
        internal StateMode StatMode
        {
            get => state_mode;
            set
            {
                state_mode = value;
                state_register = (byte)((state_register & 0xFC) | (int)value);

                UpdateStatIRQSignal();
            }
        }
    }
}
