namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class ProductDto
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string Sku { get; set; }
    public long? VariantId { get; set; }
    public int InventoryQuantity { get; set; }
}
