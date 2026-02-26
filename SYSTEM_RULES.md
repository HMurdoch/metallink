# MetalLink System Business Rules

## ⚠️ CRITICAL - NB!!!!!

### Git Operations
- **NEVER** make commit messages or commit anything to Git unless explicitly asked
- **NEVER** perform hard-resets
- Always ask for permission before any Git operations

This document defines the core business rules that govern the MetalLink system. These rules are permanent and must not be deleted. All code implementations must comply with these rules.

## 1. Ticket Types
- **Type 1 (Weighbridge/RWB/SWB)**: Uses weight scales with first_weight_kg and second_weight_kg
- **Type 2 (Platform/RPL/SPL)**: Does not use weight scales; weight-related fields are NULL

## 2. Ticket Number Generation
When creating a new ticket (receiving or sending), the system must:
1. Determine the ticket type (1 = Weighbridge, 2 = Platform)
2. Find the highest existing ticket number for that type
3. Increment by 1 and assign the next number

**Prefixes:**
- Receiving Weighbridge: `RWB-` (e.g., RWB-00000001)
- Receiving Platform: `RPL-` (e.g., RPL-00000001)
- Sending Weighbridge: `SWB-` (e.g., SWB-00000001)
- Sending Platform: `SPL-` (e.g., SPL-00000001)

Example: If highest RWB is RWB-00000004, next will be RWB-00000005.

## 3. Invoice Number and Account Number Uniqueness
Invoice numbers and account numbers must be unique across BOTH receiving_tickets and sending_tickets tables.
- When creating a new ticket, query the highest invoice_number/account_number across both tables
- Increment that value by 1 for the new ticket
- Example: If receiving_tickets has [1, 3, 4, 7] and sending_tickets has [2, 5, 6], highest is 7, next is 8

## 4. Platform Ticket Fields (Type 2)
When ticket_type_id = 2 (Platform), the following fields must be NULL and disabled in the UI:
- first_weight_kg
- second_weight_kg
- driver_name
- vehicle_registration
- trailer_registration
- ofm_weighbridge_ticket
- delivery_number

## 5. Unit Price Per Kg Lookup
When creating ticket line items, unit_price_per_kg is determined by:
1. Get the customer/buyer's price_code (A, B, or C)
2. Look up the product in the prices table using product_id
3. Use the corresponding price column:
   - If price_code = 'A': use `price_a` from prices table
   - If price_code = 'B': use `price_b` from prices table
   - If price_code = 'C': use `price_c` from prices table

**Requirement**: Every customer and buyer must have a price_code assigned.

## 6. Company and Site Validation
When creating a customer or buyer:
- If is_company = true (buyers are always companies), then:
  - A company_id MUST be selected
  - A site_id MUST be selected from sites where site.company_id = selected company_id

## 7. Net Weight Calculation
- **For Weighbridge Tickets (Type 1)**:
  - Receiving: `net_weight_kg = first_weight_kg - second_weight_kg`
  - Sending: `net_weight_kg = second_weight_kg - first_weight_kg`
  - Ticket-level net_weight = SUM of all line_item.net_weight_kg

- **For Platform Tickets (Type 2)**:
  - first_weight_kg and second_weight_kg are NULL
  - Ticket-level net_weight = SUM of all line_item.net_weight_kg (only source of weight)

## 8. Financial Calculations
For each ticket, calculations must be performed as follows:
- **Total (ex. VAT)** = SUM(line_item.net_weight_kg × line_item.unit_price_per_kg) for all line items
- **VAT (15%)** = Total (ex. VAT) × 0.15
- **Total (incl. VAT)** = Total (ex. VAT) + VAT

## 9. Audit Trail - Updated Time
Every time any record is modified (created or updated) in any table:
- The `updated_time` field must be set to the current UTC time (now())
- Initial creation also sets `updated_time = now()`

### Mandatory Audit Columns (System-Wide)
All tables must include (and API code must populate) the following columns:
- `is_active`
- `created_by_operator_id`
- `created_time`
- `updated_time`

### Automatic Enforcement
- `updated_time` is enforced at the database level via a Postgres trigger (`metal_link.set_updated_time`) attached to all tables in the `metal_link` schema that contain an `updated_time` column.
- Application code should still set `updated_time` where practical, but the database trigger is the source of truth (covers raw SQL too).

## 10. Customer and Buyer Data Generation
When seeding customer or buyer data:
- **Email**: Format `[first_name][last_name]@email.com` (lowercase, no spaces)
- **Phone Number**: Format `+2711` followed by 7 random digits (12 characters total)
- **Mobile Number**: Format `+2781` followed by 7 random digits (12 characters total)

## 11. Company Classification
- **receiving_sending_flag = 'R'**: Company is a Receiving company
- **receiving_sending_flag = 'S'**: Company is a Sending company

## 12. Site Code Generation
When creating a new site for a company:
1. Find the highest existing site_code for that company
2. Extract the numeric portion
3. Increment by 1
4. Prefix with `SITE-`

Example: If a company has SITE-1, SITE-2, SITE-3, the next site will be SITE-4.

## 13. Receiving and Sending Tickets - Price Values and Line Items
For **Receiving Tickets** and **Sending Tickets** response DTOs:
- **NO financial calculations** (VAT, ex. VAT, inc. VAT) should be included in ticket results
- These calculations are specific to **Ticket Line Items ONLY**, not at the Ticket level
- The ticket response should include **Company Name** and **Site Name** for reference
- **Sending tickets should NOT include**: TotalExVat, VatAmount, TotalIncVat
- **Receiving tickets should NOT include**: Any financial totals (maintain consistency)
- Line items include only: ProductName, NetWeightKg, UnitPricePerKg, LineTotal, and Notes

## 14. Product Selection - Specific to Line Items Only
Products, Product Search, and Product Results are ONLY relevant at the **Line Item level**, NOT at the Ticket level:
- Product selection is specific to individual line items
- Product information (code, name) is NOT part of the ticket itself
- Search and selection should ONLY occur when creating/editing line items
- Ticket creation/editing should NOT include any product-related fields, product search, or product dropdown

## 15. Weight Calculation Specifics
- **Receiving Tickets**: `net_weight_kg = first_weight_kg - second_weight_kg`
- **Sending Tickets**: `net_weight_kg = second_weight_kg - first_weight_kg`
- **Ticket-level net_weight**: SUM of all line_item.net_weight_kg
- First Weight and Second Weight are populated via "Read Weight" button (weighbridge or platform scale)
- Line item weights are also populated via "Read Weight" button and are read-only until populated

## 16. Line Item Financial Calculations
For each line item in a Receiving or Sending Ticket:
- **LineTotal (ex. VAT)** = net_weight_kg × unit_price_per_kg
- **VAT (15%)** = LineTotal (ex. VAT) × 0.15
- **LineTotal (incl. VAT)** = LineTotal (ex. VAT) + VAT (15%)
- These calculations are specific to line items and should NOT appear at the ticket level

## 17. Current Development Focus - Receiving Tickets Only
**⚠️ CURRENT RULE**: Focus ALL development efforts on **Receiving Tickets ONLY**.

Do NOT make changes to Sending Ticket functionality at this time. Once Receiving Tickets are fully implemented and stable, we will copy the implementation pattern to Sending Tickets with appropriate adjustments.

**Future Note**: After Receiving Tickets are complete, rule 17 will be updated to enforce Ticket Feature Parity between Receiving and Sending.

## Implementation Status

### ✅ Implemented Services
- **TicketNumberService**: Generates RWB, RPL, SWB, SPL ticket numbers
- **PriceLookupService**: Looks up prices by product_id and price_code (A/B/C)
- **TicketCalculationService**: Calculates totals, VAT, and totals with VAT
- **WeightCalculationService**: Validates and calculates weights for weighbridge vs platform
- **SiteCodeGeneratorService**: Generates SITE-N codes for new sites
- **CompanyValidationService**: Validates company/site relationships

### ⏳ Requires API/Controller Implementation
- Rule 4: Disable platform ticket fields (API validation when creating tickets)
- Rule 9: Update updated_time on modifications (Database trigger or application interceptor)
- Rule 10: Email/Phone formatting (Migration or seeding service)

## Global Query Rules

- All GET queries must include the `WHERE is_active = true` clause (or equivalent filter) when returning entities or child collections that support soft-delete.

## Implementation Notes
- All business logic implemented in service layer for reusability
- Services use static methods where possible for stateless operations
- Foreign key constraints in database provide primary validation
- The UI should respect the rules (e.g., disable fields for platform tickets)
- All timestamp fields use UTC time

## 18. Soft Delete - System-Wide Rule

All records across the system must implement **soft delete** (not hard delete):
- **Never permanently delete records** from the database
- Set `is_active = false` to mark records as deleted
- When filtering/displaying records, always include `WHERE is_active = true` in queries
- When calculating aggregates (sums, counts, etc.), only include active records
- Examples:
  - Ticket Line Items: Sum weights only for `WHERE is_active = true`
  - Customer records: Display only where `is_active = true`
  - All other entities: Follow the same pattern

**Confirmation Dialogs**: Whenever a user deletes a record (e.g., line item), show a confirmation dialog:
- Format: "Are you sure you want to delete [entity description]?"
- Include relevant details (e.g., "Weight: X kg")
- Buttons: "No" and "Yes"
- Only proceed with soft delete on "Yes"

## 19. UI Text Capitalization Standard (Title Case)

All UI text headings, labels, and button text must follow Title Case capitalization:
- **Format**: First letter of each word capitalized (e.g., "Search Tickets", "Ticket Results")
- **Exception**: When displaying IDs (e.g., "Customer ID", "Ticket ID"), the letters "ID" must be fully uppercase
- **Applies to**:
  - Page section headings (e.g., "Search Tickets", "Ticket Results", "Create Ticket")
  - Column headers in data grids (e.g., "Customer ID", "Account Number", "Net Kg")
  - Form field labels (e.g., "First Name", "Last Name", "Vehicle Registration")
  - Button text (e.g., "Search", "Clear", "Add Line Items", "Edit Ticket")
  - Dialog titles and messages
- **Examples**:
  - ✅ "Search Tickets" not ❌ "Search tickets"
  - ✅ "Customer ID" not ❌ "Customer id" or "customer ID"
  - ✅ "Ticket Results" not ❌ "Ticket results"
  - ✅ "New Ticket Results" not ❌ "New ticket results"
  - ✅ "Account Number" not ❌ "Account number"

## Development Workflow Rules

### API Management
- **API Stays Running**: The API should remain running in the background during development
- **When to Restart API**: Only restart the API when:
  - Changes are made to any API controller files (`Controllers/`)
  - Changes are made to any service or repository files that the API uses
  - Changes are made to database models or migrations
  - You are explicitly told to restart it by the developer
- **How to Restart**: Kill the existing process and run `dotnet run --project MetalLink.Api/`

### Desktop App Testing
- **Do NOT Start Services**: After making code changes, do NOT start the API or Desktop app
- **Developer Runs Apps**: The developer will run the applications to test the changes
- **Notify Developer**: After completing changes, clearly state "✅ CHANGES COMPLETE - API restart required" or "✅ CHANGES COMPLETE" with clear instructions

### Build and Testing
- Always run `dotnet build` to verify compilation after changes
- Include build status in final summary
- Do not run the applications unless explicitly instructed

