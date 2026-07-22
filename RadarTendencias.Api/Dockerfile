FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RadarTendencias.Api.csproj", "./"]
RUN dotnet restore "./RadarTendencias.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "RadarTendencias.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RadarTendencias.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RadarTendencias.Api.dll"]
