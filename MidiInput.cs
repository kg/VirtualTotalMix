using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class MidiInputDevice : IDisposable {
    public readonly Win32Midi.HMIDIIN Handle;

    public event EventHandler<byte[]> OnData;

    private readonly Win32Midi.MidiInProc Callback;
    private readonly SynchronizationContext SynchronizationContext;
    private bool IsDisposed;

    public MidiInputDevice (uint deviceIndex) {
        SynchronizationContext = SynchronizationContext.Current;
        if (SynchronizationContext == null)
            throw new InvalidOperationException("No synchronization context");

        Callback = _MidiCallback;
        var openResult = Win32Midi.midiInOpen(out Handle, new UIntPtr(deviceIndex), Callback, UIntPtr.Zero, Win32Midi.MidiOpenFlags.CALLBACK_FUNCTION);
        if (openResult != Win32Midi.MMRESULT.MMSYSERR_NOERROR)
            throw new Exception("Opening midi input device failed with " + openResult.ToString());

        Win32Midi.midiInStart(Handle);
    }
       
    private void _InvokeEventHandler (object state) {
        if (OnData != null)
            OnData(this, (byte[])state);
    }

    private unsafe void _MidiCallback (Win32Midi.HMIDIIN handle, Win32Midi.MidiInMessage msg, UIntPtr dwInstance, UIntPtr dwParam1, UIntPtr dwParam2) {
        byte[] data = null;

        switch (msg) {
            case Win32Midi.MidiInMessage.MIM_DATA:
                data = new byte[3];
                Marshal.Copy(new IntPtr(&dwParam1), data, 0, 3);
                break;
            
            case Win32Midi.MidiInMessage.MIM_LONGDATA:
                var pHdr = (Win32Midi.MIDIHDR*)dwParam1;
                data = new byte[pHdr->dwBytesRecorded];
                Marshal.Copy(new IntPtr(pHdr->lpData), data, 0, (int)pHdr->dwBytesRecorded);
                break;

            /*
            case Win32Midi.MidiInMessage.MIM_MOREDATA:
                break;
            case Win32Midi.MidiInMessage.MIM_ERROR:
                break;
            case Win32Midi.MidiInMessage.MIM_LONGERROR:
                break;
            */

            default:
                return;
        }

        if (data != null)
            SynchronizationContext.Post(_InvokeEventHandler, data);
    }

    public void Dispose () {
        lock (this) {
            if (IsDisposed)
                return;

            IsDisposed = true;
            Win32Midi.midiInClose(Handle);
        }
    }
}
