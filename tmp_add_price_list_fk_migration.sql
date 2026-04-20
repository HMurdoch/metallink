-- Migration: Add product_price_list_id FK to stock_movements, receiving_ticket_lines, sending_ticket_lines
--
-- PURPOSE
--   Links each stock movement and ticket line to the price list that was in use
--   when the transaction occurred, enabling price-list-aware reporting.
--
-- TABLES AFFECTED
--   metal_link.stock_movements         (unified movement log – buy_weight_kg / sell_weight_kg)
--   metal_link.receiving_ticket_lines  (customer buy lines  – entity_flag = 'C')
--   metal_link.sending_ticket_lines    (buyer  sell lines   – entity_flag = 'B')
--
-- BACKFILL STRATEGY FOR EXISTING ROWS
--   We cannot know which specific price list was selected at transaction time,
--   so we assign the alphabetically-first active C/B price list as the default.
--   You can refine this afterwards with a targeted UPDATE if a better mapping exists.
--
-- SAFETY
--   Wrapped in a single transaction; rolls back completely on any error.
--   Uses ADD COLUMN IF NOT EXISTS so it is re-runnable without error.
--   DROP CONSTRAINT IF EXISTS before ADD ensures idempotency.

BEGIN;

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. stock_movements  (not an EF-mapped entity; managed via raw SQL)
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE metal_link.stock_movements
    ADD COLUMN IF NOT EXISTS product_price_list_id INTEGER;

ALTER TABLE metal_link.stock_movements
    ADD COLUMN IF NOT EXISTS unit_price_per_kg NUMERIC(18,4) NOT NULL DEFAULT 0;

-- Re-create FK (drop first for idempotency)
ALTER TABLE metal_link.stock_movements
    DROP CONSTRAINT IF EXISTS fk_stock_movements_price_list;

ALTER TABLE metal_link.stock_movements
    ADD CONSTRAINT fk_stock_movements_price_list
    FOREIGN KEY (product_price_list_id)
    REFERENCES metal_link.product_price_lists(product_price_list_id)
    ON DELETE SET NULL;

-- Backfill: buy movements → first Customer (C) price list
UPDATE metal_link.stock_movements sm
SET    product_price_list_id = (
           SELECT product_price_list_id
           FROM   metal_link.product_price_lists
           WHERE  entity_flag = 'C' AND is_active = true
           ORDER  BY product_price_list_name
           LIMIT  1
       )
WHERE  sm.product_price_list_id IS NULL
  AND  sm.buy_weight_kg > 0;

-- Backfill: sell movements → first Buyer (B) price list
UPDATE metal_link.stock_movements sm
SET    product_price_list_id = (
           SELECT product_price_list_id
           FROM   metal_link.product_price_lists
           WHERE  entity_flag = 'B' AND is_active = true
           ORDER  BY product_price_list_name
           LIMIT  1
       )
WHERE  sm.product_price_list_id IS NULL
  AND  sm.sell_weight_kg > 0;

-- ─────────────────────────────────────────────────────────────────────────────
-- 1b. stock_movement_receiving / stock_movement_sending  (EF-managed tables)
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE metal_link.stock_movement_receiving
    ADD COLUMN IF NOT EXISTS product_price_list_id INTEGER;

ALTER TABLE metal_link.stock_movement_receiving
    DROP CONSTRAINT IF EXISTS fk_smr_price_list;

ALTER TABLE metal_link.stock_movement_receiving
    ADD CONSTRAINT fk_smr_price_list
    FOREIGN KEY (product_price_list_id)
    REFERENCES metal_link.product_price_lists(product_price_list_id)
    ON DELETE SET NULL;

-- Backfill receiving movements → first Customer price list
UPDATE metal_link.stock_movement_receiving smr
SET    product_price_list_id = (
           SELECT product_price_list_id
           FROM   metal_link.product_price_lists
           WHERE  entity_flag = 'C' AND is_active = true
           ORDER  BY product_price_list_name
           LIMIT  1
       )
WHERE  smr.product_price_list_id IS NULL;

ALTER TABLE metal_link.stock_movement_sending
    ADD COLUMN IF NOT EXISTS product_price_list_id INTEGER;

ALTER TABLE metal_link.stock_movement_sending
    DROP CONSTRAINT IF EXISTS fk_sms_price_list;

ALTER TABLE metal_link.stock_movement_sending
    ADD CONSTRAINT fk_sms_price_list
    FOREIGN KEY (product_price_list_id)
    REFERENCES metal_link.product_price_lists(product_price_list_id)
    ON DELETE SET NULL;

-- Backfill sending movements → first Buyer price list
UPDATE metal_link.stock_movement_sending sms
SET    product_price_list_id = (
           SELECT product_price_list_id
           FROM   metal_link.product_price_lists
           WHERE  entity_flag = 'B' AND is_active = true
           ORDER  BY product_price_list_name
           LIMIT  1
       )
WHERE  sms.product_price_list_id IS NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. receiving_ticket_lines  (Customer – entity_flag = 'C')
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE metal_link.receiving_ticket_lines
    ADD COLUMN IF NOT EXISTS product_price_list_id INTEGER;

ALTER TABLE metal_link.receiving_ticket_lines
    DROP CONSTRAINT IF EXISTS fk_recv_ticket_lines_price_list;

ALTER TABLE metal_link.receiving_ticket_lines
    ADD CONSTRAINT fk_recv_ticket_lines_price_list
    FOREIGN KEY (product_price_list_id)
    REFERENCES metal_link.product_price_lists(product_price_list_id)
    ON DELETE SET NULL;

-- Backfill → first Customer price list
UPDATE metal_link.receiving_ticket_lines rtl
SET    product_price_list_id = (
           SELECT product_price_list_id
           FROM   metal_link.product_price_lists
           WHERE  entity_flag = 'C' AND is_active = true
           ORDER  BY product_price_list_name
           LIMIT  1
       )
WHERE  rtl.product_price_list_id IS NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. sending_ticket_lines  (Buyer – entity_flag = 'B')
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE metal_link.sending_ticket_lines
    ADD COLUMN IF NOT EXISTS product_price_list_id INTEGER;

ALTER TABLE metal_link.sending_ticket_lines
    DROP CONSTRAINT IF EXISTS fk_send_ticket_lines_price_list;

ALTER TABLE metal_link.sending_ticket_lines
    ADD CONSTRAINT fk_send_ticket_lines_price_list
    FOREIGN KEY (product_price_list_id)
    REFERENCES metal_link.product_price_lists(product_price_list_id)
    ON DELETE SET NULL;

-- Backfill → first Buyer price list
UPDATE metal_link.sending_ticket_lines stl
SET    product_price_list_id = (
           SELECT product_price_list_id
           FROM   metal_link.product_price_lists
           WHERE  entity_flag = 'B' AND is_active = true
           ORDER  BY product_price_list_name
           LIMIT  1
       )
WHERE  stl.product_price_list_id IS NULL;

COMMIT;
