# BlankLines Partner Integration API

A REST API that allows approved partners to submit and manage orders fulfilled through the BlankLines Shopify store.

## Overview

Partners authenticate via API key and can place orders against BlankLines products. Submitted orders are pushed to Shopify for fulfilment. Partners can also query available products, attach a custom design file per order, and track or cancel their orders.

## Tech Stack

- **.NET 9** - ASP.NET Core Web API
- **Entity Framework Core** with **PostgreSQL** (Npgsql)
- **ShopifySharp** - Shopify Admin API integration
- **Cloudflare R2** - Design file storage (S3-compatible)
- **Scalar** - OpenAPI documentation UI

## Architecture

Clean architecture layout:

| Project | Responsibility |
|---|---|
| `Api` | Controllers, middleware, app entry point |
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

The database is migrated automatically on startup. In development, seed data is also applied (two test partners with keys `test-api-key-123` and `test-api-key-456`, each pre-configured with sample partner products).

### API Docs

The interactive API reference is publicly available at:

**https://api.blanklines.com/scalar/v1**

Also available locally at `https://localhost:{port}/scalar/v1` when running in development.

---

## Authentication

### Partner endpoints (`/api/*`)

All partner endpoints require the `X-API-KEY` header:

```
X-API-KEY: <partner-api-key>
```

Partner API keys are generated via the admin endpoint (see below), stored as SHA-256 hashes in the database, and issued to partners once - they cannot be retrieved again.

### Admin endpoints (`/admin/*`)

All admin endpoints require the `X-ADMIN-KEY` header:

```
X-ADMIN-KEY: <admin-key>
```

The admin key is a random string stored in the `Admin:AdminKey` config value / environment variable. It never touches the database.

---

## Rate Limiting

Partner endpoints are rate-limited to **10 requests per minute** per API key (fixed-window). Requests that exceed this limit receive a `429 Too Many Requests` response:

```json
{ "error": "Too many requests. Please slow down and try again shortly." }
```

---

## Partner Endpoints

### Products

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/products` | List all products available to order |

#### Product object

```json
{
  "id": "shopify-product-id",
  "title": "Classic Tank Top",
  "sku": "M106GV100-S",
  "variantId": 49420784042305,
  "inventoryQuantity": 42
}
```

---

### Orders

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/orders` | Submit a new order (`multipart/form-data`) |
| `GET` | `/api/orders/{partnerOrderId}` | Get order status and details |
| `POST` | `/api/orders/cancel` | Cancel an order |

#### `POST /api/orders` - form fields

The request must be sent as `multipart/form-data`. The upload size limit is **10 MB**.

| Field | Type | Required | Description |
|---|---|---|---|
| `partnerOrderId` | string | Yes | Your unique order reference |
| `deliveryMethod` | string | Yes | `Shipping` or `Pickup` |
| `itemsJson` | string (JSON) | Yes | Array of order items (see below) |
| `customerJson` | string (JSON) | Yes | Customer details (see below) |
| `shippingAddressJson` | string (JSON) | When `deliveryMethod` is `Shipping` | Shipping address details (see below) |
| `designFile` | file | No | Design image - JPEG, PNG, WebP, or GIF |

**`itemsJson`** - array of items using your partner SKUs:

```json
[
  { "partnerSku": "PARTNER1-TANK-BLACK", "quantity": 1 }
]
```

Each `partnerSku` must be a SKU registered against your partner account. The API maps it to the corresponding Shopify variant automatically.

**`customerJson`**:

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phone": "+61400000000"
}
```

**`shippingAddressJson`** (required when `deliveryMethod` is `Shipping`):

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "address1": "123 Main St",
  "address2": "Apt 4",
  "city": "Sydney",
  "province": "NSW",
  "country": "Australia",
  "zip": "2000",
  "phone": "+61400000000"
}
```

**Response - 201 Created**:

```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "partnerOrderId": "your-order-ref"
}
```

---

#### `GET /api/orders/{partnerOrderId}` - order status

**Response - 200 OK**:

```json
{
  "partnerOrderId": "your-order-ref",
  "shopifyOrderId": "5678901234",
  "status": "Processing",
  "deliveryMethod": "Shipping",
  "createdAt": "2026-03-19T10:00:00Z",
  "customer": {
    "firstName": "Jane",
    "lastName": "Doe",
    "email": "jane.doe@example.com",
    "phone": "+61400000000"
  },
  "shippingAddress": {
    "firstName": "Jane",
    "lastName": "Doe",
    "address1": "123 Main St",
    "address2": "Apt 4",
    "city": "Sydney",
    "province": "NSW",
    "country": "Australia",
    "zip": "2000",
    "phone": "+61400000000"
  },
  "designFileUrl": "https://your-public-domain/partner-designs/your-order-ref.png",
  "items": [
    { "partnerSku": "PARTNER1-TANK-BLACK", "quantity": 1 }
  ]
}
```

`shippingAddress` and `designFileUrl` are `null` when not applicable.

#### Order statuses

| Status | Meaning |
|---|---|
| `Received` | Order accepted and queued |
| `Processing` | Submitted to Shopify |
| `Cancelled` | Cancelled by partner |

---

#### `POST /api/orders/cancel` - cancel an order

Orders can only be cancelled within **24 hours** of submission. If the order has already been submitted to Shopify it will also be cancelled there.

**Request body**:

```json
{ "partnerOrderId": "your-order-ref" }
```

**Response - 204 No Content**

---

### Health check

```
GET /health
```

---

## Admin Endpoints

Admin endpoints are excluded from the public API docs and protected by `X-ADMIN-KEY`.

### List all orders

```
GET /admin/orders
X-ADMIN-KEY: <admin-key>
```

Returns all orders across all partners.

---

### Create partner

```
POST /admin/partners
X-ADMIN-KEY: <admin-key>
Content-Type: application/json

{ "name": "Acme Clothing" }
```

**Response - 201 Created**

```json
{
  "partnerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Acme Clothing",
  "apiKey": "aB3xK9mP2qR7sT4uV6wY1zA8bC5dE0fG",
  "createdAt": "2026-03-19T10:00:00Z",
  "note": "Save this API key - it will not be shown again."
}
```

The `apiKey` is generated from a cryptographically secure random source and returned **once only**. Copy it and send it to the partner - it cannot be retrieved again.

---

### List partners

```
GET /admin/partners
X-ADMIN-KEY: <admin-key>
```

Returns all active (non-revoked) partners.

---

### Revoke partner

```
DELETE /admin/partners/{partnerId}
X-ADMIN-KEY: <admin-key>
```

**Response - 204 No Content**

Soft-deletes the partner, immediately invalidating their API key. The partner record is retained in the database for audit purposes.

---

### Partner Products

Partner products map a partner's own SKUs to BlankLines base SKUs and Shopify variant IDs. These must be configured before a partner can place orders.

#### List partner products

```
GET /admin/partners/{partnerId}/products
X-ADMIN-KEY: <admin-key>
```

**Response - 200 OK**

```json
{
  "id": "a1b2c3d4-0000-0000-0000-000000000000",
  "partnerSku": "PARTNER1-TANK-BLACK",
  "baseSku": "M106GV100-S",
  "designReference": "Design-001",
  "shopifyVariantId": 49420784042305
}
```

---

#### Create partner product

```
POST /admin/partners/{partnerId}/products
X-ADMIN-KEY: <admin-key>
Content-Type: application/json
```

**Request body**:

```json
{
  "partnerSku": "PARTNER1-TANK-BLACK",
  "baseSku": "M106GV100-S",
  "designReference": "Design-001",
  "shopifyVariantId": 49420784042305
}
```

| Field | Required | Description |
|---|---|---|
| `partnerSku` | Yes | The SKU the partner uses in their system. Must be unique per partner. |
| `baseSku` | Yes | The BlankLines internal base SKU |
| `designReference` | Yes | Design reference associated with this product |
| `shopifyVariantId` | No | Shopify variant ID used when creating the order. Obtain from `GET /api/products`. |

**Response - 201 Created**

```json
{
  "id": "a1b2c3d4-0000-0000-0000-000000000000",
  "partnerSku": "PARTNER1-TANK-BLACK",
  "baseSku": "M106GV100-S",
  "designReference": "Design-001",
  "shopifyVariantId": 49420784042305
}
```

---

#### Delete partner product

```
DELETE /admin/partners/{partnerId}/products/{productId}
X-ADMIN-KEY: <admin-key>
```

**Response - 204 No Content**

---

## Design Files

An optional design image can be attached at order creation via the `designFile` form field. Accepted formats: JPEG, PNG, WebP, GIF. The upload size limit is 10 MB. Files are stored in Cloudflare R2 under `{R2:UploadFolder}/{partnerOrderId}.{ext}`.

---

## Error Responses

All error responses use the following JSON shape:

```json
{ "error": "Human-readable error message" }
```

| Status | Condition |
|---|---|
| `400 Bad Request` | Invalid input or business rule violation |
| `401 Unauthorized` | Missing or invalid API key |
| `404 Not Found` | Requested resource does not exist |
| `429 Too Many Requests` | Rate limit exceeded (10 req/min per partner) |
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

**PostgreSQL connection string** (use Railway's variable reference syntax):
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

## Partner Documentation

The API reference is publicly hosted at:
**https://api.blanklines.com/scalar/v1**

For integration support contact [hello@blanklines.com](mailto:hello@blanklines.com).
