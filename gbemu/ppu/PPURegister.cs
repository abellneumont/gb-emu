using System;

namespace gbemu.ppu
{
    internal class PPURegister
    {

        private PPU ppu;

        public PPURegister(PPU ppu)
        {
            this.ppu = ppu;
            this.PaletteData = new byte[2];
        }

        internal byte ScrollX { get; set; }

        internal byte ScrollY { get; set; }

        internal byte WindowX { get; set; }

        internal byte WindowY { get; set; }

        // TODO: Palettes
        internal byte BackgroundPalette { get; set; }

        internal byte[] PaletteData { get; private set; }

        internal byte LYRegister { get; set; }

        internal byte ly_compare;
        internal byte LYCompare
        {
            get => ly_compare;
            set
            {
                ly_compare = value;
                StateIRQSignal();
            }
        }

        internal Grayscale GetColor(int number, byte palette) => number switch
        {
            0 => (Grayscale)(palette & 0x3),
            1 => (Grayscale)((palette >> 2) & 0x3),
            2 => (Grayscale)((palette >> 4) & 0x3),
            3 => (Grayscale)((palette >> 6) & 0x3),
            _ => throw new ArgumentOutOfRangeException()
        };

        internal bool LcdOn { get; private set; }

        internal bool WindowEnabled { get; private set; }

        internal bool SignedTileData { get; private set; }

        internal bool LargeSprites { get; private set; }

        internal bool SpritesEnabled { get; private set; }

        internal bool BackgroundEnabled { get; private set; }

        internal bool LYLCEnabled { get; private set; }

        internal bool Mode2OAMEnabled { get; private set; }

        internal bool Mode1VBlankEnabled { get; private set; }

        internal bool Mode0HBlankEnabled { get; private set; }

        internal int WindowTileMapOffset { get; private set; }

        internal int BackgroundTileMapOffset { get; private set; }

        internal int BackgroundWindowTileMapOffset { get; private set; }

        internal byte lcd_control;

        internal byte LCDControl
        {
            get => lcd_control;
            set
            {
                lcd_control = value;
                LcdOn = (value & 0x80) == 0x80;
                WindowTileMapOffset = (value & 0x40) == 0x40 ? 0x9c00 : 0x9800;
                WindowEnabled = (value & 0x20) == 0x20;
                BackgroundWindowTileMapOffset = (value & 0x10) == 0x10 ? 0x8000 : 0x8800;
                SignedTileData = BackgroundWindowTileMapOffset == 0x8800;
                BackgroundTileMapOffset = (value & 0x8) == 0x8 ? 0x9c00 : 0x9800;
                LargeSprites = (value & 0x4) == 0x4;
                SpritesEnabled = (value & 0x2) == 0x2;
                BackgroundEnabled = (value & 0x1) == 0x1;

                if (LcdOn)
                {
                    StateIRQSignal();
                } else
                {
                    ppu.DisableLCD();
                    LYRegister = 0x0;
                    ppu_state = PPUState.H_BLANK_PERIOD;
                    state_irq_signal = false;
                }
            }
        }

        internal byte state_register = 0x80;
        internal bool state_irq_signal;

        internal byte StateRegister
        {
            get => state_register;
            set
            {
                state_register = (byte)((value | 0x80) & 0xf8);
                LYLCEnabled = (value & 0x40) == 0x40;
                Mode2OAMEnabled = (value & 0x20) == 0x20;
                Mode1VBlankEnabled = (value & 0x10) == 0x10;
                Mode0HBlankEnabled = (value & 0x8) == 0x8;

                StateIRQSignal();
            }
        }

        internal bool coincidence_flag;

        internal bool CoincidenceFlag
        {
            get => coincidence_flag;
            set
            {
                coincidence_flag = value;

                if (value)
                    state_register |= 0x4;
                else
                    state_register &= 0xfb;
            }
        }

        internal PPUState ppu_state;

        internal PPUState StateMode
        {
            get => ppu_state;
            set
            {
                ppu_state = value;
                state_register = (byte)((state_register & 0xfc) | (int)value);

                StateIRQSignal();
            }
        }

        private void StateIRQSignal()
        {
            if (!LcdOn)
                return;

            bool oldSignal = state_irq_signal;

            CoincidenceFlag = ly_compare == LYRegister;
            state_irq_signal = (LYLCEnabled && LYRegister == ly_compare) ||
                (StateMode == PPUState.H_BLANK_PERIOD && Mode0HBlankEnabled) ||
                (StateMode == PPUState.V_BLANK_PERIOD && Mode1VBlankEnabled) ||
                (StateMode == PPUState.OAM_RAM_PERIOD && (Mode2OAMEnabled || Mode1VBlankEnabled));

            if (!oldSignal && state_irq_signal)
                ppu.bus.RequestInterrupt(InterruptType.LCD_STATE);
        }

    }
}
