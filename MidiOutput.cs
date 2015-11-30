using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MidiOutputDevice : IDisposable {
    public struct Info {
        public readonly uint   Index;
        public readonly string Name;

        internal Info (uint index) {
            Win32Midi.MIDIOUTCAPS caps;
            Win32Midi.midiOutGetDevCaps(new UIntPtr(index), out caps);
            Index = index;
            Name = caps.szPname;
        }

        public MidiOutputDevice Open () {
            return new MidiOutputDevice(Index);
        }

        public override string ToString () {
            return string.Format("#{0} '{1}'", Index, Name);
        }
    }

    public readonly string Name;
    public readonly Win32Midi.HMIDIOUT Handle;
    private bool IsDisposed;

    public MidiOutputDevice (uint deviceIndex) {
        var info = new Info(deviceIndex);
        Name = info.Name;

        var openResult = Win32Midi.midiOutOpen(out Handle, new UIntPtr(deviceIndex), null, UIntPtr.Zero, Win32Midi.MidiOpenFlags.CALLBACK_NULL);
        CheckResult(openResult);
    }

    public static MidiOutputDevice OpenByName (string name) {
        return Devices.Where(d => String.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase)).First().Open();
    }

    public static IEnumerable<Info> Devices {
        get {
            uint numDevices = Win32Midi.midiOutGetNumDevs();

            for (uint i = 0; i < numDevices; i++)
                yield return new Info(i);
        }
    }

    private static void CheckResult (Win32Midi.MMRESULT result) {
        if (result != Win32Midi.MMRESULT.MMSYSERR_NOERROR)
            throw new Exception("Midi output API failed: " + result);
    }

    public void WriteShort (MidiStatusByte status, byte data1, byte data2) {
        Win32Midi.MMRESULT result;

        uint dwMsg = (uint)((byte)status | (data1 << 8) | (data2 << 16));
        Console.WriteLine("Write {0} {1:X2} {2:X2}", status, data1, data2);
        CheckResult(Win32Midi.midiOutShortMsg(Handle, dwMsg));
    }

    public unsafe void WriteSysex (byte[] buffer, int? count = null) {
        var _count = count.GetValueOrDefault(buffer.Length);
        if (_count > buffer.Length)
            throw new ArgumentException("count");

        fixed (byte * pBuffer = buffer) {
            Win32Midi.MIDIHDR header = new Win32Midi.MIDIHDR {
                lpData = pBuffer,
                dwBufferLength = (uint)buffer.Length,
                dwBytesRecorded = (uint)_count
            };
            var sizeOfHeader = (uint)Marshal.SizeOf(header);

            CheckResult(Win32Midi.midiOutPrepareHeader(Handle, ref header, sizeOfHeader));
            CheckResult(Win32Midi.midiOutLongMsg(Handle, ref header, sizeOfHeader));
        }
    }

    public void Dispose () {
        lock (this) {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Win32Midi.midiOutClose(Handle);
        }
    }
}
