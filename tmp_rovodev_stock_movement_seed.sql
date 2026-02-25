-- Seed Stock Movements sample data (MetalLink)
--
-- WHAT THIS DOES
-- - For each active product, generate 20-80 stock_movements over the last 30 days.
-- - Each movement follows your flow:
--     base_weight_kg = current stock_levels.weight_kg (or 0 if none)
--     buy_weight_kg / sell_weight_kg populated (one side per row)
--     stock_levels.weight_kg updated to (base + buy - sell)
-- - created_time is spread over the last month.
--
-- SAFETY
-- - Uses a transaction.
-- - Does NOT delete/overwrite existing movements.
-- - Only INSERTs new movements, and UPSERTs stock_levels rows per product.
-- - Assumes operator_id = 1 exists. Change v_operator_id if needed.
--
-- IMPORTANT
-- Your stock_levels schema (\d metal_link.stock_levels) shows:
--   stock_level_id (PK), product_id (NOT UNIQUE), weight_kg, created_by_operator_id (NOT NULL)
-- So we CANNOT use ON CONFLICT(product_id). Instead we:
--   - update the newest active stock_levels row for that product, or
--   - insert a new stock_levels row if none exists.
--
-- Also: we generate movements in chronological order per product so the graph looks realistic.

BEGIN;

DO $$
DECLARE
  v_operator_id int := 1;
  v_now timestamptz := now();
  v_start timestamptz := now() - interval '30 days';

  r record;
  v_existing_weight numeric(18,2);
  v_base numeric(18,2);
  v_buy numeric(18,2);
  v_sell numeric(18,2);
  v_new numeric(18,2);
  v_moves int;
  i int;
  v_ts timestamptz;
  v_is_buy boolean;
  v_has_level boolean;
BEGIN
  -- Optional: deterministic randomness per run
  -- PERFORM setseed(0.4242);

  FOR r IN
    SELECT p.product_id
    FROM metal_link.products p
    WHERE p.is_active = true
    ORDER BY p.product_id
  LOOP
    -- Ensure an active stock_levels row exists for this product (seed a reasonable starting stock)
    SELECT EXISTS(
      SELECT 1
      FROM metal_link.stock_levels sl
      WHERE sl.product_id = r.product_id
        AND sl.is_active = true
    ) INTO v_has_level;

    IF NOT v_has_level THEN
      INSERT INTO metal_link.stock_levels
        (product_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
      VALUES
        (r.product_id, round((500 + random() * 2500)::numeric, 2), v_operator_id, true, v_start, v_start);
    END IF;
    -- number of movements for this product
    v_moves := (20 + floor(random() * 61))::int; -- 20..80

    FOR i IN 1..v_moves LOOP
      -- Movement timestamp: evenly spread & ordered over last month
      v_ts := v_start + (((i - 1)::double precision / GREATEST(v_moves - 1, 1)) * (v_now - v_start))
              + ((random() - 0.5) * interval '6 hours');
      IF v_ts < v_start THEN v_ts := v_start; END IF;
      IF v_ts > v_now THEN v_ts := v_now; END IF;

      -- load current stock level (treat missing as 0)
      SELECT COALESCE(sl.weight_kg, 0)
      INTO v_existing_weight
      FROM metal_link.stock_levels sl
      WHERE sl.product_id = r.product_id
        AND sl.is_active = true
      ORDER BY sl.updated_time DESC
      LIMIT 1;

      v_base := COALESCE(v_existing_weight, 0);

      -- choose buy vs sell (bias towards buys)
      v_is_buy := (random() < 0.60);

      IF v_is_buy THEN
        v_buy := round((10 + random() * 400)::numeric, 2); -- 10..410 kg
        v_sell := 0;
      ELSE
        -- don't allow selling more than current base
        IF v_base <= 0 THEN
          v_buy := round((10 + random() * 400)::numeric, 2);
          v_sell := 0;
        ELSE
          v_sell := round(LEAST(v_base, (10 + random() * 400)::numeric), 2);
          v_buy := 0;
        END IF;
      END IF;

      v_new := round((v_base + v_buy - v_sell)::numeric, 2);

      INSERT INTO metal_link.stock_movements
        (product_id, base_weight_kg, buy_weight_kg, sell_weight_kg,
         created_by_operator_id, is_active, created_time, updated_time, notes)
      VALUES
        (r.product_id, v_base, v_buy, v_sell,
         v_operator_id, true, v_ts, v_ts,
         'Seeded sample movement');

      -- Update the most recent active stock_levels row if it exists, else insert.
      SELECT EXISTS(
        SELECT 1
        FROM metal_link.stock_levels sl
        WHERE sl.product_id = r.product_id
          AND sl.is_active = true
      ) INTO v_has_level;

      IF v_has_level THEN
        UPDATE metal_link.stock_levels
        SET weight_kg = v_new,
            updated_time = v_ts
        WHERE stock_level_id = (
          SELECT sl.stock_level_id
          FROM metal_link.stock_levels sl
          WHERE sl.product_id = r.product_id
            AND sl.is_active = true
          ORDER BY sl.updated_time DESC
          LIMIT 1
        );
      ELSE
        INSERT INTO metal_link.stock_levels
          (product_id, weight_kg, created_by_operator_id, is_active, created_time, updated_time)
        VALUES
          (r.product_id, v_new, v_operator_id, true, v_ts, v_ts);
      END IF;

    END LOOP;
  END LOOP;
END $$;

COMMIT;
