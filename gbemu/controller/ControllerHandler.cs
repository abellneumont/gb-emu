using System;
using System.Collections.Generic;
using gbemu.interrupts;

namespace gbemu.controller
{
    internal class ControllerHandler
    {
        private readonly Device device;

        private readonly Dictionary<ControllerKey, bool> key_states = new Dictionary<ControllerKey, bool>
        {
            { ControllerKey.A, false },
            { ControllerKey.B, false },
            { ControllerKey.SELECT, false },
            { ControllerKey.START, false },
            { ControllerKey.RIGHT, false },
            { ControllerKey.LEFT, false },
            { ControllerKey.UP, false },
            { ControllerKey.DOWN, false }
        };
        private ControllerRegisterMode controller_register_mode = ControllerRegisterMode.NONE;

        internal byte ControllerRegister
        {
            get
            {
                var controller = 0xFF;
                switch (controller_register_mode)
                {
                    case ControllerRegisterMode.NONE:
                        break;
                    case ControllerRegisterMode.DPAD:
                        controller &= 0b11101111;
                        controller &= key_states[ControllerKey.RIGHT] ? ControllerKey.RIGHT.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.LEFT] ? ControllerKey.LEFT.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.UP] ? ControllerKey.UP.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.DOWN] ? ControllerKey.DOWN.BitMask() : 0xFF;
                        break;
                    case ControllerRegisterMode.BUTTONS:
                        controller &= 0b11011111;
                        controller &= key_states[ControllerKey.A] ? ControllerKey.A.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.B] ? ControllerKey.B.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.SELECT] ? ControllerKey.SELECT.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.START] ? ControllerKey.START.BitMask() : 0xFF;
                        break;
                    case ControllerRegisterMode.BOTH:
                        controller &= 0b11001111;
                        controller &= key_states[ControllerKey.RIGHT] || key_states[ControllerKey.A] ? ControllerKey.A.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.LEFT] || key_states[ControllerKey.B] ? ControllerKey.B.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.UP] || key_states[ControllerKey.SELECT] ? ControllerKey.SELECT.BitMask() : 0xFF;
                        controller &= key_states[ControllerKey.DOWN] || key_states[ControllerKey.START] ? ControllerKey.START.BitMask() : 0xFF;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return (byte)controller;
            }
            set
            {
                controller_register_mode = (ControllerRegisterMode)(((value & 0x20) | (value & 0x10)) >> 4);
                CheckForInterrupts();
            }
        }

        internal ControllerHandler(Device device)
        {
            this.device = device;
        }

        internal void KeyDown(ControllerKey key)
        {
            key_states[key] = true;

            CheckForInterrupts();
        }

        internal void KeyUp(ControllerKey key)
        {
            key_states[key] = false;
        }

        private void CheckForInterrupts()
        {
            foreach (var (key, state) in key_states)
            {
                if (state && controller_register_mode == key.RegisterMode())
                {
                    device.interrupt_registers.RequestInterrupt(Interrupt.CONTROLLER);
                    break;
                }
            }
        }

        internal void Clear()
        {
            controller_register_mode = ControllerRegisterMode.NONE;
            foreach (var key in key_states.Keys)
            {
                key_states[key] = false;
            }
        }
    }
}