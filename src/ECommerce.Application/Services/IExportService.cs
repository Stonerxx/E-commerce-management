using ECommerce.Application.DTOs;
using ECommerce.Shared.Contracts;

namespace ECommerce.Application.Services;

public interface IExportService
{
    Task<FileExportDto> ExportOrdersAsync(AdminOrderQuery query, CancellationToken cancellationToken = default);

    Task<FileExportDto> ExportInventoryAsync(InventoryLogQuery query, CancellationToken cancellationToken = default);
}
