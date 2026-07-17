# 组员开工指南

这份文档用于组员开工、切换分支、运行项目、提交代码和发起 PR。接口、代码、API、事务等细节以 `docs/DEVELOPMENT_SPEC.md` 为准。

## 1. 项目是什么

- 模式：B/S。
- 后端：ASP.NET Core MVC，目标框架 `net8.0`。
- 数据库：Oracle；建表脚本在 `migration/init_database.sql`，函数、过程、视图和触发器在 `migration/database_objects.sql`。
- 前端页面：Razor Views + Bootstrap。
- JSON API：统一使用 `/api/v1/...` 前缀。

几个常见词先说明：

| 词 | 说明 |
| --- | --- |
| Controller | 接收浏览器请求的入口，页面 Controller 返回页面，API Controller 返回 JSON。 |
| DTO | 接口请求/响应对象，只放字段，不写业务逻辑。 |
| Service | 业务能力，例如登录、创建订单、锁库存。Controller 应该调用 Service。 |
| Repository | 数据访问类，集中写 SQL 和表操作。不要在 Controller 里写 SQL。 |
| ApiResponse | JSON 接口统一返回格式，包含 `success`、`code`、`message`、`data`、`traceId`。 |
| 事务 | 多个数据库写操作要么全部成功，要么全部撤回，避免半成功。 |

项目入口。注意：`src/ECommerce.Web` 这些是已经建好的 C# 项目，不是让大家在 `src` 下面随便新建文件夹。

```text
ECommerce.sln
src/ECommerce.Web
src/ECommerce.Application
src/ECommerce.Domain
src/ECommerce.Infrastructure
src/ECommerce.Shared
tests/ECommerce.Tests
```

常用子目录也已经预建好：

```text
src/ECommerce.Web/ViewModels
src/ECommerce.Web/Filters
src/ECommerce.Application/Validators
src/ECommerce.Domain/Entities
src/ECommerce.Infrastructure/Services
src/ECommerce.Infrastructure/Repositories
```

空目录里的 `.gitkeep` 只是为了让 Git 保留这个目录；以后放了真实代码文件，可以保留也可以删除。

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

每次开始编辑代码或文档前，都先拉取一次自己的分支：

```powershell
git switch 我们的成员分支名
git pull --ff-only
```

如果 `git pull --ff-only` 提示不能快进，先停止编辑，联系组长处理分支差异，不要直接强推或覆盖别人提交。

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

当前可演示真实登录、商品浏览、购物车、下单、持久化模拟支付、优惠券、物流、评价，以及后台商品、订单、库存、优惠券、评价、统计和导出流程。

## 4. 当前阶段是什么状态

现在项目处在“业务闭环已接通、等待环境联调与发布验收”的阶段。

已完成：

| 内容 | 说明 |
| --- | --- |
| 解决方案 | `ECommerce.sln` 已包含 Web、Application、Domain、Infrastructure、Shared、Tests |
| 目录结构 | 五层目录已经建好，各层项目引用已经配置 |
| 公共约定 | 统一响应、分页、错误码、权限常量、状态枚举已定义 |
| 业务接口 | 核心 DTO、Service、Repository 和事务规则已接通 |
| API 入口 | `/api/v1/...` Controller 已调用真实业务服务 |
| 页面入口 | 前后台核心业务页面和管理入口已提供 |
| 数据库入口 | Oracle 连接、数据库健康检查、UnitOfWork 事务基础已提供 |
| 部署入口 | `deployment/` 已提供发布脚本、systemd 和 Nginx 样例 |
| 测试入口 | `tests/ECommerce.Tests` 已能运行 |

模块状态：

| 内容 | 当前表现 | 负责分支 |
| --- | --- | --- |
| 登录注册 | 已接入 member2 真实 AuthService；演示账号使用 PBKDF2 seed 密码哈希 | `feat-member2-user-permission-address-log` |
| 商品分类/SKU/库存 | 已接入 member3 数据库实现 | `feat-member3-product-category-sku-inventory` |
| 购物车/订单 | 已接入 member4 核心流程 | `feat-member4-cart-order-core` |
| 支付/优惠券/物流/评价 | 持久化模拟支付、原子核销、物流轨迹和评价审核均已接通 | `feat-member1-foundation-oracle-deploy` |
| 统计/导出/后台首页 | 已接入 member6 统计 Dashboard 和 Excel 导出基础 | `feat-member6-stats-export-ui-docs` |
| 部署 | 配置样例已提供，还需要真实服务器环境变量、访问验证和部署截图 | `feat-member1-foundation-oracle-deploy` |

技术负责人验收骨架时，看这几项即可：

1. `dotnet build ECommerce.sln` 成功。
2. `dotnet test ECommerce.sln` 成功。
3. `dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj` 后终端保持运行。
4. 浏览器能打开 `http://localhost:5052/`，看到电商购物平台入口页。
5. 浏览器能打开 `http://localhost:5052/health`，返回 `success: true`。
6. 浏览器能打开 `http://localhost:5052/account/login`。

演示登录账号：

```text
密码统一为 demo123

demo_admin    ADMIN
demo_service  SERVICE
demo_user     USER
demo_buyer    USER
```

注意：这些账号走真实 AuthService 登录，密码哈希来自 `migration/seed_demo_data.sql`。
7. 模拟支付 API 会创建 `PAYMENT` 记录；匿名模拟回调必须使用 `Payment__SimulatedCallbackSecret` 生成 HMAC-SHA256 签名。

## 5. Oracle 怎么配

默认占位配置在：

```text
src/ECommerce.Web/appsettings.json
```

不要提交真实数据库密码，也不要把真实密码写进 `appsettings.json`。数据库用户按用途区分：

| 用户 | 用途 |
| --- | --- |
| `ECOMMERCE_DEV` | 开发联调。组员在自己电脑上运行后端时连接这个用户。 |
| `ECOMMERCE_DEMO` | 最终演示。部署演示环境时连接这个用户，避免演示数据被开发过程污染。 |

后端代码不需要因为“本地数据库”或“远程数据库”而修改；区别只在连接字符串的 `Data Source`。开发时 `Data Source` 写服务器 IP 或域名，服务器部署时可以写 `127.0.0.1`、内网地址或服务器域名。

推荐用环境变量：

```powershell
$env:Oracle__ConnectionString = "User Id=ECOMMERCE_DEV;Password=我们的开发库密码;Data Source=数据库服务器IP:1521/服务名"
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

演示环境示例：

```powershell
$env:Oracle__ConnectionString = "User Id=ECOMMERCE_DEMO;Password=我们的演示库密码;Data Source=127.0.0.1:1521/服务名"
```

检查 Oracle 是否连通：

```powershell
Invoke-RestMethod http://localhost:5052/api/v1/system/db-check
```

如果没有配置真实连接串，接口会返回 `configured: false`，这是正常提示，不是程序崩溃。配置正确后，期望看到 `connected: true`。同时检查 `sessionUser` 和 `currentSchema`：开发时应该是 `ECOMMERCE_DEV`，演示时应该是 `ECOMMERCE_DEMO`。

数据库初始化脚本：

```text
migration/init_database.sql
migration/database_objects.sql
```

第 1 人做 Oracle 时按这个顺序验收：

1. 本地 Oracle 建库或确认服务器 Oracle 可连。
2. 执行 `migration/init_database.sql`。
3. 执行 `migration/database_objects.sql`。
4. 设置 `Oracle__ConnectionString` 环境变量。
5. 启动 Web 项目。
6. 访问 `/api/v1/system/db-check`，截图保留 `connected: true`。

## 5.1 部署怎么准备

部署样例文件在：

```text
deployment/env.example
deployment/publish.ps1
deployment/linux/ecommerce.service.example
deployment/linux/nginx-ecommerce.conf.example
```

发布包：

```powershell
powershell -ExecutionPolicy Bypass -File deployment/publish.ps1
```

服务器验收截图建议至少保留：

1. `dotnet --info` 或 ASP.NET Core Runtime 安装截图。
2. `systemctl status ecommerce` 运行截图。
3. 浏览器访问公网地址 `/` 的截图。
4. 浏览器或命令访问 `/health` 的截图。
5. 配好数据库后 `/api/v1/system/db-check` 返回 `connected: true` 的截图。

## 6. 开发前先看规范

开始写代码前，先确认自己的任务是否涉及公共接口、DTO、路由、状态码或事务边界。涉及这些内容时，先查看：

```text
docs/DEVELOPMENT_SPEC.md
```

开发时优先看这些源码：

| 内容 | 位置 |
| --- | --- |
| 请求/响应字段 DTO | `src/ECommerce.Application/DTOs` |
| 业务方法名 Service 接口 | `src/ECommerce.Application/Services` |
| Service 实现 | `src/ECommerce.Infrastructure/Services`，实现时创建 |
| SQL 和表操作 Repository | `src/ECommerce.Infrastructure/Repositories`，实现时创建 |
| API Controller 路由 | `src/ECommerce.Web/Controllers/Api` |
| 权限常量、响应、分页、错误码 | `src/ECommerce.Shared` |
| 状态枚举 | `src/ECommerce.Domain/Enums` |
| Oracle 连接与基础设施 | `src/ECommerce.Infrastructure` |

## 7. 开发规矩

- Controller 只接请求和返回结果，复杂业务交给 Service，不直接访问数据库。
- Repository 只做 SQL 和表操作，不写完整业务流程。
- 不直接改其他成员模块负责的表；需要别人模块数据时，优先调用已有 Service 接口和 DTO。
- 要改公共接口、DTO、状态枚举、错误码，先沟通，再同时改源码和 `docs/DEVELOPMENT_SPEC.md`。
- 实现 API 时同时核对权限、请求体/查询参数和 ID 类型；例如后台首页摘要是 `ServiceOrAdmin`，统计导出是 `AdminOnly`，商品接口里的 `CategoryId` 用 `int`。
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
