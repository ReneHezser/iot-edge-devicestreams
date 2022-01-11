Deploy the Azure IoT Device Streams sample as IoT Edge module

# Device Streams to securly access SSH/RDP
[Device Streams](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-device-streams-overview) is a feature to access IoT devices via IoT Hub. The IoT Hub acts as a proxy and no direct ingoing connection needs to be established.

![Device Stream Overview](https://docs.microsoft.com/en-us/azure/iot-hub/media/quickstart-device-streams-proxy-c/device-stream-proxy-diagram.png)

*Disclaimer:* Azure IoT Hub currently supports device streams as a preview feature.

Device Streams will currently work with IoT Devices only. This means that you need an additional IoT device registered in IoT Hub for the functionality.

## Using Device Streams on IoT Edge devices
To be able to use device streams on an IoT Edge device, there are two options:
1) Install the device client in addition to IoT Edge
2) deploy a module which contains the device client

This docker container uses the second options (which kind of makes sense) and the [Sample Client](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Quickstarts/device-streams-proxy/device) published in the IoT samples repository. Since the original code requires either environment variables or parameters on execution, I added the option to read the three required values from a local file on the edge device instead of passing them from the deployment manifest. This would result in the same settings for all edge devices or lots of individual deployments.

By default, devicestreams would connect with IoT device credentials (connection string) to an IoT Hub. This means that the credentials need to be available for the module. Of course this is not desirable, as we do not want to store credentials (most of the times).
Fortunately IoT Edge can authenticate internally from a module to the EdgeHub and create a SAS token, that is then used by devicestreams to connect to the IoT Hub.

### Environment variables file
The configuration of the local streaming endpoint (aka SSH server) is set via envireonment variables. They can be set in a deployment or with an environment file. If not set, the values default to the mentioned values below.

Create a file on the edge device(s) which contains two values:
```json
REMOTE_HOST_NAME=172.18.0.1
REMOTE_PORT=22
```
Save it e.g. as ```/mnt/docker/devicestreams.env```. If you have the file (with individual values) on all edge devices you are rolling this out, every IoT Edge device will be able to use it's own IoT device for the device streams. This is necessary, as only one connection is allowed for one IoT device and you don't want to have shared credentials.

### Authenticating via SAS Token
The code is taken and modified from [Azure/event-grid-iot-edge](https://github.com/Azure/event-grid-iot-edge/tree/master/SecurityDaemonClient). I have created a NuGet package, that can be used to generate the token. See [Create SAS Token from within an IoT Edge module](https://blog.hezser.de/posts/create-sas-token-iot-edge-module/).

### Deployment (Manifest)
You can use a layered deployment and target it to the edge devices you like.

| Property | Value |
| --- | --- |
| Target Condition | tags.devicestreamproxy=true |
| Module Name | devicestreamsproxy |
| Image URI | renehezser/devicestreamproxy |
| Container Create Options | `"{"HostConfig":{"Binds":["/mnt/docker/devicestreams.env:/secrets/devicestreammodule/.env"]}}"` |
|

## Usage
Remember that you will need the service part for step 4 from the picture at the top. --> [service](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Quickstarts/device-streams-proxy/service)

**Important**: You need to modify the above samle to support IoT Edge modules by adding and passing the moduleId!

```c#
DeviceStreamResponse result = await serviceClient.CreateStreamAsync(deviceId, moduleId, deviceStreamRequest, CancellationToken.None).ConfigureAwait(false);
```

At the time of writing I needed to update the referenced NuGet packages to the latest pre-release versions (1.32.0-preview-001) for *Microsoft.Azure.Devices* and change the Url property of DeviceStreamRequest to Uri.