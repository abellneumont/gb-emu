using gbemu;
using gbemu.cartridge;
using System.Threading;
using System.IO;
using System.Diagnostics;
using sdl2cs;

byte[] bytes = File.ReadAllBytes("C:\\Users\\masterdash5\\Downloads\\tetris.gb");
Cartridge cartridge = Cartridge.Create(bytes);
Bus bus = new Bus(cartridge);
var CPU = bus.cpu.Tick().GetEnumerator();
var stopwatch = new Stopwatch();

const int CLOCK_FRAME = 70256;
var delay = CLOCK_FRAME;
var fps = (int)((1.0 / 60) * 1000);

while (true)
{
    stopwatch.Start();

    CPU.MoveNext();
    bus.dma.Step(4);
    bus.ppu.Tick();
    bus.timer.Step();
    bus.cycles += 4;

    delay -= 4;

    if (delay < 0)
    {
        delay += CLOCK_FRAME;
        var sleep = fps - (stopwatch.ElapsedTicks / (double)Stopwatch.Frequency) * 1000;

        if (sleep > 0)
        {
            //SDL2.SDL_Delay((uint)sleep);
            Thread.Sleep((int)sleep);
        }

        stopwatch.Restart();
    }

}
