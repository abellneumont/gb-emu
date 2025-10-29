using System;
using System.Collections.Generic;

namespace gbemu.cpu
{

    internal class CPU
    {

        private Bus bus;
        internal CPURegister register;

        private bool halted, stopped, processing;
        private int interrupt_cooldown;

        public CPU(Bus bus)
        {
            this.bus = bus;
            this.register = new CPURegister();
        }

        private void Increment(ref byte value)
        {
            byte result = (byte) (value + 1);

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (value & 0x0f) + 1 > 0x0f);

            value = result;
        }

        internal void Decrement(ref byte value)
        {
            byte result = (byte) (value - 1);

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, true);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (value & 0x0f) < 0x01);

            value = result;
        }

        internal void ShiftLeft(ref byte value)
        {
            register.SetFlag(CpuFlag.CARRY_FLAG, (value & 0x80) == 0x80);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);

            value = (byte)(value << 1);

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0);
        }

        internal void ShiftRight(ref byte value)
        {
            register.SetFlag(CpuFlag.CARRY_FLAG, (value & 0x01) == 0x01);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);

            value = (byte)(value >> 1);

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0);
        }

        internal void ShiftRightAdjust(ref byte value)
        {
            register.SetFlag(CpuFlag.CARRY_FLAG, (value & 0x01) == 0x01);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);

            value = (byte)((value >> 1) | (value & 0x80));

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0);
        }

        internal void Swap(ref byte value)
        {
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.CARRY_FLAG, false);
            value = (byte) ((value >> 4) | (value << 4));
            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0);
        }

        internal void Bit(byte value, int bit)
        {
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, true);
            register.SetFlag(CpuFlag.ZERO_FLAG, (value & (1 << bit)) == 0);
        }

        internal void Res(ref byte value, int bit)
        {
            value = (byte)(value & ~(1 << bit));
        }

        internal void Set(ref byte value, int bit)
        {
            value = (byte) (value | (1 << bit));
        }

        internal void Add(ref byte first, byte second, bool carry)
        {
            int c = carry && register.GetFlag(CpuFlag.CARRY_FLAG) ? 1 : 0;
            int result = first + second + c;

            register.SetFlag(CpuFlag.ZERO_FLAG, (result & 0xff) == 0x0);
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (((first & 0xf) + (second & 0xf) + (c & 0xf)) & 0x10) == 0x10);
            register.SetFlag(CpuFlag.CARRY_FLAG, result > 0xff);

            first = (byte)result;
        }

        internal void Sub(ref byte first, byte second, bool carry)
        {
            int c = carry && register.GetFlag(CpuFlag.CARRY_FLAG) ? 1 : 0;
            int result = first - second - c;

            register.SetFlag(CpuFlag.ZERO_FLAG, (result & 0xff) == 0x0);
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, true);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (first & 0x0f) < (second & 0x0f) + c);
            register.SetFlag(CpuFlag.CARRY_FLAG, result < 0);

            first = (byte)result;
        }

        internal void And(ref byte first, byte second)
        {
            int result = first & second;

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, true);

            first = (byte)result;
        }

        internal void Xor(ref byte first, byte second)
        {
            int result = first ^ second;

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.HALF_CARRY_FLAG, false);

            first = (byte)result;
        }

        internal void Or(ref byte first, byte second)
        {
            int result = first | second;

            register.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            register.SetFlag(CpuFlag.CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.HALF_CARRY_FLAG, false);

            first = (byte)result;
        }

        internal void RotateLeft(ref byte value)
        {
            bool carry = (value & 0x80) == 0x80;
            value = (byte)((value << 1) | (register.GetFlag(CpuFlag.CARRY_FLAG) ? 0x1 : 0x0));

            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, carry);
            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void RotateLeftCarry(ref byte value)
        {
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, value > 0x7f);

            value = (byte) ((value << 1) | (value >> 7));

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void RotateRight(ref byte value)
        {
            bool carry = (value & 0x1) == 0x1;
            value = (byte)((value >> 1) | (register.GetFlag(CpuFlag.CARRY_FLAG) ? 0x80 : 0x0));

            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, carry);
            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void RotateRightCarry(ref byte value)
        {
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.CARRY_FLAG, (value & 0x1) == 0x1);

            value = (byte) ((value >> 1) | ((value & 1) << 7));

            register.SetFlag(CpuFlag.ZERO_FLAG, value == 0x0);
        }

        internal void DecimalAdjustRegister(ref byte address)
        {
            int tmp = address;

            if (!register.GetFlag(CpuFlag.SUBTRACT_FLAG))
            {
                if (register.GetFlag(CpuFlag.HALF_CARRY_FLAG) || (tmp & 0x0F) > 9)
                    tmp += 0x06;
                if (register.GetFlag(CpuFlag.CARRY_FLAG) || tmp > 0x9F)
                    tmp += 0x60;
            }
            else
            {
                if (register.GetFlag(CpuFlag.HALF_CARRY_FLAG))
                {
                    tmp -= 0x06;
                    if (!register.GetFlag(CpuFlag.CARRY_FLAG))
                        tmp &= 0xFF;
                }
                if (register.GetFlag(CpuFlag.CARRY_FLAG))
                    tmp -= 0x60;
            }

            address = (byte)tmp;
            register.SetFlag(CpuFlag.ZERO_FLAG, address == 0x0);

            if (tmp > 0xFF)
                register.SetFlag(CpuFlag.CARRY_FLAG, true);

            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, false);
        }

        internal void AddHL(ushort value)
        {
            int result = register.HL + value;
            register.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            register.SetFlag(CpuFlag.HALF_CARRY_FLAG, (register.HL & 0xfff) + (value & 0xfff) > 0xfff);
            register.SetFlag(CpuFlag.CARRY_FLAG, result > 0xffff);

            register.HL = (ushort) result;
        }

        private byte Read()
        {
            byte read = bus.Read(register.program_counter);
            register.program_counter++;

            return read;
        }

        public IEnumerable<int> Tick()
        {
            while (true)
            {
                if (interrupt_cooldown > 0)
                {
                    interrupt_cooldown--;

                    if (interrupt_cooldown == 0)
                        bus.InterruptsGloballyEnabled = true;
                }

                if (!bus.dma.BlockInterrupt())
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int mask = 1 << i;

                        if ((bus.InterruptEnable & bus.InterruptFlags & mask) == mask)
                        {
                            if (halted)
                                halted = false;

                            if (stopped)
                            {
                                stopped = false;
                                yield return 1;
                            }

                            if (bus.InterruptsGloballyEnabled)
                            {
                                InterruptType type = (InterruptType)i;
                                bus.InterruptsGloballyEnabled = false;
                                bus.ResetInterrupt(type);

                                yield return 1;
                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xff));
                                yield return 1;

                                register.program_counter = type.StartingAddress();
                                yield return 1;
                            }
                        }
                    }
                }

                if (halted || stopped || bus.dma.BlockInterrupt())
                {
                    yield return 0;
                } else
                {
                    byte opcode = Read();
                    processing = true;

                    //Console.WriteLine(opcode);

                    switch (opcode)
                    {
                        case 0x00:
                            break;
                        case 0x01:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                register.BC = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x02:
                            bus.Write(register.BC, register.A);
                            yield return 1;
                            break;
                        case 0x03:
                            register.BC++;
                            yield return 1;
                            break;
                        case 0x04:
                            Increment(ref register.B);
                            break;
                        case 0x05:
                            Decrement(ref register.B);
                            break;
                        case 0x06:
                            {
                                byte read = Read();
                                yield return 1;
                                register.B = read;
                                break;
                            }
                        case 0x07:
                            RotateLeftCarry(ref register.A);
                            register.SetFlag(CpuFlag.ZERO_FLAG, false);
                            break;
                        case 0x08:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                ushort address = (ushort)(first | (second << 8));
                                bus.Write(address, (byte)(register.stack_pointer & 0xFF));
                                yield return 1;
                                address++;
                                bus.Write(address, (byte)(register.stack_pointer >> 8));
                                yield return 1;
                                break;
                            }
                        case 0x09:
                            AddHL(register.BC);
                            yield return 1;
                            break;
                        case 0x0A:
                            {
                                byte read = bus.Read(register.BC);
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0x0B:
                            register.BC = (ushort)(register.BC - 1);
                            yield return 1;
                            break;
                        case 0x0C:
                            Increment(ref register.C);
                            break;
                        case 0x0D:
                            Decrement(ref register.C);
                            break;
                        case 0x0E:
                            {
                                byte read = Read();
                                yield return 1;
                                register.C = read;
                                break;
                            }
                        case 0x0F:
                            RotateRightCarry(ref register.A);
                            register.SetFlag(CpuFlag.ZERO_FLAG, false);
                            break;
                        case 0x10: // STOP
                            {
                                stopped = true;
                                break;
                            }
                        case 0x11:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                register.DE = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x12:
                            bus.Write(register.DE, register.A);
                            yield return 1;
                            break;
                        case 0x13:
                            register.DE++;
                            yield return 1;
                            break;
                        case 0x14:
                            Increment(ref register.D);
                            break;
                        case 0x15:
                            Decrement(ref register.D);
                            break;
                        case 0x16:
                            {
                                byte read = Read();
                                yield return 1;
                                register.D = read;
                                break;
                            }
                        case 0x17:
                            RotateLeft(ref register.A);
                            register.SetFlag(CpuFlag.ZERO_FLAG, false);
                            break;
                        case 0x18:
                            {
                                sbyte read = (sbyte)Read();
                                yield return 1;
                                register.program_counter = (ushort)((register.program_counter + read) & 0xFFFF);
                                yield return 1;
                                break;
                            }
                        case 0x19:
                            AddHL(register.DE);
                            yield return 1;
                            break;
                        case 0x1A:
                            {
                                byte read = bus.Read(register.DE);
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0x1B:
                            register.DE--;
                            yield return 1;
                            break;
                        case 0x1C:
                            Increment(ref register.E);
                            break;
                        case 0x1D:
                            Decrement(ref register.E);
                            break;
                        case 0x1E:
                            {
                                byte read = Read();
                                yield return 1;
                                register.E = read;
                                break;
                            }
                        case 0x1F:
                            RotateRight(ref register.A);
                            register.SetFlag(CpuFlag.ZERO_FLAG, false);
                            break;
                        case 0x20:
                            {
                                sbyte distance = (sbyte)Read();
                                yield return 1;

                                if (register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)((register.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x21:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                register.HL = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x22:
                            {
                                ushort address = register.HLI();
                                bus.Write(address, register.A);
                                yield return 1;
                                break;
                            }
                        case 0x23:
                            yield return 1;
                            register.HL++;
                            break;
                        case 0x24:
                            Increment(ref register.H);
                            break;
                        case 0x25:
                            Decrement(ref register.H);
                            break;
                        case 0x26:
                            {
                                byte read = Read();
                                yield return 1;
                                register.H = read;
                                break;
                            }
                        case 0x27:
                            DecimalAdjustRegister(ref register.A);
                            break;
                        case 0x28:
                            {
                                sbyte distance = (sbyte)Read();
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)((register.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x29:
                            yield return 1;
                            AddHL(register.HL);
                            break;
                        case 0x2A:
                            {
                                byte read = bus.Read(register.HLI());
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0x2B:
                            yield return 1;
                            register.HL--;
                            break;
                        case 0x2C:
                            Increment(ref register.L);
                            break;
                        case 0x2D:
                            Decrement(ref register.L);
                            break;
                        case 0x2E:
                            {
                                byte read = Read();
                                yield return 1;
                                register.L = read;
                                break;
                            }
                        case 0x2F:
                            register.A = (byte)(~register.A);
                            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, true);
                            break;
                        case 0x30:
                            {
                                sbyte distance = (sbyte)Read();
                                yield return 1;

                                if (register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)((register.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x31:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                register.stack_pointer = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x32:
                            {
                                ushort address = register.HLD();
                                bus.Write(address, register.A);
                                yield return 1;
                                break;
                            }
                        case 0x33:
                            yield return 1;
                            register.stack_pointer++;
                            break;
                        case 0x34:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Increment(ref read);
                                bus.Write(register.HL, read);
                                yield return 1;
                                break;
                            }
                        case 0x35:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Decrement(ref read);
                                bus.Write(register.HL, read);
                                yield return 1;
                                break;
                            }
                        case 0x36:
                            {
                                byte read = Read();
                                yield return 1;
                                bus.Write(register.HL, read);
                                yield return 1;
                                break;
                            }
                        case 0x37:
                            register.SetFlag(CpuFlag.CARRY_FLAG, true);
                            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
                            break;
                        case 0x38:
                            {
                                sbyte distance = (sbyte)Read();
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)((register.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x39:
                            yield return 1;
                            AddHL(register.stack_pointer);
                            break;
                        case 0x3A:
                            {
                                byte read = bus.Read(register.HLD());
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0x3B:
                            yield return 1;
                            register.stack_pointer--;
                            break;
                        case 0x3C:
                            Increment(ref register.A);
                            break;
                        case 0x3D:
                            Decrement(ref register.A);
                            break;
                        case 0x3E:
                            {
                                byte read = Read();
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0x3F:
                            register.SetFlag(CpuFlag.CARRY_FLAG, !register.GetFlag(CpuFlag.CARRY_FLAG));
                            register.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
                            break;
                        case 0x40: // No OP
                            break;
                        case 0x41:
                            register.B = register.C;
                            break;
                        case 0x42:
                            register.B = register.D;
                            break;
                        case 0x43:
                            register.B = register.E;
                            break;
                        case 0x44:
                            register.B = register.H;
                            break;
                        case 0x45:
                            register.B = register.L;
                            break;
                        case 0x46:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.B = read;
                                break;
                            }
                        case 0x47:
                            register.B = register.A;
                            break;
                        case 0x48:
                            register.C = register.B;
                            break;
                        case 0x49:
                            break;
                        case 0x4A:
                            register.C = register.D;
                            break;
                        case 0x4B:
                            register.C = register.E;
                            break;
                        case 0x4C:
                            register.C = register.H;
                            break;
                        case 0x4D:
                            register.C = register.L;
                            break;
                        case 0x4E:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.C = read;
                                break;
                            }
                        case 0x4F:
                            register.C = register.A;
                            break;
                        case 0x50:
                            register.D = register.B;
                            break;
                        case 0x51:
                            register.D = register.C;
                            break;
                        case 0x52:
                            break;
                        case 0x53:
                            register.D = register.E;
                            break;
                        case 0x54:
                            register.D = register.H;
                            break;
                        case 0x55:
                            register.D = register.L;
                            break;
                        case 0x56:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.D = read;
                                break;
                            }
                        case 0x57:
                            register.D = register.A;
                            break;
                        case 0x58:
                            register.E = register.B;
                            break;
                        case 0x59:
                            register.E = register.C;
                            break;
                        case 0x5A:
                            register.E = register.D;
                            break;
                        case 0x5B:
                            break;
                        case 0x5C:
                            register.E = register.H;
                            break;
                        case 0x5D:
                            register.E = register.L;
                            break;
                        case 0x5E:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.E = read;
                                break;
                            }
                        case 0x5F:
                            register.E = register.A;
                            break;
                        case 0x60:
                            register.H = register.B;
                            break;
                        case 0x61:
                            register.H = register.C;
                            break;
                        case 0x62:
                            register.H = register.D;
                            break;
                        case 0x63:
                            register.H = register.E;
                            break;
                        case 0x64:
                            break;
                        case 0x65:
                            register.H = register.L;
                            break;
                        case 0x66:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.H = read;
                                break;
                            }
                        case 0x67:
                            register.H = register.A;
                            break;
                        case 0x68:
                            register.L = register.B;
                            break;
                        case 0x69:
                            register.L = register.C;
                            break;
                        case 0x6A:
                            register.L = register.D;
                            break;
                        case 0x6B:
                            register.L = register.E;
                            break;
                        case 0x6C:
                            register.L = register.H;
                            break;
                        case 0x6D:
                            break;
                        case 0x6E:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.L = read;
                                break;
                            }
                        case 0x6F:
                            register.L = register.A;
                            break;
                        case 0x70:
                            bus.Write(register.HL, register.B);
                            yield return 1;
                            break;
                        case 0x71:
                            bus.Write(register.HL, register.C);
                            yield return 1;
                            break;
                        case 0x72:
                            bus.Write(register.HL, register.D);
                            yield return 1;
                            break;
                        case 0x73:
                            bus.Write(register.HL, register.E);
                            yield return 1;
                            break;
                        case 0x74:
                            bus.Write(register.HL, register.H);
                            yield return 1;
                            break;
                        case 0x75:
                            bus.Write(register.HL, register.L);
                            yield return 1;
                            break;
                        case 0x76: // HALT
                            halted = true;
                            break;
                        case 0x77:
                            bus.Write(register.HL, register.A);
                            yield return 1;
                            break;
                        case 0x78:
                            register.A = register.B;
                            break;
                        case 0x79:
                            register.A = register.C;
                            break;
                        case 0x7A:
                            register.A = register.D;
                            break;
                        case 0x7B:
                            register.A = register.E;
                            break;
                        case 0x7C:
                            register.A = register.H;
                            break;
                        case 0x7D:
                            register.A = register.L;
                            break;
                        case 0x7E:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0x7F:
                            break;
                        case 0x80:
                            Add(ref register.A, register.B, false);
                            break;
                        case 0x81:
                            Add(ref register.A, register.C, false);
                            break;
                        case 0x82:
                            Add(ref register.A, register.D, false);
                            break;
                        case 0x83:
                            Add(ref register.A, register.E, false);
                            break;
                        case 0x84:
                            Add(ref register.A, register.H, false);
                            break;
                        case 0x85:
                            Add(ref register.A, register.L, false);
                            break;
                        case 0x86:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Add(ref register.A, read, false);
                                break;
                            }
                        case 0x87:
                            Add(ref register.A, register.A, false);
                            break;
                        case 0x88:
                            Add(ref register.A, register.B, true);
                            break;
                        case 0x89:
                            Add(ref register.A, register.C, true);
                            break;
                        case 0x8A:
                            Add(ref register.A, register.D, true);
                            break;
                        case 0x8B:
                            Add(ref register.A, register.E, true);
                            break;
                        case 0x8C:
                            Add(ref register.A, register.H, true);
                            break;
                        case 0x8D:
                            Add(ref register.A, register.L, true);
                            break;
                        case 0x8E:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Add(ref register.A, read, true);
                                break;
                            }
                        case 0x8F:
                            Add(ref register.A, register.A, true);
                            break;
                        case 0x90:
                            Sub(ref register.A, register.B, false);
                            break;
                        case 0x91:
                            Sub(ref register.A, register.C, false);
                            break;
                        case 0x92:
                            Sub(ref register.A, register.D, false);
                            break;
                        case 0x93:
                            Sub(ref register.A, register.E, false);
                            break;
                        case 0x94:
                            Sub(ref register.A, register.H, false);
                            break;
                        case 0x95:
                            Sub(ref register.A, register.L, false);
                            break;
                        case 0x96:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Sub(ref register.A, read, false);
                                break;
                            }
                        case 0x97:
                            Sub(ref register.A, register.A, false);
                            break;
                        case 0x98:
                            Sub(ref register.A, register.B, true);
                            break;
                        case 0x99:
                            Sub(ref register.A, register.C, true);
                            break;
                        case 0x9A:
                            Sub(ref register.A, register.D, true);
                            break;
                        case 0x9B:
                            Sub(ref register.A, register.E, true);
                            break;
                        case 0x9C:
                            Sub(ref register.A, register.H, true);
                            break;
                        case 0x9D:
                            Sub(ref register.A, register.L, true);
                            break;
                        case 0x9E:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Sub(ref register.A, read, true);
                                break;
                            }
                        case 0x9F:
                            Sub(ref register.A, register.A, true);
                            break;
                        case 0xA0:
                            And(ref register.A, register.B);
                            break;
                        case 0xA1:
                            And(ref register.A, register.C);
                            break;
                        case 0xA2:
                            And(ref register.A, register.D);
                            break;
                        case 0xA3:
                            And(ref register.A, register.E);
                            break;
                        case 0xA4:
                            And(ref register.A, register.H);
                            break;
                        case 0xA5:
                            And(ref register.A, register.L);
                            break;
                        case 0xA6:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                And(ref register.A, read);
                                break;
                            }
                        case 0xA7:
                            And(ref register.A, register.A);
                            break;
                        case 0xA8:
                            Xor(ref register.A, register.B);
                            break;
                        case 0xA9:
                            Xor(ref register.A, register.C);
                            break;
                        case 0xAA:
                            Xor(ref register.A, register.D);
                            break;
                        case 0xAB:
                            Xor(ref register.A, register.E);
                            break;
                        case 0xAC:
                            Xor(ref register.A, register.H);
                            break;
                        case 0xAD:
                            Xor(ref register.A, register.L);
                            break;
                        case 0xAE:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Xor(ref register.A, read);
                                break;
                            }
                        case 0xAF:
                            Xor(ref register.A, register.A);
                            break;
                        case 0xB0:
                            Or(ref register.A, register.B);
                            break;
                        case 0xB1:
                            Or(ref register.A, register.C);
                            break;
                        case 0xB2:
                            Or(ref register.A, register.D);
                            break;
                        case 0xB3:
                            Or(ref register.A, register.E);
                            break;
                        case 0xB4:
                            Or(ref register.A, register.H);
                            break;
                        case 0xB5:
                            Or(ref register.A, register.L);
                            break;
                        case 0xB6:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Or(ref register.A, read);
                                break;
                            }
                        case 0xB7:
                            Sub(ref register.A, register.A, false);
                            break;
                        case 0xB8:
                            Sub(ref register.A, register.B, false);
                            break;
                        case 0xB9:
                            Sub(ref register.A, register.C, false);
                            break;
                        case 0xBA:
                            Sub(ref register.A, register.D, false);
                            break;
                        case 0xBB:
                            Sub(ref register.A, register.E, false);
                            break;
                        case 0xBC:
                            Sub(ref register.A, register.H, false);
                            break;
                        case 0xBD:
                            Sub(ref register.A, register.L, false);
                            break;
                        case 0xBE:
                            {
                                byte read = bus.Read(register.HL);
                                yield return 1;
                                Sub(ref register.A, read, false);
                                break;
                            }
                        case 0xBF:
                            Sub(ref register.A, register.A, false);
                            break;
                        case 0xC0:
                            {
                                yield return 1;

                                if (register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                register.program_counter = (ushort)(first | (second << 8));
                                yield return 1;
                                break;
                            }
                        case 0xC1:
                            {
                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                register.BC = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC2:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC3:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC4:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC5:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.B);
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.C);
                                yield return 1;
                                break;
                            }
                        case 0xC6:
                            {
                                byte read = Read();
                                yield return 1;
                                Add(ref register.A, read, false);
                                break;
                            }
                        case 0xC7:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;
                                register.program_counter = 0x00;
                                break;
                            }
                        case 0xC8:
                            {
                                yield return 1;
                                if (!register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC9:
                            {
                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCA:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCB:
                            {
                                var subcode = Read();
                                yield return 1;

                                switch (subcode)
                                {
                                    case 0x00:
                                        RotateLeftCarry(ref register.B);
                                        break;
                                    case 0x01:
                                        RotateLeftCarry(ref register.C);
                                        break;
                                    case 0x02:
                                        RotateLeftCarry(ref register.D);
                                        break;
                                    case 0x03:
                                        RotateLeftCarry(ref register.E);
                                        break;
                                    case 0x04:
                                        RotateLeftCarry(ref register.H);
                                        break;
                                    case 0x05:
                                        RotateLeftCarry(ref register.L);
                                        break;
                                    case 0x06:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            RotateLeftCarry(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x07:
                                        RotateLeftCarry(ref register.A);
                                        break;
                                    case 0x08:
                                        RotateRightCarry(ref register.B);
                                        break;
                                    case 0x09:
                                        RotateRightCarry(ref register.C);
                                        break;
                                    case 0x0A:
                                        RotateRightCarry(ref register.D);
                                        break;
                                    case 0x0B:
                                        RotateRightCarry(ref register.E);
                                        break;
                                    case 0x0C:
                                        RotateRightCarry(ref register.H);
                                        break;
                                    case 0x0D:
                                        RotateRightCarry(ref register.L);
                                        break;
                                    case 0x0E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            RotateRightCarry(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x0F:
                                        RotateRightCarry(ref register.A);
                                        break;
                                    case 0x10:
                                        RotateLeft(ref register.B);
                                        break;
                                    case 0x11:
                                        RotateLeft(ref register.C);
                                        break;
                                    case 0x12:
                                        RotateLeft(ref register.D);
                                        break;
                                    case 0x13:
                                        RotateLeft(ref register.E);
                                        break;
                                    case 0x14:
                                        RotateLeft(ref register.H);
                                        break;
                                    case 0x15:
                                        RotateLeft(ref register.L);
                                        break;
                                    case 0x16:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            RotateLeft(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x17:
                                        RotateLeft(ref register.A);
                                        break;
                                    case 0x18:
                                        RotateRight(ref register.B);
                                        break;
                                    case 0x19:
                                        RotateRight(ref register.C);
                                        break;
                                    case 0x1A:
                                        RotateRight(ref register.D);
                                        break;
                                    case 0x1B:
                                        RotateRight(ref register.E);
                                        break;
                                    case 0x1C:
                                        RotateRight(ref register.H);
                                        break;
                                    case 0x1D:
                                        RotateRight(ref register.L);
                                        break;
                                    case 0x1E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            RotateRight(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x1F:
                                        RotateRight(ref register.A);
                                        break;
                                    case 0x20:
                                        ShiftLeft(ref register.B);
                                        break;
                                    case 0x21:
                                        ShiftLeft(ref register.C);
                                        break;
                                    case 0x22:
                                        ShiftLeft(ref register.D);
                                        break;
                                    case 0x23:
                                        ShiftLeft(ref register.E);
                                        break;
                                    case 0x24:
                                        ShiftLeft(ref register.H);
                                        break;
                                    case 0x25:
                                        ShiftLeft(ref register.L);
                                        break;
                                    case 0x26:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            ShiftLeft(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x27:
                                        ShiftLeft(ref register.A);
                                        break;
                                    case 0x28:
                                        ShiftRightAdjust(ref register.B);
                                        break;
                                    case 0x29:
                                        ShiftRightAdjust(ref register.C);
                                        break;
                                    case 0x2A:
                                        ShiftRightAdjust(ref register.D);
                                        break;
                                    case 0x2B:
                                        ShiftRightAdjust(ref register.E);
                                        break;
                                    case 0x2C:
                                        ShiftRightAdjust(ref register.H);
                                        break;
                                    case 0x2D:
                                        ShiftRightAdjust(ref register.L);
                                        break;
                                    case 0x2E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            ShiftRightAdjust(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x2F:
                                        ShiftRightAdjust(ref register.A);
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
                                    case 0x36:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Swap(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x37:
                                        Swap(ref register.A);
                                        break;
                                    case 0x38:
                                        ShiftRight(ref register.B);
                                        break;
                                    case 0x39:
                                        ShiftRight(ref register.C);
                                        break;
                                    case 0x3A:
                                        ShiftRight(ref register.D);
                                        break;
                                    case 0x3B:
                                        ShiftRight(ref register.E);
                                        break;
                                    case 0x3C:
                                        ShiftRight(ref register.H);
                                        break;
                                    case 0x3D:
                                        ShiftRight(ref register.L);
                                        break;
                                    case 0x3E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            ShiftRight(ref read);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x3F:
                                        ShiftRight(ref register.A);
                                        break;
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
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 0);
                                            break;
                                        }
                                    case 0x47:
                                        Bit(register.A, 0);
                                        break;
                                    case 0x48:
                                        Bit(register.B, 1);
                                        break;
                                    case 0x49:
                                        Bit(register.C, 1);
                                        break;
                                    case 0x4A:
                                        Bit(register.D, 1);
                                        break;
                                    case 0x4B:
                                        Bit(register.E, 1);
                                        break;
                                    case 0x4C:
                                        Bit(register.H, 1);
                                        break;
                                    case 0x4D:
                                        Bit(register.L, 1);
                                        break;
                                    case 0x4E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 1);
                                            break;
                                        }
                                    case 0x4F:
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
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 2);
                                            break;
                                        }
                                    case 0x57:
                                        Bit(register.A, 2);
                                        break;
                                    case 0x58:
                                        Bit(register.B, 3);
                                        break;
                                    case 0x59:
                                        Bit(register.C, 3);
                                        break;
                                    case 0x5A:
                                        Bit(register.D, 3);
                                        break;
                                    case 0x5B:
                                        Bit(register.E, 3);
                                        break;
                                    case 0x5C:
                                        Bit(register.H, 3);
                                        break;
                                    case 0x5D:
                                        Bit(register.L, 3);
                                        break;
                                    case 0x5E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 3);
                                            break;
                                        }
                                    case 0x5F:
                                        Bit(register.A, 3);
                                        break;
                                    case 0x60:
                                        Bit(register.B, 4);
                                        break;
                                    case 0x61:
                                        Bit(register.C, 4);
                                        break;
                                    case 0x62:
                                        Bit(register.D, 4);
                                        break;
                                    case 0x63:
                                        Bit(register.E, 4);
                                        break;
                                    case 0x64:
                                        Bit(register.H, 4);
                                        break;
                                    case 0x65:
                                        Bit(register.L, 4);
                                        break;
                                    case 0x66:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 4);
                                            break;
                                        }
                                    case 0x67:
                                        Bit(register.A, 4);
                                        break;
                                    case 0x68:
                                        Bit(register.B, 5);
                                        break;
                                    case 0x69:
                                        Bit(register.C, 5);
                                        break;
                                    case 0x6A:
                                        Bit(register.D, 5);
                                        break;
                                    case 0x6B:
                                        Bit(register.E, 5);
                                        break;
                                    case 0x6C:
                                        Bit(register.H, 5);
                                        break;
                                    case 0x6D:
                                        Bit(register.L, 5);
                                        break;
                                    case 0x6E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 5);
                                            break;
                                        }
                                    case 0x6F:
                                        Bit(register.A, 5);
                                        break;
                                    case 0x70:
                                        Bit(register.B, 6);
                                        break;
                                    case 0x71:
                                        Bit(register.C, 6);
                                        break;
                                    case 0x72:
                                        Bit(register.D, 6);
                                        break;
                                    case 0x73:
                                        Bit(register.E, 6);
                                        break;
                                    case 0x74:
                                        Bit(register.H, 6);
                                        break;
                                    case 0x75:
                                        Bit(register.L, 6);
                                        break;
                                    case 0x76:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 6);
                                            break;
                                        }
                                    case 0x77:
                                        Bit(register.A, 6);
                                        break;
                                    case 0x78:
                                        Bit(register.B, 7);
                                        break;
                                    case 0x79:
                                        Bit(register.C, 7);
                                        break;
                                    case 0x7A:
                                        Bit(register.D, 7);
                                        break;
                                    case 0x7B:
                                        Bit(register.E, 7);
                                        break;
                                    case 0x7C:
                                        Bit(register.H, 7);
                                        break;
                                    case 0x7D:
                                        Bit(register.L, 7);
                                        break;
                                    case 0x7E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Bit(read, 7);
                                            break;
                                        }
                                    case 0x7F:
                                        Bit(register.A, 7);
                                        break;
                                    case 0x80:
                                        Res(ref register.B, 0);
                                        break;
                                    case 0x81:
                                        Res(ref register.C, 0);
                                        break;
                                    case 0x82:
                                        Res(ref register.D, 0);
                                        break;
                                    case 0x83:
                                        Res(ref register.E, 0);
                                        break;
                                    case 0x84:
                                        Res(ref register.H, 0);
                                        break;
                                    case 0x85:
                                        Res(ref register.L, 0);
                                        break;
                                    case 0x86:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 0);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x87:
                                        Res(ref register.A, 0);
                                        break;
                                    case 0x88:
                                        Res(ref register.B, 1);
                                        break;
                                    case 0x89:
                                        Res(ref register.C, 1);
                                        break;
                                    case 0x8A:
                                        Res(ref register.D, 1);
                                        break;
                                    case 0x8B:
                                        Res(ref register.E, 1);
                                        break;
                                    case 0x8C:
                                        Res(ref register.H, 1);
                                        break;
                                    case 0x8D:
                                        Res(ref register.L, 1);
                                        break;
                                    case 0x8E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 1);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x8F:
                                        Res(ref register.A, 1);
                                        break;
                                    case 0x90:
                                        Res(ref register.B, 2);
                                        break;
                                    case 0x91:
                                        Res(ref register.C, 2);
                                        break;
                                    case 0x92:
                                        Res(ref register.D, 2);
                                        break;
                                    case 0x93:
                                        Res(ref register.E, 2);
                                        break;
                                    case 0x94:
                                        Res(ref register.H, 2);
                                        break;
                                    case 0x95:
                                        Res(ref register.L, 2);
                                        break;
                                    case 0x96:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 2);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x97:
                                        Res(ref register.A, 2);
                                        break;
                                    case 0x98:
                                        Res(ref register.B, 3);
                                        break;
                                    case 0x99:
                                        Res(ref register.C, 3);
                                        break;
                                    case 0x9A:
                                        Res(ref register.D, 3);
                                        break;
                                    case 0x9B:
                                        Res(ref register.E, 3);
                                        break;
                                    case 0x9C:
                                        Res(ref register.H, 3);
                                        break;
                                    case 0x9D:
                                        Res(ref register.L, 3);
                                        break;
                                    case 0x9E:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 3);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x9F:
                                        Res(ref register.A, 3);
                                        break;
                                    case 0xA0:
                                        Res(ref register.B, 4);
                                        break;
                                    case 0xA1:
                                        Res(ref register.C, 4);
                                        break;
                                    case 0xA2:
                                        Res(ref register.D, 4);
                                        break;
                                    case 0xA3:
                                        Res(ref register.E, 4);
                                        break;
                                    case 0xA4:
                                        Res(ref register.H, 4);
                                        break;
                                    case 0xA5:
                                        Res(ref register.L, 4);
                                        break;
                                    case 0xA6:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 4);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xA7:
                                        Res(ref register.A, 4);
                                        break;
                                    case 0xA8:
                                        Res(ref register.B, 5);
                                        break;
                                    case 0xA9:
                                        Res(ref register.C, 5);
                                        break;
                                    case 0xAA:
                                        Res(ref register.D, 5);
                                        break;
                                    case 0xAB:
                                        Res(ref register.E, 5);
                                        break;
                                    case 0xAC:
                                        Res(ref register.H, 5);
                                        break;
                                    case 0xAD:
                                        Res(ref register.L, 5);
                                        break;
                                    case 0xAE:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 5);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xAF:
                                        Res(ref register.A, 5);
                                        break;
                                    case 0xB0:
                                        Res(ref register.B, 6);
                                        break;
                                    case 0xB1:
                                        Res(ref register.C, 6);
                                        break;
                                    case 0xB2:
                                        Res(ref register.D, 6);
                                        break;
                                    case 0xB3:
                                        Res(ref register.E, 6);
                                        break;
                                    case 0xB4:
                                        Res(ref register.H, 6);
                                        break;
                                    case 0xB5:
                                        Res(ref register.L, 6);
                                        break;
                                    case 0xB6:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 6);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xB7:
                                        Res(ref register.A, 6);
                                        break;
                                    case 0xB8:
                                        Res(ref register.B, 7);
                                        break;
                                    case 0xB9:
                                        Res(ref register.C, 7);
                                        break;
                                    case 0xBA:
                                        Res(ref register.D, 7);
                                        break;
                                    case 0xBB:
                                        Res(ref register.E, 7);
                                        break;
                                    case 0xBC:
                                        Res(ref register.H, 7);
                                        break;
                                    case 0xBD:
                                        Res(ref register.L, 7);
                                        break;
                                    case 0xBE:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Res(ref read, 7);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xBF:
                                        Res(ref register.A, 7);
                                        break;
                                    case 0xC0:
                                        Set(ref register.B, 0);
                                        break;
                                    case 0xC1:
                                        Set(ref register.C, 0);
                                        break;
                                    case 0xC2:
                                        Set(ref register.D, 0);
                                        break;
                                    case 0xC3:
                                        Set(ref register.E, 0);
                                        break;
                                    case 0xC4:
                                        Set(ref register.H, 0);
                                        break;
                                    case 0xC5:
                                        Set(ref register.L, 0);
                                        break;
                                    case 0xC6:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 0);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xC7:
                                        Set(ref register.A, 0);
                                        break;
                                    case 0xC8:
                                        Set(ref register.B, 1);
                                        break;
                                    case 0xC9:
                                        Set(ref register.C, 1);
                                        break;
                                    case 0xCA:
                                        Set(ref register.D, 1);
                                        break;
                                    case 0xCB:
                                        Set(ref register.E, 1);
                                        break;
                                    case 0xCC:
                                        Set(ref register.H, 1);
                                        break;
                                    case 0xCD:
                                        Set(ref register.L, 1);
                                        break;
                                    case 0xCE:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 1);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xCF:
                                        Set(ref register.A, 1);
                                        break;
                                    case 0xD0:
                                        Set(ref register.B, 2);
                                        break;
                                    case 0xD1:
                                        Set(ref register.C, 2);
                                        break;
                                    case 0xD2:
                                        Set(ref register.D, 2);
                                        break;
                                    case 0xD3:
                                        Set(ref register.E, 2);
                                        break;
                                    case 0xD4:
                                        Set(ref register.H, 2);
                                        break;
                                    case 0xD5:
                                        Set(ref register.L, 2);
                                        break;
                                    case 0xD6:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 2);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xD7:
                                        Set(ref register.A, 2);
                                        break;
                                    case 0xD8:
                                        Set(ref register.B, 3);
                                        break;
                                    case 0xD9:
                                        Set(ref register.C, 3);
                                        break;
                                    case 0xDA:
                                        Set(ref register.D, 3);
                                        break;
                                    case 0xDB:
                                        Set(ref register.E, 3);
                                        break;
                                    case 0xDC:
                                        Set(ref register.H, 3);
                                        break;
                                    case 0xDD:
                                        Set(ref register.L, 3);
                                        break;
                                    case 0xDE:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 3);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xDF:
                                        Set(ref register.A, 3);
                                        break;
                                    case 0xE0:
                                        Set(ref register.B, 4);
                                        break;
                                    case 0xE1:
                                        Set(ref register.C, 4);
                                        break;
                                    case 0xE2:
                                        Set(ref register.D, 4);
                                        break;
                                    case 0xE3:
                                        Set(ref register.E, 4);
                                        break;
                                    case 0xE4:
                                        Set(ref register.H, 4);
                                        break;
                                    case 0xE5:
                                        Set(ref register.L, 4);
                                        break;
                                    case 0xE6:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 4);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xE7:
                                        Set(ref register.A, 4);
                                        break;
                                    case 0xE8:
                                        Set(ref register.B, 5);
                                        break;
                                    case 0xE9:
                                        Set(ref register.C, 5);
                                        break;
                                    case 0xEA:
                                        Set(ref register.D, 5);
                                        break;
                                    case 0xEB:
                                        Set(ref register.E, 5);
                                        break;
                                    case 0xEC:
                                        Set(ref register.H, 5);
                                        break;
                                    case 0xED:
                                        Set(ref register.L, 5);
                                        break;
                                    case 0xEE:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 5);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xEF:
                                        Set(ref register.A, 5);
                                        break;
                                    case 0xF0:
                                        Set(ref register.B, 6);
                                        break;
                                    case 0xF1:
                                        Set(ref register.C, 6);
                                        break;
                                    case 0xF2:
                                        Set(ref register.D, 6);
                                        break;
                                    case 0xF3:
                                        Set(ref register.E, 6);
                                        break;
                                    case 0xF4:
                                        Set(ref register.H, 6);
                                        break;
                                    case 0xF5:
                                        Set(ref register.L, 6);
                                        break;
                                    case 0xF6:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 6);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xF7:
                                        Set(ref register.A, 6);
                                        break;
                                    case 0xF8:
                                        Set(ref register.B, 7);
                                        break;
                                    case 0xF9:
                                        Set(ref register.C, 7);
                                        break;
                                    case 0xFA:
                                        Set(ref register.D, 7);
                                        break;
                                    case 0xFB:
                                        Set(ref register.E, 7);
                                        break;
                                    case 0xFC:
                                        Set(ref register.H, 7);
                                        break;
                                    case 0xFD:
                                        Set(ref register.L, 7);
                                        break;
                                    case 0xFE:
                                        {
                                            byte read = bus.Read(register.HL);
                                            yield return 1;
                                            Set(ref read, 7);
                                            bus.Write(register.HL, read);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xFF:
                                        Set(ref register.A, 7);
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }
                                break;
                            }
                        case 0xCC:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCD:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCE:
                            {
                                byte read = Read();
                                yield return 1;
                                Add(ref register.A, read, true);
                                break;
                            }
                        case 0xCF:
                            {
                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x08;
                                break;
                            }
                        case 0xD0:
                            {
                                yield return 1;

                                if (register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD1:
                            {
                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                register.DE = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD2:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD3: // Unpredictable behavior
                            break;
                        case 0xD4:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD5:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.D);
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.E);
                                yield return 1;
                                break;
                            }
                        case 0xD6:
                            {
                                var read = Read();
                                yield return 1;
                                Sub(ref register.A, read, false);
                                break;
                            }
                        case 0xD7:
                            {
                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x10;
                                break;
                            }
                        case 0xD8:
                            {
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD9:
                            {
                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xDA:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xDB: // Unpredictable behavior
                            break;
                        case 0xDC:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                if (!register.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xDD: // Unpredictable behavior
                            break;
                        case 0xDE:
                            {
                                byte read = Read();
                                yield return 1;
                                Sub(ref register.A, read, true);
                                break;
                            }
                        case 0xDF:
                            {
                                yield return 1;

                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x18;
                                break;
                            }
                        case 0xE0:
                            {
                                byte read = Read();
                                yield return 1;
                                bus.Write((ushort)(0xFF00 + read), register.A);
                                yield return 1;
                                break;
                            }
                        case 0xE1:
                            {
                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                register.HL = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xE2:
                            bus.Write((ushort)(0xFF00 + register.C), register.A);
                            yield return 1;
                            break;
                        case 0xE3: // Unpredictable behavior
                        case 0xE4: // Unpredictable behavior
                            break;
                        case 0xE5:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.H);
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.L);
                                yield return 1;
                                break;
                            }
                        case 0xE6:
                            {
                                var read = Read();
                                yield return 1;
                                And(ref register.A, read);
                                break;
                            }
                        case 0xE7:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x20;
                                break;
                            }
                        case 0xE8:
                            {
                                sbyte read = (sbyte)Read();
                                yield return 1;
                                yield return 1;
                                yield return 1;

                                int result = register.stack_pointer + read;
                                register.SetFlag(CpuFlag.SUBTRACT_FLAG | CpuFlag.ZERO_FLAG, false);
                                register.SetFlag(CpuFlag.HALF_CARRY_FLAG, ((register.stack_pointer ^ read ^ (result & 0xffff)) & 0x10) == 0x10);
                                register.SetFlag(CpuFlag.CARRY_FLAG, ((register.stack_pointer ^ read ^ (result & 0xffff)) & 0x100) == 0x100);
                                register.stack_pointer = (ushort)result;

                                break;
                            }
                        case 0xE9:
                            register.program_counter = register.HL;
                            break;
                        case 0xEA:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;

                                bus.Write((ushort)(first | (second << 8)), register.A);
                                yield return 1;
                                break;
                            }
                        case 0xEB: // Unpredictable behavior
                        case 0xEC: // Unpredictable behavior
                        case 0xED: // Unpredictable behavior
                            break;
                        case 0xEE:
                            {
                                byte read = Read();
                                yield return 1;
                                Xor(ref register.A, read);
                                break;
                            }
                        case 0xEF:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x28;
                                break;
                            }
                        case 0xF0:
                            {
                                ushort address = (ushort)(0xFF00 + Read());
                                yield return 1;
                                byte b = bus.Read(address);
                                yield return 1;
                                register.A = b;
                                break;
                            }
                        case 0xF1:
                            {
                                byte first = bus.Read(register.stack_pointer++);
                                yield return 1;
                                byte second = bus.Read(register.stack_pointer++);
                                yield return 1;

                                register.AF = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xF2:
                            {
                                byte read = bus.Read((ushort)(0xFF00 + register.C));
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0xF3:
                            bus.InterruptsGloballyEnabled = false;
                            interrupt_cooldown = 0;
                            break;
                        case 0xF4: // Unpredictable behavior
                            break;
                        case 0xF5:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.A);
                                yield return 1;
                                bus.Write(--register.stack_pointer, register.F);
                                yield return 1;
                                break;
                            }
                        case 0xF6:
                            {
                                var read = Read();
                                yield return 1;
                                Or(ref register.A, read);
                                break;
                            }
                        case 0xF7:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x30;
                                break;
                            }
                        case 0xF8:
                            {
                                var distance = (sbyte)Read();
                                yield return 1;
                                yield return 1;

                                int result = register.stack_pointer + distance;
                                register.SetFlag(CpuFlag.SUBTRACT_FLAG | CpuFlag.ZERO_FLAG, false);
                                register.SetFlag(CpuFlag.HALF_CARRY_FLAG, ((register.stack_pointer ^ distance ^ (result & 0xffff)) & 0x10) == 0x10);
                                register.SetFlag(CpuFlag.CARRY_FLAG, ((register.stack_pointer ^ distance ^ (result & 0xffff)) & 0x100) == 0x100);
                                register.HL = (ushort)result;

                                break;
                            }
                        case 0xF9:
                            yield return 1;
                            register.stack_pointer = register.HL;
                            break;
                        case 0xFA:
                            {
                                byte first = Read();
                                yield return 1;
                                byte second = Read();
                                yield return 1;
                                byte read = bus.Read((ushort)(first | (second << 8)));
                                yield return 1;
                                register.A = read;
                                break;
                            }
                        case 0xFB:
                            if (interrupt_cooldown == 0)
                                interrupt_cooldown = 2;
                            break;
                        case 0xFC: // Unpredictable behavior
                        case 0xFD: // Unpredictable behavior
                            break;
                        case 0xFE:
                            {
                                var read = Read();
                                yield return 1;
                                Sub(ref register.A, read, false);
                                break;
                            }
                        case 0xFF:
                            {
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter >> 8));
                                yield return 1;
                                bus.Write(--register.stack_pointer, (byte)(register.program_counter & 0xFF));
                                yield return 1;

                                register.program_counter = 0x38;
                                break;
                            }
                        default:
                            throw new NotSupportedException();
                    }
                }

                processing = false;
                yield return 1;
            }
        }
    }

}