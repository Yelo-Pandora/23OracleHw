-- 添加测试管理员账号
-- 用于新增店面功能测试

-- 插入管理员账号（如果不存在）
BEGIN
  -- 检查是否已存在admin账号
  DECLARE
    account_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO account_count FROM ACCOUNT WHERE ACCOUNT = 'admin';
    
    IF account_count = 0 THEN
      INSERT INTO ACCOUNT (ACCOUNT, PASSWORD, IDENTITY, USERNAME, AUTHORITY) 
      VALUES ('admin', 'admin123', '管理员', '系统管理员', 1);
      DBMS_OUTPUT.PUT_LINE('管理员账号创建成功');
    ELSE
      DBMS_OUTPUT.PUT_LINE('管理员账号已存在');
    END IF;
  END;
END;
/

-- 插入普通用户账号用于权限测试
BEGIN
  DECLARE
    account_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO account_count FROM ACCOUNT WHERE ACCOUNT = 'guest';
    
    IF account_count = 0 THEN
      INSERT INTO ACCOUNT (ACCOUNT, PASSWORD, IDENTITY, USERNAME, AUTHORITY) 
      VALUES ('guest', 'guest123', '游客', '普通用户', 5);
      DBMS_OUTPUT.PUT_LINE('普通用户账号创建成功');
    ELSE
      DBMS_OUTPUT.PUT_LINE('普通用户账号已存在');
    END IF;
  END;
END;
/

COMMIT;

-- 验证账号创建结果
SELECT ACCOUNT, IDENTITY, USERNAME, AUTHORITY 
FROM ACCOUNT 
WHERE ACCOUNT IN ('admin', 'guest');

PROMPT '测试账号设置完成！'
PROMPT '- admin (管理员权限: 1) - 可以新增店面'
PROMPT '- guest (普通用户权限: 5) - 无权限新增店面'
