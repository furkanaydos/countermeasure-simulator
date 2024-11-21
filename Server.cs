using SuperSimpleTcp;
using System.Text;
using static EH_KTS.MessageStructs;

namespace EH_KTS
{

    internal class Server
    {
        public SimpleTcpServer _server;
        private readonly Form1 form;
        private static readonly string logFilePath = Path.Combine(Application.StartupPath, "LOG Files/logFile.txt");
        public Server(Form1 form)
        {
            _server = new SimpleTcpServer("", 9000);
            _server.Events.ClientConnected += Events_ClientConnected;
            _server.Events.DataReceived += Events_DataReceived;
            _server.Events.ClientDisconnected += Events_ClientDisconnected;
            this.form = form;
        }

        public static void Log_Record_Txt(LogTypes logType, double? angle = null, double? altitude = null, int? ID = null, int? success = null)
        {
            try
            {
                int logTypeValue = (int)logType;
                string logMessage = $"{DateTime.Now}, {logTypeValue}";
                if (ID.HasValue)
                {
                    logMessage += $", {ID.Value}";
                }

                if (success.HasValue)
                {
                    logMessage += $", {success.Value}";
                }

                if (angle.HasValue && altitude.HasValue)
                {
                    logMessage += $", {angle.Value}, {altitude.Value}";
                }
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Log yazma hatası: {ex.Message}");
            }

        }

        public async void SendLog(byte[] data, string client = "")
        {
            const int packetSize = 10240;
            ushort packetCount = (ushort)(data.Length / packetSize);
            if (data.Length % packetSize > 0)
            {
                packetCount++;
            }
            byte[] packetCountArray = (BitConverter.GetBytes(packetCount));

            if (string.IsNullOrEmpty(client))
            {
                foreach (string ipPort in _server.GetClients())
                {
                    await _server.SendAsync(ipPort, new byte[] { (byte)MessageType.START_SEND_LOG, packetCountArray[0], packetCountArray[1] });
                }
            }
            else
            {
                await _server.SendAsync(client, new byte[] { (byte)MessageType.START_SEND_LOG, packetCountArray[0], packetCountArray[1] });
            }

            for (int i = 0; i < packetCount; i++)
            {
                List<byte> bytes = new List<byte>();
                bytes.Add((byte)MessageType.SEND_LOG);
                bytes.AddRange(data.Skip(i * packetSize).Take((data.Length - i * packetSize) < packetSize ? (data.Length - i * packetSize) : packetSize));
                if (string.IsNullOrEmpty(client))
                {
                    foreach (string ipPort in _server.GetClients())
                    {
                        await _server.SendAsync(ipPort, bytes.ToArray());
                    }
                }
                else
                {
                    await _server.SendAsync(client, bytes.ToArray());
                }
                await Task.Delay(2);
            }

            if (string.IsNullOrEmpty(client))
            {
                foreach (string ipPort in _server.GetClients())
                {
                    await _server.SendAsync(ipPort, new byte[] { (byte)MessageType.END_SEND_LOG, packetCountArray[0], packetCountArray[1] });
                }
            }
            else
            {
                await _server.SendAsync(client, new byte[] { (byte)MessageType.END_SEND_LOG, packetCountArray[0], packetCountArray[1] });
            }
        }

        public bool IsConnected()
        {
            return _server.GetClients().Count() > 0;
        }

        public bool StartServer()
        {
            _server.Start();
            if (IsOpen())
                MessageBox.Show("Server Başlatıldı.");
            else
                MessageBox.Show("Server Başlatılamadı.");

            return IsOpen();
        }

        public void StopServer()
        {
            foreach (string ipPort in _server.GetClients())
            {
                _server.DisconnectClient(ipPort);
            }
            _server.Stop();
            MessageBox.Show("Server Durduruldu.");

        }

        public bool IsOpen()
        {
            return _server.IsListening;
        }

        private void Events_ClientDisconnected(object? sender, ConnectionEventArgs e)
        {
            //MessageBox.Show("Bağlantı kapatıldı.");
        }

        private void Events_DataReceived(object? sender, DataReceivedEventArgs e)
        {
            MessageType messageType = (MessageType)e.Data[0];

            switch (messageType)
            {
                case MessageType.SEND_MDF:
                    byte[] bytes = e.Data.Skip(1).ToArray();
                    string path = Path.Combine(Application.StartupPath, "MDF Files");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    path = Path.Combine(path, Encoding.ASCII.GetString(bytes, 2, 6) + "_" + BitConverter.ToUInt16(bytes, 0).ToString() + ".bin");
                    File.WriteAllBytes(path, bytes);
                    form.FindMDFs();
                    break;
                case MessageType.REQUEST_LOG:
                    SendLog(File.ReadAllBytes(logFilePath), e.IpPort);
                    File.WriteAllText(logFilePath, String.Empty);
                    break;
                default:
                    break;
            }
        }

        private void Events_ClientConnected(object? sender, ConnectionEventArgs e)
        {
            //MessageBox.Show("Bağlantı başlatıldı.");
            Log_Record_Txt(LogTypes.Connection);
        }
    }
}
