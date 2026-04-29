-- Soft delete existing data for reseeding
-- Mark all records as inactive before generating new seed data

BEGIN;

-- Soft delete existing ticket lines
UPDATE metal_link.receiving_ticket_lines SET is_active = false WHERE is_active = true;
UPDATE metal_link.sending_ticket_lines SET is_active = false WHERE is_active = true;

-- Soft delete existing stock levels
UPDATE metal_link.stock_levels SET is_active = false WHERE is_active = true;

-- Soft delete existing stock movements
UPDATE metal_link.stock_movements SET is_active = false WHERE is_active = true;

COMMIT;