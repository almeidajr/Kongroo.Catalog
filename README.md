# <img alt="Kongroo" src="./logo.png" width="40"/> Kongroo.Catalog

Game catalog and user library microservice for FIAP Cloud Games.

## Endpoints

- `POST /games` — Create game (Admin only)
- `GET /games` — List all games
- `GET /games/{id}` — Get game by ID
- `PUT /games/{id}` — Update game (Admin only)
- `DELETE /games/{id}` — Delete game (Admin only)
- `POST /games/{id}/promotions` — Create promotion (Admin only)
- `POST /games/{id}/reviews` — Submit a review (one per customer per game, any authenticated user) — MongoDB
- `GET /games/{id}/reviews` — Average rating, count and latest reviews — MongoDB
- `POST /orders` — Place order
- `GET /orders` — List user orders
- `GET /orders/{id}` — Get order by ID
- `GET /ownerships` — List user game ownerships
- `GET /ownerships/{id}` — Get ownership by ID
- `GET /health` — Health check
- `GET /metrics` — Prometheus metrics

## Messaging

When an authenticated user places an order, Catalog publishes `OrderPlacedIntegrationEvent`
through RabbitMQ. The event includes the order id, `CustomerId`, customer contact fields,
the order total, `Currency`, and `Lines[]` entries with each purchased `GameId` and
`UnitPrice`.

Catalog also consumes `PaymentProcessedIntegrationEvent` from Payments and applies the
payment result to the originating order.

The transport is selected by `Messaging__Transport`: `RabbitMq` (default, used by Docker Compose
and the tests) or `AmazonSqs` (Kubernetes), where the SNS topics are named `kongroo-order-placed`
and `kongroo-payment-processed`.

## Environment Variables

| Variable                                                            | Source                   | Description                                                 |
| -------------------------------------------------------------------- | --------------------------- | --------------------------------------------------------------- |
| `ConnectionStrings__Database`                                       | Secret                   | PostgreSQL connection string                                |
| `Jwt__Issuer`                                                       | ConfigMap                 | Must match Kongroo.Identity Jwt\_\_Issuer                   |
| `Jwt__Audience`                                                     | ConfigMap                 | Must match Kongroo.Identity Jwt\_\_Audience                 |
| `Jwt__SigningKey`                                                   | Secret                   | Must match Kongroo.Identity Jwt\_\_SigningKey                |
| `RabbitMq__Host`                                                    | ConfigMap                 | RabbitMQ broker hostname (e.g. `rabbitmq`)                  |
| `RabbitMq__User`                                                    | Secret                   | RabbitMQ username                                            |
| `RabbitMq__Pass`                                                    | Secret                   | RabbitMQ password                                            |
| `Messaging__Transport`                                              | ConfigMap                 | `RabbitMq` (default, compose/tests) or `AmazonSqs` (k8s)    |
| `Aws__Region`                                                       | ConfigMap                 | AWS region for SQS/SNS when `AmazonSqs` (e.g. `us-east-1`)  |
| `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`   | Secret `aws-credentials` | AWS SDK default credential chain (Learner Lab session)       |
| `Mongo__ConnectionString`                                           | Secret                   | MongoDB connection string (reviews)                          |
| `Mongo__Database`                                                   | ConfigMap                 | MongoDB database name (`kongroo_catalog`)                    |
| `ConnectionStrings__Redis`                                          | Secret                   | Redis endpoint for HybridCache (`redis:6379`)                |

## Observability

`GET /metrics` exposes OpenTelemetry metrics in Prometheus format: ASP.NET Core request duration
(by route and status), HttpClient, .NET runtime and MassTransit publish/consume counters. Scraped
by the Prometheus deployed from Kongroo.Orchestration.

## Caching

Game reads (`GET /games`, `GET /games/{id}`) go through `HybridCache` with Redis as the distributed
tier (5 min) and an in-process tier (1 min). Every game or promotion write evicts the `games` tag.

## Running Locally

```bash
docker compose up postgres rabbitmq -d   # from Kongroo.Orchestration
dotnet run --project src/Kongroo.Catalog
```

## Running Tests

```bash
dotnet test
```
