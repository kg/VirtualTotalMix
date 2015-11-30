using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualTotalmix {
    // The command portion of a midi status byte
    public enum MidiStatusByte : byte {
        // These ones have an associated channel
        NoteOff         = 0x80,
        NoteOn          = 0x90,
        PolyPressure    = 0xA0,
        ControlChange   = 0xB0,
        ProgramChange   = 0xC0,
        ChannelPressure = 0xD0,
        PitchBend       = 0xE0,
        // These do not
        Sysex           = 0xF0,
        TimeCode        = 0xF1,
        SongPosition    = 0xF2,
        SongSelect      = 0xF3,
        TuningRequest   = 0xF6,
        EndOfSysex      = 0xF7,
        TimingClock     = 0xF8,
        Start           = 0xFA,
        Continue        = 0xFB,
        Stop            = 0xFC,
        ActiveSensing   = 0xFE,
        SystemReset     = 0xFF,
    }

    public struct MidiMessage {
        public MidiStatusByte   Status;
        public SysexMessage?    Sysex;
        public MidiMessageData? Data;

        public override string ToString () {
            if (Sysex.HasValue) {
                return String.Format("Sysex ???");
            } else if (Data.HasValue) {
                return String.Format("{0} Ch{1} {2:X4}", Status, Data.Value.Channel, Data.Value.Data);
            } else {
                return Status.ToString();
            }
        }
    }

    public struct MidiMessageData {
        public byte   Channel;
        public ushort Data;
    }

    public struct SysexMessage {
        public uint SysexID;
        public ArraySegment<byte> Data;
    }

    public static class MidiProtocol {
        public static bool TryParseMessage (ArraySegment<byte> input, out MidiMessage message) {
            message = default(MidiMessage);

            message.Status = (MidiStatusByte)input.Array[input.Offset];
            if (message.Status < MidiStatusByte.NoteOff)
                return false;

            if (message.Status >= MidiStatusByte.Sysex) {
            } else {
                message.Data = new MidiMessageData {
                    Channel = (byte)((byte)message.Status & 0x0F),
                    Data    = BitConverter.ToUInt16(input.Array, input.Offset + 1)
                };

                // Mask out channel
                message.Status = (MidiStatusByte)((byte)message.Status & 0xF0);
            }

            return true;
        }
    }
}
