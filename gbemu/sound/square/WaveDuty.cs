using System;

namespace gbemu.sound
{
    internal enum WaveDuty
    {
        HALF_QUARTER = 0x0,
        QUARTER = 0x1,
        HALF = 0x2,
        THREE_QUARTER = 0x3
    }

    internal static class WaveDutyExtensions
    {
        internal static byte DutyByte(this WaveDuty waveDuty) => waveDuty switch
        {
            WaveDuty.HALF_QUARTER => 0b0000_0001,
            WaveDuty.QUARTER => 0b1000_0001,
            WaveDuty.HALF => 0b1000_0111,
            WaveDuty.THREE_QUARTER => 0b0111_1110,
            _ => throw new ArgumentOutOfRangeException(nameof(waveDuty), waveDuty, null)
        };
    }
}