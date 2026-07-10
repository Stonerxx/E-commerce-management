# 发布说明

服务器上的关键路径：

```text
源码目录：/root/E-commerce-management
发布目录：/var/www/ecommerce
服务名：ecommerce
后端监听：127.0.0.1:5000
Nginx：转发公网 80 -> 127.0.0.1:5000
环境变量：/etc/ecommerce/ecommerce.env 或 ecommerce.service
```

## 一、推荐发布流程：GitHub Actions 编译，服务器只部署

现在推荐流程是：

```text
GitHub Actions restore/build/publish
-> 打包 ecommerce-release.tar.gz
-> 通过 SSH 上传到服务器 /tmp
-> 服务器解压到 /var/www/ecommerce
-> 重启 ecommerce systemd 服务
-> 检查 /health
```

服务器不再执行 `dotnet restore`、`dotnet build` 或 `dotnet publish`，只负责运行发布好的文件。

仓库里的工作流：

```text
.github/workflows/deploy.yml
```

触发方式：

```text
push 到 main
GitHub Actions 页面手动 Run workflow（选择 main）
```

`merging` 分支只触发 `.github/workflows/build.yml` 的 build/test 检查，不部署服务器。

## 二、服务器准备 artifact 部署脚本

仓库提供脚本：

```text
deployment/linux/deploy-ecommerce-artifact.sh
```

第一次在服务器上执行：

```bash
cd /root/E-commerce-management
git pull
chmod +x deployment/linux/deploy-ecommerce-artifact.sh
```

以后 GitHub Actions 会执行：

```bash
bash /root/E-commerce-management/deployment/linux/deploy-ecommerce-artifact.sh /tmp/ecommerce-release.tar.gz
```

这个脚本会：

1. 校验 tar.gz 可以解压。
2. 解压到 `/var/www/ecommerce-new`。
3. 检查 `ECommerce.Web.dll` 是否存在。
4. 停止 `ecommerce`。
5. 把旧版移动到 `/var/www/ecommerce-old`。
6. 把新版移动到 `/var/www/ecommerce`。
7. `chown -R www-data:www-data /var/www/ecommerce`。
8. 重启服务并检查 `/health`。
9. 如果启动或健康检查失败，自动回滚上一版。

如果服务器路径以后变了，可以用环境变量覆盖：

```bash
SERVICE_NAME=ecommerce \
PUBLISH_CURRENT=/var/www/ecommerce \
HEALTH_URL=http://127.0.0.1:5000/health \
bash /root/E-commerce-management/deployment/linux/deploy-ecommerce-artifact.sh /tmp/ecommerce-release.tar.gz
```

## 三、GitHub Actions 需要的 Secrets

进入：

```text
GitHub 仓库 -> Settings -> Secrets and variables -> Actions -> New repository secret
```

添加：

```text
SERVER_HOST       服务器公网 IP
SERVER_PORT       22
SERVER_USER       root
SERVER_SSH_KEY    GitHub Actions 专用 SSH 私钥全文
```

推荐生成专用 key：

```bash
ssh-keygen -t ed25519 -C "github-actions-ecommerce" -f ecommerce_deploy_key
```

把公钥追加到服务器：

```bash
mkdir -p /root/.ssh
nano /root/.ssh/authorized_keys
chmod 700 /root/.ssh
chmod 600 /root/.ssh/authorized_keys
```

`SERVER_SSH_KEY` 填 `ecommerce_deploy_key` 私钥全文，不是 `.pub` 公钥。

## 四、数据库连接仍然在服务器

GitHub Actions 只上传编译后的代码文件，不需要数据库密码。

数据库连接仍然由服务器运行时配置决定，通常在：

```text
/etc/ecommerce/ecommerce.env
```

或者：

```text
/etc/systemd/system/ecommerce.service
```

示例：

```bash
Oracle__ConnectionString=User Id=ECOMMERCE_DEV;Password=change_me;Data Source=127.0.0.1:1521/FREEPDB1;
```

答辩前如果要切 DEMO，只改服务器环境变量，然后重启服务：

```bash
systemctl restart ecommerce
```

不需要重新跑 GitHub Actions。

## 五、SSH 安全组和登录方式

GitHub Actions runner 需要能 SSH 到服务器。如果云服务器安全组只允许你自己的 IP 访问 22 端口，Actions 会连不上。

课程项目最简单做法：

```text
22 端口允许 GitHub Actions 访问
服务器禁止密码登录，只允许密钥登录
```

建议检查 `/etc/ssh/sshd_config`：

```text
PasswordAuthentication no
PubkeyAuthentication yes
PermitRootLogin prohibit-password
```

修改后：

```bash
systemctl restart ssh
```

## 六、备用：服务器本地编译发布脚本

仓库仍保留备用脚本：

```text
deployment/linux/deploy-ecommerce.sh
```

它会在服务器本地拉代码、编译、发布和重启服务。正常发布优先使用 GitHub Actions；如果 GitHub Actions 临时不可用，可以用它救急：

```bash
/root/E-commerce-management/deployment/linux/deploy-ecommerce.sh feat-member1-foundation-oracle-deploy
```

备用脚本已经改成 `build` 后 `publish --no-build`，不会再因为 publish 阶段重复编译。

## 七、日常检查命令

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

测试服务：

```bash
curl -i http://127.0.0.1:5000/
curl -i http://127.0.0.1:5000/health
curl -i http://127.0.0.1:5000/api/v1/system/db-check
```

看 Nginx：

```bash
systemctl status nginx --no-pager
```
