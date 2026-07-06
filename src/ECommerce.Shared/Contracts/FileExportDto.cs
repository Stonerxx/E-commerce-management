namespace ECommerce.Shared.Contracts;

public sealed record FileExportDto(
    string FileName,
    string ContentType,
    byte[] Content);
