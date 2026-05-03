# README-Docker.md — Run With Docker

> **Docker Desktop required.** All services run as containers — no .NET SDK needed.  
> For the no-Docker version see [README-Local.md](README-Local.md).

---

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows / macOS / Linux)  
  No .NET SDK required.

---

## Services

| Service | URL | Swagger | API Key |
|---|---|---|---|
| BFF.API | http://localhost:7000 | http://localhost:7000/swagger | `bff-internal-key-123` |
| Product.API | http://localhost:7001 | http://localhost:7001/swagger | `product-api-key-123` |
| Order.API | http://localhost:7002 | http://localhost:7002/swagger | `order-api-key-123` |
| Payment.API | http://localhost:7003 | http://localhost:7003/swagger | `payment-api-key-123` |
| Shipping.API | http://localhost:7004 | http://localhost:7004/swagger | `shipping-api-key-123` |
| Analytics.API | http://localhost:7005 | http://localhost:7005/swagger | `analytics-api-key-123` |
| Seq (log viewer) | http://localhost:5341 | — | — |

> **HTTP not HTTPS** — containers run on `http://+:80` internally. This is correct Docker behaviour; HTTPS is handled at the reverse proxy layer in production.

---

## Option A — One command (PowerShell)

```powershell
.\start-docker.ps1
```

Runs `docker-compose up --build` with a brief status header. Press `Ctrl+C` to stop all containers.

---

## Option B — docker-compose directly

```bash
# Build images and start all containers (foreground — shows live logs from all services)
docker-compose up --build

# Or run in the background (detached)
docker-compose up --build -d

# View logs while detached
docker-compose logs -f

# Stop all containers
docker-compose down
```

**First run** pulls `sdk:8.0`, `aspnet:8.0`, and `datalust/seq` images — allow ~2–3 minutes.  
**Subsequent runs** use the layer cache and start in seconds.

---

## Option C — GitHub Codespaces (no installs, browser only)

1. Open the repo on GitHub → click **Code → Codespaces → New codespace**
2. When prompted to select a configuration, choose **"Docker - docker-compose"**  
   *(This uses `.devcontainer/docker/devcontainer.json` — includes Docker-in-Docker so `docker-compose` works inside the Codespace)*
3. Once the Codespace loads, open the Terminal (`` Ctrl+` ``) and run:
   ```powershell
   .\start-docker.ps1
   ```
   Or directly:
   ```bash
   docker-compose up --build
   ```
4. Click the **Ports** tab — all services + Seq appear with friendly labels  
   URLs follow the pattern: `https://your-codespace-name-7000.app.github.dev`

> First build inside Codespaces takes ~5–8 minutes (DinD overhead). Subsequent runs are fast.  
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

### Seq UI (recommended)

Open **http://localhost:5341** — real-time structured log viewer with filtering by service, level, and time.

### Raw log files

Log files are bind-mounted from the containers to the host:

| Host path | Service |
|---|---|
| `docker-volumes/logs/ProductAPI-YYYYMMDD.log` | Product.API |
| `docker-volumes/logs/PaymentAPI-YYYYMMDD.log` | Payment.API |
| `docker-volumes/logs/ShippingAPI-YYYYMMDD.log` | Shipping.API |
| `docker-volumes/logs/AnalyticsAPI-YYYYMMDD.log` | Analytics.API |
| `docker-volumes/logs/OrderAPI-YYYYMMDD.log` | Order.API |
| `docker-volumes/logs/BFFAPI-YYYYMMDD.log` | BFF.API |

---

## DLQ (Dead Letter Queue)

Failed saga orders appear as JSON files in `docker-volumes/dlq/` on the host.

---

## Data

SQLite databases are **ephemeral** — they live inside containers and are seeded fresh on every `docker-compose up`. This is by design: containers are stateless. In a real deployment the database would be externalised (PostgreSQL / SQL Server) with its own persistent volume.

---

## Troubleshooting

**Port already in use**
```powershell
# Windows
netstat -ano | findstr :7000
Stop-Process -Id <PID>
```

**`bff` container exits immediately**  
`depends_on` controls start order but not readiness. Wait 10–15 seconds after all containers show as started before sending requests. The BFF retries on the next request.

**Docker build fails on `swagger tofile`**  
Expected and harmless — the `GenerateSwagger` MSBuild target is configured with `ContinueOnError="true"`. It only runs in a local dev build. The image build continues normally.

**Seq shows no logs**  
Wait ~10 seconds — services connect to Seq after startup. If logs still don't appear check `docker-compose logs <service-name>` for Serilog connection errors.

---

## Angular Frontend (optional)

> Requires Node.js 18+ on your host machine. The Angular dev server is not containerised — it proxies to the BFF container at `http://localhost:7000`.

```powershell
# Start the .NET services first
.\start-docker.ps1

# In a separate terminal — install deps (first time only)
cd client-app
npm install

# Start the Angular dev server (uses environment.docker.ts → http://localhost:7000)
ng serve --configuration docker
```

Open **http://localhost:4200** in your browser.

| Credential | Role |
|---|---|
| `admin` / `pass` | Admin |
| `user1` / `pass` | Customer |
