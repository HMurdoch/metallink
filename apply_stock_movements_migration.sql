-- Migration: AddTicketReferenceColumnsToStockMovements
-- Apply the missing ticket reference columns to stock_movements table

ALTER TABLE metal_link.stock_movements
ADD COLUMN IF NOT EXISTS receiving_ticket_id INTEGER;

ALTER TABLE metal_link.stock_movements
ADD COLUMN IF NOT EXISTS receiving_ticket_line_id INTEGER;

ALTER TABLE metal_link.stock_movements
ADD COLUMN IF NOT EXISTS sending_ticket_id INTEGER;

ALTER TABLE metal_link.stock_movements
ADD COLUMN IF NOT EXISTS sending_ticket_line_id INTEGER;

-- Create indexes
CREATE INDEX IF NOT EXISTS IX_stock_movements_receiving_ticket_id
ON metal_link.stock_movements (receiving_ticket_id);

CREATE INDEX IF NOT EXISTS IX_stock_movements_receiving_ticket_line_id
ON metal_link.stock_movements (receiving_ticket_line_id);

CREATE INDEX IF NOT EXISTS IX_stock_movements_sending_ticket_id
ON metal_link.stock_movements (sending_ticket_id);

CREATE INDEX IF NOT EXISTS IX_stock_movements_sending_ticket_line_id
ON metal_link.stock_movements (sending_ticket_line_id);

-- Add foreign key constraints
ALTER TABLE metal_link.stock_movements
ADD CONSTRAINT IF NOT EXISTS FK_stock_movements_receiving_tickets_receiving_ticket_id
FOREIGN KEY (receiving_ticket_id)
REFERENCES metal_link.receiving_tickets (receiving_ticket_id)
ON DELETE SET NULL;

ALTER TABLE metal_link.stock_movements
ADD CONSTRAINT IF NOT EXISTS FK_stock_movements_receiving_ticket_lines_receiving_ticket_line_id
FOREIGN KEY (receiving_ticket_line_id)
REFERENCES metal_link.receiving_ticket_lines (receiving_ticket_line_id)
ON DELETE SET NULL;

ALTER TABLE metal_link.stock_movements
ADD CONSTRAINT IF NOT EXISTS FK_stock_movements_sending_tickets_sending_ticket_id
FOREIGN KEY (sending_ticket_id)
REFERENCES metal_link.sending_tickets (sending_ticket_id)
ON DELETE SET NULL;

ALTER TABLE metal_link.stock_movements
ADD CONSTRAINT IF NOT EXISTS FK_stock_movements_sending_ticket_lines_sending_ticket_line_id
FOREIGN KEY (sending_ticket_line_id)
REFERENCES metal_link.sending_ticket_lines (sending_ticket_line_id)
ON DELETE SET NULL;