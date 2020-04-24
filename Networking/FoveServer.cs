using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NyanbreFOVEAS.Networking
{
    public class FoveServer
    {
        private TcpListener foveServer = null;
        public float currentMultiplier = 1f;

        public void StartAsync(string ip, int port) => Task.Factory.StartNew(() => Start(ip, port));
        public void Close() => foveServer.Stop();

        public void Start(string ip, int port)
        {
            try
            {
                NyanbreFOVEAS.Logger.info("[FOVEServer]: Starting...");
                IPAddress localAddr = IPAddress.Parse(ip);
                foveServer = new TcpListener(localAddr, port);
     
                // start listener
                foveServer.Start();
     
                while (true)
                {
    //                NyanbreFOVEAS.Logger.info("[FOVEServer]: Awaiting for connections...");
     
                    // recieve incoming connection
                    TcpClient client = foveServer.AcceptTcpClient();
    //                NyanbreFOVEAS.Logger.info("[FOVEServer]: A client has connected.");
     
                    // recieve network stream for read/write operations
                    NetworkStream stream = client.GetStream();

                    string response = FormatNumDigits(currentMultiplier, 8).Substring(1);
                    // encode response as a byte array
                    byte[] data = Encoding.UTF8.GetBytes(response);
     
                    stream.Write(data, 0, data.Length);
    //                NyanbreFOVEAS.Logger.info("[FOVEServer]: response: " + response);

                    stream.Close();
                    client.Close();
                }
            }
            catch (Exception e)
            {
                NyanbreFOVEAS.Logger.info("[FOVEServer]: ERROR: " + e.Message);
    //            _settings.foveServerPort += 1;
            }
            finally
            {
                NyanbreFOVEAS.Logger.info("[FOVEServer]: Stopping.");
                foveServer?.Stop();
            }
        }

        // Copied from stackoverflow 'cause why not~
        // https://stackoverflow.com/questions/11789194/string-format-how-can-i-format-to-x-digits-regardless-of-decimal-place
        public string FormatNumDigits(float number, int x) {
            string asString = (number >= 0? "+":"") + number.ToString("F50",System.Globalization.CultureInfo.InvariantCulture);

            if (asString.Contains(".")) {
                if (asString.Length > x + 2) {
                    return asString.Substring(0, x + 2);
                } else {
                    // Pad with zeros
                    return asString.Insert(asString.Length, new String('0', x + 2 - asString.Length));
                }
            } else {
                if (asString.Length > x + 1) {
                    return asString.Substring(0, x + 1);
                } else {
                    // Pad with zeros
                    return asString.Insert(1, new String('0', x + 1 - asString.Length));
                }
            }
        }
    }
}