# start-docker.ps1
# Starts all services using Docker Compose. Requires Docker Desktop.
# Run from the solution root: .\start-docker.ps1

Write-Host "Global E-Commerce — Docker Compose Launcher" -ForegroundColor Cyan
Write-Host ""
Write-Host "Building images and starting all containers..." -ForegroundColor Yellow
Write-Host "First run pulls base images (~2-3 minutes). Subsequent runs use cache." -ForegroundColor DarkGray
Write-Host ""

docker-compose up --build

# docker-compose up runs in the foreground.
# Press Ctrl+C to stop all containers.
# To stop and remove containers run: docker-compose down
