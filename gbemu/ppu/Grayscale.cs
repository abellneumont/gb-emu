using System;

namespace gbemu.ppu
{

    public enum Grayscale
    {
        WHITE = 0x0,
        LIGHT_GRAY = 0x1,
        DARK_GRAY = 0x2,
        BLACK = 0x3
    }

    public static class GrayscaleExtensions
    {
        public static (byte, byte, byte) GrayscaleWhite = (255, 255, 255);
        public static (byte, byte, byte) GrayscaleLightGray = (192, 192, 192);
        public static (byte, byte, byte) GrayscaleDarkGray = (96, 96, 96);
        public static (byte, byte, byte) GrayscaleBlack = (0, 0, 0);

        public static (byte, byte, byte) BaseRgb(this Grayscale grayscale) => grayscale switch
        {
            Grayscale.WHITE => GrayscaleWhite,
            Grayscale.LIGHT_GRAY => GrayscaleLightGray,
            Grayscale.DARK_GRAY => GrayscaleDarkGray,
            Grayscale.BLACK => GrayscaleBlack,
            _ => throw new ArgumentOutOfRangeException(nameof(grayscale), grayscale, null)
        };
    }
}