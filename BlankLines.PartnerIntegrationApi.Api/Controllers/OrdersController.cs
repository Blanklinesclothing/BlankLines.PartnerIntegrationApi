using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
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
