# Global E-Commerce Order Management Platform

A backend-focused microservices demo built with **.NET 8** for a coding evaluation exercise.  
Six independent APIs communicate via typed HTTP clients (NSwag), with an Orchestration-based Saga, real-time SignalR order status updates, JWT authentication, Polly circuit breaker, structured logging via Serilog/Seq, and a BFF (Backend-for-Frontend) gateway.

---

## How to Run

| Guide | Requires | Script |
|---|---|---|
| [README-Local.md](README-Local.md) | .NET 8 SDK only — no Docker | `.\start-local.ps1` |
| [README-Docker.md](README-Docker.md) | Docker Desktop only — no .NET SDK | `.\start-docker.ps1` |

> **GitHub Codespaces** is covered in both guides — pick the guide that matches which Codespaces devcontainer you select.

---

## Architecture

```
+-------------------------------------------------+
|            Angular App  :4200  (coming soon)    |
+----------------------+-------------------------++
                       |  JWT Bearer + REST + SignalR
+----------------------v--------------------------+
|                   BFF.API  :7000                |
|     Auth . Aggregation . OrderStatusHub         |
+--+----------+----------+----------+------------++
   |          |          |          |          |
   v          v          v          v          v
Product   Order.API  Payment  Shipping  Analytics
 :7001      :7002     :7003    :7004      :7005

Each API: EF Core + SQLite . Swagger UI . Serilog
Order.API: Orchestration Saga . DLQ (file-backed)
Payment.API: Polly circuit breaker
BFF.API logs: Serilog -> Seq (Docker) / files (local)
```

---

## Services at a Glance

| Service | Local (HTTPS) | Docker (HTTP) | API Key |
|---|---|---|---|
| BFF.API | https://localhost:7000/swagger | http://localhost:7000/swagger | `bff-internal-key-123` |
| Product.API | https://localhost:7001/swagger | http://localhost:7001/swagger | `product-api-key-123` |
| Order.API | https://localhost:7002/swagger | http://localhost:7002/swagger | `order-api-key-123` |
| Payment.API | https://localhost:7003/swagger | http://localhost:7003/swagger | `payment-api-key-123` |
| Shipping.API | https://localhost:7004/swagger | http://localhost:7004/swagger | `shipping-api-key-123` |
| Analytics.API | https://localhost:7005/swagger | http://localhost:7005/swagger | `analytics-api-key-123` |
| Seq log viewer | (optional local install) | http://localhost:5341 | — |

---

## API Credentials

### JWT Login — `POST /api/auth/login` (no API key needed)

| Username | Password | Role |
|---|---|---|
| `admin` | `pass` | Admin |
| `user1` | `pass` | Customer |

In Swagger UI click **Authorize** and enter `Bearer <token>`.

### SignalR (order status)

Connect to `wss://<bff-host>/hubs/order-status?access_token=<jwt>`.  
Listen for the `OrderStatusUpdated` event — payload: `{ orderId, status }`.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | .NET 8 — ASP.NET Core Web API |
| ORM | EF Core 8 + SQLite (one DB per service) |
| API docs | Swashbuckle (Swagger UI on every service) |
| HTTP clients | NSwag-generated typed clients with interfaces |
| Resilience | Polly v8 circuit breaker (Payment.API) |
| Auth | JWT (runtime-signed in BFF.API) |
| Real-time | SignalR `OrderStatusHub` |
| Logging | Serilog — File sink + Seq sink |
| Saga | Orchestration-based in Order.API |
| DLQ | File-backed dead letter queue (Order.API) |
| Gateway | BFF pattern (BFF.API) |

---

## Angular Frontend

Angular 21 standalone SPA at `client-app/`. Requires Node.js 18+ to run.

| Route | Access |
|---|---|
| `/login` | Public |
| `/products` | JWT |
| `/checkout/:id` | JWT — AES-256-GCM encrypts the payment token before sending |
| `/orders` | JWT — live status via SignalR |
| `/admin/analytics` | Admin JWT |
| `/admin/logs` | Admin + API key |

See [README-Local.md](README-Local.md) or [README-Docker.md](README-Docker.md) for startup instructions.
