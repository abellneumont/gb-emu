namespace gbemu.sound
{
    internal class SquareChannel2 : SquareWave
    {
        internal SquareChannel2(Device device) : base(device)
        {
            Envelope = new SoundEnvelope(this);
        }

        internal SoundEnvelope Envelope { get; }

        private int freq_period;
        private int last_output;

        internal override void Reset()
        {
            base.Reset();
            Envelope.Reset();
        }

        internal override void Step()
        {
            freq_period--;
            if (freq_period == 0)
            {
                freq_period = FrequencyPeriod;

                if (Enabled)
                {
                    last_output = NextDutyCycleValue();
                }
            }
        }

        internal override void Trigger()
        {
            base.Trigger();
            freq_period = FrequencyPeriod;
            last_output = 0;
            Envelope.Trigger();
        }

        internal override void SkipBootRom()
        {
            ControlByte = 0x3F;
            Envelope.Register = 0x0;
            HighByte = 0xBF;
            Enabled = false;
        }

        internal override int GetOutputVolume()
        {
            return last_output * Envelope.Volume;
        }
    }
}
