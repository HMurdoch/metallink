# Ticketing System Implementation Status

## ✅ COMPLETED (Backend & API)

### 1. Database Schema
- ✅ All entities have `is_active`, `created_time`, `updated_time`
- ✅ `Ticket` entity with vehicle fields:
  - `VehicleRegistration`, `TrailerRegistration`, `DriverName`
  - `OfmWeighbridgeTicket`, `ForeignTicket`, `CkNumber`
  - `ProductId`, `CurrencyId` (optional FKs)
- ✅ `TicketLine` entity fully implemented:
  - `ProductId`, `ProductName`, `WeightKg`
  - `UnitPricePerKg`, `LineTotal`, `VatAmount`, `TotalInclVat`
  - `IsActive`, `CreatedTime`, `UpdatedTime`

### 2. API Endpoints
- ✅ `POST /api/tickets` - Create ticket
- ✅ `POST /api/tickets/search` - Search tickets
- ✅ `GET /api/tickets/{id}` - Get ticket by ID
- ✅ `DELETE /api/tickets/{id}` - Soft delete ticket
- ✅ `POST /api/tickets/{ticketId}/lines` - Add ticket line
- ✅ `GET /api/tickets/{ticketId}/lines` - Get ticket lines
- ✅ `DELETE /api/tickets/{ticketId}/lines/{lineId}` - Delete ticket line

### 3. Application Layer
- ✅ `CreateTicketCommand` & Handler
- ✅ `SearchTicketsQuery` & Handler
- ✅ `GetTicketByIdQuery` & Handler
- ✅ `TicketRepository` with full search implementation

### 4. DTOs (Shared)
- ✅ `TicketDto` - Complete ticket data
- ✅ `TicketLineDto` - Line item data
- ✅ `TicketSearchRequestDto` - Search criteria
- ✅ `TicketSearchResultDto` - Search results

### 5. Search Capabilities
Search tickets by:
- ✅ Customer ID
- ✅ ID Number
- ✅ First Name / Last Name
- ✅ Company (by letter or ID)
- ✅ Site ID
- ✅ Account Number
- ✅ Ticket Number
- ✅ Ticket Type
- ✅ Date range (CreatedFrom/CreatedTo)

## ⚠️ IN PROGRESS (Frontend)

### TicketsView.axaml - Needs Restructuring
Current structure (615 lines):
- Has search section
- Has results grid
- Has create/edit form
- Has receiving lines grid

**Required Changes:**
Need to reorganize into 6 distinct sections like CustomersView:

1. **Search Section** (2 columns)
   - Customer ID, ID Number
   - First Name, Last Name
   - Company (letter + dropdown), Site
   - Account Number, Ticket Number
   - Ticket Type, Date Range
   - Search / Clear buttons

2. **Ticket Results Grid**
   - Display search results
   - Columns: Ticket#, Customer, Company, Site, Net Weight, Total
   - **[Edit] [Delete]** buttons per row
   - Select row to view details

3. **Ticket Details Panel**
   - Show selected ticket info
   - Vehicle details
   - Weights, pricing
   - Customer info
   - Read-only display

4. **Ticket Lines Results Grid**
   - Show line items for selected ticket
   - Columns: Product, Weight, Price, Line Total, VAT, Total Incl
   - **[Edit] [Delete]** buttons per row

5. **Create/Edit Ticket Section**
   - Form for ticket header
   - Customer selection
   - Vehicle details
   - Weights (if not using ticket lines)
   - Save / Cancel buttons

6. **Create/Edit Line Item Section**
   - Product selection dropdown
   - Weight input
   - Price (auto from product or manual)
   - Calculated totals
   - Add / Update / Cancel buttons

## 📋 TODO: Desktop UI

### High Priority
1. **Restructure TicketsView.axaml**
   - Implement 6-section layout as described above
   - Match CustomersView styling and structure
   - Add Edit/Delete buttons to grids

2. **ViewModel Updates** (MainWindowViewModel.Tickets.cs)
   - Add search state properties
   - Add commands: SearchTickets, EditTicket, DeleteTicket
   - Add commands: EditLine, DeleteLine, CreateLine
   - Add selected ticket/line properties
   - Wire up API calls via TicketService

3. **TicketService** (Desktop/Services/)
   - Implement SearchTicketsAsync
   - Implement GetTicketByIdAsync
   - Implement DeleteTicketAsync
   - Implement CRUD for ticket lines
   - Add error handling

### Medium Priority
4. **Product Selection Integration**
   - Load products dropdown
   - Auto-populate price from Products table
   - Use customer's price_code (A/B/C) to select correct price

5. **Validation**
   - Required field validation
   - Weight validation (net = gross - tare)
   - Line item validation

6. **User Experience**
   - Loading indicators
   - Success/error messages
   - Confirmation dialogs for delete
   - Form reset after save

### Low Priority
7. **Advanced Features**
   - Multi-select for batch operations
   - Export to Excel
   - Print preview
   - Ticket duplication

## 🗄️ Database Status

Current state (from `/api/health/db`):
- Customers: 21
- Tickets: 0
- Companies: 18
- Sites: 23
- Products: 90

**Next Step:** Create database migration for CustomerDocument changes
```bash
cd MetalLink.Infrastructure
dotnet ef migrations add AddAuditFieldsToCustomerDocument --startup-project ../MetalLink.Api
```

## 🚀 Testing Checklist

### API Tests (via Swagger or curl)
- [ ] Create ticket with vehicle details
- [ ] Search tickets by customer
- [ ] Search tickets by date range
- [ ] Get ticket by ID with all related data
- [ ] Add line items to ticket
- [ ] Delete line item (soft delete)
- [ ] Delete ticket (soft delete)
- [ ] Verify totals calculate correctly

### Desktop Tests
- [ ] Search tickets displays results
- [ ] Select ticket shows details
- [ ] Select ticket shows line items
- [ ] Create new ticket
- [ ] Add line items to ticket
- [ ] Edit existing ticket
- [ ] Delete ticket
- [ ] Totals calculate correctly (excl VAT, VAT, incl VAT)

## 📝 Notes

### VB6 Comparison
Original VB6 had:
- Vehicle tracking (VehicleNo, Trailer, Driver)
- RFID card scanning
- Scale integration
- Signature pad
- Photo capture

Current C# implementation has:
- ✅ Vehicle tracking fields
- ❌ RFID integration (future)
- ❌ Scale integration (future)
- ✅ Hardware service interfaces ready
- ❌ Photo capture (future)

### Key Differences from VB6
1. **Ticket Lines**: New approach with separate line items table
2. **Soft Deletes**: All records preserved with is_active flag
3. **Audit Trail**: Created/Updated timestamps on all entities
4. **VAT Calculation**: Proper excl/incl VAT breakdown
5. **Product Integration**: Links to Products table for pricing

## 🔗 Related Files

### Backend
- `MetalLink.Domain/Entities/Ticket.cs`
- `MetalLink.Domain/Entities/TicketLine.cs`
- `MetalLink.Api/Controllers/TicketsController.cs`
- `MetalLink.Infrastructure/Persistence/Repositories/TicketRepository.cs`

### Frontend
- `MetalLink.Desktop/Views/TicketsView.axaml` - NEEDS UPDATE
- `MetalLink.Desktop/ViewModels/MainWindowViewModel.Tickets.cs` - NEEDS UPDATE
- `MetalLink.Desktop/Services/TicketService.cs` - NEEDS UPDATE

### Shared
- `MetalLink.Shared/Tickets/TicketDto.cs`
- `MetalLink.Shared/Tickets/TicketLineDto.cs`
- `MetalLink.Shared/Tickets/TicketSearchDtos.cs`

## 🎯 Immediate Next Steps

1. Create database migration for CustomerDocument
2. Restructure TicketsView.axaml (6 sections)
3. Update TicketService with search/CRUD methods
4. Update ViewModel with search state and commands
5. Wire up UI to ViewModel
6. Test complete workflow end-to-end

