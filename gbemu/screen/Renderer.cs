using System;
using gbemu.ppu;
using sdl2cs;

namespace gbemu.screen
{
    public class Renderer : IRenderer, IDisposable
    {
        private readonly DeviceType device_type;
        private readonly IntPtr renderer;
        private readonly IntPtr texture;

        public Renderer(IntPtr renderer, DeviceType deviceType)
        {
            this.renderer = renderer;
            device_type = deviceType;

            texture = SDL2.SDL_CreateTexture(
                renderer: this.renderer,
                format: SDL2.SDL_PIXELFORMAT_ARGB8888,
                access: (int)SDL2.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                w: Device.SCREEN_WIDTH,
                h: Device.SCREEN_HEIGHT);
        }

        public void HandleVBlankEvent(byte[] frameBuffer, long tCycles)
        {
            unsafe // Needed for SDL2 pointers
            {
                fixed (byte* p = frameBuffer)
                {
                    SDL2.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)p, Device.SCREEN_WIDTH * 4);
                }
            }

            SDL2.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
            SDL2.SDL_RenderPresent(renderer);
        }

        public void Dispose()
        {
            SDL2.SDL_DestroyTexture(texture);
        }
    }
}
