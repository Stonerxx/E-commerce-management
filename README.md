# 电商购物平台——订单与商品管理系统

## 协作基线

本项目确定采用 B/S 模式，后端使用 ASP.NET Core MVC，数据库使用 Oracle。

- [组员开工指南](docs/TEAM_GUIDE.md)：分支、运行、提交、PR 和日常协作流程。
- [开发规范](docs/DEVELOPMENT_SPEC.md)：项目结构、接口规范、代码规范、API 格式、路由、事务和跨模块协作。
- [Oracle 初始化脚本](migration/init_database.sql)：24 张业务表、约束和索引。

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

这些账号已在 `migration/seed_demo_data.sql` 中写入 member2 `AuthService` 可校验的 PBKDF2 密码哈希，不再依赖临时登录逻辑。

当前项目状态：

- 已接入：真实登录注册、地址、权限、日志、商品分类、SKU、库存、购物车、订单、优惠券模板、统计 Dashboard 和导出基础。
- 仍需注意：支付演示仍使用 `TEMP_DEMO_PAYMENT` 临时页；用户领券与核销、物流和评价接口仍未完成。结算页面已暂时禁用优惠券，携带 `UserCouponId` 的请求会返回 `COUPON_NOT_READY`。
- 现阶段目标：在 `merging` 分支完成最终联调，`main` 负责正式服务器部署。

Oracle 连接默认是占位配置，不要提交真实密码。本项目现在约定两个服务器数据库用户：

- `ECOMMERCE_DEV`：开发联调用，组员在自己电脑上跑后端时连接这个用户。
- `ECOMMERCE_DEMO`：最终演示用，部署演示环境时连接这个用户。

后端代码不用区分本地库或远程库，只看 `Oracle__ConnectionString`。开发时把 `Data Source` 写成数据库服务器地址；以后后端部署到服务器上时，可以按实际网络环境写 `127.0.0.1`、内网地址或服务器域名。

```powershell
$env:Oracle__ConnectionString = "User Id=ECOMMERCE_DEV;Password=我们的开发库密码;Data Source=数据库服务器IP:1521/服务名"
dotnet run --project src/ECommerce.Web/ECommerce.Web.csproj
```

演示环境示例：

```powershell
$env:Oracle__ConnectionString = "User Id=ECOMMERCE_DEMO;Password=我们的演示库密码;Data Source=127.0.0.1:1521/服务名"
```

启动后可以验证：

```powershell
Invoke-RestMethod http://localhost:5052/api/v1/system/db-check
```

返回结果里重点看 `connected`、`sessionUser`、`currentSchema`。如果 `sessionUser` 是 `ECOMMERCE_DEV`，说明连到开发库；如果是 `ECOMMERCE_DEMO`，说明连到演示库。

第 1 人基础设施当前交付：

- `IUnitOfWork` 已提供 Oracle 连接复用、事务开启、提交、回滚和释放。
- `/api/v1/system/db-check` 已能检查 Oracle 配置和连接耗时。
- `deployment/` 下已提供发布脚本、环境变量样例、systemd 服务样例和 Nginx 反向代理样例。
- 服务器最终验收还需要填真实 `Oracle__ConnectionString`，运行初始化脚本，截图证明服务器公网地址能访问。

发布推荐走 GitHub Actions：

```text
push main
-> GitHub Actions 编译并上传产物
-> 服务器只解压、替换目录、重启 ecommerce
```

`merging` 分支只跑 build/test 检查，不部署服务器。

详细步骤见 [发布说明](docs/PUBLISH.md)。本地发布包仍可手动生成：

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
9. 临时模拟支付与订单状态同步（`TEMP_DEMO_PAYMENT`）
10. 物流信息录入与查询（待实现）
11. 订单分页查询、条件搜索
12. 订单统计（日/月销量、销售额）
13. 优惠券发放与使用核销（待实现）
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

	
	
	
	
	
	
	
	
	
