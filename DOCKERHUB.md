# Kongroo Catalog

Game catalog, promotions, ordering, and library ownership microservice for the
Kongroo platform. Built with ASP.NET Core and PostgreSQL, following
Domain-Driven Design with a transactional outbox that reliably publishes
integration events (e.g. `OrderPlacedIntegrationEvent`) via MassTransit. The
transport is selected by `Messaging__Transport`: RabbitMQ by default, or Amazon
SQS/SNS in Kubernetes — Catalog publishes `kongroo-order-placed` and consumes
`kongroo-payment-processed`. Game reads are cached in Redis through
`HybridCache` (5 minutes distributed, 1 minute in-process, evicted on every
game or promotion write), and reviews are stored in MongoDB, one per customer
per game.

## Tags

- `latest` — most recent stable release
- `x.y.z`  — specific version (e.g. `0.1.0`)
- `dev`    — in-progress development build

## Quick start

The container listens on port **8080** and requires a PostgreSQL database, a
messaging broker (RabbitMQ or Amazon SQS/SNS), a MongoDB database (reviews),
and Redis (game read caching).

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__Database="Host=postgres;Database=kongroo_catalog;Username=kongroo;Password=development" \
  -e RabbitMq__Host="rabbitmq" \
  -e RabbitMq__User="kongroo" \
  -e RabbitMq__Pass="development" \
  -e Jwt__Issuer="Kongroo.Identity.Api" \
  -e Jwt__Audience="Kongroo.Identity.Api" \
  -e Jwt__SigningKey="<a-secret-key-at-least-32-characters-long>" \
  josealmeidajr/kongroo-catalog:latest
```

## Endpoints

| Method & path | Description |
|---|---|
| `POST /games` | Create a game (Admin only) |
| `GET /games` | Get games |
| `GET /games/{gameId}` | Get a game |
| `PUT /games/{gameId}` | Update a game (Admin only) |
| `POST /games/{gameId}/promotions` | Create a promotion (Admin only) |
| `DELETE /games/{gameId}` | Delete a game (Admin only) |
| `POST /games/{gameId}/reviews` | Submit a review (one per customer per game) |
| `GET /games/{gameId}/reviews` | Average rating, count and latest reviews |
| `GET /orders` | Get the authenticated user's orders |
| `GET /orders/{orderId}` | Get an order |
| `POST /orders` | Place an order |
| `GET /ownerships` | Get the authenticated user's library ownerships |
| `GET /ownerships/{ownershipId}` | Get an ownership |
| `GET /health` | Health check |
| `GET /metrics` | Prometheus metrics (unauthenticated, scraped in-cluster) |

## Configuration

Configured via environment variables. The double underscore (`__`) maps to
nested configuration sections.

| Variable | Description |
|---|---|
| `ConnectionStrings__Database` | PostgreSQL connection string |
| `RabbitMq__Host` | RabbitMQ broker hostname |
| `RabbitMq__User` | RabbitMQ username |
| `RabbitMq__Pass` | RabbitMQ password |
| `Jwt__Issuer` | JWT issuer (must match the Identity service) |
| `Jwt__Audience` | JWT audience (must match the Identity service) |
| `Jwt__SigningKey` | JWT signing key (min 32 chars, must match the Identity service) |
| `Jwt__AccessTokenLifetimeMinutes` | Access token lifetime in minutes |
| `OutboxProcessing__PollingInterval` | Outbox poll interval (e.g. `00:00:05`) |
| `OutboxProcessing__BatchSize` | Outbox messages processed per poll |
| `Messaging__Transport` | `RabbitMq` (default, used by Docker Compose and the tests) or `AmazonSqs` (Kubernetes) |
| `Aws__Region` | AWS region for SQS/SNS, required when `Messaging__Transport=AmazonSqs` (e.g. `us-east-1`) |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_SESSION_TOKEN` | AWS SDK default credential chain, supplied by the `aws-credentials` Secret |
| `Mongo__ConnectionString` | MongoDB connection string (reviews) |
| `Mongo__Database` | MongoDB database name |
| `ConnectionStrings__Redis` | Redis endpoint for HybridCache |

This service validates tokens it did not issue; `Jwt__Issuer`, `Jwt__Audience`,
and `Jwt__SigningKey` must match the Kongroo Identity service exactly.

## Requirements

- A reachable PostgreSQL database
- A reachable messaging broker (RabbitMQ or Amazon SQS/SNS, per `Messaging__Transport`)
- A reachable MongoDB database (reviews)
- A reachable Redis instance (game read caching)

## Source

Part of the Kongroo platform.
