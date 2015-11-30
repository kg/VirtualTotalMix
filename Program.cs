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

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main () {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

            var numInputDevices = Win32Midi.midiInGetNumDevs();
            Console.WriteLine("{0} midi input devices available:", numInputDevices);
            for (uint i = 0; i < numInputDevices; i++) {
                Win32Midi.MIDIINCAPS caps;
                Win32Midi.midiInGetDevCaps(new UIntPtr(i), out caps);
                Console.WriteLine(caps.szPname);
            }
            Console.WriteLine();

            for (var i = 0; i < Ports.Length; i++) {
                Ports[i] = new VirtualMidiPort("Virtual TotalMix Channel " + i, false);
                Ports[i].OnData += Vm_OnData;
            }
            Console.WriteLine("Virtual TotalMix device active");

            using (var input = new MidiInputDevice(0))
            try {
                Console.WriteLine("Opened midi input device #0");
                input.OnData += Input_OnData;

                Application.Run();
            } finally {
                foreach (var port in Ports)
                    port.Dispose();
            }
        }

        private static void Input_OnData (object sender, byte[] e) {
            MidiMessage msg;
            if (MidiProtocol.TryParseMessage(new ArraySegment<byte>(e), out msg)) {
                Console.WriteLine("Input {0}", msg);
                return;
            }

            Console.Write("Input {");
            foreach (var b in e)
                Console.Write("{0:X2}", b);
            Console.WriteLine("}");
        }

        private static void Vm_OnData (object sender, byte[] e) {
            var portIndex = Array.IndexOf(Ports, sender);
            Console.Write("Port #{0} {{");
            foreach (var b in e)
                Console.Write("{0:X2}", b);
            Console.WriteLine("}");
        }
    }
}
