using System;

namespace gbemu.ppu
{

    public enum Grayscale
    {
        WHITE = 0,
        LIGHT_GRAY = 1,
        DARK_GRAY = 2,
        BLACK = 3
    }

    public static class GrayscaleColor
    {

        public static (byte, byte, byte) toRGB(this Grayscale grayscale)
        {
            return grayscale switch
            {
                Grayscale.WHITE => (255, 255, 255),
                Grayscale.LIGHT_GRAY => (192, 192, 192),
                Grayscale.DARK_GRAY => (96, 96, 96),
                Grayscale.BLACK => (0, 0, 0),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

}
