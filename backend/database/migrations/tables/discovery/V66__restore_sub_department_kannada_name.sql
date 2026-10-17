-- V40 recreated sub_department_master from the pre-V32 shape but omitted the Kannada
-- display-name column added by V29. Restore the column without changing existing rows.
ALTER TABLE discovery.sub_department_master
    ADD COLUMN IF NOT EXISTS sub_department_name_kannada character varying(200);
