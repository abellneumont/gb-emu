namespace gbemu.cartridge
{

    internal class MBC5Cartridge : RomCartridge
    {

        private readonly byte[] bank_registers;

        public MBC5Cartridge(byte[] data) : base(data)
        {
            this.bank_registers = new byte[2];
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1fff)
                ram_enabled = (value & 0x0f) == 0x0a;

            if (address >= 0x2000 && address <= 0x2fff)
            {
                bank_registers[0] = value;
                rom_bank = ((bank_registers[1] << 8) | bank_registers[0]) % ROM_TYPE.NumBanks();
            }

            if (address >= 0x3000 && address <= 0x3fff)
            {
                bank_registers[1] = (byte) (value & 0b00000001); // Only last bit is used?
                rom_bank = ((bank_registers[1] << 8) | bank_registers[0]) % ROM_TYPE.NumBanks();
            }

            if (address >= 0x4000 && address <= 0x5fff)
            {
                if (ram.Length == 0)
                    ram_bank = 0;
                else
                    ram_bank = (value & 8) % RAM_TYPE.NumBanks();
            }
        }

    }

}