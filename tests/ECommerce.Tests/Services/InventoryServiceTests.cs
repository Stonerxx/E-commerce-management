using System.Data.Common;
using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using ECommerce.Shared.Abstractions;
using ECommerce.Shared.Contracts;
using ECommerce.Tests.Helpers;
using Moq;
using Xunit;

namespace ECommerce.Tests.Services;

public class InventoryServiceTests : ServiceTestBase
{
    [Fact]
    public async Task LockForOrderAsync_WithinExistingTransaction_DoesNotManageOuterTransaction()
    {
        var unitOfWork = CreateUnitOfWorkMock();
        unitOfWork.SetupGet(x => x.CurrentTransaction).Returns(new Mock<DbTransaction>().Object);

        var skuRepository = new Mock<ISkuRepository>();
        skuRepository.Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSku());
        skuRepository.Setup(x => x.UpdateAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var logRepository = new Mock<IInventoryLogRepository>();
        logRepository.Setup(x => x.CreateAsync(It.IsAny<InventoryLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new InventoryService(unitOfWork.Object, skuRepository.Object, logRepository.Object);

        await service.LockForOrderAsync(1, new[] { new OrderSkuQuantity(100, 2) });

        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeductForPaidOrderAsync_WithoutExistingTransaction_CommitsItsOwnTransaction()
    {
        var unitOfWork = CreateUnitOfWorkMock();
        var skuRepository = new Mock<ISkuRepository>();
        skuRepository.Setup(x => x.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSku(lockedStock: 2));
        skuRepository.Setup(x => x.UpdateAsync(It.IsAny<Sku>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var logRepository = new Mock<IInventoryLogRepository>();
        logRepository.Setup(x => x.CreateAsync(It.IsAny<InventoryLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new InventoryService(unitOfWork.Object, skuRepository.Object, logRepository.Object);

        await service.DeductForPaidOrderAsync(1, new[] { new OrderSkuQuantity(100, 2) });

        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Sku CreateSku(int stock = 10, int lockedStock = 0) => new()
    {
        Id = 100,
        Stock = stock,
        LockedStock = lockedStock,
        UpdatedAt = DateTime.Now
    };
}
