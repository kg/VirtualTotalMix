using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var result = string.Format("Sysex {0:X8}", Sysex.Value.SysexID);
            foreach (var b in Sysex.Value.Data)
                result += string.Format(" {0:X2}", b);

            return result;
        } else if (Data.HasValue) {
            return string.Format("{0} Ch{1} {2:X2} {3:X2}", Status, Data.Value.Channel, Data.Value.Data1, Data.Value.Data2);
        } else {
            return Status.ToString();
        }
    }
}

public struct MidiMessageData {
    public byte Channel;
    public byte Data1;
    public byte Data2;
}

public struct SysexMessage {
    public uint SysexID;
    public ArraySegment<byte> Data;
}

public static class MidiProtocol {
    public static bool TryParseMessage (ArraySegment<byte> input, out MidiMessage message, out ArraySegment<byte> remainingInput) {
        remainingInput = input;
        message = default(MidiMessage);

        message.Status = (MidiStatusByte)input.Array[input.Offset];
        if (message.Status < MidiStatusByte.NoteOff)
            return false;

        if (message.Status >= MidiStatusByte.Sysex) {
            if (message.Status == MidiStatusByte.Sysex) {
                SysexMessage sysex;
                var ok = TryParseSysex(input, out sysex, out remainingInput);
                message.Sysex = sysex;
                return ok;
            } else
                return false;
        } else {
            var data = new MidiMessageData {
                Channel = (byte)((byte)message.Status & 0x0F)
            };

            if (input.Count > 1)
                data.Data1 = input.Array[input.Offset + 1];
            if (input.Count > 2)
                data.Data2 = input.Array[input.Offset + 2];

            message.Data = data;
            // Mask out channel
            message.Status = (MidiStatusByte)((byte)message.Status & 0xF0);

            remainingInput = new ArraySegment<byte>(
                input.Array, 
                Math.Min(input.Offset + 3, input.Array.Length - 1), 
                Math.Max(input.Count - 3, 0)
            );
        }

        return true;
    }

    public static bool TryParseSysex (ArraySegment<byte> input, out SysexMessage message, out ArraySegment<byte> remainingInput) {
        message = default(SysexMessage);
        remainingInput = input;

        if (input.Array[input.Offset] != (byte)MidiStatusByte.Sysex)
            return false;

        for (var i = 0; i < (input.Count - 1); i++) {
            var b = input.Array[input.Offset + i + 1];

            if (b == (byte)MidiStatusByte.EndOfSysex) {
                ParseSysexBody(new ArraySegment<byte>(
                    input.Array, 
                    input.Offset + 1, i
                ), out message);
                return true;
            }
        }

        return false;
    }

    private static void ParseSysexBody (ArraySegment<byte> body, out SysexMessage message) {
        message = new SysexMessage {
            SysexID = 0,
            Data = body
        };
    }
}
