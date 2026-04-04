using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Responses;
using BlankLines.PartnerIntegrationApi.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

/// <summary>
/// Manage partner orders.
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const string AllowedVectorType = "image/svg+xml";

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Submit a new order for fulfilment.
    /// </summary>
    /// <remarks>
    /// Submits the order to the BlankLines Shopify store for fulfilment.
    /// The request must be sent as <c>multipart/form-data</c>.
    ///
    /// **Items** (<c>itemsJson</c>) — JSON array, at least one item required:
    /// <code>
    /// [{"partnerSku":"MY-SKU-001","quantity":2,"designReference":"Logo-White-LeftChest"}]
    /// </code>
    /// <c>designReference</c> is optional per item. When omitted, the value registered against the SKU is used.
    ///
    /// **Customer** (<c>customerJson</c>) — JSON object:
    /// <code>
    /// {"firstName":"Jane","lastName":"Smith","email":"jane@example.com","phone":"+61400000000"}
    /// </code>
    /// <c>phone</c> is optional.
    ///
    /// **Shipping address** (<c>shippingAddressJson</c>) — required when <c>deliveryMethod</c> is <c>Shipping</c>:
    /// <code>
    /// {"address1":"42 Example St","address2":"Unit 3","city":"Melbourne","province":"VIC","country":"AU","zip":"3000","phone":"+61400000000"}
    /// </code>
    /// <c>address2</c> and <c>phone</c> are optional. <c>country</c> must be a 2-letter ISO code.
    ///
    /// **File uploads** — all files are optional:
    /// <list type="bullet">
    /// <item><c>designFiles[]</c> — up to 5 design images. Accepted: JPEG, PNG, WebP, GIF. Max 10 MB each.</item>
    /// <item><c>vectorFiles[]</c> — up to 5 vector files. Accepted: SVG only. Max 10 MB each.</item>
    /// </list>
    ///
    /// **Validation rules:**
    /// <list type="bullet">
    /// <item>Each item must have a non-empty <c>partnerSku</c> and a <c>quantity</c> greater than zero.</item>
    /// <item>Duplicate <c>partnerSku</c> values within the same order are not allowed — combine quantities into one line item.</item>
    /// <item>Customer <c>email</c> must be a valid address containing <c>@</c>.</item>
    /// <item>Live inventory is checked per item before the order is persisted. Insufficient stock returns <c>400</c>.</item>
    /// </list>
    /// </remarks>
    /// <param name="partnerOrderId">Your unique order reference. Must not already exist for your account.</param>
    /// <param name="deliveryMethod"><c>Shipping</c> or <c>Pickup</c>.</param>
    /// <param name="itemsJson">JSON array of order items. Each item requires <c>partnerSku</c> and <c>quantity</c>. <c>designReference</c> is optional.</param>
    /// <param name="customerJson">JSON object with <c>firstName</c>, <c>lastName</c>, <c>email</c> (required) and <c>phone</c> (optional).</param>
    /// <param name="shippingAddressJson">JSON object with <c>address1</c>, <c>city</c>, <c>country</c> (ISO 2-letter), <c>zip</c> (required) and <c>address2</c>, <c>province</c>, <c>phone</c> (optional). Required when <c>deliveryMethod</c> is <c>Shipping</c>.</param>
    /// <param name="designFiles">Optional design image files (max 5). Accepted: JPEG, PNG, WebP, GIF. Max 10 MB each.</param>
    /// <param name="vectorFiles">Optional vector files (max 5). Accepted: SVG only. Max 10 MB each.</param>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromForm] string partnerOrderId,
        [FromForm] DeliveryMethod deliveryMethod,
        [FromForm] string itemsJson,
        [FromForm] string customerJson,
        [FromForm] string? shippingAddressJson,
        [FromForm] List<IFormFile>? designFiles,
        [FromForm] List<IFormFile>? vectorFiles)
    {
        var designFileDtos = new List<UploadedFileDto>();
        var vectorFileDtos = new List<UploadedFileDto>();

        if (designFiles != null)
        {
            foreach (var file in designFiles)
            {
                if (!AllowedImageTypes.Contains(file.ContentType))
                {
                    return BadRequest(new { error = $"Design file '{file.FileName}' must be an image (JPEG, PNG, WebP, or GIF)." });
                }

                designFileDtos.Add(new UploadedFileDto
                {
                    FileName = file.FileName,
                    Content = file.OpenReadStream(),
                    ContentType = file.ContentType,
                    Extension = Path.GetExtension(file.FileName),
                    SizeBytes = file.Length
                });
            }
        }

        if (vectorFiles != null)
        {
            foreach (var file in vectorFiles)
            {
                if (!file.ContentType.Equals(AllowedVectorType, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = $"Vector file '{file.FileName}' must be an SVG." });
                }

                vectorFileDtos.Add(new UploadedFileDto
                {
                    FileName = file.FileName,
                    Content = file.OpenReadStream(),
                    ContentType = file.ContentType,
                    Extension = Path.GetExtension(file.FileName),
                    SizeBytes = file.Length
                });
            }
        }

        var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemDto>>(itemsJson, JsonOptions);
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(customerJson, JsonOptions);
        var shippingAddress = shippingAddressJson is not null
            ? System.Text.Json.JsonSerializer.Deserialize<ShippingAddressDto>(shippingAddressJson, JsonOptions)
            : null;

        if (items == null || customer == null)
        {
            return BadRequest(new { error = "Invalid items or customer data." });
        }

        var request = new CreateOrderRequest
        {
            PartnerOrderId = partnerOrderId,
            DeliveryMethod = deliveryMethod,
            Items = items,
            Customer = customer,
            ShippingAddress = shippingAddress,
            DesignFiles = designFileDtos,
            VectorFiles = vectorFileDtos
        };

        var partnerId = GetPartnerId();
        var orderId = await _orderService.CreateOrderAsync(partnerId, request);

        return CreatedAtAction(
            nameof(GetOrder),
            new { partnerOrderId = request.PartnerOrderId },
            new { orderId, partnerOrderId = request.PartnerOrderId });
    }

    /// <summary>
    /// Retrieve the current status and details of an order.
    /// </summary>
    /// <remarks>
    /// Returns the full order record including status, customer details, line items, and any uploaded files.
    /// Each entry in the <c>files</c> array includes a <c>viewUrl</c> — use
    /// <c>GET /api/orders/{partnerOrderId}/files/{fileId}</c> to retrieve a fresh presigned download link.
    /// </remarks>
    /// <param name="partnerOrderId">The order ID you provided at creation.</param>
    [HttpGet("{partnerOrderId}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(string partnerOrderId)
    {
        var partnerId = GetPartnerId();
        var order = await _orderService.GetOrderAsync(partnerId, partnerOrderId);
        return Ok(order);
    }

    /// <summary>
    /// Get a download link for a file attached to an order.
    /// </summary>
    /// <remarks>
    /// Returns a <c>302</c> redirect to a presigned R2 URL valid for <b>1 hour</b>.
    /// File IDs are found in the <c>files</c> array on the order response.
    /// Following the redirect opens the file directly in the browser or downloads it.
    /// </remarks>
    /// <param name="partnerOrderId">The order ID you provided at creation.</param>
    /// <param name="fileId">The file ID from the <c>files</c> array on the order response.</param>
    [HttpGet("{partnerOrderId}/files/{fileId:guid}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewFile(string partnerOrderId, Guid fileId)
    {
        var partnerId = GetPartnerId();
        var presignedUrl = await _orderService.GetFilePresignedUrlAsync(partnerId, partnerOrderId, fileId);
        return Redirect(presignedUrl);
    }

    /// <summary>
    /// Cancel an order.
    /// </summary>
    /// <remarks>
    /// Cancels the order in both the BlankLines system and Shopify.
    /// Orders can only be cancelled within <b>24 hours</b> of submission.
    ///
    /// Request body:
    /// <code>
    /// {"partnerOrderId":"YOUR-ORDER-REF"}
    /// </code>
    /// </remarks>
    /// <param name="request">Object containing the <c>partnerOrderId</c> to cancel.</param>
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest request)
    {
        var partnerId = GetPartnerId();
        await _orderService.CancelOrderAsync(partnerId, request);
        return NoContent();
    }

    private Guid GetPartnerId()
    {
        if (HttpContext.Items["PartnerId"] is Guid partnerId)
        {
            return partnerId;
        }

        throw new UnauthorizedAccessException("Partner ID not found");
    }
}
