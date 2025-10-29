using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gbemu
{
    internal class DMAController
    {

        private enum DMATransferState
        {
            REQUESTED, SETTING_UP, RUNNING, STOPPED
        }

        private enum HDMAMode
        {
            GDMA = 0, HDMA = 1
        }

        private enum HDMAState
        {
            AWAITING_H_BLANK, COPYING, FINISHED_LINE
        }

        private Bus bus;

        public DMAController(Bus bus)
        {
            this.bus = bus;
        }

        private DMATransferState dma_transfer_state = DMATransferState.STOPPED;
        private DMATransferState old_dma_transfer_state = DMATransferState.STOPPED;
        private DMATransferState hdma_transfer_state = DMATransferState.STOPPED;
        private ushort hdma_source, hdma_destination, dma_transfer_address;
        private byte hdma, dma;
        private HDMAMode hdma_mode;
        private HDMAState hdma_state;
        private int hdma_transfer_blocks, hdma_transfer_size, hdma_bytes_remaining, current_dma_transfer_index;

        internal byte DMA
        {
            get => dma;
            set
            {
                dma = value;
                old_dma_transfer_state = dma_transfer_state;
                dma_transfer_state = DMATransferState.REQUESTED;
            }
        }

        internal bool BlockInterrupt()
        {
            return hdma_transfer_state == DMATransferState.RUNNING && hdma_mode == HDMAMode.GDMA;
        }

        internal bool BlockOAM()
        {
            return dma_transfer_state == DMATransferState.RUNNING || old_dma_transfer_state == DMATransferState.RUNNING;
        }

        internal byte HDMA1
        {
            get => 0xff;
            set => hdma_source = (ushort)((hdma_source & 0xf0) | (value << 8));
        }

        internal byte HDMA2
        {
            get => 0xff;
            set => hdma_source = (ushort)((hdma_source & 0xff00) | (value & 0xf0));
        }

        internal byte HDMA3
        {
            get => 0xff;
            set => hdma_destination = (ushort)((hdma_destination & 0xf0) | (value << 8) | 0x8000);
        }

        internal byte HDMA4
        {
            get => 0xff;
            set => hdma_destination = (ushort)((hdma_destination & 0x1f00) | (value & 0xf0));
        }

        internal byte HDMA5
        {
            get => hdma;
            set
            {
                hdma = value;
                hdma_mode = (HDMAMode)(value >> 7);
                hdma_state = HDMAState.AWAITING_H_BLANK;
                hdma_transfer_blocks = value & 0x7f;
                hdma_transfer_size = (hdma_transfer_blocks + 1) * 16;
                hdma_transfer_state = DMATransferState.REQUESTED;
            }
        }

        private void StepDMATransfer(int cycles)
        {
            if (dma_transfer_state == DMATransferState.REQUESTED || dma_transfer_state == DMATransferState.SETTING_UP)
            {
                cycles -= 4;

                if (old_dma_transfer_state == DMATransferState.RUNNING)
                    bus.ppu.WriteOAM((ushort)current_dma_transfer_index, bus.Read((ushort)(dma_transfer_address + current_dma_transfer_index)));

                dma_transfer_address = (ushort)(dma << 8);
                dma_transfer_state = dma_transfer_state == DMATransferState.REQUESTED ? DMATransferState.SETTING_UP : DMATransferState.RUNNING;
                old_dma_transfer_state = dma_transfer_state == DMATransferState.RUNNING ? DMATransferState.STOPPED : old_dma_transfer_state;
                current_dma_transfer_index = 0;
            }

            while (cycles > 0)
            {
                bus.ppu.WriteOAM((ushort)current_dma_transfer_index, bus.Read((ushort)(dma_transfer_address + current_dma_transfer_index)));
                cycles -= 4;

                current_dma_transfer_index++;

                if (current_dma_transfer_index >= 160)
                {
                    current_dma_transfer_index = 0;
                    dma_transfer_state = DMATransferState.STOPPED;
                    cycles = 0;
                }
            }
        }

        private void StepHDMATransfer(int cycles)
        {
            if (hdma_mode == HDMAMode.HDMA)
            {
                switch (hdma_state)
                {
                    case HDMAState.FINISHED_LINE when bus.ppu.register.StateMode != ppu.PPUState.H_BLANK_PERIOD:
                        hdma_state = HDMAState.AWAITING_H_BLANK;
                        break;
                    case HDMAState.AWAITING_H_BLANK when bus.ppu.register.StateMode == ppu.PPUState.H_BLANK_PERIOD:
                        hdma_state = HDMAState.COPYING;
                        hdma_bytes_remaining = 16;
                        break;
                }
            }

            while (cycles > 0)
            {
                switch (hdma_transfer_state)
                {
                    case DMATransferState.REQUESTED:
                        cycles -= 4;
                        hdma_transfer_state = DMATransferState.SETTING_UP;
                        break;
                    case DMATransferState.SETTING_UP:
                        cycles -= 4;
                        hdma_state = HDMAState.AWAITING_H_BLANK;
                        hdma_transfer_state = DMATransferState.RUNNING;
                        break;
                    case DMATransferState.RUNNING:
                        if ((hdma_state == HDMAState.COPYING && hdma_mode == HDMAMode.HDMA) || hdma_mode == HDMAMode.GDMA)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                bus.ppu.WriteVRAM((ushort)((0x8000 | (hdma_destination & 0x1fff)) - 0x8000), bus.Read(hdma_source));

                                hdma_destination++;
                                hdma_source++;
                                hdma_transfer_size--;

                                if (hdma_transfer_size <= 0)
                                {
                                    hdma_transfer_state = DMATransferState.STOPPED;
                                    hdma = 0xff;
                                    cycles = 0;
                                    break;
                                }

                                if (hdma_mode == HDMAMode.HDMA)
                                {
                                    hdma_bytes_remaining--;

                                    if (hdma_bytes_remaining <= 0)
                                    {
                                        hdma_state = HDMAState.FINISHED_LINE;
                                        cycles = 0;
                                        break;
                                    }
                                }
                            }

                            cycles -= 4;
                        }
                        else
                            cycles = 0;
                        break;
                    case DMATransferState.STOPPED:
                        cycles = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        internal void Step(int cycles)
        {
            if (dma_transfer_state != DMATransferState.STOPPED)
                StepDMATransfer(cycles);

            if (hdma_transfer_state != DMATransferState.STOPPED)
                StepHDMATransfer(cycles);
        }

    }
}
