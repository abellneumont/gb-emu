using System;
using gbemu.ppu;

namespace gbemu
{
    internal class Bus
    {
        private const int WRAM_SIZE_DMG = 0x2000;
        private const int WRAM_SIZE_CGB = 0x8000;
        private const int HRAM_SIZE = 0x7F;
        private readonly Device device;
        private readonly byte[] rom;
        private readonly byte[] wram;
        private readonly byte[] hram = new byte[HRAM_SIZE];
        private byte wram_bank = 1;

        public Bus(byte[] rom, Device device)
        {
            this.rom = rom;
            this.device = device;

            wram = device.device_mode switch
            {
                DeviceType.DMG => new byte[WRAM_SIZE_DMG],
                DeviceType.CGB => new byte[WRAM_SIZE_CGB],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal byte ReadByte(ushort address)
        {
            if (address <= 0xFF)
                return device.control_registers.RomDisabledRegister == 0
                    ? rom[address]
                    : device.cartridge.ReadRom(address);
            if (address <= 0x7FFF)
                return device.cartridge.ReadRom(address);
            if (address <= 0x9FFF)
            {
                if (device.ppu_registers.StatMode == StateMode.TRANSFERRING_DATA)
                {
                    return 0xFF;
                }

                return device.ppu.GetVRAMByte(address);
            }
            if (address <= 0xBFFF)
                return device.cartridge.ReadRam(address);
            if (address <= 0xDFFF)
                return ReadFromRam(address);
            if (address <= 0xFDFF)
                return ReadFromRam((ushort)(address - 0x2000));
            if (address <= 0xFE9F)
            {
                if (device.ppu_registers.StatMode == StateMode.OAM_RAM_PERIOD || device.ppu_registers.StatMode == StateMode.TRANSFERRING_DATA || device.dma_controller.BlocksOAMRAM())
                {
                    return 0xFF;
                }

                return device.ppu.GetOAMByte(address);
            }
            if (address <= 0xFEFF)
                return 0xFF;
            if (address == 0xFF00)
                return device.controller_handler.ControllerRegister;
            if (address == 0xFF01)
                return device.control_registers.SerialTransferData;
            if (address == 0xFF02)
                return device.control_registers.SerialTransferControl;
            if (address == 0xFF03)
                return 0xFF;
            if (address == 0xFF04)
                return device.timer.Divider;
            if (address == 0xFF05)
                return device.timer.TimerCounter;
            if (address == 0xFF06)
                return device.timer.TimerModulo;
            if (address == 0xFF07)
                return device.timer.TimerController;
            if (address >= 0xFF08 && address <= 0xFF0E)
                return 0xFF;
            if (address == 0xFF0F)
                return device.interrupt_registers.InterruptFlags;
            if (address >= 0xFF10 && address <= 0xFF3F)
                return device.apu.Read(address);
            if (address == 0xFF40)
                return device.ppu_registers.LCDControlRegister;
            if (address == 0xFF41)
                return device.ppu_registers.StateRegister;
            if (address == 0xFF42)
                return device.ppu_registers.ScrollY;
            if (address == 0xFF43)
                return device.ppu_registers.ScrollX;
            if (address == 0xFF44)
                return device.ppu_registers.LYRegister;
            if (address == 0xFF45)
                return device.ppu_registers.LYCompare;
            if (address == 0xFF46)
                return device.dma_controller.DMA;
            if (address == 0xFF47)
                return device.ppu_registers.BackgroundPaletteData;
            if (address == 0xFF48)
                return device.ppu_registers.ObjectPaletteData0;
            if (address == 0xFF49)
                return device.ppu_registers.ObjectPaletteData1;
            if (address == 0xFF4A)
                return device.ppu_registers.WindowY;
            if (address == 0xFF4B)
                return device.ppu_registers.WindowX;
            if (address == 0xFF4C)
                return 0xFF;
            if (address == 0xFF4D)
                return 0xFF;
            if (address == 0xFF4E)
                return 0xFF;
            if (address == 0xFF4F)
                return 0xFF;
            if (address == 0xFF50)
                return device.control_registers.RomDisabledRegister;
            if (address == 0xFF51)
                return 0xFF;
            if (address == 0xFF52)
                return 0xFF;
            if (address == 0xFF53)
                return 0xFF;
            if (address == 0xFF54)
                return 0xFF;
            if (address == 0xFF55)
                return 0xFF;
            if (address == 0xFF56)
                return 0xFF;
            if (address >= 0xFF57 && address <= 0xFF67)
                return 0xFF;
            if (address == 0xFF68)
                return 0xFF;
            if (address == 0xFF69)
                return 0xFF;
            if (address == 0xFF6A)
                return 0xFF;
            if (address == 0xFF6B)
                return 0xFF;
            if (address == 0xFF6C)
                return 0xFF;
            if (address >= 0xFF6D && address <= 0xFF6F)
                return 0xFF;
            if (address == 0xFF70)
                return 0xFF;
            if (address == 0xFF71)
                return 0xFF;
            if (address == 0xFF72)
                return 0xFF;
            if (address == 0xFF73)
                return 0xFF;
            if (address == 0xFF74)
                return 0xFF;
            if (address == 0xFF75)
                return 0xFF;
            if (address == 0xFF76)
                return 0xFF;
            if (address == 0xFF77)
                return 0xFF;
            if (address >= 0xFF78 && address <= 0xFF7F)
                return 0xFF;
            if (address >= 0xFF80 && address <= 0xFFFE)
                return hram[address - 0xFF80];
            if (address == 0xFFFF)
                return device.interrupt_registers.InterruptEnable;

            throw new ArgumentOutOfRangeException();
        }

        internal ushort ReadWord(ushort address) =>
            (ushort)(ReadByte(address) | (ReadByte((ushort)((address + 1) & 0xFFFF)) << 8));

        internal int WriteByte(ushort address, byte value)
        {
            if (address <= 0x7FFF)
                device.cartridge.WriteRom(address, value);
            else if (address >= 0x8000 && address <= 0x9FFF)
            {
                if (!device.ppu_registers.lcd_on || device.ppu_registers.StatMode != StateMode.TRANSFERRING_DATA)
                {
                    device.ppu.WriteVRAMByte(address, value);
                }
            }
            else if (address >= 0xA000 && address <= 0xBFFF)
                device.cartridge.WriteRam(address, value);
            else if (address >= 0xC000 && address <= 0xDFFF)
                WriteToRam(address, value);
            else if (address >= 0xE000 && address <= 0xFDFF)
                WriteToRam((ushort)(address - 0x2000), value);
            else if (address >= 0xFE00 && address <= 0xFE9F)
            {
                if ((!device.ppu_registers.lcd_on ||
                     device.ppu_registers.StatMode == StateMode.H_BLANK_PERIOD ||
                     device.ppu_registers.StatMode == StateMode.V_BLANK_PERIOD) && !device.dma_controller.BlocksOAMRAM())
                {
                    device.ppu.WriteOAMByte(address, value);
                }
            }
            else if (address >= 0xFEA0 && address <= 0xFEFF) { }
            else if (address == 0xFF00)
                device.controller_handler.ControllerRegister = value;
            else if (address == 0xFF01)
            {
                if (device.control_registers.SerialTransferControl == 0x81)
                {
                    Console.Write(Convert.ToChar(value));
                    device.control_registers.SerialTransferData = value;
                }
            }
            else if (address == 0xFF02)
                device.control_registers.SerialTransferControl = value;
            else if (address == 0xFF03) { }
            else if (address == 0xFF04)
                device.timer.Divider = value;
            else if (address == 0xFF05)
                device.timer.TimerCounter = value;
            else if (address == 0xFF06)
                device.timer.TimerModulo = value;
            else if (address == 0xFF07)
                device.timer.TimerController = value;
            else if (address >= 0xFF08 && address <= 0xFF0E) { }
            else if (address == 0xFF0F)
                device.interrupt_registers.InterruptFlags = value;
            else if (address >= 0xFF10 && address <= 0xFF3F)
                device.apu.Write(address, value);
            else if (address == 0xFF40)
                device.ppu_registers.LCDControlRegister = value;
            else if (address == 0xFF41)
                device.ppu_registers.StateRegister = value;
            else if (address == 0xFF42)
                device.ppu_registers.ScrollY = value;
            else if (address == 0xFF43)
                device.ppu_registers.ScrollX = value;
            else if (address == 0xFF44) { }
            else if (address == 0xFF45)
                device.ppu_registers.LYCompare = value;
            else if (address == 0xFF46)
                device.dma_controller.DMA = value;
            else if (address == 0xFF47)
                device.ppu_registers.BackgroundPaletteData = value;
            else if (address == 0xFF48)
                device.ppu_registers.ObjectPaletteData0 = value;
            else if (address == 0xFF49)
                device.ppu_registers.ObjectPaletteData1 = value;
            else if (address == 0xFF4A)
                device.ppu_registers.WindowY = value;
            else if (address == 0xFF4B)
                device.ppu_registers.WindowX = value;
            else if (address == 0xFF4D)
                device.control_registers.SpeedSwitchRequested = (value & 0x1) == 0x1;
            else if (address >= 0xFF4C && address <= 0xFF4E) { }
            else if (address == 0xFF4F)
                device.ppu.SetVRAMBankRegister(value);
            else if (address == 0xFF50)
                device.control_registers.RomDisabledRegister = value;
            else if (address == 0xFF51) { }
            else if (address == 0xFF52) { }
            else if (address == 0xFF53) { }
            else if (address == 0xFF54) { }
            else if (address == 0xFF55) { }
            else if (address == 0xFF56) { }
            else if (address >= 0xFF57 && address <= 0xFF67) { }
            else if (address == 0xFF68) { }
            else if (address == 0xFF69) { }
            else if (address == 0xFF6A) { }
            else if (address == 0xFF6B) { }
            else if (address == 0xFF6C) { }
            else if (address >= 0xFF6D && address <= 0xFF6F) { }
            else if (address == 0xFF70)
            {
                wram_bank = (byte)(value & 0x7);
                if (wram_bank == 0) wram_bank = 1;
            }
            else if (address == 0xFF71) { }
            else if (address == 0xFF72)
                device.control_registers.FF72 = value;
            else if (address == 0xFF73)
                device.control_registers.FF73 = value;
            else if (address == 0xFF74) { }
            else if (address == 0xFF75)
                device.control_registers.FF75 = value;
            else if (address == 0xFF76) { }
            else if (address == 0xFF77) { }
            else if (address >= 0xFF78 && address <= 0xFF7F) { }
            else if (address >= 0xFF80 && address <= 0xFFFE)
                hram[address - 0xFF80] = value;
            else if (address == 0xFFFF)
                device.interrupt_registers.InterruptEnable = value;
            else
                throw new ArgumentOutOfRangeException();

            return 8;
        }

        internal int WriteWord(ushort address, ushort value)
        {
            return
                WriteByte(address, (byte)(value & 0xFF)) +
                WriteByte((ushort)((address + 1) & 0xFFFF), (byte)(value >> 8));
        }

        private void WriteToRam(ushort address, byte value)
        {
            if (device.device_mode == DeviceType.DMG || address < 0xD000)
            {
                wram[address - 0xC000] = value;
                return;
            }

            wram[address - 0xD000 + wram_bank * 0x1000] = value;
        }

        private byte ReadFromRam(ushort address)
        {
            if (device.device_mode == DeviceType.DMG || address < 0xD000)
            {
                return wram[address - 0xC000];
            }

            return wram[address - 0xD000 + wram_bank * 0x1000];
        }
    }
}
