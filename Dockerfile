#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Discord Driver Bot/Discord Driver Bot.csproj", "Discord Driver Bot/"]
RUN dotnet restore "Discord Driver Bot/Discord Driver Bot.csproj"
COPY . .
WORKDIR "/src/Discord Driver Bot"
RUN dotnet build "Discord Driver Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Discord Driver Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .

ENV TZ="Asia/Taipei"

STOPSIGNAL SIGQUIT

ENTRYPOINT ["dotnet", "Discord Driver Bot.dll"]