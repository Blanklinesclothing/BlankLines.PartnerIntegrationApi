using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateOrder(
        [FromForm] string partnerOrderId,
        [FromForm] DeliveryMethod deliveryMethod,
        [FromForm] string itemsJson,
        [FromForm] string customerJson,
        [FromForm] string? shippingAddressJson,
        IFormFile? designFile)
    {
        if (designFile != null && !AllowedImageTypes.Contains(designFile.ContentType))
        {
            return BadRequest(new { error = "Design file must be an image (JPEG, PNG, WebP, or GIF)" });
        }

        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var items = System.Text.Json.JsonSerializer.Deserialize<List<OrderItemDto>>(itemsJson, options);
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(customerJson, options);
        var shippingAddress = shippingAddressJson is not null
            ? System.Text.Json.JsonSerializer.Deserialize<ShippingAddressDto>(shippingAddressJson, options)
            : null;

        if (items == null || customer == null)
        {
            return BadRequest(new { error = "Invalid items or customer data" });
        }

        DesignFileDto? designFileDto = null;
        if (designFile != null)
        {
            designFileDto = new DesignFileDto
            {
                Content = designFile.OpenReadStream(),
                ContentType = designFile.ContentType,
                Extension = Path.GetExtension(designFile.FileName)
            };
        }

        var request = new CreateOrderRequest
        {
            PartnerOrderId = partnerOrderId,
            DeliveryMethod = deliveryMethod,
            Items = items,
            Customer = customer,
            ShippingAddress = shippingAddress,
            DesignFile = designFileDto
        };

        var partnerId = GetPartnerId();
        var orderId = await _orderService.CreateOrderAsync(partnerId, request);

        return CreatedAtAction(
            nameof(GetOrder),
            new { partnerOrderId = request.PartnerOrderId },
            new { orderId, partnerOrderId = request.PartnerOrderId });
    }

    [HttpGet("{partnerOrderId}")]
    public async Task<IActionResult> GetOrder(string partnerOrderId)
    {
        var partnerId = GetPartnerId();

        var order = await _orderService.GetOrderAsync(partnerId, partnerOrderId);

        return Ok(order);
    }

    [HttpPost("cancel")]
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
