FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/BFF.API/BFF.API.csproj src/BFF.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore "src/BFF.API/BFF.API.csproj"

COPY src/BFF.API/ src/BFF.API/
COPY src/Shared/ src/Shared/
RUN dotnet publish "src/BFF.API/BFF.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BFF.API.dll"]
