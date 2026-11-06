#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src
COPY ["DiscordDriverBot/DiscordDriverBot.csproj", "DiscordDriverBot/"]
RUN dotnet restore "DiscordDriverBot/DiscordDriverBot.csproj"
COPY . .
WORKDIR "/src/DiscordDriverBot"
RUN dotnet build "DiscordDriverBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordDriverBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-noble AS base
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends opencc && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .

ENV TZ="Asia/Taipei"

STOPSIGNAL SIGQUIT

ENTRYPOINT ["dotnet", "DiscordDriverBot.dll"]