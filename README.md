# <img alt="Kongroo" src="./logo.png" width="40"/> Kongroo.Catalog

Game catalog and user library microservice for FIAP Cloud Games.

## Endpoints

- `POST /games` — Create game (Admin only)
- `GET /games` — List all games
- `GET /games/{id}` — Get game by ID
- `PUT /games/{id}` — Update game (Admin only)
- `DELETE /games/{id}` — Delete game (Admin only)
- `POST /games/{id}/promotions` — Create promotion (Admin only)
- `POST /orders` — Place order
- `GET /orders` — List user orders
- `GET /orders/{id}` — Get order by ID
- `GET /ownerships` — List user game ownerships
- `GET /ownerships/{id}` — Get ownership by ID
- `GET /health` — Health check

## Messaging

When an authenticated user places an order, Catalog publishes `OrderPlacedIntegrationEvent`
through RabbitMQ. The event includes the order id, `UserId`, customer contact fields,
the order total, and `Games[]` entries with each purchased `GameId`, `Price`, and
`Currency`.

## Environment Variables

| Variable                      | Source    | Description                                   |
| ----------------------------- | --------- | --------------------------------------------- |
| `ConnectionStrings__Database` | Secret    | PostgreSQL connection string                  |
| `Jwt__Issuer`                 | ConfigMap | Must match Kongroo.Identity Jwt\_\_Issuer     |
| `Jwt__Audience`               | ConfigMap | Must match Kongroo.Identity Jwt\_\_Audience   |
| `Jwt__SigningKey`             | Secret    | Must match Kongroo.Identity Jwt\_\_SigningKey |
| `RabbitMq__Host`              | ConfigMap | RabbitMQ broker hostname (e.g. `rabbitmq`)    |
| `RabbitMq__User`              | Secret    | RabbitMQ username                             |
| `RabbitMq__Pass`              | Secret    | RabbitMQ password                             |

## Running Locally

```bash
docker compose up postgres rabbitmq -d   # from Kongroo.Orchestration
dotnet run --project src/Kongroo.Catalog
```

## Running Tests

```bash
dotnet test
```
