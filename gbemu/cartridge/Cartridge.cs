using System;
using System.IO;
using System.Linq;

namespace gbemu.cartridge
{

    public abstract class Cartridge
    {

        public static Cartridge Create(byte[] data)
        {
            if (data.Length < 0x150)
                throw new InvalidDataException();

            return (CartridgeType) data[0x147] switch
            {
                CartridgeType.ROM => new RomCartridge(data),
                CartridgeType.MBC1 => new MBC1Cartridge(data),
                CartridgeType.MBC1_RAM => new MBC1Cartridge(data),
                CartridgeType.MBC1_RAM_BATTERY => new MBC1Cartridge(data),
                CartridgeType.MBC2 => new MBC2Cartridge(data),
                CartridgeType.MBC2_BATTERY => new MBC2Cartridge(data),
                CartridgeType.ROM_RAM => throw new NotSupportedException(),
                CartridgeType.ROM_RAM_BATTERY => throw new NotSupportedException(),
                CartridgeType.MMM01 => throw new NotSupportedException(),
                CartridgeType.MMM01_RAM => throw new NotSupportedException(),
                CartridgeType.MMM01_RAM_BATTERY => throw new NotSupportedException(),
                CartridgeType.MBC3_TIMER_BATTERY => new MBC3Cartridge(data),
                CartridgeType.MBC3_TIMER_RAM_BATTERY => new MBC3Cartridge(data),
                CartridgeType.MBC3 => new MBC3Cartridge(data),
                CartridgeType.MBC3_RAM => new MBC3Cartridge(data),
                CartridgeType.MBC3_RAM_BATTERY => new MBC3Cartridge(data),
                CartridgeType.MBC5 => new MBC5Cartridge(data),
                CartridgeType.MBC5_RAM => new MBC5Cartridge(data),
                CartridgeType.MBC5_RAM_BATTERY => new MBC5Cartridge(data),
                CartridgeType.MBC5_RUMBLE => new MBC5Cartridge(data),
                CartridgeType.MBC5_RUMBLE_RAM => new MBC5Cartridge(data),
                CartridgeType.MBC5_RUMBLE_RAM_BATTERY => new MBC5Cartridge(data),
                CartridgeType.MBC6 => throw new NotSupportedException(),
                CartridgeType.MBC7_SENSOR_RUMBLE_RAM_BATTERY => throw new NotSupportedException(),
                CartridgeType.POCKET_CAMERA => throw new NotSupportedException(),
                CartridgeType.BANDAI_TAMA5 => throw new NotSupportedException(),
                CartridgeType.HUC3 => throw new NotSupportedException(),
                CartridgeType.HUC1_RAM_BATTERY => throw new NotSupportedException(),
                _ => throw new InvalidDataException()
            };
        }

        public const int ROM_BANK_SIZE = 0x4000;
        public const int RAM_ADDRESS_START = 0xa000;

        protected readonly byte[] data;
        protected byte[] ram;

        protected bool ram_enabled;
        protected int rom_bank = 1, ram_bank = 0;

        internal Cartridge(byte[] data)
        {
            this.data = data;
            this.ram = new byte[RAM_TYPE.NumBanks() * CartridgeRam.BANK_SIZE];
            this.ram_enabled = false;
        }

        public CartridgeType CARTRIDGE_TYPE => (CartridgeType) data[0x147];

        public CartridgeRomType ROM_TYPE => (CartridgeRomType) data[0x148];

        public CartridgeRamType RAM_TYPE => (CartridgeRamType) data[0x149];

        public bool validateHeader()
        { // https://gbdev.io/pandocs/The_Cartridge_Header.html#014d--header-checksum
            int checksum = data[0x134..0x14d].Aggregate(0, (total, each) => total - each - 1);

            return checksum == data[0x14d];
        }

        public bool validateROM()
        { // https://gbdev.io/pandocs/The_Cartridge_Header.html#014e-014f--global-checksum
            int checksum = data[..0x14d].Aggregate(0, (total, each) => total + each);

            return checksum == (ushort) (data[0x14e] << 8 | data[0x14f]);
        }

        internal abstract byte ReadRom(ushort address);

        internal abstract void WriteRom(ushort address, byte value);

        internal abstract byte ReadRam(ushort address);

        internal abstract void WriteRam(ushort address, byte value);
    }

}