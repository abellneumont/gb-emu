using System;

namespace gbemu.controller
{
    public enum ControllerKey
    {
        UP, DOWN, LEFT, RIGHT,
        A, B, SELECT, START
    }

    public enum ControllerRegisterMode
    {
        NONE = 0x3,
        DPAD = 0x2,
        BUTTONS = 0x1,
        BOTH = 0x0
    }

    public static class ControllerKeyExtensions
    {
        public static byte BitMask(this ControllerKey key) => key switch
        {
            _ when key == ControllerKey.RIGHT || key == ControllerKey.A => 0b11111110,
            _ when key == ControllerKey.LEFT || key == ControllerKey.B => 0b11111101,
            _ when key == ControllerKey.UP || key == ControllerKey.SELECT => 0b11111011,
            _ when key == ControllerKey.DOWN || key == ControllerKey.START => 0b11110111,
            _ => throw new ArgumentOutOfRangeException()
        };

        public static ControllerRegisterMode RegisterMode(this ControllerKey key) => key switch
        {
            ControllerKey.RIGHT => ControllerRegisterMode.DPAD,
            ControllerKey.LEFT => ControllerRegisterMode.DPAD,
            ControllerKey.UP => ControllerRegisterMode.DPAD,
            ControllerKey.DOWN => ControllerRegisterMode.DPAD,
            ControllerKey.A => ControllerRegisterMode.BUTTONS,
            ControllerKey.B => ControllerRegisterMode.BUTTONS,
            ControllerKey.SELECT => ControllerRegisterMode.BUTTONS,
            ControllerKey.START => ControllerRegisterMode.BUTTONS,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}