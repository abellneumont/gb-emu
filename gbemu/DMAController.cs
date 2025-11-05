using System;
using gbemu.ppu;

namespace gbemu
{
    internal class DMAController
    {
        private readonly Device device;

        internal DMAController(Device device)
        {
            this.device = device;
        }

        private DMATransferState dma_transfer_state = DMATransferState.STOPPED;
        private DMATransferState dma_transfer_state_old = DMATransferState.STOPPED;
        private int current_dma_index;
        private ushort dma_transfer_address;
        private ushort hdma_source_address;
        private ushort hdma_destination_address;
        private byte hdma5;
        private HDMAMode hdma_mode;
        private int hdma_transfer_blocks;
        private int hdma_transfer_size;
        private HDMAState hdma_state;
        private int hdma_remaining_bytes;

        private DMATransferState hdma_transfer_state = DMATransferState.STOPPED;
        private byte dma;
        internal byte DMA
        {
            get => dma;
            set
            {
                dma = value;

                dma_transfer_state_old = dma_transfer_state;
                dma_transfer_state = DMATransferState.REQUESTED;
            }
        }

        private void StepDMATransfer(int tCycles)
        {
            if (dma_transfer_state == DMATransferState.REQUESTED || dma_transfer_state == DMATransferState.SETTING_UP)
            {
                tCycles -= 4;

                if (dma_transfer_state_old == DMATransferState.RUNNING)
                {
                    device.ppu.WriteOAMByte(
                    (ushort)(0xFE00 + current_dma_index),
                    device.bus.ReadByte((ushort)(dma_transfer_address + current_dma_index)));
                }

                dma_transfer_address = (ushort)(dma << 8);
                dma_transfer_state = dma_transfer_state == DMATransferState.REQUESTED ? DMATransferState.SETTING_UP : DMATransferState.RUNNING;
                dma_transfer_state_old = dma_transfer_state == DMATransferState.RUNNING ? DMATransferState.STOPPED : dma_transfer_state_old;
                current_dma_index = 0;
            }

            while (tCycles > 0)
            {
                device.ppu.WriteOAMByte(
                    (ushort)(0xFE00 + current_dma_index),
                    device.bus.ReadByte((ushort)(dma_transfer_address + current_dma_index)));

                tCycles -= 4;

                current_dma_index++;
                if (current_dma_index == 160)
                {
                    current_dma_index = 0;
                    dma_transfer_state = DMATransferState.STOPPED;
                    tCycles = 0;
                }
            }
        }

        internal void Step(int tCycles)
        {
            if (dma_transfer_state != DMATransferState.STOPPED)
            {
                StepDMATransfer(tCycles);
            }

            if (hdma_transfer_state != DMATransferState.STOPPED)
            {
                StepHDMATransfer(tCycles);
            }
        }

        internal bool HaltCpu()
        {
            return hdma_transfer_state == DMATransferState.RUNNING && hdma_mode == HDMAMode.GDMA;
        }

        internal bool BlockInterrupts()
        {
            return hdma_transfer_state == DMATransferState.RUNNING && hdma_mode == HDMAMode.GDMA;
        }

        internal bool BlocksOAMRAM()
        {
            return dma_transfer_state == DMATransferState.RUNNING || 
                   dma_transfer_state_old == DMATransferState.RUNNING;
        }

        private void StepHDMATransfer(int tCycles)
        {
            if (hdma_mode == HDMAMode.HDMA)
            {
                switch (hdma_state)
                {
                    case HDMAState.FINISHED_LINE when device.ppu_registers.StatMode != StateMode.H_BLANK_PERIOD:
                        hdma_state = HDMAState.AWAITING_H_BLANK;
                        break;
                    case HDMAState.AWAITING_H_BLANK when device.ppu_registers.StatMode == StateMode.H_BLANK_PERIOD:
                        hdma_state = HDMAState.COPYING;
                        hdma_remaining_bytes = 16;
                        break;
                }
            }

            while (tCycles > 0)
            {
                switch (hdma_transfer_state)
                {
                    case DMATransferState.REQUESTED:
                        tCycles -= 4;
                        hdma_transfer_state = DMATransferState.SETTING_UP;
                        break;
                    case DMATransferState.SETTING_UP:
                        tCycles -= 4;
                        hdma_state = HDMAState.AWAITING_H_BLANK;
                        hdma_transfer_state = DMATransferState.RUNNING;
                        break;
                    case DMATransferState.RUNNING:
                        if ((hdma_state == HDMAState.COPYING && hdma_mode == HDMAMode.HDMA) || hdma_mode == HDMAMode.GDMA)
                        {
                            for (var ii = 0; ii < 2; ii++)
                            {
                                device.ppu.WriteVRAMByte((ushort)(0x8000 | (hdma_destination_address & 0x1FFF)), device.bus.ReadByte(hdma_source_address));
                                hdma_destination_address++;
                                hdma_source_address++;
                                hdma_transfer_size -= 1;

                                if (hdma_transfer_size == 0)
                                {
                                    hdma_transfer_state = DMATransferState.STOPPED;
                                    hdma5 = 0xFF;
                                    tCycles = 0;
                                    break;
                                }

                                if (hdma_mode == HDMAMode.HDMA)
                                {
                                    hdma_remaining_bytes -= 1;
                                    if (hdma_remaining_bytes == 0)
                                    {
                                        hdma_state = HDMAState.FINISHED_LINE;
                                        tCycles = 0;
                                        break;
                                    }
                                }
                            }

                            tCycles -= 4;
                        }
                        else
                        {
                            tCycles = 0;
                        }
                        break;
                    case DMATransferState.STOPPED:
                        tCycles = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal byte HDMA1
        {
            get => 0xFF;
            set => hdma_source_address = (ushort)((hdma_source_address & 0xF0) | (value << 8));
        }

        internal byte HDMA2
        {
            get => 0xFF;
            set => hdma_source_address = (ushort)((hdma_source_address & 0xFF00) | (value & 0xF0));
        }

        internal byte HDMA3
        {
            get => 0xFF;
            set => hdma_destination_address = (ushort)((hdma_destination_address & 0xF0) | (value << 8) | 0x8000);
        }

        internal byte HDMA4
        {
            get => 0xFF;
            set => hdma_destination_address = (ushort)((hdma_destination_address & 0x1F00) | (value & 0xF0));
        }

        internal byte HDMA5
        {
            get => hdma5;
            set
            {
                hdma5 = value;
                hdma_mode = (HDMAMode)(value >> 7);
                hdma_state = HDMAState.AWAITING_H_BLANK;
                hdma_transfer_blocks = value & 0b0111_1111;
                hdma_transfer_size = (hdma_transfer_blocks + 1) * 16;
                hdma_transfer_state = DMATransferState.REQUESTED;
            }
        }

        private enum DMATransferState
        {
            REQUESTED,
            SETTING_UP,
            RUNNING,
            STOPPED
        }

        private enum HDMAMode
        {
            GDMA = 0x0,
            HDMA = 0x1
        }

        private enum HDMAState
        {
            AWAITING_H_BLANK,
            COPYING,
            FINISHED_LINE
        }
    }
}
