# 电商购物平台管理系统

## 协作基线

本项目确定采用 B/S 模式，后端使用 ASP.NET Core MVC，数据库使用 Oracle。

- [组员开工指南](docs/TEAM_GUIDE.md)：分支、运行、提交、PR 和日常协作流程。
- [开发规范](docs/DEVELOPMENT_SPEC.md)：项目结构、接口规范、代码规范、API 格式、路由、事务和跨模块协作。
- [完整演示流程](docs/DEMO_FLOW.md)：按角色串联前台、后台和关键业务闭环。
- [Oracle 集成测试](docs/ORACLE_INTEGRATION_TESTS.md)：连接串、跳过条件和真实数据库测试说明。
- [发布说明](docs/PUBLISH.md)：GitHub Actions、systemd、Nginx 与服务器验收。
- [Oracle 初始化脚本](migration/init_database.sql)：24 张业务表、约束和索引。
- [Oracle 数据库对象脚本](migration/database_objects.sql)：库存函数、统计过程、报表视图，以及订单一致性/销量触发器。

## 项目启动

项目已创建为 ASP.NET Core MVC + 五层类库结构：

```text
ECommerce.sln
src/ECommerce.Web
src/ECommerce.Application
src/ECommerce.Domain
src/ECommerce.Infrastructure
src/ECommerce.Shared
tests/ECommerce.Tests
tests/ECommerce.OracleIntegrationTests
```

本地运行：

```powershell
dotnet restore ECommerce.sln
dotnet build ECommerce.sln
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

看到类似下面输出时，服务才是在运行中：

```text
Now listening on: http://localhost:5052
Application started. Press Ctrl+C to shut down.
```

不要关闭这个终端，然后在浏览器打开：

```text
http://localhost:5052/
http://localhost:5052/health
http://localhost:5052/account/login
http://localhost:5052/admin/dashboard
```

如果终端已经回到 `PS C:\...>`，说明 Web 服务已经停止，需要重新执行 `dotnet run`。

演示登录账号：

```text
密码统一为 demo123

demo_admin    ADMIN，后台管理演示
demo_service  SERVICE，后台订单演示
demo_user     USER，购物车和我的订单演示
demo_buyer    USER，已完成订单和评价演示
```

这些账号已在 `migration/seed_demo_data.sql` 中写入 `AuthService` 可校验的 PBKDF2 密码哈希。

当前项目状态：

- 已接入：登录注册、地址、权限、日志、商品、SKU、库存、购物车、订单、持久化模拟支付、优惠券、物流、评价、统计 Dashboard 和 Excel 导出。
- 前后台页面已覆盖优惠券领取与模板管理、订单评价与审核、物流发货与轨迹维护；模拟支付会写入 `PAYMENT` 并与订单状态、库存扣减使用同一事务。
- `merging` 作为统合基线，功能收尾在成员分支验证后再进入 `main` 正式部署。

Oracle 连接默认是占位配置，不要提交真实密码。`User Id` 应填写实际执行过三份数据库脚本的 Schema；当前开发和演示共用 `ECOMMERCE_DEV` 用户。后端只读取 `Oracle__ConnectionString`，在本机连接云数据库时，`Data Source` 要填写云服务器 IP 或域名；只有 Oracle 与应用运行在同一台服务器上时才使用 `127.0.0.1`。

```powershell
$env:Oracle__ConnectionString = "User Id=ECOMMERCE_DEV;Password=数据库密码;Data Source=数据库服务器IP:1521/服务名"
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

启动后可以验证：

```powershell
Invoke-RestMethod http://localhost:5052/api/v1/system/db-check
```

返回结果里重点看 `connected`、`sessionUser`、`currentSchema`，确认应用连接的是预期 Schema。

数据库首次初始化或明确允许重建时，按下面顺序手动执行。`init_database.sql` 会删除并重建 24 张业务表，不要在需要保留数据的 Schema 上误执行：

```powershell
sqlplus 用户名/密码@//数据库地址:1521/服务名 @migration/init_database.sql
sqlplus 用户名/密码@//数据库地址:1521/服务名 @migration/database_objects.sql
sqlplus 用户名/密码@//数据库地址:1521/服务名 @migration/seed_demo_data.sql
```

发布推荐走 GitHub Actions：

```text
push main
-> GitHub Actions 编译并上传产物
-> 服务器只解压、替换目录、重启 ecommerce
```

`merging` 分支只跑 build/test 检查，不部署服务器。

详细步骤见 [发布说明](docs/PUBLISH.md)。本地发布包也可手动生成：

```powershell
powershell -ExecutionPolicy Bypass -File deployment/publish.ps1
```

### 项目功能点

1. 用户注册/登录/权限控制
2. 商品分类管理（增删改查）
3. 商品信息管理（含上下架、图片、规格）
4. 商品库存预警与盘点
5. 购物车添加/修改/清空
6. 订单创建、取消、确认
7. 订单状态流转（待支付→已支付→发货→完成）
8. 多地址管理与默认地址
9. 持久化模拟支付、签名回调与订单状态同步
10. 物流信息录入、轨迹维护与用户查询
11. 订单分页查询、条件搜索
12. 订单统计（日/月销量、销售额）
13. 优惠券发放、领取与订单原子核销
14. 商品热度排行与推荐
15. 数据导出（Excel订单报表）
16. 管理员操作日志审计
17. 商品评价管理
18. 库存自动扣减与回滚
19. 订单超时自动关闭
20. 权限分级管理（普通用户/客服/管理员）

### 项目成员（按学号排序）
2451883	孙铭坤（组长）
2352164	宋金昊
2450204	郑书宏
2451389	颜泽霖
2451460	庞乐鸣
2451917	秦康乔
2452606	陈祉越
2452688	余伟强
2454339	张一诺
2557326	庄辰和
