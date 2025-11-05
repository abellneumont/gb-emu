using System;

namespace gbemu.ppu
{
    [Flags]
    internal enum StateMode
    {
        H_BLANK_PERIOD = 0x00,
        V_BLANK_PERIOD = 0x01,
        OAM_RAM_PERIOD = 0x02,
        TRANSFERRING_DATA = 0x03
    }
}