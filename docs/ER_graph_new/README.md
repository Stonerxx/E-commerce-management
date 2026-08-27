# ER_graph_new — 调整后的 ER 图

本目录存放根据 `migration/` 建库 SQL 调整后的 ER 图。

## 调整内容

| 文件 | 调整 |
|---|---|
| `2.1.drawio` | 「商品评价」实体补充 `商品ID` 属性（原概念图漏画；3.1 物理图已有，建库 SQL `REVIEW` 表含 `product_id`） |
| `2.1new.png` | 由 `2.1.drawio` 重新导出的 PNG（当前生效的图，文档 `figures/2.1.png` 指向它） |
| `3.1.drawio` | 「优惠券模板」实体补充 `适用分类ID` 属性，并补 `优惠券模板→商品分类` 外键关系线（对应建库 SQL 的 `COUPON_TEMPLATE.applicable_category_id`，`fk_coup_category → CATEGORY(id)`，NULL 表示全场通用） |

其余 ER 图（2.2~2.5、3.1）经核对与建库脚本一致，无需调整。

> 概念图 2.1 的「用户-角色」「角色-权限」画成直接 M:N（未展开 `USER_ROLE`/`ROLE_PERMISSION` 中间表），属概念模型的标准画法；物理模型 3.1 已完整展开这两张表。

## 重新导出 PNG 的方法（需要 drawio）

1. 打开 https://app.diagrams.net （网页版，免安装；或用 draw.io 桌面版）
2. 打开本目录的 `2.1.drawio`
3. 确认「商品评价」实体中已出现 `商品ID`
4. 菜单 File → Export As → PNG，缩放(Zoom)建议 200%，导出并命名为 `2.1new.png`（文档的 `figures/2.1.png` 符号链接已指向本文件，重新编译文档即生效）
