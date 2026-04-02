using BlankLines.PartnerIntegrationApi.Application.Requests;

namespace BlankLines.PartnerIntegrationApi.Application.Validators;

public class CreatePartnerProductRequestValidator : IRequestValidator<CreatePartnerProductRequest>
{
    public void Validate(CreatePartnerProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerSku))
        {
            throw new InvalidOperationException("PartnerSku is required.");
        }

        if (string.IsNullOrWhiteSpace(request.BaseSku))
        {
            throw new InvalidOperationException("BaseSku is required.");
        }

        if (string.IsNullOrWhiteSpace(request.DesignReference))
        {
            throw new InvalidOperationException("DesignReference is required.");
        }
    }
}
