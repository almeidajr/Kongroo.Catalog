# Kongroo.Catalog

Game catalog and user library microservice for FIAP Cloud Games.

## Endpoints

- `POST /catalog/games` — Create game (Admin only)
- `GET /catalog/games` — List all games
- `GET /catalog/games/{id}` — Get game by ID
- `PUT /catalog/games/{id}` — Update game (Admin only)
- `DELETE /catalog/games/{id}` — Delete game (Admin only)
- `POST /catalog/games/{id}/promotions` — Create promotion (Admin only)
- `POST /catalog/orders` — Place order
- `GET /catalog/orders` — List user orders
- `GET /catalog/orders/{id}` — Get order by ID
- `GET /library/ownerships` — List user game ownerships
- `GET /library/ownerships/{id}` — Get ownership by ID
- `GET /health` — Health check

## Environment Variables

| Variable | Source | Description |
|---|---|---|
| `ConnectionStrings__Database` | Secret | PostgreSQL connection string |
| `Jwt__Issuer` | ConfigMap | Must match Kongroo.Identity Jwt__Issuer |
| `Jwt__Audience` | ConfigMap | Must match Kongroo.Identity Jwt__Audience |
| `Jwt__SigningKey` | Secret | Must match Kongroo.Identity Jwt__SigningKey |

## Running Locally

```bash
docker compose up postgres -d   # from Kongroo.Orchestration
dotnet run --project src/Kongroo.Catalog.Api
```

## Running Tests

```bash
dotnet test
```
