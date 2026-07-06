# B/S 架构与接口契约

本文档是团队开分支前的共同基线。所有成员先按这里的目录、接口、路由、DTO、状态码和权限约定开发，确需变更时先在 `main` 更新本文档，再同步到各自分支。

## 1. 系统模式

本项目确定采用 B/S 模式：

- Browser：用户和管理员通过浏览器访问系统页面。
- Server：ASP.NET Core MVC 提供 Razor 页面、表单提交、JSON API、身份认证、权限控制和业务服务。
- Database：Oracle 18c+，初始化脚本见 `migration/init_database.sql`。
- UI：统一使用 Bootstrap，前台和后台共享基础布局，但后台使用独立导航。

页面请求优先走 MVC Controller + View；页面内的异步操作、列表筛选、状态变更、导出等走 `/api/v1/...` JSON 接口。

## 2. 五层目录约定

ASP.NET Core MVC 项目创建后，目录按五层组织：

```text
src/
  ECommerce.Web/                 # 表现层：Controllers, Views, ViewModels, Filters
  ECommerce.Application/         # 应用层：Services, DTOs, Validators, Use cases
  ECommerce.Domain/              # 领域层：Entities, Enums, Domain services
  ECommerce.Infrastructure/      # 基础设施层：Oracle repositories, UnitOfWork, File storage
  ECommerce.Shared/              # 公共层：Result, Pagination, Constants, Exceptions
tests/
  ECommerce.Tests/
```

依赖方向固定为：

```text
Web -> Application -> Domain
Application -> Infrastructure abstractions
Infrastructure -> Domain + Shared
Shared -> no project dependency
```

Controller 不直接访问数据库；Controller 只调用 Application Service。Repository 不写页面逻辑；Repository 只处理数据持久化。

## 3. 通用接口规范

### 3.1 JSON 响应

所有 JSON API 使用统一响应结构：

```json
{
  "success": true,
  "code": "OK",
  "message": "success",
  "data": {},
  "traceId": "00-..."
}
```

失败响应示例：

```json
{
  "success": false,
  "code": "ORDER_STOCK_NOT_ENOUGH",
  "message": "库存不足",
  "data": null,
  "traceId": "00-..."
}
```

### 3.2 分页结构

```json
{
  "items": [],
  "pageIndex": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

分页参数统一为 `pageIndex`、`pageSize`，页码从 1 开始。

### 3.3 HTTP 状态码

| 状态码 | 使用场景 |
| --- | --- |
| 200 | 查询、修改、删除成功 |
| 201 | 创建成功 |
| 400 | 参数错误、业务规则不满足 |
| 401 | 未登录 |
| 403 | 已登录但无权限 |
| 404 | 资源不存在 |
| 409 | 并发冲突、库存不足、重复提交 |
| 500 | 未处理服务器错误 |

### 3.4 时间、金额、ID

- ID：C# 使用 `long`，数据库使用 `NUMBER(19)` 或 `NUMBER(10)`。
- 金额：C# 使用 `decimal`，数据库使用 `NUMBER(10,2)` 或 `NUMBER(12,2)`。
- 时间：服务端统一存 Oracle `DATE`，JSON 输出 ISO 8601 格式。
- 布尔值：数据库使用 `0/1`，C# 映射为 `bool`。

### 3.5 认证与权限

- 登录态：Cookie Authentication。
- Cookie 名称：`ECommerce.Auth`。
- 角色编码与数据库保持一致：`USER`、`SERVICE`、`ADMIN`。
- 普通用户页面要求 `USER` 或以上角色。
- 客服后台能力要求 `SERVICE` 或 `ADMIN`。
- 管理员后台能力要求 `ADMIN`。

建议策略名：

| Policy | 允许角色 | 用途 |
| --- | --- | --- |
| `CustomerOnly` | `USER`, `SERVICE`, `ADMIN` | 购物车、下单、评价、地址 |
| `ServiceOrAdmin` | `SERVICE`, `ADMIN` | 订单查询、发货、物流维护 |
| `AdminOnly` | `ADMIN` | 用户、角色、商品、优惠券、统计、日志 |

## 4. 状态枚举

| 领域 | 字段 | 值 |
| --- | --- | --- |
| 用户 | `USER.status` | `0=禁用`, `1=正常` |
| 商品 | `PRODUCT.status` | `0=下架`, `1=上架`, `2=预售` |
| SKU | `SKU.status` | `0=停售`, `1=在售` |
| 库存日志 | `INVENTORY_LOG.change_type` | `SALE`, `CANCEL`, `RESTOCK`, `ADJUST` |
| 优惠券模板 | `COUPON_TEMPLATE.type` | `1=满减券`, `2=折扣券` |
| 用户优惠券 | `USER_COUPON.status` | `0=未使用`, `1=已使用`, `2=已过期` |
| 订单 | `ORDER_MAIN.status` | `0=待支付`, `1=已支付`, `2=已发货`, `3=已完成`, `4=已取消` |
| 支付 | `PAYMENT.status` | `0=待支付`, `1=支付成功`, `2=支付失败`, `3=已退款` |
| 物流 | `LOGISTICS.status` | `0=已揽件`, `1=运输中`, `2=派件中`, `3=已签收` |
| 评价 | `REVIEW.status` | `0=审核中`, `1=已发布`, `2=已屏蔽` |
| 操作日志 | `OPERATION_LOG.result` | `0=失败`, `1=成功` |

## 5. 页面路由契约

| 模块 | 页面 | 路由 | 权限 | 负责人 |
| --- | --- | --- | --- | --- |
| 首页 | 首页 | `GET /` | 公开 | 第 1 人 |
| 登录 | 登录页 | `GET /account/login` | 公开 | 第 2 人 |
| 登录 | 注册页 | `GET /account/register` | 公开 | 第 2 人 |
| 商品 | 商品列表 | `GET /products` | 公开 | 第 3 人 |
| 商品 | 商品详情 | `GET /products/{productId}` | 公开 | 第 3 人 |
| 购物车 | 我的购物车 | `GET /cart` | `CustomerOnly` | 第 4 人 |
| 订单 | 我的订单 | `GET /orders` | `CustomerOnly` | 第 4 人 |
| 订单 | 订单详情 | `GET /orders/{orderId}` | `CustomerOnly` | 第 4 人 |
| 地址 | 收货地址 | `GET /addresses` | `CustomerOnly` | 第 2 人 |
| 优惠券 | 我的优惠券 | `GET /coupons` | `CustomerOnly` | 第 5 人 |
| 支付 | 支付页 | `GET /payment/{orderId}` | `CustomerOnly` | 第 5 人 |
| 后台 | 后台首页 | `GET /admin` | `ServiceOrAdmin` | 第 6 人 |
| 后台用户 | 用户管理 | `GET /admin/users` | `AdminOnly` | 第 2 人 |
| 后台日志 | 操作日志 | `GET /admin/operation-logs` | `AdminOnly` | 第 2 人 |
| 后台商品 | 商品管理 | `GET /admin/products` | `AdminOnly` | 第 3 人 |
| 后台分类 | 分类管理 | `GET /admin/categories` | `AdminOnly` | 第 3 人 |
| 后台库存 | 库存管理 | `GET /admin/inventory` | `AdminOnly` | 第 3 人 |
| 后台订单 | 订单管理 | `GET /admin/orders` | `ServiceOrAdmin` | 第 4 人 |
| 后台优惠券 | 优惠券管理 | `GET /admin/coupons` | `AdminOnly` | 第 5 人 |
| 后台物流 | 物流管理 | `GET /admin/logistics` | `ServiceOrAdmin` | 第 5 人 |
| 后台统计 | 统计报表 | `GET /admin/statistics` | `AdminOnly` | 第 6 人 |

## 6. JSON API 契约

### 6.1 系统基础与部署

| 方法 | 路径 | 权限 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| `GET` | `/health` | 公开 | 应用健康检查 | 第 1 人 |
| `GET` | `/api/v1/system/db-check` | `AdminOnly` | Oracle 连接检查 | 第 1 人 |
| `GET` | `/api/v1/system/version` | 公开 | 返回版本、环境、构建时间 | 第 1 人 |

`GET /api/v1/system/db-check` 响应数据：

```json
{
  "connected": true,
  "database": "Oracle",
  "serverTime": "2026-07-06T15:00:00+08:00"
}
```

### 6.2 用户、角色、地址、日志

| 方法 | 路径 | 权限 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| `POST` | `/api/v1/auth/register` | 公开 | 用户注册 | 第 2 人 |
| `POST` | `/api/v1/auth/login` | 公开 | 用户登录 | 第 2 人 |
| `POST` | `/api/v1/auth/logout` | 已登录 | 退出登录 | 第 2 人 |
| `GET` | `/api/v1/auth/me` | 已登录 | 当前登录用户 | 第 2 人 |
| `GET` | `/api/v1/admin/users` | `AdminOnly` | 用户分页查询 | 第 2 人 |
| `PUT` | `/api/v1/admin/users/{userId}/status` | `AdminOnly` | 启用/禁用用户 | 第 2 人 |
| `PUT` | `/api/v1/admin/users/{userId}/roles` | `AdminOnly` | 分配角色 | 第 2 人 |
| `GET` | `/api/v1/addresses` | `CustomerOnly` | 我的地址列表 | 第 2 人 |
| `POST` | `/api/v1/addresses` | `CustomerOnly` | 新增地址 | 第 2 人 |
| `PUT` | `/api/v1/addresses/{addressId}` | `CustomerOnly` | 修改地址 | 第 2 人 |
| `DELETE` | `/api/v1/addresses/{addressId}` | `CustomerOnly` | 删除地址 | 第 2 人 |
| `PUT` | `/api/v1/addresses/{addressId}/default` | `CustomerOnly` | 设为默认地址 | 第 2 人 |
| `GET` | `/api/v1/admin/operation-logs` | `AdminOnly` | 操作日志分页查询 | 第 2 人 |

核心 DTO：

```csharp
public sealed record RegisterRequest(
    string Username,
    string Password,
    string? Phone,
    string? Email);

public sealed record LoginRequest(
    string Username,
    string Password,
    bool RememberMe);

public sealed record AddressRequest(
    string ReceiverName,
    string ReceiverPhone,
    string Province,
    string City,
    string District,
    string DetailAddress,
    bool IsDefault);
```

### 6.3 商品、分类、SKU、库存

| 方法 | 路径 | 权限 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| `GET` | `/api/v1/categories` | 公开 | 前台分类树 | 第 3 人 |
| `GET` | `/api/v1/products` | 公开 | 前台商品分页、搜索、分类筛选 | 第 3 人 |
| `GET` | `/api/v1/products/{productId}` | 公开 | 商品详情，含图片、规格、SKU | 第 3 人 |
| `GET` | `/api/v1/admin/categories` | `AdminOnly` | 后台分类列表 | 第 3 人 |
| `POST` | `/api/v1/admin/categories` | `AdminOnly` | 新增分类 | 第 3 人 |
| `PUT` | `/api/v1/admin/categories/{categoryId}` | `AdminOnly` | 修改分类 | 第 3 人 |
| `DELETE` | `/api/v1/admin/categories/{categoryId}` | `AdminOnly` | 删除或禁用分类 | 第 3 人 |
| `GET` | `/api/v1/admin/products` | `AdminOnly` | 后台商品分页 | 第 3 人 |
| `POST` | `/api/v1/admin/products` | `AdminOnly` | 新增商品 | 第 3 人 |
| `PUT` | `/api/v1/admin/products/{productId}` | `AdminOnly` | 修改商品 | 第 3 人 |
| `PUT` | `/api/v1/admin/products/{productId}/status` | `AdminOnly` | 商品上下架 | 第 3 人 |
| `POST` | `/api/v1/admin/products/{productId}/images` | `AdminOnly` | 新增商品图片 | 第 3 人 |
| `DELETE` | `/api/v1/admin/product-images/{imageId}` | `AdminOnly` | 删除商品图片 | 第 3 人 |
| `GET` | `/api/v1/admin/products/{productId}/skus` | `AdminOnly` | SKU 列表 | 第 3 人 |
| `POST` | `/api/v1/admin/products/{productId}/skus` | `AdminOnly` | 新增 SKU | 第 3 人 |
| `PUT` | `/api/v1/admin/skus/{skuId}` | `AdminOnly` | 修改 SKU | 第 3 人 |
| `PUT` | `/api/v1/admin/skus/{skuId}/status` | `AdminOnly` | SKU 启停 | 第 3 人 |
| `POST` | `/api/v1/admin/skus/{skuId}/inventory-adjustments` | `AdminOnly` | 库存调整 | 第 3 人 |
| `GET` | `/api/v1/admin/inventory/warnings` | `AdminOnly` | 库存预警列表 | 第 3 人 |
| `GET` | `/api/v1/admin/inventory/logs` | `AdminOnly` | 库存变动日志 | 第 3 人 |

核心 DTO：

```csharp
public sealed record ProductSaveRequest(
    long CategoryId,
    string Name,
    string? Description,
    string MainImage,
    int Status,
    IReadOnlyList<ProductImageRequest> Images,
    IReadOnlyList<ProductSpecRequest> Specs,
    IReadOnlyList<SkuSaveRequest> Skus);

public sealed record SkuSaveRequest(
    string SpecDescJson,
    decimal Price,
    decimal? OriginalPrice,
    int Stock,
    int WarningStock,
    string? SkuImage,
    int Status);

public sealed record InventoryAdjustRequest(
    int ChangeQty,
    string Remark);
```

### 6.4 购物车与订单核心流程

| 方法 | 路径 | 权限 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| `GET` | `/api/v1/cart` | `CustomerOnly` | 查看购物车 | 第 4 人 |
| `POST` | `/api/v1/cart/items` | `CustomerOnly` | 加入购物车 | 第 4 人 |
| `PUT` | `/api/v1/cart/items/{cartItemId}` | `CustomerOnly` | 修改数量或选中状态 | 第 4 人 |
| `DELETE` | `/api/v1/cart/items/{cartItemId}` | `CustomerOnly` | 删除购物车项 | 第 4 人 |
| `DELETE` | `/api/v1/cart` | `CustomerOnly` | 清空购物车 | 第 4 人 |
| `POST` | `/api/v1/orders/preview` | `CustomerOnly` | 下单预览，计算金额和优惠 | 第 4 人 |
| `POST` | `/api/v1/orders` | `CustomerOnly` | 创建订单并锁定库存 | 第 4 人 |
| `GET` | `/api/v1/orders` | `CustomerOnly` | 我的订单分页 | 第 4 人 |
| `GET` | `/api/v1/orders/{orderId}` | `CustomerOnly` | 订单详情 | 第 4 人 |
| `POST` | `/api/v1/orders/{orderId}/cancel` | `CustomerOnly` | 取消订单并释放库存 | 第 4 人 |
| `POST` | `/api/v1/orders/{orderId}/confirm` | `CustomerOnly` | 确认收货 | 第 4 人 |
| `GET` | `/api/v1/orders/{orderId}/logs` | `CustomerOnly` | 订单状态日志 | 第 4 人 |
| `GET` | `/api/v1/admin/orders` | `ServiceOrAdmin` | 后台订单分页 | 第 4 人 |
| `GET` | `/api/v1/admin/orders/{orderId}` | `ServiceOrAdmin` | 后台订单详情 | 第 4 人 |

核心 DTO：

```csharp
public sealed record CartItemRequest(
    long SkuId,
    int Quantity);

public sealed record UpdateCartItemRequest(
    int Quantity,
    bool Selected);

public sealed record CreateOrderRequest(
    long AddressId,
    long? UserCouponId,
    IReadOnlyList<long> CartItemIds,
    string? Remark);
```

创建订单事务要求：

1. 校验用户、地址、购物车项、SKU 状态和可售库存。
2. 计算商品总额、优惠金额、实付金额。
3. 新增 `ORDER_MAIN`、`ORDER_ITEM`、`ORDER_LOG`。
4. 增加 SKU `locked_stock`。
5. 写入 `INVENTORY_LOG`，`change_type=SALE`，此时只锁定库存，不减少 `stock`。
6. 清理已下单购物车项。

### 6.5 支付、优惠券、物流、评价

| 方法 | 路径 | 权限 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| `GET` | `/api/v1/coupons` | `CustomerOnly` | 我的优惠券 | 第 5 人 |
| `GET` | `/api/v1/coupon-templates/available` | `CustomerOnly` | 可领取优惠券 | 第 5 人 |
| `POST` | `/api/v1/coupon-templates/{templateId}/receive` | `CustomerOnly` | 领取优惠券 | 第 5 人 |
| `POST` | `/api/v1/coupons/{userCouponId}/validate` | `CustomerOnly` | 下单前校验优惠券 | 第 5 人 |
| `GET` | `/api/v1/admin/coupon-templates` | `AdminOnly` | 优惠券模板分页 | 第 5 人 |
| `POST` | `/api/v1/admin/coupon-templates` | `AdminOnly` | 新增优惠券模板 | 第 5 人 |
| `PUT` | `/api/v1/admin/coupon-templates/{templateId}` | `AdminOnly` | 修改优惠券模板 | 第 5 人 |
| `PUT` | `/api/v1/admin/coupon-templates/{templateId}/status` | `AdminOnly` | 启停优惠券模板 | 第 5 人 |
| `POST` | `/api/v1/payments/simulate` | `CustomerOnly` | 模拟支付 | 第 5 人 |
| `GET` | `/api/v1/payments/{orderId}` | `CustomerOnly` | 查询支付状态 | 第 5 人 |
| `POST` | `/api/v1/payments/callback/simulated` | 公开 | 模拟支付回调 | 第 5 人 |
| `POST` | `/api/v1/admin/orders/{orderId}/shipments` | `ServiceOrAdmin` | 后台发货 | 第 5 人 |
| `GET` | `/api/v1/logistics/{orderId}` | `CustomerOnly` | 查询物流 | 第 5 人 |
| `POST` | `/api/v1/admin/logistics/{logisticsId}/tracks` | `ServiceOrAdmin` | 录入物流轨迹 | 第 5 人 |
| `POST` | `/api/v1/reviews` | `CustomerOnly` | 提交评价 | 第 5 人 |
| `GET` | `/api/v1/products/{productId}/reviews` | 公开 | 商品评价列表 | 第 5 人 |
| `GET` | `/api/v1/admin/reviews` | `AdminOnly` | 后台评价审核列表 | 第 5 人 |
| `PUT` | `/api/v1/admin/reviews/{reviewId}/status` | `AdminOnly` | 审核、屏蔽评价 | 第 5 人 |

支付成功事务要求：

1. `PAYMENT.status` 改为 `1=支付成功`。
2. `ORDER_MAIN.status` 从 `0=待支付` 改为 `1=已支付`。
3. `SKU.stock` 扣减购买数量，`SKU.locked_stock` 减少锁定数量。
4. `USER_COUPON.status` 改为 `1=已使用`，写入 `used_at` 和 `order_id`。
5. 写入 `ORDER_LOG` 和必要的 `INVENTORY_LOG`。

核心 DTO：

```csharp
public sealed record CouponTemplateRequest(
    string Name,
    int Type,
    decimal Amount,
    decimal MinAmount,
    int TotalCount,
    DateTime StartTime,
    DateTime EndTime,
    int Status);

public sealed record SimulatePaymentRequest(
    long OrderId,
    string PayMethod);

public sealed record ShipmentRequest(
    string CompanyName,
    string TrackingNo,
    DateTime? ShippedAt);

public sealed record ReviewRequest(
    long OrderId,
    long ProductId,
    int Rating,
    string? Content,
    IReadOnlyList<string> Images,
    bool IsAnonymous);
```

### 6.6 统计、导出、后台首页、文档

| 方法 | 路径 | 权限 | 说明 | 负责人 |
| --- | --- | --- | --- | --- |
| `GET` | `/api/v1/admin/dashboard/summary` | `AdminOnly` | 后台首页汇总卡片 | 第 6 人 |
| `GET` | `/api/v1/admin/statistics/orders` | `AdminOnly` | 订单统计，支持日/月维度 | 第 6 人 |
| `GET` | `/api/v1/admin/statistics/top-products` | `AdminOnly` | 商品销量排行 | 第 6 人 |
| `GET` | `/api/v1/admin/exports/orders` | `AdminOnly` | 导出订单 Excel | 第 6 人 |
| `GET` | `/api/v1/admin/exports/inventory` | `AdminOnly` | 导出库存 Excel | 第 6 人 |

导出接口直接返回文件：

- `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- 文件名示例：`orders_20260706.xlsx`

## 7. Application Service 接口

Service 放在 `ECommerce.Application/Services`。所有异步方法以 `Async` 结尾，返回 `Task<T>`。

```csharp
public interface IAuthService
{
    Task<UserSessionDto> RegisterAsync(RegisterRequest request);
    Task<UserSessionDto> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<UserSessionDto?> GetCurrentUserAsync();
}

public interface IUserService
{
    Task<PagedResult<UserDto>> SearchUsersAsync(UserQuery query);
    Task SetUserStatusAsync(long userId, int status, long operatorId);
    Task AssignRolesAsync(long userId, IReadOnlyList<int> roleIds, long operatorId);
}

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetMyAddressesAsync(long userId);
    Task<long> CreateAsync(long userId, AddressRequest request);
    Task UpdateAsync(long userId, long addressId, AddressRequest request);
    Task DeleteAsync(long userId, long addressId);
    Task SetDefaultAsync(long userId, long addressId);
}

public interface IOperationLogService
{
    Task WriteAsync(OperationLogRequest request);
    Task<PagedResult<OperationLogDto>> SearchAsync(OperationLogQuery query);
}

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(bool includeDisabled);
    Task<int> CreateAsync(CategoryRequest request, long operatorId);
    Task UpdateAsync(int categoryId, CategoryRequest request, long operatorId);
    Task DeleteOrDisableAsync(int categoryId, long operatorId);
}

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> SearchAsync(ProductQuery query);
    Task<ProductDetailDto> GetDetailAsync(long productId);
    Task<long> CreateAsync(ProductSaveRequest request, long operatorId);
    Task UpdateAsync(long productId, ProductSaveRequest request, long operatorId);
    Task SetStatusAsync(long productId, int status, long operatorId);
}

public interface ISkuService
{
    Task<IReadOnlyList<SkuDto>> GetByProductAsync(long productId);
    Task<long> CreateAsync(long productId, SkuSaveRequest request, long operatorId);
    Task UpdateAsync(long skuId, SkuSaveRequest request, long operatorId);
    Task SetStatusAsync(long skuId, int status, long operatorId);
}

public interface IInventoryService
{
    Task AdjustAsync(long skuId, InventoryAdjustRequest request, long operatorId);
    Task LockForOrderAsync(long orderId, IReadOnlyList<OrderSkuQuantity> items);
    Task ReleaseForCancelledOrderAsync(long orderId);
    Task DeductForPaidOrderAsync(long orderId);
    Task<PagedResult<InventoryLogDto>> SearchLogsAsync(InventoryLogQuery query);
    Task<PagedResult<InventoryWarningDto>> SearchWarningsAsync(PageQuery query);
}

public interface ICartService
{
    Task<CartDto> GetCartAsync(long userId);
    Task AddItemAsync(long userId, CartItemRequest request);
    Task UpdateItemAsync(long userId, long cartItemId, UpdateCartItemRequest request);
    Task RemoveItemAsync(long userId, long cartItemId);
    Task ClearAsync(long userId);
}

public interface IOrderService
{
    Task<OrderPreviewDto> PreviewAsync(long userId, CreateOrderRequest request);
    Task<long> CreateAsync(long userId, CreateOrderRequest request);
    Task<PagedResult<OrderListItemDto>> SearchMineAsync(long userId, OrderQuery query);
    Task<PagedResult<OrderListItemDto>> SearchAdminAsync(AdminOrderQuery query);
    Task<OrderDetailDto> GetDetailAsync(long userId, long orderId);
    Task<OrderDetailDto> GetAdminDetailAsync(long orderId);
    Task CancelAsync(long userId, long orderId, string? reason);
    Task ConfirmAsync(long userId, long orderId);
}

public interface ICouponService
{
    Task<PagedResult<CouponTemplateDto>> SearchTemplatesAsync(CouponTemplateQuery query);
    Task<int> CreateTemplateAsync(CouponTemplateRequest request, long operatorId);
    Task UpdateTemplateAsync(int templateId, CouponTemplateRequest request, long operatorId);
    Task SetTemplateStatusAsync(int templateId, int status, long operatorId);
    Task ReceiveAsync(long userId, int templateId);
    Task<IReadOnlyList<UserCouponDto>> GetMineAsync(long userId);
    Task<CouponValidationDto> ValidateAsync(long userId, long userCouponId, decimal orderAmount);
}

public interface IPaymentService
{
    Task<PaymentDto> CreateOrGetPendingAsync(long userId, long orderId);
    Task<PaymentResultDto> SimulatePayAsync(long userId, SimulatePaymentRequest request);
    Task SyncSimulatedCallbackAsync(SimulatedPaymentCallback request);
    Task<PaymentDto> GetByOrderAsync(long userId, long orderId);
}

public interface ILogisticsService
{
    Task ShipAsync(long orderId, ShipmentRequest request, long operatorId);
    Task AddTrackAsync(long logisticsId, LogisticsTrackRequest request, long operatorId);
    Task<LogisticsDto?> GetByOrderAsync(long userId, long orderId);
}

public interface IReviewService
{
    Task<long> CreateAsync(long userId, ReviewRequest request);
    Task<PagedResult<ReviewDto>> SearchByProductAsync(long productId, PageQuery query);
    Task<PagedResult<ReviewDto>> SearchAdminAsync(ReviewQuery query);
    Task SetStatusAsync(long reviewId, int status, long operatorId);
}

public interface IStatisticsService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    Task<OrderStatisticsDto> GetOrderStatisticsAsync(StatisticsQuery query);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(StatisticsQuery query);
}

public interface IExportService
{
    Task<FileExportDto> ExportOrdersAsync(AdminOrderQuery query);
    Task<FileExportDto> ExportInventoryAsync(InventoryLogQuery query);
}
```

## 8. Repository 与事务边界

Repository 放在 `ECommerce.Infrastructure/Repositories`，接口可放在 Application 或 Infrastructure abstractions 目录中。命名统一为 `I{Entity}Repository`。

必须支持事务的方法：

| 业务 | 事务边界 |
| --- | --- |
| 注册 | 新增用户、分配默认角色 |
| 创建订单 | 订单主表、明细、订单日志、库存锁定、库存日志、购物车清理 |
| 取消订单 | 订单状态、订单日志、锁定库存释放、库存日志 |
| 支付成功 | 支付状态、订单状态、库存扣减、优惠券核销、订单日志、库存日志 |
| 发货 | 物流信息、订单状态、订单日志 |
| 默认地址 | 取消旧默认地址、设置新默认地址 |

建议公共接口：

```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
```

具体数据访问技术由第 1 人在项目骨架中确定，但对上层暴露的 Service 接口不变。

## 9. 错误码

| 错误码 | 含义 |
| --- | --- |
| `VALIDATION_ERROR` | 参数校验失败 |
| `AUTH_INVALID_CREDENTIALS` | 用户名或密码错误 |
| `AUTH_FORBIDDEN` | 无访问权限 |
| `USER_DISABLED` | 用户已禁用 |
| `RESOURCE_NOT_FOUND` | 资源不存在 |
| `PRODUCT_OFF_SHELF` | 商品已下架 |
| `SKU_NOT_AVAILABLE` | SKU 不可售 |
| `ORDER_STOCK_NOT_ENOUGH` | 库存不足 |
| `ORDER_STATUS_INVALID` | 订单状态不允许当前操作 |
| `COUPON_NOT_AVAILABLE` | 优惠券不可用 |
| `PAYMENT_ALREADY_PAID` | 订单已支付 |
| `EXPORT_FAILED` | 导出失败 |

## 10. 模块边界

| 负责人 | 可直接修改 | 需要先沟通 |
| --- | --- | --- |
| 第 1 人 | 项目骨架、配置、数据库连接、部署、README | 公共响应结构、认证中间件、依赖注入 |
| 第 2 人 | 用户、角色、权限、地址、操作日志 | Cookie/Policy 名称、用户表结构 |
| 第 3 人 | 分类、商品、图片、规格、SKU、库存日志 | 订单库存锁定/扣减规则 |
| 第 4 人 | 购物车、订单、订单日志 | 优惠券抵扣、支付成功扣库存 |
| 第 5 人 | 优惠券、支付、物流、评价 | 订单状态流转、库存扣减 |
| 第 6 人 | 统计、Excel、后台首页、UI、测试、文档、PPT | 公共布局、导出字段、统计口径 |

跨模块改动先更新本文档，再开发代码。
