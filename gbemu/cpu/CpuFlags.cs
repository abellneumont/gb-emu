using System;

namespace gbemu.cpu
{
    [Flags]
    internal enum CpuFlag : byte
    {
        ZERO_FLAG = 0b10000000,
        SUBTRACT_FLAG = 0b01000000,
        HALF_CARRY_FLAG = 0b00100000,
        CARRY_FLAG = 0b00010000,
    }
}
