using gbemu.cartridge;
using gbemu.cpu;
using gbemu.ppu;
using System;

namespace gbemu
{

    internal class Bus
    {

        internal CPU cpu;
        internal PPU ppu;
        internal DMAController dma;
        internal Memory memory;
        internal Timer timer;
        internal Cartridge cartridge;
        internal Screen screen;

        internal long cycles;
        private byte interrupt_flags = 0xe0;
        private bool disable_rom;

        public Bus(Cartridge cartridge)
        {
            this.cartridge = cartridge;
            this.cpu = new CPU(this);
            this.ppu = new PPU(this);
            this.dma = new DMAController(this);
            this.memory = new Memory();
            this.timer = new Timer(this);
            this.screen = new Screen(4);

            Init();
        }

        public void Init()
        {
            cpu.register.AF = 0x01b0;
            cpu.register.BC = 0x0013;
            cpu.register.DE = 0x00d8;
            cpu.register.HL = 0x014d;
            cpu.register.program_counter = 0x0100;
            cpu.register.stack_pointer = 0xfffe;

            Write(0xFF05, 0);
            Write(0xFF06, 0);
            Write(0xFF07, 0);
            Write(0xFF40, 0x91);
            Write(0xFF42, 0);
            Write(0xFF43, 0);
            Write(0xFF45, 0);
            Write(0xFF47, 0xFC);
            Write(0xFF48, 0xFF);
            Write(0xFF49, 0xFF);
            Write(0xFF4A, 0);
            Write(0xFF4B, 0);
            Write(0xFF50, 0x1); // Turn off boot ROM
        }

        internal bool InterruptsGloballyEnabled { get; set; }

        internal byte InterruptFlags
        {
            get => interrupt_flags;
            set => interrupt_flags = (byte)(0xe0 | value);
        }

        internal byte InterruptEnable { get; set; }

        internal void RequestInterrupt(InterruptType type)
        {
            interrupt_flags = (byte)(interrupt_flags | type.Mask());
        }

        internal void ResetInterrupt(InterruptType type)
        {
            interrupt_flags = (byte)(interrupt_flags & ~type.Mask());
        }

        public byte Read(ushort address)
        {
            byte read = Read2(address);
            Console.WriteLine("READ " + address + " - " + read);
            return read;
        }

        public byte Read2(ushort address)
        {
            //Console.WriteLine("READ " + address);

            if (address <= 0x7fff)
                return cartridge.ReadRom(address);

            if (address <= 0x9fff)
            {
                if (ppu.register.StateMode == PPUState.TRANSFERRING_DATA)
                    return 0xff;

                return ppu.ReadVRAM((ushort)(address - 0x8000));
            }

            if (address <= 0xbfff)
                return cartridge.ReadRam(address);

            if (address <= 0xdfff)
                return memory.ReadWide((ushort)(address - 0xc000));

            if (address <= 0xfdff)
                return memory.ReadWide((ushort)(address - 0xc000 - 0x2000));

            if (address <= 0xfe9f)
            {
                if (ppu.register.StateMode == PPUState.OAM_RAM_PERIOD || ppu.register.StateMode == PPUState.TRANSFERRING_DATA || dma.BlockOAM())
                {
                    return 0xff;
                }

                return ppu.ReadOAM((ushort)(address - 0xfe00));
            }

            if (address <= 0xfeff)
                return 0xff;

            if (address <= 0xff00)
                return 0x00; // Player Controller

            if (address <= 0xff01)
                return 0xff; // SB register

            if (address <= 0xff02)
                return 0xff; // SC register

            if (address <= 0xff03)
                return 0xff;

            if (address <= 0xff04)
                return timer.Divider;

            if (address <= 0xff05)
                return timer.TimerCounter;

            if (address <= 0xff06)
                return timer.TimerModulo;

            if (address <= 0xff07)
                return timer.TimerController;

            if (address <= 0xff0e)
                return 0xff;

            if (address <= 0xff0f)
                return InterruptFlags;

            if (address <= 0xff3f) // APU
                return 0xff;

            if (address <= 0xff40)
                return ppu.register.LCDControl;

            if (address <= 0xff41)
                return ppu.register.StateRegister;

            if (address <= 0xff42)
                return ppu.register.ScrollY;

            if (address <= 0xff43)
                return ppu.register.ScrollX;

            if (address <= 0xff44)
                return ppu.register.LYRegister;

            if (address <= 0xff45)
                return ppu.register.LYCompare;

            if (address <= 0xff46)
                return dma.DMA;

            if (address <= 0xff47)
                return ppu.register.BackgroundPalette;

            if (address <= 0xff48)
                return ppu.register.PaletteData[0];

            if (address <= 0xff49)
                return ppu.register.PaletteData[1];

            if (address <= 0xff4a)
                return ppu.register.WindowY;

            if (address <= 0xff4b)
                return ppu.register.WindowX;

            if (address == 0xff50)
                return (byte)(disable_rom ? 0xff : 0x0);

            if (address <= 0xfffd)
                return 0xff;

            if (address <= 0xfffe)
                return memory.Read((ushort)(address - 0xff80));

            if (address == 0xffff)
                return InterruptEnable;

            return 0xff;
        }

        public void Write(ushort address, byte value)
        {
            if (address <= 0x7fff)
                cartridge.WriteRom(address, value);
            else if (address <= 0x9fff)
            {
                if (!ppu.register.LcdOn || ppu.register.StateMode != PPUState.TRANSFERRING_DATA)
                    ppu.WriteVRAM((ushort)(address - 0x8000), value);
            }
            else if (address <= 0xbfff)
                cartridge.WriteRam(address, value);
            else if (address <= 0xdfff)
                memory.WriteWide((ushort)(address - 0xc000), value);
            else if (address <= 0xfdff)
                memory.WriteWide((ushort)(address - 0xc000 - 0x2000), value);
            else if (address <= 0xfe9f)
            {
                if ((!ppu.register.LcdOn || ppu.register.StateMode == PPUState.H_BLANK_PERIOD || ppu.register.StateMode == PPUState.V_BLANK_PERIOD) && !dma.BlockOAM())
                    ppu.WriteOAM((ushort)(address - 0xFE00), value);
            }
            else if (address <= 0xfeff)
                return;
            else if (address == 0xff00)
                return; // TODO: Player Controller
            else if (address <= 0xff03)
                return;
            else if (address == 0xff04)
                timer.Divider = value;
            else if (address == 0xff05)
                timer.TimerCounter = value;
            else if (address == 0xff06)
                timer.TimerModulo = value;
            else if (address == 0xff07)
                timer.TimerController = value;
            else if (address <= 0xff0e)
                return;
            else if (address == 0xff0f)
                InterruptFlags = value;
            else if (address == 0xff3f)
                return; // TODO: APU
            else if (address == 0xff40)
                ppu.register.LCDControl = value;
            else if (address == 0xff41)
                ppu.register.StateRegister = value;
            else if (address == 0xff42)
                ppu.register.ScrollY = value;
            else if (address == 0xff43)
                ppu.register.ScrollX = value;
            else if (address == 0xff44)
                return; // Ignore write to LY
            else if (address == 0xff45)
                ppu.register.LYCompare = value;
            else if (address == 0xff46)
                dma.DMA = value;
            else if (address == 0xff47)
                ppu.register.BackgroundPalette = value;
            else if (address == 0xff48)
                ppu.register.PaletteData[0] = value;
            else if (address == 0xff49)
                ppu.register.PaletteData[1] = value;
            else if (address == 0xff4a)
                ppu.register.WindowY = value;
            else if (address == 0xff4b)
                ppu.register.WindowX = value;
            else if (address == 0xff4f)
                return;
            else if (address == 0xff50)
            {
                if (!disable_rom)
                    disable_rom = value == 0x1;
            }
            else if (address <= 0xff7f)
                return;
            else if (address <= 0xfffe)
                memory.Write((ushort)(address - 0xff80), value);
            else if (address <= 0xffff)
                InterruptEnable = value;
        }

    }

}