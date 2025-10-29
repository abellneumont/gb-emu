namespace gbemu.cartridge
{
    
    internal class RomCartridge : Cartridge
    {

        public RomCartridge(byte[] data) : base(data) { }

        internal override byte ReadRom(ushort address)
        {
            if (address < ROM_BANK_SIZE)
                return data[address % data.Length];

            if (address < ROM_BANK_SIZE * 2)
            {
                int bank_address = address + (rom_bank - 1) * ROM_BANK_SIZE;
                return data[bank_address % data.Length];
            }

            return 0;
        }

        internal override void WriteRom(ushort address, byte value) { }

        internal override byte ReadRam(ushort address)
        {
            return 0xff;
        }

        internal override void WriteRam(ushort address, byte value) { }

    }
}