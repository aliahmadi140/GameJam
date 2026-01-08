FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["GameJam.csproj", "./"]
RUN dotnet restore "GameJam.csproj"

COPY . .
RUN dotnet build "GameJam.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GameJam.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

RUN apt-get update && apt-get install -y \
    libicu-dev \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

RUN mkdir -p /app/Uploads && chmod 777 /app/Uploads

ENTRYPOINT ["dotnet", "GameJam.dll"]