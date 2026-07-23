FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RadarTendencias.Worker.csproj", "./"]
RUN dotnet restore "./RadarTendencias.Worker.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "RadarTendencias.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RadarTendencias.Worker.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RadarTendencias.Worker.dll"]
