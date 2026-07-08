# Member1-6 统合集成清单

本文用于 `merging` 统合分支。每次只合并一个成员分支，合并后先构建、测试、检查入口，再继续下一个分支。

## 1. 分支约定

统合分支：

```text
merging
```

基础规则：

- `merging` 从最新 `main` 或已同步 `main` 的 member1 基础分支创建。
- 成员分支不要互相合并，统一由 `merging` 做最终整合。
- 每次合并前先 `git fetch origin --prune`。
- 每次只处理一个成员分支，冲突解决、构建、测试通过后再合下一个。
- 不在 `main` 上直接处理冲突。

建议合并顺序：

```text
1. feat-member1-foundation-oracle-deploy
2. feat-member2-user-permission-address-log
3. feat-member3-product-category-sku-inventory
4. feat-member4-cart-order-core
5. feat-member5-payment-coupon-logistics-review
6. feat-member6-stats-export-ui-docs
```

说明：member4 已合入 `main` 时，不需要重复合 member4；只需要确认它和后续真实服务实现的接线。

## 2. 每次合并命令

```powershell
git switch merging
git fetch origin --prune
git merge origin/目标成员分支
dotnet restore ECommerce.sln
dotnet build ECommerce.sln -c Release --no-restore
dotnet test ECommerce.sln -c Release --no-build --no-restore
git status
```

提交信息使用项目规范：

```text
feat(foundation)：统合成员基础功能
fix(order)：修复订单统合冲突
docs(workflow)：更新统合集成清单
```

## 3. 当前模块状态

| 成员 | 分支 | 模块 | 统合重点 | 状态 |
| --- | --- | --- | --- | --- |
| member1 | `feat-member1-foundation-oracle-deploy` | Oracle、部署、首页入口 | 环境变量、systemd、GitHub Actions、默认页导航 | 已有基础 |
| member2 | `feat-member2-user-permission-address-log` | 用户、权限、地址、操作日志 | Cookie 登录、角色、地址服务、日志服务 | 待合并检查 |
| member3 | `feat-member3-product-category-sku-inventory` | 分类、商品、SKU、库存 | 商品 API、SKU 服务、库存扣减/回滚 | 待合并检查 |
| member4 | `feat-member4-cart-order-core` | 购物车、订单 | Mock 服务替换、事务、订单状态流转 | 已进 main，仍需接线 |
| member5 | `feat-member5-payment-coupon-logistics-review` | 支付、优惠券、物流、评价 | 支付状态、优惠券核销、物流发货、评价权限 | 待合并检查 |
| member6 | `feat-member6-stats-export-ui-docs` | 统计、导出、UI、文档 | 后台首页、导出、页面统一、最终文档 | 待合并检查 |

## 4. Mock 替换表

member4 为了先跑通购物车和订单流程，当前存在 Mock 服务。最终统合时必须逐项替换为真实实现。

| Mock 类 | 真实模块来源 | 检查点 | 状态 |
| --- | --- | --- | --- |
| `MockAddressService` | member2 | 创建订单读取用户地址，默认地址逻辑正确 | 待替换 |
| `MockOperationLogService` | member2 | 订单取消、发货、支付等操作写入日志 | 待替换 |
| `MockSkuService` | member3 | 购物车和订单能读取真实 SKU、价格、商品名 | 待替换 |
| `MockInventoryService` | member3 | 创建订单锁库存，取消订单释放库存 | 待替换 |
| `MockCouponService` | member5 | 订单预览和提交能校验优惠券并核销 | 待替换 |

替换后必须检查：

- `src/ECommerce.Infrastructure/DependencyInjection.cs` 不再注册对应 Mock。
- 真实 Service 的接口签名与 `src/ECommerce.Application/Services` 一致。
- 单元测试不依赖真实数据库密码。

## 5. 公共接口冻结清单

合并时重点保护这些公共契约：

```text
src/ECommerce.Application/Services/*
src/ECommerce.Application/DTOs/*
src/ECommerce.Shared/Contracts/*
src/ECommerce.Shared/Constants/*
src/ECommerce.Domain/Enums/*
src/ECommerce.Web/Controllers/Api/*
migration/init_database.sql
```

发现成员分支修改公共接口时，要同步检查所有调用方：

- Controller 入参和返回 DTO。
- 页面 JS 调用的 API 路由。
- Service 实现方法签名。
- 测试里的构造参数和断言。

## 6. 数据库统合检查

每合一个成员分支后检查：

- 是否修改 `migration/init_database.sql`。
- 是否新增表、字段、索引、约束、触发器或序列。
- 是否影响已有 DEV/DEMO 数据。
- 是否需要 seed 数据支撑演示。
- 是否包含真实密码或本地连接字符串。

最终发布前确认：

```sql
SELECT COUNT(*) FROM USER_TABLES;
```

DEV 和 DEMO 用户都应完成同一套结构初始化或迁移。

演示/联调用测试数据单独放在：

```text
migration/seed_demo_data.sql
```

执行顺序：

```sql
@migration/init_database.sql
@migration/seed_demo_data.sql
```

注意：`seed_demo_data.sql` 使用 9000-9999 号段的显式 ID，可重复执行；其中 `password_hash` 是占位值，member2 完成真实认证后需要替换为登录算法对应的哈希。

## 7. 临时 Demo 登录标记

当前为了让购物车、订单和后台页面可以提前联调，`AccountController` 里保留了临时登录逻辑：

```text
TEMP_DEMO_AUTH
```

配置位置：

```json
"DemoAuth": {
  "Enabled": true,
  "Password": "demo123"
}
```

临时账号：

```text
demo_admin    ADMIN
demo_service  SERVICE
demo_user     USER
demo_buyer    USER
```

member2 合入真实注册登录后必须处理：

- 删除 `AccountController` 中的 `TEMP_DEMO_AUTH` 分支逻辑。
- 登录改为调用真实 `IAuthService`。
- `seed_demo_data.sql` 中的 `DEMO_HASH_REPLACE_AFTER_AUTH` 替换为真实密码哈希。
- 确认登录后写入 `NameIdentifier`、`Name`、`Role` claims。
- 如不再需要，删除或关闭 `DemoAuth` 配置。

## 8. 权限和路由检查

前台页面：

```text
GET /
GET /account/login
GET /account/register
GET /cart
GET /orders
GET /orders/{orderId}
GET /orders/create
```

后台页面：

```text
GET /admin
GET /admin/dashboard
GET /admin/orders
GET /admin/orders/{orderId}
```

系统检查：

```text
GET /health
GET /api/v1/system/db-check
GET /api/v1/system/version
```

权限要求：

- 首页、登录、注册、健康检查公开。
- 购物车、我的订单、创建订单使用 `CustomerOnly`。
- 后台订单和后台首页使用 `ServiceOrAdmin`。
- 管理员专属统计、导出、用户管理使用 `AdminOnly`。

## 9. 页面和 API 联调检查

合并后至少手动打开：

```text
/
/account/login
/cart
/orders
/admin
/admin/orders
/health
/api/v1/system/db-check
```

重点看：

- 页面是否 404。
- 未登录访问受保护页面是否跳转登录页。
- 登录后角色是否能进入对应页面。
- 页面 JS 请求路径是否和 API Controller 路由一致。
- API 返回是否仍符合 `ApiResponse<T>`。

## 10. 部署前检查

```powershell
dotnet restore ECommerce.sln
dotnet build ECommerce.sln -c Release --no-restore
dotnet test ECommerce.sln -c Release --no-build --no-restore
```

确认 GitHub Actions：

- `.github/workflows/build.yml` 只构建和测试，不部署。
- `.github/workflows/deploy.yml` 只在允许的分支或手动触发时部署。
- 数据库密码不进入 GitHub Secrets，仍由服务器环境变量决定。

## 11. 服务器发布后检查

服务器上执行：

```bash
systemctl status ecommerce --no-pager
journalctl -u ecommerce -n 100 --no-pager
curl -i http://127.0.0.1:5000/
curl -i http://127.0.0.1:5000/health
curl -i http://127.0.0.1:5000/api/v1/system/db-check
```

浏览器检查：

```text
http://服务器IP/
http://服务器IP/account/login
http://服务器IP/admin/orders
```

如果发布失败，优先看：

- GitHub Actions 日志。
- `/root/E-commerce-management/deployment/linux/deploy-ecommerce-artifact.sh` 输出。
- `journalctl -u ecommerce`。
- `/etc/ecommerce/ecommerce.env` 或 `ecommerce.service` 中的运行时环境变量。

## 12. 最终演示数据

答辩前至少准备：

- 管理员账号。
- 客服账号。
- 普通用户账号。
- 分类、商品、SKU、库存。
- 用户地址。
- 可用优惠券。
- 待支付、已支付、已发货、已完成、已取消订单。
- 支付记录、物流记录、评价。
- 后台统计和导出样例数据。

当前基础演示数据脚本已覆盖上述核心对象，后续成员合并真实功能后按实际字段继续维护 `migration/seed_demo_data.sql`。
