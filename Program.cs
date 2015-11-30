using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using teVirtualMIDI;

namespace VirtualTotalmix {
    static class Program {
        private static readonly VirtualMidiPort[] Ports = new VirtualMidiPort[4];

        private static MidiInputDevice Input;
        private static MidiOutputDevice Output;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main () {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

            for (var i = 0; i < Ports.Length; i++) {
                Ports[i] = new VirtualMidiPort("Virtual TotalMix Channel " + i, false);
                Ports[i].OnData += Vm_OnData;
            }
            Console.WriteLine("Virtual TotalMix device active");

            var deviceName = "nanokontrol2";

            using (Input  = MidiInputDevice.OpenByName(deviceName))
            using (Output = MidiOutputDevice.OpenByName(deviceName))
            try {
                Console.WriteLine("Opened midi input device {0}", Input.Name);
                Console.WriteLine("Opened midi output device {0}", Output.Name);
                Input.OnData += Input_OnData;

                Application.Run();
            } finally {
                foreach (var port in Ports)
                    port.Dispose();
            }
        }

        /*
        private static async void Animate () {
            await Task.Delay(500);

            const int min = 32;
            const int max = 71;
            const int c = (max - min) + 1;

            int i = 0;
            while (true) {
                var prev = min + ((i - 2) % c);
                var curr = min + (i % c);

                Output.WriteShort(MidiStatusByte.ControlChange, (byte)prev, 0x00);
                Output.WriteShort(MidiStatusByte.ControlChange, (byte)curr, 0x7F);

                i += 1;

                await Task.Delay(40);
            }
        }
        */

        private static void Input_OnData (object sender, byte[] e) {
            var bytes = new ArraySegment<byte>(e);
            ArraySegment<byte> extraBytes;

            MidiMessage msg;
            if (MidiProtocol.TryParseMessage(bytes, out msg, out extraBytes)) {
                Console.WriteLine("Input {0}", msg);
                return;
            }

            Console.Write("Input {");
            foreach (var b in e)
                Console.Write("{0:X2}", b);
            Console.WriteLine("}");
        }

        private static void Vm_OnData (object sender, byte[] e) {
            var bytes = new ArraySegment<byte>(e);
            ArraySegment<byte> extraBytes;

            var portIndex = Array.IndexOf(Ports, sender);

            MidiMessage msg;
            if (MidiProtocol.TryParseMessage(bytes, out msg, out extraBytes)) {
                Console.WriteLine("Port #{0} {1}", portIndex, msg);
                return;
            }

            Console.Write("Port #{0} {{", portIndex);
            foreach (var b in e)
                Console.Write("{0:X2}", b);
            Console.WriteLine("}");
        }
    }
}
