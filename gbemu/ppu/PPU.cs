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
        private readonly byte[] _frameBuffer = new byte[Device.SCREEN_HEIGHT * Device.SCREEN_WIDTH * 4];
        private readonly byte[] _tileBuffer = new byte[Device.SCREEN_HEIGHT * Device.SCREEN_WIDTH];
        private int current_scanline_cycles;
        private int current_scanline;
        private int window_lines_skipped;
        private bool frame_uses_window;
        private bool scanline_used_window;
        private readonly byte[] scanline = new byte[Device.SCREEN_WIDTH * 4];
        private readonly ScanlineBgPriority[] scanline_priorities = new ScanlineBgPriority[Device.SCREEN_WIDTH];
        private readonly Sprite[] sprintes_line = new Sprite[10];
        private readonly DMGSpriteComparer sprite_converter = new DMGSpriteComparer();

        private readonly Sprite[] _sprites = {
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(),
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(),
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(),
            new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite(), new Sprite()
        };

        internal PPU(Device device)
        {
            this.device = device;
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
                    _sprites[spriteNumber].Y = value - 16;
                    break;
                case 1:
                    _sprites[spriteNumber].X = value - 8;
                    break;
                case 2:
                    _sprites[spriteNumber].TileNumber = value;
                    break;
                case 3:
                    _sprites[spriteNumber].SpriteToBgPriority = (value & 0x80) == 0x80 ? SpritePriority.BehindColors123 : SpritePriority.Above;
                    _sprites[spriteNumber].YFlip = (value & 0x40) == 0x40;
                    _sprites[spriteNumber].XFlip = (value & 0x20) == 0x20;
                    _sprites[spriteNumber].UsePalette1 = (value & 0x10) == 0x10;
                    _sprites[spriteNumber].VRAMBankNumber = (value & 0x8) >> 3;
                    _sprites[spriteNumber].CGBPaletteNumber = value & 0x7;
                    break;
            }
        }

        internal byte[] GetCurrentFrame()
        {
            return _frameBuffer;
        }

        internal void Step()
        {
            if (!device.ppu_registers.IsLcdOn) return;

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

                if (device.ppu_registers.IsBackgroundEnabled)
                {
                    DrawBackground();
                }

                if (device.ppu_registers.AreSpritesEnabled)
                {
                    DrawSprites();
                }

                Array.Copy(scanline, 0,
                    _frameBuffer, current_scanline * Device.SCREEN_WIDTH * 4,
                    scanline.Length);
            }
        }

        private void DrawSprites()
        {
            var spriteSize = device.ppu_registers.LargeSprites ? 16 : 8;
            var spritesFoundOnLine = 0;

            Array.Clear(sprintes_line, 0, sprintes_line.Length);
            for (var spriteIndex = 0; spriteIndex < MAX_SPRITES_PER_FRAME; spriteIndex++)
            {
                if (spritesFoundOnLine == MAX_SPRITES_PER_SCANLINE) break;

                var sprite = _sprites[spriteIndex];

                if (current_scanline < sprite.Y || current_scanline >= sprite.Y + spriteSize) continue;

                sprintes_line[spritesFoundOnLine] = sprite;

                spritesFoundOnLine++;
            }

            if (device.device_mode == DeviceType.DMG)
            {
                Array.Sort(sprintes_line, sprite_converter);
            }

            for (var spriteIndex = sprintes_line.Length - 1; spriteIndex >= 0; spriteIndex--)
            {
                var sprite = sprintes_line[spriteIndex];
                if (sprite == null) continue;

                var tileNumber = spriteSize == 8 ? sprite.TileNumber : sprite.TileNumber & 0xFE;
                var palette = sprite.UsePalette1
                    ? device.ppu_registers.ObjectPaletteData1
                    : device.ppu_registers.ObjectPaletteData0;

                var tileAddress = sprite.YFlip ?
                    tileNumber * 16 + (spriteSize - 1 - (current_scanline - sprite.Y)) * 2 :
                    tileNumber * 16 + (current_scanline - sprite.Y) * 2;
                var b1 = sprite.VRAMBankNumber == 0
                    ? vram_bank0[tileAddress]
                    : vram_bank1[tileAddress];
                var b2 = sprite.VRAMBankNumber == 0
                    ? vram_bank0[tileAddress + 1]
                    : vram_bank1[tileAddress + 1];

                for (var x = 0; x < 8; x++)
                {
                    var pixel = sprite.X + x;
                    if (pixel < 0 || pixel >= Device.SCREEN_WIDTH) continue;
                    if (scanline_priorities[pixel] == ScanlineBgPriority.PRIORITY && !device.ppu_registers.IsCgbSpriteMasterPriorityOn) continue;

                    var colorBit = sprite.XFlip ? x : 7 - x;
                    var colorBitMask = 1 << colorBit;
                    var colorNumber =
                        ((b2 & colorBitMask) == colorBitMask ? 2 : 0) +
                        ((b1 & colorBitMask) == colorBitMask ? 1 : 0);

                    if (colorNumber == 0) continue;

                    var (r, g, b) = device.ppu_registers.GetColorFromNumberPalette(colorNumber, palette).BaseRgb();

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
                int xPos, yPos, tileMapAddress;
                if (UsingWindowForScanline && pixel >= device.ppu_registers.WindowX - 7)
                {
                    yPos = (current_scanline - device.ppu_registers.WindowY - window_lines_skipped) & 0xFF;
                    xPos = (pixel - device.ppu_registers.WindowX + 7) & 0xFF;
                    tileMapAddress = device.ppu_registers.WindowTileMapOffset;
                    scanline_used_window = true;
                    frame_uses_window = true;
                }
                else
                {
                    yPos = (current_scanline + device.ppu_registers.ScrollY) & 0xFF;
                    xPos = (pixel + device.ppu_registers.ScrollX) & 0xFF;
                    tileMapAddress = device.ppu_registers.BackgroundTileMapOffset;
                }

                var tileRow = yPos / 8 * 32;
                var tileLine = yPos % 8 * 2;
                var tileCol = xPos / 8;
                var tileNumberAddress = (ushort)((tileMapAddress + tileRow + tileCol) & 0xFFFF);

                var tileNumber = vram_bank0[tileNumberAddress - 0x8000];

                _tileBuffer[current_scanline * Device.SCREEN_WIDTH + pixel] = tileNumber;

                var flagsByte = device.device_mode == DeviceType.CGB
                    ? vram_bank1[tileNumberAddress - 0x8000]
                    : 0x0;
                var paletteNumber = flagsByte & 0x7;
                var vramBankNumber = (flagsByte & 0x8) >> 3;
                var xFlip = (flagsByte & 0x20) >> 5 != 0;
                var yFlip = (flagsByte & 0x40) >> 6 != 0;
                var bgToOamPriority = (flagsByte & 0x80) >> 7;

                var tileDataAddress = yFlip
                    ? GetTileDataAddress(tileNumber) + 14 - tileLine
                    : GetTileDataAddress(tileNumber) + tileLine;

                var byte1 = vramBankNumber == 0
                    ? vram_bank0[tileDataAddress & 0xFFFF - 0x8000]
                    : vram_bank1[tileDataAddress & 0xFFFF - 0x8000];
                var byte2 = vramBankNumber == 0
                    ? vram_bank0[(tileDataAddress + 1) & 0xFFFF - 0x8000]
                    : vram_bank1[(tileDataAddress + 1) & 0xFFFF - 0x8000];

                var colorBit = xFlip ? xPos % 8 : 7 - xPos % 8;
                var colorBitMask = 1 << colorBit;
                var colorNumber =
                    ((byte2 & colorBitMask) == colorBitMask ? 2 : 0) +
                    ((byte1 & colorBitMask) == colorBitMask ? 1 : 0);

                var (r, g, b) =  device.ppu_registers.GetColorFromNumberPalette(colorNumber, device.ppu_registers.BackgroundPaletteData).BaseRgb();

                scanline[pixel * 4 + 3] = 0xFF;
                scanline[pixel * 4 + 2] = r;
                scanline[pixel * 4 + 1] = g;
                scanline[pixel * 4 + 0] = b;

                if (colorNumber == 0) scanline_priorities[pixel] = ScanlineBgPriority.COLOR;
                else if (bgToOamPriority == 1) scanline_priorities[pixel] = ScanlineBgPriority.PRIORITY;
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
            var spriteSize = device.ppu_registers.LargeSprites ? 16 : 8;
            var spritesFoundOnLine = 0;
            foreach (var sprite in _sprites)
            {
                if (spritesFoundOnLine == MAX_SPRITES_PER_SCANLINE - 1) break;
                if (current_scanline < sprite.Y || current_scanline >= sprite.Y + spriteSize) continue;

                cycles += 6 + Math.Min(0, 5 - (sprite.X % 8));
                spritesFoundOnLine++;
            }

            return cycles;
        }

        private bool SetLCDStatus(int currentScanLine, int currentTCyclesInScanline)
        {
            var oldMode = device.ppu_registers.StatMode;

            if (currentScanLine >= Device.SCREEN_HEIGHT)
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

            if (oldMode != device.ppu_registers.StatMode)
            {
                switch (device.ppu_registers.StatMode)
                {
                    case StateMode.H_BLANK_PERIOD:
                        return true;
                    case StateMode.V_BLANK_PERIOD:
                        device.renderer.HandleVBlankEvent(_frameBuffer, device.timer_cycles);
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
                device.ppu_registers.LYRegister = PPUTimingDetails.LYByLineAndClockDMG[currentScanLine][currentTCyclesInScanline / 4];
            }
            else if (device.device_type == DeviceType.CGB && device.device_mode == DeviceType.DMG)
            {
                device.ppu_registers.LYRegister = PPUTimingDetails.LYByLineAndClockCGBDMGMode[currentScanLine][currentTCyclesInScanline / 4];
            }
            else if (device.device_type == DeviceType.CGB && device.device_mode == DeviceType.CGB)
            {
                device.ppu_registers.LYRegister = PPUTimingDetails.LYByLineAndClockCGBMode[currentScanLine][currentTCyclesInScanline / 4];
            }

            return false;
        }

        private bool UsingWindowForScanline => device.ppu_registers.IsWindowEnabled && current_scanline >= device.ppu_registers.WindowY;

        private ushort GetTileDataAddress(byte tileNumber)
        {
            var tilesetAddress = device.ppu_registers.BackgroundAndWindowTilesetOffset;
            ushort tileDataAddress;
            if (device.ppu_registers.UsingSignedByteForTileData)
            {
                tileDataAddress = (ushort)(tilesetAddress + ((sbyte)tileNumber + 128) * 16);
            }
            else
            {
                tileDataAddress = (ushort)(tilesetAddress + tileNumber * 16);
            }
            return tileDataAddress;
        }


        private enum ScanlineBgPriority
        {
            COLOR,
            PRIORITY,
            NORMAL
        }
    }
}
