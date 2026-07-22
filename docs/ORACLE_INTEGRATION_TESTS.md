# Oracle 集成测试使用说明

`tests/ECommerce.OracleIntegrationTests` 会连接真实 Oracle，验证表结构、约束、事务、并发控制、数据库对象和 Excel 导出。它不会自动建表，也不要求数据库用户以 `TEST` 结尾。

## 1. 环境变量

| 变量 | 用途 | 默认行为 |
| --- | --- | --- |
| `ECOMMERCE_ORACLE_DEV_CONNECTION_STRING` | 可写 Schema，用于大部分集成测试 | 未设置时跳过可写测试 |
| `ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING` | 只读检查固定演示数据 | 未设置时跳过 DEMO 检查 |
| `ECOMMERCE_ORACLE_LONG_RUNNING` | 值为 `1` 时启用 5,000 行 Excel 测试 | 未设置时跳过长耗时测试 |

项目实际使用两个已创建的 Oracle 用户（Schema）：`ECOMMERCE_DEV` 负责开发联调，`ECOMMERCE_DEMO` 负责最终演示。测试环境变量与这两个用户一一对应：DEV 连接串用于可写测试，DEMO 连接串用于不写入数据的演示基线检查。项目没有、也无需创建 `ECOMMERCE_TEST` 用户。

PowerShell 当前窗口临时设置：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING = 'User Id=ECOMMERCE_DEV;Password=数据库密码;Data Source=服务器公网IP:1521/FREEPDB1'
$env:ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING = 'User Id=ECOMMERCE_DEMO;Password=演示库密码;Data Source=服务器公网IP:1521/FREEPDB1'
```

持久写入当前 Windows 用户：

```powershell
[Environment]::SetEnvironmentVariable(
  'ECOMMERCE_ORACLE_DEV_CONNECTION_STRING',
  'User Id=ECOMMERCE_DEV;Password=数据库密码;Data Source=服务器公网IP:1521/FREEPDB1',
  'User')

[Environment]::SetEnvironmentVariable(
  'ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING',
  'User Id=ECOMMERCE_DEMO;Password=演示库密码;Data Source=服务器公网IP:1521/FREEPDB1',
  'User')
```

持久变量只对新启动的进程生效。设置后重新打开 PowerShell 或 IDE；如果要在当前窗口立即测试，仍需给 `$env:...` 赋值。

检查当前进程是否已读取变量：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING
$env:ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING
```

本机连接云数据库时，`Data Source` 必须写云服务器公网 IP。只有 Oracle 就运行在本机时，才写 `127.0.0.1`。

## 2. 数据库准备

测试不会自动执行以下脚本：

```text
migration/init_database.sql
migration/database_objects.sql
migration/seed_demo_data.sql
```

`ECOMMERCE_DEV` 和 `ECOMMERCE_DEMO` 都应按顺序执行过三份脚本。两者是不同 Schema，需要分别初始化；如果当前结构和演示数据已经齐全，直接运行测试即可。只在新建空 Schema 或明确允许清空数据时手动执行：

```powershell
sqlplus ECOMMERCE_DEV/密码@//服务器公网IP:1521/FREEPDB1 @migration/init_database.sql
sqlplus ECOMMERCE_DEV/密码@//服务器公网IP:1521/FREEPDB1 @migration/database_objects.sql
sqlplus ECOMMERCE_DEV/密码@//服务器公网IP:1521/FREEPDB1 @migration/seed_demo_data.sql

sqlplus ECOMMERCE_DEMO/密码@//服务器公网IP:1521/FREEPDB1 @migration/init_database.sql
sqlplus ECOMMERCE_DEMO/密码@//服务器公网IP:1521/FREEPDB1 @migration/database_objects.sql
sqlplus ECOMMERCE_DEMO/密码@//服务器公网IP:1521/FREEPDB1 @migration/seed_demo_data.sql
```

> `init_database.sql` 会删除并重建 24 张业务表，不是增量升级脚本。不要在需要保留数据或其他成员正在使用的 Schema 上误执行。

## 3. 运行测试

运行全部常规 Oracle 集成测试：

```powershell
dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj -c Release
```

测试覆盖：

- Oracle 连通性、24 张表和关键约束；
- 参数绑定和事务回滚；
- 购物车与订单明细数量约束；
- 库存竞争、支付与取消并发；
- 支付、优惠券、物流与评价业务；
- 函数、存储过程、视图和触发器；
- DEMO 固定数据只读校验（设置 DEMO 连接串后）。

测试摘要应满足“失败为 0”。实际总数会随测试代码变化，不在文档中固定写死。

### 为什么仍有跳过项

以下两项默认跳过是正常的：

- `Demo_database_contains_seed_baseline_without_writing`：未设置 `ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING`；
- `Order_export_returns_a_valid_5000_row_workbook`：未设置 `ECOMMERCE_ORACLE_LONG_RUNNING=1`。

如果所有可写测试都显示 `SKIP`，说明运行 `dotnet test` 的进程没有读到 `ECOMMERCE_ORACLE_DEV_CONNECTION_STRING`。

## 4. 运行 5,000 行 Excel 测试

该测试会临时插入 5,000 张订单，生成并重新读取工作簿，耗时和数据库写入量都更大：

```powershell
$env:ECOMMERCE_ORACLE_LONG_RUNNING = '1'

dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj `
  -c Release `
  --filter "Category=LongRunning"
```

如需持久开启：

```powershell
[Environment]::SetEnvironmentVariable('ECOMMERCE_ORACLE_LONG_RUNNING', '1', 'User')
```

日常开发不建议持久开启长耗时测试；运行完可删除用户变量：

```powershell
[Environment]::SetEnvironmentVariable('ECOMMERCE_ORACLE_LONG_RUNNING', $null, 'User')
Remove-Item Env:ECOMMERCE_ORACLE_LONG_RUNNING -ErrorAction SilentlyContinue
```

## 5. 数据清理

事务类测试会回滚；并发和导出测试会在 `finally` 中删除临时记录。若测试进程被强制结束，可用订单号前缀定位残留：

```sql
SELECT ID, ORDER_NO
FROM ORDER_MAIN
WHERE ORDER_NO LIKE 'ORACLE-IT-%'
   OR ORDER_NO LIKE 'ORACLE-EXPORT-%';
```

确认是测试数据后再清理，不要按前缀之外的条件批量删除。

## 6. 常见问题

### 当前窗口有变量，IDE 里仍跳过

PowerShell 的 `$env:` 只传给从该窗口启动的子进程。若从 Visual Studio、VS Code 或 Codex 运行测试，需要重启对应程序，或在它实际使用的终端里重新赋值。

### 提示缺少表、基础数据或数据库对象

确认连接到了预期 Schema，并核对三份 SQL 的执行顺序。只缺演示数据时可单独执行 `seed_demo_data.sql`；不要因此重复执行会重建表的 `init_database.sql`。

### 提示缺少数量约束

检查约束状态：

```sql
SELECT CONSTRAINT_NAME, STATUS
FROM USER_CONSTRAINTS
WHERE CONSTRAINT_NAME IN ('CH_CART_QUANTITY', 'CH_ORDER_ITEM_QUANTITY');
```

### CI 变绿是否代表真实 Oracle 已验证

不一定。没有连接串时相关用例会跳过。验收时既要看“失败为 0”，也要确认需要执行的 Oracle 用例显示为“成功”而不是“已跳过”。
