# Oracle 集成测试

这些测试连接真实 Oracle，用于验证 Repository SQL、参数绑定、事务回滚、约束、并发状态更新、库存原子锁定，以及可选的 5,000 行 Excel 导出。测试不会自动执行 `init_database.sql` 或 `seed_demo_data.sql`。

## 准备数据库

先由操作者在 DEV 库手动执行以下脚本；测试假定已有基础演示数据，但不会重置它：

```powershell
sqlplus ecommerce/你的密码@//127.0.0.1:1521/FREEPDB1 @migration/init_database.sql
sqlplus ecommerce/你的密码@//127.0.0.1:1521/FREEPDB1 @migration/seed_demo_data.sql
```

配置只用于集成测试的连接串。DEV 账号需要能插入、更新和删除临时测试数据；DEMO 账号只需要只读权限：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING = 'User Id=ecommerce;Password=你的密码;Data Source=127.0.0.1:1521/FREEPDB1'
$env:ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING = 'User Id=ecommerce_demo;Password=你的只读密码;Data Source=127.0.0.1:1521/FREEPDB1'
```

运行常规真实 Oracle 测试：

```powershell
dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj -c Release --no-restore
```

5,000 行导出测试刻意默认跳过，避免日常验证占用数据库和生成大文件。需要时显式启用：

```powershell
$env:ECOMMERCE_ORACLE_LONG_RUNNING = '1'
dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj -c Release --no-restore --filter 'Category=LongRunning'
```

DEV 测试的临时记录使用运行时生成的 ID；每个会提交的测试都在 `finally` 清理。若进程在测试期间被强制终止，可通过订单号前缀 `ORACLE-IT-` 或 `ORACLE-EXPORT-` 查找并清理残留记录。DEMO 测试不会进行任何写操作。
