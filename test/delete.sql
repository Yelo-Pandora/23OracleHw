-- Switch to the correct schema
ALTER SESSION SET CURRENT_SCHEMA = ORACLEDBA;

-- Disable foreign key constraints temporarily (to avoid issues with deletion order)
BEGIN
  FOR c IN (SELECT table_name, constraint_name 
            FROM user_constraints 
            WHERE constraint_type = 'R') LOOP
    EXECUTE IMMEDIATE 'ALTER TABLE ' || c.table_name || 
                      ' DISABLE CONSTRAINT ' || c.constraint_name;
  END LOOP;
END;
/

-- Delete data from all tables (ordered to respect foreign key relationships)
DELETE FROM VENUE_EVENT_DETAIL;
DELETE FROM PART_STORE;
DELETE FROM TEMP_AUTHORITY;
DELETE FROM STAFF_ACCOUNT;
DELETE FROM STORE_ACCOUNT;
DELETE FROM SALARY_SLIP;
DELETE FROM REPAIR_ORDER;
DELETE FROM RENT_STORE;
DELETE FROM PARKING_SPACE_DISTRIBUTION;
DELETE FROM PARK;
DELETE FROM CAR;
DELETE FROM EQUIPMENT_LOCATION;
DELETE FROM SALE_EVENT;
DELETE FROM VENUE_EVENT;
DELETE FROM EVENT;
DELETE FROM STORE;
DELETE FROM STAFF;
DELETE FROM ACCOUNT;
DELETE FROM RETAIL_AREA;
DELETE FROM PARKING_LOT;
DELETE FROM EVENT_AREA;
DELETE FROM OTHER_AREA;
DELETE FROM EQUIPMENT;
DELETE FROM PARKING_SPACE;
DELETE FROM COLLABORATION;
DELETE FROM AREA;
DELETE FROM MONTH_SALARY_COST;

-- Re-enable foreign key constraints
BEGIN
  FOR c IN (SELECT table_name, constraint_name 
            FROM user_constraints 
            WHERE constraint_type = 'R') LOOP
    EXECUTE IMMEDIATE 'ALTER TABLE ' || c.table_name || 
                      ' ENABLE CONSTRAINT ' || c.constraint_name;
  END LOOP;
END;
/

COMMIT;