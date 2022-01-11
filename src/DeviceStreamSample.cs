// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using IoTEdgeSasTokenHelper;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Samples.Common;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public class DeviceStreamSample
    {
        private readonly string _hostName;
        private readonly int _port;

        public DeviceStreamSample(string hostName, int port)
        {
            _hostName = hostName;
            _port = port;
        }

        private static async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[10240];

            while (remoteStream.State == WebSocketState.Open)
            {
                var receiveResult = await remoteStream.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                await localStream.WriteAsync(buffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        private static async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[10240];

            while (localStream.CanRead)
            {
                int receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                await remoteStream.SendAsync(new ArraySegment<byte>(buffer, 0, receiveCount), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task Init(CancellationTokenSource cancellationTokenSource)
        {
            var tokenHelper = new SecurityDaemonClient();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"tokenHelper: '{tokenHelper.ToString()}'");
                    var token = tokenHelper.GetModuleToken(3600).Result;
                    Console.WriteLine($"Token: '{token}'");
                    var tokenAuthentication = new ModuleAuthenticationWithToken(tokenHelper.DeviceId, tokenHelper.ModuleId, token);

                    var transportType = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
                    ITransportSettings[] settings = { transportType };
                    using (ModuleClient moduleClient = ModuleClient.Create(tokenHelper.IotHubHostName, tokenAuthentication, settings))
                    {
                        if (moduleClient == null)
                        {
                            Console.WriteLine("Failed to create ModuleClient!");
                            throw new InvalidOperationException("Failed to create ModuleClient");
                        }
                        moduleClient.SetConnectionStatusChangesHandler((ConnectionStatus status, ConnectionStatusChangeReason reason) =>
                        {
                            if (reason == ConnectionStatusChangeReason.Bad_Credential)
                                Init(cancellationTokenSource).Wait();
                        });

                        Console.WriteLine($"Starting DeviceStreamSample on '{_hostName}:{_port}'");
                        await RunSampleAsync(moduleClient, true, cancellationTokenSource);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.UtcNow} - Got an exception: {ex.ToString()}");
                    Console.WriteLine("Waiting 1 minute and trying again...");
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }

        private async Task RunSampleAsync(ModuleClient moduleClient, bool acceptDeviceStreamingRequest, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("Creating DeviceStreamRequest");
            DeviceStreamRequest streamRequest = await moduleClient.WaitForDeviceStreamRequestAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            if (streamRequest != null)
            {
                if (acceptDeviceStreamingRequest)
                {
                    await moduleClient.AcceptDeviceStreamRequestAsync(streamRequest, cancellationTokenSource.Token).ConfigureAwait(false);
                    using (ClientWebSocket webSocket = await DeviceStreamingCommon.GetStreamingClientAsync(streamRequest.Uri, streamRequest.AuthorizationToken, cancellationTokenSource.Token).ConfigureAwait(false))
                    {
                        using (TcpClient tcpClient = new TcpClient())
                        {
                            await tcpClient.ConnectAsync(_hostName, _port).ConfigureAwait(false);
                            using (NetworkStream localStream = tcpClient.GetStream())
                            {
                                Console.WriteLine("Starting streaming");
                                await Task.WhenAny(
                                    HandleIncomingDataAsync(localStream, webSocket, cancellationTokenSource.Token),
                                    HandleOutgoingDataAsync(localStream, webSocket, cancellationTokenSource.Token)
                                ).ConfigureAwait(false);

                                localStream.Close();
                            }
                        }

                        if (webSocket.State == WebSocketState.Open)
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationTokenSource.Token).ConfigureAwait(false);

                        Console.WriteLine($"{DateTime.UtcNow} - Streaming stopped");
                    }
                }
                else
                {
                    await moduleClient.RejectDeviceStreamRequestAsync(streamRequest, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }
    }
}