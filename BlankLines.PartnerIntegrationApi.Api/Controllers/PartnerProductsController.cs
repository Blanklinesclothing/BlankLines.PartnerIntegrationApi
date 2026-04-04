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
public class PartnerProductsController(IPartnerProductService partnerProductService) : ControllerBase
{
    private readonly IPartnerProductService _partnerProductService = partnerProductService;

    /// <summary>
    /// List all products registered under your partner account.
    /// </summary>
    /// <remarks>
    /// Returns your registered SKU mappings. Use <c>partnerSku</c> when placing orders.
    /// <c>baseSku</c> is the BlankLines product SKU. <c>shopifyVariantId</c> is resolved automatically
    /// at registration time and is used internally when creating Shopify orders.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PartnerProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProducts()
    {
        var partnerId = GetPartnerId();
        var products = await _partnerProductService.GetPartnerProductsAsync(partnerId);
        return Ok(products);
    }

    /// <summary>
    /// Register a new product under your partner account.
    /// </summary>
    /// <remarks>
    /// Maps one of your internal SKUs to a BlankLines product variant.
    ///
    /// Request body:
    /// <code>
    /// {"partnerSku":"MY-SKU-001","baseSku":"BL-TEE-WHITE-M","designReference":"spring-2025-logo"}
    /// </code>
    ///
    /// <list type="bullet">
    /// <item><c>partnerSku</c> — your internal identifier, used when placing orders. Must be unique per account.</item>
    /// <item><c>baseSku</c> — must exactly match an active variant SKU in the BlankLines Shopify store. Returns <c>400</c> if not found.</item>
    /// <item><c>designReference</c> — default design identifier applied to orders for this SKU. Can be overridden per order line item.</item>
    /// </list>
    ///
    /// <c>shopifyVariantId</c> is resolved automatically from <c>baseSku</c> and returned in the response.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PartnerProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProduct([FromBody] CreatePartnerProductRequest request)
    {
        var partnerId = GetPartnerId();
        var product = await _partnerProductService.CreatePartnerProductAsync(partnerId, request);
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
