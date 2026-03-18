namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class CustomerDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
}
