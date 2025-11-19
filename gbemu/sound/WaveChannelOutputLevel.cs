using System;

namespace gbemu.sound
{
    internal enum WaveChannelOutputLevel
    {
        MUTE = 0b00,
        NO_CHANGE = 0b01,
        HALF = 0b10,
        QUARTER = 0b11
    }

    internal static class WaveChannelOutputLevelExtensions
    {
        internal static int RightShiftValue(this WaveChannelOutputLevel outputLevel) => outputLevel switch
        {
            WaveChannelOutputLevel.MUTE => 4,
            WaveChannelOutputLevel.NO_CHANGE => 0,
            WaveChannelOutputLevel.HALF => 1,
            WaveChannelOutputLevel.QUARTER => 2,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}