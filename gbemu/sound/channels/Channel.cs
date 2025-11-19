namespace gbemu.sound
{
    internal abstract class Channel
    {
        protected Channel(Device device)
        {
            Device = device;
        }

        protected Device Device;

        internal bool Enabled { get; set; }

        protected abstract int BaseSoundLength { get; }

        internal int SoundLength { get; set; }

        internal bool UseSoundLength { get; set; }

        internal abstract int GetOutputVolume();

        internal virtual void Reset()
        {
            Enabled = false;
            SoundLength = 0;
            UseSoundLength = false;
        }

        internal abstract void Step();

        internal virtual void Trigger()
        {
            Enabled = true;
            if (SoundLength == 0)
            {
                SoundLength = 0x3F;
            }
        }

        internal void StepLength()
        {
            if (UseSoundLength && SoundLength > 0)
            {
                SoundLength--;

                if (SoundLength == 0)
                {
                    Enabled = false;
                }
            }
        }

        internal abstract void SkipBootRom();
    }
}
