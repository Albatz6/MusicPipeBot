FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

RUN apt-get update && apt-get upgrade -y
RUN apt-get install python3-pip -y
RUN apt install ffmpeg -y
RUN pip3 install spotdl

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MusicPipeBot.csproj", "./"]
RUN dotnet restore "MusicPipeBot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "MusicPipeBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MusicPipeBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

RUN apt update && apt install
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MusicPipeBot.dll"]
