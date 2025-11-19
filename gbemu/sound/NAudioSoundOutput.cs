using System;
using NAudio.Wave;

namespace gbemu.sound
{
    public class NAudioSoundOutput : ISoundOutput, IDisposable
    {
        private const int AUDIO_SAMPLES = 2048;
        private const int CHANNELS = 2;

        private readonly BufferedWaveProvider wave_provider;
        private readonly WaveOutEvent wave_player;

        private readonly byte[] sound_buffer = new byte[AUDIO_SAMPLES * CHANNELS];
        private int sound_buffer_index;

        internal NAudioSoundOutput()
        {
            wave_provider = new BufferedWaveProvider(new WaveFormat(AudioFrequency, 8, CHANNELS));
            wave_player = new WaveOutEvent();
            wave_player.DesiredLatency = 100;
            wave_player.Init(wave_provider);
            wave_player.Play();
        }

        public int AudioFrequency => 48000;

        public void PlaySoundByte(int left, int right)
        {
            sound_buffer[sound_buffer_index] = (byte)left;
            sound_buffer[sound_buffer_index + 1] = (byte)right;
            sound_buffer_index += 2;

            if (sound_buffer_index == sound_buffer.Length)
            {
                while (wave_provider.BufferedDuration.Milliseconds > 100)
                {
                    // Wait for wave player
                }

                wave_provider.AddSamples(sound_buffer, 0, sound_buffer_index);
                sound_buffer_index = 0;

                Array.Clear(sound_buffer, 0, sound_buffer.Length);
            }
        }

        public bool IsBufferLow()
        {
            return wave_provider.BufferedDuration.Milliseconds <= 100;
        }

        public void Dispose()
        {
            wave_player?.Dispose();
        }
    }
}
