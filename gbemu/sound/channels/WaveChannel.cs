namespace gbemu.sound
{
    internal class WaveChannel : Channel
    {
        private static readonly byte[] dmg_wave =
        {
            0xAC, 0xDD, 0xDA, 0x48, 0x36, 0x02, 0xCF, 0x16,
            0x2C, 0x04, 0xE5, 0x2C, 0xAC, 0xDD, 0xDA, 0x48
        };

        protected override int BaseSoundLength => 0xFF;

        private const int WAVE_RAM_SIZE = 0x10;
        private const int WAVE_SAMPLE_SIZE = WAVE_RAM_SIZE * 2;
        private readonly byte[] wave_ram = new byte[WAVE_RAM_SIZE];
        private readonly int[] wave_samples = new int[WAVE_SAMPLE_SIZE];
        private int wave_sample_couter;

        internal WaveChannelOutputLevel Volume { get; private set; }

        internal int FrequencyData { get; private set; }

        private int FrequencyPeriod => 2 * (2048 - FrequencyData);
        private int current_freq_period;
        private byte sample_buffer;

        internal WaveChannel(Device device) : base(device)
        {
            for (var ii = 0; ii < 0x10; ii++)
            {
                WriteRam((ushort)(ii + 0xFF30), dmg_wave[ii]);
            }
        }

        internal byte ReadRam(ushort address)
        {
            return wave_ram[address - 0xFF30];
        }

        internal void WriteRam(ushort address, byte value)
        {
            var ramAddress = address - 0xFF30;
            wave_ram[ramAddress] = value;

            wave_samples[ramAddress * 2] = value >> 4;
            wave_samples[ramAddress * 2 + 1] = value & 0b1111;
        }

        internal byte NR30
        {
            get => (byte)(0b0111_1111 | (Enabled ? 0b1000_0000 : 0));
            set => Enabled = (value & 0x80) == 0x80;
        }


        internal byte NR31
        {
            get => (byte)(256 - SoundLength);
            set => SoundLength = 256 - value;
        }

        internal byte NR32
        {
            get => (byte)(0b1001_1111 | (int)Volume << 5);
            set => Volume = (WaveChannelOutputLevel)((value >> 5) & 0x3);
        }

        internal byte NR33
        {
            get => (byte)FrequencyData;
            set => FrequencyData = (FrequencyData & 0x700) | value;
        }

        internal byte NR34
        {
            get =>
                (byte)(0b1011_1000 |
                        (FrequencyData >> 8) |
                        (UseSoundLength ? 0b0100_0000 : 0));
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
            current_freq_period = FrequencyPeriod;
            wave_sample_couter = 0;

            if ((NR30 & 0x80) == 0)
            {
                Enabled = false;
            }
        }

        internal override void SkipBootRom()
        {
            NR30 = 0x7F;
            NR31 = 0xFF;
            NR32 = 0x9F;
            NR34 = 0xBF;
            Enabled = false;
        }

        internal override void Reset()
        {
            base.Reset();
            FrequencyData = 0x0;
            Volume = WaveChannelOutputLevel.MUTE;
            wave_sample_couter = 0;
            current_freq_period = 0;
        }

        internal override void Step()
        {
            current_freq_period--;
            if (current_freq_period == 0)
            {
                current_freq_period = FrequencyPeriod;
                wave_sample_couter = (wave_sample_couter + 1) % WAVE_SAMPLE_SIZE;
                sample_buffer = (byte)wave_samples[wave_sample_couter];
            }
        }

        internal override int GetOutputVolume()
        {
            return sample_buffer >> Volume.RightShiftValue();
        }
    }
}
