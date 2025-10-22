namespace gbemu {

    internal class Bus {

        internal CPU cpu;
        internal Cartridge cartridge;

        internal byte[] memory;

        public Bus(Cartridge cartridge) {
            this.cpu = new CPU(this);
            this.cartridge = cartridge;
        }

        public void init() {
            cpu.register.AF = 0x01b0;
            cpu.register.BC = 0x0013;
            cpu.register.DE = 0x00d8;
            cpu.register.HL = 0x014d;
            cpu.register.program_counter = 0x0100;
            cpu.register.stack_pointer = 0xfffe;

            Write(0xff40, 0x91);
            Write(0xff47, 0xfc);
            Write(0xff48, 0xff);
            Write(0xff49, 0xff);
            Write(0xff50, 0x1);
        }

        public byte Read(ushort address) {
            if (address <= 0x7fff) {
                return cartridge.ReadRom(address);
            }

            if (address <= 0x9fff) {
                return 0xff; // TODO: VRAM
            }

            if (address <= 0xbfff) {
                return cartridge.ReadRam(address);
            }

            return 0;
        }

        public void Write(ushort address, byte value) {
            if (address <= 0x7fff) {
                cartridge.WriteRom(address, value);
                return;
            }

            if (address <= 0x9fff) {
                // TODO: VRAM
                return;
            }

            if (address <= 0xbfff) {
                cartridge.WriteRam(address, value);
                return;
            }
        }

    }

}