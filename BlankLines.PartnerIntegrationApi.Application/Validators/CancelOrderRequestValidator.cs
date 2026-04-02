using BlankLines.PartnerIntegrationApi.Application.Requests;

namespace BlankLines.PartnerIntegrationApi.Application.Validators;

public class CancelOrderRequestValidator : IRequestValidator<CancelOrderRequest>
{
    public void Validate(CancelOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerOrderId))
        {
            throw new InvalidOperationException("PartnerOrderId is required.");
        }
    }
}
