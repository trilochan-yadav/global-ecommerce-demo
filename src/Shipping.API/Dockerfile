FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shipping.API/Shipping.API.csproj src/Shipping.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore "src/Shipping.API/Shipping.API.csproj"

COPY src/Shipping.API/ src/Shipping.API/
COPY src/Shared/ src/Shared/
RUN dotnet publish "src/Shipping.API/Shipping.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/Data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Shipping.API.dll"]
