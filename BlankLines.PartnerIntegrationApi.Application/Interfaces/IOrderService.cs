using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Responses;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IOrderService
{
    Task<string> CreateOrderAsync(Guid partnerId, CreateOrderRequest request);
    Task<OrderResponse> GetOrderAsync(Guid partnerId, string partnerOrderId);
    Task CancelOrderAsync(Guid partnerId, CancelOrderRequest request);
    Task<string> GetFilePresignedUrlAsync(Guid partnerId, string partnerOrderId, Guid fileId);
}
