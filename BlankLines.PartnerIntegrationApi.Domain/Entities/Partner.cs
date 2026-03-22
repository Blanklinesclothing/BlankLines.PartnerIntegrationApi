namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class Partner
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ApiKey { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
}
