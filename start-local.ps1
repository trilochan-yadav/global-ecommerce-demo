# start-local.ps1
# Launches all 6 microservices locally using dotnet run (no Docker needed).
# Run from the solution root: .\start-local.ps1
# Each service opens in its own PowerShell window.

$root = $PSScriptRoot

function Start-Service($name, $dir) {
    $path = Join-Path $root $dir
    Start-Process powershell -ArgumentList "-NoExit", "-Command", `
        "cd '$path'; Write-Host '--- $name ---' -ForegroundColor Cyan; dotnet run --launch-profile https"
    Write-Host "Started $name" -ForegroundColor Green
}

Write-Host "Starting downstream services..." -ForegroundColor Yellow
Start-Service "Product.API"   "src/Product.API"
Start-Service "Payment.API"   "src/Payment.API"
Start-Service "Shipping.API"  "src/Shipping.API"
Start-Service "Analytics.API" "src/Analytics.API"

Write-Host "Waiting 12 seconds for downstream services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 12

Write-Host "Starting Order.API..." -ForegroundColor Yellow
Start-Service "Order.API" "src/Order.API"

Write-Host "Waiting 8 seconds for Order.API to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

Write-Host "Starting BFF.API..." -ForegroundColor Yellow
Start-Service "BFF.API" "src/BFF.API"

Write-Host ""
Write-Host "All services started." -ForegroundColor Cyan
Write-Host "  BFF Swagger:       https://localhost:7000/swagger" -ForegroundColor White
Write-Host "  Product Swagger:   https://localhost:7001/swagger" -ForegroundColor White
Write-Host "  Order Swagger:     https://localhost:7002/swagger" -ForegroundColor White
Write-Host "  Payment Swagger:   https://localhost:7003/swagger" -ForegroundColor White
Write-Host "  Shipping Swagger:  https://localhost:7004/swagger" -ForegroundColor White
Write-Host "  Analytics Swagger: https://localhost:7005/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Logs are written to the logs/ folder at the solution root." -ForegroundColor DarkGray

Write-Host ""
Write-Host "Starting Angular SPA..." -ForegroundColor Yellow
$clientPath = Join-Path $root "client-app"
Start-Process powershell -ArgumentList "-NoExit", "-Command", `
    "cd '$clientPath'; Write-Host '--- Angular SPA ---' -ForegroundColor Cyan; ng serve --open"
Write-Host "Angular dev server starting at http://localhost:4200" -ForegroundColor Green
