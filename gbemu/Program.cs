using System.IO;
using CommandLine;
using gbemu.cartridge;
using gbemu.screen;

namespace gbemu
{
    public class CommandLineOptions(string romFilePath, string bootRomFilePath, int framesPerSecond, int pixelSize, DeviceType mode)
    {
        [Value(0, HelpText = "The full file path to a binary rom dump", MetaName = "RomFilePath", Required = true)]
        public string RomFilePath { get; } = romFilePath;

        [Option("bootRomFilePath", Required = false)]
        public string BootRomFilePath { get; } = bootRomFilePath;

        [Option('s', "framesPerSecondCap", Default = 60)]
        public int FramesPerSecond { get; } = framesPerSecond;

        [Option('p', "pixelSize", Default = 4)]
        public int PixelSize { get; } = pixelSize;

        [Option("mode", Default = DeviceType.DMG)]
        public DeviceType Mode { get; } = mode;
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                .MapResult(RunProgram, _ => -1);
        }

        private static int RunProgram(CommandLineOptions options)
        {
            byte[] romBytes = File.ReadAllBytes(options.RomFilePath);

            using var screen = new Screen(
                CartridgeFactory.CreateCartridge(romBytes),
                options.Mode,
                options.PixelSize,
                (options.BootRomFilePath == null) ? null : File.ReadAllBytes(options.BootRomFilePath),
                options.FramesPerSecond);
            screen.ExecuteProgram();

            return 0;
        }
    }
}
