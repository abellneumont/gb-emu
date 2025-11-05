using System;

namespace gbemu.interrupts
{
    internal enum Interrupt
    {
        VERTICAL_BLANK = 0,
        LCD_STATE = 1,
        TIMER = 2,
        SERIAL = 3,
        CONTROLLER = 4
    }

    internal static class InterruptExtensions
    {
        internal static int Priority(this Interrupt interrupt) => interrupt switch
        {
            Interrupt.VERTICAL_BLANK => 1,
            Interrupt.LCD_STATE => 2,
            Interrupt.TIMER => 3,
            Interrupt.SERIAL => 4,
            Interrupt.CONTROLLER => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, null)
        };

        internal static ushort StartingAddress(this Interrupt interrupt) => interrupt switch
        {
            Interrupt.VERTICAL_BLANK => 0x40,
            Interrupt.LCD_STATE => 0x48,
            Interrupt.TIMER => 0x50,
            Interrupt.SERIAL => 0x58,
            Interrupt.CONTROLLER => 0x60,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, null)
        };

        internal static byte Mask(this Interrupt interrupt) => interrupt switch
        {
            Interrupt.VERTICAL_BLANK => 0b00000001,
            Interrupt.LCD_STATE => 0b00000010,
            Interrupt.TIMER => 0b00000100,
            Interrupt.SERIAL => 0b00001000,
            Interrupt.CONTROLLER => 0b00010000,
            _ => throw new ArgumentOutOfRangeException(nameof(interrupt), interrupt, null)
        };
    }
}