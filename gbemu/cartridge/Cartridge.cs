using System.Linq;
using System.Text;

namespace gbemu.cartridge
{
    public abstract class Cartridge
    {
        protected const int ROM_BANK_SIZE = 0x4000;
        protected const int RAM_ADDRESS_START = 0xA000;

        protected bool ram_enabled;

        protected int ram_bank;
        protected int rom_bank;

        protected readonly byte[] rom;
        protected byte[] ram;

        internal Cartridge(byte[] data)
        {
            rom = data;
            ram = new byte[RAMSize.NumberBanks() * RAMSize.BankSizeBytes()];
            ram_bank = 0x0;
            rom_bank = 0x1;
            ram_enabled = false;
        }

        internal virtual byte ReadRom(ushort address)
        {
            if (address < ROM_BANK_SIZE)
            {
                return rom[address % rom.Length];
            }

            if (address < ROM_BANK_SIZE * 2)
            {
                var bankAddress = address + (rom_bank - 1) * ROM_BANK_SIZE;
                return rom[bankAddress % rom.Length];
            }

            return 0x0;
        }

        internal virtual byte ReadRam(ushort address)
        {
            if (!ram_enabled || RAMSize == CartridgeRAMSize.NONE)
                return 0xFF;

            var bankedAddress = (address - RAM_ADDRESS_START + ram_bank * RAMSize.BankSizeBytes()) % ram.Length;

            return ram[bankedAddress];
        }

        internal abstract void WriteRom(ushort address, byte value);

        internal virtual void WriteRam(ushort address, byte value)
        {
            if (!ram_enabled || RAMSize == CartridgeRAMSize.NONE)
                return;

            var bankedAddress = (address - RAM_ADDRESS_START + ram_bank * RAMSize.BankSizeBytes()) % ram.Length;

            ram[bankedAddress] = value;
        }

        public string GameTitle => Encoding.ASCII.GetString(rom[0x134..0x13F]);

        public string ManufacturerCode => Encoding.ASCII.GetString(rom[0x13F..0x143]);

        public string MakerCode => rom[0x14B] switch
        {
            0x33 => Encoding.ASCII.GetString(rom[0x144..0x146]),
            _ => Encoding.ASCII.GetString(new[] { rom[0x14B] })
        };

        public CartridgeROMSize ROMSize => (CartridgeROMSize)rom[0x148];

        public CartridgeRAMSize RAMSize => (CartridgeRAMSize)rom[0x149];

        public CartridgeDestinationCode DestinationCode => (CartridgeDestinationCode)rom[0x14A];

        public byte RomVersion => rom[0x14C];

        public byte HeaderChecksum => rom[0x14D];

        public ushort ROMChecksum => (ushort)(rom[0x14E] << 8 | rom[0x14F]);

        public bool IsHeaderValid()
        {
            var calculatedChecksum = rom[0x134..0x14D].Aggregate(0, (c, b) => c - b - 1);

            return (byte)calculatedChecksum == HeaderChecksum;
        }

        public bool IsROMChecksumValid()
        {
            var calculatedChecksum = rom[..0x14D].Aggregate(0, (i, b) => i + b);
            return (ushort)calculatedChecksum == ROMChecksum;
        }

        public override string ToString()
        {
            return $"{GameTitle} - {ManufacturerCode} - {MakerCode}";
        }
    }
}
