namespace BlankLines.PartnerIntegrationApi.Domain.Exceptions;

public class UpstreamServiceException : Exception
{
    public string ServiceName { get; }

    public UpstreamServiceException(string serviceName, string message, Exception innerException)
        : base(message, innerException)
    {
        ServiceName = serviceName;
    }
}
