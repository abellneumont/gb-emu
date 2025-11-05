namespace gbemu.cartridge
{
    internal class MBC2Cartridge : Cartridge
    {
        public MBC2Cartridge(byte[] contents) : base(contents)
        {
            ram = new byte[0x200]; // MBC2 is special
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address < 0x4000)
            {
                if ((address & 0x100) == 0x100)
                {
                    rom_bank = value & 0b1111;
                    rom_bank = rom_bank == 0 ? 1 : rom_bank;
                }
                else
                {
                    ram_enabled = (value & 0x0F) == 0x0A;
                }
            }
        }

        internal override void WriteRam(ushort address, byte value)
        {
            if (!ram_enabled)
                return;

            ram[(address - RAM_ADDRESS_START) % 0x200] = (byte)((value & 0b1111) | 0b11110000);
        }

        internal override byte ReadRam(ushort address)
        {
            if (!ram_enabled)
                return 0xFF;

            return ram[(address - RAM_ADDRESS_START) % 0x200];
        }
    }
}
