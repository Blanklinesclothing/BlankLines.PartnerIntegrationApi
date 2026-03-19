namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class DesignFileDto
{
    public required Stream Content { get; set; }
    public required string ContentType { get; set; }
    public required string Extension { get; set; }
}
