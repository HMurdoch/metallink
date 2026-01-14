using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetalLink.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateExistingTicketsToReceiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate existing tickets where Status = 'receiving' and CustomerId is not null
            // to the new tickets_receiving table
            migrationBuilder.Sql(@"
                INSERT INTO metal_link.tickets_receiving (
                    company_id,
                    site_id,
                    customer_id,
                    ticket_number,
                    ticket_type,
                    first_weight_kg,
                    second_weight_kg,
                    net_weight_kg,
                    unit_price_per_kg,
                    total_amount,
                    currency_code,
                    product_id,
                    product_description,
                    vehicle_registration,
                    trailer_registration,
                    driver_name,
                    ofm_weighbridge_ticket,
                    foreign_ticket,
                    ck_number,
                    delivery_number,
                    rfid_tag,
                    delivery_status,
                    notes,
                    is_active,
                    created_time,
                    updated_time,
                    created_by_operator_id
                )
                SELECT 
                    COALESCE(c.company_id, s.company_id, 1) as company_id,
                    t.site_id,
                    t.customer_id,
                    t.ticket_number,
                    t.ticket_type,
                    t.first_weight_kg,
                    t.second_weight_kg,
                    t.net_weight_kg,
                    t.unit_price_per_kg,
                    t.total_amount_ex_vat as total_amount,
                    t.currency_code,
                    t.product_id,
                    t.product_description,
                    t.vehicle_registration,
                    t.trailer_registration,
                    t.driver_name,
                    t.ofm_weighbridge_ticket,
                    t.foreign_ticket,
                    t.ck_number,
                    t.delivery_number,
                    t.rfid_card_number as rfid_tag,
                    'completed' as delivery_status,
                    t.notes,
                    t.is_active,
                    t.created_time,
                    t.updated_time,
                    t.operator_id as created_by_operator_id
                FROM metal_link.tickets t
                LEFT JOIN metal_link.customers c ON t.customer_id = c.customer_id
                LEFT JOIN metal_link.sites s ON t.site_id = s.site_id
                WHERE t.status = 'receiving' 
                  AND t.customer_id IS NOT NULL
                  AND t.is_active = true;
            ");

            // Create stock movements for migrated receiving tickets
            migrationBuilder.Sql(@"
                INSERT INTO metal_link.stock_movements_receiving (
                    site_id,
                    product_id,
                    ticket_receiving_id,
                    ticket_receiving_line_id,
                    quantity_kg,
                    unit_price_per_kg,
                    total_value,
                    currency_code,
                    ticket_number,
                    customer_id,
                    customer_name,
                    notes,
                    is_active,
                    movement_date,
                    created_time,
                    updated_time
                )
                SELECT 
                    tr.site_id,
                    COALESCE(tr.product_id, 1) as product_id,
                    tr.ticket_receiving_id,
                    NULL as ticket_receiving_line_id,
                    tr.net_weight_kg as quantity_kg,
                    tr.unit_price_per_kg,
                    tr.total_amount as total_value,
                    tr.currency_code,
                    tr.ticket_number,
                    tr.customer_id,
                    COALESCE(c.first_name || ' ' || c.last_name, 'Unknown Customer') as customer_name,
                    tr.notes,
                    tr.is_active,
                    tr.created_time as movement_date,
                    tr.created_time,
                    tr.updated_time
                FROM metal_link.tickets_receiving tr
                LEFT JOIN metal_link.customers c ON tr.customer_id = c.customer_id
                WHERE tr.product_id IS NOT NULL;
            ");

            // Initialize stock_on_hand for products that were received
            migrationBuilder.Sql(@"
                INSERT INTO metal_link.stock_on_hand (
                    site_id,
                    product_id,
                    quantity_on_hand_kg,
                    total_received_kg,
                    total_sent_kg,
                    average_unit_cost,
                    total_value,
                    last_movement_date,
                    last_movement_type,
                    created_time,
                    updated_time
                )
                SELECT 
                    smr.site_id,
                    smr.product_id,
                    SUM(smr.quantity_kg) as quantity_on_hand_kg,
                    SUM(smr.quantity_kg) as total_received_kg,
                    0 as total_sent_kg,
                    AVG(smr.unit_price_per_kg) as average_unit_cost,
                    SUM(smr.total_value) as total_value,
                    MAX(smr.movement_date) as last_movement_date,
                    'receiving' as last_movement_type,
                    NOW() as created_time,
                    NOW() as updated_time
                FROM metal_link.stock_movements_receiving smr
                WHERE smr.is_active = true
                GROUP BY smr.site_id, smr.product_id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove migrated stock data
            migrationBuilder.Sql(@"
                DELETE FROM metal_link.stock_on_hand;
            ");

            migrationBuilder.Sql(@"
                DELETE FROM metal_link.stock_movements_receiving;
            ");

            migrationBuilder.Sql(@"
                DELETE FROM metal_link.tickets_receiving;
            ");
        }
    }
}
