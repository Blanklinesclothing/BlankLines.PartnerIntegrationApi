using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = [];
    private readonly List<OrderFile> _files = [];

    private Order() { }

    public Guid Id { get; private set; }
    public Guid PartnerId { get; private set; }
    public string PartnerOrderId { get; private set; } = default!;
    public string? ShopifyOrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DeliveryMethod DeliveryMethod { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CustomerFirstName { get; private set; } = default!;
    public string CustomerLastName { get; private set; } = default!;
    public string CustomerEmail { get; private set; } = default!;
    public string? CustomerPhone { get; private set; }
    public string? ShippingAddress1 { get; private set; }
    public string? ShippingAddress2 { get; private set; }
    public string? ShippingCity { get; private set; }
    public string? ShippingProvince { get; private set; }
    public string? ShippingCountry { get; private set; }
    public string? ShippingZip { get; private set; }
    public string? ShippingPhone { get; private set; }

    public Partner? Partner { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderFile> Files => _files.AsReadOnly();

    public static Order Create(
        Guid partnerId,
        string partnerOrderId,
        DeliveryMethod deliveryMethod,
        string customerFirstName,
        string customerLastName,
        string customerEmail,
        string? customerPhone,
        string? address1,
        string? address2,
        string? city,
        string? province,
        string? country,
        string? zip,
        string? shippingPhone,
        IEnumerable<OrderItem> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            PartnerOrderId = partnerOrderId,
            Status = OrderStatus.Received,
            DeliveryMethod = deliveryMethod,
            CreatedAt = DateTime.UtcNow,
            CustomerFirstName = customerFirstName,
            CustomerLastName = customerLastName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            ShippingAddress1 = address1,
            ShippingAddress2 = address2,
            ShippingCity = city,
            ShippingProvince = province,
            ShippingCountry = country,
            ShippingZip = zip,
            ShippingPhone = shippingPhone
        };

        foreach (var item in items)
        {
            item.SetOrderId(order.Id);
            order._items.Add(item);
        }

        return order;
    }

    public void MarkAsProcessing(string shopifyOrderId)
    {
        ShopifyOrderId = shopifyOrderId;
        Status = OrderStatus.Processing;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException($"Order '{PartnerOrderId}' is already cancelled.");

        if ((DateTime.UtcNow - CreatedAt).TotalHours > 24)
            throw new InvalidOperationException("Order cannot be cancelled after 24 hours.");

        Status = OrderStatus.Cancelled;
    }

    public void AttachFile(OrderFile file)
    {
        file.SetOrderId(Id);
        _files.Add(file);
    }
}
