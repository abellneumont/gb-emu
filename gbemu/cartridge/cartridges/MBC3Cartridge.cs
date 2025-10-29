using System;

namespace gbemu.cartridge
{

    internal class MBC3Cartridge : RomCartridge
    {

        public enum TimerType : byte
        {

            SECONDS = 0x08,
            MINUTES = 0x09,
            HOURS = 0xa,
            DAYS_LOW = 0xb,
            DAYS_HIGH = 0xc

        }

        private DateTime? paused_time;
        private TimerType? timer_selection;

        public MBC3Cartridge(byte[] data) : base(data) { }

        internal override byte ReadRam(ushort address)
        {
            if (!ram_enabled)
                return 0xff;

            if (timer_selection.HasValue)
            {
                DateTime time = paused_time.HasValue ? paused_time.Value : DateTime.UtcNow;

                return timer_selection switch
                {
                    TimerType.SECONDS => (byte) time.Second,
                    TimerType.MINUTES => (byte) time.Minute,
                    TimerType.HOURS => (byte) time.Hour,
                    TimerType.DAYS_LOW => (byte) (time.DayOfYear & 0xff),
                    TimerType.DAYS_HIGH => (byte) ((time.DayOfYear >> 8) & 0xff),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return base.ReadRam(address);
        }

        internal override void WriteRom(ushort address, byte value) {
            if (address <= 0x1fff)
                ram_enabled = (value & 0x0f) == 0x0a;
            
            if (address >= 0x2000 && address <= 0x3fff)
            {
                rom_bank = (value & 0x7f) % ROM_TYPE.NumBanks();

                if (rom_bank == 0x0)
                    rom_bank = 0x1;
            }
            
            if (address >= 0x4000 && address <= 0x5fff)
            {
                if (value <= 0x3)
                {
                    ram_bank = (value & 0x7f) % RAM_TYPE.NumBanks();
                    timer_selection = null;
                }
                else if (value >= 0x8 && value <= 0xc)
                    timer_selection = (TimerType) value;
            }

            if (address < 0x7fff)
            {
                if (value == 0x1)
                {
                    if (paused_time.HasValue)
                        paused_time = null;
                    else
                        paused_time = DateTime.UtcNow;
                }
            }
        }

    }
}