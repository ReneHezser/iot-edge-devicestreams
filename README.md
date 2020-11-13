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

### Environment variables file
Create a file on the edge device(s) which contains three values (remember to create an IoT device and not to use an IoT Edge device):
```json
REMOTE_HOST_NAME=172.18.0.1
REMOTE_PORT=22
IOTHUB_DEVICE_CONN_STRING="HostName=your_iot_hub.azure-devices.net;DeviceId=your_iot_device_id;SharedAccessKey=the_super_secret_key"
```
Save it e.g. as ```/mnt/docker/devicestreams.env```. If you have the file (with individual values) on all edge devices you are rolling this out, every IoT Edge device will be able to use it's own IoT device for the device streams. This is necessary, as only one connection is allowed for one IoT device and you don't want to have shared credentials.

### Deployment (Manifest)
You can use a layered deployment and target it to the edge devices you like.

| Property | Value |
| --- | --- |
| Target Condition | tags.devicestreamproxy=true |
| Module Name | devicestreamsproxy |
| Image URI | renehezser/devicestreamproxy |
| Container Create Options | `"{"HostConfig":{"Binds":["/mnt/docker/devicestreams.env:/secrets/devicestreammodule/.env"]}}"` |
|

### Usage
Remember that you will need the service part for step 4 from the picture at the top. --> [service](https://github.com/Azure-Samples/azure-iot-samples-csharp/tree/master/iot-hub/Quickstarts/device-streams-proxy/service)

At the time of writing I needed to update the referenced NuGet packages to the latest pre-release versions for *Microsoft.Azure.Devices* and change the Url property of DeviceStreamRequest to Uri.