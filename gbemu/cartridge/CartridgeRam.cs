namespace gbemu {
    
    public enum CartridgeRamType : byte { // https://gbdev.io/pandocs/The_Cartridge_Header.html#0149--ram-size
        NO_RAM = 0x00,
        A8KiB = 0x02,
        A32KiB = 0x03,
        A128KiB = 0x04,
        A64KiB = 0x05
    }

    public static class CartridgeRam {

        public const int BANK_SIZE = 0x2000;

        public static int NumBanks(this CartridgeRamType ramType) {
            return ramType switch {
                CartridgeRamType.NO_RAM => 0,
                CartridgeRamType.A8KiB => 1,
                CartridgeRamType.A32KiB => 4,
                CartridgeRamType.A128KiB => 16,
                CartridgeRamType.A64KiB => 8,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

    }

}