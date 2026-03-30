using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

/// <summary>
/// Browse products available for ordering.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController(IShopifyApiService shopifyService) : ControllerBase
{
    private readonly IShopifyApiService _shopifyService = shopifyService;

    /// <summary>
    /// Returns all products available to order from the BlankLines Shopify store.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _shopifyService.GetProductsAsync();
        return Ok(products);
    }
}
