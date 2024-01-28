FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

RUN apt-get update && apt-get upgrade -y
RUN apt-get install python3-pip -y
RUN apt install ffmpeg -y
RUN pip3 install spotdl --break-system-packages
# Other solutions to avoid system & python packages mixing:
# 1. Use apt install python3-spotdl or something similar for system-wide installation
# 2. Use venv like showed below
#RUN python3 -m venv .venv
#RUN . ./.venv/bin/activate && pip install spotdl

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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