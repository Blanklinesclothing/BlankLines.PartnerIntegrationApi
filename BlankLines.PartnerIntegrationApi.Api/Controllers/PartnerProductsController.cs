using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

/// <summary>
/// Manage your registered products.
/// </summary>
[ApiController]
[Route("api/partner-products")]
public class PartnerProductsController(IPartnerService partnerService) : ControllerBase
{
    private readonly IPartnerService _partnerService = partnerService;

    /// <summary>
    /// List all products registered under your partner account.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PartnerProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProducts()
    {
        var partnerId = GetPartnerId();
        var products = await _partnerService.GetPartnerProductsAsync(partnerId);
        return Ok(products);
    }

    /// <summary>
    /// Register a new product under your partner account.
    /// </summary>
    /// <remarks>
    /// The <c>baseSku</c> must correspond to an active product variant in the BlankLines Shopify store.
    /// The <c>partnerSku</c> is your own internal SKU used when placing orders.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PartnerProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProduct([FromBody] CreatePartnerProductRequest request)
    {
        var partnerId = GetPartnerId();
        var product = await _partnerService.CreatePartnerProductAsync(partnerId, request);
        return CreatedAtAction(nameof(GetProducts), product);
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
