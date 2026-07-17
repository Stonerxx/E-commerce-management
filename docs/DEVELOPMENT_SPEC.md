# 开发规范

本文件定义项目结构、接口格式、代码规范、API 格式、路由、事务边界和跨模块协作规则。开发公共接口、DTO、路由、状态码或事务逻辑时，以本文件和源码接口为准。

## 1. 工作原则

- 本项目采用 B/S 模式：浏览器访问 ASP.NET Core MVC 服务，数据存 Oracle。
- 目标框架是 `net8.0`，解决方案是 `ECommerce.sln`。
- 先读现有源码，再改代码；不要发明与现有接口格式不一致的新接口。
- Controller 只能调用 Application Service，不直接访问数据库。
- 跨模块调用只依赖 `src/ECommerce.Application/Services` 中的接口和 `src/ECommerce.Application/DTOs` 中的 DTO。
- 公共响应、分页、错误码、权限常量以 `src/ECommerce.Shared` 为准。
- 状态枚举以 `src/ECommerce.Domain/Enums` 为准。
- 如果必须修改公共接口、DTO、状态码、路由或枚举，必须同时更新本文件。
- 不要回滚、覆盖或格式化无关文件。
- 提交信息使用 `<type>(<scope>)：中文说明`。

### 1.1 常见术语先说明

| 术语 | 通俗解释 | 本项目里看哪里 |
| --- | --- | --- |
| B/S | Browser/Server，用户用浏览器访问系统，服务器负责页面、接口和数据库。 | `src/ECommerce.Web` |
| MVC | ASP.NET Core 的页面组织方式。Controller 接请求，View 显示页面，Model/ViewModel 传页面数据。 | `Controllers`、`Views` |
| Controller | 接收浏览器请求的入口。页面 Controller 返回页面，API Controller 返回 JSON。 | `src/ECommerce.Web/Controllers` |
| Razor View | `.cshtml` 页面文件，里面写 HTML 和少量 Razor 语法。 | `src/ECommerce.Web/Views` |
| JSON API | 给前端或页面 JS 调用的数据接口，统一返回 JSON。 | `src/ECommerce.Web/Controllers/Api` |
| DTO | Data Transfer Object，接口入参/出参对象。它只负责传数据，不写业务逻辑。 | `src/ECommerce.Application/DTOs` |
| Service 接口 | 模块对外承诺能做什么，例如创建订单、锁库存、模拟支付。 | `src/ECommerce.Application/Services` |
| Service 实现 | 真正执行业务流程的类，例如检查库存、写订单、记日志。 | `src/ECommerce.Infrastructure/Services` |
| Repository | 数据访问类，集中写某张表或某组表的 SQL。它只读写数据，不决定完整业务流程。 | `src/ECommerce.Infrastructure/Repositories` |
| Entity | 和数据库表比较接近的 C# 对象，例如 `Product`、`Sku`、`OrderMain`。 | `src/ECommerce.Domain/Entities`，需要时创建 |
| Enum | 枚举，给状态值起名字，避免代码里到处写 `0`、`1`、`2`。 | `src/ECommerce.Domain/Enums` |
| ApiResponse | 统一 JSON 外壳。成功、失败、错误码、数据都按同一种格式返回。 | `src/ECommerce.Shared/Contracts/ApiResponse.cs` |
| DI | 依赖注入。简单说就是在 `DependencyInjection.cs` 注册接口和实现，然后 Controller/Service 构造函数里直接要接口。 | `src/ECommerce.Infrastructure/DependencyInjection.cs` |
| Cookie | 浏览器保存登录状态的小票据。登录成功后服务器写 Cookie，后续请求靠它识别用户。 | `Program.cs` 登录配置 |
| Policy | 权限规则，例如只能普通用户访问、客服或管理员访问、仅管理员访问。 | `AuthConstants.Policies` |
| IUnitOfWork | 一次业务操作共用同一个数据库连接和事务的工具。 | `src/ECommerce.Shared/Abstractions/IUnitOfWork.cs` |
| 事务 | 要么全部成功，要么全部回滚。比如创建订单时订单、明细、库存日志必须一起成功。 | 本文第 10 节 |
| migration | 数据库脚本，不是 C# 代码。`init_database.sql` 负责建表、约束和索引，`database_objects.sql` 负责函数、过程、视图和触发器。 | `migration/init_database.sql`、`migration/database_objects.sql` |

## 2. 项目结构

先记住一句话：浏览器请求先进 `Web`，业务规则看 `Application` 的接口，状态和枚举看 `Domain`，Oracle 落库放 `Infrastructure`，所有人共用的响应、分页、错误码放 `Shared`。

注意：`ECommerce.Web`、`ECommerce.Application` 这些不是随便建的普通文件夹，而是已经在解决方案里的 `.csproj` 项目。写代码时先进入对应项目，再放到项目内部的子目录。

```text
src/
  ECommerce.Web/                 # 表现层项目：页面、Controller、静态资源
    Controllers/                 # 页面 Controller
    Controllers/Api/             # JSON API Controller
    Views/                       # Razor 页面
    ViewModels/                  # 页面专用显示模型
    Filters/                     # MVC 过滤器，例如统一异常或权限过滤
    wwwroot/                     # CSS、JS、图片等静态资源

  ECommerce.Application/         # 应用接口项目：DTO、Service 接口
    DTOs/                        # 请求和响应对象
    Services/                    # Service 接口，只定义方法名和参数
    Validators/                  # 参数校验类，需要时使用

  ECommerce.Domain/              # 领域项目：业务枚举、领域对象、状态值
    Entities/                    # 和数据库表接近的 C# 对象
    Enums/                       # 订单状态、支付状态等枚举

  ECommerce.Infrastructure/      # 基础设施项目：Oracle 连接、事务、Service 实现、Repository
    Data/                        # Oracle 连接、健康检查、UnitOfWork
    Services/                    # Service 实现，写业务流程
    Repositories/                # Repository，集中写 SQL 和表操作

  ECommerce.Shared/              # 公共项目：统一响应、分页、错误码、权限常量、公共异常
    Abstractions/                # 公共抽象，例如 IUnitOfWork
    Constants/                   # 公共常量
    Contracts/                   # 通用响应、分页、导出对象
    Errors/                      # 错误码
    Exceptions/                  # 公共异常

tests/
  ECommerce.Tests/               # 自动化测试项目
```

部分空目录里有 `.gitkeep`，它只是为了让 Git 记录空文件夹。以后目录里有真实代码文件时，可以保留也可以删除 `.gitkeep`。

每层具体怎么用：

| 项目 | 通俗解释 | 现在已有 | 后续新增文件放哪里 | 不要做什么 |
| --- | --- | --- | --- | --- |
| `ECommerce.Web` | 和浏览器打交道。负责接收 HTTP 请求，返回 Razor 页面或 JSON。 | `Controllers`、`Controllers/Api`、`Views`、`ViewModels`、`Filters`、`wwwroot`、`Program.cs` | 页面 Controller 放 `src/ECommerce.Web/Controllers`；JSON API 放 `src/ECommerce.Web/Controllers/Api`；页面放 `src/ECommerce.Web/Views/{Controller}/{Action}.cshtml`；页面 JS 放 `src/ECommerce.Web/wwwroot/js`。 | 不写 SQL，不直接打开 Oracle 连接，不把复杂业务流程写在 Controller。 |
| `ECommerce.Application` | 定义“别人怎么调用这个模块”。这里放接口和传输对象。 | `DTOs`、`Services`、`Validators` | 新增接口放 `src/ECommerce.Application/Services/IxxxService.cs`；请求/响应 DTO 放 `src/ECommerce.Application/DTOs/XxxDtos.cs`；校验类放 `src/ECommerce.Application/Validators`。 | 不引用 `Infrastructure`，不写 Oracle SQL，不返回 MVC 的 `IActionResult`。 |
| `ECommerce.Domain` | 放全项目认可的业务概念。比如订单状态、支付状态、库存变动类型。 | `Entities`、`Enums` | 新增业务枚举放 `src/ECommerce.Domain/Enums`；领域对象放 `src/ECommerce.Domain/Entities`。 | 不写数据库访问代码，不写页面代码。 |
| `ECommerce.Infrastructure` | 真正和外部资源打交道。Oracle、事务、数据查询、Service 实现都在这里。 | `Data`、`Services`、`Repositories`、`DependencyInjection.cs` | Service 实现放 `src/ECommerce.Infrastructure/Services/XxxService.cs`；Repository 放 `src/ECommerce.Infrastructure/Repositories/XxxRepository.cs`；Oracle 基础设施放 `src/ECommerce.Infrastructure/Data`。 | 不返回页面，不处理 Razor，不定义新的公共 DTO 格式。 |
| `ECommerce.Shared` | 所有人都会用的公共小工具和公共约定。 | `Contracts`、`Constants`、`Errors`、`Exceptions`、`Abstractions` | 只有确实跨模块共用时才放这里，例如统一响应、分页、错误码、权限常量。 | 不放具体业务流程，不放某个模块私有 DTO。 |
| `ECommerce.Tests` | 验证接口、状态、事务基础和关键业务流程。 | `ContractTests`、基础设施测试 | 新增测试按模块命名，例如 `OrderTests.cs`、`InventoryTests.cs`。 | 不依赖真实数据库密码，不提交本地私有配置。 |

`Repository` 是“数据访问类”，也可以理解为某张表或某组表的 SQL 操作集中放置处。比如 `ProductRepository` 负责查询/新增/修改 `PRODUCT`、`SKU` 等表，`OrderRepository` 负责写 `ORDER_MAIN`、`ORDER_ITEM`、`ORDER_LOG`。它只做数据读写，不决定完整业务流程；业务流程放在 `XxxService` 里。

一次请求的推荐流转：

```text
浏览器
  -> ECommerce.Web Controller
  -> ECommerce.Application 里的 Service 接口
  -> ECommerce.Infrastructure 里的 Service 实现
  -> ECommerce.Infrastructure 里的 Repository
  -> Oracle
```

返回时反过来：

```text
Oracle
  -> Repository 查询结果
  -> Service 组装 DTO
  -> Controller 包成 ApiResponse 或返回 Razor View
  -> 浏览器
```

新增一个功能时，按这个顺序找位置：

| 要做的事 | 放哪里 | 例子 |
| --- | --- | --- |
| 定义请求/响应格式 | `src/ECommerce.Application/DTOs` | `CreateOrderRequest`、`OrderDetailDto` |
| 定义模块能力 | `src/ECommerce.Application/Services` | `IOrderService.CreateAsync` |
| 实现业务流程 | `src/ECommerce.Infrastructure/Services` | `OrderService` 调库存、优惠券、订单 Repository |
| 写 SQL 或表操作 | `src/ECommerce.Infrastructure/Repositories` | `OrderRepository.InsertOrderAsync` |
| 暴露 JSON API | `src/ECommerce.Web/Controllers/Api` | `OrdersApiController` |
| 做 Razor 页面 | `src/ECommerce.Web/Views` | `Views/AdminOrders/Index.cshtml` |
| 做页面交互 JS | `src/ECommerce.Web/wwwroot/js` | `admin-orders.js` |
| 写测试 | `tests/ECommerce.Tests` | `OrderTests.cs` |

依赖方向只能这样走：

```text
Web -> Application
Web -> Infrastructure
Application -> Domain + Shared
Infrastructure -> Application + Domain + Shared
Shared -> 不依赖其他项目
```

意思是：`Application` 不能调用 `Infrastructure`，所以不能在 `Application` 里写 Oracle SQL；`Controller` 可以通过依赖注入拿到 `IOrderService` 这类接口，但不要直接 new Repository。

## 3. 接口与代码入口

这一节用于“我想改某件事，应该先打开哪个文件”。这里的“约定”就是团队提前说好的格式和边界：请求长什么样、响应长什么样、接口叫什么、错误码叫什么。

| 要找的东西 | 源码位置 |
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
| Service 实现 | `src/ECommerce.Infrastructure/Services/*.cs`，实现时创建 |
| Repository 数据访问 | `src/ECommerce.Infrastructure/Repositories/*.cs`，实现时创建 |
| API 路由骨架 | `src/ECommerce.Web/Controllers/Api/*.cs` |
| 页面 Controller | `src/ECommerce.Web/Controllers/*.cs` |
| Razor 页面 | `src/ECommerce.Web/Views/**/*.cshtml` |
| 页面 JS/CSS | `src/ECommerce.Web/wwwroot/js/*.js`, `src/ECommerce.Web/wwwroot/css/*.css` |
| Oracle 连接 | `src/ECommerce.Infrastructure/Data/*.cs` |
| DI 注册 | `src/ECommerce.Infrastructure/DependencyInjection.cs`, `src/ECommerce.Web/Program.cs` |
| 数据库脚本 | `migration/init_database.sql`、`migration/database_objects.sql` |

API Controller 路由已统一定义并调用对应 Service；Controller 不直接写 SQL。模拟支付持久化 `PAYMENT`，并通过签名回调或页面操作完成支付事务。

看接口时按这个顺序：

1. 先看 `src/ECommerce.Web/Controllers/Api`，确认路由。
2. 再看 `src/ECommerce.Application/DTOs`，确认请求和响应字段。
3. 再看 `src/ECommerce.Application/Services`，确认应该调用哪个 Service 方法。
4. 最后在 `src/ECommerce.Infrastructure` 里补 Service 实现和 Repository。

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
$env:Oracle__ConnectionString = "User Id=ECOMMERCE_DEV;Password=...;Data Source=数据库服务器IP:1521/服务名"
```

开发联调用 `ECOMMERCE_DEV`，最终演示用 `ECOMMERCE_DEMO`。后端代码不区分本地库或远程库，连接到哪里只由 `Data Source` 决定。

数据库连通性检查：

```text
GET /api/v1/system/db-check
```

该接口会返回 `connected`、`sessionUser`、`currentSchema` 和 `serviceName`，用于确认当前后端实际连到哪个 Oracle 用户和服务。

该接口继续允许匿名访问，便于负载均衡和部署探针验证就绪状态；返回内容不包含连接密码。

## 5. 分支职责

| 分支 | 主责 | 直接负责接口 |
| --- | --- | --- |
| `feat-member1-foundation-oracle-deploy` | 项目骨架、Oracle、部署 | `IUnitOfWork`、Oracle 连接、数据库健康检查、认证/授权、DI、README、部署样例 |
| `feat-member2-user-permission-address-log` | 用户、权限、地址、日志 | `IAuthService`、`IUserService`、`IAddressService`、`IOperationLogService` |
| `feat-member3-product-category-sku-inventory` | 商品、分类、SKU、库存 | `ICategoryService`、`IProductService`、`ISkuService`、`IInventoryService` |
| `feat-member4-cart-order-core` | 购物车、订单核心流程 | `ICartService`、`IOrderService` |
| `feat-member5-payment-coupon-logistics-review` | 支付、优惠券、物流、评价 | `ICouponService`、`IPaymentService`、`ILogisticsService`、`IReviewService` |
| `feat-member6-stats-export-ui-docs` | 统计、导出、UI、测试、文档、PPT | `IStatisticsService`、`IExportService`、后台首页、统一 Bootstrap 页面 |

### 5.1 数据表归属

数据表以 `migration/init_database.sql` 为准。主责人负责对应表的 Repository（数据访问类）、写入规则、状态流转和后台维护页面；其他人需要写这些表时，必须通过主责人的 Service 接口协作，不要跨模块直接改表。

| 人员 | 主责表 | 依赖表 | 边界说明 |
| --- | --- | --- | --- |
| 第 1 人 | 全部表的建表脚本、索引、约束、数据库对象与初始化顺序 | 全部业务表 | 负责 `migration/init_database.sql`、`migration/database_objects.sql`、Oracle 连接、`IUnitOfWork`、部署配置；不负责业务 CRUD 规则。 |
| 第 2 人 | `"USER"`、`"ROLE"`、`"PERMISSION"`、`USER_ROLE`、`ROLE_PERMISSION`、`ADDRESS`、`OPERATION_LOG` | 其他模块会引用 `"USER"` 和 `OPERATION_LOG` | 负责注册登录、角色权限、地址、操作日志；其他模块记录后台操作日志时调用 `IOperationLogService`。 |
| 第 3 人 | `"CATEGORY"`、`PRODUCT`、`PRODUCT_IMAGE`、`PRODUCT_SPEC`、`SKU`、`INVENTORY_LOG` | `"USER"`、`ORDER_MAIN` | 负责商品、分类、图片、规格、SKU、库存和库存日志；订单模块只能通过 `IInventoryService` 锁定、释放、扣减库存。 |
| 第 4 人 | `CART`、`ORDER_MAIN`、`ORDER_ITEM`、`ORDER_LOG` | `"USER"`、`ADDRESS`、`USER_COUPON`、`SKU`、`PRODUCT` | 负责购物车、订单主流程和订单日志；创建订单时依赖地址、SKU、库存锁定、优惠券校验。 |
| 第 5 人 | `COUPON_TEMPLATE`、`USER_COUPON`、`PAYMENT`、`LOGISTICS`、`LOGISTICS_TRACK`、`REVIEW` | `"USER"`、`ORDER_MAIN`、`ORDER_ITEM`、`PRODUCT`、`SKU` | 负责优惠券、模拟支付、物流轨迹、评价；支付成功后通过订单和库存接口完成状态流转。 |
| 第 6 人 | `ORDER_STAT_SNAPSHOT` | `ORDER_MAIN`、`ORDER_ITEM`、`PAYMENT`、`PRODUCT`、`SKU`、`INVENTORY_LOG`、`"USER"`、`REVIEW`、`LOGISTICS` | 负责统计、导出、后台首页和 UI 整合；统计和导出默认只读上游业务表，必要快照写入 `ORDER_STAT_SNAPSHOT`。 |

### 5.2 表级依赖关系

外键和业务依赖按下面关系处理：

| 分组 | 表级依赖 |
| --- | --- |
| 用户权限 | `USER_ROLE.user_id -> "USER".id`；`USER_ROLE.role_id -> "ROLE".id`；`ROLE_PERMISSION.role_id -> "ROLE".id`；`ROLE_PERMISSION.permission_id -> "PERMISSION".id`。 |
| 地址与日志 | `ADDRESS.user_id -> "USER".id`；`OPERATION_LOG.operator_id -> "USER".id`。 |
| 商品与库存 | `PRODUCT.category_id -> "CATEGORY".id`；`PRODUCT_IMAGE.product_id -> PRODUCT.id`；`PRODUCT_SPEC.product_id -> PRODUCT.id`；`SKU.product_id -> PRODUCT.id`；`INVENTORY_LOG.sku_id -> SKU.id`；`INVENTORY_LOG.operator_id -> "USER".id`。 |
| 购物车 | `CART.user_id -> "USER".id`；`CART.sku_id -> SKU.id`。 |
| 订单 | `ORDER_MAIN.user_id -> "USER".id`；`ORDER_MAIN.address_id -> ADDRESS.id`；`ORDER_MAIN.user_coupon_id -> USER_COUPON.id`；`ORDER_ITEM.order_id -> ORDER_MAIN.id`；`ORDER_ITEM.sku_id -> SKU.id`；`ORDER_LOG.order_id -> ORDER_MAIN.id`；`ORDER_LOG.operator_id -> "USER".id`；`ORDER_LOG.operator_name` 是操作人用户名快照，不做外键。 |
| 优惠券 | `USER_COUPON.user_id -> "USER".id`；`USER_COUPON.coupon_template_id -> COUPON_TEMPLATE.id`；`USER_COUPON.order_id` 业务上关联订单。 |
| 支付物流评价 | `PAYMENT.order_id -> ORDER_MAIN.id`；`LOGISTICS.order_id -> ORDER_MAIN.id`；`LOGISTICS_TRACK.logistics_id -> LOGISTICS.id`；`REVIEW.order_id -> ORDER_MAIN.id`；`REVIEW.product_id -> PRODUCT.id`；`REVIEW.user_id -> "USER".id`。 |
| 统计 | `ORDER_STAT_SNAPSHOT` 没有外键，数据来源是订单、支付、商品、库存、用户等表的汇总快照。 |

跨模块协作规则：

- 只能直接写自己主责表；其他表的写入必须调用对应 Service。
- 可以只读依赖表做展示和校验，但不要绕过主责模块修改状态。
- 订单状态日志需要区分订单所属用户和实际操作者；`IOrderService.CancelAsync` 中 `userId` 表示订单所属用户，`operatorId`、`operatorName` 和 `ipAddress` 表示本次操作人上下文。
- `operatorName` 和 `ipAddress` 必须由后端 Controller 从登录态、Claims、用户上下文和 `HttpContext` 取得，不要相信请求体里手写的用户名或 IP。
- Service 层不要直接读取 `HttpContext`。需要记录 IP 时，由 Controller 调用 `ApiControllerBase.GetClientIpAddress()` 后传给 Service。
- 购物车和订单根据 `SkuId` 做前置校验时，调用 `ISkuService.GetByIdAsync` 查询 `SkuDto`；可用库存统一按 `Stock - LockedStock` 计算。
- 查询 SKU 只用于校验，创建订单真正锁定库存仍必须调用 `IInventoryService.LockForOrderAsync`，不能直接修改 `SKU`。
- 订单创建必须通过 `IInventoryService.LockForOrderAsync` 锁库存，通过 `ICouponService.ValidateAsync` 校验优惠券。
- 订单取消必须通过 `IInventoryService.ReleaseForCancelledOrderAsync` 释放锁定库存。
- 订单创建必须在同一事务中写入订单、锁定库存并通过 `ICouponService.UseForOrderAsync` 原子核销优惠券；支付成功再通过 `IOrderService.MarkPaidAsync` 和 `IInventoryService.DeductForPaidOrderAsync` 串起状态流转。
- 后台关键写操作统一通过 `IOperationLogService` 写 `OPERATION_LOG`。
- 统计和导出默认只读上游表；如果要增加统计口径，先和对应业务表主责人确认状态含义。
- 修改表结构、外键、状态字段、索引时，必须同步更新 `migration/init_database.sql`、`migration/database_objects.sql`（若对象依赖这些结构）、本文件和相关 DTO/Service 接口。

## 6. 路由与权限

认证：

- Cookie 名称：`ECommerce.Auth`。
- 角色：`USER`、`SERVICE`、`ADMIN`。
- Policy：`CustomerOnly`、`ServiceOrAdmin`、`AdminOnly`。

这几个词的意思：

| 名称 | 意思 |
| --- | --- |
| `USER` | 普通用户，可以浏览商品、购物车、下单、评价。 |
| `SERVICE` | 客服，可以处理后台订单、发货等客服相关操作。 |
| `ADMIN` | 管理员，可以维护用户、商品、统计等后台功能。 |
| `CustomerOnly` | 需要登录用户。当前包含 `USER`、`SERVICE`、`ADMIN`，方便后台角色也能访问自己的用户功能。 |
| `ServiceOrAdmin` | 客服或管理员可以访问。 |
| `AdminOnly` | 只有管理员可以访问。 |

Controller 上加权限时用 `[Authorize(Policy = AuthConstants.Policies.AdminOnly)]` 这类写法；公开页面和公开接口用 `[AllowAnonymous]`。

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

JSON API 权限要和页面入口匹配：

- 系统健康检查、注册、登录、商品浏览和商品评价列表是公开接口，用 `[AllowAnonymous]`。
- `POST /api/v1/auth/logout` 和 `GET /api/v1/auth/me` 必须是 `CustomerOnly`，只服务已登录用户。
- 用户侧购物车、订单、地址、优惠券、支付、物流查询和评价创建接口使用 `CustomerOnly`。
- 后台订单、发货、物流轨迹和后台首页摘要使用 `ServiceOrAdmin`。
- 后台用户、商品、优惠券模板、评价审核、统计和导出使用 `AdminOnly`。

路由写法说明：

- `GET /api/v1/products`：查询列表。
- `POST /api/v1/products`：新增数据。
- `PUT /api/v1/products/{productId}`：修改指定 ID 的数据，`{productId}` 是路径参数。
- `DELETE /api/v1/cart/items/{cartItemId}`：删除指定 ID 的数据。
- `GET|POST /api/v1/addresses`：同一个地址支持 GET 和 POST 两种方法，不是一个真的 HTTP 方法名。

## 7. 核心 API 分组

系统：

- `GET /health`
- `GET /api/v1/system/version`
- `GET /api/v1/system/db-check`：返回 Oracle 是否配置、是否连通、服务器时间和耗时。

用户、权限、地址、日志：

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`：`CustomerOnly`
- `GET /api/v1/auth/me`：`CustomerOnly`
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

商品相关 DTO 中 `CategoryId` 使用 `int`，和数据库 `CATEGORY.id NUMBER(10)`、`ICategoryService` 的 `categoryId` 类型保持一致；`ProductId`、`SkuId`、图片 ID 等 `NUMBER(19)` 主键使用 `long`。

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
- `POST /api/v1/coupons/{userCouponId}/validate`：请求体使用 `CouponValidationRequest`，字段为 `orderAmount`
- `GET|POST /api/v1/admin/coupon-templates`
- `PUT /api/v1/admin/coupon-templates/{templateId}`
- `PUT /api/v1/admin/coupon-templates/{templateId}/status`
- `POST /api/v1/payments/simulate`
- `GET /api/v1/payments/{orderId}`
- `POST /api/v1/payments/callback/simulated`
- `POST /api/v1/admin/orders/{orderId}/shipments`
- `GET /api/v1/admin/orders/{orderId}/logistics`
- `GET /api/v1/logistics/{orderId}`
- `POST /api/v1/admin/logistics/{logisticsId}/tracks`
- `POST /api/v1/reviews`
- `GET /api/v1/products/{productId}/reviews`
- `GET /api/v1/admin/reviews`
- `PUT /api/v1/admin/reviews/{reviewId}/status`

统计、导出：

- `GET /api/v1/admin/dashboard/summary`：`ServiceOrAdmin`
- `GET /api/v1/admin/statistics/orders`：`AdminOnly`
- `GET /api/v1/admin/statistics/top-products`：`AdminOnly`
- `GET /api/v1/admin/exports/orders`：`AdminOnly`
- `GET /api/v1/admin/exports/inventory`：`AdminOnly`

## 8. 状态值

数据库里很多状态是数字，页面和代码里不要让用户直接看数字。写代码时用枚举或常量表达含义，例如订单状态 `0` 表示“待支付”，不是随便的数字。

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

这里的“发起模块”表示这次业务动作由谁负责触发；“必须依赖”表示不能自己绕过别人模块直接改表，应该调用对方已经定义好的 Service。

| 场景 | 发起模块 | 必须依赖 |
| --- | --- | --- |
| 登录 | 用户模块 | 登录 Cookie 和权限 Policy |
| 加入购物车 | 购物车模块 | `ISkuService.GetByIdAsync` 查询 SKU 基本信息，校验 SKU 在售和 `Stock - LockedStock` 可用库存；需要商品上架状态时再通过商品查询接口校验 |
| 创建订单 | 订单模块 | 地址校验、`ISkuService.GetByIdAsync` 前置校验 SKU、`IInventoryService.LockForOrderAsync` 锁库存、优惠券校验 |
| 取消订单 | 订单模块 | 调用 `IOrderService.CancelAsync` 时传入订单所属用户和实际操作者，释放锁定库存、写订单日志 |
| 支付成功 | 支付模块 | 订单支付上下文、扣减库存、核销优惠券、标记订单已支付 |
| 发货 | 物流模块 | 创建物流、标记订单已发货、写订单日志 |
| 确认收货 | 订单模块 | 可选校验物流，标记订单完成 |
| 评价 | 评价模块 | 校验订单已完成且包含该商品 |
| 统计导出 | 统计模块 | 订单、商品、库存、支付状态口径一致 |

关键接口名：

这些方法名是跨模块协作时优先调用的 Service 方法，不是页面地址。

- `ISkuService.GetByIdAsync`
- `IInventoryService.LockForOrderAsync`
- `IInventoryService.ReleaseForCancelledOrderAsync`
- `IInventoryService.DeductForPaidOrderAsync`
- `IOrderService.GetPaymentContextAsync`
- `IOrderService.GetSkuQuantitiesAsync`
- `IOrderService.MarkPaidAsync`
- `IOrderService.CancelAsync(userId, orderId, operatorId, operatorName, ipAddress, reason)`
- `IOrderService.MarkShippedAsync(orderId, logisticsId, operatorId, operatorName, ipAddress)`
- `ICouponService.ValidateAsync`
- `ICouponService.UseForOrderAsync`

## 10. 事务边界

事务的意思是：一组数据库写操作必须一起成功；中途任何一步失败，前面已经写进去的数据都要撤回。

例子：创建订单时，如果订单主表写成功了，但库存锁定失败，不能留下一个“没有锁库存的订单”。这种情况必须回滚，让数据库回到创建订单之前的状态。

必须支持事务的业务：

| 业务 | 同一事务内完成 |
| --- | --- |
| 注册 | 新增用户、分配默认角色 |
| 创建订单 | 订单主表、明细、订单日志、库存锁定、库存日志、购物车清理 |
| 取消订单 | 订单状态、订单日志、锁定库存释放、库存日志 |
| 支付成功 | 支付状态、订单状态、库存扣减、优惠券核销、订单日志、库存日志 |
| 发货 | 物流信息、订单状态、订单日志 |
| 默认地址 | 取消旧默认地址、设置新默认地址 |

`IUnitOfWork` 在 `src/ECommerce.Shared/Abstractions/IUnitOfWork.cs`。业务 Service 里需要多表写入时，应该通过 `IUnitOfWork` 开启事务，并让相关 Repository 使用同一个连接和事务。

当前事务基础由 `src/ECommerce.Infrastructure/Data/UnitOfWork.cs` 实现：

- `GetOpenConnectionAsync`：按请求作用域复用 Oracle 连接。
- `BeginTransactionAsync`：在当前连接上开启事务。
- `CurrentConnection`、`CurrentTransaction`：供后续 Repository 执行 SQL 时复用。
- `CommitAsync`：提交并释放当前事务。
- `RollbackAsync`：回滚并释放当前事务。
- `DisposeAsync`：请求结束时释放事务和连接。

Repository 需要执行多表写入时，应复用同一个 `IUnitOfWork`，不要自己临时 new Oracle 连接。

## 11. 数据库命名

- 表名：大写下划线，例如 `ORDER_MAIN`；保留字表使用双引号，例如 `"USER"`。
- 字段名：小写下划线，例如 `created_at`。
- 主键：统一为 `id`。
- 外键：`{entity}_id`，例如 `user_id`、`order_id`。
- C# Entity/DTO 属性使用 PascalCase，例如 `OrderNo`、`LockedStock`。

## 12. UI、Vue 与页面

- Bootstrap 是页面样式库，按钮、表格、表单、栅格布局优先用它现成的 class。
- Razor 是 `.cshtml` 页面模板，负责首屏 HTML 结构和服务器端布局。
- Vue 只负责局部交互，例如刷新状态、列表筛选、按钮点击后更新页面。
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

脱敏的意思是隐藏敏感内容，例如密码不记录，手机号只保留前后几位，身份证、邮箱、详细地址不要完整写进日志。

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

## 15. 部署约定

部署相关样例文件放在 `deployment/`：

```text
deployment/env.example
deployment/publish.ps1
deployment/linux/ecommerce.service.example
deployment/linux/nginx-ecommerce.conf.example
```

生产环境通过环境变量配置，不提交真实密码：

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
Oracle__ConnectionString=User Id=...;Password=...;Data Source=...:1521/FREEPDB1
```

云服务器部署验收至少包括：

- 发布包能由 `dotnet ECommerce.Web.dll` 启动。
- Nginx 或服务器公网地址能访问 `/`。
- `/health` 返回 `success: true`。
- 配置真实 Oracle 后，`/api/v1/system/db-check` 返回 `connected: true`。
- 截图放入课程文档或 PPT，不提交真实密码截图。
