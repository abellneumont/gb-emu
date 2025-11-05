using System;

namespace gbemu.cartridge
{
    internal class MBC3Cartridge : Cartridge
    {
        private byte rtc_seconds;
        private byte rtc_minutes;
        private byte rtc_hours;
        private byte rtc_day_low;
        private byte rtc_day_high;
        private byte? mapped_rtc;
        private DateTime? time;

        public MBC3Cartridge(byte[] contents) : base(contents) { }

        internal override byte ReadRam(ushort address)
        {
            if (!ram_enabled)
                return 0xFF;

            if (mapped_rtc.HasValue)
                return mapped_rtc.Value;

            return base.ReadRam(address);
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
                ram_enabled = (value & 0x0F) == 0x0A;
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                rom_bank = (value & 0x7f) % ROMSize.NumberBanks();
                
                if (rom_bank == 0x0)
                    rom_bank = 0x1;
            }
            else if (address >= 0x4000 && address < 0x5FFF)
            {
                if (value <= 0x3)
                {
                    ram_bank = value % RAMSize.NumberBanks();
                    mapped_rtc = null;
                }
                else if (value >= 0x8 && value <= 0xC)
                {
                    mapped_rtc = value switch
                    {
                        0x8 => rtc_seconds,
                        0x9 => rtc_minutes,
                        0xA => rtc_hours,
                        0xB => rtc_day_low,
                        0xC => rtc_day_high,
                        _ => throw new ArgumentOutOfRangeException(nameof(value), value, $"Value {value} isn't mapped to an RTC register")
                    };
                }
            }
            else if (address < 0x7FFF)
            {
                if (value == 0x1 && time.HasValue)
                    time = null;
                else if (value == 0x1)
                    time = DateTime.UtcNow;
            }
        }
    }
}
