-- insert_test_accounts.sql
-- 为开发/测试方便，向 ACCOUNT、STAFF、STORE 及其关联表插入示例数据
-- 注意：此脚本适用于测试环境。PASSWORD 字段为明文示例，仅用于本地测试。
-- 强烈建议在生产环境使用 bcrypt 等强哈希并通过后端注册接口创建账号。

-- 使用前请确认当前 schema 已切换为项目 schema（示例中为 ORACLEDBA）
-- ALTER SESSION SET CURRENT_SCHEMA = ORACLEDBA;

SET DEFINE OFF;

-- -----------------------------
-- 1) 示例：员工账号（张三）
-- -----------------------------
-- 新增员工基础信息
INSERT INTO STAFF (STAFF_ID, STAFF_NAME, STAFF_SEX, STAFF_APARTMENT, STAFF_POSTITION, STAFF_SALARY)
VALUES (1001, '张三', '男', '客服部', '客服', 6000);

-- 新增账号（测试用明文密码 password123）
INSERT INTO ACCOUNT (ACCOUNT, PASSWORD, IDENTITY, USERNAME, AUTHORITY)
VALUES ('staff_zhang', 'password123', '员工', '张三', 1);

-- 关联员工账号
INSERT INTO STAFF_ACCOUNT (ACCOUNT, STAFF_ID) VALUES ('staff_zhang', 1001);

-- -----------------------------
-- 2) 示例：商户账号（测试店铺）
-- -----------------------------
-- 新增店铺信息
INSERT INTO STORE (STORE_ID, STORE_NAME, STORE_STATUS, STORE_TYPE, TENANT_NAME, CONTACT_INFO, RENT_START, RENT_END)
VALUES (2001, '测试便利店', '正常', '个人', '李四', '13800001111', TO_DATE('2025-01-01','YYYY-MM-DD'), TO_DATE('2026-01-01','YYYY-MM-DD'));

-- 新增商户账号（测试用明文密码 shop@123）
INSERT INTO ACCOUNT (ACCOUNT, PASSWORD, IDENTITY, USERNAME, AUTHORITY)
VALUES ('shop_2001', 'shop@123', '商户', '测试便利店', 2);

-- 关联商户账号与店铺
INSERT INTO STORE_ACCOUNT (ACCOUNT, STORE_ID) VALUES ('shop_2001', 2001);

-- -----------------------------
-- 3) 示例：管理员账号（可查看/管理更多模块）
-- -----------------------------
INSERT INTO ACCOUNT (ACCOUNT, PASSWORD, IDENTITY, USERNAME, AUTHORITY)
VALUES ('admin', 'admin123', '员工', '管理员', 9);

-- -----------------------------
-- 提示与校验
-- 1) 插入完成后请执行 COMMIT;
-- 2) 若使用序列产生 ID，请替换 STAFF_ID/STORE_ID 为序列值。
-- 3) 为安全起见，建议把 PASSWORD 列替换为哈希值（bcrypt），示例：
--    在 Node.js 中生成 bcrypt hash:
--      const bcrypt = require('bcrypt');
--      const hash = await bcrypt.hash('password123', 10);
--    然后把 hash 字符串放入 PASSWORD 字段。

COMMIT;

-- 验证查询示例：
-- SELECT * FROM ACCOUNT;
-- SELECT a.*, s.STAFF_NAME FROM ACCOUNT a JOIN STAFF_ACCOUNT sa ON sa.ACCOUNT = a.ACCOUNT JOIN STAFF s ON s.STAFF_ID = sa.STAFF_ID;
-- SELECT a.*, st.STORE_NAME FROM ACCOUNT a JOIN STORE_ACCOUNT sa ON sa.ACCOUNT = a.ACCOUNT JOIN STORE st ON st.STORE_ID = sa.STORE_ID;
