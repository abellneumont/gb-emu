namespace gbemu.sound
{
    public interface ISoundOutput
    {
        int AudioFrequency { get; }

        public bool IsBufferLow();

        public void PlaySoundByte(int left, int right);
    }
}
