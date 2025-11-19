using System;
using gbemu.interrupts;

namespace gbemu.ppu
{
    internal class PPU
    {
        public const int VRAM_SIZE = 0x2000;
        public const int OAM_RAM_SIZE = 0xA0;
        public const int CLOCK_CYCLES_FOR_SCANLINE = 456;
        public const int MAX_SPRITES_PER_SCANLINE = 10;
        public const int MAX_SPRITES_PER_FRAME = 40;

        private readonly Device device;
        private readonly byte[] vram_bank0 = new byte[VRAM_SIZE];
        private readonly byte[] vram_bank1 = new byte[VRAM_SIZE];
        private readonly byte[] oam_ram = new byte[OAM_RAM_SIZE];
        private byte vram_bank;
        private readonly byte[] frame_buffer = new byte[Device.SCREEN_HEIGHT * Device.SCREEN_WIDTH * 4];
        private readonly byte[] tile_buffer = new byte[Device.SCREEN_HEIGHT * Device.SCREEN_WIDTH];
        private int current_scanline_cycles;
        private int current_scanline;
        private int window_lines_skipped;
        private bool frame_uses_window;
        private bool scanline_used_window;
        private readonly byte[] scanline = new byte[Device.SCREEN_WIDTH * 4];
        private readonly ScanlineBgPriority[] scanline_priorities = new ScanlineBgPriority[Device.SCREEN_WIDTH];
        private readonly Sprite[] sprintes_line = new Sprite[10];
        private readonly DMGSpriteComparer sprite_converter = new DMGSpriteComparer();

        private readonly Sprite[] sprites = new Sprite[50];

        internal PPU(Device device)
        {
            this.device = device;

            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = new Sprite();
            }
        }

        internal byte GetVRAMBankRegister()
        {
            return vram_bank == 1 ? (byte)0xFF : (byte)0xFE;
        }

        internal void SetVRAMBankRegister(byte value)
        {
            if (device.device_mode == DeviceType.DMG) return;

            vram_bank = (byte)(value & 0x1);
        }

        internal byte GetVRAMByte(ushort address)
        {
            return vram_bank == 0 ? vram_bank0[address - 0x8000] : vram_bank1[address - 0x8000];
        }

        internal void WriteVRAMByte(ushort address, byte value)
        {
            if (vram_bank == 0) vram_bank0[address - 0x8000] = value;
            else vram_bank1[address - 0x8000] = value;
        }

        internal byte GetOAMByte(ushort address)
        {
            return oam_ram[address - 0xFE00];
        }

        internal void WriteOAMByte(ushort address, byte value)
        {
            var modAddress = address - 0xFE00;
            var spriteNumber = modAddress >> 2;
            oam_ram[modAddress] = value;

            switch (modAddress & 0x3)
            {
                case 0:
                    sprites[spriteNumber].Y = value - 16;
                    break;
                case 1:
                    sprites[spriteNumber].X = value - 8;
                    break;
                case 2:
                    sprites[spriteNumber].TileNumber = value;
                    break;
                case 3:
                    sprites[spriteNumber].SpriteToBgPriority = (value & 0x80) == 0x80 ? SpritePriority.BehindColors123 : SpritePriority.Above;
                    sprites[spriteNumber].YFlip = (value & 0x40) == 0x40;
                    sprites[spriteNumber].XFlip = (value & 0x20) == 0x20;
                    sprites[spriteNumber].UsePalette1 = (value & 0x10) == 0x10;
                    sprites[spriteNumber].VRAMBankNumber = (value & 0x8) >> 3;
                    sprites[spriteNumber].CGBPaletteNumber = value & 0x7;
                    break;
            }
        }

        internal byte[] GetCurrentFrame()
        {
            return frame_buffer;
        }

        internal void Step()
        {
            if (!device.ppu_registers.lcd_on) return;

            current_scanline_cycles = (current_scanline_cycles + 4) % CLOCK_CYCLES_FOR_SCANLINE;

            if (current_scanline_cycles == 0)
            {
                current_scanline = (current_scanline + 1) % 154;
            }

            var redrawScanline = SetLCDStatus(current_scanline, current_scanline_cycles);

            if (current_scanline < Device.SCREEN_HEIGHT && redrawScanline)
            {
                for (var ii = 0; ii < Device.SCREEN_WIDTH * 4; ii += 4)
                {
                    scanline_priorities[ii / 4] = ScanlineBgPriority.NORMAL;
                }

                if (device.ppu_registers.background_enabled)
                {
                    DrawBackground();
                }

                if (device.ppu_registers.sprites_enabled)
                {
                    DrawSprites();
                }

                Array.Copy(scanline, 0,
                    frame_buffer, current_scanline * Device.SCREEN_WIDTH * 4,
                    scanline.Length);
            }
        }

        private void DrawSprites()
        {
            var sprite_size = device.ppu_registers.large_sprites ? 16 : 8;
            var sprites_on_line = 0;

            Array.Clear(sprintes_line, 0, sprintes_line.Length);
            for (var spriteIndex = 0; spriteIndex < MAX_SPRITES_PER_FRAME; spriteIndex++)
            {
                if (sprites_on_line == MAX_SPRITES_PER_SCANLINE) break;

                var sprite = sprites[spriteIndex];

                if (current_scanline < sprite.Y || current_scanline >= sprite.Y + sprite_size) continue;

                sprintes_line[sprites_on_line] = sprite;

                sprites_on_line++;
            }

            if (device.device_mode == DeviceType.DMG)
            {
                Array.Sort(sprintes_line, sprite_converter);
            }

            for (var spriteIndex = sprintes_line.Length - 1; spriteIndex >= 0; spriteIndex--)
            {
                var sprite = sprintes_line[spriteIndex];
                if (sprite == null) continue;

                var tile_num = sprite_size == 8 ? sprite.TileNumber : sprite.TileNumber & 0xFE;
                var palette = sprite.UsePalette1
                    ? device.ppu_registers.ObjectPaletteData1
                    : device.ppu_registers.ObjectPaletteData0;

                var tile_address = sprite.YFlip ?
                    tile_num * 16 + (sprite_size - 1 - (current_scanline - sprite.Y)) * 2 :
                    tile_num * 16 + (current_scanline - sprite.Y) * 2;
                var b1 = sprite.VRAMBankNumber == 0
                    ? vram_bank0[tile_address]
                    : vram_bank1[tile_address];
                var b2 = sprite.VRAMBankNumber == 0
                    ? vram_bank0[tile_address + 1]
                    : vram_bank1[tile_address + 1];

                for (var x = 0; x < 8; x++)
                {
                    var pixel = sprite.X + x;
                    if (pixel < 0 || pixel >= Device.SCREEN_WIDTH) continue;
                    if (scanline_priorities[pixel] == ScanlineBgPriority.PRIORITY && !device.ppu_registers.IsCgbSpriteMasterPriorityOn) continue;

                    var color_bit = sprite.XFlip ? x : 7 - x;
                    var color_mask = 1 << color_bit;
                    var color_num =
                        ((b2 & color_mask) == color_mask ? 2 : 0) +
                        ((b1 & color_mask) == color_mask ? 1 : 0);

                    if (color_num == 0) continue;

                    var (r, g, b) = device.ppu_registers.GetColorFromNumberPalette(color_num, palette).BaseRgb();

                    if (!device.ppu_registers.IsCgbSpriteMasterPriorityOn &&
                        sprite.SpriteToBgPriority == SpritePriority.BehindColors123 &&
                        scanline_priorities[pixel] == ScanlineBgPriority.NORMAL) continue;

                    scanline[pixel * 4 + 3] = 0xFF; // Alpha channel
                    scanline[pixel * 4 + 2] = r;
                    scanline[pixel * 4 + 1] = g;
                    scanline[pixel * 4 + 0] = b;
                }
            }
        }

        private void DrawBackground()
        {
            for (var pixel = 0; pixel < Device.SCREEN_WIDTH; pixel++)
            {
                int x, y, tileMapAddress;
                if (UsingWindowForScanline && pixel >= device.ppu_registers.WindowX - 7)
                {
                    y = (current_scanline - device.ppu_registers.WindowY - window_lines_skipped) & 0xFF;
                    x = (pixel - device.ppu_registers.WindowX + 7) & 0xFF;
                    tileMapAddress = device.ppu_registers.window_tile_offset;
                    scanline_used_window = true;
                    frame_uses_window = true;
                }
                else
                {
                    y = (current_scanline + device.ppu_registers.ScrollY) & 0xFF;
                    x = (pixel + device.ppu_registers.ScrollX) & 0xFF;
                    tileMapAddress = device.ppu_registers.background_tile_offset;
                }

                var tile_row = y / 8 * 32;
                var tile_line = y % 8 * 2;
                var tile_column = x / 8;
                var tile_num_address = (ushort)((tileMapAddress + tile_row + tile_column) & 0xFFFF);

                var tileNumber = vram_bank0[tile_num_address - 0x8000];

                tile_buffer[current_scanline * Device.SCREEN_WIDTH + pixel] = tileNumber;

                var flagsByte = device.device_mode == DeviceType.CGB
                    ? vram_bank1[tile_num_address - 0x8000]
                    : 0x0;
                var vram_bank_num = (flagsByte & 0x8) >> 3;
                var x_flip = (flagsByte & 0x20) >> 5 != 0;
                var y_flip = (flagsByte & 0x40) >> 6 != 0;
                var bg_oam_priority = (flagsByte & 0x80) >> 7;

                var tile_data_address = y_flip
                    ? GetTileDataAddress(tileNumber) + 14 - tile_line
                    : GetTileDataAddress(tileNumber) + tile_line;

                var byte1 = vram_bank_num == 0
                    ? vram_bank0[tile_data_address & 0xFFFF - 0x8000]
                    : vram_bank1[tile_data_address & 0xFFFF - 0x8000];
                var byte2 = vram_bank_num == 0
                    ? vram_bank0[(tile_data_address + 1) & 0xFFFF - 0x8000]
                    : vram_bank1[(tile_data_address + 1) & 0xFFFF - 0x8000];

                var color_bit = x_flip ? x % 8 : 7 - x % 8;
                var color_mask = 1 << color_bit;
                var color_num =
                    ((byte2 & color_mask) == color_mask ? 2 : 0) +
                    ((byte1 & color_mask) == color_mask ? 1 : 0);

                var (r, g, b) =  device.ppu_registers.GetColorFromNumberPalette(color_num, device.ppu_registers.BackgroundPaletteData).BaseRgb();

                scanline[pixel * 4 + 3] = 0xFF;
                scanline[pixel * 4 + 2] = r;
                scanline[pixel * 4 + 1] = g;
                scanline[pixel * 4 + 0] = b;

                if (color_num == 0) scanline_priorities[pixel] = ScanlineBgPriority.COLOR;
                else if (bg_oam_priority == 1) scanline_priorities[pixel] = ScanlineBgPriority.PRIORITY;
                else scanline_priorities[pixel] = ScanlineBgPriority.NORMAL;
            }

            if (frame_uses_window && !scanline_used_window)
            {
                window_lines_skipped++;
            }
        }

        internal void TurnLCDOff()
        {
            current_scanline_cycles = 0x0;
        }

        private int Mode3CyclesOnCurrentLine()
        {
            var cycles = 172 + (device.ppu_registers.ScrollX & 0x7);
            var sprite_size = device.ppu_registers.large_sprites ? 16 : 8;
            var sprites_on_line = 0;
            foreach (var sprite in sprites)
            {
                if (sprites_on_line == MAX_SPRITES_PER_SCANLINE - 1) break;
                if (current_scanline < sprite.Y || current_scanline >= sprite.Y + sprite_size) continue;

                cycles += 6 + Math.Min(0, 5 - (sprite.X % 8));
                sprites_on_line++;
            }

            return cycles;
        }

        private bool SetLCDStatus(int current_scanline, int current_cycles_in_scanline)
        {
            var old_mode = device.ppu_registers.StatMode;

            if (current_scanline >= Device.SCREEN_HEIGHT)
            {
                device.ppu_registers.StatMode = StateMode.V_BLANK_PERIOD;
            }
            else
            {
                device.ppu_registers.StatMode = current_scanline_cycles switch
                {
                    _ when current_scanline_cycles < 76 => StateMode.OAM_RAM_PERIOD,
                    _ when current_scanline_cycles < 76 + Mode3CyclesOnCurrentLine() => StateMode.TRANSFERRING_DATA,
                    _ => StateMode.H_BLANK_PERIOD
                };
            }

            if (old_mode != device.ppu_registers.StatMode)
            {
                switch (device.ppu_registers.StatMode)
                {
                    case StateMode.H_BLANK_PERIOD:
                        return true;
                    case StateMode.V_BLANK_PERIOD:
                        device.renderer.HandleVBlankEvent(frame_buffer, device.timer_cycles);
                        device.interrupt_registers.RequestInterrupt(Interrupt.VERTICAL_BLANK);

                        window_lines_skipped = 0;
                        frame_uses_window = false;
                        return false;
                    case StateMode.OAM_RAM_PERIOD:
                        scanline_used_window = false;
                        return false;
                    case StateMode.TRANSFERRING_DATA:
                        return false;
                    default:
                        throw new ArgumentException();
                }
            }

            if (device.device_type == DeviceType.DMG)
            {
                device.ppu_registers.LYRegister = PPUTimingDetails.LYByLineAndClockDMG[current_scanline][current_cycles_in_scanline / 4];
            }
            else if (device.device_type == DeviceType.CGB && device.device_mode == DeviceType.DMG)
            {
                device.ppu_registers.LYRegister = PPUTimingDetails.LYByLineAndClockCGBDMGMode[current_scanline][current_cycles_in_scanline / 4];
            }
            else if (device.device_type == DeviceType.CGB && device.device_mode == DeviceType.CGB)
            {
                device.ppu_registers.LYRegister = PPUTimingDetails.LYByLineAndClockCGBMode[current_scanline][current_cycles_in_scanline / 4];
            }

            return false;
        }

        private bool UsingWindowForScanline => device.ppu_registers.window_enabled && current_scanline >= device.ppu_registers.WindowY;

        private ushort GetTileDataAddress(byte tile_num)
        {
            var tileset_address = device.ppu_registers.background_and_window_tile_offset;
            ushort tile_data_address;

            if (device.ppu_registers.signed_tile_data)
            {
                tile_data_address = (ushort)(tileset_address + ((sbyte)tile_num + 128) * 16);
            }
            else
            {
                tile_data_address = (ushort)(tileset_address + tile_num * 16);
            }
            return tile_data_address;
        }


        private enum ScanlineBgPriority
        {
            COLOR,
            PRIORITY,
            NORMAL
        }
    }
}
