FROM mcr.microsoft.com/dotnet/sdk:2.1 as build-env

WORKDIR /app
RUN git clone https://github.com/Azure-Samples/azure-iot-samples-csharp.git edgeproxy 
RUN cd edgeproxy/iot-hub/Quickstarts/device-streams-proxy/device && dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:2.1
WORKDIR /app
COPY --from=build-env /app/edgeproxy/iot-hub/Quickstarts/device-streams-proxy/device/out .
# entrypoint will load variables from file /secrets/devicestreammodule/.env
COPY entrypoint.sh .
RUN ["chmod", "+x", "entrypoint.sh"]

RUN useradd -ms /bin/bash moduleuser
USER moduleuser

ENTRYPOINT [ "/bin/sh", "entrypoint.sh", "dotnet" , "DeviceLocalProxyStreamingSample.dll" ]