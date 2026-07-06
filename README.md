# 电商购物平台——订单与商品管理系统

## 协作基线

本项目确定采用 B/S 模式，后端使用 ASP.NET Core MVC，数据库使用 Oracle。

- [B/S 架构与接口契约](docs/ARCHITECTURE_AND_INTERFACES.md)：五层目录、页面路由、JSON API、DTO、Service 接口、事务边界。
- [团队协作、命名与提交规范](docs/DEVELOPMENT_WORKFLOW.md)：成员分工、独立分支、命名规范、提交信息格式、合并检查清单。
- [组员开工指南](docs/TEAM_START_GUIDE.md)：克隆项目、切换分支、配置 Oracle、运行项目、提交和 PR 流程。
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

Oracle 连接配置位于 `src/ECommerce.Web/appsettings.json` 的 `Oracle:ConnectionString`，本地开发时按自己的数据库账号修改，不要提交真实密码。

### 项目功能点
1. 用户注册/登录/权限控制
2. 商品分类管理（增删改查）
3. 商品信息管理（含上下架、图片、规格）
4. 商品库存预警与盘点
5. 购物车添加/修改/清空
6. 订单创建、取消、确认
7. 订单状态流转（待支付→已支付→发货→完成）
8. 多地址管理与默认地址
9. 支付模拟与支付状态同步
10. 物流信息录入与查询
11. 订单分页查询、条件搜索
12. 订单统计（日/月销量、销售额）
13. 优惠券发放与使用核销
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

	
	
	
	
	
	
	
	
	
