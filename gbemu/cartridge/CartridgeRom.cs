using System;

namespace gbemu.cartridge
{

    public enum CartridgeRomType
    {
        A32KB = 0x00,
        A64KB = 0x01,
        A128KB = 0x02,
        A256KB = 0x03,
        A512KB = 0x04,
        A1MB = 0x05,
        A2MB = 0x06,
        A4MB = 0x07,
        A8MB = 0x08,
        A1p1MB = 0x52,
        A1p2MB = 0x53,
        A1p3MB = 0x54,
    }

    public static class CartridgeRom
    {

        public static int NumBanks(this CartridgeRomType romType)
        {
            return romType switch {
                CartridgeRomType.A32KB => 0,
                CartridgeRomType.A64KB => 4,
                CartridgeRomType.A128KB => 8,
                CartridgeRomType.A256KB => 16,
                CartridgeRomType.A512KB => 32,
                CartridgeRomType.A1MB => 64,
                CartridgeRomType.A2MB => 128,
                CartridgeRomType.A4MB => 256,
                CartridgeRomType.A8MB => 512,
                CartridgeRomType.A1p1MB => 72,
                CartridgeRomType.A1p2MB => 80,
                CartridgeRomType.A1p3MB => 96,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

}