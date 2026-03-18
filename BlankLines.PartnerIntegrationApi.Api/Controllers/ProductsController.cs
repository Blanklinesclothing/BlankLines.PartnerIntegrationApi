using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlankLines.PartnerIntegrationApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IShopifyApiService _shopifyService;

    public ProductsController(IShopifyApiService shopifyService)
    {
        _shopifyService = shopifyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _shopifyService.GetProductsAsync();
        return Ok(products);
    }
}
