namespace gbemu.cartridge
{

    internal class MBC2Cartridge : RomCartridge {

        public MBC2Cartridge(byte[] data) : base(data)
        {
            this.ram = new byte[0x200]; // MBC2 RAM is special
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address < 0x4000)
            {
                if ((address & 0x100) == 0x100)
                {
                    rom_bank = value & 0xf;
                    rom_bank = rom_bank == 0 ? 1 : rom_bank;
                }
                else
                    ram_enabled = (value & 0x0f) == 0x0a;
            }
        }

        internal override void WriteRam(ushort address, byte value)
        {
            if (ram_enabled)
                ram[(address - RAM_ADDRESS_START) % ram.Length] = (byte) ((value & 0xf) | 0xf0);
        }

    }

}