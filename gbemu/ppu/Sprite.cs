namespace gbemu.ppu
{
    internal class Sprite
    {
        internal SpritePriority SpriteToBgPriority { get; set; }

        internal int Y { get; set; } = -16;

        internal int X { get; set; } = -8;

        internal int TileNumber { get; set; } = 0;

        internal bool YFlip { get; set; } = false;

        internal bool XFlip { get; set; } = false;

        internal bool UsePalette1 { get; set; } = false;

        internal int VRAMBankNumber { get; set; } = 0;

        internal int CGBPaletteNumber { get; set; } = 0;
    }
}
