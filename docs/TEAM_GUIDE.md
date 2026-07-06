# 组员开工指南

这份文档用于组员开工、切换分支、运行项目、提交代码和发起 PR。接口、代码、API、事务等细节以 `docs/DEVELOPMENT_SPEC.md` 为准。

## 1. 项目是什么

- 模式：B/S。
- 后端：ASP.NET Core MVC，目标框架 `net8.0`。
- 数据库：Oracle，建表脚本在 `migration/init_database.sql`。
- 前端页面：Razor Views + Bootstrap。
- JSON API：统一使用 `/api/v1/...` 前缀。

项目入口：

```text
ECommerce.sln
src/ECommerce.Web
src/ECommerce.Application
src/ECommerce.Domain
src/ECommerce.Infrastructure
src/ECommerce.Shared
tests/ECommerce.Tests
```

## 2. 每个人在哪个分支做

| 人员 | 分支 | 主责 |
| --- | --- | --- |
| 第 1 人 | `feat-member1-foundation-oracle-deploy` | 项目骨架、Oracle、部署 |
| 第 2 人 | `feat-member2-user-permission-address-log` | 用户、权限、地址、日志 |
| 第 3 人 | `feat-member3-product-category-sku-inventory` | 商品、分类、SKU、库存 |
| 第 4 人 | `feat-member4-cart-order-core` | 购物车、订单核心流程 |
| 第 5 人 | `feat-member5-payment-coupon-logistics-review` | 支付、优惠券、物流、评价 |
| 第 6 人 | `feat-member6-stats-export-ui-docs` | 统计、导出、UI、测试、文档、PPT |

切分支：

```powershell
git fetch origin
git switch --track origin/feat-member3-product-category-sku-inventory
```

如果本地已经有分支：

```powershell
git switch feat-member3-product-category-sku-inventory
git pull --ff-only
```

## 3. 第一次运行

需要安装 .NET 8 SDK 或更高版本。

```powershell
dotnet restore ECommerce.sln
dotnet build ECommerce.sln
dotnet test ECommerce.sln
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

看到下面输出时，服务正在运行：

```text
Now listening on: http://localhost:5052
Application started. Press Ctrl+C to shut down.
```

不要关闭终端，然后用浏览器打开：

```text
http://localhost:5052/
http://localhost:5052/health
http://localhost:5052/account/login
http://localhost:5052/admin/dashboard
http://localhost:5052/docs/team-guide
http://localhost:5052/docs/development-spec
```

如果终端回到 PowerShell 提示符，说明服务已经停止，需要重新运行 `dotnet run`。

当前能看到的是项目状态页、健康检查、登录/注册占位页、Vue Dashboard 示例页和两份文档。业务页面和业务接口还没有实现，大多数 `/api/v1/...` 接口会返回 `501 NOT_IMPLEMENTED`。

## 4. 当前阶段是什么状态

现在项目处在“可运行骨架 + 接口契约已定”的阶段，不是完整业务系统。

已完成：

| 内容 | 说明 |
| --- | --- |
| 解决方案 | `ECommerce.sln` 已包含 Web、Application、Domain、Infrastructure、Shared、Tests |
| 目录结构 | 五层目录已经建好，各层项目引用已经配置 |
| 公共契约 | 统一响应、分页、错误码、权限常量、状态枚举已定义 |
| 业务接口 | DTO 和 Service 接口已放在 `src/ECommerce.Application` |
| API 入口 | `/api/v1/...` Controller 路由骨架已占位 |
| 页面入口 | 首页、登录页、注册页、后台布局、Vue Dashboard 示例页已占位 |
| 数据库入口 | Oracle 连接配置和健康检查服务已占位 |
| 测试入口 | `tests/ECommerce.Tests` 已能运行 |

未完成：

| 内容 | 当前表现 | 负责分支 |
| --- | --- | --- |
| 登录注册 | 页面能打开，提交后还没有真实认证 | `feat-member2-user-permission-address-log` |
| 商品分类/SKU/库存 | API 路由已占位，未连数据库 | `feat-member3-product-category-sku-inventory` |
| 购物车/订单 | API 路由已占位，未实现业务事务 | `feat-member4-cart-order-core` |
| 支付/优惠券/物流/评价 | API 路由已占位，未实现状态流转 | `feat-member5-payment-coupon-logistics-review` |
| 统计/导出/后台首页 | API 路由已占位，Vue Dashboard 示例页已提供，未实现统计和 Excel | `feat-member6-stats-export-ui-docs` |
| 部署 | 还需要服务器配置、环境变量、部署截图 | `feat-member1-foundation-oracle-deploy` |

技术负责人验收骨架时，看这几项即可：

1. `dotnet build ECommerce.sln` 成功。
2. `dotnet test ECommerce.sln` 成功。
3. `dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj` 后终端保持运行。
4. 浏览器能打开 `http://localhost:5052/`，看到“电商购物平台 - 项目状态”。
5. 浏览器能打开 `http://localhost:5052/health`，返回 `success: true`。
6. 浏览器能打开 `http://localhost:5052/account/login`。
7. 访问业务 API 如果返回 `501 NOT_IMPLEMENTED`，这在当前阶段是正常的，表示“接口已占位，等待对应成员实现”。

## 5. Oracle 怎么配

默认占位配置在：

```text
src/ECommerce.Web/appsettings.json
```

不要提交真实数据库密码。推荐用环境变量：

```powershell
$env:Oracle__ConnectionString = "User Id=你的账号;Password=你的密码;Data Source=localhost:1521/XEPDB1"
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

数据库初始化脚本：

```text
migration/init_database.sql
```

## 6. 开发前先看规范

开始写代码前，先确认自己的任务是否涉及公共接口、DTO、路由、状态码或事务边界。涉及这些内容时，先查看：

```text
docs/DEVELOPMENT_SPEC.md
```

开发时优先看这些源码：

| 内容 | 位置 |
| --- | --- |
| DTO | `src/ECommerce.Application/DTOs` |
| Service 接口 | `src/ECommerce.Application/Services` |
| API Controller 路由 | `src/ECommerce.Web/Controllers/Api` |
| 权限常量、响应、分页、错误码 | `src/ECommerce.Shared` |
| 状态枚举 | `src/ECommerce.Domain/Enums` |
| Oracle 连接与基础设施 | `src/ECommerce.Infrastructure` |

## 7. 开发规矩

- Controller 只调用 Application Service，不直接访问数据库。
- 不直接改其他成员模块的 Repository 或表操作。
- 跨模块调用只依赖已有 Service 接口和 DTO。
- 要改公共接口、DTO、状态枚举、错误码，先沟通，再同时改源码和 `docs/DEVELOPMENT_SPEC.md`。
- 后台写操作要记录操作日志。
- 订单、库存、优惠券、支付相关逻辑必须注意事务，不能出现半成功状态。

## 8. 提交规范

提交信息格式：

```text
<type>(<scope>)：中文说明
```

例子：

```text
feat(product)：新增商品分类维护页面
fix(order)：修复取消订单未释放锁定库存
docs(workflow)：整理组员开工文档
test(cart)：新增购物车数量校验测试
```

常用 `type`：

```text
feat fix docs style refactor test build chore perf
```

常用 `scope`：

```text
foundation oracle auth user permission address log category product sku inventory cart order payment coupon logistics review statistics export ui docs test
```

提交前：

```powershell
dotnet build ECommerce.sln
dotnet test ECommerce.sln
git status
```

提交并推送：

```powershell
git add 相关文件
git commit -m "feat(product)：新增商品分类维护页面"
git push origin 当前分支名
```

## 9. 合并流程

`main` 分支受保护，不能直接推送。功能完成后：

1. 推送自己的 `feat-member...` 分支。
2. 在 GitHub 创建 Pull Request。
3. PR 描述写清楚完成内容、测试结果、截图位置。
4. 由组长或负责集成的人检查后合并。

合并顺序建议：

1. 第 1 人项目骨架、数据库连接、部署基础。
2. 第 2 人用户、权限、地址、日志。
3. 第 3 人商品、分类、SKU、库存。
4. 第 4 人购物车、订单核心流程。
5. 第 5 人支付、优惠券、物流、评价。
6. 第 6 人统计、导出、UI、测试、文档、PPT。

## 10. 不要提交

- 真实数据库密码。
- `bin/`、`obj/` 构建产物。
- Office 临时文件，例如 `~$xxx.docx`。
- 与自己任务无关的大量格式化改动。

发现公共文件冲突时，先沟通再改，不要直接覆盖别人工作。
