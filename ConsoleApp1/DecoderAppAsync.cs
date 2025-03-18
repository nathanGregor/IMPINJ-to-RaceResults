using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DecoderApp
{
    public class DecoderAppAsync
    {
        private TcpClient rfid_client;
        private TcpListener rr12_listener;

        private readonly string rr12_ip = "127.0.0.1";
        private readonly int rr12_port = 3601;

        private readonly string rfid_ip = "speedwayr-11-8e-a5";
        private readonly int rfid_port = 14150;

        public string deviceId = "T-21753";
        public float protocolVersion = 3.4F;

        public DecoderAppAsync()
        {
            Task.Run(async () => await RunAsync()).Wait(); // Run async in constructor
        }

        private async Task RunAsync()
        {
            try
            {
                // Start the TCP listener for RR12
                rr12_listener = new TcpListener(IPAddress.Parse(rr12_ip), rr12_port);
                rr12_listener.Start();
                Console.WriteLine($"Listening for RR12 on {rr12_ip}:{rr12_port}");

                // Accept client connection (async)
                TcpClient rr12_client = await rr12_listener.AcceptTcpClientAsync();
                NetworkStream rr12_stream = rr12_client.GetStream();
                Console.WriteLine("Connected to RR12 Client!");

                // Send protocol setup message
                string protocol_message_string = $"SETPROTOCOL;{protocolVersion}\r\n";
                byte[] protocol_message_bytes = Encoding.UTF8.GetBytes(protocol_message_string);
                await rr12_stream.WriteAsync(protocol_message_bytes, 0, protocol_message_bytes.Length);

                // Read response from RR12 (async)
                byte[] buffer = new byte[1024];
                int bytesRead = await rr12_stream.ReadAsync(buffer, 0, buffer.Length);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received from RR12: {receivedMessage}");

                // Connect to the IMPINJ Reader (async)
                rfid_client = new TcpClient();
                await rfid_client.ConnectAsync(rfid_ip, rfid_port);
                Console.WriteLine($"Connected to IMPINJ Reader at {rfid_ip}:{rfid_port}");

                // Start data transmission asynchronously
                await StartTransmissionAsync(rr12_client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async Task StartTransmissionAsync(TcpClient rr12_client)
        {
            try
            {
                NetworkStream rfid_stream = rfid_client.GetStream();
                NetworkStream rr12_stream = rr12_client.GetStream();

                while (true)
                {
                    byte[] buffer = new byte[1024];

                    // Read from IMPINJ (async)
                    int bytesRead = await rfid_stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Received from RFID Reader: {receivedData}");

                        // Write to RR12 (async)
                        await rr12_stream.WriteAsync(buffer, 0, bytesRead);
                        Console.WriteLine("Data forwarded to RR12");
                    }

                    // Add a delay to prevent excessive CPU usage (adjust as needed)
                    await Task.Delay(10);

                    // Exit condition (Modify as needed)
                    if (false) // Change this for actual logic
                    {
                        Console.WriteLine("Stopping connections...");

                        rr12_listener.Stop();
                        rr12_client.Close();
                        rfid_client.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transmission Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        // public static async Task Main()
        // {
        //     await Task.Run(() => new DecoderAppAsync());
        // }
    }
}
