新版本代码部署发布：

服务器上的关键路径：

```text
源码目录：/root/E-commerce-management
发布目录：/var/www/ecommerce
服务名：ecommerce
Web 项目：src/ECommerce.Web/ECommerce.Web.csproj
解决方案：ECommerce.sln
后端监听：127.0.0.1:5000
Nginx：转发公网 80 → 127.0.0.1:5000
```

## 一、最常用的发布流程

如果要部署当前分支的新代码：

```bash
cd /root/E-commerce-management

git status
git pull

dotnet restore ECommerce.sln
dotnet build ECommerce.sln -c Release

rm -rf /var/www/ecommerce-new
mkdir -p /var/www/ecommerce-new

dotnet publish src/ECommerce.Web/ECommerce.Web.csproj \
  -c Release \
  -o /var/www/ecommerce-new

systemctl stop ecommerce

rm -rf /var/www/ecommerce-old
mv /var/www/ecommerce /var/www/ecommerce-old
mv /var/www/ecommerce-new /var/www/ecommerce

chown -R www-data:www-data /var/www/ecommerce

systemctl start ecommerce
systemctl status ecommerce --no-pager
```

然后测试：

```bash
curl -i http://127.0.0.1:5000/
curl -i http://127.0.0.1:5000/health
```

浏览器访问：

```text
http://服务器IP/
```

如果网页正常，新代码就部署完成了。

## 二、如果要切换到某个分支再部署

比如要部署 `member1` 分支或者别人的功能分支：

```bash
cd /root/E-commerce-management

git fetch --all --prune
git branch -a
```

看到目标分支后切换：

```bash
git switch 分支名
git pull --ff-only
```

如果是第一次切远程分支，比如：

```bash
git switch --track origin/feat-member1-foundation-oracle-deploy
```

然后再走发布流程：

```bash
dotnet restore ECommerce.sln
dotnet build ECommerce.sln -c Release

rm -rf /var/www/ecommerce-new
mkdir -p /var/www/ecommerce-new

dotnet publish src/ECommerce.Web/ECommerce.Web.csproj \
  -c Release \
  -o /var/www/ecommerce-new

systemctl stop ecommerce

rm -rf /var/www/ecommerce-old
mv /var/www/ecommerce /var/www/ecommerce-old
mv /var/www/ecommerce-new /var/www/ecommerce

chown -R www-data:www-data /var/www/ecommerce

systemctl start ecommerce
systemctl status ecommerce --no-pager
```

## 三、如果发布后挂了，怎么回滚

如果新代码启动失败，先看日志：

```bash
journalctl -u ecommerce -n 100 --no-pager
```

如果短时间内修不了，直接回滚上一版：

```bash
systemctl stop ecommerce

rm -rf /var/www/ecommerce-bad
mv /var/www/ecommerce /var/www/ecommerce-bad
mv /var/www/ecommerce-old /var/www/ecommerce

chown -R www-data:www-data /var/www/ecommerce

systemctl start ecommerce
systemctl status ecommerce --no-pager
```

这样至少网站能恢复到上一版。

## 四、封装好的一键发布脚本

仓库已经提供可重复运行的发布脚本：

```text
deployment/linux/deploy-ecommerce.sh
```

第一次在服务器上拉到脚本后，给它执行权限：

```bash
cd /root/E-commerce-management
chmod +x deployment/linux/deploy-ecommerce.sh
ln -sf /root/E-commerce-management/deployment/linux/deploy-ecommerce.sh /root/deploy-ecommerce.sh
```

以后发布当前分支新代码：

```bash
/root/deploy-ecommerce.sh
```

发布指定分支：

```bash
/root/deploy-ecommerce.sh feat-member1-foundation-oracle-deploy
```

发布前顺便跑测试：

```bash
RUN_TESTS=1 /root/deploy-ecommerce.sh
```

如果刚发布的新版本有问题，回滚到上一版：

```bash
/root/deploy-ecommerce.sh --rollback
```

脚本做的事情：

1. 检查 Git 工作区是否干净。
2. 拉取当前分支或指定分支最新代码。
3. `dotnet restore`、`dotnet build -c Release`。
4. 发布到 `/var/www/ecommerce-new`。
5. 停止 `ecommerce` 服务。
6. 把旧版本移到 `/var/www/ecommerce-old`。
7. 把新版本切到 `/var/www/ecommerce`。
8. 启动服务并检查 `/health`。
9. 如果启动或健康检查失败，自动回滚上一版。

如果服务器路径以后变了，不用改脚本，可以用环境变量覆盖：

```bash
PROJECT_DIR=/root/E-commerce-management \
PUBLISH_CURRENT=/var/www/ecommerce \
SERVICE_NAME=ecommerce \
HEALTH_URL=http://127.0.0.1:5000/health \
/root/deploy-ecommerce.sh
```

## 五、数据库变更不要混在普通发布里

普通代码发布不会重置数据库。
只有当后端同学改了表结构，比如新增字段、改表名、加触发器、加存储过程，才需要额外执行 SQL。

建议规则：

```text
普通代码更新：
git pull → build → publish → restart

数据库结构更新：
先备份/确认
再由你执行 migration SQL
不要让每个人随便执行 init_database.sql
```

尤其不要随便重新跑完整 `init_database.sql`，因为它如果包含 `DROP TABLE`，会把大家测试数据清掉。

## 六、日常检查命令

看服务状态：

```bash
systemctl status ecommerce --no-pager
```

看后端日志：

```bash
journalctl -u ecommerce -n 100 --no-pager
```

实时看日志：

```bash
journalctl -u ecommerce -f
```

看后端端口：

```bash
ss -lntp | grep 5000
```

看 Nginx 状态：

```bash
systemctl status nginx --no-pager
```

看 Oracle 容器：

```bash
docker ps
```

看 Oracle 表：

```bash
docker exec -it oracle-free bash -lc 'sqlplus ECOMMERCE_DEV/"我们的开发库密码"@localhost:1521/FREEPDB1'
```

进去后：

```sql
SELECT COUNT(*) FROM USER_TABLES;
EXIT;
```

## 七、以后发布时记住

**先 build，后 publish；先发布到新目录，再停服务替换；出问题就看 journalctl，必要时回滚 `/var/www/ecommerce-old`。**
