using gbemu.interrupts;
using System;
using System.Collections;
using System.Collections.Generic;

namespace gbemu.cpu
{
    internal class CPU : IEnumerable<int>
    {
        private readonly ALU alu;
        private readonly Device device;

        private bool halted;
        private bool halted_bug_state;
        internal bool stopped;
        private int interrupt_cooldown;
        internal bool processing_intruction;

        internal Registers Registers { get; }

        internal CPU(Device device)
        {
            Registers = new Registers();
            alu = new ALU(this);
            this.device = device;
            Reset();
        }

        public IEnumerator<int> GetEnumerator()
        {
            while (true)
            {
                if (interrupt_cooldown > 0)
                {
                    interrupt_cooldown--;

                    if (interrupt_cooldown == 0)
                    {
                        device.interrupt_registers.AreInterruptsEnabledGlobally = true;
                    }
                }

                if (!device.dma_controller.BlockInterrupts())
                {
                    for (var bit = 0; bit < 6; bit++)
                    {
                        var mask = 1 << bit;
                        if ((device.interrupt_registers.InterruptEnable & device.interrupt_registers.InterruptFlags & mask) == mask)
                        {
                            if (halted)
                            {
                                halted = false;
                            }

                            if (device.cpu.stopped)
                            {
                                stopped = false;
                                yield return 1;
                            }

                            if (device.interrupt_registers.AreInterruptsEnabledGlobally)
                            {
                                var interrupt = (Interrupt)bit;

                                device.interrupt_registers.AreInterruptsEnabledGlobally = false;
                                device.interrupt_registers.ResetInterrupt(interrupt);

                                yield return 1;
                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(device.cpu.Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(device.cpu.Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = interrupt.StartingAddress();
                                yield return 1;
                            }
                        }
                    }
                }

                if (halted || stopped || device.dma_controller.HaltCpu())
                {
                    yield return 0;
                }
                else
                {
                    var opcode = Read();
                    processing_intruction = true;

                    if (halted_bug_state)
                    {
                        Registers.program_counter--;
                        halted_bug_state = false;
                    }

                    switch (opcode)
                    {
                        case 0x00:
                            break;
                        case 0x01:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                Registers.BC = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x02:
                            device.bus.WriteByte(Registers.BC, Registers.A);
                            yield return 1;
                            break;
                        case 0x03:
                            Registers.BC++;
                            yield return 1;
                            break;
                        case 0x04:
                            alu.Increment(ref Registers.B);
                            break;
                        case 0x05:
                            alu.Decrement(ref Registers.B);
                            break;
                        case 0x06:
                            var d8 = Read();
                            yield return 1;
                            Registers.B = d8;
                            break;
                        case 0x07:
                            alu.RotateLeftCarryA();
                            break;
                        case 0x08:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                var address = (ushort)(first | (second << 8));
                                device.bus.WriteByte(address, (byte)(Registers.stack_pointer & 0xFF));
                                yield return 1;
                                address++;
                                device.bus.WriteByte(address, (byte)(Registers.stack_pointer >> 8));
                                yield return 1;
                                break;
                            }
                        case 0x09:
                            alu.AddHL(Registers.BC);
                            yield return 1;
                            break;
                        case 0x0A:
                            {
                                var b = device.bus.ReadByte(Registers.BC);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x0B:
                            Registers.BC = (ushort)(Registers.BC - 1);
                            yield return 1;
                            break;
                        case 0x0C:
                            alu.Increment(ref Registers.C);
                            break;
                        case 0x0D:
                            alu.Decrement(ref Registers.C);
                            break;
                        case 0x0E:
                            {
                                var b = Read();
                                yield return 1;
                                Registers.C = b;
                                break;
                            }
                        case 0x0F:
                            alu.RotateRightCarryA();
                            break;
                        case 0x10: // STOP
                            {
                                Read();
                                yield return 1;

                                stopped = true;
                                break;
                            }
                        case 0x11:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                Registers.DE = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x12:
                            device.bus.WriteByte(Registers.DE, Registers.A);
                            yield return 1;
                            break;
                        case 0x13:
                            Registers.DE++;
                            yield return 1;
                            break;
                        case 0x14:
                            alu.Increment(ref Registers.D);
                            break;
                        case 0x15:
                            alu.Decrement(ref Registers.D);
                            break;
                        case 0x16:
                            {
                                var b = Read();
                                yield return 1;
                                Registers.D = b;
                                break;
                            }
                        case 0x17:
                            alu.RotateLeftA();
                            break;
                        case 0x18:
                            {
                                var r8 = (sbyte)Read();
                                yield return 1;
                                Registers.program_counter = (ushort)((Registers.program_counter + r8) & 0xFFFF);
                                yield return 1;
                                break;
                            }
                        case 0x19:
                            alu.AddHL(Registers.DE);
                            yield return 1;
                            break;
                        case 0x1A:
                            {
                                var b = device.bus.ReadByte(Registers.DE);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x1B:
                            Registers.DE--;
                            yield return 1;
                            break;
                        case 0x1C:
                            alu.Increment(ref Registers.E);
                            break;
                        case 0x1D:
                            alu.Decrement(ref Registers.E);
                            break;
                        case 0x1E:
                            {
                                var b = Read();
                                yield return 1;
                                Registers.E = b;
                                break;
                            }
                        case 0x1F:
                            alu.RotateRightA();
                            break;
                        case 0x20:
                            {
                                var distance = (sbyte)Read();
                                yield return 1;
                                if (Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)((Registers.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x21:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                Registers.HL = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x22:
                            {
                                var address = Registers.HLI();
                                device.bus.WriteByte(address, Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0x23:
                            yield return 1;
                            Registers.HL++;
                            break;
                        case 0x24:
                            alu.Increment(ref Registers.H);
                            break;
                        case 0x25:
                            alu.Decrement(ref Registers.H);
                            break;
                        case 0x26:
                            {
                                var b = Read();
                                yield return 1;
                                Registers.H = b;
                                break;
                            }
                        case 0x27:
                            alu.DecimalAdjustRegister(ref Registers.A);
                            break;
                        case 0x28:
                            {
                                var distance = (sbyte)Read();
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)((Registers.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x29:
                            yield return 1;
                            alu.AddHL(Registers.HL);
                            break;
                        case 0x2A:
                            {
                                var b = device.bus.ReadByte(Registers.HLI());
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x2B:
                            yield return 1;
                            Registers.HL--;
                            break;
                        case 0x2C:
                            alu.Increment(ref Registers.L);
                            break;
                        case 0x2D:
                            alu.Decrement(ref Registers.L);
                            break;
                        case 0x2E:
                            {
                                var b = Read();
                                yield return 1;
                                Registers.L = b;
                                break;
                            }
                        case 0x2F:
                            alu.CPL();
                            break;
                        case 0x30:
                            {
                                var distance = (sbyte)Read();
                                yield return 1;
                                if (Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)((Registers.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x31:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                Registers.stack_pointer = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0x32:
                            {
                                var address = Registers.HLD();
                                device.bus.WriteByte(address, Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0x33:
                            yield return 1;
                            Registers.stack_pointer++;
                            break;
                        case 0x34:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Increment(ref b);
                                device.bus.WriteByte(Registers.HL, b);
                                yield return 1;
                                break;
                            }
                        case 0x35:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Decrement(ref b);
                                device.bus.WriteByte(Registers.HL, b);
                                yield return 1;
                                break;
                            }
                        case 0x36:
                            {
                                var b = Read();
                                yield return 1;
                                device.bus.WriteByte(Registers.HL, b);
                                yield return 1;
                                break;
                            }
                        case 0x37:
                            alu.SCF();
                            break;
                        case 0x38:
                            {
                                var distance = (sbyte)Read();
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)((Registers.program_counter + distance) & 0xFFFF);
                                break;
                            }
                        case 0x39:
                            yield return 1;
                            alu.AddHL(Registers.stack_pointer);
                            break;
                        case 0x3A:
                            {
                                var b = device.bus.ReadByte(Registers.HLD());
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x3B:
                            yield return 1;
                            --Registers.stack_pointer;
                            break;
                        case 0x3C:
                            alu.Increment(ref Registers.A);
                            break;
                        case 0x3D:
                            alu.Decrement(ref Registers.A);
                            break;
                        case 0x3E:
                            {
                                var b = Read();
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x3F:
                            alu.CCF();
                            break;
                        case 0x40:
                            break;
                        case 0x41:
                            Registers.B = Registers.C;
                            break;
                        case 0x42:
                            Registers.B = Registers.D;
                            break;
                        case 0x43:
                            Registers.B = Registers.E;
                            break;
                        case 0x44:
                            Registers.B = Registers.H;
                            break;
                        case 0x45:
                            Registers.B = Registers.L;
                            break;
                        case 0x46:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.B = b;
                                break;
                            }
                        case 0x47:
                            Registers.B = Registers.A;
                            break;
                        case 0x48:
                            Registers.C = Registers.B;
                            break;
                        case 0x49:
                            break;
                        case 0x4A:
                            Registers.C = Registers.D;
                            break;
                        case 0x4B:
                            Registers.C = Registers.E;
                            break;
                        case 0x4C:
                            Registers.C = Registers.H;
                            break;
                        case 0x4D:
                            Registers.C = Registers.L;
                            break;
                        case 0x4E:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.C = b;
                                break;
                            }
                        case 0x4F:
                            Registers.C = Registers.A;
                            break;
                        case 0x50:
                            Registers.D = Registers.B;
                            break;
                        case 0x51:
                            Registers.D = Registers.C;
                            break;
                        case 0x52:
                            break;
                        case 0x53:
                            Registers.D = Registers.E;
                            break;
                        case 0x54:
                            Registers.D = Registers.H;
                            break;
                        case 0x55:
                            Registers.D = Registers.L;
                            break;
                        case 0x56:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.D = b;
                                break;
                            }
                        case 0x57:
                            Registers.D = Registers.A;
                            break;
                        case 0x58:
                            Registers.E = Registers.B;
                            break;
                        case 0x59:
                            Registers.E = Registers.C;
                            break;
                        case 0x5A:
                            Registers.E = Registers.D;
                            break;
                        case 0x5B:
                            break;
                        case 0x5C:
                            Registers.E = Registers.H;
                            break;
                        case 0x5D:
                            Registers.E = Registers.L;
                            break;
                        case 0x5E:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.E = b;
                                break;
                            }
                        case 0x5F:
                            Registers.E = Registers.A;
                            break;
                        case 0x60:
                            Registers.H = Registers.B;
                            break;
                        case 0x61:
                            Registers.H = Registers.C;
                            break;
                        case 0x62:
                            Registers.H = Registers.D;
                            break;
                        case 0x63:
                            Registers.H = Registers.E;
                            break;
                        case 0x64:
                            break;
                        case 0x65:
                            Registers.H = Registers.L;
                            break;
                        case 0x66:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.H = b;
                                break;
                            }
                        case 0x67:
                            Registers.H = Registers.A;
                            break;
                        case 0x68:
                            Registers.L = Registers.B;
                            break;
                        case 0x69:
                            Registers.L = Registers.C;
                            break;
                        case 0x6A:
                            Registers.L = Registers.D;
                            break;
                        case 0x6B:
                            Registers.L = Registers.E;
                            break;
                        case 0x6C:
                            Registers.L = Registers.H;
                            break;
                        case 0x6D:
                            break;
                        case 0x6E:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.L = b;
                                break;
                            }
                        case 0x6F:
                            Registers.L = Registers.A;
                            break;
                        case 0x70:
                            device.bus.WriteByte(Registers.HL, Registers.B);
                            yield return 1;
                            break;
                        case 0x71:
                            device.bus.WriteByte(Registers.HL, Registers.C);
                            yield return 1;
                            break;
                        case 0x72:
                            device.bus.WriteByte(Registers.HL, Registers.D);
                            yield return 1;
                            break;
                        case 0x73:
                            device.bus.WriteByte(Registers.HL, Registers.E);
                            yield return 1;
                            break;
                        case 0x74:
                            device.bus.WriteByte(Registers.HL, Registers.H);
                            yield return 1;
                            break;
                        case 0x75:
                            device.bus.WriteByte(Registers.HL, Registers.L);
                            yield return 1;
                            break;
                        case 0x76: // HALT
                            halted = true;
                            halted_bug_state = false;

                            if (!device.interrupt_registers.AreInterruptsEnabledGlobally &&
                                (device.interrupt_registers.InterruptEnable & device.interrupt_registers.InterruptFlags & 0x1F) != 0)
                            {
                                halted_bug_state = true;
                            }
                            break;
                        case 0x77:
                            device.bus.WriteByte(Registers.HL, Registers.A);
                            yield return 1;
                            break;
                        case 0x78:
                            Registers.A = Registers.B;
                            break;
                        case 0x79:
                            Registers.A = Registers.C;
                            break;
                        case 0x7A:
                            Registers.A = Registers.D;
                            break;
                        case 0x7B:
                            Registers.A = Registers.E;
                            break;
                        case 0x7C:
                            Registers.A = Registers.H;
                            break;
                        case 0x7D:
                            Registers.A = Registers.L;
                            break;
                        case 0x7E:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0x7F:
                            break;
                        case 0x80:
                            alu.Add(ref Registers.A, Registers.B, false);
                            break;
                        case 0x81:
                            alu.Add(ref Registers.A, Registers.C, false);
                            break;
                        case 0x82:
                            alu.Add(ref Registers.A, Registers.D, false);
                            break;
                        case 0x83:
                            alu.Add(ref Registers.A, Registers.E, false);
                            break;
                        case 0x84:
                            alu.Add(ref Registers.A, Registers.H, false);
                            break;
                        case 0x85:
                            alu.Add(ref Registers.A, Registers.L, false);
                            break;
                        case 0x86:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Add(ref Registers.A, b, false);
                                break;
                            }
                        case 0x87:
                            alu.Add(ref Registers.A, Registers.A, false);
                            break;
                        case 0x88:
                            alu.Add(ref Registers.A, Registers.B, true);
                            break;
                        case 0x89:
                            alu.Add(ref Registers.A, Registers.C, true);
                            break;
                        case 0x8A:
                            alu.Add(ref Registers.A, Registers.D, true);
                            break;
                        case 0x8B:
                            alu.Add(ref Registers.A, Registers.E, true);
                            break;
                        case 0x8C:
                            alu.Add(ref Registers.A, Registers.H, true);
                            break;
                        case 0x8D:
                            alu.Add(ref Registers.A, Registers.L, true);
                            break;
                        case 0x8E:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Add(ref Registers.A, b, true);
                                break;
                            }
                        case 0x8F:
                            alu.Add(ref Registers.A, Registers.A, true);
                            break;
                        case 0x90:
                            alu.Sub(ref Registers.A, Registers.B, false);
                            break;
                        case 0x91:
                            alu.Sub(ref Registers.A, Registers.C, false);
                            break;
                        case 0x92:
                            alu.Sub(ref Registers.A, Registers.D, false);
                            break;
                        case 0x93:
                            alu.Sub(ref Registers.A, Registers.E, false);
                            break;
                        case 0x94:
                            alu.Sub(ref Registers.A, Registers.H, false);
                            break;
                        case 0x95:
                            alu.Sub(ref Registers.A, Registers.L, false);
                            break;
                        case 0x96:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Sub(ref Registers.A, b, false);
                                break;
                            }
                        case 0x97:
                            alu.Sub(ref Registers.A, Registers.A, false);
                            break;
                        case 0x98:
                            alu.Sub(ref Registers.A, Registers.B, true);
                            break;
                        case 0x99:
                            alu.Sub(ref Registers.A, Registers.C, true);
                            break;
                        case 0x9A:
                            alu.Sub(ref Registers.A, Registers.D, true);
                            break;
                        case 0x9B:
                            alu.Sub(ref Registers.A, Registers.E, true);
                            break;
                        case 0x9C:
                            alu.Sub(ref Registers.A, Registers.H, true);
                            break;
                        case 0x9D:
                            alu.Sub(ref Registers.A, Registers.L, true);
                            break;
                        case 0x9E:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Sub(ref Registers.A, b, true);
                                break;
                            }
                        case 0x9F:
                            alu.Sub(ref Registers.A, Registers.A, true);
                            break;
                        case 0xA0:
                            alu.And(ref Registers.A, Registers.B);
                            break;
                        case 0xA1:
                            alu.And(ref Registers.A, Registers.C);
                            break;
                        case 0xA2:
                            alu.And(ref Registers.A, Registers.D);
                            break;
                        case 0xA3:
                            alu.And(ref Registers.A, Registers.E);
                            break;
                        case 0xA4:
                            alu.And(ref Registers.A, Registers.H);
                            break;
                        case 0xA5:
                            alu.And(ref Registers.A, Registers.L);
                            break;
                        case 0xA6:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.And(ref Registers.A, b);
                                break;
                            }
                        case 0xA7:
                            alu.And(ref Registers.A, Registers.A);
                            break;
                        case 0xA8:
                            alu.Xor(ref Registers.A, Registers.B);
                            break;
                        case 0xA9:
                            alu.Xor(ref Registers.A, Registers.C);
                            break;
                        case 0xAA:
                            alu.Xor(ref Registers.A, Registers.D);
                            break;
                        case 0xAB:
                            alu.Xor(ref Registers.A, Registers.E);
                            break;
                        case 0xAC:
                            alu.Xor(ref Registers.A, Registers.H);
                            break;
                        case 0xAD:
                            alu.Xor(ref Registers.A, Registers.L);
                            break;
                        case 0xAE:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Xor(ref Registers.A, b);
                                break;
                            }
                        case 0xAF:
                            alu.Xor(ref Registers.A, Registers.A);
                            break;
                        case 0xB0:
                            alu.Or(ref Registers.A, Registers.B);
                            break;
                        case 0xB1:
                            alu.Or(ref Registers.A, Registers.C);
                            break;
                        case 0xB2:
                            alu.Or(ref Registers.A, Registers.D);
                            break;
                        case 0xB3:
                            alu.Or(ref Registers.A, Registers.E);
                            break;
                        case 0xB4:
                            alu.Or(ref Registers.A, Registers.H);
                            break;
                        case 0xB5:
                            alu.Or(ref Registers.A, Registers.L);
                            break;
                        case 0xB6:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Or(ref Registers.A, b);
                                break;
                            }
                        case 0xB7:
                            alu.Or(ref Registers.A, Registers.A);
                            break;
                        case 0xB8:
                            alu.Cp(Registers.A, Registers.B);
                            break;
                        case 0xB9:
                            alu.Cp(Registers.A, Registers.C);
                            break;
                        case 0xBA:
                            alu.Cp(Registers.A, Registers.D);
                            break;
                        case 0xBB:
                            alu.Cp(Registers.A, Registers.E);
                            break;
                        case 0xBC:
                            alu.Cp(Registers.A, Registers.H);
                            break;
                        case 0xBD:
                            alu.Cp(Registers.A, Registers.L);
                            break;
                        case 0xBE:
                            {
                                var b = device.bus.ReadByte(Registers.HL);
                                yield return 1;
                                alu.Cp(Registers.A, b);
                                break;
                            }
                        case 0xBF:
                            alu.Cp(Registers.A, Registers.A);
                            break;
                        case 0xC0:
                            {
                                yield return 1;
                                if (Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                Registers.program_counter = (ushort)(first | (second << 8));
                                yield return 1;
                                break;
                            }
                        case 0xC1:
                            {
                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                Registers.BC = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC2:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC3:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC4:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC5:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.B);
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.C);
                                yield return 1;
                                break;
                            }
                        case 0xC6:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Add(ref Registers.A, b, false);
                                break;
                            }
                        case 0xC7:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;
                                Registers.program_counter = 0x00;
                                break;
                            }
                        case 0xC8:
                            {
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xC9:
                            {
                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCA:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCB:
                            {
                                var subcode = Read();
                                yield return 1;

                                switch (subcode)
                                {
                                    case 0x00:
                                        alu.RotateLeftCarry(ref Registers.B);
                                        break;
                                    case 0x01:
                                        alu.RotateLeftCarry(ref Registers.C);
                                        break;
                                    case 0x02:
                                        alu.RotateLeftCarry(ref Registers.D);
                                        break;
                                    case 0x03:
                                        alu.RotateLeftCarry(ref Registers.E);
                                        break;
                                    case 0x04:
                                        alu.RotateLeftCarry(ref Registers.H);
                                        break;
                                    case 0x05:
                                        alu.RotateLeftCarry(ref Registers.L);
                                        break;
                                    case 0x06:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.RotateLeftCarry(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x07:
                                        alu.RotateLeftCarry(ref Registers.A);
                                        break;
                                    case 0x08:
                                        alu.RotateRightCarry(ref Registers.B);
                                        break;
                                    case 0x09:
                                        alu.RotateRightCarry(ref Registers.C);
                                        break;
                                    case 0x0A:
                                        alu.RotateRightCarry(ref Registers.D);
                                        break;
                                    case 0x0B:
                                        alu.RotateRightCarry(ref Registers.E);
                                        break;
                                    case 0x0C:
                                        alu.RotateRightCarry(ref Registers.H);
                                        break;
                                    case 0x0D:
                                        alu.RotateRightCarry(ref Registers.L);
                                        break;
                                    case 0x0E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.RotateRightCarry(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x0F:
                                        alu.RotateRightCarry(ref Registers.A);
                                        break;
                                    case 0x10:
                                        alu.RotateLeft(ref Registers.B);
                                        break;
                                    case 0x11:
                                        alu.RotateLeft(ref Registers.C);
                                        break;
                                    case 0x12:
                                        alu.RotateLeft(ref Registers.D);
                                        break;
                                    case 0x13:
                                        alu.RotateLeft(ref Registers.E);
                                        break;
                                    case 0x14:
                                        alu.RotateLeft(ref Registers.H);
                                        break;
                                    case 0x15:
                                        alu.RotateLeft(ref Registers.L);
                                        break;
                                    case 0x16:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.RotateLeft(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x17:
                                        alu.RotateLeft(ref Registers.A);
                                        break;
                                    case 0x18:
                                        alu.RotateRight(ref Registers.B);
                                        break;
                                    case 0x19:
                                        alu.RotateRight(ref Registers.C);
                                        break;
                                    case 0x1A:
                                        alu.RotateRight(ref Registers.D);
                                        break;
                                    case 0x1B:
                                        alu.RotateRight(ref Registers.E);
                                        break;
                                    case 0x1C:
                                        alu.RotateRight(ref Registers.H);
                                        break;
                                    case 0x1D:
                                        alu.RotateRight(ref Registers.L);
                                        break;
                                    case 0x1E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.RotateRight(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x1F:
                                        alu.RotateRight(ref Registers.A);
                                        break;
                                    case 0x20:
                                        alu.ShiftLeft(ref Registers.B);
                                        break;
                                    case 0x21:
                                        alu.ShiftLeft(ref Registers.C);
                                        break;
                                    case 0x22:
                                        alu.ShiftLeft(ref Registers.D);
                                        break;
                                    case 0x23:
                                        alu.ShiftLeft(ref Registers.E);
                                        break;
                                    case 0x24:
                                        alu.ShiftLeft(ref Registers.H);
                                        break;
                                    case 0x25:
                                        alu.ShiftLeft(ref Registers.L);
                                        break;
                                    case 0x26:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.ShiftLeft(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x27:
                                        alu.ShiftLeft(ref Registers.A);
                                        break;
                                    case 0x28:
                                        alu.ShiftRightAdjust(ref Registers.B);
                                        break;
                                    case 0x29:
                                        alu.ShiftRightAdjust(ref Registers.C);
                                        break;
                                    case 0x2A:
                                        alu.ShiftRightAdjust(ref Registers.D);
                                        break;
                                    case 0x2B:
                                        alu.ShiftRightAdjust(ref Registers.E);
                                        break;
                                    case 0x2C:
                                        alu.ShiftRightAdjust(ref Registers.H);
                                        break;
                                    case 0x2D:
                                        alu.ShiftRightAdjust(ref Registers.L);
                                        break;
                                    case 0x2E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.ShiftRightAdjust(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x2F:
                                        alu.ShiftRightAdjust(ref Registers.A);
                                        break;
                                    case 0x30:
                                        alu.Swap(ref Registers.B);
                                        break;
                                    case 0x31:
                                        alu.Swap(ref Registers.C);
                                        break;
                                    case 0x32:
                                        alu.Swap(ref Registers.D);
                                        break;
                                    case 0x33:
                                        alu.Swap(ref Registers.E);
                                        break;
                                    case 0x34:
                                        alu.Swap(ref Registers.H);
                                        break;
                                    case 0x35:
                                        alu.Swap(ref Registers.L);
                                        break;
                                    case 0x36:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Swap(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x37:
                                        alu.Swap(ref Registers.A);
                                        break;
                                    case 0x38:
                                        alu.ShiftRight(ref Registers.B);
                                        break;
                                    case 0x39:
                                        alu.ShiftRight(ref Registers.C);
                                        break;
                                    case 0x3A:
                                        alu.ShiftRight(ref Registers.D);
                                        break;
                                    case 0x3B:
                                        alu.ShiftRight(ref Registers.E);
                                        break;
                                    case 0x3C:
                                        alu.ShiftRight(ref Registers.H);
                                        break;
                                    case 0x3D:
                                        alu.ShiftRight(ref Registers.L);
                                        break;
                                    case 0x3E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.ShiftRight(ref b);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x3F:
                                        alu.ShiftRight(ref Registers.A);
                                        break;
                                    case 0x40:
                                        alu.Bit(Registers.B, 0);
                                        break;
                                    case 0x41:
                                        alu.Bit(Registers.C, 0);
                                        break;
                                    case 0x42:
                                        alu.Bit(Registers.D, 0);
                                        break;
                                    case 0x43:
                                        alu.Bit(Registers.E, 0);
                                        break;
                                    case 0x44:
                                        alu.Bit(Registers.H, 0);
                                        break;
                                    case 0x45:
                                        alu.Bit(Registers.L, 0);
                                        break;
                                    case 0x46:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 0);
                                            break;
                                        }
                                    case 0x47:
                                        alu.Bit(Registers.A, 0);
                                        break;
                                    case 0x48:
                                        alu.Bit(Registers.B, 1);
                                        break;
                                    case 0x49:
                                        alu.Bit(Registers.C, 1);
                                        break;
                                    case 0x4A:
                                        alu.Bit(Registers.D, 1);
                                        break;
                                    case 0x4B:
                                        alu.Bit(Registers.E, 1);
                                        break;
                                    case 0x4C:
                                        alu.Bit(Registers.H, 1);
                                        break;
                                    case 0x4D:
                                        alu.Bit(Registers.L, 1);
                                        break;
                                    case 0x4E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 1);
                                            break;
                                        }
                                    case 0x4F:
                                        alu.Bit(Registers.A, 1);
                                        break;
                                    case 0x50:
                                        alu.Bit(Registers.B, 2);
                                        break;
                                    case 0x51:
                                        alu.Bit(Registers.C, 2);
                                        break;
                                    case 0x52:
                                        alu.Bit(Registers.D, 2);
                                        break;
                                    case 0x53:
                                        alu.Bit(Registers.E, 2);
                                        break;
                                    case 0x54:
                                        alu.Bit(Registers.H, 2);
                                        break;
                                    case 0x55:
                                        alu.Bit(Registers.L, 2);
                                        break;
                                    case 0x56:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 2);
                                            break;
                                        }
                                    case 0x57:
                                        alu.Bit(Registers.A, 2);
                                        break;
                                    case 0x58:
                                        alu.Bit(Registers.B, 3);
                                        break;
                                    case 0x59:
                                        alu.Bit(Registers.C, 3);
                                        break;
                                    case 0x5A:
                                        alu.Bit(Registers.D, 3);
                                        break;
                                    case 0x5B:
                                        alu.Bit(Registers.E, 3);
                                        break;
                                    case 0x5C:
                                        alu.Bit(Registers.H, 3);
                                        break;
                                    case 0x5D:
                                        alu.Bit(Registers.L, 3);
                                        break;
                                    case 0x5E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 3);
                                            break;
                                        }
                                    case 0x5F:
                                        alu.Bit(Registers.A, 3);
                                        break;
                                    case 0x60:
                                        alu.Bit(Registers.B, 4);
                                        break;
                                    case 0x61:
                                        alu.Bit(Registers.C, 4);
                                        break;
                                    case 0x62:
                                        alu.Bit(Registers.D, 4);
                                        break;
                                    case 0x63:
                                        alu.Bit(Registers.E, 4);
                                        break;
                                    case 0x64:
                                        alu.Bit(Registers.H, 4);
                                        break;
                                    case 0x65:
                                        alu.Bit(Registers.L, 4);
                                        break;
                                    case 0x66:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 4);
                                            break;
                                        }
                                    case 0x67:
                                        alu.Bit(Registers.A, 4);
                                        break;
                                    case 0x68:
                                        alu.Bit(Registers.B, 5);
                                        break;
                                    case 0x69:
                                        alu.Bit(Registers.C, 5);
                                        break;
                                    case 0x6A:
                                        alu.Bit(Registers.D, 5);
                                        break;
                                    case 0x6B:
                                        alu.Bit(Registers.E, 5);
                                        break;
                                    case 0x6C:
                                        alu.Bit(Registers.H, 5);
                                        break;
                                    case 0x6D:
                                        alu.Bit(Registers.L, 5);
                                        break;
                                    case 0x6E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 5);
                                            break;
                                        }
                                    case 0x6F:
                                        alu.Bit(Registers.A, 5);
                                        break;
                                    case 0x70:
                                        alu.Bit(Registers.B, 6);
                                        break;
                                    case 0x71:
                                        alu.Bit(Registers.C, 6);
                                        break;
                                    case 0x72:
                                        alu.Bit(Registers.D, 6);
                                        break;
                                    case 0x73:
                                        alu.Bit(Registers.E, 6);
                                        break;
                                    case 0x74:
                                        alu.Bit(Registers.H, 6);
                                        break;
                                    case 0x75:
                                        alu.Bit(Registers.L, 6);
                                        break;
                                    case 0x76:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 6);
                                            break;
                                        }
                                    case 0x77:
                                        alu.Bit(Registers.A, 6);
                                        break;
                                    case 0x78:
                                        alu.Bit(Registers.B, 7);
                                        break;
                                    case 0x79:
                                        alu.Bit(Registers.C, 7);
                                        break;
                                    case 0x7A:
                                        alu.Bit(Registers.D, 7);
                                        break;
                                    case 0x7B:
                                        alu.Bit(Registers.E, 7);
                                        break;
                                    case 0x7C:
                                        alu.Bit(Registers.H, 7);
                                        break;
                                    case 0x7D:
                                        alu.Bit(Registers.L, 7);
                                        break;
                                    case 0x7E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Bit(b, 7);
                                            break;
                                        }
                                    case 0x7F:
                                        alu.Bit(Registers.A, 7);
                                        break;
                                    case 0x80:
                                        alu.Res(ref Registers.B, 0);
                                        break;
                                    case 0x81:
                                        alu.Res(ref Registers.C, 0);
                                        break;
                                    case 0x82:
                                        alu.Res(ref Registers.D, 0);
                                        break;
                                    case 0x83:
                                        alu.Res(ref Registers.E, 0);
                                        break;
                                    case 0x84:
                                        alu.Res(ref Registers.H, 0);
                                        break;
                                    case 0x85:
                                        alu.Res(ref Registers.L, 0);
                                        break;
                                    case 0x86:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 0);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x87:
                                        alu.Res(ref Registers.A, 0);
                                        break;
                                    case 0x88:
                                        alu.Res(ref Registers.B, 1);
                                        break;
                                    case 0x89:
                                        alu.Res(ref Registers.C, 1);
                                        break;
                                    case 0x8A:
                                        alu.Res(ref Registers.D, 1);
                                        break;
                                    case 0x8B:
                                        alu.Res(ref Registers.E, 1);
                                        break;
                                    case 0x8C:
                                        alu.Res(ref Registers.H, 1);
                                        break;
                                    case 0x8D:
                                        alu.Res(ref Registers.L, 1);
                                        break;
                                    case 0x8E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 1);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x8F:
                                        alu.Res(ref Registers.A, 1);
                                        break;
                                    case 0x90:
                                        alu.Res(ref Registers.B, 2);
                                        break;
                                    case 0x91:
                                        alu.Res(ref Registers.C, 2);
                                        break;
                                    case 0x92:
                                        alu.Res(ref Registers.D, 2);
                                        break;
                                    case 0x93:
                                        alu.Res(ref Registers.E, 2);
                                        break;
                                    case 0x94:
                                        alu.Res(ref Registers.H, 2);
                                        break;
                                    case 0x95:
                                        alu.Res(ref Registers.L, 2);
                                        break;
                                    case 0x96:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 2);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x97:
                                        alu.Res(ref Registers.A, 2);
                                        break;
                                    case 0x98:
                                        alu.Res(ref Registers.B, 3);
                                        break;
                                    case 0x99:
                                        alu.Res(ref Registers.C, 3);
                                        break;
                                    case 0x9A:
                                        alu.Res(ref Registers.D, 3);
                                        break;
                                    case 0x9B:
                                        alu.Res(ref Registers.E, 3);
                                        break;
                                    case 0x9C:
                                        alu.Res(ref Registers.H, 3);
                                        break;
                                    case 0x9D:
                                        alu.Res(ref Registers.L, 3);
                                        break;
                                    case 0x9E:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 3);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0x9F:
                                        alu.Res(ref Registers.A, 3);
                                        break;
                                    case 0xA0:
                                        alu.Res(ref Registers.B, 4);
                                        break;
                                    case 0xA1:
                                        alu.Res(ref Registers.C, 4);
                                        break;
                                    case 0xA2:
                                        alu.Res(ref Registers.D, 4);
                                        break;
                                    case 0xA3:
                                        alu.Res(ref Registers.E, 4);
                                        break;
                                    case 0xA4:
                                        alu.Res(ref Registers.H, 4);
                                        break;
                                    case 0xA5:
                                        alu.Res(ref Registers.L, 4);
                                        break;
                                    case 0xA6:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 4);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xA7:
                                        alu.Res(ref Registers.A, 4);
                                        break;
                                    case 0xA8:
                                        alu.Res(ref Registers.B, 5);
                                        break;
                                    case 0xA9:
                                        alu.Res(ref Registers.C, 5);
                                        break;
                                    case 0xAA:
                                        alu.Res(ref Registers.D, 5);
                                        break;
                                    case 0xAB:
                                        alu.Res(ref Registers.E, 5);
                                        break;
                                    case 0xAC:
                                        alu.Res(ref Registers.H, 5);
                                        break;
                                    case 0xAD:
                                        alu.Res(ref Registers.L, 5);
                                        break;
                                    case 0xAE:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 5);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xAF:
                                        alu.Res(ref Registers.A, 5);
                                        break;
                                    case 0xB0:
                                        alu.Res(ref Registers.B, 6);
                                        break;
                                    case 0xB1:
                                        alu.Res(ref Registers.C, 6);
                                        break;
                                    case 0xB2:
                                        alu.Res(ref Registers.D, 6);
                                        break;
                                    case 0xB3:
                                        alu.Res(ref Registers.E, 6);
                                        break;
                                    case 0xB4:
                                        alu.Res(ref Registers.H, 6);
                                        break;
                                    case 0xB5:
                                        alu.Res(ref Registers.L, 6);
                                        break;
                                    case 0xB6:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 6);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xB7:
                                        alu.Res(ref Registers.A, 6);
                                        break;
                                    case 0xB8:
                                        alu.Res(ref Registers.B, 7);
                                        break;
                                    case 0xB9:
                                        alu.Res(ref Registers.C, 7);
                                        break;
                                    case 0xBA:
                                        alu.Res(ref Registers.D, 7);
                                        break;
                                    case 0xBB:
                                        alu.Res(ref Registers.E, 7);
                                        break;
                                    case 0xBC:
                                        alu.Res(ref Registers.H, 7);
                                        break;
                                    case 0xBD:
                                        alu.Res(ref Registers.L, 7);
                                        break;
                                    case 0xBE:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Res(ref b, 7);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xBF:
                                        alu.Res(ref Registers.A, 7);
                                        break;
                                    case 0xC0:
                                        alu.Set(ref Registers.B, 0);
                                        break;
                                    case 0xC1:
                                        alu.Set(ref Registers.C, 0);
                                        break;
                                    case 0xC2:
                                        alu.Set(ref Registers.D, 0);
                                        break;
                                    case 0xC3:
                                        alu.Set(ref Registers.E, 0);
                                        break;
                                    case 0xC4:
                                        alu.Set(ref Registers.H, 0);
                                        break;
                                    case 0xC5:
                                        alu.Set(ref Registers.L, 0);
                                        break;
                                    case 0xC6:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 0);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xC7:
                                        alu.Set(ref Registers.A, 0);
                                        break;
                                    case 0xC8:
                                        alu.Set(ref Registers.B, 1);
                                        break;
                                    case 0xC9:
                                        alu.Set(ref Registers.C, 1);
                                        break;
                                    case 0xCA:
                                        alu.Set(ref Registers.D, 1);
                                        break;
                                    case 0xCB:
                                        alu.Set(ref Registers.E, 1);
                                        break;
                                    case 0xCC:
                                        alu.Set(ref Registers.H, 1);
                                        break;
                                    case 0xCD:
                                        alu.Set(ref Registers.L, 1);
                                        break;
                                    case 0xCE:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 1);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xCF:
                                        alu.Set(ref Registers.A, 1);
                                        break;
                                    case 0xD0:
                                        alu.Set(ref Registers.B, 2);
                                        break;
                                    case 0xD1:
                                        alu.Set(ref Registers.C, 2);
                                        break;
                                    case 0xD2:
                                        alu.Set(ref Registers.D, 2);
                                        break;
                                    case 0xD3:
                                        alu.Set(ref Registers.E, 2);
                                        break;
                                    case 0xD4:
                                        alu.Set(ref Registers.H, 2);
                                        break;
                                    case 0xD5:
                                        alu.Set(ref Registers.L, 2);
                                        break;
                                    case 0xD6:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 2);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xD7:
                                        alu.Set(ref Registers.A, 2);
                                        break;
                                    case 0xD8:
                                        alu.Set(ref Registers.B, 3);
                                        break;
                                    case 0xD9:
                                        alu.Set(ref Registers.C, 3);
                                        break;
                                    case 0xDA:
                                        alu.Set(ref Registers.D, 3);
                                        break;
                                    case 0xDB:
                                        alu.Set(ref Registers.E, 3);
                                        break;
                                    case 0xDC:
                                        alu.Set(ref Registers.H, 3);
                                        break;
                                    case 0xDD:
                                        alu.Set(ref Registers.L, 3);
                                        break;
                                    case 0xDE:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 3);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xDF:
                                        alu.Set(ref Registers.A, 3);
                                        break;
                                    case 0xE0:
                                        alu.Set(ref Registers.B, 4);
                                        break;
                                    case 0xE1:
                                        alu.Set(ref Registers.C, 4);
                                        break;
                                    case 0xE2:
                                        alu.Set(ref Registers.D, 4);
                                        break;
                                    case 0xE3:
                                        alu.Set(ref Registers.E, 4);
                                        break;
                                    case 0xE4:
                                        alu.Set(ref Registers.H, 4);
                                        break;
                                    case 0xE5:
                                        alu.Set(ref Registers.L, 4);
                                        break;
                                    case 0xE6:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 4);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xE7:
                                        alu.Set(ref Registers.A, 4);
                                        break;
                                    case 0xE8:
                                        alu.Set(ref Registers.B, 5);
                                        break;
                                    case 0xE9:
                                        alu.Set(ref Registers.C, 5);
                                        break;
                                    case 0xEA:
                                        alu.Set(ref Registers.D, 5);
                                        break;
                                    case 0xEB:
                                        alu.Set(ref Registers.E, 5);
                                        break;
                                    case 0xEC:
                                        alu.Set(ref Registers.H, 5);
                                        break;
                                    case 0xED:
                                        alu.Set(ref Registers.L, 5);
                                        break;
                                    case 0xEE:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 5);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xEF:
                                        alu.Set(ref Registers.A, 5);
                                        break;
                                    case 0xF0:
                                        alu.Set(ref Registers.B, 6);
                                        break;
                                    case 0xF1:
                                        alu.Set(ref Registers.C, 6);
                                        break;
                                    case 0xF2:
                                        alu.Set(ref Registers.D, 6);
                                        break;
                                    case 0xF3:
                                        alu.Set(ref Registers.E, 6);
                                        break;
                                    case 0xF4:
                                        alu.Set(ref Registers.H, 6);
                                        break;
                                    case 0xF5:
                                        alu.Set(ref Registers.L, 6);
                                        break;
                                    case 0xF6:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 6);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xF7:
                                        alu.Set(ref Registers.A, 6);
                                        break;
                                    case 0xF8:
                                        alu.Set(ref Registers.B, 7);
                                        break;
                                    case 0xF9:
                                        alu.Set(ref Registers.C, 7);
                                        break;
                                    case 0xFA:
                                        alu.Set(ref Registers.D, 7);
                                        break;
                                    case 0xFB:
                                        alu.Set(ref Registers.E, 7);
                                        break;
                                    case 0xFC:
                                        alu.Set(ref Registers.H, 7);
                                        break;
                                    case 0xFD:
                                        alu.Set(ref Registers.L, 7);
                                        break;
                                    case 0xFE:
                                        {
                                            var b = device.bus.ReadByte(Registers.HL);
                                            yield return 1;
                                            alu.Set(ref b, 7);
                                            device.bus.WriteByte(Registers.HL, b);
                                            yield return 1;
                                            break;
                                        }
                                    case 0xFF:
                                        alu.Set(ref Registers.A, 7);
                                        break;
                                    default:
                                        throw new ArgumentException();
                                }
                                break;
                            }
                        case 0xCC:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlag.ZERO_FLAG))
                                {
                                    break;
                                }

                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCD:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xCE:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Add(ref Registers.A, b, true);
                                break;
                            }
                        case 0xCF:
                            {
                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x08;
                                break;
                            }
                        case 0xD0:
                            {
                                yield return 1;
                                if (Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD1:
                            {
                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                Registers.DE = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD2:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD3:
                            break;
                        case 0xD4:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD5:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.D);
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.E);
                                yield return 1;
                                break;
                            }
                        case 0xD6:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Sub(ref Registers.A, b, false);
                                break;
                            }
                        case 0xD7:
                            {
                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x10;
                                break;
                            }
                        case 0xD8:
                            {
                                yield return 1;
                                if (!Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xD9:
                            {
                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));

                                device.interrupt_registers.AreInterruptsEnabledGlobally = true;
                                break;
                            }
                        case 0xDA:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;
                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xDB:
                            break;
                        case 0xDC:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                if (!Registers.GetFlag(CpuFlag.CARRY_FLAG))
                                {
                                    break;
                                }

                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xDD:
                            break;
                        case 0xDE:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Sub(ref Registers.A, b, true);
                                break;
                            }
                        case 0xDF:
                            {
                                yield return 1;

                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x18;
                                break;
                            }
                        case 0xE0:
                            {
                                var b = Read();
                                yield return 1;
                                device.bus.WriteByte((ushort)(0xFF00 + b), Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0xE1:
                            {
                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                Registers.HL = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xE2:
                            device.bus.WriteByte((ushort)(0xFF00 + Registers.C), Registers.A);
                            yield return 1;
                            break;
                        case 0xE3:
                        case 0xE4:
                            break;
                        case 0xE5:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.H);
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.L);
                                yield return 1;
                                break;
                            }
                        case 0xE6:
                            {
                                var b = Read();
                                yield return 1;
                                alu.And(ref Registers.A, b);
                                break;
                            }
                        case 0xE7:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x20;
                                break;
                            }
                        case 0xE8:
                            {
                                var b = (sbyte)Read();
                                yield return 1;
                                yield return 1;
                                yield return 1;
                                alu.AddSP(b);
                                break;
                            }
                        case 0xE9:
                            Registers.program_counter = Registers.HL;
                            break;
                        case 0xEA:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;

                                device.bus.WriteByte((ushort)(first | (second << 8)), Registers.A);
                                yield return 1;
                                break;
                            }
                        case 0xEB:
                        case 0xEC:
                        case 0xED:
                            break;
                        case 0xEE:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Xor(ref Registers.A, b);
                                break;
                            }
                        case 0xEF:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x28;
                                break;
                            }
                        case 0xF0:
                            {
                                var address = (ushort)(0xFF00 + Read());
                                yield return 1;
                                var b = device.bus.ReadByte(address);
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0xF1:
                            {
                                var first = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;
                                var second = device.bus.ReadByte(Registers.stack_pointer++);
                                yield return 1;

                                Registers.AF = (ushort)(first | (second << 8));
                                break;
                            }
                        case 0xF2:
                            {
                                var b = device.bus.ReadByte((ushort)(0xFF00 + Registers.C));
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0xF3:
                            device.interrupt_registers.AreInterruptsEnabledGlobally = false;
                            interrupt_cooldown = 0;
                            break;
                        case 0xF4:
                            break;
                        case 0xF5:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.A);
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, Registers.F);
                                yield return 1;
                                break;
                            }
                        case 0xF6:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Or(ref Registers.A, b);
                                break;
                            }
                        case 0xF7:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x30;
                                break;
                            }
                        case 0xF8:
                            {
                                var distance = (sbyte)Read();
                                yield return 1;
                                yield return 1;
                                alu.LoadHLSpPlusR8(distance);
                                break;
                            }
                        case 0xF9:
                            yield return 1;
                            Registers.stack_pointer = Registers.HL;
                            break;
                        case 0xFA:
                            {
                                var first = Read();
                                yield return 1;
                                var second = Read();
                                yield return 1;
                                var b = device.bus.ReadByte((ushort)(first | (second << 8)));
                                yield return 1;
                                Registers.A = b;
                                break;
                            }
                        case 0xFB:
                            if (interrupt_cooldown == 0)
                            {
                                interrupt_cooldown = 2;
                            }
                            break;
                        case 0xFC:
                        case 0xFD:
                            break;
                        case 0xFE:
                            {
                                var b = Read();
                                yield return 1;
                                alu.Cp(Registers.A, b);
                                break;
                            }
                        case 0xFF:
                            {
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter >> 8));
                                yield return 1;
                                device.bus.WriteByte(--Registers.stack_pointer, (byte)(Registers.program_counter & 0xFF));
                                yield return 1;

                                Registers.program_counter = 0x38;
                                break;
                            }
                        default:
                            throw new ArgumentException();
                    }

                    processing_intruction = false;
                    yield return 1;
                }
            }
        }

        internal void Reset()
        {
            Registers.Clear();
            halted = false;
            interrupt_cooldown = 0;
        }

        internal byte Read()
        {
            var b = device.bus.ReadByte(Registers.program_counter);
            Registers.program_counter = (ushort)(Registers.program_counter + 1);
            return b;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
