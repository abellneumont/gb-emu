using System;
using System.Collections.Generic;
using System.Diagnostics;
using gbemu.cartridge;
using gbemu.controller;
using gbemu.sound;
using sdl2cs;

namespace gbemu.screen
{
    internal class Screen : IDisposable
    {
        private readonly IntPtr window;
        private readonly IntPtr renderer;
        private readonly Device device;
        private readonly Stopwatch stopwatch = new();
        private readonly int ms_per_frame;
        private bool quit;
        private const int CLOCK_PER_FRAME = 70256;
        private const int CLOCK_PER_INPUT = 35000;
        private int input_cooldown = CLOCK_PER_INPUT;
        private int delay_cooldown = CLOCK_PER_FRAME;
        private NAudioSoundOutput sound_output;

        private readonly Dictionary<SDL2.SDL_Keycode, ControllerKey> key_map = new Dictionary<SDL2.SDL_Keycode, ControllerKey>
        {
            { SDL2.SDL_Keycode.SDLK_RIGHT, ControllerKey.RIGHT },
            { SDL2.SDL_Keycode.SDLK_LEFT, ControllerKey.LEFT },
            { SDL2.SDL_Keycode.SDLK_UP, ControllerKey.UP },
            { SDL2.SDL_Keycode.SDLK_DOWN, ControllerKey.DOWN },
            { SDL2.SDL_Keycode.SDLK_z, ControllerKey.A },
            { SDL2.SDL_Keycode.SDLK_x, ControllerKey.B },
            { SDL2.SDL_Keycode.SDLK_RETURN, ControllerKey.START },
            { SDL2.SDL_Keycode.SDLK_RSHIFT, ControllerKey.SELECT },
        };

        internal Screen(Cartridge cartridge, DeviceType mode, int pixel_size, byte[] boot_rom, int fps)
        {
            SDL2.SDL_Init(SDL2.SDL_INIT_VIDEO | SDL2.SDL_INIT_AUDIO);

            SDL2.SDL_CreateWindowAndRenderer(
                Device.SCREEN_WIDTH * pixel_size,
                Device.SCREEN_HEIGHT * pixel_size,
                SDL2.SDL_WindowFlags.SDL_WINDOW_RESIZABLE,
                out window,
                out this.renderer);
            SDL2.SDL_SetRenderDrawColor(this.renderer, 0, 0, 0, 255);
            SDL2.SDL_RenderClear(this.renderer);
            SDL2.SDL_SetWindowTitle(window, "GB Emu");
            SDL2.SDL_SetHint(SDL2.SDL_HINT_RENDER_SCALE_QUALITY, "0");

            var renderer = new Renderer(this.renderer, mode);
            ms_per_frame = (int)(1.0 / fps * 1000);
            sound_output = new NAudioSoundOutput();
            device = new Device(cartridge, DeviceType.DMG, renderer, sound_output, boot_rom);
        }

        public void ExecuteProgram()
        {
            stopwatch.Start();

            while (!quit)
            {
                var clocks = device.Step();
                input_cooldown -= clocks;

                if (input_cooldown < 0)
                {
                    input_cooldown += CLOCK_PER_INPUT;
                    CheckForInput();
                }

                delay_cooldown -= clocks;

                if (delay_cooldown < 0)
                {
                    delay_cooldown += CLOCK_PER_FRAME;
                    var msToSleep = ms_per_frame - stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000;

                    if (msToSleep > 0)
                    {
                        SDL2.SDL_Delay((uint)msToSleep);
                    }

                    stopwatch.Restart();
                }
            }
        }

        private void CheckForInput()
        {
            if (SDL2.SDL_PollEvent(out var e) != 0)
            {
                switch (e.type)
                {
                    case SDL2.SDL_EventType.SDL_QUIT:
                        quit = true;
                        break;
                    case SDL2.SDL_EventType.SDL_KEYUP:
                    {
                        if (key_map.TryGetValue(e.key.keysym.sym, out ControllerKey key))
                            device.HandleKeyUp(key);
                        break;
                    }
                    case SDL2.SDL_EventType.SDL_KEYDOWN:
                    {
                        if (key_map.TryGetValue(e.key.keysym.sym, out ControllerKey key))
                            device.HandleKeyDown(key);
                        break;
                    }
                }
            }
        }


        public void Dispose()
        {
            sound_output?.Dispose();
            SDL2.SDL_DestroyRenderer(renderer);
            SDL2.SDL_DestroyWindow(window);
            SDL2.SDL_Quit();
        }
    }
}
