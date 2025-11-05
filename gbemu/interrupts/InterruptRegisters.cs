namespace gbemu.interrupts
{
    internal class InterruptRegisters
    {
        internal bool AreInterruptsEnabledGlobally { get; set; }

        private byte interrupt_flags = 0b11100000;
        internal byte InterruptFlags
        {
            get => interrupt_flags;
            set => interrupt_flags = (byte)(0b11100000 | value);
        }

        internal byte InterruptEnable { get; set; }

        internal void RequestInterrupt(Interrupt interrupt)
        {
            interrupt_flags = (byte)(interrupt_flags | interrupt.Mask());
        }

        internal void ResetInterrupt(Interrupt interrupt)
        {
            interrupt_flags = (byte)(interrupt_flags & ~interrupt.Mask());
        }
    }
}
