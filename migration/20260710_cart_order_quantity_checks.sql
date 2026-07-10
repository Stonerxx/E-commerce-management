-- 为已初始化的 Oracle 数据库补充数量正值约束。
-- 执行前请先处理已有的 quantity <= 0 脏数据，否则 Oracle 会拒绝添加约束。

ALTER TABLE CART
    ADD CONSTRAINT ch_cart_quantity CHECK (quantity > 0);

ALTER TABLE ORDER_ITEM
    ADD CONSTRAINT ch_order_item_quantity CHECK (quantity > 0);
