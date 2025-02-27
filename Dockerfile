#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["DiscordDriverBot/DiscordDriverBot.csproj", "DiscordDriverBot/"]
RUN dotnet restore "DiscordDriverBot/DiscordDriverBot.csproj"
COPY . .
WORKDIR "/src/DiscordDriverBot"
RUN dotnet build "DiscordDriverBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DiscordDriverBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM jun112561/dotnet_with_opencc:1.1.1 AS base
WORKDIR /app
COPY --from=publish /app/publish .

ENV TZ="Asia/Taipei"

STOPSIGNAL SIGQUIT

ENTRYPOINT ["dotnet", "DiscordDriverBot.dll"]