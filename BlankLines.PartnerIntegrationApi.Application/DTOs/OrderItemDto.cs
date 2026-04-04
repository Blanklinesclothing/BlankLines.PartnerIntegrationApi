namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class OrderItemDto
{
    public required string PartnerSku { get; set; }
    public int Quantity { get; set; }
    public string? DesignReference { get; set; }
}
