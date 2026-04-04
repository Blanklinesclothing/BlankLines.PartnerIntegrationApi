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
    /// Sends the order to the BlankLines Shopify store for fulfilment.
    /// The request must be sent as <c>multipart/form-data</c>.
    /// <c>shippingAddressJson</c> is required when <c>deliveryMethod</c> is <c>Shipping</c>.
    ///
    /// File upload rules:
    /// <list type="bullet">
    /// <item>Up to 5 design image files via <c>designFiles[]</c>. Accepted formats: JPEG, PNG, WebP, GIF.</item>
    /// <item>Up to 5 vector files via <c>vectorFiles[]</c>. Accepted format: SVG only.</item>
    /// <item>Maximum 10 MB per file.</item>
    /// </list>
    ///
    /// Validation rules:
    /// <list type="bullet">
    /// <item>Each item must have a non-empty <c>partnerSku</c> and a <c>quantity</c> greater than zero.</item>
    /// <item>Duplicate <c>partnerSku</c> values within the same order are not allowed - combine quantities into one line item.</item>
    /// <item>Customer <c>email</c> must contain an <c>@</c> symbol.</item>
    /// <item>When <c>deliveryMethod</c> is <c>Shipping</c>, the shipping address <c>country</c> must be a 2-letter ISO code (e.g. AU, US, GB).</item>
    /// <item>Live inventory is checked per item before the order is persisted. Insufficient stock returns a <c>400</c>.</item>
    /// </list>
    /// </remarks>
    /// <param name="partnerOrderId">Your unique order reference.</param>
    /// <param name="deliveryMethod"><c>Shipping</c> or <c>Pickup</c>.</param>
    /// <param name="itemsJson">JSON array of order items: <c>[{"partnerSku":"SKU-001","quantity":1,"designReference":"Logo-White-LeftChest"}]</c>. <c>designReference</c> is optional - if omitted the value registered against the SKU is used.</param>
    /// <param name="customerJson">JSON object with customer details: firstName, lastName, email, phone (optional).</param>
    /// <param name="shippingAddressJson">JSON object with shipping address. Required when deliveryMethod is Shipping.</param>
    /// <param name="designFiles">Optional design image files (max 5). Accepted formats: JPEG, PNG, WebP, GIF. Max 10 MB each.</param>
    /// <param name="vectorFiles">Optional vector files (max 5). Accepted format: SVG only. Max 10 MB each.</param>
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
    /// Retrieve the status of an order by your partner order ID.
    /// </summary>
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
    /// View a design or vector file attached to an order. Redirects to a time-limited presigned URL (valid 1 hour).
    /// </summary>
    /// <param name="partnerOrderId">The order ID you provided at creation.</param>
    /// <param name="fileId">The file ID returned in the order response.</param>
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
    /// Cancel an order. Orders can only be cancelled within 24 hours of submission.
    /// </summary>
    /// <param name="request">The partner order ID to cancel.</param>
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
