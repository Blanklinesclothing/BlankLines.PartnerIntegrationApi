using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

[ApiController]
[Route("admin/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class PartnersController : ControllerBase
{
    private readonly IPartnerService _partnerService;

    public PartnersController(IPartnerService partnerService)
    {
        _partnerService = partnerService;
    }

    /// <summary>
    /// Create a new partner and generate their API key.
    /// The plain text API key is returned once and never stored — save it immediately.
    /// </summary>
    /// <param name="request">Partner name.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePartner([FromBody] CreatePartnerRequest request)
    {
        var (partner, plainTextKey) = await _partnerService.CreatePartnerAsync(request.Name);

        return CreatedAtAction(nameof(CreatePartner), new
        {
            partnerId = partner.Id,
            name = partner.Name,
            apiKey = plainTextKey,
            createdAt = partner.CreatedAt,
            note = "Save this API key — it will not be shown again."
        });
    }
}

public record CreatePartnerRequest(string Name);
