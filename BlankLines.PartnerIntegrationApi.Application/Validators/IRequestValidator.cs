namespace BlankLines.PartnerIntegrationApi.Application.Validators;

public interface IRequestValidator<T>
{
    void Validate(T request);
}