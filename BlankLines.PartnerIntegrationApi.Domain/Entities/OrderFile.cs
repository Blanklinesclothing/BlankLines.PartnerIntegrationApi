using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class OrderFile
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public OrderFileType FileType { get; set; }
    public required string FileName { get; set; }
    public required string ObjectKey { get; set; }
    public required string ContentType { get; set; }
    public DateTime UploadedAt { get; set; }
}
