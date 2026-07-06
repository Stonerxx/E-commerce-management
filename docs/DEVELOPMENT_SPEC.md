# 开发规范

本文件定义项目结构、接口规范、代码规范、API 格式、路由、事务边界和跨模块协作规则。开发公共接口、DTO、路由、状态码或事务逻辑时，以本文件和源码接口为准。

## 1. 工作原则

- 本项目采用 B/S 模式：浏览器访问 ASP.NET Core MVC 服务，数据存 Oracle。
- 目标框架是 `net8.0`，解决方案是 `ECommerce.sln`。
- 先读现有源码，再改代码；不要发明与现有契约不一致的新接口。
- Controller 只能调用 Application Service，不直接访问数据库。
- 跨模块调用只依赖 `src/ECommerce.Application/Services` 中的接口和 `src/ECommerce.Application/DTOs` 中的 DTO。
- 公共响应、分页、错误码、权限常量以 `src/ECommerce.Shared` 为准。
- 状态枚举以 `src/ECommerce.Domain/Enums` 为准。
- 如果必须修改公共接口、DTO、状态码、路由或枚举，必须同时更新本文件。
- 不要回滚、覆盖或格式化无关文件。
- 提交信息使用 `<type>(<scope>)：中文说明`。

## 2. 项目结构

```text
src/
  ECommerce.Web/                 # MVC 表现层：Controllers, Views, ViewModels, Filters
  ECommerce.Application/         # 应用层：DTOs, Services, Validators, Use cases
  ECommerce.Domain/              # 领域层：Entities, Enums, Domain services
  ECommerce.Infrastructure/      # 基础设施层：Oracle, Repositories, UnitOfWork
  ECommerce.Shared/              # 公共层：ApiResponse, Pagination, Constants, Exceptions
tests/
  ECommerce.Tests/
```

依赖方向：

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application + Domain + Shared
Application -> Domain + Shared
Infrastructure -> Application + Domain + Shared
Shared -> no project dependency
```

## 3. 接口与代码入口

| 契约 | 源码位置 |
| --- | --- |
| 统一响应 | `src/ECommerce.Shared/Contracts/ApiResponse.cs` |
| 分页 | `src/ECommerce.Shared/Contracts/PageQuery.cs`, `PagedResult.cs` |
| 文件导出 | `src/ECommerce.Shared/Contracts/FileExportDto.cs` |
| 权限与角色 | `src/ECommerce.Shared/Constants/AuthConstants.cs` |
| 错误码 | `src/ECommerce.Shared/Errors/ErrorCodes.cs` |
| 业务异常 | `src/ECommerce.Shared/Exceptions/BusinessException.cs` |
| 业务枚举 | `src/ECommerce.Domain/Enums/BusinessEnums.cs` |
| 库存变动类型 | `src/ECommerce.Domain/Enums/InventoryChangeType.cs` |
| DTO | `src/ECommerce.Application/DTOs/*.cs` |
| Service 接口 | `src/ECommerce.Application/Services/*.cs` |
| API 路由骨架 | `src/ECommerce.Web/Controllers/Api/*.cs` |
| 页面 Controller | `src/ECommerce.Web/Controllers/*.cs` |
| Oracle 连接 | `src/ECommerce.Infrastructure/Data/*.cs` |
| DI 注册 | `src/ECommerce.Infrastructure/DependencyInjection.cs`, `src/ECommerce.Web/Program.cs` |
| 数据库脚本 | `migration/init_database.sql` |

当前所有 API Controller 只是占位，默认返回 `501 NOT_IMPLEMENTED`。实现功能时应把对应 Controller 改为调用 Service。

## 4. 运行与验证

```powershell
dotnet restore ECommerce.sln
dotnet build ECommerce.sln
dotnet test ECommerce.sln
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

运行后至少验证：

```text
GET /health
GET /account/login
```

Oracle 连接使用 `Oracle:ConnectionString`，推荐通过环境变量覆盖，不要提交真实密码：

```powershell
$env:Oracle__ConnectionString = "User Id=...;Password=...;Data Source=localhost:1521/XEPDB1"
```

## 5. 分支职责

| 分支 | 主责 | 直接负责接口 |
| --- | --- | --- |
| `feat-member1-foundation-oracle-deploy` | 项目骨架、Oracle、部署 | `IUnitOfWork`、Oracle 连接、认证/授权、DI、README |
| `feat-member2-user-permission-address-log` | 用户、权限、地址、日志 | `IAuthService`、`IUserService`、`IAddressService`、`IOperationLogService` |
| `feat-member3-product-category-sku-inventory` | 商品、分类、SKU、库存 | `ICategoryService`、`IProductService`、`ISkuService`、`IInventoryService` |
| `feat-member4-cart-order-core` | 购物车、订单核心流程 | `ICartService`、`IOrderService` |
| `feat-member5-payment-coupon-logistics-review` | 支付、优惠券、物流、评价 | `ICouponService`、`IPaymentService`、`ILogisticsService`、`IReviewService` |
| `feat-member6-stats-export-ui-docs` | 统计、导出、UI、测试、文档、PPT | `IStatisticsService`、`IExportService`、后台首页、统一 Bootstrap 页面 |

## 6. 路由与权限

认证：

- Cookie 名称：`ECommerce.Auth`。
- 角色：`USER`、`SERVICE`、`ADMIN`。
- Policy：`CustomerOnly`、`ServiceOrAdmin`、`AdminOnly`。

页面路由：

| 页面 | 路由 | 权限 |
| --- | --- | --- |
| 首页 | `GET /` | 公开 |
| 登录 | `GET /account/login` | 公开 |
| 注册 | `GET /account/register` | 公开 |
| 商品列表 | `GET /products` | 公开 |
| 商品详情 | `GET /products/{productId}` | 公开 |
| 购物车 | `GET /cart` | `CustomerOnly` |
| 我的订单 | `GET /orders` | `CustomerOnly` |
| 订单详情 | `GET /orders/{orderId}` | `CustomerOnly` |
| 地址 | `GET /addresses` | `CustomerOnly` |
| 我的优惠券 | `GET /coupons` | `CustomerOnly` |
| 支付页 | `GET /payment/{orderId}` | `CustomerOnly` |
| 后台首页 | `GET /admin`, `GET /admin/dashboard` | `ServiceOrAdmin` |
| 后台用户 | `GET /admin/users` | `AdminOnly` |
| 后台商品 | `GET /admin/products` | `AdminOnly` |
| 后台订单 | `GET /admin/orders` | `ServiceOrAdmin` |
| 后台统计 | `GET /admin/statistics` | `AdminOnly` |

JSON API 路由以 `src/ECommerce.Web/Controllers/Api/*.cs` 为准。不要在多个 Controller 中定义重复路由。

## 7. 核心 API 分组

系统：

- `GET /health`
- `GET /api/v1/system/version`
- `GET /api/v1/system/db-check`

用户、权限、地址、日志：

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `GET /api/v1/auth/me`
- `GET /api/v1/admin/users`
- `PUT /api/v1/admin/users/{userId}/status`
- `PUT /api/v1/admin/users/{userId}/roles`
- `GET|POST /api/v1/addresses`
- `PUT|DELETE /api/v1/addresses/{addressId}`
- `PUT /api/v1/addresses/{addressId}/default`

商品、分类、SKU、库存：

- `GET /api/v1/categories`
- `GET /api/v1/products`
- `GET /api/v1/products/{productId}`
- `GET|POST /api/v1/admin/categories`
- `PUT|DELETE /api/v1/admin/categories/{categoryId}`
- `GET|POST /api/v1/admin/products`
- `PUT /api/v1/admin/products/{productId}`
- `PUT /api/v1/admin/products/{productId}/status`
- `POST /api/v1/admin/products/{productId}/images`
- `DELETE /api/v1/admin/product-images/{imageId}`
- `GET|POST /api/v1/admin/products/{productId}/skus`
- `PUT /api/v1/admin/skus/{skuId}`
- `PUT /api/v1/admin/skus/{skuId}/status`
- `POST /api/v1/admin/skus/{skuId}/inventory-adjustments`
- `GET /api/v1/admin/inventory/warnings`
- `GET /api/v1/admin/inventory/logs`

购物车、订单：

- `GET|DELETE /api/v1/cart`
- `POST /api/v1/cart/items`
- `PUT|DELETE /api/v1/cart/items/{cartItemId}`
- `POST /api/v1/orders/preview`
- `GET|POST /api/v1/orders`
- `GET /api/v1/orders/{orderId}`
- `POST /api/v1/orders/{orderId}/cancel`
- `POST /api/v1/orders/{orderId}/confirm`
- `GET /api/v1/orders/{orderId}/logs`
- `GET /api/v1/admin/orders`
- `GET /api/v1/admin/orders/{orderId}`

支付、优惠券、物流、评价：

- `GET /api/v1/coupons`
- `GET /api/v1/coupon-templates/available`
- `POST /api/v1/coupon-templates/{templateId}/receive`
- `POST /api/v1/coupons/{userCouponId}/validate`
- `GET|POST /api/v1/admin/coupon-templates`
- `PUT /api/v1/admin/coupon-templates/{templateId}`
- `PUT /api/v1/admin/coupon-templates/{templateId}/status`
- `POST /api/v1/payments/simulate`
- `GET /api/v1/payments/{orderId}`
- `POST /api/v1/payments/callback/simulated`
- `POST /api/v1/admin/orders/{orderId}/shipments`
- `GET /api/v1/logistics/{orderId}`
- `POST /api/v1/admin/logistics/{logisticsId}/tracks`
- `POST /api/v1/reviews`
- `GET /api/v1/products/{productId}/reviews`
- `GET /api/v1/admin/reviews`
- `PUT /api/v1/admin/reviews/{reviewId}/status`

统计、导出：

- `GET /api/v1/admin/dashboard/summary`
- `GET /api/v1/admin/statistics/orders`
- `GET /api/v1/admin/statistics/top-products`
- `GET /api/v1/admin/exports/orders`
- `GET /api/v1/admin/exports/inventory`

## 8. 状态值

| 领域 | 值 |
| --- | --- |
| 用户 | `0=禁用`, `1=正常` |
| 商品 | `0=下架`, `1=上架`, `2=预售` |
| SKU | `0=停售`, `1=在售` |
| 库存日志 | `SALE`, `CANCEL`, `RESTOCK`, `ADJUST` |
| 优惠券模板 | `1=满减券`, `2=折扣券` |
| 用户优惠券 | `0=未使用`, `1=已使用`, `2=已过期` |
| 订单 | `0=待支付`, `1=已支付`, `2=已发货`, `3=已完成`, `4=已取消` |
| 支付 | `0=待支付`, `1=支付成功`, `2=支付失败`, `3=已退款` |
| 物流 | `0=已揽件`, `1=运输中`, `2=派件中`, `3=已签收` |
| 评价 | `0=审核中`, `1=已发布`, `2=已屏蔽` |
| 操作日志 | `0=失败`, `1=成功` |

## 9. 跨分支协作链路

| 场景 | 发起模块 | 必须依赖 |
| --- | --- | --- |
| 登录 | 用户模块 | Cookie Authentication、Policy |
| 加入购物车 | 购物车模块 | 商品/SKU 查询，校验商品上架、SKU 在售 |
| 创建订单 | 订单模块 | 地址、SKU 库存锁定、优惠券校验 |
| 取消订单 | 订单模块 | 释放锁定库存、写订单日志 |
| 支付成功 | 支付模块 | 订单支付上下文、扣减库存、核销优惠券、标记订单已支付 |
| 发货 | 物流模块 | 创建物流、标记订单已发货、写订单日志 |
| 确认收货 | 订单模块 | 可选校验物流，标记订单完成 |
| 评价 | 评价模块 | 校验订单已完成且包含该商品 |
| 统计导出 | 统计模块 | 订单、商品、库存、支付状态口径一致 |

关键接口名：

- `IInventoryService.LockForOrderAsync`
- `IInventoryService.ReleaseForCancelledOrderAsync`
- `IInventoryService.DeductForPaidOrderAsync`
- `IOrderService.GetPaymentContextAsync`
- `IOrderService.GetSkuQuantitiesAsync`
- `IOrderService.MarkPaidAsync`
- `IOrderService.MarkShippedAsync`
- `ICouponService.ValidateAsync`
- `ICouponService.UseForOrderAsync`

## 10. 事务边界

必须支持事务的业务：

| 业务 | 同一事务内完成 |
| --- | --- |
| 注册 | 新增用户、分配默认角色 |
| 创建订单 | 订单主表、明细、订单日志、库存锁定、库存日志、购物车清理 |
| 取消订单 | 订单状态、订单日志、锁定库存释放、库存日志 |
| 支付成功 | 支付状态、订单状态、库存扣减、优惠券核销、订单日志、库存日志 |
| 发货 | 物流信息、订单状态、订单日志 |
| 默认地址 | 取消旧默认地址、设置新默认地址 |

`IUnitOfWork` 在 `src/ECommerce.Shared/Abstractions/IUnitOfWork.cs`。

## 11. 数据库命名

- 表名：大写下划线，例如 `ORDER_MAIN`；保留字表使用双引号，例如 `"USER"`。
- 字段名：小写下划线，例如 `created_at`。
- 主键：统一为 `id`。
- 外键：`{entity}_id`，例如 `user_id`、`order_id`。
- C# Entity/DTO 属性使用 PascalCase，例如 `OrderNo`、`LockedStock`。

## 12. UI、Vue 与页面

- 使用 Bootstrap。
- 前台布局：`Views/Shared/_Layout.cshtml`。
- 后台布局：`Views/Shared/_AdminLayout.cshtml`。
- 表单必须显示校验错误。
- 列表页必须有空状态、分页和筛选。
- 删除、取消订单、禁用用户、商品下架等重要操作必须二次确认。

### 12.1 Vue 页面样板

后台首页已提供 Vue 示例页面：

```text
GET /admin
GET /admin/dashboard
```

相关文件：

```text
src/ECommerce.Web/Controllers/AdminController.cs
src/ECommerce.Web/Views/Admin/Dashboard.cshtml
src/ECommerce.Web/wwwroot/js/admin-dashboard.js
```

页面组织方式：

- Razor View 负责页面路由、布局、首屏 HTML 结构。
- Vue 负责局部交互，例如状态卡片、列表筛选、按钮刷新、接口状态展示。
- Bootstrap 负责基础 UI 样式，避免每个模块单独写一套样式。
- 每个页面的 Vue 逻辑放在 `wwwroot/js/{module-page}.js`，不要把大量 JavaScript 写进 `.cshtml`。
- 业务数据统一通过 `/api/v1/...` 获取，按 `ApiResponse<T>` 解析。
- Razor 页面里不要使用 Vue 的 `@click` 写法，避免和 Razor 语法冲突；统一使用 `v-on:click`。
- 需要绑定 class、style 时使用 `v-bind:class`、`v-bind:style`，写法保持清晰。
- 当前没有引入 Vite/npm 前端构建流程；如果后续要改为独立 Vue 工程，必须先统一更新本文档和项目结构。

Dashboard 样板当前使用示例数据，后续由第 6 人接入：

```text
GET /api/v1/admin/dashboard/summary
GET /api/v1/admin/statistics/orders
GET /api/v1/admin/statistics/top-products
```

其他组员写后台页面时，可以照这个结构建：

```text
Views/AdminXxx/Index.cshtml
wwwroot/js/admin-xxx.js
Controllers/AdminXxxController.cs
Controllers/Api/AdminXxxApiController.cs
```

## 13. 操作日志

后台关键写操作都要记录 `OPERATION_LOG`：

- 禁用/启用用户、分配角色。
- 新增/修改/上下架商品、删除图片。
- 手工调整库存。
- 新增/修改/启停优惠券。
- 后台取消订单、发货。
- 审核/屏蔽评价。

日志描述要能看出谁在什么时候对哪个资源做了什么，请求参数要脱敏。

## 14. 提交和收尾

提交信息：

```text
<type>(<scope>)：中文说明
```

改完必须尽量运行：

```powershell
dotnet build ECommerce.sln
dotnet test ECommerce.sln
```

不要提交：

- 真实密码。
- `bin/`、`obj/`。
- Office 临时文件 `~$xxx.docx`。
- 与任务无关的格式化。
