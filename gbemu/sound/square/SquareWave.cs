namespace gbemu.sound
{
    internal abstract class SquareWave : Channel
    {
        private const byte CONTROL_MASK = 0b0011_1111;
        private const byte HIGH_MASK = 0b1011_1111;

        protected SquareWave(Device device) : base(device)
        {
        }

        protected override int BaseSoundLength => 64;

        internal int FrequencyData { get; set; }

        protected int ActualFrequencyHz => 131072 / (2048 - FrequencyData);

        protected int FrequencyPeriod => 4 * (2048 - FrequencyData);

        protected WaveDuty DutyCycle { get; private set; }

        private int duty_cycle_bit;

        internal byte ControlByte
        {
            get => (byte)(
                CONTROL_MASK |
                ((int)DutyCycle << 6));
            set
            {
                DutyCycle = (WaveDuty)(value >> 6);
                SoundLength = 64 - (value & CONTROL_MASK);
            }
        }

        internal byte LowByte
        {
            get => (byte)FrequencyData;
            set => FrequencyData = (FrequencyData & 0x700) | value;
        }

        internal byte HighByte
        {
            get => (byte)(HIGH_MASK | ((FrequencyData & 0x700) >> 8));
            set
            {
                FrequencyData = (FrequencyData & 0xFF) | ((value & 0x7) << 8);
                UseSoundLength = (value & 0x40) == 0x40;
                if ((value & 0x80) == 0x80)
                {
                    Trigger();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();

            duty_cycle_bit = 0;
        }

        internal override void Reset()
        {
            base.Reset();
            FrequencyData = 0x0;
            DutyCycle = WaveDuty.HALF_QUARTER;
            duty_cycle_bit = 0;
        }

        protected int NextDutyCycleValue()
        {
            var output = (DutyCycle.DutyByte() & (1 << duty_cycle_bit)) >> duty_cycle_bit;
            duty_cycle_bit = (duty_cycle_bit + 1) % 8;
            return output;
        }
    }
}
