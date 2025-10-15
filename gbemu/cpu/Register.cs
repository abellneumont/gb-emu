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
    }

}