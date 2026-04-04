using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class OrderFileDto
{
    public Guid Id { get; set; }
    public OrderFileType FileType { get; set; }
    public required string FileName { get; set; }
    public required string ViewUrl { get; set; }
}
