FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["CharonGateway/src/CharonGateway/CharonGateway.csproj", "CharonGateway/src/CharonGateway/"]

COPY ["nuget-packages/", "/nuget-packages/"]

RUN if [ -d "/nuget-packages" ] && [ "$(ls -A /nuget-packages/*.nupkg 2>/dev/null)" ]; then \
        dotnet nuget add source /nuget-packages --name local-packages; \
    fi

RUN dotnet restore "CharonGateway/src/CharonGateway/CharonGateway.csproj"

COPY ["CharonGateway/src/CharonGateway/", "CharonGateway/src/CharonGateway/"]

WORKDIR "/src/CharonGateway/src/CharonGateway"
RUN dotnet build "CharonGateway.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/CharonGateway/src/CharonGateway"
RUN dotnet publish "CharonGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CharonGateway.dll"]

