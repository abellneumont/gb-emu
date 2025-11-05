using System;

namespace gbemu.cartridge
{
    public static class CartridgeFactory
    {
        public static Cartridge CreateCartridge(byte[] contents)
        {
            if (contents.Length < 0x150)
            {
                return null;
            }

            return contents[0x147] switch
            {
                0x00 => new RomOnlyCartridge(contents), // ROM Only
                0x01 => new MBC1Cartridge(contents), // MBC1
                0x02 => new MBC1Cartridge(contents), // MBC1 + RAM
                0x03 => new MBC1Cartridge(contents), // MBC1 + RAM + Battery
                0x05 => new MBC2Cartridge(contents), // MBC2
                0x06 => new MBC2Cartridge(contents), // MBC2 + Battery
                0x0F => new MBC3Cartridge(contents), // MBC3 + Timer + Battery
                0x10 => new MBC3Cartridge(contents), // MBC3 + RAM + Timer + Battery
                0x11 => new MBC3Cartridge(contents), // MBC3
                0x12 => new MBC3Cartridge(contents), // MBC3 + RAM
                0x13 => new MBC3Cartridge(contents), // MBC3 + RAM + Battery
                0x19 => new MBC5Cartridge(contents), // MBC5
                0x1A => new MBC5Cartridge(contents), // MBC5 + RAM
                0x1B => new MBC5Cartridge(contents), // MBC5 + RAM + BATTERY
                0x1C => new MBC5Cartridge(contents), // MBC5 + RUMBLE
                0x1D => new MBC5Cartridge(contents), // MBC5 + RUMBLE + RAM
                0x1E => new MBC5Cartridge(contents), // MBC5 + RUMBLE + RAM + BATTERY
                _ => throw new NotSupportedException()
            };
        }
    }
}
