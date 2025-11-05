namespace gbemu.cartridge
{
    internal class MBC1Cartridge : Cartridge
    {
        private byte bank_register1;
        private byte bank_register2;
        private byte mode_register;
        private int offset_low;
        private int offset_high;

        public MBC1Cartridge(byte[] contents) : base(contents)
        {
            bank_register1 = 0x1;
            bank_register2 = 0x0;
            UpdateBankValues();
        }

        internal override byte ReadRom(ushort address)
        {
            var bankAddress = address switch
            {
                _ when (address < ROM_BANK_SIZE) => offset_low + address,
                _ when (address < ROM_BANK_SIZE * 2) => offset_high + address,
                _ => 0x0
            } % rom.Length; // TODO - Is this wrapping behavior correct?

            return rom[bankAddress];
        }

        internal override void WriteRom(ushort address, byte value)
        {
            if (address <= 0x1FFF)
                ram_enabled = (value & 0x0F) == 0x0A;
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                var regValue = value & 0x1F;
                bank_register1 = (byte)(regValue == 0x0 ? 0x1 : regValue);
            }
            else if (address >= 0x4000 && address <= 0x5FFF)
                bank_register2 = (byte)(value & 0x3);
            else if (address >= 0x6000 && address <= 0x7FFF)
                mode_register = (byte)(value & 0x1);

            UpdateBankValues();
        }

        private void UpdateBankValues()
        {
            ram_bank = (mode_register == 0x0 ? 0x0 : bank_register2);
            var romBank = bank_register2 << 5 | bank_register1;
            offset_high = (romBank - 1) * ROM_BANK_SIZE;
            offset_low = mode_register == 0x0 ? 0x0 : (bank_register2 << 5) * ROM_BANK_SIZE;
        }
    }
}
