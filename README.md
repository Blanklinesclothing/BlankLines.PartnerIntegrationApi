# BlankLines Partner Integration API

A REST API that allows approved partners to submit and manage orders fulfilled through the BlankLines Shopify store.

## Overview

Partners authenticate via API key and can place orders against BlankLines products. Submitted orders are pushed to Shopify for fulfilment. Partners can also register their own product SKUs, query available products, attach a custom design file per order, and track or cancel their orders.

Before an order is submitted to Shopify, live inventory is checked for each line item. If any item cannot be fulfilled at the requested quantity, the order is rejected immediately with a clear error before anything is persisted.

## Tech Stack

- **.NET 9** - ASP.NET Core Web API
- **Entity Framework Core** with **PostgreSQL** (Npgsql)
- **ShopifySharp** - Shopify Admin API integration
- **Cloudflare R2** - Design file storage (S3-compatible)
- **Scalar** - Interactive OpenAPI reference

## Architecture

Clean architecture layout:

| Project | Responsibility |
|---|---|
| `Api` | Controllers, middleware, OpenAPI transformers, app entry point |
| `Application` | Services, interfaces, DTOs, request/response models |
| `Domain` | Entities and enums |
| `Infrastructure` | EF Core DbContext, Shopify service, R2 storage, migrations |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL instance
- Cloudflare R2 bucket with S3-compatible credentials

### Configuration

Fill in your values in `appsettings.Development.json` or via [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=blanklines;Username=postgres;Password=yourpassword"
  },
  "Admin": {
    "AdminKey": "your-strong-random-admin-key"
  },
  "Shopify": {
    "StoreUrl": "https://your-store.myshopify.com",
    "AccessToken": "your-shopify-admin-api-token",
    "ApiVersion": "2026-01"
  },
  "R2": {
    "AccountId": "your-cloudflare-account-id",
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key",
    "BucketName": "your-bucket-name",
    "PublicUrlBase": "https://your-public-domain-or-r2-url",
    "UploadFolder": "partner-designs"
  }
}
```

> `appsettings.Development.json` is in `.gitignore` and must never be committed.

### Run

```bash
dotnet run --project BlankLines.PartnerIntegrationApi.Api
```

The database is migrated automatically on startup. In development, seed data is applied - two test partners with keys `test-api-key-123` and `test-api-key-456`, each pre-configured with sample partner products.

---

## API Reference

### Partner endpoints (`/api/*`)

The interactive reference for all partner-facing endpoints is at:

**https://api.blanklines.com/scalar/v1**

Available locally at `https://localhost:{port}/scalar/v1` when running in development.

The raw OpenAPI spec is at `/openapi/v1.json`.

#### Browse BlankLines products (`/api/products`)

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/products` | List all active products available in the BlankLines Shopify store |

#### Partner products (`/api/partner-products`)

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/partner-products` | List all products registered under your partner account |
| `POST` | `/api/partner-products` | Register a new product |

**POST request body:**

```json
{
  "partnerSku": "MY-SKU-001",
  "baseSku": "BL-TEE-WHITE-M",
  "designReference": "spring-2025-logo"
}
```

- `partnerSku` - Your own internal SKU. Used when placing orders. Must be unique per partner account.
- `baseSku` - The BlankLines base product SKU. Must exactly match an active variant in the BlankLines Shopify store. A `400` error is returned if the SKU is not found.
- `designReference` - A reference string identifying the design to apply to this product.

`shopifyVariantId` is resolved automatically from the validated `baseSku` and is included in the response.

#### Orders (`/api/orders`)

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/orders` | Submit a new order for fulfilment |
| `GET` | `/api/orders/{partnerOrderId}` | Retrieve the status of an order |
| `POST` | `/api/orders/cancel` | Cancel an order (within 24 hours of submission) |

Before an order is accepted, live inventory is verified for every line item against the BlankLines Shopify store. If any item has insufficient stock a `400` error is returned immediately and nothing is persisted:

```json
{ "error": "Insufficient stock for 'MY-SKU-001': 3 available, 10 requested." }
```

### Admin endpoints (`/admin/*`)

Admin endpoints are excluded from the public spec. Full reference:

**[docs/admin-endpoints.md](docs/admin-endpoints.md)**

---

## Postman

### Partner collection

Import the live OpenAPI spec directly into Postman - no file to maintain:

1. Open Postman > **Import**
2. Select **Link**
3. Enter `https://localhost:{port}/openapi/v1.json`

Postman generates the collection from the spec. Re-import whenever the API changes.

### Admin collection

A hand-maintained collection covering all `/admin/*` endpoints:

```
docs/BlankLines.PartnerIntegrationApi.admin.postman_collection.json
```

Set the `adminKey` collection variable before use.

---

## Authentication

### Partner endpoints (`/api/*`)

```
X-API-KEY: <partner-api-key>
```

Partner API keys are generated via the admin endpoint, stored as SHA-256 hashes, and issued once - they cannot be retrieved again.

### Admin endpoints (`/admin/*`)

```
X-ADMIN-KEY: <admin-key>
```

Configured via `Admin:AdminKey` in app settings / environment variables. Never stored in the database.

---

## Rate Limiting

Partner endpoints are rate-limited to **10 requests per minute** per API key (fixed window):

```json
{ "error": "Too many requests. Please slow down and try again shortly." }
```

---

## Design Files

Attach an optional design image at order creation via the `designFile` form field. Accepted formats: JPEG, PNG, WebP, GIF. Max size: **10 MB**. Files are stored in Cloudflare R2 under `{R2:UploadFolder}/{partnerOrderId}.{ext}`.

---

## Error Responses

All error responses use this shape:

```json
{ "error": "Human-readable error message" }
```

| Status | Condition |
|---|---|
| `400 Bad Request` | Invalid input, business rule violation, or insufficient stock |
| `401 Unauthorized` | Missing or invalid API key |
| `404 Not Found` | Requested resource does not exist |
| `429 Too Many Requests` | Rate limit exceeded |
| `503 Service Unavailable` | Upstream Shopify or R2 failure |
| `500 Internal Server Error` | Unexpected server error |

---

## Railway Deployment

Set the following environment variables on your Railway API service:

| Variable | Description |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Composed from Railway Postgres plugin variables |
| `Admin__AdminKey` | Strong random string - your admin secret |
| `Shopify__StoreUrl` | Shopify store URL |
| `Shopify__AccessToken` | Shopify Admin API access token |
| `Shopify__ApiVersion` | e.g. `2026-01` |
| `R2__AccountId` | Cloudflare account ID |
| `R2__AccessKeyId` | R2 S3-compatible access key |
| `R2__SecretAccessKey` | R2 S3-compatible secret key |
| `R2__BucketName` | R2 bucket name |
| `R2__PublicUrlBase` | Public base URL for uploaded files |
| `R2__UploadFolder` | `partner-designs` (production) |

**PostgreSQL connection string** (Railway variable reference syntax):

```
Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}}
```

---

## Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project BlankLines.PartnerIntegrationApi.Infrastructure \
  --startup-project BlankLines.PartnerIntegrationApi.Api

# Apply pending migrations manually
dotnet ef database update \
  --project BlankLines.PartnerIntegrationApi.Infrastructure \
  --startup-project BlankLines.PartnerIntegrationApi.Api
```

---

## Changelog

See [docs/CHANGELOG.md](docs/CHANGELOG.md).

---

## Partner Documentation

API reference: **https://api.blanklines.com/scalar/v1**

Integration support: [hello@blanklines.com](mailto:hello@blanklines.com)
