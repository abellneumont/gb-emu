namespace gbemu
{
    internal class ControlRegisters
    {
        private bool rom_disabled;
        private byte serial_transfer_control = 0b01111110;

        internal byte RomDisabledRegister
        {
            get => (byte)(rom_disabled ? 0xFF : 0x0);
            set
            {
                if (rom_disabled) return;
                rom_disabled = value == 0x1;
            }
        }

        private byte ff6c = 0xFE;
        internal byte FF6C
        {
            get => ff6c;
            set => ff6c = (byte)(value | 0xFE);
        }

        internal byte FF72 { get; set; }

        internal byte FF73 { get; set; }

        internal byte FF74 { get; set; }

        private byte ff75 = 0b1000_1111;
        internal byte FF75
        {
            get => ff75;
            set => ff75 = (byte)(value | 0b1000_1111);
        }

        internal bool SpeedSwitchRequested { get; set; }

        internal byte SerialTransferData { get; set; }
        internal byte SerialTransferControl { get => serial_transfer_control; set => serial_transfer_control = (byte)(0b01111110 | value); }

    }
}
