-- ============================================================
-- 电商平台 Oracle 数据库对象 (Oracle 18c+)
-- 执行顺序：init_database.sql -> database_objects.sql -> seed_demo_data.sql
-- 本脚本可重复执行；过程内不提交事务，由应用层统一提交或回滚。
-- ============================================================

-- 1. 返回 SKU 的可用库存；不存在的 SKU 返回 NULL。
CREATE OR REPLACE FUNCTION FN_AVAILABLE_STOCK(
    p_sku_id IN NUMBER
) RETURN NUMBER
IS
    v_available_stock NUMBER;
BEGIN
    SELECT stock - locked_stock
    INTO v_available_stock
    FROM SKU
    WHERE id = p_sku_id;

    RETURN v_available_stock;
EXCEPTION
    WHEN NO_DATA_FOUND THEN
        RETURN NULL;
END;
/
SHOW ERRORS FUNCTION FN_AVAILABLE_STOCK;

-- 2. 刷新指定自然日的订单统计快照。
CREATE OR REPLACE PROCEDURE SP_REFRESH_ORDER_STAT_SNAPSHOT(
    p_stat_date IN DATE
)
IS
    v_stat_date DATE;
BEGIN
    IF p_stat_date IS NULL THEN
        RAISE_APPLICATION_ERROR(-20001, 'p_stat_date must not be null');
    END IF;

    v_stat_date := TRUNC(p_stat_date);

    MERGE INTO ORDER_STAT_SNAPSHOT target
    USING (
        SELECT
            v_stat_date AS stat_date,
            (SELECT COUNT(1) FROM ORDER_MAIN om
             WHERE om.created_at >= v_stat_date AND om.created_at < v_stat_date + 1) AS order_count,
            (SELECT COUNT(1) FROM ORDER_MAIN om
             WHERE om.created_at >= v_stat_date AND om.created_at < v_stat_date + 1
               AND om.status IN (1, 2, 3)) AS paid_count,
            (SELECT NVL(SUM(om.pay_amount), 0) FROM ORDER_MAIN om
             WHERE om.created_at >= v_stat_date AND om.created_at < v_stat_date + 1
               AND om.status IN (1, 2, 3)) AS sales_amount,
            (SELECT COUNT(1) FROM "USER" u
             WHERE u.created_at >= v_stat_date AND u.created_at < v_stat_date + 1) AS new_user_count
        FROM DUAL
    ) source
    ON (target.stat_date = source.stat_date)
    WHEN MATCHED THEN UPDATE SET
        target.order_count = source.order_count,
        target.paid_count = source.paid_count,
        target.sales_amount = source.sales_amount,
        target.refund_amount = 0,
        target.avg_order_amount = CASE
            WHEN source.paid_count = 0 THEN 0
            ELSE ROUND(source.sales_amount / source.paid_count, 2)
        END,
        target.new_user_count = source.new_user_count
    WHEN NOT MATCHED THEN INSERT
        (stat_date, order_count, paid_count, sales_amount, refund_amount, avg_order_amount, new_user_count)
    VALUES
        (source.stat_date, source.order_count, source.paid_count, source.sales_amount, 0,
         CASE WHEN source.paid_count = 0 THEN 0 ELSE ROUND(source.sales_amount / source.paid_count, 2) END,
         source.new_user_count);
END;
/
SHOW ERRORS PROCEDURE SP_REFRESH_ORDER_STAT_SNAPSHOT;

-- 3. SKU 库存报表视图；一行表示一个 SKU，供库存预警查询使用。
CREATE OR REPLACE VIEW V_PRODUCT_INVENTORY AS
SELECT
    inventory.product_id,
    inventory.product_name,
    inventory.category_name,
    inventory.product_status,
    inventory.sku_id,
    inventory.spec_desc,
    inventory.price,
    inventory.stock,
    inventory.locked_stock,
    inventory.available_stock,
    inventory.warning_stock,
    inventory.sku_status,
    CASE
        WHEN inventory.available_stock <= inventory.warning_stock THEN 1
        ELSE 0
    END AS is_warning
FROM (
    SELECT
        p.id AS product_id,
        p.name AS product_name,
        c.name AS category_name,
        p.status AS product_status,
        s.id AS sku_id,
        s.spec_desc,
        s.price,
        s.stock,
        s.locked_stock,
        FN_AVAILABLE_STOCK(s.id) AS available_stock,
        s.warning_stock,
        s.status AS sku_status
    FROM PRODUCT p
    INNER JOIN "CATEGORY" c ON c.id = p.category_id
    INNER JOIN SKU s ON s.product_id = p.id
) inventory;
/
SHOW ERRORS VIEW V_PRODUCT_INVENTORY;

-- 4. 订单导出视图；一行表示一个订单，避免 ORDER_ITEM 连接造成重复行。
CREATE OR REPLACE VIEW V_ORDER_REPORT AS
SELECT
    om.id AS order_id,
    om.order_no,
    om.user_id,
    u.username,
    om.status AS order_status,
    om.total_amount,
    om.discount_amount,
    om.pay_amount,
    om.created_at,
    om.updated_at,
    p.id AS payment_id,
    p.status AS payment_status,
    p.paid_at AS payment_time,
    p.pay_method,
    l.id AS logistics_id,
    l.company_name AS logistics_company_name,
    l.tracking_no,
    l.status AS logistics_status,
    (SELECT COUNT(1) FROM ORDER_ITEM oi WHERE oi.order_id = om.id) AS item_count,
    (SELECT NVL(SUM(oi.quantity), 0) FROM ORDER_ITEM oi WHERE oi.order_id = om.id) AS item_quantity
FROM ORDER_MAIN om
INNER JOIN "USER" u ON u.id = om.user_id
LEFT JOIN PAYMENT p ON p.order_id = om.id
LEFT JOIN LOGISTICS l ON l.order_id = om.id;
/
SHOW ERRORS VIEW V_ORDER_REPORT;

-- 5. 订单由待支付变为已支付时，按订单明细自动累计商品销量。
CREATE OR REPLACE TRIGGER TRG_ORDER_PAID_UPDATE_SALES
AFTER UPDATE OF status ON ORDER_MAIN
FOR EACH ROW
WHEN (OLD.status = 0 AND NEW.status = 1)
BEGIN
    FOR sales IN (
        SELECT
            s.product_id,
            SUM(oi.quantity) AS sold_quantity
        FROM ORDER_ITEM oi
        INNER JOIN SKU s ON s.id = oi.sku_id
        WHERE oi.order_id = :NEW.id
        GROUP BY s.product_id
    ) LOOP
        UPDATE PRODUCT
        SET sales_count = sales_count + sales.sold_quantity,
            updated_at = SYSDATE
        WHERE id = sales.product_id;
    END LOOP;
END;
/
SHOW ERRORS TRIGGER TRG_ORDER_PAID_UPDATE_SALES;
