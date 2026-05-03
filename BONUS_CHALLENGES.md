# Bonus Challenges

---

## 1. Optimize for Cost: How would you reduce cloud expenses?

### Problem
Microservices running as always-on containers/VMs are billed 24/7 regardless of traffic. In an e-commerce platform, traffic is highly uneven — spikes during sales events, near-zero overnight.

### Strategies

**Right-size compute**
- Profile CPU/memory per service under realistic load. Product.API and Analytics.API are read-heavy and stateless — they can run on smaller instances or spot/preemptible VMs. Order.API and Payment.API handle money and need reliability guarantees, so they warrant reserved instances.

**Scale-to-zero for low-traffic services**
- Move Analytics.API and Shipping.API to serverless (Azure Container Apps with scale-to-zero, or AWS Lambda behind API Gateway). These are invoked infrequently and do not need a warm process at all times.

**Consolidate the message queue**
- The current in-process `BackgroundService` (MessageProcessorService) works for a demo but in production, replacing it with a managed queue (Azure Service Bus, AWS SQS) means the Order.API worker only runs when there are messages. No messages = no cost.

**Cache aggressively**
- Product catalogue data changes rarely. Add a Redis cache in front of Product.API with a short TTL (60–300 s). This reduces DB reads and allows Product.API replicas to be scaled down during quiet periods.

**Database cost**
- SQLite is demo-only. In production, use a single managed SQL instance with separate schemas per service instead of one RDS instance per service. For Analytics.API, a columnar store (Redshift, BigQuery) on a pay-per-query model is cheaper than a continuously provisioned OLTP DB.

**CDN for the Angular SPA**
- Serve the static Angular bundle from a CDN (CloudFront, Azure CDN). Eliminates the need for a running web server for the front end entirely. Combined with pre-rendering, most product-browsing traffic never hits the BFF.

**Observability cost**
- Log sampling: emit DEBUG logs only in staging. In production, log WARN and above. Ship logs to an S3/Blob bucket, query with Athena/Log Analytics on demand rather than streaming everything into an expensive APM tool 24/7.

---

## 2. Improve Real-Time Updates: Compare WebSockets vs. Kafka for order status

### Current approach
This project uses **SignalR** (WebSockets under the hood) to push `OrderStatusUpdated` events from the BFF hub to the Angular client. The BFF receives a simple HTTP POST from Order.API's `MessageProcessorService` and fans the event out to the relevant SignalR group.

### WebSockets (SignalR) — what it is good for

| Strength | Detail |
|---|---|
| Low latency | Sub-100 ms push to the browser — ideal for live UI updates |
| Stateful per-user | Each browser tab holds a persistent connection; the server knows exactly who to notify |
| Simple model | One hub, named groups (`order-{id}`), and typed messages — trivial to reason about |
| No extra infrastructure | Runs inside the existing ASP.NET Core process; no broker to operate |

**Limitation**: WebSockets are a transport, not a durable message bus. If the BFF crashes between the POST from Order.API and the push to the client, the event is lost. There is no replay, no consumer group, no at-least-once guarantee.

### Apache Kafka — what it adds

| Strength | Detail |
|---|---|
| Durability | Events are persisted to disk with configurable retention (days/weeks). A crashed consumer can replay from its last committed offset |
| Fan-out | Multiple independent consumers can read the same `order-status` topic — analytics pipeline, notification service, CRM sync — without the producer knowing or caring |
| Back-pressure | Producers write at their own rate; consumers process at theirs. A slow analytics consumer does not block the order pipeline |
| Audit log | The topic IS the audit trail. Every status transition is immutable and replayable |

**Limitation**: Kafka does not push to browsers. You still need a WebSocket layer (the SignalR hub) as the last mile to the client. Kafka replaces the internal HTTP POST between microservices, not the browser connection.

### Recommended hybrid architecture

```
Order.API ──publishes──► Kafka topic: order-status
                                │
              ┌─────────────────┼──────────────────┐
              ▼                 ▼                  ▼
        BFF.API            Analytics.API      Audit Service
     (consumer)            (consumer)         (consumer)
         │
    SignalR hub
         │
    Browser client
```

- **Within the platform**: services communicate via Kafka — durable, replayable, decoupled.
- **To the browser**: BFF consumes Kafka, maintains SignalR hub, pushes updates in real time.

This gives you the durability and fan-out of Kafka without losing the low-latency WebSocket experience for the end user.

---

## 3. Handle Data Privacy: How would you comply with GDPR for user data?

### What constitutes personal data in this platform
- Username / email used for login (BFF.API)
- `CustomerId` associated with orders (Order.API)
- Payment tokens, even encrypted (Payment.API)
- Shipment addresses if extended (Shipping.API)
- Click events and conversion stats linked to a customer ID (Analytics.API)

### Key GDPR obligations and implementations

**Right to erasure ("right to be forgotten")**
Add a `DELETE /api/account` endpoint in BFF.API that:
1. Resolves the `CustomerId` from the JWT claim
2. Calls each downstream service to delete or anonymise records for that customer
3. Invalidates the JWT (blacklist or rotate the signing key per-user)

Anonymisation is preferable to hard deletion for analytics — replace `CustomerId` with a null or a sentinel value so aggregate reports remain valid.

**Data minimisation**
The current `ResolveCustomerId` hashes the username to an integer. In production, store only what is needed. Do not log usernames in plaintext — use the hashed `CustomerId` in structured log fields.

**Encryption at rest and in transit**
- All services already communicate over HTTPS (localhost dev certs; proper certs in production via Let's Encrypt or ACM).
- SQLite DBs should be replaced in production with encrypted-at-rest managed databases (RDS with encryption, Azure SQL TDE).
- Payment tokens are AES-GCM encrypted client-side before leaving the browser — this is already implemented.

**Audit logging**
Log who accessed what and when using structured Serilog entries with `CustomerId` (not username). Retain audit logs separately from application logs, with a longer retention policy.

**Data residency**
Deploy to a region within the EU for EU customers. Use separate storage accounts/DB instances per region. Do not replicate personal data across regulatory boundaries without Standard Contractual Clauses.

**Consent and data portability**
Provide a `GET /api/account/export` endpoint returning all data held for the authenticated user as a JSON/CSV download. This satisfies the portability obligation.

**Third-party processors**
If Serilog ships logs to a third-party aggregator (Datadog, Splunk), ensure a Data Processing Agreement is in place and that PII is scrubbed or masked before export using a Serilog destructuring policy.

---

## 4. A/B Testing: How would you design a system to run A/B tests on product pages?

### Goal
Serve variant A or variant B of a product page to different user segments, measure conversion rates (order placed after viewing), and determine a winner with statistical confidence.

### Architecture

**1 — Assignment service**
A lightweight service (or feature flag provider — LaunchDarkly, Azure App Config, Unleash) that deterministically assigns a user to a variant based on a hash of their `CustomerId` or session ID. Deterministic assignment means the same user always sees the same variant within a test window.

```
GET /api/experiments/assignment?experiment=product-page-v2&userId=1234
→ { "variant": "B", "experimentId": "exp-42" }
```

**2 — BFF enriches the response**
The BFF calls the assignment service when serving `GET /api/products/{id}`. It attaches the variant identifier to the product response so Angular knows which layout to render:

```json
{ "id": 3, "name": "...", "price": 14.99, "variant": "B", "experimentId": "exp-42" }
```

**3 — Angular renders the variant**
The product detail component switches layout based on `product.variant`. Both variants are shipped in the same bundle — no separate deployments.

**4 — Analytics tracks exposure and conversion**
- When the product page loads, Angular calls `POST /api/analytics/clicks` with `{ productId, experimentId, variant }` — this records the **exposure**.
- When an order is placed, `MessageProcessorService` logs a conversion via `POST /api/analytics/conversions` with `{ orderId, experimentId, variant }`.

**5 — Analysis**
The Analytics.API aggregates:
- Exposure count per variant
- Conversion count per variant
- Conversion rate = conversions / exposures

Apply a two-proportion z-test to determine statistical significance. Only ship the winning variant permanently once p < 0.05 with sufficient sample size.

### Guardrails
- **Holdout group**: always keep ~5 % of users on the control (variant A) even after launching variant B, to detect long-term regression.
- **Mutex experiments**: use experiment namespacing so a user is not simultaneously enrolled in two conflicting tests.
- **Rollback**: the assignment service is a feature flag — flipping the flag reverts all users to variant A instantly, with no deployment.
- **Data integrity**: record `experimentId` and `variant` on every `ClickEvent` and `ConversionStat` row so historical data remains queryable even after an experiment ends.
