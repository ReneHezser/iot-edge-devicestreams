FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY src/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ../src ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .

# entrypoint will load variables from file /secrets/devicestreammodule/.env
COPY entrypoint.sh .
RUN ["chmod", "+x", "entrypoint.sh"]

RUN useradd -ms /bin/bash moduleuser
USER moduleuser

ENTRYPOINT [ "/bin/sh", "entrypoint.sh", "dotnet" , "DeviceStreamsClient.dll" ]