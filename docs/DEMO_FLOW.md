# 演示流程清单

本文用于答辩和联调演示。演示前先确认服务器已部署最新代码，并且数据库执行过：

```sql
@migration/init_database.sql
@migration/seed_demo_data.sql
```

## 1. 基础检查

浏览器打开：

```text
http://服务器IP/
http://服务器IP/health
http://服务器IP/api/v1/system/db-check
```

期望结果：

- 首页能打开。
- `/health` 返回成功。
- `/api/v1/system/db-check` 显示 `connected: true`。
- `sessionUser` 是当前演示环境对应的 Oracle 用户，例如 `ECOMMERCE_DEMO`。

## 2. 演示账号

密码统一为：

```text
demo123
```

账号用途：

| 账号 | 角色 | 用途 |
| --- | --- | --- |
| `demo_user` | `USER` | 从购物车创建新订单、模拟支付 |
| `demo_buyer` | `USER` | 查看已有已完成和已取消订单 |
| `demo_service` | `SERVICE` | 查看后台订单管理 |
| `demo_admin` | `ADMIN` | 管理员入口预留 |

## 3. 普通用户下单演示

使用 `demo_user / demo123` 登录。

操作顺序：

1. 打开首页。
2. 进入“购物车”。
3. 确认购物车里有已选商品。
4. 点击结算进入确认订单页。
5. 确认默认地址为 `演示收货人A`。
6. 提交订单。
7. 页面跳转到 `/payment/{orderId}`。
8. 点击“模拟支付成功”。
9. 返回“我的订单”，确认新订单状态变为“已支付”。

说明：`/payment/{orderId}` 当前是 `TEMP_DEMO_PAYMENT` 临时页，只负责在 member5 合入前打通演示闭环。

## 4. 历史订单演示

使用 `demo_buyer / demo123` 登录。

操作顺序：

1. 打开“我的订单”。
2. 查看 `已完成` 订单。
3. 打开订单详情。
4. 查看订单商品、收货信息和订单追踪。
5. 返回列表，查看 `已取消` 订单。

`demo_buyer` 不用于创建新订单，主要用于展示已有订单和评价数据。

## 5. 后台订单演示

使用 `demo_service / demo123` 登录。

操作顺序：

1. 打开首页。
2. 进入“后台订单管理”。
3. 查看全部演示订单。
4. 使用状态筛选查看待支付、已支付、已发货、已完成、已取消订单。
5. 打开订单详情，查看订单状态和明细。

普通 `USER` 账号不应该看到后台入口，手动访问 `/admin` 也应进入拒绝访问页面。

## 6. 临时逻辑替换清单

以下标记都表示临时代码，成员模块合入后需要替换：

| 标记 | 文件 | 替换来源 |
| --- | --- | --- |
| `TEMP_DEMO_ADDRESS` | `src/ECommerce.Infrastructure/Services/Mocks/MockAddressService.cs` | member2 地址服务 |
| `TEMP_DEMO_SKU` | `src/ECommerce.Infrastructure/Services/Mocks/MockSkuService.cs` | member3 SKU 服务 |
| `TEMP_DEMO_INVENTORY` | `src/ECommerce.Infrastructure/Services/Mocks/MockInventoryService.cs` | member3 库存服务 |
| `TEMP_DEMO_COUPON` | `src/ECommerce.Infrastructure/Services/Mocks/MockCouponService.cs` | member5 优惠券服务 |
| `TEMP_DEMO_PAYMENT` | `src/ECommerce.Web/Controllers/PaymentController.cs` | member5 支付服务 |

最终统合前可以运行：

```powershell
rg -n "TEMP_DEMO_" src docs README.md
```

确认所有临时代码是否仍需要保留。
