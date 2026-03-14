DO $$
DECLARE
    v_operator_id INTEGER := 1;
    v_setting_id INTEGER;
    v_option_true_id INTEGER;
    v_option_false_id INTEGER;
    r_op RECORD;
BEGIN
    -- 1. Add the Setting
    INSERT INTO metal_link.settings (setting_name, setting_description, is_active, created_by_operator_id, time_created, time_updated)
    VALUES ('enforce_buyer_company', 'Force buyer to have a company selection (bool)', true, v_operator_id, now(), now())
    RETURNING setting_id INTO v_setting_id;

    -- 2. Add Options
    INSERT INTO metal_link.setting_options (setting_id, setting_option_value, is_active, created_by_operator_id, time_created, time_updated)
    VALUES (v_setting_id, 'true', true, v_operator_id, now(), now())
    RETURNING setting_option_id INTO v_option_true_id;

    INSERT INTO metal_link.setting_options (setting_id, setting_option_value, is_active, created_by_operator_id, time_created, time_updated)
    VALUES (v_setting_id, 'false', true, v_operator_id, now(), now())
    RETURNING setting_option_id INTO v_option_false_id;

    -- 3. Apply to all existing operators (default true)
    FOR r_op IN SELECT operator_id FROM metal_link.operators WHERE is_active = true LOOP
        INSERT INTO metal_link.operator_settings (operator_id, setting_id, setting_option_id, is_active, created_by_operator_id, time_created, time_updated)
        VALUES (r_op.operator_id, v_setting_id, v_option_true_id, true, v_operator_id, now(), now());
    END LOOP;

END $$;
