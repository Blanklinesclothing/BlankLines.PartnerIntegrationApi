using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class OrderFile
{
    private OrderFile() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderFileType FileType { get; private set; }
    public string FileName { get; private set; } = default!;
    public string ObjectKey { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public DateTime UploadedAt { get; private set; }

    public static OrderFile Create(
        OrderFileType fileType,
        string fileName,
        string objectKey,
        string contentType) => new()
    {
        Id = Guid.NewGuid(),
        FileType = fileType,
        FileName = fileName,
        ObjectKey = objectKey,
        ContentType = contentType,
        UploadedAt = DateTime.UtcNow
    };

    internal void SetOrderId(Guid orderId) => OrderId = orderId;
}
