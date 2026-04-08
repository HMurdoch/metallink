-- Mark existing data as inactive
UPDATE metal_link.stock_levels SET is_active = false;
UPDATE metal_link.stock_movements SET is_active = false;

-- Get starred products
DO $$
DECLARE
    prod RECORD;
    op_id INT;
    opening_weight DECIMAL;
    move_date TIMESTAMP;
    i INT;
    j INT;
    buy_w DECIMAL;
    sell_w DECIMAL;
    current_weight DECIMAL;
    v_site_id INT;
BEGIN
    -- Get first operator
    SELECT operator_id INTO op_id FROM metal_link.operators LIMIT 1;
    IF op_id IS NULL THEN op_id := 1; END IF;

    -- Get first site
    SELECT s.site_id INTO v_site_id FROM metal_link.sites s LIMIT 1;
    IF v_site_id IS NULL THEN
        -- Fallback: check if we can find any site or use 1
        v_site_id := 1;
    END IF;

    FOR prod IN SELECT product_id FROM metal_link.products WHERE starred_product = true AND is_active = true LOOP
        -- Opening balance
        opening_weight := (random() * 5000 + 1000)::DECIMAL(18,2);
        
        -- Insert opening balance into stock_levels
        -- We don't have site_id in stock_levels based on the schema I saw earlier (\d metal_link.stock_levels)
        -- It has: stock_level_id, product_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time
        INSERT INTO metal_link.stock_levels (product_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (prod.product_id, opening_weight, op_id, true, now() - INTERVAL '6 days', now() - INTERVAL '6 days');

        current_weight := opening_weight;

        -- Generate movements for the last 5 days
        FOR i IN 0..4 LOOP
            move_date := (now() - (i || ' days')::INTERVAL)::date + '08:00:00'::time;
            
            -- 4 to 8 movements per day
            FOR j IN 1..(floor(random() * 5) + 4) LOOP
                move_date := move_date + (random() * 60 || ' minutes')::INTERVAL;
                buy_w := 0;
                sell_w := 0;
                
                IF random() > 0.5 THEN
                    buy_w := (random() * 500 + 50)::DECIMAL(18,2);
                    current_weight := current_weight + buy_w;
                ELSE
                    sell_w := (random() * 500 + 50)::DECIMAL(18,2);
                    current_weight := current_weight - sell_w;
                END IF;

                INSERT INTO metal_link.stock_movements 
                    (product_id, base_weight_kg, buy_weight_kg, sell_weight_kg, created_by_operator_id, is_active, created_time, updated_time, notes)
                VALUES 
                    (prod.product_id, current_weight, buy_w, sell_w, op_id, true, move_date, move_date, 'Auto-generated movement');
            END LOOP;
        END LOOP;
        
        -- Update final stock level
        UPDATE metal_link.stock_levels 
        SET weight_kg = GREATEST(0, current_weight), updated_time = now() 
        WHERE product_id = prod.product_id AND is_active = true;
    END LOOP;
END $$;
