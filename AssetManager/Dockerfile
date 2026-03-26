# Build from repo root: docker build -f AssetManager/Dockerfile -t asset-manager .
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY AssetManager/AssetManager.csproj AssetManager/
RUN dotnet restore AssetManager/AssetManager.csproj
COPY AssetManager/ AssetManager/
WORKDIR /src/AssetManager
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssetManager.dll"]
