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

启动后检查：

```text
/health
/account/login
```

`/health` 应返回统一 JSON，登录页应能打开。

## 4. Oracle 怎么配

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

## 5. 开发前先看规范

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

## 6. 开发规矩

- Controller 只调用 Application Service，不直接访问数据库。
- 不直接改其他成员模块的 Repository 或表操作。
- 跨模块调用只依赖已有 Service 接口和 DTO。
- 要改公共接口、DTO、状态枚举、错误码，先沟通，再同时改源码和 `docs/DEVELOPMENT_SPEC.md`。
- 后台写操作要记录操作日志。
- 订单、库存、优惠券、支付相关逻辑必须注意事务，不能出现半成功状态。

## 7. 提交规范

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

## 8. 合并流程

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

## 9. 不要提交

- 真实数据库密码。
- `bin/`、`obj/` 构建产物。
- Office 临时文件，例如 `~$xxx.docx`。
- 与自己任务无关的大量格式化改动。

发现公共文件冲突时，先沟通再改，不要直接覆盖别人工作。
