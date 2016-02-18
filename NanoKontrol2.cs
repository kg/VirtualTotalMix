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

    public class LEDCollection {
        private readonly NanoKontrol2 Parent;
        public readonly NanoKontrol2.ButtonCategory Category;

        public LEDCollection (NanoKontrol2 parent, NanoKontrol2.ButtonCategory category) {
            Parent = parent;
            Category = category;
        }

        public bool this[int index] {
            set {
                Parent.ChangeButtonLED(Category, index, value);
            }
        }
    }

    public class ButtonStateCollection {
        public readonly NanoKontrol2.ButtonCategory Category;
        public readonly LEDCollection LEDs;
        private readonly bool[] Values;

        internal ButtonStateCollection (NanoKontrol2 parent, NanoKontrol2.ButtonCategory category, bool[] values) {
            LEDs = new LEDCollection(parent, category);
            Values = values;
            Category = category;
        }

        public bool this[int index] {
            get {
                return Values[index];
            }
        }

        public override string ToString () {
            return Category.ToString();
        }
    }

    public class ControlStateCollection {
        private readonly MidiValue[] Values;
        private readonly string Name;

        internal ControlStateCollection (string name, MidiValue[] values) {
            Values = values;
            Name = name;
        }

        public MidiValue this[int index] {
            get {
                return new MidiValue(Values[index]);
            }
        }

        public override string ToString () {
            return Name;
        }
    }

    public class NanoKontrol2 : IDisposable {
        public const int TrackCount = 8;

        public const int FirstSlider = 0x00;
        public const int FirstKnob = 0x10;
        public const int FirstButton = FirstSolo;
        public const int FirstSolo = 0x20;
        public const int FirstMute = 0x30;
        public const int FirstRecord = 0x40;
        public const int LastButton = FirstRecord + TrackCount - 1;

        public enum ButtonCategory {
            Solo = FirstSolo,
            Mute = FirstMute,
            Record = FirstRecord,
            Track,
            Marker,
            Shuttle
        }

        public struct ButtonEventArgs {
            public readonly ButtonCategory Category;
            public readonly int  Index;
            public readonly bool PreviousValue;
            public readonly bool NewValue;

            public ButtonEventArgs (ButtonCategory category, int index, bool previousValue, bool newValue) {
                Category = category;
                Index = index;
                PreviousValue = previousValue;
                NewValue = newValue;
            }
        }

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

        public event Action<ControlStateCollection, ControlEventArgs> OnControlChanged;
        public event Action<ButtonStateCollection, ButtonEventArgs>   OnButtonChanged;

        public readonly MidiInputDevice  Input;
        public readonly MidiOutputDevice Output;

        private readonly MidiValue[] _Slider = new MidiValue[TrackCount];
        private readonly MidiValue[] _Knob = new MidiValue[TrackCount];
        private readonly bool[] _Solo = new bool[TrackCount];
        private readonly bool[] _Mute = new bool[TrackCount];
        private readonly bool[] _Record = new bool[TrackCount];

        public readonly ControlStateCollection Slider, Knob;
        public readonly ButtonStateCollection Solo, Mute, Record;

        public NanoKontrol2 (string midiDeviceName = "nanoKONTROL2") {
            Slider = new ControlStateCollection("Slider", _Slider);
            Knob = new ControlStateCollection("Knob", _Knob);
            Solo = new ButtonStateCollection(this, ButtonCategory.Solo, _Solo);
            Mute = new ButtonStateCollection(this, ButtonCategory.Mute, _Mute);
            Record = new ButtonStateCollection(this, ButtonCategory.Record, _Record);

            Input = MidiInputDevice.OpenByName(midiDeviceName);
            Output = MidiOutputDevice.OpenByName(midiDeviceName);

            Input.OnData += Input_OnData;
        }

        private void ChangeValue (ControlStateCollection sender, MidiValue[] values, int index, byte newValue) {
            var oldValue = values[index];
            var _newValue = new MidiValue(newValue);
            values[index] = _newValue;

            if (OnControlChanged != null)
                OnControlChanged(sender, new ControlEventArgs(index, oldValue, _newValue));
        }

        private void ChangeButtonState (ButtonStateCollection sender, bool[] values, int index, bool newValue) {
            var oldValue = values[index];
            values[index] = newValue;

            if (OnButtonChanged != null)
                OnButtonChanged(sender, new ButtonEventArgs(sender.Category, index, oldValue, newValue));
        }

        private bool HandleMessage (ref MidiMessage msg) {
            if (msg.Status != MidiStatusByte.ControlChange)
                return false;

            var data = msg.Data.Value;
            var channel = data.Channel;

            var sliderIndex = data.Data1 - FirstSlider;
            var knobIndex   = data.Data1 - FirstKnob;
            var buttonIndex = data.Data1 - FirstButton;

            if ((sliderIndex >= 0) && (sliderIndex < TrackCount))
                ChangeValue(Slider, _Slider, sliderIndex, data.Data2);
            else if ((knobIndex >= 0) && (knobIndex < TrackCount))
                ChangeValue(Knob, _Knob, knobIndex, data.Data2);
            else if ((buttonIndex >= 0) && (buttonIndex <= LastButton)) {
                ButtonStateCollection bsc;
                bool[] bs;

                if (data.Data1 >= FirstRecord) {
                    bsc = Record;
                    bs = _Record;
                } else if (data.Data1 >= FirstMute) {
                    bsc = Mute;
                    bs = _Mute;
                } else {
                    bsc = Solo;
                    bs = _Solo;
                }

                ChangeButtonState(bsc, bs, buttonIndex % TrackCount, data.Data2 == 0x7F); 
            } else
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

        public void ChangeButtonLED (ButtonCategory category, int index, bool state) {
            var selector = (byte)(category + index);
            Output.WriteShort(MidiStatusByte.ControlChange, selector, (byte)(state ? 0x7F : 0x00));
        }

        public void Dispose () {
            Input.Dispose();
            Output.Dispose();
        }
    }
}
