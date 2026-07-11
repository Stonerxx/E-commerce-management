-- ============================================================
-- 电商购物平台演示/联调用测试数据
-- 执行顺序：init_database.sql -> database_objects.sql -> seed_demo_data.sql
-- 说明：
--   1. 本脚本只使用 9000-9999 号段的显式 ID，可重复执行。
--   2. 任意 SQL 或跨表自校验失败时，整批种子数据都会回滚。
--   3. 演示账号密码统一为 demo123；其他客户账号仅用于列表和报表数据。
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
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

PROMPT Insert users, roles and permissions...

INSERT ALL
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9001, 'demo_admin', 'PBKDF2$100000$pUhcA/f6O5DV7JUYBopvrQ==$L1CqQo16vrK86twS/rw3qnU3ufSQJe7FeJLw5E7TX5s=', '13800009001', 'admin.demo@example.com', '/images/avatar-admin.png', 1, SYSDATE - 30, SYSDATE - 1)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9002, 'demo_service', 'PBKDF2$100000$2Hi1fmr81JAugjLWW/E4kA==$iKPwX3VM1cx0SxC5j3Y9B2zfJvFgQvLaAVFHSA13or8=', '13800009002', 'service.demo@example.com', '/images/avatar-service.png', 1, SYSDATE - 28, SYSDATE - 1)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9003, 'demo_user', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009003', 'user.demo@example.com', '/images/avatar-user.png', 1, SYSDATE - 20, SYSDATE)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9004, 'demo_buyer', 'PBKDF2$100000$ZOFDALJPuuqvM948nOjqNA==$QhuqKuzO/awOhDxrwBlq0j5zGd2YA5jmhkxzkBOpJ4s=', '13800009004', 'buyer.demo@example.com', '/images/avatar-buyer.png', 1, SYSDATE - 12, SYSDATE - 2)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9005, 'demo_chen', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009005', 'chen.demo@example.com', '/images/avatar-chen.png', 1, SYSDATE - 18, SYSDATE - 3)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9006, 'demo_lin', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009006', 'lin.demo@example.com', '/images/avatar-lin.png', 1, SYSDATE - 16, SYSDATE - 5)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9007, 'demo_wang', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009007', 'wang.demo@example.com', '/images/avatar-wang.png', 1, SYSDATE - 14, SYSDATE - 4)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9008, 'demo_zhang', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009008', 'zhang.demo@example.com', '/images/avatar-zhang.png', 1, SYSDATE - 11, SYSDATE - 1)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9009, 'demo_li', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009009', 'li.demo@example.com', '/images/avatar-li.png', 1, SYSDATE - 9, SYSDATE - 2)
    INTO "USER" (id, username, password_hash, phone, email, avatar_url, status, created_at, last_login_at)
    VALUES (9010, 'demo_zhao', 'PBKDF2$100000$D/H7Y0vVnmA5q4VAvyslcg==$y96YJhA5X1raFyxaE773Ulg1xIItsuWr80YeDmCPZ7w=', '13800009010', 'zhao.demo@example.com', '/images/avatar-zhao.png', 1, SYSDATE - 7, SYSDATE - 1)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO "ROLE" (id, role_name, description, created_at) VALUES (9001, 'ADMIN', '演示管理员', SYSDATE - 30)
    INTO "ROLE" (id, role_name, description, created_at) VALUES (9002, 'SERVICE', '演示客服', SYSDATE - 30)
    INTO "ROLE" (id, role_name, description, created_at) VALUES (9003, 'USER', '演示普通用户', SYSDATE - 30)
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9001, 'ADMIN_UI_GET', '/admin/**', 'GET', '后台页面访问')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9002, 'ADMIN_API_GET', '/api/v1/admin/**', 'GET', '后台 API 查询兜底规则')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9003, 'ADMIN_API_POST', '/api/v1/admin/**', 'POST', '后台 API 新增兜底规则')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9004, 'ADMIN_API_PUT', '/api/v1/admin/**', 'PUT', '后台 API 修改兜底规则')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9005, 'ADMIN_API_DELETE', '/api/v1/admin/**', 'DELETE', '后台 API 删除兜底规则')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9006, 'SERVICE_DASHBOARD_GET', '/api/v1/admin/dashboard/**', 'GET', '客服查看后台仪表盘')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9007, 'SERVICE_ORDERS_GET', '/api/v1/admin/orders/**', 'GET', '客服查看后台订单')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9008, 'SERVICE_SHIPMENT_POST', '/api/v1/admin/orders/*/shipments', 'POST', '客服发货')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9009, 'SERVICE_LOGISTICS_TRACK_POST', '/api/v1/admin/logistics/*/tracks', 'POST', '客服追加物流轨迹')
    INTO "PERMISSION" (id, perm_name, resource_path, action, description) VALUES (9010, 'CUSTOMER_ORDER_GET', '/api/v1/orders/**', 'GET', '用户查看本人订单')
SELECT 1 FROM DUAL;

INSERT ALL
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9001, 9001, 9001, SYSDATE - 30)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9002, 9002, 9002, SYSDATE - 30)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9003, 9003, 9003, SYSDATE - 20)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9004, 9004, 9003, SYSDATE - 12)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9005, 9005, 9003, SYSDATE - 18)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9006, 9006, 9003, SYSDATE - 16)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9007, 9007, 9003, SYSDATE - 14)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9008, 9008, 9003, SYSDATE - 11)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9009, 9009, 9003, SYSDATE - 9)
    INTO USER_ROLE (id, user_id, role_id, assigned_at) VALUES (9010, 9010, 9003, SYSDATE - 7)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9001, 9001, 9001, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9002, 9001, 9002, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9003, 9001, 9003, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9004, 9001, 9004, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9005, 9001, 9005, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9006, 9001, 9006, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9007, 9001, 9007, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9008, 9001, 9008, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9009, 9001, 9009, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9010, 9002, 9001, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9011, 9002, 9006, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9012, 9002, 9007, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9013, 9002, 9008, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9014, 9002, 9009, SYSDATE - 30)
    INTO ROLE_PERMISSION (id, role_id, permission_id, created_at) VALUES (9015, 9003, 9010, SYSDATE - 30)
SELECT 1 FROM DUAL;

PROMPT Insert addresses, catalog and inventory...

INSERT ALL
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9001, 9003, '演示收货人A', '13800009003', '上海市', '上海市', '浦东新区', '张江高科演示路100号', 1, SYSDATE - 15)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9002, 9003, '演示收货人A', '13800009003', '浙江省', '杭州市', '西湖区', '文三路测试小区8幢302', 0, SYSDATE - 10)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9003, 9004, '演示收货人B', '13800009004', '广东省', '深圳市', '南山区', '科技园演示街66号', 1, SYSDATE - 8)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9004, 9005, '陈晨', '13800009005', '北京市', '北京市', '朝阳区', '望京街道88号', 1, SYSDATE - 15)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9005, 9006, '林晓', '13800009006', '四川省', '成都市', '武侯区', '天府大道199号', 1, SYSDATE - 13)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9006, 9007, '王楠', '13800009007', '湖北省', '武汉市', '洪山区', '珞喻路299号', 1, SYSDATE - 12)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9007, 9008, '张宁', '13800009008', '江苏省', '南京市', '建邺区', '江东中路66号', 1, SYSDATE - 10)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9008, 9009, '李佳', '13800009009', '陕西省', '西安市', '雁塔区', '科技路128号', 1, SYSDATE - 8)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9009, 9010, '赵阳', '13800009010', '福建省', '厦门市', '思明区', '环岛东路36号', 1, SYSDATE - 6)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9020, 9003, '演示用户', '13800009003', '上海市', '上海市', '浦东新区', '世纪大道200号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9021, 9004, '演示买家', '13800009004', '广东省', '深圳市', '南山区', '深南大道88号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9022, 9005, '陈晨', '13800009005', '北京市', '北京市', '朝阳区', '阜通东大街6号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9023, 9006, '林晓', '13800009006', '四川省', '成都市', '武侯区', '交子大道177号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9024, 9007, '王楠', '13800009007', '湖北省', '武汉市', '洪山区', '关山大道9号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9025, 9008, '张宁', '13800009008', '江苏省', '南京市', '建邺区', '河西大街18号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9026, 9009, '李佳', '13800009009', '陕西省', '西安市', '雁塔区', '丈八北路12号', 0, SYSDATE - 5)
    INTO ADDRESS (id, user_id, receiver_name, receiver_phone, province, city, district, detail_address, is_default, created_at)
    VALUES (9027, 9010, '赵阳', '13800009010', '福建省', '厦门市', '思明区', '鹭江道18号', 0, SYSDATE - 5)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9001, NULL, '数码电子', 1, 10, 1, '/images/category-digital.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9002, 9001, '手机通讯', 2, 10, 1, '/images/category-phone.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9003, 9001, '电脑配件', 2, 20, 1, '/images/category-computer.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9004, NULL, '食品饮料', 1, 20, 1, '/images/category-food.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9005, 9004, '咖啡茶饮', 2, 10, 1, '/images/category-coffee.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9006, NULL, '居家生活', 1, 30, 1, '/images/category-home.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9007, 9006, '家居日用', 2, 10, 1, '/images/category-daily.png')
    INTO "CATEGORY" (id, parent_id, name, tree_level, sort_order, status, icon_url) VALUES (9008, 9006, '收纳出行', 2, 20, 1, '/images/category-travel.png')
SELECT 1 FROM DUAL;

INSERT ALL
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9001, 9002, '星河 X1 智能手机', '高刷屏、长续航，覆盖手机下单与多规格展示。', '/images/demo-phone.jpg', 1, 0, 0, 1280, 0, SYSDATE - 30, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9002, 9003, '青轴机械键盘 K87', '87键机械键盘，覆盖库存、评价和订单明细。', '/images/demo-keyboard.jpg', 1, 0, 0, 960, 0, SYSDATE - 28, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9003, 9005, '山谷冷萃咖啡礼盒', '组合装冷萃咖啡，覆盖低库存预警和已完成评价。', '/images/demo-coffee.jpg', 1, 0, 0, 820, 0, SYSDATE - 26, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9004, 9003, '云听降噪耳机', '头戴式主动降噪耳机。', '/images/demo-headphone.jpg', 1, 0, 0, 650, 0, SYSDATE - 24, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9005, 9003, '星幕 27英寸显示器', '2K高刷显示器，适合发货和物流演示。', '/images/demo-monitor.jpg', 1, 0, 0, 540, 0, SYSDATE - 22, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9006, 9007, '轻量保温水杯', '日常通勤水杯。', '/images/demo-bottle.jpg', 1, 0, 0, 430, 0, SYSDATE - 20, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9007, 9008, '城市通勤双肩包', '通勤与短途出行双肩包。', '/images/demo-backpack.jpg', 1, 0, 0, 390, 0, SYSDATE - 18, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9008, 9007, '暖光阅读台灯', '三档调光阅读台灯。', '/images/demo-lamp.jpg', 1, 0, 0, 360, 0, SYSDATE - 16, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9009, 9005, '桂花乌龙茶礼盒', '十二泡茶礼盒。', '/images/demo-tea.jpg', 1, 0, 0, 310, 0, SYSDATE - 14, SYSDATE - 1)
    INTO PRODUCT (id, category_id, name, description, main_image, status, price_min, sales_count, view_count, avg_rating, created_at, updated_at)
    VALUES (9010, 9003, '极光无线鼠标', '预售商品，用于前台预售状态展示。', '/images/demo-mouse.jpg', 2, 0, 0, 290, 0, SYSDATE - 8, SYSDATE - 1)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9001, 9001, '/images/demo-phone.jpg', 1, SYSDATE - 30)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9002, 9002, '/images/demo-keyboard.jpg', 1, SYSDATE - 28)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9003, 9003, '/images/demo-coffee.jpg', 1, SYSDATE - 26)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9004, 9004, '/images/demo-headphone.jpg', 1, SYSDATE - 24)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9005, 9005, '/images/demo-monitor.jpg', 1, SYSDATE - 22)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9006, 9006, '/images/demo-bottle.jpg', 1, SYSDATE - 20)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9007, 9007, '/images/demo-backpack.jpg', 1, SYSDATE - 18)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9008, 9008, '/images/demo-lamp.jpg', 1, SYSDATE - 16)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9009, 9009, '/images/demo-tea.jpg', 1, SYSDATE - 14)
    INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at) VALUES (9010, 9010, '/images/demo-mouse.jpg', 1, SYSDATE - 8)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9001, 9001, '颜色', '星河银', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9002, 9001, '存储', '256GB', 2)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9003, 9002, '轴体', '青轴', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9004, 9003, '规格', '12瓶装', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9005, 9004, '颜色', '曜石黑', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9006, 9005, '分辨率', '2K', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9007, 9006, '容量', '500ml', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9008, 9007, '颜色', '深灰', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9009, 9008, '光源', '暖光', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9010, 9009, '口味', '桂花乌龙', 1)
    INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order) VALUES (9011, 9010, '连接方式', '蓝牙', 1)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9001, 9001, '{"颜色":"星河银","存储":"256GB"}', 3299.00, 3699.00, 80, 0, 10, '/images/demo-phone-silver.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9002, 9001, '{"颜色":"深空黑","存储":"512GB"}', 3999.00, 4399.00, 45, 0, 8, '/images/demo-phone-black.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9003, 9002, '{"轴体":"青轴","配列":"87键"}', 399.00, 499.00, 120, 0, 20, '/images/demo-keyboard-blue.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9004, 9002, '{"轴体":"红轴","配列":"87键"}', 459.00, 529.00, 75, 0, 15, '/images/demo-keyboard-red.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9005, 9003, '{"规格":"12瓶装","口味":"原味"}', 129.00, 159.00, 26, 0, 30, '/images/demo-coffee-original.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9006, 9003, '{"规格":"12瓶装","口味":"低糖"}', 149.00, 169.00, 22, 0, 25, '/images/demo-coffee-low-sugar.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9007, 9004, '{"颜色":"曜石黑","版本":"标准"}', 599.00, 699.00, 58, 0, 10, '/images/demo-headphone-black.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9008, 9004, '{"颜色":"月光白","版本":"标准"}', 699.00, 799.00, 36, 0, 8, '/images/demo-headphone-white.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9009, 9005, '{"尺寸":"27英寸","分辨率":"2K"}', 1899.00, 2099.00, 32, 0, 6, '/images/demo-monitor-27.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9010, 9005, '{"尺寸":"32英寸","分辨率":"4K"}', 2499.00, 2799.00, 18, 0, 5, '/images/demo-monitor-32.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9011, 9006, '{"颜色":"雾蓝","容量":"500ml"}', 79.00, 99.00, 100, 0, 15, '/images/demo-bottle-blue.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9012, 9007, '{"颜色":"深灰","容量":"22L"}', 269.00, 329.00, 40, 0, 8, '/images/demo-backpack-gray.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9013, 9008, '{"颜色":"奶油白","光源":"暖光"}', 159.00, 199.00, 48, 0, 10, '/images/demo-lamp-white.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9014, 9009, '{"口味":"桂花乌龙","规格":"12泡"}', 88.00, 108.00, 16, 0, 18, '/images/demo-tea-osmanthus.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9015, 9010, '{"颜色":"极光蓝","连接":"蓝牙"}', 159.00, 199.00, 65, 0, 12, '/images/demo-mouse-blue.jpg', 1)
    INTO SKU (id, product_id, spec_desc, price, original_price, stock, locked_stock, warning_stock, sku_image, status)
    VALUES (9016, 9010, '{"颜色":"曜石黑","连接":"蓝牙"}', 199.00, 239.00, 20, 0, 5, '/images/demo-mouse-black.jpg', 0)
SELECT 1 FROM DUAL;

-- 额外生成 140 个可见商品，配合上方 10 个重点演示商品，正好覆盖三页 50 条的商品流。
DECLARE
    v_product_id  NUMBER(19);
    v_category_id NUMBER(10);
    v_price       NUMBER(10,2);
    v_stock       NUMBER(10);
    v_warning     NUMBER(10);
    v_status      NUMBER(1);
    v_name        VARCHAR2(200);
BEGIN
    FOR i IN 1..140 LOOP
        v_product_id := 9100 + i;
        v_category_id := CASE MOD(i, 5)
            WHEN 0 THEN 9002
            WHEN 1 THEN 9003
            WHEN 2 THEN 9005
            WHEN 3 THEN 9007
            ELSE 9008
        END;
        v_price := 49 + MOD(i * 17, 240) + 0.90;
        v_stock := CASE WHEN MOD(i, 25) = 0 THEN 8 ELSE 40 + MOD(i * 7, 60) END;
        v_warning := CASE WHEN MOD(i, 25) = 0 THEN 15 ELSE 5 + MOD(i, 10) END;
        v_status := CASE WHEN MOD(i, 20) = 0 THEN 2 ELSE 1 END;
        v_name := CASE MOD(i, 5)
            WHEN 0 THEN '灵感数码配件'
            WHEN 1 THEN '桌面效率好物'
            WHEN 2 THEN '轻享咖啡茶饮'
            WHEN 3 THEN '品质家居日用'
            ELSE '通勤收纳出行'
        END || ' ' || LPAD(i, 3, '0');

        INSERT INTO PRODUCT (
            id, category_id, name, description, main_image, status,
            price_min, sales_count, view_count, avg_rating, created_at, updated_at)
        VALUES (
            v_product_id, v_category_id, v_name,
            '批量演示商品，用于商品列表、筛选、分页和无限滚动测试。',
            '/images/demo-bulk-product-' || LPAD(i, 3, '0') || '.jpg',
            v_status, v_price, 0, 30 + i * 3, 0,
            SYSDATE - MOD(i, 45), SYSDATE - MOD(i, 20));

        INSERT INTO PRODUCT_IMAGE (id, product_id, image_url, sort_order, created_at)
        VALUES (
            v_product_id, v_product_id,
            '/images/demo-bulk-product-' || LPAD(i, 3, '0') || '.jpg',
            1, SYSDATE - MOD(i, 45));

        INSERT INTO PRODUCT_SPEC (id, product_id, spec_name, spec_value, sort_order)
        VALUES (v_product_id, v_product_id, '演示款式', '标准版' || LPAD(i, 3, '0'), 1);

        INSERT INTO SKU (
            id, product_id, spec_desc, price, original_price, stock,
            locked_stock, warning_stock, sku_image, status)
        VALUES (
            v_product_id, v_product_id,
            '{"款式":"演示' || LPAD(i, 3, '0') || '","版本":"标准"}',
            v_price, v_price + 30, v_stock, 0, v_warning,
            '/images/demo-bulk-product-' || LPAD(i, 3, '0') || '.jpg', 1);
    END LOOP;
END;
/

INSERT INTO INVENTORY_LOG (id, sku_id, change_type, change_qty, before_stock, after_stock, operator_id, ref_order_id, remark, created_at)
SELECT 9000 + s.id - 9000, s.id, 'RESTOCK', s.stock, 0, s.stock, 9001, NULL, '演示数据初始化入库', SYSDATE - 30
FROM SKU s
WHERE s.id BETWEEN 9001 AND 9240;

INSERT ALL
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9001, 9003, 9001, 1, 1, SYSDATE - 2, SYSDATE - 1)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9002, 9003, 9003, 2, 1, SYSDATE - 2, SYSDATE - 1)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9003, 9004, 9005, 1, 0, SYSDATE - 3, SYSDATE - 2)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9004, 9005, 9007, 1, 1, SYSDATE - 1, SYSDATE - 1)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9005, 9006, 9009, 1, 1, SYSDATE - 4, SYSDATE - 2)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9006, 9007, 9012, 1, 1, SYSDATE - 2, SYSDATE - 1)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9007, 9008, 9014, 2, 0, SYSDATE - 1, SYSDATE - 1)
    INTO CART (id, user_id, sku_id, quantity, selected, created_at, updated_at) VALUES (9008, 9009, 9015, 1, 1, SYSDATE - 1, SYSDATE - 1)
SELECT 1 FROM DUAL;

PROMPT Insert coupons and representative orders...

INSERT ALL
    INTO COUPON_TEMPLATE (id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
    VALUES (9001, '满500减50演示券', 1, 50.00, 500.00, 1000, 2, SYSDATE - 30, SYSDATE + 60, 1)
    INTO COUPON_TEMPLATE (id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
    VALUES (9002, '咖啡礼盒85折券', 2, 0.85, 100.00, 500, 2, SYSDATE - 30, SYSDATE + 60, 1)
    INTO COUPON_TEMPLATE (id, name, type, amount, min_amount, total_count, received_count, start_time, end_time, status)
    VALUES (9003, '过期20元券', 1, 20.00, 99.00, 100, 1, SYSDATE - 60, SYSDATE - 1, 0)
    INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
    VALUES (9001, 9003, 9001, 1, SYSDATE - 12, SYSDATE - 8, NULL)
    INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
    VALUES (9002, 9003, 9001, 0, SYSDATE - 5, NULL, NULL)
    INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
    VALUES (9003, 9004, 9002, 0, SYSDATE - 4, NULL, NULL)
    INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
    VALUES (9004, 9005, 9002, 2, SYSDATE - 20, NULL, NULL)
    INTO USER_COUPON (id, user_id, coupon_template_id, status, received_at, used_at, order_id)
    VALUES (9005, 9006, 9003, 2, SYSDATE - 30, NULL, NULL)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9001, 'DEMO202607080001', 9003, 9001, NULL, 0, 3299.00, 0.00, 3299.00, SYSDATE + 1, '{"receiverName":"演示收货人A","receiverPhone":"13800009003","province":"上海市","city":"上海市","district":"浦东新区","detailAddress":"张江高科演示路100号"}', '待支付演示订单', SYSDATE - 10 / 1440, SYSDATE - 10 / 1440)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9002, 'DEMO202607080002', 9003, 9001, 9001, 1, 798.00, 50.00, 748.00, SYSDATE - 7, '{"receiverName":"演示收货人A","receiverPhone":"13800009003","province":"上海市","city":"上海市","district":"浦东新区","detailAddress":"张江高科演示路100号"}', '已支付待发货演示订单', SYSDATE - 8, SYSDATE - 8 + 1 / 24)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9003, 'DEMO202607080003', 9003, 9002, NULL, 2, 3999.00, 0.00, 3999.00, SYSDATE - 6, '{"receiverName":"演示收货人A","receiverPhone":"13800009003","province":"浙江省","city":"杭州市","district":"西湖区","detailAddress":"文三路测试小区8幢302"}', '已发货演示订单', SYSDATE - 7, SYSDATE - 6)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9004, 'DEMO202607080004', 9004, 9003, NULL, 3, 129.00, 0.00, 129.00, SYSDATE - 13, '{"receiverName":"演示收货人B","receiverPhone":"13800009004","province":"广东省","city":"深圳市","district":"南山区","detailAddress":"科技园演示街66号"}', '已完成演示订单', SYSDATE - 14, SYSDATE - 10)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9005, 'DEMO202607080005', 9004, 9003, NULL, 4, 3299.00, 0.00, 0.00, SYSDATE - 15, '{"receiverName":"演示收货人B","receiverPhone":"13800009004","province":"广东省","city":"深圳市","district":"南山区","detailAddress":"科技园演示街66号"}', '用户取消订单', SYSDATE - 16, SYSDATE - 15)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9006, 'DEMO202607080006', 9005, 9004, NULL, 4, 798.00, 0.00, 0.00, SYSDATE - 3, '{"receiverName":"陈晨","receiverPhone":"13800009005","province":"北京市","city":"北京市","district":"朝阳区","detailAddress":"望京街道88号"}', '支付超时自动关闭', SYSDATE - 4, SYSDATE - 3)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9007, 'DEMO202607080007', 9005, 9004, NULL, 1, 758.00, 0.00, 758.00, SYSDATE - 4, '{"receiverName":"陈晨","receiverPhone":"13800009005","province":"北京市","city":"北京市","district":"朝阳区","detailAddress":"望京街道88号"}', '多商品已支付订单', SYSDATE - 5, SYSDATE - 4)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9008, 'DEMO202607080008', 9006, 9005, NULL, 2, 2358.00, 0.00, 2358.00, SYSDATE - 9, '{"receiverName":"林晓","receiverPhone":"13800009006","province":"四川省","city":"成都市","district":"武侯区","detailAddress":"天府大道199号"}', '大件已发货订单', SYSDATE - 10, SYSDATE - 9)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9009, 'DEMO202607080009', 9007, 9006, NULL, 3, 258.00, 0.00, 258.00, SYSDATE - 11, '{"receiverName":"王楠","receiverPhone":"13800009007","province":"湖北省","city":"武汉市","district":"洪山区","detailAddress":"珞喻路299号"}', '已签收待评价订单', SYSDATE - 12, SYSDATE - 7)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9010, 'DEMO202607080010', 9008, 9007, NULL, 3, 445.00, 0.00, 445.00, SYSDATE - 8, '{"receiverName":"张宁","receiverPhone":"13800009008","province":"江苏省","city":"南京市","district":"建邺区","detailAddress":"江东中路66号"}', '完成订单含多件商品', SYSDATE - 9, SYSDATE - 5)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9011, 'DEMO202607080011', 9009, 9008, NULL, 0, 158.00, 0.00, 158.00, SYSDATE + 20 / 1440, '{"receiverName":"李佳","receiverPhone":"13800009009","province":"陕西省","city":"西安市","district":"雁塔区","detailAddress":"科技路128号"}', '待支付水杯订单', SYSDATE - 5 / 1440, SYSDATE - 5 / 1440)
    INTO ORDER_MAIN (id, order_no, user_id, address_id, user_coupon_id, status, total_amount, discount_amount, pay_amount, pay_expire_time, receiver_snapshot, remark, created_at, updated_at)
    VALUES (9012, 'DEMO202607080012', 9010, 9009, NULL, 4, 3299.00, 100.00, 0.00, SYSDATE - 2, '{"receiverName":"赵阳","receiverPhone":"13800009010","province":"福建省","city":"厦门市","district":"思明区","detailAddress":"环岛东路36号"}', '优惠后仍超时关闭订单', SYSDATE - 3, SYSDATE - 2)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9001, 9001, 9001, '星河 X1 智能手机', '{"颜色":"星河银","存储":"256GB"}', '/images/demo-phone-silver.jpg', 3299.00, 1, 3299.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9002, 9002, 9003, '青轴机械键盘 K87', '{"轴体":"青轴","配列":"87键"}', '/images/demo-keyboard-blue.jpg', 399.00, 2, 798.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9003, 9003, 9002, '星河 X1 智能手机', '{"颜色":"深空黑","存储":"512GB"}', '/images/demo-phone-black.jpg', 3999.00, 1, 3999.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9004, 9004, 9005, '山谷冷萃咖啡礼盒', '{"规格":"12瓶装","口味":"原味"}', '/images/demo-coffee-original.jpg', 129.00, 1, 129.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9005, 9005, 9001, '星河 X1 智能手机', '{"颜色":"星河银","存储":"256GB"}', '/images/demo-phone-silver.jpg', 3299.00, 1, 3299.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9006, 9006, 9003, '青轴机械键盘 K87', '{"轴体":"青轴","配列":"87键"}', '/images/demo-keyboard-blue.jpg', 399.00, 2, 798.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9007, 9007, 9007, '云听降噪耳机', '{"颜色":"曜石黑","版本":"标准"}', '/images/demo-headphone-black.jpg', 599.00, 1, 599.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9008, 9007, 9015, '极光无线鼠标', '{"颜色":"极光蓝","连接":"蓝牙"}', '/images/demo-mouse-blue.jpg', 159.00, 1, 159.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9009, 9008, 9009, '星幕 27英寸显示器', '{"尺寸":"27英寸","分辨率":"2K"}', '/images/demo-monitor-27.jpg', 1899.00, 1, 1899.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9010, 9008, 9004, '青轴机械键盘 K87', '{"轴体":"红轴","配列":"87键"}', '/images/demo-keyboard-red.jpg', 459.00, 1, 459.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9011, 9009, 9005, '山谷冷萃咖啡礼盒', '{"规格":"12瓶装","口味":"原味"}', '/images/demo-coffee-original.jpg', 129.00, 2, 258.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9012, 9010, 9012, '城市通勤双肩包', '{"颜色":"深灰","容量":"22L"}', '/images/demo-backpack-gray.jpg', 269.00, 1, 269.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9013, 9010, 9014, '桂花乌龙茶礼盒', '{"口味":"桂花乌龙","规格":"12泡"}', '/images/demo-tea-osmanthus.jpg', 88.00, 2, 176.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9014, 9011, 9011, '轻量保温水杯', '{"颜色":"雾蓝","容量":"500ml"}', '/images/demo-bottle-blue.jpg', 79.00, 2, 158.00)
    INTO ORDER_ITEM (id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap, unit_price, quantity, subtotal) VALUES (9015, 9012, 9001, '星河 X1 智能手机', '{"颜色":"星河银","存储":"256GB"}', '/images/demo-phone-silver.jpg', 3299.00, 1, 3299.00)
SELECT 1 FROM DUAL;

DECLARE
    v_order_id         NUMBER(19);
    v_sku_id           NUMBER(19);
    v_user_id          NUMBER(19);
    v_address_id       NUMBER(19);
    v_status           NUMBER(1);
    v_quantity         NUMBER(10);
    v_unit_price       NUMBER(10,2);
    v_total            NUMBER(10,2);
    v_discount         NUMBER(10,2);
    v_pay_amount       NUMBER(10,2);
    v_created_at       DATE;
    v_pay_expire_time  DATE;
    v_product_name     PRODUCT.name%TYPE;
    v_spec_desc        SKU.spec_desc%TYPE;
    v_image            SKU.sku_image%TYPE;
BEGIN
    FOR i IN 0..35 LOOP
        v_order_id := 9020 + i;
        v_sku_id := 9001 + MOD(i, 15);
        v_user_id := 9003 + MOD(i, 8);
        v_address_id := 9020 + MOD(i, 8);
        v_quantity := 1 + MOD(i, 3);
        v_status := CASE MOD(i, 6)
            WHEN 0 THEN 0
            WHEN 1 THEN 1
            WHEN 2 THEN 2
            WHEN 3 THEN 3
            WHEN 4 THEN 4
            ELSE 3
        END;

        SELECT p.name, s.spec_desc, s.sku_image, s.price
        INTO v_product_name, v_spec_desc, v_image, v_unit_price
        FROM SKU s
        INNER JOIN PRODUCT p ON p.id = s.product_id
        WHERE s.id = v_sku_id;

        v_total := v_unit_price * v_quantity;
        v_discount := CASE WHEN MOD(i, 5) = 0 THEN LEAST(20, v_total) ELSE 0 END;
        v_pay_amount := CASE WHEN v_status = 4 THEN 0 ELSE v_total - v_discount END;

        IF v_status = 0 THEN
            v_created_at := SYSDATE - MOD(i, 10) / 1440;
            v_pay_expire_time := SYSDATE + 20 / 1440;
        ELSE
            v_created_at := TRUNC(SYSDATE) - (1 + MOD(i, 27)) + (9 + MOD(i, 8)) / 24;
            v_pay_expire_time := v_created_at + 30 / 1440;
        END IF;

        INSERT INTO ORDER_MAIN (
            id, order_no, user_id, address_id, user_coupon_id, status,
            total_amount, discount_amount, pay_amount, pay_expire_time,
            receiver_snapshot, remark, created_at, updated_at)
        VALUES (
            v_order_id, 'DEMOAUTO' || TO_CHAR(v_order_id), v_user_id, v_address_id, NULL, v_status,
            v_total, v_discount, v_pay_amount, v_pay_expire_time,
            '{"receiverName":"自动演示用户","receiverPhone":"13800000000","province":"演示省","city":"演示市","district":"演示区","detailAddress":"自动生成订单地址"}',
            '自动生成的多场景演示订单', v_created_at, v_created_at);

        INSERT INTO ORDER_ITEM (
            id, order_id, sku_id, product_name_snap, spec_snap, main_image_snap,
            unit_price, quantity, subtotal)
        VALUES (
            v_order_id, v_order_id, v_sku_id, v_product_name, v_spec_desc, v_image,
            v_unit_price, v_quantity, v_total);

        INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
        VALUES (9500 + i * 4, v_order_id, NULL, 0, v_user_id, '演示用户', '创建订单', v_created_at);

        IF v_status = 4 THEN
            INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
            VALUES (9501 + i * 4, v_order_id, 0, 4, 1, 'system', '支付超时自动关闭', v_created_at + 31 / 1440);
        ELSIF v_status IN (1, 2, 3) THEN
            INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
            VALUES (9501 + i * 4, v_order_id, 0, 1, v_user_id, '演示用户', '模拟支付成功', v_created_at + 10 / 1440);

            INSERT INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data)
            VALUES (9200 + i, v_order_id, '模拟支付', 1, 'AUTO-PAY-' || TO_CHAR(v_order_id), v_pay_amount, v_created_at + 10 / 1440, '{"channel":"demo","status":"success"}');
        END IF;

        IF v_status IN (2, 3) THEN
            INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
            VALUES (9502 + i * 4, v_order_id, 1, 2, 9002, 'demo_service', '客服发货', v_created_at + 1);

            INSERT INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status)
            VALUES (9300 + i, v_order_id, CASE WHEN MOD(i, 2) = 0 THEN '顺丰速运' ELSE '京东物流' END, 'AUTO-TRACK-' || TO_CHAR(v_order_id), v_created_at + 1, CASE WHEN v_status = 3 THEN 3 ELSE 1 END);

            INSERT INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location)
            VALUES (9400 + i * 2, 9300 + i, '包裹已由商家交付物流', v_created_at + 1, '演示发货仓');
        END IF;

        IF v_status = 3 THEN
            INSERT INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at)
            VALUES (9503 + i * 4, v_order_id, 2, 3, v_user_id, '演示用户', '用户确认收货', v_created_at + 4);

            INSERT INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location)
            VALUES (9401 + i * 2, 9300 + i, '包裹已签收，签收人：本人', v_created_at + 4, '演示收货地');
        END IF;
    END LOOP;
END;
/

UPDATE USER_COUPON
SET order_id = 9002
WHERE id = 9001;

INSERT ALL
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9001, 9001, NULL, 0, 9003, 'demo_user', '用户创建订单', SYSDATE - 10 / 1440)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9002, 9002, NULL, 0, 9003, 'demo_user', '用户创建订单', SYSDATE - 8)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9003, 9002, 0, 1, 9003, 'demo_user', '模拟支付成功', SYSDATE - 8 + 1 / 24)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9004, 9003, NULL, 0, 9003, 'demo_user', '用户创建订单', SYSDATE - 7)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9005, 9003, 0, 1, 9003, 'demo_user', '模拟支付成功', SYSDATE - 7 + 1 / 24)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9006, 9003, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 6)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9007, 9004, NULL, 0, 9004, 'demo_buyer', '用户创建订单', SYSDATE - 14)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9008, 9004, 0, 1, 9004, 'demo_buyer', '模拟支付成功', SYSDATE - 14 + 1 / 24)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9009, 9004, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 13)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9010, 9004, 2, 3, 9004, 'demo_buyer', '用户确认收货', SYSDATE - 10)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9011, 9005, NULL, 0, 9004, 'demo_buyer', '用户创建订单', SYSDATE - 16)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9012, 9005, 0, 4, 9004, 'demo_buyer', '用户主动取消', SYSDATE - 15)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9013, 9006, NULL, 0, 9005, 'demo_chen', '用户创建订单', SYSDATE - 4)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9014, 9006, 0, 4, 1, 'system', '订单支付超时自动关闭', SYSDATE - 3)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9015, 9007, NULL, 0, 9005, 'demo_chen', '用户创建订单', SYSDATE - 5)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9016, 9007, 0, 1, 9005, 'demo_chen', '模拟支付成功', SYSDATE - 4)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9017, 9008, NULL, 0, 9006, 'demo_lin', '用户创建订单', SYSDATE - 10)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9018, 9008, 0, 1, 9006, 'demo_lin', '模拟支付成功', SYSDATE - 9)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9019, 9008, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 8)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9020, 9009, NULL, 0, 9007, 'demo_wang', '用户创建订单', SYSDATE - 12)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9021, 9009, 0, 1, 9007, 'demo_wang', '模拟支付成功', SYSDATE - 11)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9022, 9009, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 10)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9023, 9009, 2, 3, 9007, 'demo_wang', '用户确认收货', SYSDATE - 7)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9024, 9010, NULL, 0, 9008, 'demo_zhang', '用户创建订单', SYSDATE - 9)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9025, 9010, 0, 1, 9008, 'demo_zhang', '模拟支付成功', SYSDATE - 8)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9026, 9010, 1, 2, 9002, 'demo_service', '客服发货', SYSDATE - 7)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9027, 9010, 2, 3, 9008, 'demo_zhang', '用户确认收货', SYSDATE - 5)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9028, 9011, NULL, 0, 9009, 'demo_li', '用户创建订单', SYSDATE - 5 / 1440)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9029, 9012, NULL, 0, 9010, 'demo_zhao', '用户创建订单', SYSDATE - 3)
    INTO ORDER_LOG (id, order_id, from_status, to_status, operator_id, operator_name, remark, created_at) VALUES (9030, 9012, 0, 4, 1, 'system', '订单支付超时自动关闭', SYSDATE - 2)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9001, 9002, '模拟支付', 1, 'DEMO-PAY-9002', 748.00, SYSDATE - 8 + 1 / 24, '{"channel":"demo","status":"success"}')
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9002, 9003, '模拟支付', 1, 'DEMO-PAY-9003', 3999.00, SYSDATE - 7 + 1 / 24, '{"channel":"demo","status":"success"}')
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9003, 9004, '模拟支付', 1, 'DEMO-PAY-9004', 129.00, SYSDATE - 14 + 1 / 24, '{"channel":"demo","status":"success"}')
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9004, 9007, '模拟支付', 1, 'DEMO-PAY-9007', 758.00, SYSDATE - 4, '{"channel":"demo","status":"success"}')
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9005, 9008, '模拟支付', 1, 'DEMO-PAY-9008', 2358.00, SYSDATE - 9, '{"channel":"demo","status":"success"}')
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9006, 9009, '模拟支付', 1, 'DEMO-PAY-9009', 258.00, SYSDATE - 11, '{"channel":"demo","status":"success"}')
    INTO PAYMENT (id, order_id, pay_method, status, trade_no, pay_amount, paid_at, callback_data) VALUES (9007, 9010, '模拟支付', 1, 'DEMO-PAY-9010', 445.00, SYSDATE - 8, '{"channel":"demo","status":"success"}')
SELECT 1 FROM DUAL;

INSERT ALL
    INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status) VALUES (9001, 9003, '顺丰速运', 'SFDEMO9003', SYSDATE - 6, 1)
    INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status) VALUES (9002, 9004, '京东物流', 'JDDEMO9004', SYSDATE - 13, 3)
    INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status) VALUES (9003, 9008, '顺丰速运', 'SFDEMO9008', SYSDATE - 8, 1)
    INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status) VALUES (9004, 9009, '京东物流', 'JDDEMO9009', SYSDATE - 10, 3)
    INTO LOGISTICS (id, order_id, company_name, tracking_no, shipped_at, status) VALUES (9005, 9010, '顺丰速运', 'SFDEMO9010', SYSDATE - 7, 3)
    INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location) VALUES (9001, 9001, '包裹已由商家交付物流', SYSDATE - 6, '上海仓')
    INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location) VALUES (9002, 9001, '运输中，下一站杭州分拨中心', SYSDATE - 5, '嘉兴转运中心')
    INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location) VALUES (9003, 9002, '包裹已签收，签收人：本人', SYSDATE - 10, '深圳南山')
    INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location) VALUES (9004, 9003, '运输中，预计明日送达', SYSDATE - 7, '成都分拨中心')
    INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location) VALUES (9005, 9004, '包裹已签收，签收人：本人', SYSDATE - 7, '武汉洪山')
    INTO LOGISTICS_TRACK (id, logistics_id, track_desc, track_time, location) VALUES (9006, 9005, '包裹已签收，签收人：本人', SYSDATE - 5, '南京建邺')
SELECT 1 FROM DUAL;

INSERT ALL
    INTO REVIEW (id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
    VALUES (9001, 9004, 9003, 9004, 5, '冷萃咖啡礼盒包装完整，适合作为评价展示数据。', '[]', 0, 1, SYSDATE - 9)
    INTO REVIEW (id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
    VALUES (9002, 9009, 9003, 9007, 4, '咖啡口感清爽，物流也很及时。', '[]', 0, 1, SYSDATE - 6)
    INTO REVIEW (id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
    VALUES (9003, 9010, 9007, 9008, 5, '双肩包容量很合适，通勤使用方便。', '[]', 0, 1, SYSDATE - 4)
    INTO REVIEW (id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
    VALUES (9004, 9010, 9009, 9008, 4, '茶礼盒外观不错，这条用于后台待审核展示。', '[]', 0, 0, SYSDATE - 3)
    INTO REVIEW (id, order_id, product_id, user_id, rating, content, images, is_anonymous, status, created_at)
    VALUES (9005, 9004, 9003, 9004, 3, '历史屏蔽评价，不计入商品均分。', '[]', 1, 2, SYSDATE - 8)
SELECT 1 FROM DUAL;

INSERT ALL
    INTO OPERATION_LOG (id, operator_id, operator_name, module, action, description, ip_address, request_params, result, created_at)
    VALUES (9001, 9002, 'demo_service', '订单管理', '发货', '客服为演示订单 DEMO202607080003 创建物流信息', '127.0.0.1', '{"orderId":9003}', 1, SYSDATE - 6)
    INTO OPERATION_LOG (id, operator_id, operator_name, module, action, description, ip_address, request_params, result, created_at)
    VALUES (9002, 9002, 'demo_service', '订单管理', '发货', '客服为演示订单 DEMO202607080008 创建物流信息', '127.0.0.1', '{"orderId":9008}', 1, SYSDATE - 8)
    INTO OPERATION_LOG (id, operator_id, operator_name, module, action, description, ip_address, request_params, result, created_at)
    VALUES (9003, 9001, 'demo_admin', '商品管理', '调整库存', '管理员补充咖啡和茶礼盒库存', '127.0.0.1', '{"skuIds":[9005,9006,9014]}', 1, SYSDATE - 2)
    INTO OPERATION_LOG (id, operator_id, operator_name, module, action, description, ip_address, request_params, result, created_at)
    VALUES (9004, 9001, 'demo_admin', '商品管理', '初始化演示数据', '管理员初始化商品、SKU、订单和统计演示数据', '127.0.0.1', '{}', 1, SYSDATE - 1)
SELECT 1 FROM DUAL;

-- 当前库存的锁定数只来自仍处于待支付状态的演示订单。
UPDATE SKU s
SET locked_stock = (
    SELECT NVL(SUM(oi.quantity), 0)
    FROM ORDER_ITEM oi
    INNER JOIN ORDER_MAIN om ON om.id = oi.order_id
    WHERE oi.sku_id = s.id
      AND om.status = 0
)
WHERE s.id BETWEEN 9001 AND 9240;

-- 商品销量、最低价和均分均由当前演示数据推导，保证列表与订单明细一致。
UPDATE PRODUCT p
SET price_min = (SELECT MIN(s.price) FROM SKU s WHERE s.product_id = p.id),
    sales_count = (
        SELECT NVL(SUM(oi.quantity), 0)
        FROM ORDER_ITEM oi
        INNER JOIN SKU s ON s.id = oi.sku_id
        INNER JOIN ORDER_MAIN om ON om.id = oi.order_id
        WHERE s.product_id = p.id
          AND om.status IN (1, 2, 3)
    ),
    avg_rating = (
        SELECT NVL(ROUND(AVG(r.rating), 2), 0)
        FROM REVIEW r
        WHERE r.product_id = p.id
          AND r.status = 1
    )
WHERE p.id BETWEEN 9001 AND 9240;

PROMPT Insert statistics snapshots...

INSERT INTO ORDER_STAT_SNAPSHOT (
    id, stat_date, order_count, paid_count, sales_amount,
    refund_amount, avg_order_amount, new_user_count)
SELECT
    9800 + ROW_NUMBER() OVER (ORDER BY d.stat_date),
    d.stat_date,
    d.order_count,
    d.paid_count,
    d.sales_amount,
    0,
    CASE WHEN d.paid_count = 0 THEN 0 ELSE ROUND(d.sales_amount / d.paid_count, 2) END,
    (SELECT COUNT(1) FROM "USER" u WHERE u.created_at >= d.stat_date AND u.created_at < d.stat_date + 1)
FROM (
    SELECT
        TRUNC(om.created_at) AS stat_date,
        COUNT(1) AS order_count,
        SUM(CASE WHEN om.status IN (1, 2, 3) THEN 1 ELSE 0 END) AS paid_count,
        NVL(SUM(CASE WHEN om.status IN (1, 2, 3) THEN om.pay_amount ELSE 0 END), 0) AS sales_amount
    FROM ORDER_MAIN om
    WHERE om.id BETWEEN 9000 AND 9999
    GROUP BY TRUNC(om.created_at)
) d;

-- 脚本级跨表校验：若任一规则失败，WHENEVER SQLERROR 会回滚所有删除和插入。
DECLARE
    v_count NUMBER;

    PROCEDURE assert_clean(p_rule IN VARCHAR2, p_count IN NUMBER) IS
    BEGIN
        IF p_count > 0 THEN
            RAISE_APPLICATION_ERROR(-20050, 'Seed validation failed: ' || p_rule || ', violations=' || p_count);
        END IF;
    END;
BEGIN
    SELECT COUNT(1)
    INTO v_count
    FROM ORDER_MAIN om
    WHERE om.id BETWEEN 9000 AND 9999
      AND (
          om.total_amount < 0
          OR om.discount_amount < 0
          OR om.discount_amount > om.total_amount
          OR om.pay_amount < 0
          OR (om.status = 4 AND om.pay_amount <> 0)
          OR (om.status IN (0,1,2,3) AND om.pay_amount <> om.total_amount - om.discount_amount)
      );
    assert_clean('order amount/status consistency', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM ORDER_MAIN om
    INNER JOIN ADDRESS a ON a.id = om.address_id
    WHERE om.id BETWEEN 9000 AND 9999
      AND a.user_id <> om.user_id;
    assert_clean('order address ownership', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM ORDER_ITEM oi
    WHERE oi.id BETWEEN 9000 AND 9999
      AND (oi.quantity <= 0 OR oi.unit_price < 0 OR oi.subtotal <> oi.unit_price * oi.quantity);
    assert_clean('order item subtotal', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM ORDER_MAIN om
    WHERE om.id BETWEEN 9000 AND 9999
      AND om.total_amount <> (
          SELECT NVL(SUM(oi.subtotal), 0)
          FROM ORDER_ITEM oi
          WHERE oi.order_id = om.id
      );
    assert_clean('order total equals item sum', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM ORDER_MAIN om
    WHERE om.id BETWEEN 9000 AND 9999
      AND (
          (om.status IN (1,2,3) AND NOT EXISTS (
              SELECT 1 FROM PAYMENT p
              WHERE p.order_id = om.id
                AND p.status = 1
                AND p.pay_amount = om.pay_amount
                AND p.paid_at IS NOT NULL
          ))
          OR (om.status IN (0,4) AND EXISTS (
              SELECT 1 FROM PAYMENT p
              WHERE p.order_id = om.id AND p.status = 1
          ))
      );
    assert_clean('order and payment consistency', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM ORDER_MAIN om
    WHERE om.id BETWEEN 9000 AND 9999
      AND (
          (om.status IN (2,3) AND NOT EXISTS (
              SELECT 1 FROM LOGISTICS l WHERE l.order_id = om.id
          ))
          OR (om.status NOT IN (2,3) AND EXISTS (
              SELECT 1 FROM LOGISTICS l WHERE l.order_id = om.id
          ))
      );
    assert_clean('order and logistics consistency', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM REVIEW r
    INNER JOIN ORDER_MAIN om ON om.id = r.order_id
    WHERE r.id BETWEEN 9000 AND 9999
      AND (
          om.status <> 3
          OR om.user_id <> r.user_id
          OR NOT EXISTS (
              SELECT 1
              FROM ORDER_ITEM oi
              INNER JOIN SKU s ON s.id = oi.sku_id
              WHERE oi.order_id = r.order_id
                AND s.product_id = r.product_id
          )
      );
    assert_clean('review belongs to completed ordered product', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM USER_COUPON uc
    LEFT JOIN ORDER_MAIN om ON om.id = uc.order_id
    WHERE uc.id BETWEEN 9000 AND 9999
      AND (
          (uc.status = 1 AND (
              uc.order_id IS NULL
              OR om.user_id <> uc.user_id
              OR om.status NOT IN (1,2,3)
          ))
          OR (uc.status <> 1 AND uc.order_id IS NOT NULL)
      );
    assert_clean('user coupon order consistency', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM SKU s
    WHERE s.id BETWEEN 9001 AND 9240
      AND s.locked_stock <> (
          SELECT NVL(SUM(oi.quantity), 0)
          FROM ORDER_ITEM oi
          INNER JOIN ORDER_MAIN om ON om.id = oi.order_id
          WHERE oi.sku_id = s.id
            AND om.status = 0
      );
    assert_clean('locked stock equals pending order quantity', v_count);

    SELECT COUNT(1)
    INTO v_count
    FROM PRODUCT p
    WHERE p.id BETWEEN 9001 AND 9240
      AND p.sales_count <> (
          SELECT NVL(SUM(oi.quantity), 0)
          FROM ORDER_ITEM oi
          INNER JOIN SKU s ON s.id = oi.sku_id
          INNER JOIN ORDER_MAIN om ON om.id = oi.order_id
          WHERE s.product_id = p.id
            AND om.status IN (1,2,3)
      );
    assert_clean('product sales count', v_count);
END;
/

COMMIT;
SET DEFINE ON;

PROMPT Demo seed data inserted and validated successfully.
