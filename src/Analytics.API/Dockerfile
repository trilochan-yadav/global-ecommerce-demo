FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Analytics.API/Analytics.API.csproj src/Analytics.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore "src/Analytics.API/Analytics.API.csproj"

COPY src/Analytics.API/ src/Analytics.API/
COPY src/Shared/ src/Shared/
RUN dotnet publish "src/Analytics.API/Analytics.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/Data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Analytics.API.dll"]
