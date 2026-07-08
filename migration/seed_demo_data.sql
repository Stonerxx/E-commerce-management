-- ============================================================
-- 电商购物平台演示/联调用测试数据
-- 适用：先执行 migration/init_database.sql 建表后，再执行本脚本。
-- 说明：
--   1. 本脚本只使用 9000-9999 号段的显式 ID。
--   2. 每次执行前会删除同号段演示数据，因此可重复执行。
--   3. password_hash 目前是占位值；member2 完成真实登录后，需要替换为对应算法生成的密码哈希。
-- ============================================================

SET DEFINE OFF;

PROMPT Reset demo seed data...

DELETE FROM ORDER_STAT_SNAPSHOT WHERE id BETWEEN 9000 AND 9999;
DELETE FROM OPERATION_LOG WHERE id BETWEEN 9000 AND 9999;
DELETE FROM REVIEW WHERE id BETWEEN 9000 AND 9999;
DELETE FROM LOGISTICS_TRACK WHERE logistics_id BETWEEN 9000 AND 9999;
DELETE FROM LOGISTICS WHERE id BETWEEN 9000 AND 9999;
DELETE FROM PAYMENT WHERE id BETWEEN 9000 AND 9999;
DELETE FROM ORDER_LOG WHERE id BETWEEN 9000 AND 9999;
DELETE FROM ORDER_ITEM WHERE id BETWEEN 9000 AND 9999;
DELETE FROM ORDER_MAIN WHERE id BETWEEN 9000 AND 9999;
DELETE FROM USER_COUPON WHERE id BETWEEN 9000 AND 9999;
DELETE FROM COUPON_TEMPLATE WHERE id BETWEEN 9000 AND 9999;
DELETE FROM CART WHERE id BETWEEN 9000 AND 9999;
DELETE FROM INVENTORY_LOG WHERE id BETWEEN 9000 AND 9999;
DELETE FROM SKU WHERE id BETWEEN 9000 AND 9999;
DELETE FROM PRODUCT_SPEC WHERE id BETWEEN 9000 AND 9999;
DELETE FROM PRODUCT_IMAGE WHERE id BETWEEN 9000 AND 9999;
DELETE FROM PRODUCT WHERE id BETWEEN 9000 AND 9999;
DELETE FROM "CATEGORY" WHERE id BETWEEN 9000 AND 9999 AND tree_level >= 2;
DELETE FROM "CATEGORY" WHERE id BETWEEN 9000 AND 9999;
DELETE FROM ADDRESS WHERE id BETWEEN 9000 AND 9999;
DELETE FROM ROLE_PERMISSION WHERE id BETWEEN 9000 AND 9999;
DELETE FROM USER_ROLE WHERE id BETWEEN 9000 AND 9999;
DELETE FROM "PERMISSION" WHERE id BETWEEN 9000 AND 9999;
DELETE FROM "ROLE" WHERE id BETWEEN 9000 AND 9999;
DELETE FROM "USER" WHERE id BETWEEN 9000 AND 9999;

PROMPT Insert users and roles...

INSERT INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
VALUES (9001, 'demo_admin', 'DEMO_HASH_REPLACE_AFTER_AUTH', '13800009001', 'admin.demo@example.com', '/images/avatar-admin.png', 1, SYSDATE - 30, SYSDATE - 1);

INSERT INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
VALUES (9002, 'demo_service', 'DEMO_HASH_REPLACE_AFTER_AUTH', '13800009002', 'service.demo@example.com', '/images/avatar-service.png', 1, SYSDATE - 28, SYSDATE - 1);

INSERT INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
VALUES (9003, 'demo_user', 'DEMO_HASH_REPLACE_AFTER_AUTH', '13800009003', 'user.demo@example.com', '/images/avatar-user.png', 1, SYSDATE - 20, SYSDATE);

INSERT INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
VALUES (9004, 'demo_buyer', 'DEMO_HASH_REPLACE_AFTER_AUTH', '13800009004', 'buyer.demo@example.com', '/images/avatar-buyer.png', 1, SYSDATE - 12, SYSDATE - 2);

INSERT INTO "ROLE" (id, role_name, description, created_at)
VALUES (9001, 'ADMIN', '演示管理员', SYSDATE - 30);

INSERT INTO "ROLE" (id, role_name, description, created_at)
VALUES (9002, 'SERVICE', '演示客服', SYSDATE - 30);

INSERT INTO "ROLE" (id, role_name, description, created_at)
VALUES (9003, 'USER', '演示普通用户', SYSDATE - 30);

INSERT INTO "PERMISSION" (id, perm_name, resource_path, action, description)
VALUES (9001, 'ADMIN_DASHBOARD_VIEW', '/admin/**', 'QUERY', '查看后台页面');

INSERT INTO "PERMISSION" (id, perm_name, resource_path, action, description)
VALUES (9002, 'ORDER_MANAGE', '/api/v1/admin/orders/**', 'UPDATE', '管理后台订单');

INSERT INTO "PERMISSION" (id, perm_name, resource_path, action, description)
VALUES (9003, 'CUSTOMER_ORDER', '/api/v1/orders/**', 'QUERY', '查看和操作本人订单');

INSERT INTO USER_ROLE (id, user_id, role_id, assigned_at)
VALUES (9001, 9001, 9001, SYSDATE - 30);

INSERT INTO USER_ROLE (id, user_id, role_id, assigned_at)
VALUES (9002, 9002, 9002, SYSDATE - 30);

INSERT INTO USER_ROLE (id, user_id, role_id, assigned_at)
VALUES (9003, 9003, 9003, SYSDATE - 20);

INSERT INTO USER_ROLE (id, user_id, role_id, assigned_at)
VALUES (9004, 9004, 9003, SYSDATE - 12);

INSERT INTO ROLE_PERMISSION (id, role_id, permission_id, created_at)
VALUES (9001, 9001, 9001, SYSDATE - 30);

INSERT INTO ROLE_PERMISSION (id, role_id, permission_id, created_at)
VALUES (9002, 9001, 9002, SYSDATE - 30);

INSERT INTO ROLE_PERMISSION (id, role_id, permission_id, created_at)
VALUES (9003, 9001, 9003, SYSDATE - 30);

INSERT INTO ROLE_PERMISSION (id, role_id, permission_id, created_at)
VALUES (9004, 9002, 9001, SYSDATE - 30);

INSERT INTO ROLE_PERMISSION (id, role_id, permission_id, created_at)
VALUES (9005, 9002, 9002, SYSDATE - 30);

INSERT INTO ROLE_PERMISSION (id, role_id, permission_id, created_at)
VALUES (9006, 9003, 9003, SYSDATE - 30);

PROMPT Insert addresses...

INSERT INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
VALUES (9001, 9003, '演示收货人A', '13800009003', '上海市', '上海市', '浦东新区', '张江高科演示路 100 号', 1, SYSDATE - 15);

INSERT INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
VALUES (9002, 9003, '演示收货人A', '13800009003', '浙江省', '杭州市', '西湖区', '文三路测试小区 8 幢 302', 0, SYSDATE - 10);

INSERT INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
VALUES (9003, 9004, '演示收货人B', '13800009004', '广东省', '深圳市', '南山区', '科技园演示街 66 号', 1, SYSDATE - 8);

PROMPT Insert categories, products, images, specs and skus...

INSERT INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url)
VALUES (9001, NULL, '数码电子', 1, 10, 1, '/images/category-digital.png');

INSERT INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url)
VALUES (9002, 9001, '手机通讯', 2, 10, 1, '/images/category-phone.png');

INSERT INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url)
VALUES (9003, 9001, '电脑配件', 2, 20, 1, '/images/category-keyboard.png');

INSERT INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url)
VALUES (9004, NULL, '食品饮料', 1, 20, 1, '/images/category-food.png');

INSERT INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url)
VALUES (9005, 9004, '咖啡茶饮', 2, 10, 1, '/images/category-coffee.png');

INSERT INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
VALUES (9001, 9002, '星河 X1 智能手机', '演示商品：高刷屏、长续航、适合购物流程测试。', '/images/demo-phone.jpg', 1, 2999.00, 86, 1280, 4.80, SYSDATE - 18, SYSDATE - 1);

INSERT INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
VALUES (9002, 9003, '青轴机械键盘 K87', '演示商品：87 键布局，适合库存和订单明细测试。', '/images/demo-keyboard.jpg', 1, 399.00, 142, 960, 4.60, SYSDATE - 16, SYSDATE - 1);

INSERT INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
VALUES (9003, 9005, '山谷冷萃咖啡礼盒', '演示商品：组合装，用于优惠券和评价展示。', '/images/demo-coffee.jpg', 1, 129.00, 55, 410, 4.90, SYSDATE - 14, SYSDATE - 1);

INSERT INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at)
VALUES (9001, 9001, '/images/demo-phone.jpg', 1, SYSDATE - 18);

INSERT INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at)
VALUES (9002, 9002, '/images/demo-keyboard.jpg', 1, SYSDATE - 16);

INSERT INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at)
VALUES (9003, 9003, '/images/demo-coffee.jpg', 1, SYSDATE - 14);

INSERT INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order)
VALUES (9001, 9001, '颜色', '星河银', 1);

INSERT INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order)
VALUES (9002, 9001, '存储', '256GB', 2);

INSERT INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order)
VALUES (9003, 9002, '轴体', '青轴', 1);

INSERT INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order)
VALUES (9004, 9003, '规格', '12瓶装', 1);

INSERT INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
VALUES (9001, 9001, '{"颜色":"星河银","存储":"256GB"}', 3299.00, 3699.00, 80, 3, 10, '/images/demo-phone-silver.jpg', 1);

INSERT INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
VALUES (9002, 9001, '{"颜色":"深空黑","存储":"512GB"}', 3999.00, 4399.00, 45, 1, 8, '/images/demo-phone-black.jpg', 1);

INSERT INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
VALUES (9003, 9002, '{"轴体":"青轴","配列":"87键"}', 399.00, 499.00, 120, 2, 20, '/images/demo-keyboard-blue.jpg', 1);

INSERT INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
VALUES (9004, 9003, '{"规格":"12瓶装","口味":"原味"}', 129.00, 159.00, 200, 0, 30, '/images/demo-coffee-box.jpg', 1);

INSERT INTO INVENTORY_LOG (id, sku_id, change_type, change_qty, before_stock, after_stock, operator_id, ref_order_id, remark, created_at)
VALUES (9001, 9001, 'RESTOCK', 100, 0, 100, 9001, NULL, '演示数据初始化入库', SYSDATE - 18);

INSERT INTO INVENTORY_LOG (id, sku_id, change_type, change_qty, before_stock, after_stock, operator_id, ref_order_id, remark, created_at)
VALUES (9002, 9003, 'RESTOCK', 150, 0, 150, 9001, NULL, '演示数据初始化入库', SYSDATE - 16);

PROMPT Insert cart and coupons...

INSERT INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at)
VALUES (9001, 9003, 9001, 1, 1, SYSDATE - 2, SYSDATE - 1);

INSERT INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at)
VALUES (9002, 9003, 9003, 2, 1, SYSDATE - 2, SYSDATE - 1);

INSERT INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at)
VALUES (9003, 9004, 9004, 1, 0, SYSDATE - 3, SYSDATE - 2);

INSERT INTO COUPON_TEMPLATE (id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
VALUES (9001, '满 500 减 50 演示券', 1, 50.00, 500.00, 1000, 2, SYSDATE - 30, SYSDATE + 60, 1);

INSERT INTO COUPON_TEMPLATE (id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
VALUES (9002, '咖啡礼盒 85 折券', 2, 0.85, 100.00, 500, 1, SYSDATE - 30, SYSDATE + 60, 1);

INSERT INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
VALUES (9001, 9003, 9001, 0, SYSDATE - 5, NULL, NULL);

INSERT INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
VALUES (9002, 9003, 9001, 1, SYSDATE - 12, SYSDATE - 8, NULL);

INSERT INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
VALUES (9003, 9004, 9002, 0, SYSDATE - 4, NULL, NULL);

PROMPT Insert orders, payments, logistics and reviews...

INSERT INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
VALUES (9001, 'DEMO202607080001', 9003, 9001, NULL, 0, 3299.00, 0.00, 3299.00, SYSDATE + 1, '{"receiverName":"演示收货人A","receiverPhone":"13800009003","province":"上海市","city":"上海市","district":"浦东新区","detailAddress":"张江高科演示路 100 号"}', '待支付演示订单', SYSDATE - 1, SYSDATE - 1);

INSERT INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
VALUES (9002, 'DEMO202607080002', 9003, 9001, 9002, 1, 798.00, 50.00, 748.00, SYSDATE - 7, '{"receiverName":"演示收货人A","receiverPhone":"13800009003","province":"上海市","city":"上海市","district":"浦东新区","detailAddress":"张江高科演示路 100 号"}', '已支付待发货演示订单', SYSDATE - 8, SYSDATE - 8);

INSERT INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
VALUES (9003, 'DEMO202607080003', 9003, 9002, NULL, 2, 3999.00, 0.00, 3999.00, SYSDATE - 5, '{"receiverName":"演示收货人A","receiverPhone":"13800009003","province":"浙江省","city":"杭州市","district":"西湖区","detailAddress":"文三路测试小区 8 幢 302"}', '已发货演示订单', SYSDATE - 6, SYSDATE - 5);

INSERT INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
VALUES (9004, 'DEMO202607080004', 9004, 9003, NULL, 3, 129.00, 0.00, 129.00, SYSDATE - 12, '{"receiverName":"演示收货人B","receiverPhone":"13800009004","province":"广东省","city":"深圳市","district":"南山区","detailAddress":"科技园演示街 66 号"}', '已完成演示订单', SYSDATE - 13, SYSDATE - 10);

INSERT INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
VALUES (9005, 'DEMO202607080005', 9004, 9003, NULL, 4, 3299.00, 0.00, 3299.00, SYSDATE - 15, '{"receiverName":"演示收货人B","receiverPhone":"13800009004","province":"广东省","city":"深圳市","district":"南山区","detailAddress":"科技园演示街 66 号"}', '已取消演示订单', SYSDATE - 16, SYSDATE - 15);

UPDATE USER_COUPON SET order_id = 9002 WHERE id = 9002;

INSERT INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
VALUES (9001, 9001, 9001, '星河 X1 智能手机', '{"颜色":"星河银","存储":"256GB"}', '/images/demo-phone-silver.jpg', 3299.00, 1, 3299.00);

INSERT INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
VALUES (9002, 9002, 9003, '青轴机械键盘 K87', '{"轴体":"青轴","配列":"87键"}', '/images/demo-keyboard-blue.jpg', 399.00, 2, 798.00);

INSERT INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
VALUES (9003, 9003, 9002, '星河 X1 智能手机', '{"颜色":"深空黑","存储":"512GB"}', '/images/demo-phone-black.jpg', 3999.00, 1, 3999.00);

INSERT INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
VALUES (9004, 9004, 9004, '山谷冷萃咖啡礼盒', '{"规格":"12瓶装","口味":"原味"}', '/images/demo-coffee-box.jpg', 129.00, 1, 129.00);

INSERT INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal)
VALUES (9005, 9005, 9001, '星河 X1 智能手机', '{"颜色":"星河银","存储":"256GB"}', '/images/demo-phone-silver.jpg', 3299.00, 1, 3299.00);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9001, 9001, NULL, 0, 9003, 'demo_user', '用户创建订单', SYSDATE - 1);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9002, 9002, NULL, 0, 9003, 'demo_user', '用户创建订单', SYSDATE - 8);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9003, 9002, 0, 1, 9003, 'demo_user', '模拟支付成功', SYSDATE - 8 + 1 / 24);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9004, 9003, NULL, 0, 9003, 'demo_user', '用户创建订单', SYSDATE - 6);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9005, 9003, 0, 1, 9003, 'demo_user', '模拟支付成功', SYSDATE - 6 + 1 / 24);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9006, 9003, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 5);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9007, 9004, NULL, 0, 9004, 'demo_buyer', '用户创建订单', SYSDATE - 13);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9008, 9004, 0, 1, 9004, 'demo_buyer', '模拟支付成功', SYSDATE - 13 + 1 / 24);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9009, 9004, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 12);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9010, 9004, 2, 3, 9004, 'demo_buyer', '用户确认收货', SYSDATE - 10);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9011, 9005, NULL, 0, 9004, 'demo_buyer', '用户创建订单', SYSDATE - 16);

INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
VALUES (9012, 9005, 0, 4, 9004, 'demo_buyer', '用户取消订单', SYSDATE - 15);

INSERT INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data)
VALUES (9001, 9002, '模拟支付', 1, 'DEMO-PAY-9002', 748.00, SYSDATE - 8 + 1 / 24, '{"channel":"demo","status":"success"}');

INSERT INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data)
VALUES (9002, 9003, '模拟支付', 1, 'DEMO-PAY-9003', 3999.00, SYSDATE - 6 + 1 / 24, '{"channel":"demo","status":"success"}');

INSERT INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data)
VALUES (9003, 9004, '模拟支付', 1, 'DEMO-PAY-9004', 129.00, SYSDATE - 13 + 1 / 24, '{"channel":"demo","status":"success"}');

INSERT INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status)
VALUES (9001, 9003, '顺丰速运', 'SFDEMO9003', SYSDATE - 5, 1);

INSERT INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status)
VALUES (9002, 9004, '京东物流', 'JDDEMO9004', SYSDATE - 12, 3);

INSERT INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location)
VALUES (9001, 9001, '包裹已由商家交付物流', SYSDATE - 5, '上海仓');

INSERT INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location)
VALUES (9002, 9001, '运输中，下一站杭州分拨中心', SYSDATE - 4, '嘉兴转运中心');

INSERT INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location)
VALUES (9003, 9002, '包裹已签收，签收人：本人', SYSDATE - 10, '深圳南山');

INSERT INTO REVIEW (id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
VALUES (9001, 9004, 9003, 9004, 5, '冷萃咖啡礼盒包装完整，适合作为已完成订单和评价演示数据。', '[]', 0, 1, SYSDATE - 9);

INSERT INTO OPERATION_LOG (id, operator_id, operator_name, module, action, description, ip_address, request_params, result, created_at)
VALUES (9001, 9002, 'demo_service', '订单管理', '发货', '客服为演示订单 DEMO202607080003 创建物流信息', '127.0.0.1', '{"orderId":9003}', 1, SYSDATE - 5);

INSERT INTO OPERATION_LOG (id, operator_id, operator_name, module, action, description, ip_address, request_params, result, created_at)
VALUES (9002, 9001, 'demo_admin', '商品管理', '初始化演示数据', '管理员初始化商品、SKU、库存和订单演示数据', '127.0.0.1', '{}', 1, SYSDATE - 1);

PROMPT Insert statistics snapshots...

INSERT INTO ORDER_STAT_SNAPSHOT (id, stat_date, order_count, paid_count, sales_amount, refund_amount, avg_order_amount, new_user_count)
VALUES (9001, TRUNC(SYSDATE) - 8, 1, 1, 748.00, 0.00, 748.00, 0);

INSERT INTO ORDER_STAT_SNAPSHOT (id, stat_date, order_count, paid_count, sales_amount, refund_amount, avg_order_amount, new_user_count)
VALUES (9002, TRUNC(SYSDATE) - 6, 1, 1, 3999.00, 0.00, 3999.00, 0);

INSERT INTO ORDER_STAT_SNAPSHOT (id, stat_date, order_count, paid_count, sales_amount, refund_amount, avg_order_amount, new_user_count)
VALUES (9003, TRUNC(SYSDATE) - 1, 1, 0, 0.00, 0.00, 0.00, 0);

COMMIT;

PROMPT Demo seed data inserted successfully.
