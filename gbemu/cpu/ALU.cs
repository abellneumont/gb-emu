namespace gbemu.cpu
{
    internal class ALU
    {
        private readonly CPU cpu;

        internal ALU(CPU cpu)
        {
            this.cpu = cpu;
        }

        internal void RotateLeftCarryA()
        {
            RotateLeftCarry(ref cpu.Registers.A);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, false);
        }

        internal void RotateLeftCarry(ref byte a)
        {
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, a > 0x7F);
            a = (byte)((a << 1) | (a >> 7));
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0x0);
        }

        internal void RotateLeftA()
        {
            RotateLeft(ref cpu.Registers.A);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, false);
        }

        internal void RotateLeft(ref byte a)
        {
            var setCarry = (a & 0x80) == 0x80;
            a = (byte)((a << 1) | (cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG) ? 0x1 : 0x0));
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, setCarry);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0x0);
        }

        internal void RotateRightCarryA()
        {
            RotateRightCarry(ref cpu.Registers.A);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, false);
        }

        internal void RotateRightCarry(ref byte a)
        {
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, (a & 0x1) == 0x1);
            a = (byte)((a >> 1) | ((a & 1) << 7));
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0x0);
        }

        internal void RotateRightA()
        {
            RotateRight(ref cpu.Registers.A);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, false);
        }

        internal void RotateRight(ref byte a)
        {
            var setCarry = (a & 0x1) == 0x1;
            a = (byte)((a >> 1) | (cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG) ? 0x80 : 0x0));
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, setCarry);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0x0);
        }

        internal void ShiftLeft(ref byte a)
        {
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, (a & 0x80) == 0x80);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            a = (byte)(a << 1);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0);
        }

        internal void ShiftRightAdjust(ref byte a)
        {
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, (a & 0x01) == 0x01);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            a = (byte)((a >> 1) | (a & 0x80));
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0);
        }

        internal void ShiftRight(ref byte a)
        {
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, (a & 0x01) == 0x01);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            a = (byte)(a >> 1);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0);
        }

        internal void Swap(ref byte a)
        {
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.CARRY_FLAG, false);
            a = (byte)((a >> 4) | (a << 4));
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0);
        }

        internal void Bit(byte a, int bit)
        {
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, true);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, (a & (1 << bit)) == 0);
        }

        internal void Res(ref byte a, int bit)
        {
            a = (byte)(a & ~(1 << bit));
        }

        internal void Set(ref byte a, int bit)
        {
            a = (byte)(a | (1 << bit));
        }

        internal void Increment(ref byte a)
        {
            var result = (byte)(a + 1);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, (a & 0x0F) + 1 > 0x0F);
            a = result;
        }

        internal void Decrement(ref byte a)
        {
            var result = (byte)(a - 1);
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG, true);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, (a & 0x0F) < 0x01);
            a = result;
        }

        internal void Add(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG) ? 1 : 0;
            var result = a + b + c;
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, (result & 0xFF) == 0x0);
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, (((a & 0xF) + (b & 0xF) + (c & 0xF)) & 0x10) == 0x10);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, result > 0xFF);
            a = (byte)result;
        }

        internal void Sub(ref byte a, byte b, bool includeCarry)
        {
            var c = includeCarry && cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG) ? 1 : 0;
            var result = a - b - c;
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, (result & 0xFF) == 0x0);
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG, true);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, (a & 0x0F) < (b & 0x0F) + c);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, result < 0);
            a = (byte)result;
        }

        internal void And(ref byte a, byte b)
        {
            var result = a & b;
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, true);
            a = (byte)result;
        }

        internal void Xor(ref byte a, byte b)
        {
            var result = a ^ b;
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.HALF_CARRY_FLAG, false);
            a = (byte)result;
        }

        internal void Or(ref byte a, byte b)
        {
            var result = a | b;
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, result == 0);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG | CpuFlag.SUBTRACT_FLAG | CpuFlag.HALF_CARRY_FLAG, false);
            a = (byte)result;
        }

        internal void Cp(byte a, byte b)
        {
            Sub(ref a, b, false);
        }

        internal void DecimalAdjustRegister(ref byte a)
        {
            int tmp = a;

            if (!cpu.Registers.GetFlag(CpuFlag.SUBTRACT_FLAG))
            {
                if (cpu.Registers.GetFlag(CpuFlag.HALF_CARRY_FLAG) || (tmp & 0x0F) > 9)
                    tmp += 0x06;
                if (cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG) || tmp > 0x9F)
                    tmp += 0x60;
            }
            else
            {
                if (cpu.Registers.GetFlag(CpuFlag.HALF_CARRY_FLAG))
                {
                    tmp -= 0x06;
                    if (!cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG))
                        tmp &= 0xFF;
                }
                if (cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG))
                    tmp -= 0x60;
            }

            a = (byte)tmp;
            cpu.Registers.SetFlag(CpuFlag.ZERO_FLAG, a == 0x0);
            if (tmp > 0xFF) cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, true);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, false);
        }

        internal void CCF()
        {
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, !cpu.Registers.GetFlag(CpuFlag.CARRY_FLAG));
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
        }

        internal void SCF()
        {
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, true);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, false);
        }

        internal void CPL()
        {
            cpu.Registers.A = (byte)(~cpu.Registers.A);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG | CpuFlag.SUBTRACT_FLAG, true);
        }

        internal void AddHL(ushort b)
        {
            var result = cpu.Registers.HL + b;
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, (cpu.Registers.HL & 0xFFF) + (b & 0xFFF) > 0xFFF);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, result > 0xFFFF);
            cpu.Registers.HL = (ushort)result;
        }

        internal void AddSP(sbyte operand)
        {
            var result = cpu.Registers.stack_pointer + operand;
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG | CpuFlag.ZERO_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, ((cpu.Registers.stack_pointer ^ operand ^ (result & 0xFFFF)) & 0x10) == 0x10);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, ((cpu.Registers.stack_pointer ^ operand ^ (result & 0xFFFF)) & 0x100) == 0x100);
            cpu.Registers.stack_pointer = (ushort)result;
        }

        internal void LoadHLSpPlusR8(sbyte operand)
        {
            var result = cpu.Registers.stack_pointer + operand;
            cpu.Registers.SetFlag(CpuFlag.SUBTRACT_FLAG | CpuFlag.ZERO_FLAG, false);
            cpu.Registers.SetFlag(CpuFlag.HALF_CARRY_FLAG, ((cpu.Registers.stack_pointer ^ operand ^ (result & 0xFFFF)) & 0x10) == 0x10);
            cpu.Registers.SetFlag(CpuFlag.CARRY_FLAG, ((cpu.Registers.stack_pointer ^ operand ^ (result & 0xFFFF)) & 0x100) == 0x100);
            cpu.Registers.HL = (ushort)result;
        }
    }
}
