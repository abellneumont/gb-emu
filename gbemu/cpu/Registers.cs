namespace gbemu.cpu
{

    internal class Registers
    {
        internal byte A;
        internal byte B;
        internal byte C;
        internal byte D;
        internal byte E;
        internal byte F;
        internal byte H;
        internal byte L;
        internal ushort program_counter;
        internal ushort stack_pointer;

        internal ushort AF
        {
            get => (ushort)((A << 8) | F);
            set
            {
                A = (byte)(value >> 8);
                F = (byte)(value & 0xF0);
            }
        }

        internal ushort BC
        {
            get => (ushort)((B << 8) | C);
            set
            {
                B = (byte)(value >> 8);
                C = (byte)(value & 0xFF);
            }
        }

        internal ushort DE
        {
            get => (ushort)((D << 8) | E);
            set
            {
                D = (byte)(value >> 8);
                E = (byte)(value & 0xFF);
            }
        }
        internal ushort HL
        {
            get => (ushort)((H << 8) | L);
            set
            {
                H = (byte)(value >> 8);
                L = (byte)(value & 0xFF);
            }
        }

        internal ushort HLI()
        {
            HL = (ushort)((HL + 1) & 0xFFFF);
            return (ushort)((HL - 1) & 0xFFFF);
        }

        internal ushort HLD()
        {
            HL = (ushort)((HL - 1) & 0xFFFF);
            return (ushort)((HL + 1) & 0xFFFF);
        }

        internal void SetFlag(CpuFlag flag, bool set)
        {
            switch (set)
            {
                case true:
                    F |= (byte)flag;
                    break;
                case false:
                    F &= (byte)~flag;
                    break;
            }

            F &= 0x00F0;
        }

        internal bool GetFlag(CpuFlag flag)
        {
            return (F & (byte)flag) == (byte)flag;
        }

        public void Clear()
        {
            AF = 0x0000;
            BC = 0x0000;
            DE = 0x0000;
            HL = 0x0000;
            program_counter = 0x0000;
            stack_pointer = 0x0000;
        }
    }

    internal enum Register16Bit
    {
        AF,
        BC,
        DE,
        HL,
        SP
    }
}
