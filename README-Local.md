# README-Local.md — Run Without Docker

> **No Docker needed.** Uses `dotnet run` directly. Works on your local machine or in GitHub Codespaces.  
> For the Docker Desktop version see [README-Docker.md](README-Docker.md).

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Trust the dev HTTPS certificate once (local machine only):
  ```bash
  dotnet dev-certs https --trust
  ```

---

## Services

| Service | URL | Swagger | API Key |
|---|---|---|---|
| BFF.API | https://localhost:7000 | https://localhost:7000/swagger | `bff-internal-key-123` |
| Product.API | https://localhost:7001 | https://localhost:7001/swagger | `product-api-key-123` |
| Order.API | https://localhost:7002 | https://localhost:7002/swagger | `order-api-key-123` |
| Payment.API | https://localhost:7003 | https://localhost:7003/swagger | `payment-api-key-123` |
| Shipping.API | https://localhost:7004 | https://localhost:7004/swagger | `shipping-api-key-123` |
| Analytics.API | https://localhost:7005 | https://localhost:7005/swagger | `analytics-api-key-123` |

---

## Option A — One command (PowerShell)

```powershell
.\start-local.ps1
```

Opens all 6 services in separate PowerShell windows in the correct startup order. Wait for all windows to show `Application started` before sending requests.

---

## Option B — Manual (any terminal)

Run each command in a **separate terminal**, in this order:

```bash
# Step 1 — Start downstream services (any order among themselves)
cd src/Product.API   && dotnet run --launch-profile https
cd src/Payment.API   && dotnet run --launch-profile https
cd src/Shipping.API  && dotnet run --launch-profile https
cd src/Analytics.API && dotnet run --launch-profile https

# Step 2 — After ~12 seconds, start Order.API
cd src/Order.API     && dotnet run --launch-profile https

# Step 3 — After ~8 seconds, start BFF.API
cd src/BFF.API       && dotnet run --launch-profile https
```

---

## Option C — GitHub Codespaces (no installs, browser only)

1. Open the repo on GitHub → click **Code → Codespaces → New codespace**
2. When prompted to select a configuration, choose **"Local - dotnet run (no Docker)"**  
   *(This uses `.devcontainer/local/devcontainer.json` — .NET 8 SDK, no Docker required)*
3. Once the Codespace loads, open the Terminal (`` Ctrl+` ``) and run:
   ```powershell
   pwsh start-local.ps1
   ```
4. Click the **Ports** tab — all 6 services appear with friendly labels  
   URLs follow the pattern: `https://your-codespace-name-7000.app.github.dev`

> Codespaces gives **60 free hours/month** on the free tier.

---

## API Credentials

### JWT Login

`POST /api/auth/login` — no API key needed.

| Username | Password | Role |
|---|---|---|
| `admin` | `pass` | Admin |
| `user1` | `pass` | Customer |

In Swagger UI click **Authorize** and enter `Bearer <token>`.

---

## Logs

Log files are written to the `logs/` folder at the solution root — one rolling file per service, daily rotation, 7-day retention.

| File | Service |
|---|---|
| `logs/ProductAPI-YYYYMMDD.log` | Product.API |
| `logs/PaymentAPI-YYYYMMDD.log` | Payment.API |
| `logs/ShippingAPI-YYYYMMDD.log` | Shipping.API |
| `logs/AnalyticsAPI-YYYYMMDD.log` | Analytics.API |
| `logs/OrderAPI-YYYYMMDD.log` | Order.API |
| `logs/BFFAPI-YYYYMMDD.log` | BFF.API |

> Seq is not available in this mode. If you have Seq installed at `http://localhost:5341` it will receive logs automatically — otherwise Serilog skips that sink silently.

---

## DLQ (Dead Letter Queue)

Failed saga orders (payment declined after retries) are written as JSON files to the `dlq/` folder at the solution root.

---

## Troubleshooting

**Port already in use**
```powershell
netstat -ano | findstr :7000
Stop-Process -Id <PID>
```

**HTTPS certificate not trusted**  
Run `dotnet dev-certs https --trust` and restart your browser.

**Services can't reach each other**  
Ensure all 6 terminals show `Application started` before placing orders. Order.API must start after the 4 downstream services are ready.

**Codespaces: `pwsh` not found**  
Use `dotnet run` manually (Option B) — or install PowerShell: `sudo apt-get install -y powershell`

---

## Angular Frontend (optional)

> Requires Node.js 18+ and Angular CLI 17+.  
> The frontend calls the BFF at `https://localhost:7000` — start all 6 .NET services first.

```powershell
# Install dependencies (first time only)
cd client-app
npm install

# Start the dev server
ng serve
```

Open **http://localhost:4200** in your browser.

| Credential | Role | Can access |
|---|---|---|
| `admin` / `pass` | Admin | Products, Orders, Analytics, Logs |
| `user1` / `pass` | Customer | Products, Orders |

> **HTTPS note:** The dev server talks to `https://localhost:7000`. If your browser blocks the self-signed cert, visit `https://localhost:7000` directly once and click "Proceed" to trust it, then return to the Angular app.
