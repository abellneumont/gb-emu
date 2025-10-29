namespace gbemu
{
    internal class Memory
    {

        internal readonly byte[] wram, hram;

        public Memory()
        {
            this.wram = new byte[0x2000];
            this.hram = new byte[0x7f];
        }

        public void WriteWide(ushort address, byte value)
        {
            wram[address] = value;
        }

        public byte ReadWide(ushort address)
        {
            return wram[address];
        }

        public void Write(ushort address, byte value)
        {
            hram[address] = value;
        }

        public byte Read(ushort address)
        {
            return hram[address];
        }

    }
}
