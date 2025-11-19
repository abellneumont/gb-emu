using System.Collections.Generic;

namespace gbemu.sound
{
    internal class APU
    {
        private const byte CONTROL_MASK = 0b0111_0000;

        private const int FRAME_SEQUENCE_TIMER = Device.CYCLES_PER_SECOND / 512;
        private int frame_sequence = FRAME_SEQUENCE_TIMER;

        private bool enabled;
        private readonly SquareChannel1 square_channel0;
        private readonly SquareChannel2 square_channel1;
        private readonly WaveChannel wave_channel;
        private readonly NoiseChannel noise_channel;
        private readonly Channel[] channels;

        private int left_volume;
        private bool left_vin;
        private int right_volume;
        private bool right_vin;

        private int down_sample_clock;

        private readonly Dictionary<Channel, (bool, bool)> sound_channels;

        private readonly Device device;

        internal APU(Device device)
        {
            this.device = device;
            square_channel0 = new SquareChannel1(this.device);
            square_channel1 = new SquareChannel2(this.device);
            wave_channel = new WaveChannel(this.device);
            noise_channel = new NoiseChannel(this.device);
            channels = new Channel[] { square_channel0, square_channel1, wave_channel, noise_channel };
            sound_channels = new Dictionary<Channel, (bool, bool)>
            {
                { square_channel0, (false, false) },
                { square_channel1, (false, false) },
                { wave_channel, (false, false) },
                { noise_channel, (false, false) },
            };
            down_sample_clock = Device.CYCLES_PER_SECOND / this.device.sound_output.AudioFrequency;
        }

        private byte nr50;
        internal byte NR50
        {
            get => nr50; private set
            {
                nr50 = value;
                right_volume = value & 0x7;
                right_vin = (value & 0x8) == 0x8;
                left_volume = (value >> 4) & 0x7;
                right_vin = (value & 0x80) == 0x80;
            }
        }

        private byte nr51;
        internal byte NR51
        {
            get => nr51;
            private set
            {
                nr51 = value;

                for (var ii = 0; ii < 4; ii++)
                {
                    var rightBit = 1 << ii;
                    var leftBit = 1 << (ii + 4);
                    sound_channels[channels[ii]] = ((value & rightBit) == rightBit, (value & leftBit) == leftBit);
                }
            }
        }

        internal byte NR52
        {
            get =>
                (byte)(CONTROL_MASK
                        | (enabled ? 0x80 : 0x0)
                        | (noise_channel.Enabled ? 0x08 : 0x0)
                        | (wave_channel.Enabled ? 0x04 : 0x0)
                        | (square_channel1.Enabled ? 0x02 : 0x0)
                        | (square_channel0.Enabled ? 0x01 : 0x0));
            private set
            {
                enabled = (value & 0x80) == 0x80;

                if (!enabled)
                {
                    Reset();
                }
            }
        }

        internal byte PCM12 =>
            (byte)(
                (square_channel1.Enabled ? square_channel1.GetOutputVolume() << 4 : 0) |
                (square_channel0.Enabled ? square_channel0.GetOutputVolume() : 0)
            );

        internal byte PCM34 =>
            (byte)(
                (noise_channel.Enabled ? noise_channel.GetOutputVolume() << 4 : 0) |
                (wave_channel.Enabled ? wave_channel.GetOutputVolume() : 0)
            );

        private void Reset()
        {
            left_volume = 0;
            left_vin = false;
            right_volume = 0;
            right_vin = false;
            down_sample_clock = Device.CYCLES_PER_SECOND / device.sound_output.AudioFrequency;

            foreach (var channel in channels)
            {
                channel.Reset();
                sound_channels[channel] = (false, false);
            }
        }

        internal void Write(ushort address, byte value)
        {
            if (!enabled)
            {
                if (address == 0xFF26)
                {
                    NR52 = value;
                }
                else if (address >= 0xFF30 && address <= 0xFF3F)
                {
                    wave_channel.WriteRam(address, value);
                }

                return;
            }

            if (address == 0xFF10)
                square_channel0.Sweep.Register = value;
            else if (address == 0xFF11)
                square_channel0.ControlByte = value;
            else if (address == 0xFF12)
                square_channel0.Envelope.Register = value;
            else if (address == 0xFF13)
                square_channel0.LowByte = value;
            else if (address == 0xFF14)
                square_channel0.HighByte = value;
            else if (address == 0xFF16)
                square_channel1.ControlByte = value;
            else if (address == 0xFF17)
                square_channel1.Envelope.Register = value;
            else if (address == 0xFF18)
                square_channel1.LowByte = value;
            else if (address == 0xFF19)
                square_channel1.HighByte = value;
            else if (address == 0xFF1A)
                wave_channel.NR30 = value;
            else if (address == 0xFF1B)
                wave_channel.NR31 = value;
            else if (address == 0xFF1C)
                wave_channel.NR32 = value;
            else if (address == 0xFF1D)
                wave_channel.NR33 = value;
            else if (address == 0xFF1E)
                wave_channel.NR34 = value;
            else if (address == 0xFF20)
                noise_channel.NR41 = value;
            else if (address == 0xFF21)
                noise_channel.Envelope.Register = value;
            else if (address == 0xFF22)
                noise_channel.NR43 = value;
            else if (address == 0xFF23)
                noise_channel.NR44 = value;
            else if (address == 0xFF24)
                NR50 = value;
            else if (address == 0xFF25)
                NR51 = value;
            else if (address == 0xFF26)
                NR52 = value;
            else if (address >= 0xFF30 && address <= 0xFF3F)
                wave_channel.WriteRam(address, value);
        }

        internal byte Read(ushort address)
        {
            return address switch
            {
                0xFF10 => square_channel0.Sweep.Register,
                0xFF11 => square_channel0.ControlByte,
                0xFF12 => square_channel0.Envelope.Register,
                0xFF13 => square_channel0.LowByte,
                0xFF14 => square_channel0.HighByte,
                0xFF15 => 0xFF,
                0xFF16 => square_channel1.ControlByte,
                0xFF17 => square_channel1.Envelope.Register,
                0xFF18 => square_channel1.LowByte,
                0xFF19 => square_channel1.HighByte,
                0xFF1A => wave_channel.NR30,
                0xFF1B => wave_channel.NR31,
                0xFF1C => wave_channel.NR32,
                0xFF1D => wave_channel.NR33,
                0xFF1E => wave_channel.NR34,
                0xFF1F => 0xFF,
                0xFF20 => noise_channel.NR41,
                0xFF21 => noise_channel.Envelope.Register,
                0xFF22 => noise_channel.NR43,
                0xFF23 => noise_channel.NR44,
                0xFF24 => NR50,
                0xFF25 => NR51,
                0xFF26 => NR52,
                _ when address >= 0xFF27 && address <= 0xFF29 => 0xFF,
                _ when address >= 0xFF30 && address <= 0xFF3F => wave_channel.ReadRam(address),
                _ => 0xFF
            };
        }

        private int step_sequence;
        private void StepFrameSequencer()
        {
            if (step_sequence % 2 == 0)
            {
                foreach (var channel in channels)
                {
                    if (channel.Enabled) channel.StepLength();
                }
            }

            switch (step_sequence)
            {
                case 2:
                case 6:
                    if (square_channel0.Enabled) square_channel0.Sweep.Step();
                    break;
                case 7:
                    if (square_channel0.Enabled) square_channel0.Envelope.Step();
                    if (square_channel1.Enabled) square_channel1.Envelope.Step();
                    if (noise_channel.Enabled) noise_channel.Envelope.Step();
                    break;
            }

            step_sequence = (step_sequence + 1) % 8;
        }

        internal void Step(int cycles)
        {
            while (cycles > 0)
            {
                cycles--;
                frame_sequence--;
                
                if (frame_sequence == 0)
                {
                    frame_sequence = FRAME_SEQUENCE_TIMER;

                    StepFrameSequencer();
                }

                foreach (var sound in channels)
                {
                    if (sound.Enabled)
                    {
                        sound.Step();
                    }
                }

                down_sample_clock--;
                
                if (down_sample_clock == 0)
                {
                    down_sample_clock = Device.CYCLES_PER_SECOND / device.sound_output.AudioFrequency;
                    var left = 0;
                    var right = 0;

                    noise_channel.GetOutputVolume();
                    foreach (var (channel, (right_enabled, left_enabled)) in sound_channels)
                    {
                        if (channel.Enabled)
                        {
                            if (right_enabled) right += channel.GetOutputVolume();
                            if (left_enabled) left += channel.GetOutputVolume();
                        }
                    }

                    left *= left_volume;
                    right *= right_volume;
                    
                    device.sound_output.PlaySoundByte(left, right);
                }
            }
        }

        public void SkipBootRom()
        {
            foreach (var channel in channels)
            {
                channel.SkipBootRom();
            }

            NR50 = 0x77;
            NR51 = 0xF3;
            NR52 = 0xF1;
        }
    }
}
