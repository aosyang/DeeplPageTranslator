using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace TranslatorHost
{
    public partial class Form1 : Form
    {
        private Thread TranslatorThread;

        public Form1()
        {
            InitializeComponent();

            TranslatorThread = new Thread(() => ListenForMessage());

            // Prevent stdio stream from blocking the thread when exits
            TranslatorThread.IsBackground = true;

            // Need a STA thread for clipboard to work properly
            TranslatorThread.SetApartmentState(ApartmentState.STA);
            TranslatorThread.Start();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            TranslatorThread.Abort();
        }

        void ListenForMessage()
        {
            try
            {
                TranslatorService Translator = new TranslatorService();

                LogMessage("Ready");

                JObject data;
                while (true)
                {
                    if ((data = Read()) != null)
                    {
                        if (data.HasValues)
                        {
                            var processed = ProcessMessage(data);
                            int tab = processed.TabId;
                            int index = processed.Index;
                            string text = processed.Text;

                            if (text == "exit")
                            {
                                LogMessage("Received command 'exit'");
                                return;
                            }
                            else
                            {
                                LogMessage("-----------------------------------------------------------");
                                LogMessage("Original: " + text);

                                string Translation = Translator.TranslateHtml(text);
                                Translation = Translator.FixMissingPunctuation(Translation);
                                LogMessage("Translation: " + Translation);

                                var json = new JObject();
                                json["tab"] = tab;
                                json["index"] = index;
                                json["text"] = Translation;

                                Write(json);
                            }
                        }
                    }
                    else
                    {
                        // Sleep when idle to avoid high CPU usage
                        Thread.Sleep(50);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                Debug.WriteLine("Aborting message listening thread");
            }
        }

        void LogMessage(string Text)
        {
            textBox1.Invoke((Action)delegate { textBox1.AppendText(Text + "\r\n"); });
            Debug.WriteLine(Text);
        }

        public struct TranslationRequest
        {
            public TranslationRequest(int InTabId, int InIndex, string InText)
            {
                TabId = InTabId;
                Index = InIndex;
                Text = InText;
            }

            public int TabId;
            public int Index;
            public string Text;
        };

        public static TranslationRequest ProcessMessage(JObject data)
        {
            int tab_id = data["tab"].Value<int>();
            int index = data["index"].Value<int>();
            var message = data["text"].Value<string>();
            switch (message)
            {
                case "test":
                    return new TranslationRequest(-1, -1, "testing!");
                case "exit":
                    return new TranslationRequest(-1, -1, "exit");
                default:
                    return new TranslationRequest(tab_id, index, message);
            }
        }

        public static JObject Read()
        {
            var stdin = Console.OpenStandardInput();
            var lengthBytes = new byte[4];

            // Read buffer length
            stdin.Read(lengthBytes, 0, 4);

            int length = BitConverter.ToInt32(lengthBytes, 0);
            var buffer = new char[length];
            using (var reader = new StreamReader(stdin))
            {
                if (reader.Peek() >= 0)
                {
                    reader.Read(buffer, 0, buffer.Length);
                }
            }

            string JsonString = new string(buffer);
            JObject JsonObj;
            try
            {
                JsonObj = JsonConvert.DeserializeObject<JObject>(JsonString);
            }
            catch (Exception)
            {
                Debug.WriteLine("Exception: Invalid json string - " + JsonString);
                JsonObj = new JObject();
            }

            return JsonObj;
        }

        public static void Write(JObject JsonObj)
        {
            string JsonString = JsonObj.ToString(Formatting.None);
            var bytes = System.Text.Encoding.UTF8.GetBytes(JsonString);

            var stdout = Console.OpenStandardOutput();
            stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
            stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
            stdout.Write(bytes, 0, bytes.Length);
            stdout.Flush();
        }

    }
}
