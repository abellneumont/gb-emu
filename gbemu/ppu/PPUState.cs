namespace gbemu.ppu
{
    internal enum PPUState
    {
        H_BLANK_PERIOD = 0,
        V_BLANK_PERIOD = 1,
        OAM_RAM_PERIOD = 2,
        TRANSFERRING_DATA = 3
    }
}
