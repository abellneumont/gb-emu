namespace gbemu {

    internal class CPU {

        private Bus bus;
        internal Register register;

        private bool halted;
        private bool stopped;

        private int interrupt_cooldown;

        public CPU(Bus bus) {
            this.bus = bus;
            this.register = new Register();
        }

        private void Increment(ref byte value) {
            byte result = (byte) (value + 1);

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (value & 0x0f) + 1 > 0x0f);

            value = result;
        }

        internal void Decrement(ref byte value) {
            byte result = (byte) (value - 1);

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, true);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (value & 0x0f) < 0x01);

            value = result;
        }

        internal void Swap(ref byte value) {
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.CARRY_FLAG, false);
            value = (byte) ((value >> 4) | (value << 4));
            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0);
        }

        internal void Bit(byte value, int bit) {
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, true);
            register.SetFlag(CpuFlag.ZERO_FLAG, (value & (1 << bit)) == 0);
        }

        internal void Set(ref byte value, int bit) {
            value = (byte) (value | (1 << bit));
        }

        internal void RotateLeft(ref byte value) {
            bool carry = (value & 0x80) == 0x80;
            value = (byte)((value << 1) | (register.GetFlag(CpuFlag.CARRY_FLAG) ? 0x1 : 0x0));

            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, carry);
            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void RotateLeftCarry(ref byte value) {
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, value > 0x7f);

            value = (byte) ((value << 1) | (value >> 7));

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void RotateRight(ref byte value) {
            bool carry = (value & 0x1) == 0x1;
            value = (byte)((value >> 1) | (register.GetFlag(CpuFlag.CARRY_FLAG) ? 0x80 : 0x0));

            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, carry);
            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void RotateRightCarry(ref byte value) {
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, (value & 0x1) == 0x1);

            value = (byte) ((value >> 1) | ((value & 1) << 7));

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void AddHL(ushort value) {
            int result = register.HL + value;
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (register.HL & 0xfff) + (value & 0xfff) > 0xfff);
            register.SetFlag(CpuFlag.CARRY_FLAG, result > 0xffff);

            register.HL = (ushort) result;
        }

        private byte Read() {
            byte read = bus.Read(register.program_counter);
            register.program_counter += (ushort) (register.program_counter + 1);

            return read;
        }

        public int Tick() {
            int cycles = 0;

            if (interrupt_cooldown > 0) {
                interrupt_cooldown--;
            }

            register.program_counter++;

            byte opcode = Read();

            switch (opcode) {
                case 0x00:
                    break; // NO OP
                case 0x01:
                    cycles += 2;
                    register.BC = (ushort) (Read() | (Read()) << 8);
                    break;
                case 0x02:
                    cycles++;
                    bus.Write(register.BC, register.A);
                    break;
                case 0x03:
                    cycles++;
                    register.BC++;
                    break;
                case 0x04:
                    Increment(ref register.B);
                    break;
                case 0x05:
                    Decrement(ref register.B);
                    break;
                case 0x06:
                    cycles++;
                    register.B = Read();
                    break;
                case 0x07:
                    RotateLeftCarry(ref register.A);
                    register.SetFlag(CpuFlag.ZERO_FLAG, false);
                    break;
                case 0x08: {
                    cycles += 4;
                    ushort address = (ushort) (Read() | (Read() << 8));
                    bus.Write(address, (byte) (register.stack_pointer & 0xff));
                    address++;
                    bus.Write(address, (byte) (register.stack_pointer >> 8));
                    break;
                }
                case 0x09:
                    cycles++;
                    AddHL(register.BC);
                    break;
                case 0x0a:
                    cycles++;
                    register.A = bus.Read(register.BC);
                    break;
                case 0x0b:
                    cycles++;
                    register.BC = (ushort) (register.BC - 1);
                    break;
                case 0x0c:
                    Increment(ref register.C);
                    break;
                case 0x0d:
                    Decrement(ref register.C);
                    break;
                case 0x0e:
                    cycles++;
                    register.C = Read();
                    break;
                case 0x0f:
                    RotateRightCarry(ref register.A);
                    register.SetFlag(CpuFlag.ZERO_FLAG, false);
                    break;
                case 0x10: // stop instruction
                    stopped = true;
                    break;
                case 0x11:
                    cycles += 2;
                    register.DE = (ushort) (Read() | (Read() << 8));
                    break;
                case 0x12:
                    cycles++;
                    bus.Write(register.DE, register.A);
                    break;
                case 0x13:
                    cycles++;
                    register.DE++;
                    break;
                case 0x14:
                    Increment(ref register.D);
                    break;
                case 0x15:
                    Decrement(ref register.D);
                    break;
                case 0x16:
                    cycles++;
                    register.D = Read();
                    break;
                case 0x17:
                    RotateLeft(ref register.A);
                    register.SetFlag(CpuFlag.ZERO_FLAG, false);
                    break;
                case 0x18:
                    cycles += 2;
                    register.program_counter = (ushort) (register.program_counter + ((sbyte)Read()) & 0xffff);
                    break;
                case 0x19:
                    cycles++;
                    AddHL(register.DE);
                    break;
                case 0x1a:
                    cycles++;
                    register.A = bus.Read(register.DE);
                    break;
                case 0x1b:
                    cycles++;
                    register.DE--;
                    break;
                case 0x1c:
                    Increment(ref register.E);
                    break;
                case 0x1d:
                    Decrement(ref register.E);
                    break;
                case 0x1e:
                    cycles++;
                    register.E = Read();
                    break;
                case 0x1f:
                    RotateRight(ref register.A);
                    register.SetFlag(CpuFlag.ZERO_FLAG, false);
                    break;
                case 0x20: {
                    cycles++;
                    sbyte distance = (sbyte) Read();

                    if (register.GetFlag(CpuFlag.ZERO_FLAG))
                        break;

                    cycles++;
                    register.program_counter = (ushort) ((register.program_counter + distance) & 0xffff);
                    break;
                }
                case 0x21:
                    cycles += 2;
                    register.HL = (ushort) (Read() | (Read() << 8));
                    break;
                case 0x22:
                    cycles++;
                    bus.Write(register.HLI(), register.A);
                    break;
                case 0x23:
                    cycles++;
                    register.HL++;
                    break;
                case 0x24:
                    Increment(ref register.H);
                    break;
                case 0x25:
                    Decrement(ref register.H);
                    break;
                case 0x26:
                    cycles++;
                    register.H = Read();
                    break;
                case 0x27:
                    // TODO
                    break;
                case 0x28: {
                    cycles++;
                    sbyte distance = (sbyte) Read();

                    if (!register.GetFlag(CpuFlag.ZERO_FLAG))
                        break;

                    cycles++;
                    register.program_counter = (ushort) ((register.program_counter + distance) & 0xffff);
                    break;
                }
                case 0x29:
                    cycles++;
                    AddHL(register.HL);
                    break;
                case 0x2a:
                    cycles++;
                    register.A = bus.Read(register.HLI());
                    break;
                case 0x2b:
                    cycles++;
                    register.HL--;
                    break;
                case 0x2c:
                    Increment(ref register.L);
                    break;
                case 0x2d:
                    Decrement(ref register.L);
                    break;
                case 0x2e:
                    cycles++;
                    register.L = Read();
                    break;
                case 0x2f:
                    // TODO
                    break;
                case 0x30:
                    Swap(ref register.B);
                    break;
                case 0x31:
                    Swap(ref register.C);
                    break;
                case 0x32:
                    Swap(ref register.D);
                    break;
                case 0x33:
                    Swap(ref register.E);
                    break;
                case 0x34:
                    Swap(ref register.H);
                    break;
                case 0x35:
                    Swap(ref register.L);
                    break;
                case 0x36: {
                    cycles += 2;
                    byte read = bus.Read(register.HL);

                    Swap(ref read);
                    bus.Write(register.HL, read);
                    break;
                }
                case 0x37:
                    Swap(ref register.A);
                    break;
                case 0x38: {
                    cycles++;
                    sbyte distance = (sbyte) Read();

                    if (!register.GetFlag(CpuFlag.CARRY_FLAG))
                        break;

                    cycles++;
                    register.program_counter = (ushort) ((register.program_counter + distance) & 0xffff);
                    break;
                }
                // TODO: Shift right instructions
                case 0x40:
                    Bit(register.B, 0);
                    break;
                case 0x41:
                    Bit(register.C, 0);
                    break;
                case 0x42:
                    Bit(register.D, 0);
                    break;
                case 0x43:
                    Bit(register.E, 0);
                    break;
                case 0x44:
                    Bit(register.H, 0);
                    break;
                case 0x45:
                    Bit(register.L, 0);
                    break;
                case 0x46:
                    cycles++;
                    Bit(bus.Read(register.HL), 0);
                    break;
                case 0x47:
                    Bit(register.A, 0);
                    break;
                case 0x48:
                    Bit(register.B, 1);
                    break;
                case 0x49:
                    Bit(register.C, 1);
                    break;
                case 0x4a:
                    Bit(register.D, 1);
                    break;
                case 0x4b:
                    Bit(register.E, 1);
                    break;
                case 0x4c:
                    Bit(register.H, 1);
                    break;
                case 0x4d:
                    Bit(register.L, 1);
                    break;
                case 0x4e:
                    cycles++;
                    Bit(bus.Read(register.HL), 1);
                    break;
                case 0x4f:
                    Bit(register.A, 1);
                    break;
                case 0x50:
                    Bit(register.B, 2);
                    break;
                case 0x51:
                    Bit(register.C, 2);
                    break;
                case 0x52:
                    Bit(register.D, 2);
                    break;
                case 0x53:
                    Bit(register.E, 2);
                    break;
                case 0x54:
                    Bit(register.H, 2);
                    break;
                case 0x55:
                    Bit(register.L, 2);
                    break;
                case 0x56:
                    cycles++;
                    Bit(bus.Read(register.HL), 2);
                    break;
                case 0x5e:
                    cycles++;
                    register.E = bus.Read(register.HL);
                    break;
                case 0x62:
                    register.H = register.D;
                    break;
                case 0x7d:
                    register.A = register.L;
                    break;
                case 0xc0:
                    cycles++;

                    if (register.GetFlag(CpuFlag.ZERO_FLAG))
                        break;

                    cycles += 3;
                    register.program_counter = (ushort)(bus.Read(register.stack_pointer++) | (bus.Read(register.stack_pointer++) << 8));
                    break;
                case 0xc1:
                    cycles += 2;
                    register.BC = (ushort) (bus.Read(register.stack_pointer++) | (bus.Read(register.stack_pointer++) << 8));
                    break;
                case 0xc5:
                    cycles += 3;
                    bus.Write(--register.stack_pointer, register.B);
                    bus.Write(--register.stack_pointer, register.C);
                    break;
                case 0xd0:
                    cycles++;

                    if (register.GetFlag(CpuFlag.CARRY_FLAG))
                        break;

                    cycles += 3;
                    register.program_counter = (ushort) ((Read() | Read() << 8));
                    break;
                case 0xe1:
                    Set(ref register.C, 4);
                    break;
                case 0xe7:
                    Set(ref register.A, 4);
                    break;
                case 0xf0:
                    Set(ref register.B, 6);
                    break;
                case 0xf5:
                    Set(ref register.L, 6);
                    break;
                case 0xfa:
                    Set(ref register.D, 7);
                    break;
                case 0xff:
                    Set(ref register.A, 7);
                    break;
                default:
                    Console.WriteLine("Unimplemented opcode");
                    break;
            }

            Console.WriteLine(opcode);

            return cycles + 1;
        }
    }

}