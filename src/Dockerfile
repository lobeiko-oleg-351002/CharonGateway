FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CharonGateway.sln", "."]
COPY ["CharonGateway/CharonGateway.csproj", "CharonGateway/"]

# Restore dependencies
RUN dotnet restore "CharonGateway/CharonGateway.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/CharonGateway"
RUN dotnet build "CharonGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CharonGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "CharonGateway.dll"]

