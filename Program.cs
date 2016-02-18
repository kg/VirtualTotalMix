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

            using (var nk2 = new NanoKontrol2())
            try {
                nk2.OnControlChanged += Nk2_OnChanged;
                nk2.OnButtonChanged += Nk2_OnButtonChanged;

                Application.Run();
            } finally {
                foreach (var port in Ports)
                    port.Dispose();
            }
        }

        private static void Nk2_OnButtonChanged (object sender, NanoKontrol2.ButtonEventArgs e) {
            Console.WriteLine("NK2 {0}[{1}] {2}", sender, e.Index, e.NewValue ? "+" : "-");
        }

        private static void Nk2_OnChanged (object sender, NanoKontrol2.ControlEventArgs e) {
            Console.WriteLine("NK2 {0}[{1}] = {2}", sender, e.Index, e.NewValue);
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
