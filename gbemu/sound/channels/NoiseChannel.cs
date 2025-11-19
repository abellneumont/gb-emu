using System;

namespace gbemu.sound
{
    internal class NoiseChannel : Channel
    {
        internal NoiseChannel(Device device) : base(device)
        {
            Envelope = new SoundEnvelope(this);
        }

        protected override int BaseSoundLength => 64;

        internal byte NR41
        {
            get => 0xFF;
            set => SoundLength = 64 - (value & 0b0011_1111);
        }

        internal SoundEnvelope Envelope { get; }

        private int internal_period;
        private int current_cycle;
        private int lfsr;
        private bool width_mode;
        private int output_volume;

        private byte nr43;
        internal byte NR43
        {
            get => nr43;
            set
            {
                nr43 = value;
                var clock_shift = value >> 4;
                width_mode = (value & 0x8) == 0x8;
                var divisor = (value & 0x7) switch
                {
                    0 => 8,
                    1 => 16,
                    2 => 32,
                    3 => 48,
                    4 => 64,
                    5 => 80,
                    6 => 96,
                    7 => 112,
                    _ => throw new ArgumentOutOfRangeException()
                };
                internal_period = divisor << clock_shift;
                current_cycle = 1;
            }
        }

        internal byte NR44
        {
            get =>
                (byte)(0xBF |
                        (UseSoundLength ? 0x40 : 0x0) |
                        (Enabled ? 0x80 : 0x0));
            set
            {
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

            current_cycle = internal_period;
            lfsr = 0x7FFF;
            Envelope.Trigger();
        }

        internal override void SkipBootRom()
        {
            NR41 = 0xFF;
            Envelope.Register = 0x0;
            NR43 = 0x0;
            NR44 = 0xBF;
            Enabled = false;
        }

        internal override void Reset()
        {
            base.Reset();
            Envelope.Reset();
            internal_period = 0;
            current_cycle = 1;
            lfsr = 0x7FFF;
            width_mode = false;
            output_volume = 0;
        }

        internal override void Step()
        {
            current_cycle--;
            if (current_cycle < 0)
            {
                current_cycle = internal_period;

                var xor_bit = (lfsr & 0x1) ^ ((lfsr & 0x2) >> 1);
                lfsr >>= 1;
                lfsr |= (xor_bit << 14);
                if (width_mode)
                {
                    lfsr |= (xor_bit << 6);
                }

                output_volume = (lfsr & 0x1) == 0x1 ? 0 : 0x1;
            }
        }

        internal override int GetOutputVolume()
        {
            return output_volume * Envelope.Volume;
        }
    }
}
