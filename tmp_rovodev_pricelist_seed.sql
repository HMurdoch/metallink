DO $$
DECLARE
    v_operator_id INTEGER := 1; -- Assumes operator_id=1 exists
    v_cust_alpha_id INTEGER;
    v_cust_beta_id INTEGER;
    v_cust_charlie_id INTEGER;
    v_buyer_alpha_id INTEGER;
    v_buyer_beta_id INTEGER;
    v_buyer_charlie_id INTEGER;
    r_prod RECORD;
BEGIN
    -- 1. Seed Product Price Lists
    INSERT INTO metal_link.product_price_lists (product_price_list_name, product_price_list_description, entity_flag, created_by_operator_id, is_active, created_time, updated_time)
    VALUES ('Customer Alpha', 'Standard Customer Price List A', 'C', v_operator_id, true, now(), now())
    RETURNING product_price_list_id INTO v_cust_alpha_id;

    INSERT INTO metal_link.product_price_lists (product_price_list_name, product_price_list_description, entity_flag, created_by_operator_id, is_active, created_time, updated_time)
    VALUES ('Customer Beta', 'Discounted Customer Price List B', 'C', v_operator_id, true, now(), now())
    RETURNING product_price_list_id INTO v_cust_beta_id;

    INSERT INTO metal_link.product_price_lists (product_price_list_name, product_price_list_description, entity_flag, created_by_operator_id, is_active, created_time, updated_time)
    VALUES ('Customer Charlie', 'Special Customer Price List C', 'C', v_operator_id, true, now(), now())
    RETURNING product_price_list_id INTO v_cust_charlie_id;

    INSERT INTO metal_link.product_price_lists (product_price_list_name, product_price_list_description, entity_flag, created_by_operator_id, is_active, created_time, updated_time)
    VALUES ('Buyer Alpha', 'Standard Buyer Price List A', 'B', v_operator_id, true, now(), now())
    RETURNING product_price_list_id INTO v_buyer_alpha_id;

    INSERT INTO metal_link.product_price_lists (product_price_list_name, product_price_list_description, entity_flag, created_by_operator_id, is_active, created_time, updated_time)
    VALUES ('Buyer Beta', 'Discounted Buyer Price List B', 'B', v_operator_id, true, now(), now())
    RETURNING product_price_list_id INTO v_buyer_beta_id;

    INSERT INTO metal_link.product_price_lists (product_price_list_name, product_price_list_description, entity_flag, created_by_operator_id, is_active, created_time, updated_time)
    VALUES ('Buyer Charlie', 'Special Buyer Price List C', 'B', v_operator_id, true, now(), now())
    RETURNING product_price_list_id INTO v_buyer_charlie_id;

    -- 2. Seed Product Prices (Randomized for all active products)
    FOR r_prod IN SELECT product_id FROM metal_link.products WHERE is_active = true LOOP
        -- Seed for each list
        INSERT INTO metal_link.product_price_list_product_prices (product_price_list_id, product_id, price, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (v_cust_alpha_id, r_prod.product_id, (random() * 50 + 10)::numeric(10,2), v_operator_id, true, now(), now());
        
        INSERT INTO metal_link.product_price_list_product_prices (product_price_list_id, product_id, price, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (v_cust_beta_id, r_prod.product_id, (random() * 50 + 10)::numeric(10,2), v_operator_id, true, now(), now());

        INSERT INTO metal_link.product_price_list_product_prices (product_price_list_id, product_id, price, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (v_cust_charlie_id, r_prod.product_id, (random() * 50 + 10)::numeric(10,2), v_operator_id, true, now(), now());

        INSERT INTO metal_link.product_price_list_product_prices (product_price_list_id, product_id, price, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (v_buyer_alpha_id, r_prod.product_id, (random() * 50 + 10)::numeric(10,2), v_operator_id, true, now(), now());

        INSERT INTO metal_link.product_price_list_product_prices (product_price_list_id, product_id, price, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (v_buyer_beta_id, r_prod.product_id, (random() * 50 + 10)::numeric(10,2), v_operator_id, true, now(), now());

        INSERT INTO metal_link.product_price_list_product_prices (product_price_list_id, product_id, price, created_by_operator_id, is_active, created_time, updated_time)
        VALUES (v_buyer_charlie_id, r_prod.product_id, (random() * 50 + 10)::numeric(10,2), v_operator_id, true, now(), now());
    END LOOP;

    -- 3. Update Copper products must_declare = true
    UPDATE metal_link.products SET must_declare = true 
    WHERE (product_name ILIKE '%copper%' OR product_code ILIKE '%cop%') AND is_active = true;

    -- 4. Assign price lists to customers based on price_code
    UPDATE metal_link.customers SET product_price_list_id = 
        CASE 
            WHEN price_code = 'A' THEN v_cust_alpha_id
            WHEN price_code = 'B' THEN v_cust_beta_id
            WHEN price_code = 'C' THEN v_cust_charlie_id
            ELSE v_cust_alpha_id -- Default
        END
    WHERE is_active = true;

    -- 5. Assign price lists to buyers based on price_code
    UPDATE metal_link.buyers SET product_price_list_id = 
        CASE 
            WHEN price_code = 'A' THEN v_buyer_alpha_id
            WHEN price_code = 'B' THEN v_buyer_beta_id
            WHEN price_code = 'C' THEN v_buyer_charlie_id
            ELSE v_buyer_alpha_id -- Default
        END
    WHERE is_active = true;

END $$;
