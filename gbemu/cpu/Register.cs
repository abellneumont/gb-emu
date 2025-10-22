namespace gbemu {

    internal class Register { // https://gbdev.io/pandocs/CPU_Registers_and_Flags.html
        internal byte A, B, C, D, E, F, H, L;
        internal ushort program_counter, stack_pointer;

        internal ushort AF {
            get => (ushort) ((A << 8) | F);
            set {
                A = (byte) (value >> 8);
                F = (byte) (value & 0xf0);
            }
        }

        internal ushort BC {
            get => (ushort) ((B << 8) | C);
            set {
                B = (byte) (value >> 8);
                C = (byte) (value & 0xff);
            }
        }
        
        internal ushort DE {
            get => (ushort) ((D << 8) | E);
            set {
                D = (byte) (value >> 8);
                E = (byte) (value & 0xff);
            }
        }
        
        internal ushort HL {
            get => (ushort) ((H << 8) | L);
            set {
                H = (byte) (value >> 8);
                L = (byte) (value & 0xff);
            }
        }

        internal ushort HLI() {
            HL = (ushort) ((HL + 1) & 0xffff);
            return (ushort) ((HL - 1) & 0xffff);
        }

        internal ushort HLD() {
            HL = (ushort) ((HL - 1) & 0xFFFF);
            return (ushort) ((HL + 1) & 0xFFFF);
        }

        internal void SetFlag(CpuFlag flag, bool set) {
            if (set) {
                F |= (byte) flag;
            } else {
                F &= (byte) flag;
            }

            F &= 0x00f0;
        }

        internal bool GetFlag(CpuFlag flag) {
            return (F & (byte) flag) == (byte) flag;
        }

        public void Clear() {
            AF = 0;
            BC = 0;
            DE = 0;
            HL = 0;
            program_counter = 0;
            stack_pointer = 0;
        }

    }

    internal enum CpuFlag : byte {
        ZERO_FLAG = 0x80,
        SUBTRACT_FLAG = 0x40,
        HALF_CARRY_FLAG = 0x20,
        CARRY_FLAG = 0x10
    }

}