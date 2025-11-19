namespace gbemu.sound
{
    internal class SoundEnvelope
    {
        private readonly Channel channel;

        internal SoundEnvelope(Channel channel)
        {
            this.channel = channel;
        }

        private int current_period;
        internal int Period { get; private set; }

        internal EnvelopeUpDown EnvelopeUpDown { get; private set; }

        internal int InitialVolume { get; private set; }

        internal int Volume;

        internal byte Register
        {
            get =>
                (byte)(Period |
                        (EnvelopeUpDown == EnvelopeUpDown.AMPLIFY ? 0x8 : 0x0) |
                        InitialVolume << 4);
            set
            {
                Period = value & 0x7;
                ResetCurrentPeriod();
                EnvelopeUpDown = (value & 0x8) == 0x8 ? EnvelopeUpDown.AMPLIFY : EnvelopeUpDown.ATTENUATE;
                InitialVolume = value >> 4;
                Volume = InitialVolume;

                if ((value & 0b1111_1000) == 0)
                {
                    channel.Enabled = false;
                }
            }
        }

        internal void Reset()
        {
            Period = 0x0;
            ResetCurrentPeriod();
            EnvelopeUpDown = EnvelopeUpDown.ATTENUATE;
            InitialVolume = 0x0;
        }

        private void ResetCurrentPeriod()
        {
            current_period = Period == 0 ? 8 : Period;
        }

        internal void Step()
        {
            if ((EnvelopeUpDown == EnvelopeUpDown.AMPLIFY && Volume == 15) || (EnvelopeUpDown == EnvelopeUpDown.ATTENUATE && Volume == 0))
            {
                return;
            }

            current_period--;

            if (current_period == 0)
            {
                ResetCurrentPeriod();

                if (current_period == 0)
                {
                    current_period = 8;
                }

                Volume += EnvelopeUpDown == EnvelopeUpDown.AMPLIFY ? 1 : -1;
            }
        }

        internal void Trigger()
        {
            ResetCurrentPeriod();
            Volume = InitialVolume;

            if ((Register & 0b1111_1000) == 0)
            {
                channel.Enabled = false;
            }
        }
    }
}