using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbemu
{
    internal class Timer
    {
        internal ushort SystemTimer { get; private set; }

        private readonly Bus bus;
        private bool timer_enabled;
        private byte clock_speed, timer_counter, timer_controller = 0xf8;
        private int internal_time, reloading_cycles;

        internal Timer(Bus bus)
        {
            this.bus = bus;
        }

        internal byte TimerModulo { get; set; }

        internal byte TimerCounter
        {
            get => reloading_cycles > 0 ? (byte)0x0 : timer_counter;
            set
            {
                if (reloading_cycles > 0)
                    return;

                timer_counter = value;
            }
        }

        internal byte TimerController
        {
            get => timer_controller;
            set
            {
                timer_controller = (byte)((value & 0x7) | 0xf8);
                timer_enabled = (value & 0x4) == 0x4;
                clock_speed = (byte)(value & 0x3);
            }
        }

        internal byte Divider
        {
            get => (byte)(SystemTimer >> 8);
            set
            {
                if ((SystemTimer & 0x100) == 0x100)
                    StepTimerCount();

                SystemTimer = 0;
                internal_time = 0;
            }
        }

        private void StepTimerCount()
        {
            if (TimerCounter == 0xff)
            {
                reloading_cycles = 4;
                timer_counter = TimerModulo;
                bus.RequestInterrupt(InterruptType.TIMER);
            }
            else
                timer_counter = (byte)(timer_counter + 1);

            internal_time -= GetIntegerSpeed(clock_speed);
        }

        internal void Step()
        {
            SystemTimer = (ushort)(SystemTimer + 4);

            if (reloading_cycles > 0)
                reloading_cycles -= reloading_cycles;

            if (!timer_enabled)
                return;

            internal_time += 4;

            while (internal_time >= GetIntegerSpeed(clock_speed))
                StepTimerCount();
        }

        private int GetIntegerSpeed(byte value)
        {
            return value switch
            {
                0 => 1024,
                1 => 16,
                2 => 64,
                3 => 256,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

    }
}
