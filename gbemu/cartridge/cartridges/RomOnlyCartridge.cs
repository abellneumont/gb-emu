namespace gbemu.cartridge
{
    internal class RomOnlyCartridge : Cartridge
    {
        public RomOnlyCartridge(byte[] contents) : base(contents)
        {
        }

        internal override byte ReadRam(ushort address)
        {
            return 0xFF;
        }

        internal override void WriteRom(ushort address, byte value)
        {
        }

        internal override void WriteRam(ushort address, byte value)
        {
        }
    }
}
