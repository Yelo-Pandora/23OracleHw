-- =====================================================
-- 停车场2.8.3功能测试数据清理脚本 (改进版)
-- 功能：清理2.8.3功能测试过程中创建的所有测试数据
-- 创建时间：2024年
-- 改进时间：2025年
-- 特点：更安全的清理、详细的统计、错误处理
-- =====================================================

-- 声明变量用于统计
DECLARE
    v_park_count NUMBER := 0;
    v_car_count NUMBER := 0;
    v_distribution_count NUMBER := 0;
    v_space_count NUMBER := 0;
    v_lot_count NUMBER := 0;
    v_area_count NUMBER := 0;
    v_staff_account_count NUMBER := 0;
    v_account_count NUMBER := 0;
    v_total_deleted NUMBER := 0;
    v_start_time TIMESTAMP := SYSTIMESTAMP;
BEGIN

    DBMS_OUTPUT.PUT_LINE('🚀 开始清理2.8.3功能测试数据...');
    DBMS_OUTPUT.PUT_LINE('⏰ 开始时间: ' || TO_CHAR(v_start_time, 'YYYY-MM-DD HH24:MI:SS.FF3'));
    DBMS_OUTPUT.PUT_LINE('==========================================');

    -- 1. 清理测试停车记录 (PARK表)
    SELECT COUNT(*) INTO v_park_count FROM PARK WHERE LICENSE_PLATE_NUMBER LIKE 'TEST%';
    DELETE FROM PARK WHERE LICENSE_PLATE_NUMBER LIKE 'TEST%';
    DBMS_OUTPUT.PUT_LINE('✅ 已清理测试停车记录: ' || v_park_count || ' 条');
    v_total_deleted := v_total_deleted + v_park_count;

    -- 2. 清理测试车辆信息 (CAR表)
    SELECT COUNT(*) INTO v_car_count FROM CAR WHERE LICENCE_PLATE_NUMBER LIKE 'TEST%';
    DELETE FROM CAR WHERE LICENCE_PLATE_NUMBER LIKE 'TEST%';
    DBMS_OUTPUT.PUT_LINE('✅ 已清理测试车辆信息: ' || v_car_count || ' 条');
    v_total_deleted := v_total_deleted + v_car_count;

    -- 3. 清理车位分布记录 (PARKING_SPACE_DISTRIBUTION表)
    SELECT COUNT(*) INTO v_distribution_count FROM PARKING_SPACE_DISTRIBUTION WHERE AREA_ID IN (1001, 1002, 1003);
    DELETE FROM PARKING_SPACE_DISTRIBUTION WHERE AREA_ID IN (1001, 1002, 1003);
    DBMS_OUTPUT.PUT_LINE('✅ 已清理车位分布记录: ' || v_distribution_count || ' 条');
    v_total_deleted := v_total_deleted + v_distribution_count;

    -- 4. 清理测试车位信息 (PARKING_SPACE表)
    SELECT COUNT(*) INTO v_space_count 
    FROM PARKING_SPACE 
    WHERE PARKING_SPACE_ID BETWEEN 1001 AND 1020 
       OR PARKING_SPACE_ID BETWEEN 2001 AND 2015 
       OR PARKING_SPACE_ID BETWEEN 3001 AND 3025;
    
    DELETE FROM PARKING_SPACE WHERE PARKING_SPACE_ID BETWEEN 1001 AND 1020;
    DELETE FROM PARKING_SPACE WHERE PARKING_SPACE_ID BETWEEN 2001 AND 2015;
    DELETE FROM PARKING_SPACE WHERE PARKING_SPACE_ID BETWEEN 3001 AND 3025;
    
    -- 额外清理：确保清理所有可能的测试车位ID
    DECLARE
        v_extra_count NUMBER := 0;
    BEGIN
        SELECT COUNT(*) INTO v_extra_count 
        FROM PARKING_SPACE 
        WHERE PARKING_SPACE_ID BETWEEN 1001 AND 3025 
          AND PARKING_SPACE_ID NOT BETWEEN 1001 AND 1020
          AND PARKING_SPACE_ID NOT BETWEEN 2001 AND 2015
          AND PARKING_SPACE_ID NOT BETWEEN 3001 AND 3025;
        
        IF v_extra_count > 0 THEN
            DELETE FROM PARKING_SPACE 
            WHERE PARKING_SPACE_ID BETWEEN 1001 AND 3025 
              AND PARKING_SPACE_ID NOT BETWEEN 1001 AND 1020
              AND PARKING_SPACE_ID NOT BETWEEN 2001 AND 2015
              AND PARKING_SPACE_ID NOT BETWEEN 3001 AND 3025;
            DBMS_OUTPUT.PUT_LINE('✅ 额外清理车位信息: ' || v_extra_count || ' 条');
            v_space_count := v_space_count + v_extra_count;
        END IF;
    END;
    
    DBMS_OUTPUT.PUT_LINE('✅ 已清理测试车位信息: ' || v_space_count || ' 条');
    v_total_deleted := v_total_deleted + v_space_count;

    -- 5. 清理停车场费用信息 (PARKING_LOT表)
    SELECT COUNT(*) INTO v_lot_count FROM PARKING_LOT WHERE AREA_ID IN (1001, 1002, 1003);
    DELETE FROM PARKING_LOT WHERE AREA_ID IN (1001, 1002, 1003);
    DBMS_OUTPUT.PUT_LINE('✅ 已清理停车场费用信息: ' || v_lot_count || ' 条');
    v_total_deleted := v_total_deleted + v_lot_count;

    -- 6. 清理停车场区域 (AREA表)
    SELECT COUNT(*) INTO v_area_count FROM AREA WHERE AREA_ID IN (1001, 1002, 1003);
    DELETE FROM AREA WHERE AREA_ID IN (1001, 1002, 1003);
    DBMS_OUTPUT.PUT_LINE('✅ 已清理停车场区域: ' || v_area_count || ' 条');
    v_total_deleted := v_total_deleted + v_area_count;

    -- 7. 清理测试账号关联 (STAFF_ACCOUNT表)
    BEGIN
        SELECT COUNT(*) INTO v_staff_account_count FROM STAFF_ACCOUNT WHERE ACCOUNT LIKE '%test%';
        DELETE FROM STAFF_ACCOUNT WHERE ACCOUNT LIKE '%test%';
        DBMS_OUTPUT.PUT_LINE('✅ 已清理测试账号关联: ' || v_staff_account_count || ' 条');
        v_total_deleted := v_total_deleted + v_staff_account_count;
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('⚠️  STAFF_ACCOUNT表不存在或清理失败: ' || SQLERRM);
    END;

    -- 8. 清理测试账号 (ACCOUNT表)
    SELECT COUNT(*) INTO v_account_count FROM ACCOUNT WHERE ACCOUNT LIKE '%test%';
    DELETE FROM ACCOUNT WHERE ACCOUNT LIKE '%test%';
    DBMS_OUTPUT.PUT_LINE('✅ 已清理测试账号: ' || v_account_count || ' 条');
    v_total_deleted := v_total_deleted + v_account_count;

    -- 提交事务
    COMMIT;
    
    -- 计算执行时间
    DECLARE
        v_end_time TIMESTAMP := SYSTIMESTAMP;
        v_duration INTERVAL DAY TO SECOND;
    BEGIN
        v_duration := v_end_time - v_start_time;
        
        -- 显示清理结果
        DBMS_OUTPUT.PUT_LINE('==========================================');
        DBMS_OUTPUT.PUT_LINE('🎉 2.8.3功能测试数据清理完成！');
        DBMS_OUTPUT.PUT_LINE('==========================================');
        DBMS_OUTPUT.PUT_LINE('📊 清理统计:');
        DBMS_OUTPUT.PUT_LINE('   - 停车记录: ' || v_park_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 车辆信息: ' || v_car_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 车位分布: ' || v_distribution_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 车位信息: ' || v_space_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 费用信息: ' || v_lot_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 停车场区域: ' || v_area_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 账号关联: ' || v_staff_account_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 测试账号: ' || v_account_count || ' 条');
        DBMS_OUTPUT.PUT_LINE('   - 总计删除: ' || v_total_deleted || ' 条记录');
        DBMS_OUTPUT.PUT_LINE('⏱️  执行时间: ' || EXTRACT(SECOND FROM v_duration) || ' 秒');
        DBMS_OUTPUT.PUT_LINE('⏰ 完成时间: ' || TO_CHAR(v_end_time, 'YYYY-MM-DD HH24:MI:SS.FF3'));
    END;

EXCEPTION
    WHEN OTHERS THEN
        -- 回滚事务
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('==========================================');
        DBMS_OUTPUT.PUT_LINE('❌ 清理过程中发生错误！');
        DBMS_OUTPUT.PUT_LINE('错误代码: ' || SQLCODE);
        DBMS_OUTPUT.PUT_LINE('错误信息: ' || SQLERRM);
        DBMS_OUTPUT.PUT_LINE('已回滚所有更改');
        DBMS_OUTPUT.PUT_LINE('==========================================');
        RAISE;
END;
/

-- =====================================================
-- 验证清理结果
-- =====================================================
PROMPT '正在验证清理结果...'

-- 验证清理结果
SELECT 
    '测试账号' AS DATA_TYPE,
    COUNT(*) AS REMAINING_COUNT,
    CASE WHEN COUNT(*) = 0 THEN '✅ 清理成功' ELSE '❌ 仍有残留' END AS STATUS
FROM ACCOUNT WHERE ACCOUNT LIKE '%test%'
UNION ALL
SELECT 
    '测试账号关联',
    COUNT(*),
    CASE WHEN COUNT(*) = 0 THEN '✅ 清理成功' ELSE '❌ 仍有残留' END
FROM STAFF_ACCOUNT WHERE ACCOUNT LIKE '%test%'
UNION ALL
SELECT 
    '停车场区域',
    COUNT(*),
    CASE WHEN COUNT(*) = 0 THEN '✅ 清理成功' ELSE '❌ 仍有残留' END
FROM AREA WHERE AREA_ID IN (1001, 1002, 1003)
UNION ALL
SELECT 
    '车位信息',
    COUNT(*),
    CASE WHEN COUNT(*) = 0 THEN '✅ 清理成功' ELSE '❌ 仍有残留' END
FROM PARKING_SPACE 
WHERE PARKING_SPACE_ID BETWEEN 1001 AND 1020 
   OR PARKING_SPACE_ID BETWEEN 2001 AND 2015 
   OR PARKING_SPACE_ID BETWEEN 3001 AND 3025
UNION ALL
SELECT 
    '测试车辆',
    COUNT(*),
    CASE WHEN COUNT(*) = 0 THEN '✅ 清理成功' ELSE '❌ 仍有残留' END
FROM CAR WHERE LICENCE_PLATE_NUMBER LIKE 'TEST%'
UNION ALL
SELECT 
    '停车记录',
    COUNT(*),
    CASE WHEN COUNT(*) = 0 THEN '✅ 清理成功' ELSE '❌ 仍有残留' END
FROM PARK WHERE LICENSE_PLATE_NUMBER LIKE 'TEST%';

-- 显示最终验证结果
SELECT 
    CASE 
        WHEN SUM(REMAINING_COUNT) = 0 THEN '🎉 所有测试数据已完全清理！'
        ELSE '⚠️  仍有 ' || SUM(REMAINING_COUNT) || ' 条测试数据残留'
    END AS FINAL_STATUS
FROM (
    SELECT COUNT(*) AS REMAINING_COUNT FROM ACCOUNT WHERE ACCOUNT LIKE '%test%'
    UNION ALL
    SELECT COUNT(*) FROM STAFF_ACCOUNT WHERE ACCOUNT LIKE '%test%'
    UNION ALL
    SELECT COUNT(*) FROM AREA WHERE AREA_ID IN (1001, 1002, 1003)
    UNION ALL
    SELECT COUNT(*) FROM PARKING_SPACE 
    WHERE PARKING_SPACE_ID BETWEEN 1001 AND 1020 
       OR PARKING_SPACE_ID BETWEEN 2001 AND 2015 
       OR PARKING_SPACE_ID BETWEEN 3001 AND 3025
    UNION ALL
    SELECT COUNT(*) FROM CAR WHERE LICENCE_PLATE_NUMBER LIKE 'TEST%'
    UNION ALL
    SELECT COUNT(*) FROM PARK WHERE LICENSE_PLATE_NUMBER LIKE 'TEST%'
);

-- 调试：显示具体的残留车位ID（如果有的话）
SELECT 
    '残留车位ID详情' AS DEBUG_INFO,
    PARKING_SPACE_ID,
    OCCUPIED,
    CASE WHEN OCCUPIED = 0 THEN '空闲' ELSE '占用' END AS STATUS
FROM PARKING_SPACE 
WHERE PARKING_SPACE_ID BETWEEN 1001 AND 1020 
   OR PARKING_SPACE_ID BETWEEN 2001 AND 2015 
   OR PARKING_SPACE_ID BETWEEN 3001 AND 3025
ORDER BY PARKING_SPACE_ID;

PROMPT '=========================================='
PROMPT '🔧 清理脚本执行完成！'
PROMPT '=========================================='
