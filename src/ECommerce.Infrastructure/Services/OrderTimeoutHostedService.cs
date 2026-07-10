using ECommerce.Application.Services;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// 订单超时自动取消托管服务
/// 使用 PeriodicTimer 每 1 分钟扫描一次超时订单并自动取消
/// </summary>
public sealed class OrderTimeoutHostedService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderTimeoutHostedService> _logger;
    private readonly PeriodicTimer _timer;
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    public OrderTimeoutHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderTimeoutHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("订单超时自动取消服务已启动，检查间隔: 1分钟");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("订单超时自动取消服务正在停止...");

        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        if (_executingTask != null)
        {
            try
            {
                await _executingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
        }

        _logger.LogInformation("订单超时自动取消服务已停止");
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessExpiredOrdersAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "订单超时自动取消服务执行异常");
        }
    }

    private async Task ProcessExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        // 1. 查询超时订单 ID 列表
        var expiredOrderIds = await orderRepository.GetExpiredOrderIdsAsync(DateTime.Now, cancellationToken);

        if (expiredOrderIds.Count == 0)
        {
            return;
        }

        _logger.LogInformation("发现 {Count} 个超时订单，开始自动取消", expiredOrderIds.Count);

        // 2. 逐个取消
        foreach (var orderId in expiredOrderIds)
        {
            try
            {
                // 先获取订单详情，得到 userId
                var order = await orderRepository.GetOrderByIdAsync(orderId, cancellationToken);
                if (order == null)
                {
                    _logger.LogWarning("订单 {OrderId} 不存在，跳过", orderId);
                    continue;
                }

                // 检查订单状态是否仍然为"待支付"（防止并发修改）
                if (order.Status != (int)OrderStatus.PendingPayment)
                {
                    _logger.LogWarning("订单 {OrderId} 当前状态为 {Status}，已不是待支付状态，跳过", orderId, order.Status);
                    continue;
                }

                // 调用 CancelAsync 取消订单
                // userId: 订单归属者
                // operatorId: 0 表示系统自动操作
                // operatorName: "System" 表示系统
                await orderService.CancelAsync(
                    userId: order.UserId,
                    orderId: orderId,
                    operatorId: 0,
                    operatorName: "System",
                    reason: "支付超时自动取消",
                    ipAddress: "127.0.0.1",
                    cancellationToken: cancellationToken
                );

                _logger.LogInformation("订单 {OrderId} 已自动取消", orderId);
            }
            catch (BusinessException ex)
            {
                // 业务异常（比如订单状态已被其他操作改变）
                _logger.LogWarning("自动取消订单 {OrderId} 失败: {Message}", orderId, ex.Message);
            }
            catch (Exception ex)
            {
                // 其他异常，记录但继续处理下一个订单
                _logger.LogError(ex, "自动取消订单 {OrderId} 发生异常", orderId);
            }
        }

        _logger.LogInformation("超时订单自动取消处理完成，共处理 {Count} 个", expiredOrderIds.Count);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
