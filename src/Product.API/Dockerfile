FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Product.API/Product.API.csproj src/Product.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore "src/Product.API/Product.API.csproj"

COPY src/Product.API/ src/Product.API/
COPY src/Shared/ src/Shared/
RUN dotnet publish "src/Product.API/Product.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/Data
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Product.API.dll"]
