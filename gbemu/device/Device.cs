using System.Collections.Generic;
using System.Threading;
using gbemu.ppu;
using gbemu.cpu;
using gbemu.cartridge;
using gbemu.interrupts;
using gbemu.controller;
using gbemu.sound;
using System.Threading.Tasks;

namespace gbemu
{
    public class Device
    {
        public const int SCREEN_WIDTH = 160;
        public const int SCREEN_HEIGHT = 144;
        public const int CYCLES_PER_SECOND = 4_194_304;

        public long timer_cycles;

        public readonly DeviceType device_mode;
        public readonly DeviceType device_type;
        internal readonly IRenderer renderer;
        internal readonly Bus bus;
        internal readonly CPU cpu;
        private readonly IEnumerator<int> cpu_enumerator;
        internal readonly ControlRegisters control_registers;
        internal readonly PPURegisters ppu_registers;
        internal readonly InterruptRegisters interrupt_registers;
        internal readonly Cartridge cartridge;
        internal readonly PPU ppu;
        internal readonly timers.Timer timer;
        internal readonly DMAController dma_controller;
        internal readonly ControllerHandler controller_handler;
		internal readonly APU apu;
        internal readonly ISoundOutput sound_output;
        internal bool apu_running = true;
        internal bool double_speed = false;
        private bool step_ppu_on_next_double_speed_cycle = true;
        public Device(Cartridge cartridge, DeviceType type, IRenderer renderer, ISoundOutput sound_output, byte[] boot_rom)
        {
            device_type = type;
            device_mode = DeviceType.DMG;

            this.renderer = renderer;

            interrupt_registers = new InterruptRegisters();
            control_registers = new ControlRegisters();
            ppu_registers = new PPURegisters(this);
            this.cartridge = cartridge;
            bus = new Bus(boot_rom, this);
            cpu = new CPU(this);
            cpu_enumerator = cpu.GetEnumerator();
            ppu = new PPU(this);
            timer = new timers.Timer(this);
            dma_controller = new DMAController(this);
            controller_handler = new ControllerHandler(this);
            this.sound_output = sound_output;
			apu = new APU(this);

            if (boot_rom == null)
            {
                SkipBootRom();
            }

            Task.Run(RunAPU);
        }

        public void SkipBootRom()
        {
            cpu.Registers.AF = 0x01B0;
            cpu.Registers.BC = 0x0013;
            cpu.Registers.DE = 0x00D8;
            cpu.Registers.HL = 0x014D;
            cpu.Registers.program_counter = 0x0100;
            cpu.Registers.stack_pointer = 0xFFFE;

            bus.WriteByte(0xFF05, 0);
            bus.WriteByte(0xFF06, 0);
            bus.WriteByte(0xFF07, 0);
            bus.WriteByte(0xFF40, 0x91);
            bus.WriteByte(0xFF42, 0);
            bus.WriteByte(0xFF43, 0);
            bus.WriteByte(0xFF45, 0);
            bus.WriteByte(0xFF47, 0xFC);
            bus.WriteByte(0xFF48, 0xFF);
            bus.WriteByte(0xFF49, 0xFF);
            bus.WriteByte(0xFF4A, 0);
            bus.WriteByte(0xFF4B, 0);
            bus.WriteByte(0xFF50, 0x1);

            timer.Reset(true);
        }

        public int Step()
        {
            cpu_enumerator.MoveNext();
            dma_controller.Step(4);

            if (!double_speed || step_ppu_on_next_double_speed_cycle)
            {
                ppu.Step();
                step_ppu_on_next_double_speed_cycle = false;
            }
            else
            {
                step_ppu_on_next_double_speed_cycle = true;
            }

            timer.Step();
            timer_cycles += 4;

            return 4;
        }

        public void HandleKeyDown(ControllerKey key)
        {
            controller_handler.KeyDown(key);
        }

        public void HandleKeyUp(ControllerKey key)
        {
            controller_handler.KeyUp(key);
        }

        private void RunAPU()
        {
            while (apu_running)
            {
                apu.Step(1);
            }
        }
    }
}
