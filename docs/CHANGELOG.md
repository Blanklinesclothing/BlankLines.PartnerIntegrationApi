# Changelog

All notable changes to the BlankLines Partner Integration API are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Added
- "BlankLines Partner" created in PROD for production verification testing (PVT). See [Admin Endpoints - PVT](admin-endpoints.md#production-verification-testing-pvt) for usage.

---

## [1.0.0] - 2026-03-22

### Added
- `POST /api/orders` - submit a fulfilment order (`multipart/form-data`, optional design file upload)
- `GET /api/orders/{partnerOrderId}` - retrieve order status and details
- `POST /api/orders/cancel` - cancel an order within 24 hours of submission
- `GET /api/products` - list all Shopify products available to order
- `GET /health` - health check endpoint (no auth required)
- API key authentication via `X-API-KEY` header for all `/api/*` endpoints
- Rate limiting: 10 requests per minute per partner (fixed window)
- Design file storage in Cloudflare R2 (JPEG, PNG, WebP, GIF - max 10 MB)
- Admin endpoints (`/admin/*`) protected by `X-ADMIN-KEY` header
  - `GET /admin/orders` - list all orders across all partners
  - `POST /admin/partners` - create a partner and issue a one-time API key
  - `GET /admin/partners` - list active partners
  - `DELETE /admin/partners/{partnerId}` - revoke a partner (soft delete)
  - `GET /admin/partners/{partnerId}/products` - list partner SKU mappings
  - `POST /admin/partners/{partnerId}/products` - register a partner SKU mapping
  - `DELETE /admin/partners/{partnerId}/products/{productId}` - remove a SKU mapping
- Scalar interactive API reference at `/scalar/v1`
- OpenAPI spec at `/openapi/v1.json`
