using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualTotalmix {
    public struct MidiValue {
        public readonly byte Raw;

        public MidiValue (byte raw) {
            Raw = raw;
        }

        public static explicit operator MidiValue (byte raw) {
            return new MidiValue(raw);
        }

        public static implicit operator byte (MidiValue v) {
            return v.Raw;
        }

        public static implicit operator float (MidiValue v) {
            return v.Raw / 0x7Ff;
        }

        public override string ToString () {
            return Raw.ToString("X2");
        }
    }

    public class NanoKontrol2 : IDisposable {
        public const int FirstSlider = 0x00;
        public const int SliderCount = 8;

        public struct ControlEventArgs {
            public readonly int Index;
            public readonly MidiValue PreviousValue;
            public readonly MidiValue NewValue;

            public ControlEventArgs (int index, MidiValue previousValue, MidiValue newValue) {
                Index = index;
                PreviousValue = previousValue;
                NewValue = newValue;
            }
        }

        public class SliderState {
            private readonly MidiValue[] Values;

            internal SliderState (MidiValue[] values) {
                Values = values;
            }

            public MidiValue this[int index] {
                get {
                    return new MidiValue(Values[index]);
                }
            }

            public override string ToString () {
                return "Sliders";
            }
        }

        public event EventHandler<ControlEventArgs> OnChanged;

        public readonly MidiInputDevice  Input;
        public readonly MidiOutputDevice Output;

        private readonly MidiValue[] _Sliders = new MidiValue[SliderCount];
        public readonly SliderState Sliders;

        public NanoKontrol2 (string midiDeviceName = "nanoKONTROL2") {
            Sliders = new SliderState(_Sliders);

            Input = MidiInputDevice.OpenByName(midiDeviceName);
            Output = MidiOutputDevice.OpenByName(midiDeviceName);

            Input.OnData += Input_OnData;
        }

        private void ChangeValue<T>(T sender, MidiValue[] values, int index, byte newValue) {
            var oldValue = values[index];
            var _newValue = new MidiValue(newValue);
            values[index] = _newValue;

            if (OnChanged != null)
                OnChanged(sender, new ControlEventArgs(index, oldValue, _newValue));
        }

        private bool HandleMessage (ref MidiMessage msg) {
            if (msg.Status != MidiStatusByte.ControlChange)
                return false;

            var data = msg.Data.Value;
            var channel = data.Channel;

            var sliderIndex = data.Data1 - FirstSlider;

            if ((sliderIndex >= 0) && (sliderIndex < SliderCount))
                ChangeValue(Sliders, _Sliders, sliderIndex, data.Data2);
            else
                return false;

            return true;
        }

        private void Input_OnData (object sender, byte[] e) {
            var data = new ArraySegment<byte>(e);

            while (data.Count > 0) {
                MidiMessage msg;
                if (!MidiProtocol.TryParseMessage(data, out msg, out data))
                    break;

                if (!HandleMessage(ref msg))
                    Console.WriteLine("NK2> {0}", msg);
            }
        }

        public void Dispose () {
            Input.Dispose();
            Output.Dispose();
        }
    }
}
