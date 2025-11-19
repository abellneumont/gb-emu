namespace gbemu.sound
{
    internal class FrequencySweep
    {
        private readonly SquareChannel1 sound;

        internal FrequencySweep(SquareChannel1 sound)
        {
            this.sound = sound;
        }

        private const byte REGISTER_MASK = 0b1000_0000;

        private bool enabled;

        private bool sweep_decrease;
        private int sweep_period;
        private int sweep_shift;
        private int shadow_register;

        private int current_period;

        internal byte Register
        {
            get =>
                (byte)(REGISTER_MASK |
                        (sweep_decrease ? 0x8 : 0x0) |
                        (sweep_period << 4) |
                        sweep_shift);
            set
            {
                sweep_shift = value & 0x7;
                sweep_decrease = (value & 0x8) == 0x8;
                sweep_period = (value >> 4) & 0x7;
            }
        }

        internal void Trigger(int squareWaveFrequency)
        {
            shadow_register = squareWaveFrequency;
            current_period = sweep_period;

            enabled = (current_period != 0 || sweep_shift != 0);

            if (sweep_shift != 0)
            {
                SweepCalculation();
            }
        }

        internal void Reset()
        {
            sweep_shift = 0x0;
            sweep_decrease = false;
            sweep_period = 0;
        }

        internal void Step()
        {
            current_period--;

            if (current_period == 0)
            {
                current_period = sweep_period;

                if (current_period == 0)
                {
                    current_period = 8;
                }

                if (enabled && sweep_period > 0)
                {
                    SweepCalculation();
                }
            }
        }

        private void SweepCalculation()
        {
            var workingValue = shadow_register >> sweep_shift;
            
            if (sweep_decrease)
            {
                workingValue = shadow_register - workingValue;
            }
            else
            {
                workingValue = shadow_register + workingValue;
            }

            if (workingValue > 2047)
            {
                sound.Enabled = false;
            }

            shadow_register = workingValue;
            sound.FrequencyData = workingValue;
        }
    }
}
