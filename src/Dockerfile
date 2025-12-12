FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CharonGateway/CharonGateway.csproj", "CharonGateway/"]
RUN dotnet restore "CharonGateway/CharonGateway.csproj"
COPY . .
WORKDIR "/src/CharonGateway"
RUN dotnet build "CharonGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CharonGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CharonGateway.dll"]

