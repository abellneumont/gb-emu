namespace gbemu.ppu
{

    internal enum SpritePriority
    {
        Above, Normal
    }

    internal class Sprite
    {

        internal SpritePriority Priority { get; set; }

        internal int X { get; set; } = -8;

        internal int Y { get; set; } = -16;

        internal int Tile { get; set; } = 0;

        internal bool Palette { get; set; } = false;

        internal bool XFlip { get; set; } = false;

        internal bool YFlip { get; set; } = false;

    }
}
