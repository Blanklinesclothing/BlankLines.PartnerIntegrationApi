namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class UploadedFileDto
{
    public required string FileName { get; set; }
    public required Stream Content { get; set; }
    public required string ContentType { get; set; }
    public required string Extension { get; set; }
    public long SizeBytes { get; set; }
}
