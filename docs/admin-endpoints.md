# Admin Endpoints

Admin endpoints are **not** included in the public OpenAPI spec or Scalar reference. They are protected by the `X-ADMIN-KEY` header.

> The admin key is configured via `Admin:AdminKey` in `appsettings.json` / environment variables. It never touches the database.

---

## Production Verification Testing (PVT)

A dedicated partner named **"BlankLines Partner"** exists in PROD for PVT.

To run PVT against production:

1. Retrieve the `partnerId` for "BlankLines Partner" via `GET /admin/partners` using the PROD admin key.
2. Ensure the required SKU mappings are registered under that partner via `GET /admin/partners/{partnerId}/products`.
3. Use the partner's API key (held securely - request from a team lead if needed) to authenticate partner-facing requests against `https://api.blanklines.com`.
4. Do **not** create new partners or SKU mappings in PROD without sign-off.

---

## Authentication

All requests in this document require:

```
X-ADMIN-KEY: <your-admin-key>
```

A missing or incorrect key returns:

```json
{ "error": "Admin key is missing" }
{ "error": "Invalid admin key" }
```

---

## Postman collection

A ready-to-use Postman collection for all admin endpoints is in this folder:

```
docs/BlankLines.PartnerIntegrationApi.admin.postman_collection.json
```

Import it into Postman and set the `adminKey` collection variable before use.

---

## Endpoints

### Orders

#### List all orders

```
GET /admin/orders
```

Returns all orders across all partners.

**Response - 200 OK**

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "partnerId": "a1b2c3d4-0000-0000-0000-000000000001",
    "partnerName": "Test Partner 1",
    "partnerOrderId": "my-order-001",
    "shopifyOrderId": "5678901234",
    "status": "Processing",
    "deliveryMethod": "Shipping",
    "createdAt": "2026-03-19T10:00:00Z"
  }
]
```

---

### Partners

#### Create partner

```
POST /admin/partners
Content-Type: application/json
```

**Request body**

```json
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

> The `apiKey` is generated from a cryptographically secure random source and returned **once only**. Copy it and send it to the partner - it cannot be retrieved again.

---

#### List partners

```
GET /admin/partners
```

Returns all active (non-revoked) partners.

**Response - 200 OK**

```json
[
  {
    "id": "a1b2c3d4-0000-0000-0000-000000000001",
    "name": "Test Partner 1",
    "createdAt": "2026-03-10T12:54:02Z"
  }
]
```

---

#### Revoke partner

```
DELETE /admin/partners/{partnerId}
```

Soft-deletes the partner, immediately invalidating their API key. The record is retained in the database for auditing.

**Response - 204 No Content**

---

### Partner Products

Partner products map a partner's own SKUs to BlankLines base SKUs and Shopify variant IDs. These must be configured before a partner can place orders.

#### List partner products

```
GET /admin/partners/{partnerId}/products
```

**Response - 200 OK**

```json
[
  {
    "id": "a1b2c3d4-0000-0000-0000-000000000010",
    "partnerSku": "PARTNER1-TANK-BLACK",
    "baseSku": "M106GV100-S",
    "designReference": "Design-001",
    "shopifyVariantId": 49420784042305
  }
]
```

---

#### Create partner product

```
POST /admin/partners/{partnerId}/products
Content-Type: application/json
```

**Request body**

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
| `partnerSku` | Yes | The SKU used in the partner's system. Must be unique per partner. |
| `baseSku` | Yes | The BlankLines internal base SKU. |
| `designReference` | Yes | Design reference associated with this product. |
| `shopifyVariantId` | No | Shopify variant ID. Obtain from `GET /api/products`. |

**Response - 201 Created** - returns the created product object (same shape as list).

---

#### Delete partner product

```
DELETE /admin/partners/{partnerId}/products/{productId}
```

**Response - 204 No Content**
