namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class OrderItem
{
    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string PartnerSku { get; private set; } = default!;
    public string BaseSku { get; private set; } = default!;
    public string DesignReference { get; private set; } = default!;
    public long? ShopifyVariantId { get; private set; }
    public int Quantity { get; private set; }

    public static OrderItem Create(
        string partnerSku,
        string baseSku,
        string designReference,
        long? shopifyVariantId,
        int quantity) => new()
    {
        Id = Guid.NewGuid(),
        PartnerSku = partnerSku,
        BaseSku = baseSku,
        DesignReference = designReference,
        ShopifyVariantId = shopifyVariantId,
        Quantity = quantity
    };

    internal void SetOrderId(Guid orderId) => OrderId = orderId;
}
