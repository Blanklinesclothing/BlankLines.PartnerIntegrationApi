using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

[ApiController]
[Route("admin")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminController : ControllerBase
{
    private readonly IPartnerService _partnerService;

    public AdminController(IPartnerService partnerService)
    {
        _partnerService = partnerService;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders()
    {
        var orders = await _partnerService.GetAllOrdersAsync();
        return Ok(orders);
    }

    [HttpPost("partners")]
    public async Task<IActionResult> CreatePartner([FromBody] CreatePartnerRequest request)
    {
        var (partner, plainTextKey) = await _partnerService.CreatePartnerAsync(request.Name);

        return Created(string.Empty, new
        {
            partnerId = partner.Id,
            name = partner.Name,
            apiKey = plainTextKey,
            createdAt = partner.CreatedAt,
            note = "Save this API key — it will not be shown again."
        });
    }

    [HttpGet("partners")]
    public async Task<IActionResult> GetPartners()
    {
        var partners = await _partnerService.GetAllPartnersAsync();
        return Ok(partners);
    }

    [HttpDelete("partners/{partnerId:guid}")]
    public async Task<IActionResult> RevokePartner(Guid partnerId)
    {
        await _partnerService.RevokePartnerAsync(partnerId);
        return NoContent();
    }
}

public record CreatePartnerRequest(string Name);