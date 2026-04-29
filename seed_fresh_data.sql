-- Seed fresh data for 3 months past and 3 months future
-- Past data: is_active = true
-- Future data: is_active = false

BEGIN;

DO $$
DECLARE
  v_today date := CURRENT_DATE;
  v_start_date date := v_today - INTERVAL '3 months';
  v_end_date date := v_today + INTERVAL '3 months';
  v_current_date date;

  -- Counts
  v_purchase_tickets_per_day int;
  v_sale_tickets_per_day int;
  v_lines_per_purchase_ticket int;
  v_lines_per_sale_ticket int;

  -- IDs
  v_customer_id int;
  v_buyer_id int;
  v_product_id int;
  v_price_list_id int;
  v_price_id int;
  v_ticket_id int;
  v_ticket_line_id int;
  v_ticket_type_id int;
  v_ticket_number text;

  -- Ticket counters
  v_rwb_seq int := 0;
  v_rpl_seq int := 0;
  v_swb_seq int := 0;
  v_spl_seq int := 0;

  -- Weights
  v_weight numeric(18,2);
  v_total_weight numeric(18,2);
  v_unit_price numeric(18,2);
  v_base_weight numeric(18,2);

  -- Random
  v_random float;

  -- Collections
  v_customers int[];
  v_buyers int[];
  v_products int[];
  v_price_lists int[];
  v_prices int[];

  v_is_future boolean;
  v_is_active boolean;

BEGIN
  -- Get all active customers, buyers, products, price lists, prices
  SELECT array_agg(customer_id) INTO v_customers FROM metal_link.customers WHERE is_active = true;
  SELECT array_agg(buyer_id) INTO v_buyers FROM metal_link.buyers WHERE is_active = true;
  SELECT array_agg(product_id) INTO v_products FROM metal_link.products WHERE starred_product = true;
  SELECT array_agg(product_price_list_id) INTO v_price_lists FROM metal_link.product_price_lists WHERE is_active = true;
  -- For prices, we'll select per product and price list

  -- Correct any existing bad seeded ticket numbers from older seed runs
  UPDATE metal_link.receiving_tickets r
  SET ticket_number = s.new_ticket_number
  FROM (
    SELECT receiving_ticket_id,
      'RWB-' || lpad((COALESCE((SELECT MAX(CAST(split_part(ticket_number, '-', 2) AS int)) FROM metal_link.receiving_tickets WHERE ticket_number LIKE 'RWB-%'), 0) + row_number() OVER (ORDER BY created_time, receiving_ticket_id))::text, 8, '0') AS new_ticket_number
    FROM metal_link.receiving_tickets
    WHERE ticket_number LIKE 'PUR-%' AND ticket_type_id = 1
  ) s
  WHERE r.receiving_ticket_id = s.receiving_ticket_id;

  UPDATE metal_link.receiving_tickets r
  SET ticket_number = s.new_ticket_number
  FROM (
    SELECT receiving_ticket_id,
      'RPL-' || lpad((COALESCE((SELECT MAX(CAST(split_part(ticket_number, '-', 2) AS int)) FROM metal_link.receiving_tickets WHERE ticket_number LIKE 'RPL-%'), 0) + row_number() OVER (ORDER BY created_time, receiving_ticket_id))::text, 8, '0') AS new_ticket_number
    FROM metal_link.receiving_tickets
    WHERE ticket_number LIKE 'PUR-%' AND ticket_type_id = 2
  ) s
  WHERE r.receiving_ticket_id = s.receiving_ticket_id;

  UPDATE metal_link.sending_tickets s
  SET ticket_number = u.new_ticket_number
  FROM (
    SELECT sending_ticket_id,
      'SWB-' || lpad((COALESCE((SELECT MAX(CAST(split_part(ticket_number, '-', 2) AS int)) FROM metal_link.sending_tickets WHERE ticket_number LIKE 'SWB-%'), 0) + row_number() OVER (ORDER BY created_time, sending_ticket_id))::text, 8, '0') AS new_ticket_number
    FROM metal_link.sending_tickets
    WHERE ticket_number LIKE 'SAL-%' AND ticket_type_id = 1
  ) u
  WHERE s.sending_ticket_id = u.sending_ticket_id;

  UPDATE metal_link.sending_tickets s
  SET ticket_number = u.new_ticket_number
  FROM (
    SELECT sending_ticket_id,
      'SPL-' || lpad((COALESCE((SELECT MAX(CAST(split_part(ticket_number, '-', 2) AS int)) FROM metal_link.sending_tickets WHERE ticket_number LIKE 'SPL-%'), 0) + row_number() OVER (ORDER BY created_time, sending_ticket_id))::text, 8, '0') AS new_ticket_number
    FROM metal_link.sending_tickets
    WHERE ticket_number LIKE 'SAL-%' AND ticket_type_id = 2
  ) u
  WHERE s.sending_ticket_id = u.sending_ticket_id;

  -- Initialize ticket counters from existing ticket numbers
  SELECT COALESCE(MAX(CAST(split_part(ticket_number, '-', 2) AS int)), 0) INTO v_rwb_seq
  FROM metal_link.receiving_tickets WHERE ticket_number LIKE 'RWB-%';
  SELECT COALESCE(MAX(CAST(split_part(ticket_number, '-', 2) AS int)), 0) INTO v_rpl_seq
  FROM metal_link.receiving_tickets WHERE ticket_number LIKE 'RPL-%';
  SELECT COALESCE(MAX(CAST(split_part(ticket_number, '-', 2) AS int)), 0) INTO v_swb_seq
  FROM metal_link.sending_tickets WHERE ticket_number LIKE 'SWB-%';
  SELECT COALESCE(MAX(CAST(split_part(ticket_number, '-', 2) AS int)), 0) INTO v_spl_seq
  FROM metal_link.sending_tickets WHERE ticket_number LIKE 'SPL-%';

  -- Loop over each day
  v_current_date := v_start_date;
  WHILE v_current_date <= v_end_date LOOP
    v_is_future := v_current_date > v_today;
    v_is_active := NOT v_is_future;

    -- Only Mon-Fri
    IF EXTRACT(DOW FROM v_current_date) BETWEEN 1 AND 5 THEN
      -- Purchases (receiving)
      v_purchase_tickets_per_day := 60 + floor(random() * 21)::int; -- 60-80
      FOR i IN 1..v_purchase_tickets_per_day LOOP
        -- Random customer
        v_customer_id := v_customers[1 + floor(random() * array_length(v_customers, 1))::int];

        -- Random ticket type: 1=weighbridge, 2=platform
        v_ticket_type_id := CASE WHEN random() < 0.5 THEN 1 ELSE 2 END;

        -- Build ticket number
        IF v_ticket_type_id = 1 THEN
          v_rwb_seq := v_rwb_seq + 1;
          v_ticket_number := 'RWB-' || lpad(v_rwb_seq::text, 8, '0');
        ELSE
          v_rpl_seq := v_rpl_seq + 1;
          v_ticket_number := 'RPL-' || lpad(v_rpl_seq::text, 8, '0');
        END IF;

        -- Insert ticket
        INSERT INTO metal_link.receiving_tickets (
          customer_id, ticket_type_id, ticket_number, net_weight_kg, ticket_state, 
          driver_name, vehicle_registration, trailer_registration, notes, 
          ofm_weighbridge_ticket, ck_number, delivery_number, foreign_ticket,
          created_time, updated_time, created_by_operator_id, is_active
        )
        VALUES (
          v_customer_id, v_ticket_type_id, v_ticket_number, 0, 'C',
          CASE WHEN v_ticket_type_id = 1 THEN 'Driver ' || (1000 + floor(random() * 9000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'REG' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'TRL' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'Sample notes for weighbridge ticket' ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'OFM' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'CK' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'DEL' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'FRN' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 
          v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 
          1, v_is_active
        )
        RETURNING receiving_ticket_id INTO v_ticket_id;

        v_total_weight := 0;
        v_lines_per_purchase_ticket := 10 + floor(random() * 5)::int; -- 10-14
        FOR j IN 1..v_lines_per_purchase_ticket LOOP
          -- Random product
          v_product_id := v_products[1 + floor(random() * array_length(v_products, 1))::int];

          -- Random price list for product (assuming customer 'C')
          SELECT product_price_list_id INTO v_price_list_id
          FROM metal_link.product_price_list_product_prices
          WHERE product_id = v_product_id AND product_price_list_id IN (
            SELECT product_price_list_id FROM metal_link.product_price_lists WHERE entity_flag = 'C'
          )
          ORDER BY random() LIMIT 1;

          -- If no price, skip
          IF v_price_list_id IS NULL THEN CONTINUE; END IF;

          -- Get price id
          SELECT product_price_list_product_price_id INTO v_price_id
          FROM metal_link.product_price_list_product_prices
          WHERE product_id = v_product_id AND product_price_list_id = v_price_list_id LIMIT 1;

          -- Random weight based on ticket type
          IF v_ticket_type_id = 2 THEN
            v_weight := 10 + random() * 790; -- Platform: 10-800kg
          ELSE
            v_weight := 200 + random() * 3800; -- Weighbridge: 200-4000kg
          END IF;

          -- Get price
          SELECT price INTO v_unit_price FROM metal_link.product_price_list_product_prices WHERE product_price_list_product_price_id = v_price_id;

          -- Insert line
          INSERT INTO metal_link.receiving_ticket_lines (receiving_ticket_id, product_id, net_weight_kg, unit_price_per_kg, product_price_list_product_price_id, created_time, updated_time, created_by_operator_id, is_active)
          VALUES (v_ticket_id, v_product_id, v_weight, v_unit_price, v_price_id, v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 1, v_is_active)
          RETURNING receiving_ticket_line_id INTO v_ticket_line_id;

          -- Update stock level: find the latest active row for this price and update, or insert new
          UPDATE metal_link.stock_levels 
          SET weight_kg = weight_kg + v_weight, updated_time = v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours')
          WHERE product_price_list_product_price_id = v_price_id AND is_active = true
          AND stock_level_id = (
            SELECT stock_level_id FROM metal_link.stock_levels 
            WHERE product_price_list_product_price_id = v_price_id AND is_active = true 
            ORDER BY created_time DESC LIMIT 1
          );
          
          IF NOT FOUND THEN
            INSERT INTO metal_link.stock_levels (product_id, product_price_list_product_price_id, weight_kg, created_time, updated_time, created_by_operator_id, is_active)
            VALUES (v_product_id, v_price_id, v_weight, v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 1, v_is_active);
          END IF;

          -- Get base weight from current stock level
          SELECT COALESCE(
            (SELECT weight_kg FROM metal_link.stock_levels
             WHERE product_price_list_product_price_id = v_price_id AND is_active = true
             ORDER BY created_time DESC LIMIT 1),
            0
          ) INTO v_base_weight;

          -- Insert stock movement
          INSERT INTO metal_link.stock_movements (product_id, receiving_ticket_id, receiving_ticket_line_id, base_weight_kg, buy_weight_kg, product_price_list_id, product_price_list_product_price_id, created_time, updated_time, created_by_operator_id, is_active)
          VALUES (v_product_id, v_ticket_id, v_ticket_line_id, v_base_weight, v_weight, v_price_list_id, v_price_id, v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 1, v_is_active);

          v_total_weight := v_total_weight + v_weight;
        END LOOP;

        -- Update ticket total
        UPDATE metal_link.receiving_tickets SET net_weight_kg = v_total_weight WHERE receiving_ticket_id = v_ticket_id;
      END LOOP;

      -- Sales (sending)
      v_sale_tickets_per_day := 4 + floor(random() * 7)::int; -- 4-10
      FOR i IN 1..v_sale_tickets_per_day LOOP
        -- Random buyer
        v_buyer_id := v_buyers[1 + floor(random() * array_length(v_buyers, 1))::int];

        -- Random ticket type: 1=weighbridge, 2=platform
        v_ticket_type_id := CASE WHEN random() < 0.5 THEN 1 ELSE 2 END;

        -- Build ticket number
        IF v_ticket_type_id = 1 THEN
          v_swb_seq := v_swb_seq + 1;
          v_ticket_number := 'SWB-' || lpad(v_swb_seq::text, 8, '0');
        ELSE
          v_spl_seq := v_spl_seq + 1;
          v_ticket_number := 'SPL-' || lpad(v_spl_seq::text, 8, '0');
        END IF;

        -- Insert ticket
        INSERT INTO metal_link.sending_tickets (
          buyer_id, ticket_type_id, ticket_number, net_weight_kg, ticket_state,
          driver_name, vehicle_registration, trailer_registration, notes,
          ofm_weighbridge_ticket, ck_number, delivery_number, foreign_ticket,
          created_time, updated_time, created_by_operator_id, is_active
        )
        VALUES (
          v_buyer_id, v_ticket_type_id, v_ticket_number, 0, 'C',
          CASE WHEN v_ticket_type_id = 1 THEN 'Driver ' || (1000 + floor(random() * 9000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'REG' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'TRL' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'Sample notes for sending weighbridge ticket' ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'OFM' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'CK' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'DEL' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          CASE WHEN v_ticket_type_id = 1 THEN 'FRN' || (100000 + floor(random() * 900000)::int)::text ELSE NULL END,
          v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'),
          v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'),
          1, v_is_active
        )
        RETURNING sending_ticket_id INTO v_ticket_id;

        v_total_weight := 0;
        v_lines_per_sale_ticket := 4 + floor(random() * 15)::int; -- 4-18
        FOR j IN 1..v_lines_per_sale_ticket LOOP
          -- Random product
          v_product_id := v_products[1 + floor(random() * array_length(v_products, 1))::int];

          -- Random price list for product (buyer 'B')
          SELECT product_price_list_id INTO v_price_list_id
          FROM metal_link.product_price_list_product_prices
          WHERE product_id = v_product_id AND product_price_list_id IN (
            SELECT product_price_list_id FROM metal_link.product_price_lists WHERE entity_flag = 'B'
          )
          ORDER BY random() LIMIT 1;

          -- If no price, skip
          IF v_price_list_id IS NULL THEN CONTINUE; END IF;

          -- Get price id
          SELECT product_price_list_product_price_id INTO v_price_id
          FROM metal_link.product_price_list_product_prices
          WHERE product_id = v_product_id AND product_price_list_id = v_price_list_id LIMIT 1;

          -- Check if enough stock
          -- For simplicity, assume always enough, or adjust weight
          SELECT weight_kg INTO v_weight FROM metal_link.stock_levels WHERE product_price_list_product_price_id = v_price_id AND is_active = true;
          IF v_weight IS NULL OR v_weight < 200 THEN
            v_weight := 200 + random() * 3800; -- 200-4000
          ELSE
            v_weight := 200 + random() * LEAST(3800, v_weight - 200);
          END IF;

          -- Get price
          SELECT price INTO v_unit_price FROM metal_link.product_price_list_product_prices WHERE product_price_list_product_price_id = v_price_id;

          -- Insert line
          INSERT INTO metal_link.sending_ticket_lines (sending_ticket_id, product_id, net_weight_kg, unit_price_per_kg, product_price_list_product_price_id, created_time, updated_time, created_by_operator_id, is_active)
          VALUES (v_ticket_id, v_product_id, v_weight, v_unit_price, v_price_id, v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 1, v_is_active)
          RETURNING sending_ticket_line_id INTO v_ticket_line_id;

          -- Update stock level: find the latest active row for this price and update
          UPDATE metal_link.stock_levels 
          SET weight_kg = weight_kg - v_weight, updated_time = v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours')
          WHERE product_price_list_product_price_id = v_price_id AND is_active = true
          AND stock_level_id = (
            SELECT stock_level_id FROM metal_link.stock_levels 
            WHERE product_price_list_product_price_id = v_price_id AND is_active = true 
            ORDER BY created_time DESC LIMIT 1
          );

          -- Get base weight from current stock level
          SELECT COALESCE(
            (SELECT weight_kg FROM metal_link.stock_levels
             WHERE product_price_list_product_price_id = v_price_id AND is_active = true
             ORDER BY created_time DESC LIMIT 1),
            0
          ) INTO v_base_weight;

          -- Insert stock movement
          INSERT INTO metal_link.stock_movements (product_id, sending_ticket_id, sending_ticket_line_id, base_weight_kg, sell_weight_kg, product_price_list_id, product_price_list_product_price_id, created_time, updated_time, created_by_operator_id, is_active)
          VALUES (v_product_id, v_ticket_id, v_ticket_line_id, v_base_weight, v_weight, v_price_list_id, v_price_id, v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), v_current_date + INTERVAL '9 hours' + (random() * INTERVAL '8 hours'), 1, v_is_active);

          v_total_weight := v_total_weight + v_weight;
        END LOOP;

        -- Update ticket total
        UPDATE metal_link.sending_tickets SET net_weight_kg = v_total_weight WHERE sending_ticket_id = v_ticket_id;
      END LOOP;
    END IF;

    v_current_date := v_current_date + 1;
  END LOOP;

END
$$ LANGUAGE plpgsql;

COMMIT;