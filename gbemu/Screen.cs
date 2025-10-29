using sdl2cs;
using System;

namespace gbemu
{
    internal class Screen
    {

        public const int WIDTH = 160, HEIGHT = 144;
        private readonly IntPtr window, renderer, texture;

        internal Screen(int scale)
        {
            SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO);
            SDL2.SDL_CreateWindowAndRenderer(
                WIDTH * scale,
                HEIGHT * scale,
                0,
                out window,
                out renderer
            );
            SDL2.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
            SDL2.SDL_RenderClear(renderer);
            SDL2.SDL_SetWindowTitle(window, "GB Emu");

            this.texture = SDL2.SDL_CreateTexture(
                renderer: renderer,
                format: SDL2.SDL_PIXELFORMAT_ARGB8888,
                access: (int)SDL2.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                w: WIDTH,
                h: HEIGHT);
        }

        public void VBlankEvent(byte[] framebuffer)
        {
            unsafe // Allows pointers
            {
                fixed (byte* ptr = framebuffer)
                {
                    SDL2.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)ptr, WIDTH * 4);
                }
            }

            SDL2.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
            SDL2.SDL_RenderPresent(renderer);
        }

        public void Close()
        {
            SDL2.SDL_DestroyRenderer(renderer);
            SDL2.SDL_DestroyWindow(window);
            SDL2.SDL_DestroyTexture(texture);
            SDL2.SDL_Quit();
        }
    }
}
