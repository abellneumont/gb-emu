namespace gbemu.ppu
{
    public interface IRenderer
    {
        public void HandleVBlankEvent(byte[] frameBuffer, long tCycles);
    }
}
