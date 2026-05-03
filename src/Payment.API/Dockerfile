FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Payment.API/Payment.API.csproj src/Payment.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore "src/Payment.API/Payment.API.csproj"

COPY src/Payment.API/ src/Payment.API/
COPY src/Shared/ src/Shared/
RUN dotnet publish "src/Payment.API/Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/Data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
