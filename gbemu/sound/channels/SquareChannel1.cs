namespace gbemu.sound
{
    internal class SquareChannel1 : SquareWave
    {
        internal SquareChannel1(Device device) : base(device)
        {
            Sweep = new FrequencySweep(this);
            Envelope = new SoundEnvelope(this);
        }

        internal FrequencySweep Sweep { get; }

        internal SoundEnvelope Envelope { get; }

        private int current_freq_period;
        private int last_output;

        internal override void Reset()
        {
            base.Reset();
            Sweep.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            current_freq_period--;
            if (current_freq_period < 0)
            {
                current_freq_period = FrequencyPeriod;

                if (Enabled)
                {
                    last_output = NextDutyCycleValue();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();
            current_freq_period = FrequencyPeriod;
            last_output = 0;
            Envelope.Trigger();
            Sweep.Trigger(FrequencyPeriod);
        }

        internal override void SkipBootRom()
        {
            Sweep.Register = 0x80;
            ControlByte = 0xBF;
            Envelope.Register = 0xF3;
            LowByte = 0xFF;
            HighByte = 0xBF;
            Enabled = false;
        }

        internal override int GetOutputVolume()
        {
            return last_output * Envelope.Volume;
        }
    }
}
