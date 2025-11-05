namespace gbemu.cartridge
{
    internal class MBC5Cartridge : Cartridge
    {
        private byte rom_bank0;
        private byte rom_bank1;

        public MBC5Cartridge(byte[] contents) : base(contents)
        {
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
            {
                ram_enabled = (value & 0x0F) == 0x0A;
            }
            else if (address >= 0x2000 && address <= 0x2FFF)
            {
                rom_bank0 = value;
                rom_bank = ((rom_bank1 << 8) | rom_bank0) % ROMSize.NumberBanks();
            }
            else if (address >= 0x3000 && address <= 0x3FFF)
            {
                rom_bank1 = (byte)(value & 0b0000_0001);
                rom_bank = ((rom_bank1 << 8) | rom_bank0) % ROMSize.NumberBanks();
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
            {
                if (RAMSize == CartridgeRAMSize.NONE)
                    ram_bank = 0;
                else
                    ram_bank = (value & 8) % RAMSize.NumberBanks();
            }
        }
    }
}
