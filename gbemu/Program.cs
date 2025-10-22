using gbemu;

byte[] bytes = File.ReadAllBytes("C:\\Users\\masterdash5\\Downloads\\tetris.gb");
Cartridge cartridge = Cartridge.Create(bytes);
Bus bus = new Bus(cartridge);

while (true) {
    Thread.Sleep(100);
    bus.cpu.Tick();
}