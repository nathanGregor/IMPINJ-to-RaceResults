﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime;

namespace DecoderApp
{

	 //public class DecoderApp
	public partial class DecoderApp
	{
		private readonly AppSettings _settings;
		private TcpListener? _rr12Listener;
		private TcpClient? _rr12TcpClient;
		private TcpClient? _rfidClient;
		private bool _isOperational;
		private string? _lastRfidData;
		private double _ProtocolVersion;
		private string _deviceID;

		public DecoderApp(AppSettings settings)
		{
			_settings = settings;
			_ProtocolVersion = settings.ProtocolVersion;
			_deviceID = settings.deviceID;
		}

		public async Task RunAsync()
		{
			Console.WriteLine("Starting DecoderApp...");
			await StartListeningForRr12();
			await ConnectToRfidStream();

			// Start status updater and continuous feeding tasks
			_ = Task.Run(() => StatusUpdater());
			_ = Task.Run(() => ContinuousDataFeed());
		}

		private List<TcpClient> _rr12Clients = new List<TcpClient>();
		// private void StartListeningForRr12()
		private async Task StartListeningForRr12()
		{
			_rr12Listener = new TcpListener(IPAddress.Parse(_settings.Rr12IpAddress), _settings.Rr12Port);
			_rr12Listener.Start();
			Console.WriteLine($"Listening for RR12 on {_settings.Rr12IpAddress}:{_settings.Rr12Port}");

			var client = await _rr12Listener.AcceptTcpClientAsync();
			try
			{
				_rr12Clients.Add(client);
				Console.WriteLine("RR12 connected.");
				// await HandleRr12Client(client);
				}
				catch
				{
					Console.WriteLine("RR12 connection failed.");
					await StartListeningForRr12();
				}

			// _ = Task.Run(async () =>
			// {
			// 	while (true)
			// 	{
			// 	var client = await _rr12Listener.AcceptTcpClientAsync();
			// 		lock (_rr12Clients)
			// 		{
			// 			_rr12Clients.Add(client);
			// 		}
			// 		Console.WriteLine("RR12 connected.");
			// 		_ = HandleRr12Client(client);
			// 	}
			// });
		}

		private async Task HandleRr12Client(TcpClient client)
		{
			using var stream = client.GetStream();
			var buffer = new byte[1024];
			// while (true)
			// {
			try
			{
				var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
				//If no bytes are read (stream might be closed or empty), continue to the next iteration.
				// if (bytesRead == 0)
				// {
				// 	Console.WriteLine("No data received.");
				// 	break;
				// }
				var incoming_data = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();  // Use ASCII encoding for incoming data
				Console.WriteLine($"Received from RR12: {incoming_data}");

				// if (Regex.IsMatch(incoming_data, @"^SETPROTOCOL;<=([0-9.]+)$"))
				// if (MyRegex().IsMatch(incoming_data))
				if (true)
				{
					// Protocol version handler
					var match = Regex.Match(incoming_data, @"^SETPROTOCOL;<=([0-9.]+)$");
					if (match.Success && double.TryParse(match.Groups[1].Value, out double requestedVersion))
					{
						Console.WriteLine($"Requested Version: {requestedVersion}");
						Console.WriteLine($"Protocol Version: {_ProtocolVersion}");
						if (requestedVersion >= _ProtocolVersion)
						{
							await stream.WriteAsync(Encoding.ASCII.GetBytes($"SETPROTOCOL;{_ProtocolVersion}\r\n"));
						}
						else
						{
							await stream.WriteAsync(Encoding.ASCII.GetBytes("ERROR,Unsupported protocol version\r\n"));
						}
					}
					else
					{
						await stream.WriteAsync(Encoding.ASCII.GetBytes("ERROR,Invalid protocol format\r\n"));
					}
				}
				else if (incoming_data == "GO_LIVE")
				{
					_isOperational = true;
					Console.WriteLine("System is live.");
				}
				else if (incoming_data == "GETSTATUS")
				{
					// Get current time and send a sample status update
					DateTime currentTime = DateTime.UtcNow.AddHours(11);
					string status = $"GETSTATUS;{currentTime:yyyy-MM-dd};{currentTime:HH:mm:ss.fff};1;10000000;1;1;;;1;100;0;0;0;;1;1;100;1;0;1;0;0;0;0;13.23\r\n";
					await stream.WriteAsync(Encoding.ASCII.GetBytes(status));
					Console.WriteLine($"Sent status: {status}");
				}

				// After sending GETSTATUS, continue processing and feeding data to RR12

				else if (incoming_data == "GETCONFIG;GENERAL;BOXNAME")
				{
					Console.WriteLine($"Responded: GETCONFIG;GENERAL;BOXNAME;Race Result Emulator;{_deviceID}");
					await stream.WriteAsync(Encoding.ASCII.GetBytes($"GETCONFIG;GENERAL;BOXNAME;Race Result Emulator;{_deviceID}\r\n"));
				}

				else
				{
					Console.WriteLine("Incoming Data does not match protocol REGEX.");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error handling RR12 client: {ex.Message}");
			}
            // Ensure the loop continues processing further requests
            // await Task.Delay(1); // Prevent CPU spinning; small delay ensures continuous checking for new data
			// }
		}

		private async Task ConnectToRfidStream()
		{
			int retryDelay = 1000;
			const int maxRetryDelay = 30000;

			while (true)
			{
				try
				{
					_rfidClient = new TcpClient();
					await _rfidClient.ConnectAsync(_settings.RfidStreamIpAddress, _settings.RfidStreamPort);
					Console.WriteLine("Connected to RFID stream.");
					retryDelay = 1000;
					await HandleRfidStream(_rfidClient);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error connecting to RFID stream: {ex.Message}. Retrying in {retryDelay / 1000} seconds.");
					await Task.Delay(retryDelay);
					retryDelay = Math.Min(retryDelay * 2, maxRetryDelay);
				}
			}
		}

		private async Task HandleRfidStream(TcpClient client)
		{
			using var stream = client.GetStream();
			var buffer = new byte[1024];
			while (true)
			{
				try
				{
					var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
					if (bytesRead == 0) break;

					_lastRfidData = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();  // Ensure RFID data is processed in ASCII
					Console.WriteLine($"Received RFID data: {_lastRfidData}");

					if (_isOperational && _rr12TcpClient != null && _rr12TcpClient.Connected)
					{
						var rr12Stream = _rr12TcpClient.GetStream();
						await rr12Stream.WriteAsync(Encoding.ASCII.GetBytes(_lastRfidData));  // Send data to RR12 in ASCII
						Console.WriteLine($"Forwarded RFID data to RR12: {_lastRfidData}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error handling RFID stream: {ex.Message}");
					//break;
				}
			}
		}

		private async Task StatusUpdater()
		{
			while (true)
			{
				if (_isOperational)
				{
					Console.WriteLine("System status: Operational");
				}
				else
				{
					Console.WriteLine("System status: Idle");
				}
				await Task.Delay(_settings.StatusUpdateInterval);
			}
		}

		private async Task ContinuousDataFeed()
		{
			while (true)
			{
				if (_isOperational && _rr12TcpClient != null && _rr12TcpClient.Connected)
				{
					var rr12Stream = _rr12TcpClient.GetStream();
					if (!string.IsNullOrWhiteSpace(_lastRfidData))
					{
						await rr12Stream.WriteAsync(Encoding.ASCII.GetBytes(_lastRfidData));  // Use ASCII encoding for continuous data
						Console.WriteLine($"Continuously sent RFID data: {_lastRfidData}");
					}
					else
					{
						Console.WriteLine("No RFID data to send.");
					}
				}
				await Task.Delay(1000); // Delay between continuous data sends (1 second)
			}
		}
		[GeneratedRegex(@">SETPROTOCOL;<=([0-9.]+)$")]
		private static partial Regex MyRegex();
	}
}



		
