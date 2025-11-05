using System;

namespace gbemu.timers
{
    internal class Timer
    {
        internal ushort SystemCounter { get; private set; }

        private readonly Device device;
        private int count;
        private int cycles;

        internal Timer(Device device)
        {
            this.device = device;
        }

        internal void Reset(bool skip_boot_rom)
        {
            if (!skip_boot_rom)
            {
                SystemCounter = 0x0;
                return;
            }

            SystemCounter = (device.device_type, device.device_mode) switch
            {
                (DeviceType.DMG, DeviceType.DMG) => 0xABCC,
                (DeviceType.CGB, DeviceType.DMG) => 0x267C,
                (DeviceType.CGB, DeviceType.CGB) => 0x1EA0,
                _ => throw new ArgumentOutOfRangeException(nameof(device.device_type), $"Invalid device type & mode combination ({device.device_type}, {device.device_mode})")
            };
        }

        internal void Step()
        {
            SystemCounter = (ushort)(SystemCounter + 4);

            if (cycles > 0) cycles -= cycles;

            if (!timer_enabled) return;

            count += 4;

            while (count >= clock_select.Step())
            {
                StepTimerCounter();
            }
        }

        private void StepTimerCounter()
        {
            if (TimerCounter == 0xFF)
            {
                cycles = 4;
                _timerCounter = TimerModulo;
                device.interrupt_registers.RequestInterrupt(interrupts.Interrupt.TIMER);
            }
            else
            {
                _timerCounter = (byte)(_timerCounter + 1);
            }

            count -= clock_select.Step();
        }

        private bool timer_enabled;
        private TimerClockSelect clock_select;

        private byte timer_controller = 0b11111000;
        
        internal byte TimerController
        {
            get => timer_controller;
            set
            {
                timer_controller = (byte)((value & 0x7) | 0b11111000);
                timer_enabled = (value & 0x4) == 0x4;
                clock_select = (TimerClockSelect)(value & 0x3);
            }
        }

        internal byte Divider
        {
            get => (byte)(SystemCounter >> 8);
            set
            {
                if ((SystemCounter & 0b1_0000_0000) == 0b1_0000_0000)
                {
                    StepTimerCounter();
                }
                SystemCounter = 0;
                count = 0x0;
            }
        }

        private byte _timerCounter;
        internal byte TimerCounter
        {
            get => cycles > 0 ? (byte)0x0 : _timerCounter;
            set
            {
                if (cycles > 0) return;
                _timerCounter = value;
            }
        }
        internal byte TimerModulo { get; set; }
    }
}
