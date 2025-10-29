using System;

namespace gbemu.ppu
{
    internal class PPU
    {

        public const int SCANLINE_CLOCK_CYCLES = 456;
        public const int MAX_SCANLINES = 154;
        public const int MAX_SPRITES_PER_SCANELINE = 10;
        public const int MAX_SPRITES_PER_FRAME = 40;

        internal Bus bus;
        internal PPURegister register;

        private readonly byte[] framebuffer = new byte[Screen.WIDTH * Screen.HEIGHT * 4];
        private readonly byte[] tilebuffer = new byte[Screen.WIDTH * Screen.HEIGHT];
        private readonly Sprite[] sprites = new Sprite[40];
        private readonly Sprite[] sprites_line = new Sprite[10];

        private readonly byte[] scanline = new byte[Screen.WIDTH * 4];
        private readonly SpritePriority[] priorities = new SpritePriority[Screen.WIDTH];

        private readonly byte[] oam = new byte[0xa0];
        private readonly byte[] vram = new byte[0x2000];

        private int scanline_current_cycles, current_scanline, window_lines_skipped;
        private bool frame_uses_window, scanline_used_window;

        private bool UsingWindowForScanline => register.WindowEnabled && current_scanline >= register.WindowY;

        public PPU(Bus bus)
        {
            this.bus = bus;
            this.register = new PPURegister(this);

            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = new Sprite();
            }
        }

        public byte[] GetFramebuffer()
        {
            return framebuffer;
        }

        public void DisableLCD()
        {
            scanline_current_cycles = 0;
        }

        internal byte ReadVRAM(ushort address)
        {
            return vram[address];
        }

        internal void WriteVRAM(ushort address, byte value)
        {
            vram[address] = value;
        }

        internal byte ReadOAM(ushort address)
        {
            return oam[address];
        }

        internal void WriteOAM(ushort address, byte value)
        {
            oam[address] = value;

            int spriteId = address >> 2;

            switch (address & 0x3)
            {
                case 0:
                    sprites[spriteId].Y = value - 16;
                    break;
                case 1:
                    sprites[spriteId].X = value - 8;
                    break;
                case 2:
                    sprites[spriteId].Tile = value;
                    break;
                case 3:
                    sprites[spriteId].Priority = (value & 0x80) == 0x80 ? SpritePriority.Normal : SpritePriority.Above;
                    sprites[spriteId].YFlip = (value & 0x40) == 0x40;
                    sprites[spriteId].XFlip = (value & 0x20) == 0x20;
                    sprites[spriteId].Palette = (value & 0x10) == 0x10;
                    break;
            }
        }

        private int Mode3CyclesOnLine()
        {
            int cycles = 172 + (register.ScrollX & 0x7);
            int sprite_size = register.LargeSprites ? 16 : 8;
            int spritesOnLine = 0;

            foreach (Sprite sprite in sprites)
            {
                if (spritesOnLine >= MAX_SPRITES_PER_SCANELINE - 1)
                    break;

                if (current_scanline < sprite.Y || current_scanline >= sprite.Y + sprite_size)
                    continue;

                cycles += 6 + Math.Min(0, 5 - (sprite.X % 8));
                spritesOnLine++;
            }

            return cycles;
        }

        private void DrawSprites()
        {
            int sprite_size = register.LargeSprites ? 16 : 8;
            int spritesOnLine = 0;

            Array.Clear(sprites_line, 0, sprites_line.Length);

            for (int i = 0; i < MAX_SPRITES_PER_FRAME; i++)
            {
                if (spritesOnLine >= MAX_SPRITES_PER_SCANELINE)
                    break;

                Sprite sprite = sprites[i];

                if (current_scanline < sprite.Y || current_scanline >= sprite.Y + sprite_size)
                    continue;

                sprites_line[spritesOnLine] = sprite;
                spritesOnLine++;
            }

            Array.Sort(sprites_line, (x, y) =>
            {
                if (x == null && y == null)
                    return 0;

                if (x == null)
                    return 1;

                if (y == null)
                    return -1;

                if (x.X == y.X)
                    return 0;

                if (x.X < y.X)
                    return -1;

                return 1;
            });

            for (int i = sprites_line.Length - 1; i >= 0; i--)
            {
                Sprite sprite = sprites_line[i];

                if (sprite == null)
                    continue;

                int tile = sprite_size == 8 ? sprite.Tile : sprite.Tile & 0xfe;
                byte palette = sprite.Palette ? register.PaletteData[1] : register.PaletteData[0];
                int tile_address = sprite.YFlip ?
                    tile * 16 + (sprite_size - 1 - (current_scanline - sprite.Y)) * 2 :
                    tile * 16 + (current_scanline - sprite.Y) * 2;
                byte first = vram[tile];
                byte second = vram[tile + 1];

                for (int x = 0; x < 8; x++)
                {
                    int pixel = sprite.X + x;

                    if (pixel < 0 || pixel >= Screen.WIDTH)
                        continue;

                    if (priorities[pixel] == SpritePriority.Above)
                        continue;

                    int color = sprite.XFlip ? x : 7 - x;
                    int colorMask = 1 << color;
                    int colorNumber =
                        ((first & colorMask) == colorMask ? 2 : 0) +
                        ((second & colorMask) == colorMask ? 1 : 0);

                    if (colorNumber == 0)
                        continue;

                    var (r, g, b) = register.GetColor(colorNumber, palette).toRGB();

                    if (sprite.Priority == SpritePriority.Normal && priorities[pixel] == SpritePriority.Normal)
                        continue;

                    scanline[pixel * 4 + 3] = 0xff;
                    scanline[pixel * 4 + 2] = r;
                    scanline[pixel * 4 + 1] = g;
                    scanline[pixel * 4] = b;
                }
            }
        }

        private void DrawBackground()
        {
            for (int i = 0; i < Screen.WIDTH; i++)
            {
                int x, y, tile_map;

                if (UsingWindowForScanline && i >= register.WindowX - 7)
                {
                    y = (current_scanline - register.WindowY - window_lines_skipped) & 0xff;
                    x = (i - register.WindowX + 7) & 0xff;
                    tile_map = register.WindowTileMapOffset;
                    scanline_used_window = true;
                    frame_uses_window = true;
                }
                else
                {
                    y = (current_scanline + register.ScrollY) & 0xff;
                    x = (i + register.ScrollX) & 0xff;
                    tile_map = register.BackgroundTileMapOffset;
                }

                int tile_row = y / 8 * 32;
                int tile_line = y % 8 * 2;
                int tile_column = x / 8;
                int tile_address = (ushort)((tile_map + tile_row + tile_column) & 0xffff);
                byte tile_number = vram[tile_address - 0x8000];

                tilebuffer[current_scanline * Screen.WIDTH + i] = tile_number;

                int tile_data_address = GetTileAddress(tile_number) + tile_line;
                byte first = vram[tile_data_address & 0xffff - 0x8000];
                byte second = vram[(tile_data_address + 1) & 0xfffd - 0x8000];

                int color = 7 - x % 8;
                int color_mask = 1 << color;
                int color_number =
                    ((first & color_mask) == color_mask ? 2 : 0) +
                    ((second & color_mask) == color_mask ? 1 : 0);

                var (r, g, b) = register.GetColor(color_number, register.BackgroundPalette).toRGB();

                scanline[i * 4 + 3] = 0xff;
                scanline[i * 4 + 2] = r;
                scanline[i * 4 + 1] = g;
                scanline[i * 4] = b;

                if (color_number == 0)
                    priorities[i] = SpritePriority.Above;
                else
                    priorities[i] = SpritePriority.Normal;
            }

            if (frame_uses_window && !scanline_used_window)
                window_lines_skipped++;
        }

        private bool SetState()
        {
            PPUState oldState = register.StateMode;
            int oldScanline = current_scanline;
            int oldCycle = scanline_current_cycles;

            if (current_scanline >= Screen.HEIGHT)
                register.StateMode = PPUState.V_BLANK_PERIOD;
            else
            {
                register.StateMode = scanline_current_cycles switch
                {
                    _ when scanline_current_cycles < 76 => PPUState.OAM_RAM_PERIOD,
                    _ when scanline_current_cycles < 76 + Mode3CyclesOnLine() => PPUState.TRANSFERRING_DATA,
                    _ => PPUState.H_BLANK_PERIOD
                };
            }

            if (oldState != register.StateMode)
            {
                switch (register.StateMode)
                {
                    case PPUState.H_BLANK_PERIOD:
                        return true;
                    case PPUState.V_BLANK_PERIOD:
                        bus.screen.VBlankEvent(framebuffer);
                        bus.RequestInterrupt(InterruptType.VERTICAL_BLANK);
                        window_lines_skipped = 0;
                        frame_uses_window = false;
                        return false;
                    case PPUState.OAM_RAM_PERIOD:
                        scanline_used_window = false;
                        return false;
                    case PPUState.TRANSFERRING_DATA:
                        return false;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            register.LYRegister = PPUTimings.PPU_CLOCK_TIMINGS[current_scanline][scanline_current_cycles / 4];
            return false;
        }

        private ushort GetTileAddress(byte tile)
        {
            int offset = register.BackgroundWindowTileMapOffset;
            ushort address;

            if (register.SignedTileData)
                address = (ushort)(offset + ((sbyte)tile + 128) * 16);
            else
                address = (ushort)(offset + tile * 16);

            return address;
        }

        public void Tick()
        {
            if (!register.LcdOn)
                return;

            scanline_current_cycles = (scanline_current_cycles + 4) % SCANLINE_CLOCK_CYCLES;

            if (scanline_current_cycles == 0)
                current_scanline = (current_scanline + 1) % MAX_SCANLINES;

            bool draw_scanline = SetState();

            if (current_scanline < Screen.HEIGHT * 4 && draw_scanline)
            {
                for (int i = 0; i < Screen.WIDTH * 4; i++)
                {
                    priorities[i / 4] = SpritePriority.Normal;
                }

                if (register.BackgroundEnabled)
                {
                    DrawBackground();
                }

                if (register.SpritesEnabled)
                {
                    DrawSprites();
                }

                Array.Copy(scanline, 0, framebuffer, current_scanline * Screen.WIDTH * 4, scanline.Length);
            }
        }

    }
}
