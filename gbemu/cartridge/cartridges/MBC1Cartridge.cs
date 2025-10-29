namespace gbemu.cartridge {

    internal class MBC1Cartridge : RomCartridge
    {

        private readonly byte[] bank_registers;
        private byte mode_register;
        private int offset_low_rom, offset_high_rom;

        public MBC1Cartridge(byte[] data) : base(data)
        {
            this.bank_registers = new byte[2];
            this.bank_registers[0] = 0x1;

            UpdateBank();
        }

        internal override byte ReadRom(ushort address)
        {
            int bank_address = address switch {
                _ when (address < ROM_BANK_SIZE) => offset_low_rom + address,
                _ when (address < ROM_BANK_SIZE * 2) => offset_high_rom + address,
                _ => 0x0
            };

            return data[bank_address];
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1fff)
                ram_enabled = (value & 0x0f) == 0x0a;
            
            if (address >= 0x2000 && address <= 0x3fff)
            {
                int register = value & 0x1f;
                bank_registers[0] = (byte) (register == 0x0 ? 0x1 : register);
            }
            
            if (address >= 0x4000 && address <= 0x5fff)
                bank_registers[1] = (byte) (value & 0x3);
            
            if (address >= 0x6000 && address <= 0x7fff)
                mode_register = (byte) (value & 0x1);

            UpdateBank();
        }

        private void UpdateBank()
        {
            this.ram_bank = (mode_register == 0x0 ? 0x0 : bank_registers[1]);

            offset_low_rom = mode_register == 0x0 ? 0x0 : (bank_registers[1] << 5) * ROM_BANK_SIZE;
            offset_high_rom = ((bank_registers[1] << 5 | bank_registers[0]) - 1) * ROM_BANK_SIZE;
        }
    }
}