// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Microsoft.Azure.Devices.Client.Samples
{
    public static class Program
    {
        // Host name or IP address of a service the device will proxy traffic to.
        private static string s_hostName = Environment.GetEnvironmentVariable("REMOTE_HOST_NAME") ?? "172.18.0.1";

        // Port of a service the device will proxy traffic to.
        private static string s_port = Environment.GetEnvironmentVariable("REMOTE_PORT") ?? "22";

        public static int Main(string[] args)
        {
            Console.WriteLine("HostName: " + s_hostName);
            Console.WriteLine("Port: " + s_port);
            if (string.IsNullOrEmpty(s_hostName) || string.IsNullOrEmpty(s_port))
            {
                Console.WriteLine("Please provide a target host and port");
                return 1;
            }
            int port = int.Parse(s_port, CultureInfo.InvariantCulture);

            Console.WriteLine($"{DateTime.UtcNow} - Starting...");
            var deviceStream = new DeviceStreamSample(s_hostName, port);
            deviceStream.Init(new CancellationTokenSource()).GetAwaiter().GetResult();

            Console.WriteLine("Done.\n");
            return 0;
        }
    }
}