# Next Session Plan - TicketsView UI Restructure

## ✅ COMPLETED TODAY

### 1. Database Migration ✅
- Created and applied migration: 20260110180505_Tickets_Refactor
- Created `currencies` table
- Created `ticket_lines` table  
- Added vehicle fields to `tickets` table
- Added audit fields to `customer_documents`
- ✅ All tables verified in database
- ✅ API running on http://localhost:5066

### 2. Backend Verification ✅
- 7 API endpoints working
- Search functionality complete
- Ticket lines CRUD complete
- VAT calculations working

## 📋 TODO: TicketsView UI Restructure

### Current Status
- Current file: 615 lines
- Backup created: TicketsView.axaml.backup
- Structure plan documented

### Required Changes

#### Section 1: SEARCH (2-column grid)
**Bindings needed:**
- SearchCustomerId → long?
- SearchIdNumber → string?
- SearchFirstName → string?
- SearchLastName → string?
- SearchCompanyLetter → string?
- SearchCompanyId → long?
- SearchSiteId → long?
- SearchAccountNumber → long?
- SearchTicketNumber → string?
- SearchTicketType → string?
- SearchDateFrom → DateTimeOffset?
- SearchDateTo → DateTimeOffset?

**Commands:**
- SearchTicketsCommand
- ClearSearchCommand

#### Section 2: TICKET RESULTS GRID
**ItemsSource:** TicketSearchResults (ObservableCollection<TicketSearchResultDto>)

**Columns:**
- TicketNumber
- Customer (FirstName + LastName)
- CompanyName
- SiteName
- NetWeightKg
- TotalInclVat
- CreatedTime
- Edit button → EditTicketCommand
- Delete button → DeleteTicketCommand

**Selection:** SelectedTicket → triggers loading ticket details + lines

#### Section 3: TICKET DETAILS PANEL
**Bindings (read-only):**
- SelectedTicket.TicketNumber
- SelectedTicket.CustomerName
- SelectedTicket.VehicleRegistration
- SelectedTicket.TrailerRegistration
- SelectedTicket.DriverName
- SelectedTicket.FirstWeightKg
- SelectedTicket.SecondWeightKg
- SelectedTicket.NetWeightKg
- SelectedTicket.UnitPricePerKg
- SelectedTicket.TotalExclVat
- SelectedTicket.VatAmount
- SelectedTicket.TotalInclVat

#### Section 4: TICKET LINES GRID
**ItemsSource:** TicketLines (ObservableCollection<TicketLineDto>)

**Columns:**
- ProductName
- WeightKg
- UnitPricePerKg
- LineTotal (Ex VAT)
- VatAmount
- TotalInclVat
- Edit button → EditTicketLineCommand
- Delete button → DeleteTicketLineCommand

#### Section 5: CREATE/EDIT TICKET FORM
**Bindings:**
- IsEditingTicket (bool) → show/hide form
- EditTicketId (long?) → null for create, ID for edit
- EditTicketCustomerId (long)
- EditTicketSiteId (long)
- EditTicketVehicleReg (string?)
- EditTicketTrailerReg (string?)
- EditTicketDriverName (string?)
- EditTicketOfmTicket (string?)
- EditTicketForeignTicket (string?)
- EditTicketCkNumber (string?)

**Commands:**
- SaveTicketCommand
- CancelEditTicketCommand

#### Section 6: CREATE/EDIT LINE ITEM FORM
**Bindings:**
- IsEditingLine (bool)
- EditLineId (long?)
- EditLineProductId (long)
- EditLineWeightKg (decimal)
- EditLineUnitPrice (decimal)
- EditLineCalculatedTotal (computed)

**Commands:**
- SaveTicketLineCommand
- CancelEditLineCommand

### ViewModel Updates Required

File: `MainWindowViewModel.Tickets.cs` (or create new partial class)

**Properties to Add:**
```csharp
// Search
public long? SearchCustomerId { get; set; }
public string? SearchIdNumber { get; set; }
public string? SearchFirstName { get; set; }
public string? SearchLastName { get; set; }
public string? SearchCompanyLetter { get; set; }
public ObservableCollection<CompanyLookupDto> SearchCompanies { get; }
public ObservableCollection<SiteLookupDto> SearchSites { get; }
// ... etc

// Results
public ObservableCollection<TicketSearchResultDto> TicketSearchResults { get; }
public TicketSearchResultDto? SelectedTicket { get; set; }

// Details  
public ObservableCollection<TicketLineDto> TicketLines { get; }
public TicketLineDto? SelectedTicketLine { get; set; }

// Edit forms
public bool IsEditingTicket { get; set; }
public bool IsEditingLine { get; set; }
// ... edit properties
```

**Commands to Add:**
```csharp
public IAsyncRelayCommand SearchTicketsCommand { get; }
public IRelayCommand ClearSearchCommand { get; }
public IRelayCommand<TicketSearchResultDto> EditTicketCommand { get; }
public IAsyncRelayCommand<TicketSearchResultDto> DeleteTicketCommand { get; }
public IRelayCommand<TicketLineDto> EditTicketLineCommand { get; }
public IAsyncRelayCommand<TicketLineDto> DeleteTicketLineCommand { get; }
public IAsyncRelayCommand SaveTicketCommand { get; }
public IRelayCommand CancelEditTicketCommand { get; }
public IAsyncRelayCommand SaveTicketLineCommand { get; }
public IRelayCommand CancelEditLineCommand { get; }
```

### Service Updates Required

File: `TicketService.cs`

**Methods to Add/Update:**
```csharp
Task<TicketSearchResultDto[]> SearchTicketsAsync(TicketSearchRequestDto request, CancellationToken ct);
Task<TicketDto?> GetTicketByIdAsync(long ticketId, CancellationToken ct);
Task DeleteTicketAsync(long ticketId, CancellationToken ct);
Task<TicketLineDto[]> GetTicketLinesAsync(long ticketId, CancellationToken ct);
Task<TicketLineDto> CreateTicketLineAsync(long ticketId, CreateTicketLineRequest request, CancellationToken ct);
Task DeleteTicketLineAsync(long ticketId, long lineId, CancellationToken ct);
```

## 🎯 Step-by-Step Implementation Plan

### Phase 1: ViewModel & Service (1-2 hours)
1. Create MainWindowViewModel.TicketsSearch.cs partial class
2. Add all search properties
3. Add TicketSearchResults collection
4. Add SearchTicketsCommand implementation
5. Update TicketService with search method
6. Test search via Swagger first

### Phase 2: UI Sections 1-2 (1-2 hours)
7. Build Section 1: Search form (2 columns)
8. Build Section 2: Results grid with Edit/Delete buttons
9. Wire up SearchTicketsCommand
10. Test search displays results

### Phase 3: UI Sections 3-4 (1 hour)
11. Build Section 3: Details panel (read-only)
12. Build Section 4: Ticket lines grid
13. Wire up SelectedTicket to load details
14. Test selection shows details

### Phase 4: UI Sections 5-6 (1-2 hours)
15. Build Section 5: Ticket create/edit form
16. Build Section 6: Line item create/edit form
17. Wire up save/cancel commands
18. Test create/edit workflows

### Phase 5: Testing & Polish (1 hour)
19. End-to-end test: search, create, edit, delete
20. Test ticket with multiple line items
21. Verify VAT calculations
22. Fix any UI/UX issues

**Total Estimated Time: 5-7 hours**

## 📁 Files to Modify

1. `MetalLink.Desktop/Views/TicketsView.axaml` - Complete restructure
2. `MetalLink.Desktop/ViewModels/MainWindowViewModel.TicketsSearch.cs` - New file
3. `MetalLink.Desktop/Services/TicketService.cs` - Add methods
4. `MetalLink.Desktop/Properties/TicketProperties.cs` - Add properties

## 🧪 Test Scenarios

After implementation, test:
1. Search by customer name → displays results
2. Search by ticket number → finds specific ticket
3. Search by date range → filters correctly
4. Select ticket → shows details and line items
5. Create new ticket → saves successfully
6. Add line items → calculates totals correctly
7. Edit ticket → updates database
8. Delete line item → soft deletes
9. Delete ticket → soft deletes ticket and lines
10. VAT calculations → excl VAT + VAT = incl VAT

## 💾 Database Ready

Tables verified:
✅ metal_link.currencies
✅ metal_link.ticket_lines
✅ metal_link.tickets (with all new columns)
✅ metal_link.customer_documents (with audit fields)

Migration applied:
✅ 20260110180505_Tickets_Refactor

API Status:
✅ Running on http://localhost:5066
✅ All 7 endpoints tested and working

## 📌 Quick Start Commands

```bash
# Start API
cd MetalLink.Api && dotnet run --launch-profile http

# Test search endpoint
curl -X POST http://localhost:5066/api/tickets/search \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"customerId": 1}'

# View Swagger
open http://localhost:5066/swagger
```

