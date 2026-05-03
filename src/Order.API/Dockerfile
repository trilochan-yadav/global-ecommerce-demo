FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Order.API/Order.API.csproj src/Order.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore "src/Order.API/Order.API.csproj"

COPY src/Order.API/ src/Order.API/
COPY src/Shared/ src/Shared/
RUN dotnet publish "src/Order.API/Order.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/Data /app/dlq
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.API.dll"]
