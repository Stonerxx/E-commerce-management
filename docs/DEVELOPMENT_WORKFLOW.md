# 团队协作、命名与提交规范

本文档约定分工、分支、命名、提交信息和合并流程。所有成员从同一份接口契约出发，减少后期集成冲突。

## 1. 分工与分支

| 人员 | 主责 | 具体任务 | 最终交付 | 分支 |
| --- | --- | --- | --- | --- |
| 第 1 人 | 项目骨架、数据库连接、部署 | 搭 ASP.NET Core MVC 项目；配置 Oracle 连接；建立五层目录；跑通登录页；准备云服务器部署；写 `README` 启动说明 | `.sln` 项目能启动；能连 Oracle；服务器能访问；有部署截图 | `feat-member1-foundation-oracle-deploy` |
| 第 2 人 | 用户、权限、地址、日志 | 注册/登录；普通用户、客服、管理员角色；权限控制；收货地址管理；管理员操作日志 | 登录注册页面；角色权限生效；地址增删改查；操作日志可查 | `feat-member2-user-permission-address-log` |
| 第 3 人 | 商品、分类、SKU、库存 | 商品分类；商品上下架；图片/规格/SKU；库存预警；库存调整；库存变动日志 | 后台能维护商品和 SKU；前台能浏览商品；库存日志能追溯 | `feat-member3-product-category-sku-inventory` |
| 第 4 人 | 购物车、订单核心流程 | 加入购物车；修改数量；创建订单；订单取消；订单确认；订单状态日志；创建订单时锁定库存 | 能从购物车生成订单；订单明细正确；库存锁定；订单日志正确 | `feat-member4-cart-order-core` |
| 第 5 人 | 支付、优惠券、物流、评价 | 优惠券发放/领取/核销；模拟支付；支付状态同步；发货和物流轨迹；商品评价 | 支付后订单变已支付；优惠券状态变已使用；后台发货；用户评价 | `feat-member5-payment-coupon-logistics-review` |
| 第 6 人 | 统计、导出、测试、UI、文档整合 | 订单统计；Excel 导出；后台首页；统一 Bootstrap 页面；整理测试用例；写系统需求分析和设计实现文档的大部分内容；PPT 初稿 | 统计页面；Excel 文件；测试截图；两份文档初稿；PPT 初稿 | `feat-member6-stats-export-ui-docs` |

说明：表中的“第 N 人”按本次任务分工编号，组长可再映射到具体姓名。每个分支只做对应模块，公共接口变更必须先更新 `docs/ARCHITECTURE_AND_INTERFACES.md`。

## 2. 开发流程

1. 先阅读 `README.md`、`docs/ARCHITECTURE_AND_INTERFACES.md`、本文档。
2. 切到自己的分支，例如：`git switch feat-member3-product-category-sku-inventory`。
3. 开发前同步主线：`git fetch origin`，必要时从 `main` 合并最新接口文档。
4. 只修改自己负责模块；确需修改公共结构时先在群里说明。
5. 每完成一个可运行的小功能就提交一次。
6. 合并前本地跑通项目、数据库连接和自己模块的关键流程。
7. 合并顺序建议：第 1 人骨架先合并；第 2、3 人基础数据模块随后；第 4、5 人订单链路随后；第 6 人最后统一 UI、测试、文档和演示材料。

## 3. 分支命名

统一使用：

```text
feat-member{编号}-{模块关键词}
```

规则：

- 全小写。
- 单词用 `-` 分隔。
- 编号不补零，例如 `member1`。
- 模块关键词表达主责，不使用姓名拼音。
- 临时修复可以用 `fix-{问题关键词}`。

示例：

```text
feat-member1-foundation-oracle-deploy
feat-member4-cart-order-core
fix-order-status-log
```

## 4. 提交信息规范

提交信息使用 Conventional Commits 风格，但摘要写中文：

```text
<type>(<scope>)：<中文摘要>
```

其中 `type` 和 `scope` 使用英文，冒号使用中文全角冒号 `：`，摘要使用中文。

常用 `type`：

| type | 用途 |
| --- | --- |
| `feat` | 新功能 |
| `fix` | 缺陷修复 |
| `docs` | 文档 |
| `style` | 仅格式、样式，不影响逻辑 |
| `refactor` | 重构，不新增功能、不修 bug |
| `test` | 测试用例 |
| `build` | 构建、依赖、项目文件 |
| `chore` | 杂项维护 |
| `perf` | 性能优化 |

建议 `scope`：

```text
foundation
oracle
auth
user
permission
address
log
category
product
sku
inventory
cart
order
payment
coupon
logistics
review
statistics
export
ui
docs
test
```

好例子：

```text
feat(auth)：新增登录注册页面
feat(order)：创建订单时锁定 SKU 库存
fix(payment)：防止模拟支付回调重复处理
docs(api)：定义模块接口契约
test(cart)：新增购物车数量校验用例
```

不要这样写：

```text
update
fix bug
提交一下
修改代码
```

## 5. C# 命名规范

| 类型 | 规范 | 示例 |
| --- | --- | --- |
| 项目 | PascalCase | `ECommerce.Web` |
| 命名空间 | PascalCase | `ECommerce.Application.Services` |
| 类 | PascalCase | `ProductService` |
| 接口 | `I` + PascalCase | `IProductService` |
| 方法 | PascalCase，异步加 `Async` | `CreateOrderAsync` |
| 参数和局部变量 | camelCase | `orderId` |
| 私有字段 | `_camelCase` | `_orderService` |
| 常量 | PascalCase | `DefaultPageSize` |
| DTO | 动作 + Request/Response/Dto | `CreateOrderRequest` |
| ViewModel | 页面 + ViewModel | `ProductDetailViewModel` |
| Controller | 资源名 + Controller | `ProductsController` |

Controller 命名建议：

```text
AccountController
ProductsController
CartController
OrdersController
AddressesController
AdminProductsController
AdminOrdersController
AdminStatisticsController
```

## 6. 路由与 API 命名

- 页面路由使用小写复数资源名：`/products`、`/orders`。
- 后台页面统一前缀：`/admin/...`。
- JSON API 统一前缀：`/api/v1/...`。
- 后台 JSON API 统一前缀：`/api/v1/admin/...`。
- URL 单词使用 `-`，例如 `/operation-logs`。
- 资源 ID 参数统一写作 `{resourceId}`，例如 `{orderId}`、`{skuId}`。

## 7. 数据库命名

已有 Oracle 脚本采用：

- 表名：大写下划线，例如 `ORDER_MAIN`；保留字表使用双引号，例如 `"USER"`。
- 字段名：小写下划线，例如 `created_at`。
- 主键：统一为 `id`。
- 外键：`{entity}_id`，例如 `user_id`、`order_id`。
- 唯一约束：`uk_{table}_{columns}`。
- 外键约束：`fk_{table}_{ref}`。
- 检查约束：`ch_{table}_{field}`。
- 索引：`idx_{table}_{columns}`。

C# Entity 属性使用 PascalCase，映射到数据库字段：

```text
ORDER_MAIN.order_no -> OrderMain.OrderNo
SKU.locked_stock -> Sku.LockedStock
```

## 8. DTO 与 ViewModel 规范

- Request DTO 只承载输入，不包含数据库实体。
- Response/Dto 只暴露页面需要的字段，不直接返回 Entity。
- ViewModel 用于 Razor 页面组合展示数据。
- 金额字段统一 `decimal`。
- 时间字段统一 `DateTime`。
- 列表接口统一返回 `PagedResult<T>`。

示例：

```csharp
public sealed record ProductListItemDto(
    long ProductId,
    string Name,
    string MainImage,
    decimal PriceMin,
    int SalesCount,
    decimal AvgRating);
```

## 9. 页面与 UI 规范

- 统一 Bootstrap。
- 前台布局：`Views/Shared/_Layout.cshtml`。
- 后台布局：`Views/Shared/_AdminLayout.cshtml`。
- 表单必须显示校验错误。
- 列表页必须支持空状态。
- 后台列表页必须支持分页和筛选。
- 重要状态操作必须二次确认，例如删除、取消订单、禁用用户、商品下架。

## 10. 操作日志规范

后台关键写操作都要记录 `OPERATION_LOG`：

| 模块 | 需要记录的操作 |
| --- | --- |
| 用户 | 禁用/启用用户、分配角色 |
| 商品 | 新增、修改、上下架、删除图片 |
| 库存 | 手工调整库存 |
| 优惠券 | 新增、修改、启停 |
| 订单 | 后台取消、发货 |
| 评价 | 审核、屏蔽 |

日志描述要能看出“谁在什么时候对哪个资源做了什么”，请求参数需脱敏。

## 11. 接口变更流程

接口已经在 `docs/ARCHITECTURE_AND_INTERFACES.md` 定义。变更流程如下：

1. 在群里说明要改的接口、原因、影响模块。
2. 先改接口文档。
3. 文档合并到 `main` 后，各成员同步。
4. 再改代码实现。

禁止先在个人分支里随意改公共路由、DTO 字段、状态码或枚举值。

## 12. 合并前检查清单

- 项目能启动。
- 数据库连接配置不提交个人密码。
- 自己模块页面能正常访问。
- 自己负责的 JSON API 返回统一格式。
- 表单校验错误能显示。
- 后台操作有权限控制。
- 写操作需要的日志已记录。
- 订单、库存、优惠券、支付相关事务没有半成功状态。
- README 或文档同步更新。
- 提交信息符合 `<type>(<scope>)：<中文摘要>`。
