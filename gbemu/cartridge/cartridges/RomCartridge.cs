namespace gbemu {
    
    internal class RomCartridge : Cartridge {

        public RomCartridge(byte[] data) : base(data) { }

        internal override byte ReadRom(ushort address) {
            return data[address % data.Length];
        }

        internal override void WriteRom(ushort address, byte value) { }

        internal override byte ReadRam(ushort address) {
            if (ram.Length == 0 || !ram_enabled) {
                return 0xff;
            }

            int bank_address = (address - RAM_ADDRESS_START + ram_bank * CartridgeRam.BANK_SIZE) % ram.Length;

            return ram[bank_address];
        }

        internal override void WriteRam(ushort address, byte value) {
            if (ram.Length > 0 && ram_enabled) {
                int bank_address = (address - RAM_ADDRESS_START + ram_bank * CartridgeRam.BANK_SIZE) % ram.Length;

                ram[bank_address] = value;
            }
        }

    }
}