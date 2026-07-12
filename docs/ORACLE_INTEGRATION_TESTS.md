下面这版可以直接替换 `docs/ORACLE_INTEGRATION_TESTS.md`。

````markdown
# Oracle 集成测试使用说明

这组测试不是普通的“假数据库测试”，而是会真的连接 Oracle 数据库，检查项目里的 SQL 和数据库约束能不能正常工作。

它主要检查这些内容：

- 能不能正常连接 Oracle；
- 项目需要的表和约束是否存在；
- SQL 参数绑定是否正确；
- 事务回滚后，临时数据是否真的被撤销；
- 购物车和订单商品数量能不能被限制为大于 0；
- 两个请求同时抢最后一件库存时，是否只允许一个成功；
- 支付和取消同时操作同一张订单时，是否只允许一个成功；
- 函数、存储过程、视图和触发器是否真实存在并参与业务操作；
- 可选测试：一次导出 5,000 行订单到 Excel。

---

## 一、运行前要知道的事情

### 1. 测试不会自动建表

运行测试时，它不会自动执行：

```text
migration/init_database.sql
migration/database_objects.sql
migration/seed_demo_data.sql
````

也就是说，数据库里必须已经有项目需要的表和基础演示数据。

至少要有：

* 用户；
* 收货地址；
* 商品；
* SKU；
* 订单相关表；
* 购物车相关表。

---

### 2. 不要随便在共用数据库运行 `init_database.sql`

`init_database.sql` 不是普通升级脚本。

它会先删除现有的所有业务表，再重新创建。

因此，执行：

```powershell
sqlplus 用户名/密码@//数据库地址:1521/FREEPDB1 @migration/init_database.sql
```

会清空当前数据库中的业务数据。

只应在以下情况执行：

* 新建了一个空的测试数据库用户；
* 明确允许清空并重建当前数据库；
* 已经备份了需要保留的数据。

不要在其他组员正在共同使用的开发库里随便执行。

---

## 二、推荐使用专门的测试数据库

最安全的方式是单独创建一个测试数据库用户，例如：

```text
ECOMMERCE_TEST
```

数据库用途可以这样划分：

```text
ECOMMERCE_DEV   组员日常开发和联调
ECOMMERCE_DEMO  最终答辩演示
ECOMMERCE_TEST  自动化集成测试
```

然后只在 `ECOMMERCE_TEST` 中执行：

```powershell
sqlplus ECOMMERCE_TEST/密码@//数据库地址:1521/FREEPDB1 @migration/init_database.sql
sqlplus ECOMMERCE_TEST/密码@//数据库地址:1521/FREEPDB1 @migration/database_objects.sql
sqlplus ECOMMERCE_TEST/密码@//数据库地址:1521/FREEPDB1 @migration/seed_demo_data.sql
```

这样测试出问题时，不会影响开发库和演示库。

---

## 三、设置 DEV 测试连接串

Oracle 测试项目通过环境变量读取连接信息。

在 PowerShell 中执行：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING = 'User Id=ECOMMERCE_TEST;Password=你的密码;Data Source=数据库地址:1521/FREEPDB1'
```

这里的数据库地址要根据运行位置填写。

### 在数据库服务器本机运行

可以写：

```text
127.0.0.1
```

例如：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING = 'User Id=ECOMMERCE_TEST;Password=你的密码;Data Source=127.0.0.1:1521/FREEPDB1'
```

### 在自己的 Windows 电脑运行

应该填写服务器 IP 或能够访问数据库的域名：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING = 'User Id=ECOMMERCE_TEST;Password=你的密码;Data Source=服务器IP:1521/FREEPDB1'
```

不要在自己电脑上写 `127.0.0.1`，除非 Oracle 就安装在自己电脑上。

---

## 四、运行普通 Oracle 测试

第一次运行前，先恢复依赖并编译：

```powershell
dotnet restore ECommerce.sln

dotnet build ECommerce.sln `
  -c Release `
  --no-restore
```

然后运行 Oracle 集成测试：

```powershell
dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj `
  -c Release `
  --no-restore
```

普通测试会检查：

1. 关键数据库表是否存在；
2. 数量约束是否存在；
3. Oracle 事务回滚是否正常；
4. 数量为 0 的购物车和订单明细是否会被数据库拒绝；
5. 两个请求同时抢一件库存时，是否只有一个成功；
6. 支付和取消同时操作订单时，是否只有一个成功。

---

## 五、测试是否会污染数据库

普通测试会临时插入一些数据，例如：

* 临时 SKU；
* 临时购物车记录；
* 临时订单。

测试代码会通过两种方式清理：

### 事务测试

测试结束时执行：

```text
ROLLBACK
```

所以临时数据不会真正保存。

### 并发测试和导出测试

测试完成后，会在 `finally` 中执行 DELETE，删除临时记录。

临时订单号通常以这些内容开头：

```text
ORACLE-IT-
ORACLE-EXPORT-
```

如果测试进程被强制关闭，导致清理代码没有运行，可以根据这些前缀查找残留数据。

例如：

```sql
SELECT *
FROM ORDER_MAIN
WHERE ORDER_NO LIKE 'ORACLE-IT-%'
   OR ORDER_NO LIKE 'ORACLE-EXPORT-%';
```

确认是测试数据后再手动删除。

---

## 六、DEMO 数据库检查

项目还提供一个只读取演示数据库的测试。

它主要检查演示数据库中是否存在固定的基础数据，例如：

* 演示用户；
* 演示商品；
* 演示订单。

先设置：

```powershell
$env:ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING = 'User Id=ECOMMERCE_DEMO;Password=你的密码;Data Source=数据库地址:1521/FREEPDB1'
```

然后正常运行测试：

```powershell
dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj `
  -c Release `
  --no-restore
```

这个测试只执行查询，不会主动修改 DEMO 数据。

没有设置该环境变量时，测试会显示：

```text
SKIP
```

这是正常现象，不是失败。

---

## 七、5,000 行 Excel 导出测试

5,000 行导出测试默认不会执行，因为它会：

1. 插入 5,000 张临时订单；
2. 调用真实的 Excel 导出服务；
3. 生成 Excel；
4. 再次打开 Excel；
5. 检查是否包含 5,000 行订单；
6. 删除临时订单。

需要显式开启：

```powershell
$env:ECOMMERCE_ORACLE_LONG_RUNNING = '1'
```

然后运行：

```powershell
dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj `
  -c Release `
  --no-restore `
  --filter "Category=LongRunning"
```

该测试耗时更长，也会短时间向数据库写入大量测试数据，因此不建议每次开发都运行。

---

## 八、如何看测试结果

例如输出：

```text
测试摘要: 总计: 7, 失败: 0, 成功: 5, 已跳过: 2
```

意思是：

```text
一共发现 7 个测试
5 个实际执行并通过
0 个失败
2 个因为没有开启对应条件而跳过
```

常见的两个跳过项是：

```text
DEMO 数据库检查
5,000 行 Excel 导出
```

只要失败数量是：

```text
0
```

就说明本次实际执行的测试全部通过。

---

## 九、没有设置连接串会怎样

没有设置：

```text
ECOMMERCE_ORACLE_DEV_CONNECTION_STRING
```

时，所有需要写 DEV 数据库的测试都会跳过。

没有设置：

```text
ECOMMERCE_ORACLE_DEMO_CONNECTION_STRING
```

时，DEMO 查询测试会跳过。

没有设置：

```text
ECOMMERCE_ORACLE_LONG_RUNNING=1
```

时，5,000 行导出测试会跳过。

跳过会显示：

```text
SKIP
```

不会算作测试失败。

因此，看到 GitHub Actions 变绿，也不一定代表真实 Oracle 测试已经执行。

需要检查测试输出到底是：

```text
成功
```

还是：

```text
已跳过
```

---

## 十、推荐的日常操作流程

### 普通开发时

运行：

```powershell
dotnet restore ECommerce.sln

dotnet test tests/ECommerce.OracleIntegrationTests/ECommerce.OracleIntegrationTests.csproj `
  -c Release `
  --no-restore
```

主要验证 Oracle 表结构、事务、约束和并发更新。

### 提交最终版本前

除了普通测试，还应设置 DEMO 连接串并检查演示数据。

### 导出功能完成后

单独开启：

```powershell
$env:ECOMMERCE_ORACLE_LONG_RUNNING = '1'
```

再运行 5,000 行导出测试。

---

## 十一、常见问题

### 测试全部显示 SKIP

通常是没有设置 DEV 连接串。

检查：

```powershell
$env:ECOMMERCE_ORACLE_DEV_CONNECTION_STRING
```

如果没有输出，重新设置环境变量。

---

### 提示数据库没有 USER、ADDRESS 或 PRODUCT 数据

说明只建了表，但没有执行演示数据脚本。

在允许写入的测试数据库中执行：

```powershell
sqlplus ECOMMERCE_TEST/密码@//数据库地址:1521/FREEPDB1 @migration/seed_demo_data.sql
```

---

### 提示找不到数量约束

说明数据库结构不是最新版本。

需要检查是否存在：

```text
CH_CART_QUANTITY
CH_ORDER_ITEM_QUANTITY
```

可以执行：

```sql
SELECT CONSTRAINT_NAME, STATUS
FROM USER_CONSTRAINTS
WHERE CONSTRAINT_NAME IN (
    'CH_CART_QUANTITY',
    'CH_ORDER_ITEM_QUANTITY'
);
```

---

### 测试完成后发现测试数据残留

查询：

```sql
SELECT ID, ORDER_NO
FROM ORDER_MAIN
WHERE ORDER_NO LIKE 'ORACLE-IT-%'
   OR ORDER_NO LIKE 'ORACLE-EXPORT-%';
```

确认无误后再删除。

---

## 十二、一句话说明

这套测试的作用是：

> 用 C# 自动连接真实 Oracle，临时写入测试数据，检查项目 SQL、事务、约束、库存并发和订单状态更新是否真的能够工作，然后尽量把测试数据清理掉。

它不是数据库初始化工具，也不会自动升级数据库。
