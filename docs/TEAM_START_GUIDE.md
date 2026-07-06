# 组员开工指南

这份指南给所有组员统一开工用。先把项目跑起来，再在自己的分支上实现负责模块。

## 1. 先看三份文档

开工前按顺序阅读：

1. `README.md`：项目功能、启动命令、文档入口。
2. `docs/ARCHITECTURE_AND_INTERFACES.md`：B/S 架构、五层目录、页面路由、API、DTO、Service 接口、跨分支协作接口。
3. `docs/DEVELOPMENT_WORKFLOW.md`：分工、分支名、命名规范、提交信息规范、合并检查清单。

## 2. 拉取项目

```powershell
git clone https://github.com/Stonerxx/E-commerce-management.git
cd E-commerce-management
git fetch origin
```

如果已经 clone 过：

```powershell
git fetch origin
git status
```

## 3. 切到自己的分支

每个人只在自己的分支开发：

| 人员 | 分支 | 主责 |
| --- | --- | --- |
| 第 1 人 | `feat-member1-foundation-oracle-deploy` | 项目骨架、Oracle、部署 |
| 第 2 人 | `feat-member2-user-permission-address-log` | 用户、权限、地址、日志 |
| 第 3 人 | `feat-member3-product-category-sku-inventory` | 商品、分类、SKU、库存 |
| 第 4 人 | `feat-member4-cart-order-core` | 购物车、订单核心流程 |
| 第 5 人 | `feat-member5-payment-coupon-logistics-review` | 支付、优惠券、物流、评价 |
| 第 6 人 | `feat-member6-stats-export-ui-docs` | 统计、导出、UI、测试、文档、PPT |

示例：

```powershell
git switch feat-member3-product-category-sku-inventory
git pull --ff-only
```

如果本地还没有该分支：

```powershell
git switch --track origin/feat-member3-product-category-sku-inventory
```

## 4. 本地运行项目

需要安装 .NET 8 SDK 或更高版本。当前项目目标框架是 `net8.0`。

```powershell
dotnet restore ECommerce.sln
dotnet build ECommerce.sln
dotnet test ECommerce.sln
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

启动后访问：

```text
/health
/account/login
```

`/health` 应返回统一 JSON 响应，登录页应能打开。

## 5. 配置 Oracle 连接

默认占位配置在：

```text
src/ECommerce.Web/appsettings.json
```

不要提交真实数据库密码。推荐本地用环境变量覆盖：

```powershell
$env:Oracle__ConnectionString = "User Id=你的账号;Password=你的密码;Data Source=localhost:1521/XEPDB1"
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

数据库初始化脚本：

```text
migration/init_database.sql
```

## 6. 接口和文件放哪里

公共接口已经建好，优先在这些位置继续写：

| 内容 | 目录 |
| --- | --- |
| 页面 Controller、API Controller、Views | `src/ECommerce.Web` |
| DTO、Service 接口 | `src/ECommerce.Application` |
| 状态枚举、领域常量 | `src/ECommerce.Domain` |
| Oracle 连接、Repository、UnitOfWork | `src/ECommerce.Infrastructure` |
| 统一响应、分页、错误码、权限常量 | `src/ECommerce.Shared` |
| 测试 | `tests/ECommerce.Tests` |

所有 `/api/v1/...` Controller 目前已经占好路由，默认返回 `501 NOT_IMPLEMENTED`。各成员实现自己模块时，把对应 Controller 改成调用自己的 Service。

## 7. 开发规则

- Controller 只调用 Application Service，不直接访问数据库。
- 不直接改其他成员模块的 Repository 或表操作。
- 跨模块调用只依赖 `docs/ARCHITECTURE_AND_INTERFACES.md` 里定义的 Service 接口和 DTO。
- 要改公共接口、DTO、状态枚举、错误码，先改文档并通知其他成员。
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
docs(workflow)：补充组员开工指南
test(cart)：新增购物车数量校验测试
```

提交前检查：

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

## 10. 避免提交的内容

不要提交：

- 真实数据库密码。
- `bin/`、`obj/` 构建产物。
- Office 临时文件，例如 `~$xxx.docx`。
- 与自己任务无关的大量格式化改动。

发现公共文件冲突时，先沟通再改，不要直接覆盖别人工作。
