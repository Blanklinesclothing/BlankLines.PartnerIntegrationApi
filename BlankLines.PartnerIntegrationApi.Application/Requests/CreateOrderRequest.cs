using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.Requests;

public class CreateOrderRequest
{
    public required string PartnerOrderId { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public required List<OrderItemDto> Items { get; set; }
    public required CustomerDto Customer { get; set; }
    public ShippingAddressDto? ShippingAddress { get; set; }
    public DesignFileDto? DesignFile { get; set; }
}
