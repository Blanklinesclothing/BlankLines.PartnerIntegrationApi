namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class ShippingAddressDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    public required string City { get; set; }
    public string? Province { get; set; }
    public required string Country { get; set; }
    public required string Zip { get; set; }
    public string? Phone { get; set; }
}
