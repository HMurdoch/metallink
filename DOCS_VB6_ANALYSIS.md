# VB6 Metal Link Ticketing System Analysis

## Overview
The original VB6 application (~/Projects/MetalLinkOriginal/) is a weighbridge/scale management system for metal recycling operations. This analysis focuses on the ticketing workflow to guide the C# Avalonia implementation.

## Original System Architecture

### Main Forms (48 total .frm files)
1. **frmMain2.frm** - Primary weighing interface ("NORMAL WEIGHING")
2. **frmDelNote.frm** - Delivery note generation
3. **frmMovieHire.frm** - Legacy rental system
4. **frmCustomer.frm** - Customer management
5. **frmProduct.frm** - Product/material management
6. **MainWindow.frm** - Main navigation window

### Database
- **Scale.mdb** (Access database, ~130MB)
- Tables include: InWeigh, Customer, Cards, Product, Items, etc.

## Ticketing Workflow (VB6)

### 1. Normal Weighing Process (frmMain2.frm)

#### Key Fields:
- **txtVehNo** - Vehicle number (primary identifier)
- **txtTrailer** - Trailer number
- **txtRfId** - RFID card number for customer identification
- **txtDriver** - Driver name
- **txtfirst** - First weight (gross)
- **txtSecond** - Second weight (tare)
- **txtNet** - Net weight (calculated: gross - tare)
- **txtPrice** - Unit price per kg
- **txtAmount** - Total amount (net × price)
- **txtProduct** - Product/material description
- **txtDel** - Delivery number
- **txtStatus** - "Delivery" or "Receiving"

#### Workflow Steps:
1. **Vehicle Entry**: Scan RFID card or enter vehicle number
2. **Customer Lookup**: Auto-populate customer from vehicle/card
3. **First Weight**: Capture from weighbridge scale (gross weight)
4. **Second Weight**: Capture tare weight after unloading
5. **Net Calculation**: Automatic: Net = Gross - Tare
6. **Pricing**: Apply unit price, calculate total amount
7. **Ticket Generation**: Print ticket via Crystal Reports
8. **Database Save**: Store transaction in InWeigh table

#### Key Features:
- **RFID Integration**: Card scanning for quick customer ID
- **Scale Integration**: COM port communication (MSComm32.ocx)
- **Signature Pad**: Digital signature capture (STPadCapt.ocx)
- **Photo Capture**: Customer/vehicle photos
- **Multi-weight Support**: Can handle multiple weighings per vehicle
- **Ticket Types**: "weighbridge" (2 weights) vs "platform" (1 weight)

### 2. Delivery Note System (frmDelNote.frm)

#### Key Fields:
- **txtFTicket** - Foreign ticket number (from other sites)
- **txtWbTicket** - Weighbridge ticket number
- **cboCustomer** - Customer dropdown
- **cboSearch** - Customer search combo
- Delivery note number (auto-generated from dataSetup!Sdel)

#### Workflow:
1. Select or search customer
2. Enter ticket references (foreign/WB)
3. Generate delivery note
4. Print via Crystal Reports (frmPrintDel)

### 3. Report Printing

#### Crystal Reports Used:
- **Final1.rpt** - Final weighing ticket
- **finaldel.rpt** - Delivery ticket
- **rptDelivery** - Delivery reports
- **rptPurchase** - Purchase reports

#### Print Workflow:
```vb
rptFinalDel.PrintReport
Do While Not response1 = True
    response = MsgBox("Was the printout successful?", vbYesNo, "Printing ticket")
    If response = vbYes Then
        response1 = True
        Exit Do
    ElseIf response = vbNo Then
        rptFinalDel.PrintReport  ' Retry
    End If
Loop
```

## Current C# Implementation Status

### ✅ Already Implemented:
1. **Ticket Entity** (MetalLink.Domain/Entities/Ticket.cs)
   - TicketId, TicketNumber, TicketType
   - FirstWeightKg, SecondWeightKg, NetWeightKg
   - UnitPricePerKg, TotalAmount, CurrencyCode
   - Customer, Site, Operator relationships
   - Automatic net/total calculation

2. **Ticket Commands**
   - CreateTicketCommand/Handler
   - CreateTicketCommandValidator

3. **API Endpoints** (TicketsController.cs)
   - GET /api/tickets
   - POST /api/tickets
   - GET /api/tickets/{id}

4. **Report Generation** (QuestPDF)
   - TicketReportDocument.cs
   - TicketReportModel.cs
   - PDF generation with customer, weights, pricing

5. **Desktop UI** (TicketsView.axaml)
   - Basic ticket creation form
   - Customer ID, ticket number, type
   - Weight inputs, pricing
   - Status display

### ❌ Missing from C# Implementation:

#### High Priority:
1. **Vehicle Management**
   - Vehicle number tracking
   - Trailer number
   - Vehicle-to-customer linking

2. **RFID/Card System**
   - Card scanning integration
   - Card-to-customer mapping
   - Quick lookup by card

3. **Scale Integration**
   - Real-time weight capture from hardware
   - Serial port communication
   - Weight validation

4. **Signature Capture**
   - Digital signature pad integration
   - Signature storage/display
   - Signature on reports

5. **Photo/Image Management**
   - Vehicle photos
   - Customer photos
   - Image storage and retrieval

6. **Enhanced UI Features**
   - Vehicle search/autocomplete
   - Customer quick lookup
   - Real-time weight display
   - Multi-step wizard for weighing process

#### Medium Priority:
7. **Delivery Notes**
   - Separate delivery note entity
   - Foreign ticket references
   - Delivery-specific workflow

8. **Ticket Status Tracking**
   - Draft vs Completed
   - Void/Cancel functionality
   - Edit history

9. **Product/Material Selection**
   - Link to Products table
   - Auto-populate pricing
   - Stock management integration

10. **Operator Management**
    - Already have Operator entity
    - Need operator selection in UI
    - Operator permissions

#### Low Priority:
11. **Advanced Reporting**
    - Date range reports
    - Customer summaries
    - Product summaries
    - Export to Excel

12. **Batch Operations**
    - Multiple tickets per vehicle
    - Reprint functionality
    - Ticket search/filter

## Recommendations for Implementation

### Phase 1: Core Ticketing Enhancement (Current Sprint)
1. **Add Vehicle Fields to Ticket Entity**
   ```csharp
   public string? VehicleNumber { get; private set; }
   public string? TrailerNumber { get; private set; }
   public string? DriverName { get; private set; }
   ```

2. **Enhance TicketsView UI**
   - Add vehicle number input
   - Add trailer number input
   - Add driver name input
   - Improve layout to match VB6 workflow

3. **Update Ticket Reports**
   - Add vehicle info to PDF
   - Add driver name
   - Match VB6 ticket layout

### Phase 2: Hardware Integration
1. **Scale Service Interface**
   ```csharp
   public interface IScaleService
   {
       Task<decimal> ReadWeightAsync(CancellationToken ct = default);
       Task<bool> IsConnectedAsync();
   }
   ```

2. **RFID/Card Service**
   ```csharp
   public interface ICardReaderService
   {
       Task<string> ReadCardAsync(CancellationToken ct = default);
       event EventHandler<string> CardScanned;
   }
   ```

3. **Signature Pad Service** (Already exists in Desktop/Hardware!)
   - ISignaturePadService
   - MockSignaturePadService
   - Ready for integration

### Phase 3: Advanced Features
1. **Delivery Notes Module**
2. **Photo Management**
3. **Advanced Reporting**

## Database Schema Comparison

### VB6 (Access MDB):
```
InWeigh Table:
- VehicleNo (Text)
- Trailer (Text)
- Customer (Number)
- Status ("Delivery" or "Receiving")
- FirstWeight, SecondWeight, Net
- Product, Price, Amount
- Ticket (Text)
- delivery (Number)
```

### Current C# (PostgreSQL):
```sql
tickets table:
- ticket_id (bigint)
- site_id (bigint)
- customer_id (bigint)
- operator_id (bigint)
- ticket_number (text)
- ticket_type (text)
- first_weight_kg (numeric)
- second_weight_kg (numeric)
- net_weight_kg (numeric)
- unit_price_per_kg (numeric)
- total_amount (numeric)
- currency_code (text)
- product_description (text)
- notes (text)
- created_time, updated_time
```

### Missing Fields in C#:
- vehicle_number
- trailer_number
- driver_name
- delivery_number
- status (receiving/delivery)
- rfid_card_number

## Next Steps

1. **Database Migration**: Add vehicle/driver fields to Ticket entity
2. **UI Enhancement**: Update TicketsView to include vehicle fields
3. **Report Update**: Add vehicle info to PDF ticket
4. **Hardware Abstraction**: Implement scale service interface
5. **Testing**: Test complete workflow end-to-end

## Files to Review in VB6 Project:
- frmMain2.frm - Main weighing logic (4,400+ lines)
- frmDelNote.frm - Delivery notes (2,800+ lines)
- frmCustomer.frm - Customer management
- Scale.mdb - Database structure

## Key VB6 Code Patterns:
```vb
' Net weight calculation
txtNet = Abs(CLng(txtSecond) - CLng(txtfirst))

' Amount calculation
txtAmount = Format((txtNet * txtPrice), "####0.00")

' Vehicle lookup
dataInWeigh.RecordSource = "Select * from InWeigh WHERE VehicleNo ='" & txtVehNo.Text & "'"

' Customer by RFID
dataCustomer.RecordSource = "Select * From Customer where Account = '" & txtRfId.Text & "'"

' Print confirmation loop
Do While Not response1 = True
    response = MsgBox("Was the printout successful?", vbYesNo)
    If response = vbYes Then response1 = True
    Else rptFinalDel.PrintReport
Loop
```

