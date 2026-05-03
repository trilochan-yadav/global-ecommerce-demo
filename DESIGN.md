# Global E-Commerce Order Management Platform — Design Document

---

## Table of Contents

1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Microservice Boundaries](#microservice-boundaries)
4. [Database Schema](#database-schema)
5. [Security Considerations](#security-considerations)
6. [Scaling Strategy](#scaling-strategy)
7. [Tech Stack](#tech-stack)
8. [Running the Project](#running-the-project)

---

## Overview

A full-stack e-commerce order management platform built with **6 .NET 8 microservices**, a **Backend-For-Frontend (BFF)**, and an **Angular 21 SPA**. Demonstrates production-grade patterns: JWT auth, AES-256-GCM payload encryption, a local saga orchestrator for order processing, SignalR for live order status, Serilog + Seq structured logging, API-key inter-service authentication, rate limiting, and Docker Compose orchestration.

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                         Browser (Angular 21)                         │
│                                                                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────────────────┐  │
│  │ /login   │  │/products │  │/checkout │  │ /orders  /admin/*  │  │
│  └──────────┘  └──────────┘  └──────────┘  └────────────────────┘  │
│                                                                      │
│  shared/services/auth.service.ts   (JWT signals, computed role)      │
│  shared/services/bff.service.ts    (all HTTP calls, single source)   │
│  shared/services/crypto.service.ts (AES-256-GCM via WebCrypto API)  │
│  shared/services/signalr.service.ts(live order status via WS)       │
│  shared/utils/auth.guard.ts        (CanActivateFn / CanMatchFn)      │
│  shared/utils/token.interceptor.ts (attach Bearer on every request)  │
└──────────────────────────────┬───────────────────────────────────────┘
                               │ HTTPS + JWT Bearer
                               │ WebSocket (SignalR)
┌──────────────────────────────▼───────────────────────────────────────┐
│                          BFF.API  (:7000)                            │
│                                                                      │
│  AuthController      POST /api/auth/login                            │
│                      GET  /api/auth/public-key                       │
│  ProductsController  GET  /api/products                              │
│                      GET  /api/products/{id}                         │
│  OrdersController    POST /api/orders   (decrypts payment token)     │
│                      GET  /api/orders                                │
│  AnalyticsController GET  /api/analytics/clicks      [Admin JWT]     │
│                      GET  /api/analytics/conversions [Admin JWT]     │
│  InternalController  GET  /api/internal/logs         [X-Api-Key]     │
│  NotificationCtrl    POST /api/notification           [X-Api-Key]    │
│  OrderStatusHub      WS   /hubs/order-status          [JWT]          │
│                                                                      │
│  Middleware stack:                                                   │
│    JWT validation · Fixed-window rate limit (60/min) · CORS         │
│    API-key middleware on /api/internal/* and /api/notification       │
│  Business layer:                                                     │
│    CryptoService  — AES-256-GCM decrypt                             │
│    AuthService    — HS256 JWT issuance                               │
└────┬──────────────────────────┬────────────────────────────────────┘
     │  X-Api-Key               │  X-Api-Key
     │                          │
┌────▼────────┐        ┌────────▼─────────────────────────────────────┐
│ Product.API │        │  Order.API  (:7002)                           │
│  (:7001)    │        │                                               │
│             │        │  POST /api/orders  →  202 Accepted           │
│ GET  /prods │        │  GET  /api/orders/{id}                        │
│ GET  /prods/│        │                                               │
│        {id} │        │  Background saga  (IHostedService):           │
│ PATCH /stock│        │    1. PATCH  Product.API  /stock              │
└─────────────┘        │    2. POST   Payment.API  /payments/process   │
                       │    3. POST   Shipping.API /shipments          │
                       │    4. POST   Analytics.API /conversions       │
                       │    5. POST   BFF.API /notification (SignalR)  │
                       │                                               │
                       │  IMessageQueue → LocalMessageQueue            │
                       │  (Channel<T>, swap for Service Bus/RabbitMQ) │
                       └───────────────────────────────────────────────┘

 Payment.API (:7003)         Shipping.API (:7004)       Analytics.API (:7005)
 POST /payments/process      POST /shipments            POST /clicks
 GET  /payments/{id}         GET  /shipments/{orderId}  GET  /clicks
 tok_valid_* → success                                  POST /conversions
 tok_fail_*  → 402 Declined                            GET  /conversions

 Seq (:5341) — all 6 services + BFF write structured logs via Serilog
```

### Request Flow — Place Order

```
1. Angular encrypts payment token via WebCrypto API
     randomIV (12 bytes) + AES-256-GCM encrypt → "base64(iv):base64(ciphertext)"
2. POST /api/orders  →  BFF validates JWT, checks rate limit
3. BFF decrypts token  →  plain token recovered server-side
4. BFF → POST Order.API /api/orders  (X-Api-Key)  →  202 Accepted
5. Order.API persists order (Status = Pending), enqueues saga message
6. Background MessageProcessorService dequeues and executes saga:
     a. PATCH  Product.API  /stock           reserve stock
     b. POST   Payment.API  /process         simulate charge
     c. POST   Shipping.API /shipments       create shipment + tracking #
     d. POST   Analytics.API /conversions    log conversion event
     e. POST   BFF /notification             SignalR group push to browser
7. Browser receives live OrderStatusUpdated event via WebSocket
```

---

## Microservice Boundaries

### BFF.API — port 7000

Single entry point for the Angular SPA. Owns JWT issuance, AES-GCM decryption, and all fan-out to downstream services. No domain business logic — pure orchestration and security gateway.

| Endpoint | Auth | Description |
|---|---|---|
| `POST /api/auth/login` | None | Issues JWT (HS256, 480 min). Claims: `name`, `role` |
| `GET /api/auth/public-key` | None | Returns base64 AES-256 key for browser encryption |
| `GET /api/products` | JWT | Proxies Product.API |
| `GET /api/products/{id}` | JWT | Proxies Product.API |
| `POST /api/orders` | JWT | Decrypts `paymentToken`, proxies Order.API |
| `GET /api/orders` | JWT | Proxies Order.API |
| `GET /api/analytics/clicks` | JWT (Admin role) | Proxies Analytics.API |
| `GET /api/analytics/conversions` | JWT (Admin role) | Proxies Analytics.API |
| `GET /api/internal/logs` | X-Api-Key | Reads Serilog rolling log files by service name |
| `POST /api/notification` | X-Api-Key | Fires SignalR `OrderStatusUpdated` group message |
| `WS /hubs/order-status` | JWT (query-string) | SignalR hub — `JoinOrderGroup` / `LeaveOrderGroup` |

**Rate limiting:** Fixed-window — 60 requests / 1 minute / client. Returns `429`.  
**CORS:** Explicit allowlist `http://localhost:4200`, credentials required, no wildcards.

---

### Product.API — port 7001

Product catalogue and stock management. Auth: X-Api-Key on all endpoints.

| Endpoint | Description |
|---|---|
| `GET /api/products` | List all products |
| `GET /api/products/{id}` | Get single product by id |
| `PATCH /api/products/{id}/stock` | Increment or decrement stock quantity |

---

### Order.API — port 7002

Order lifecycle and async saga orchestration. Auth: X-Api-Key.

| Endpoint | Description |
|---|---|
| `POST /api/orders` | Create order → 202 Accepted, saga enqueued |
| `GET /api/orders/{id}` | Get order by id |

**Saga:** `MessageProcessorService` (`IHostedService`) reads from `IMessageQueue` (backed by `Channel<T>`). Calls four downstream services in sequence; updates order status at each step; pushes final status via BFF notification endpoint.

---

### Payment.API — port 7003

Simulated payment processing. Auth: X-Api-Key.

| Endpoint | Description |
|---|---|
| `POST /api/payments/process` | Process payment — token prefix determines outcome |
| `GET /api/payments/{id}` | Get payment record |

**Simulation:** `tok_valid_*` → `PaymentStatus.Succeeded`; `tok_fail_*` → `PaymentDeclinedException` (HTTP 402); circuit-breaker open → HTTP 503.

---

### Shipping.API — port 7004

Creates and tracks shipments. Auth: X-Api-Key.

| Endpoint | Description |
|---|---|
| `POST /api/shipments` | Create shipment, auto-assign tracking number |
| `GET /api/shipments/{orderId}` | Get shipment by order id |

---

### Analytics.API — port 7005

Append-only event store for product engagement and order conversions. Auth: X-Api-Key.

| Endpoint | Description |
|---|---|
| `POST /api/analytics/clicks` | Log a product click event |
| `GET /api/analytics/clicks` | List all click events |
| `POST /api/analytics/conversions` | Log an order conversion |
| `GET /api/analytics/conversions` | List all conversions |

---

## Database Schema

Each microservice owns its own **SQLite** database (one file per service, EF Core 8 with migrations). No shared database — cross-service data access is HTTP-only. For production, replace SQLite with PostgreSQL using the same EF Core migrations and the `Npgsql` provider.

### Product.API — `Products.db`

```sql
CREATE TABLE Products (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Name          TEXT    NOT NULL,
    Price         DECIMAL NOT NULL,
    StockQuantity INTEGER NOT NULL
);
```

### Order.API — `Orders.db`

```sql
CREATE TABLE Orders (
    Id          INTEGER  PRIMARY KEY AUTOINCREMENT,
    CustomerId  INTEGER  NOT NULL,
    ProductId   INTEGER  NOT NULL,
    Quantity    INTEGER  NOT NULL,
    TotalAmount DECIMAL  NOT NULL,
    Status      INTEGER  NOT NULL,   -- 0=Pending 1=PaymentFailed
                                     -- 2=PaymentProcessed 3=Shipped 4=Failed
    CreatedAt   DATETIME NOT NULL
);
```

### Payment.API — `Payments.db`

```sql
CREATE TABLE Payments (
    Id        INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderId   INTEGER  NOT NULL,
    Amount    DECIMAL  NOT NULL,
    Status    INTEGER  NOT NULL,   -- 0=Pending 1=Succeeded 2=Failed
    CreatedAt DATETIME NOT NULL
);
```

### Shipping.API — `Shipments.db`

```sql
CREATE TABLE Shipments (
    Id             INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderId        INTEGER  NOT NULL,
    TrackingNumber TEXT     NOT NULL,
    Status         INTEGER  NOT NULL,   -- 0=Created 1=Dispatched 2=Delivered
    CreatedAt      DATETIME NOT NULL
);
```

### Analytics.API — `Analytics.db`

```sql
CREATE TABLE ClickEvents (
    Id        INTEGER  PRIMARY KEY AUTOINCREMENT,
    UserId    INTEGER  NOT NULL,
    ProductId INTEGER  NOT NULL,
    EventType TEXT     NOT NULL,
    CreatedAt DATETIME NOT NULL
);

CREATE TABLE ConversionStats (
    Id         INTEGER  PRIMARY KEY AUTOINCREMENT,
    OrderId    INTEGER  NOT NULL,
    CustomerId INTEGER  NOT NULL,
    CreatedAt  DATETIME NOT NULL
);
```

---

## Security Considerations

### Authentication & Authorisation

| Concern | Implementation |
|---|---|
| Browser → BFF | JWT Bearer HS256. Key: 32-char secret in `appsettings.json`. 480-min expiry. Claims: `name`, `role` (Admin / Customer) |
| BFF → Microservices | Per-service X-Api-Key injected as default request header on named `HttpClient` instances |
| Admin routes (Angular) | `adminGuard` is `CanMatchFn` — the admin lazy chunk is **never downloaded** unless `role === 'Admin'`. Guards run before the module download |
| Admin endpoints (BFF) | JWT `role` claim validated server-side on every proxied analytics request |
| SignalR upgrade | JWT passed as `?access_token=` query string on WebSocket upgrade; validated in `JwtBearerEvents.OnMessageReceived` |

### AES-256-GCM Payment Token Encryption

Payment tokens are sensitive. The plain token never travels over the external network:

```
Browser                              BFF
  │                                   │
  │── GET /api/auth/public-key ──────►│
  │◄─ base64(32-byte AES key) ────────│
  │                                   │
  │  importKey(AES-GCM, 256)          │
  │  randomIV = getRandomValues(12)   │
  │  ct = encrypt(plainToken, IV)     │
  │                                   │
  │── POST /api/orders ───────────────►│
  │   { paymentToken: "iv:ct" }        │── decrypt(iv, ct) ──► plainToken
  │                                   │── POST Order.API (plainToken)
```

In production the AES key would be an **Azure Key Vault** reference rather than `appsettings.json`.

### Rate Limiting

BFF enforces a **fixed-window limiter**: 60 requests / 1 minute / client → `429 Too Many Requests`. Protects `/api/auth/login` from credential stuffing and product/order endpoints from scraping. Queue depth: 5.

### OWASP Top 10 Coverage

| Risk | Mitigation |
|---|---|
| A01 Broken Access Control | Role validated in both Angular `CanMatchFn` guard and BFF JWT middleware |
| A02 Cryptographic Failures | AES-256-GCM E2E for payment tokens; JWT signed with 32-char key |
| A03 Injection | EF Core parameterised queries throughout; no raw SQL |
| A04 Insecure Design | Saga failures update order status to `Failed`; no silent data loss |
| A05 Security Misconfiguration | No default credentials in code; all secrets via config/env vars |
| A07 Auth Failures | `ValidateLifetime = true`; token not stored server-side |
| A09 Insufficient Logging | Serilog structured logs on every request, warning, and error across all 7 services |

---

## Scaling Strategy

### Horizontal Scaling

All services are stateless at the application layer. Replace SQLite with PostgreSQL to enable multiple replicas per service:

```
                    ┌────────────────────────────┐
                    │    Load Balancer / API GW   │
                    └──────────┬─────────┬────────┘
                               │         │
                        ┌──────▼──┐ ┌────▼────┐
                        │BFF  [1] │ │BFF  [2] │   ← sticky sessions or
                        └──┬──────┘ └──┬──────┘     Redis SignalR backplane
                           │           │
              ┌────────────┴───────────┴────────────┐
              │  Product · Order · Payment ·         │  N replicas each,
              │  Shipping · Analytics APIs           │  scaled independently
              └─────────────────────────────────────-┘
```

### SignalR

- **Current:** single-node in-process hub  
- **Production:** `AddSignalR().AddStackExchangeRedis(connectionString)` or **Azure SignalR Service** — no hub code changes required

### Message Queue

- **Current:** `IMessageQueue` → `LocalMessageQueue` (`Channel<T>`, in-process)  
- **Production:** swap to **Azure Service Bus** or **RabbitMQ** behind the same `IMessageQueue` interface — zero changes to saga business logic

### Database

| Demo | Production |
|---|---|
| SQLite (one file per service) | PostgreSQL per service |
| EF Core + SQLite provider | Same EF Core migrations + `Npgsql` provider |
| No connection pooling | PgBouncer |

### Caching

Product catalogue is read-heavy and changes infrequently. Add behind `IProductService`:

- **Single node:** `IMemoryCache`  
- **Multi-node:** `IDistributedCache` backed by Redis  

No interface or controller changes — purely an implementation swap.

### Observability

| Layer | Current | Production |
|---|---|---|
| Structured logging | Serilog → rolling file + Seq (:5341) | Elastic Stack / Azure Monitor |
| Metrics | — | Prometheus + Grafana |
| Distributed tracing | — | OpenTelemetry → Jaeger / Zipkin |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 21, TypeScript, Bootstrap 5, `@microsoft/signalr` |
| BFF | .NET 8, ASP.NET Core, JWT Bearer, SignalR, Serilog |
| Microservices | .NET 8, ASP.NET Core, EF Core 8, SQLite |
| Authentication | JWT HS256 (server) · AES-256-GCM (E2E, WebCrypto + .NET) |
| Logging | Serilog → rolling file + Seq |
| Containerisation | Docker · Docker Compose |
| API Clients | NSwag-generated typed HTTP clients (per downstream service) |
| Package management | npm (Angular) · NuGet (.NET) |

---

## Running the Project

### Docker (recommended)

```bash
docker-compose up --build
```

| Service | URL |
|---|---|
| Angular SPA | http://localhost:4200 |
| BFF.API + Swagger | https://localhost:7000/swagger |
| Product.API + Swagger | https://localhost:7001/swagger |
| Order.API + Swagger | https://localhost:7002/swagger |
| Payment.API + Swagger | https://localhost:7003/swagger |
| Shipping.API + Swagger | https://localhost:7004/swagger |
| Analytics.API + Swagger | https://localhost:7005/swagger |
| Seq log UI | http://localhost:5341 |

### Local (without Docker)

```powershell
./start-local.ps1
```

Starts all 6 .NET APIs and the Angular dev server concurrently.

### Demo Credentials

| Username | Password | Role | Access |
|---|---|---|---|
| `admin` | `pass` | Admin | All pages including Analytics + Logs |
| `user1` | `pass` | Customer | Products, Checkout, Orders |