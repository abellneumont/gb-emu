using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbemu
{

    internal enum InterruptType
    {
        VERTICAL_BLANK = 0,
        LCD_STATE = 1,
        TIMER = 2,
        SERIAL = 3,
        CONTROLLER = 4
    }

    internal static class Interrupt
    {
        internal static int Priority(this InterruptType type) => (int)type + 1;

        internal static ushort StartingAddress(this InterruptType type) => type switch
        {
            InterruptType.VERTICAL_BLANK => 0x40,
            InterruptType.LCD_STATE => 0x48,
            InterruptType.TIMER => 0x50,
            InterruptType.SERIAL => 0x58,
            InterruptType.CONTROLLER => 0x60,
            _ => throw new ArgumentOutOfRangeException()
        };

        internal static byte Mask(this InterruptType type) => type switch
        {
            InterruptType.VERTICAL_BLANK => 0x01,
            InterruptType.LCD_STATE => 0x02,
            InterruptType.TIMER => 0x04,
            InterruptType.SERIAL => 0x08,
            InterruptType.CONTROLLER => 0x10,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
