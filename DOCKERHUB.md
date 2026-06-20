# Kongroo Catalog

Game catalog, promotions, ordering, and library ownership microservice for the
Kongroo platform. Built with ASP.NET Core and PostgreSQL, following
Domain-Driven Design with a transactional outbox that reliably publishes
integration events (e.g. `OrderPlacedIntegrationEvent`) to RabbitMQ via MassTransit.

## Tags

- `latest` — most recent stable release
- `x.y.z`  — specific version (e.g. `0.0.2`)
- `dev`    — in-progress development build

## Quick start

The container listens on port **8080** and requires a PostgreSQL database and a
RabbitMQ broker.

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
| `GET /orders` | Get the authenticated user's orders |
| `GET /orders/{orderId}` | Get an order |
| `POST /orders` | Place an order |
| `GET /ownerships` | Get the authenticated user's library ownerships |
| `GET /ownerships/{ownershipId}` | Get an ownership |
| `GET /health` | Health check |

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

This service validates tokens it did not issue; `Jwt__Issuer`, `Jwt__Audience`,
and `Jwt__SigningKey` must match the Kongroo Identity service exactly.

## Requirements

- A reachable PostgreSQL database
- A reachable RabbitMQ broker

## Source

Part of the Kongroo platform.
