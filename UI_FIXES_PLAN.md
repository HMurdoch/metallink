# UI Fixes Plan - MetalLink Application

**Created:** 2026-02-25  
**Status:** In Progress  
**Estimated Iterations:** 20-25 of 30 remaining

---

## Issues Identified

### CRITICAL (Visual Breaks)
1. **Customers View** - 3 sections appear as one monolithic dark panel (no visual separation)
2. **Buyers View** - Details panel not side-by-side with Results
3. **Products & Prices** - DataGrid overlaps window on right
4. **Receiving** - Ticket Results DataGrid overlaps window on right
5. **Stock Levels** - Results DataGrid overlaps window on right
6. **Stock Movements** - Results DataGrid overlaps window on right

### HIGH PRIORITY (Functional Issues)
7. **Customers Create/Edit** - Customer Images section not included in collapse
8. **Buyers** - Collapse arrows not functional
9. **Companies & Sites** - Collapse arrows not functional
10. **Products & Prices** - No collapse arrows at all
11. **Receiving** - Collapse arrows don't work, missing arrows on other sections

### MEDIUM PRIORITY (Missing Features)
12. **Reports** - Plain white heading instead of metallic
13. **Settings** - Plain white heading instead of metallic

---

## Fix Strategy

### Phase 1: Separate Panels (Customers & Buyers)
Break monolithic panels into visually distinct separate panels with spacing.

#### Customers View Fix:
**Current Structure (WRONG):**
```
<Border> (One big panel)
  Search Existing Customer heading
  - Search criteria grid
  - Results and Details side-by-side
</Border>
```

**Target Structure (CORRECT):**
```
<Border> Panel 1: Search Criteria
  - Collapsible arrow + heading
  - Search fields grid
</Border>

<Border> Panel 2: Search Results (separate, 16px margin below Panel 1)
  - Collapsible arrow + heading  
  - DataGrid
  - Pagination
</Border>

<Border> Panel 3: Customer Details (separate, 16px margin below Panel 2)
  - Collapsible arrow + heading
  - Details grid (left) + Images (right) side-by-side
</Border>
```

#### Buyers View Fix:
Same structure as Customers, ensuring Details and Images are side-by-side.

---

### Phase 2: Fix DataGrid Overlaps
All DataGrid overlaps are caused by hardcoded widths exceeding available space.

#### Files to Fix:
1. `ProductsAndPricesView.axaml` - DataGrid width
2. `TicketsReceivingView.axaml` - DataGrid width
3. `StockLevelsView.axaml` - DataGrid width
4. `StockMovementView.axaml` - DataGrid width

#### Solution:
- Remove fixed Width attributes on DataGrids
- Use `HorizontalAlignment="Stretch"` instead
- Or reduce width to fit within ScrollViewer

---

### Phase 3: Functional Collapse Arrows

#### Buyers View:
- Add x:Name to arrows and content sections
- Wire up PointerPressed handlers (copy from Customers)
- Code-behind already has handlers, just need XAML wiring

#### Companies & Sites View:
Create code-behind handlers:
```csharp
private void ToggleSearchCompany(object? sender, PointerPressedEventArgs e)
private void ToggleResults(object? sender, PointerPressedEventArgs e)
private void ToggleCreateEditCompany(object? sender, PointerPressedEventArgs e)
private void ToggleSites(object? sender, PointerPressedEventArgs e)
private void ToggleCreateEditSite(object? sender, PointerPressedEventArgs e)
```

#### Products & Prices View:
- Add collapse arrows (missing completely)
- Create code-behind handlers
- Wire up panels

#### Receiving View:
- Fix existing Search Tickets arrow
- Add arrows to Create/Edit Ticket section
- Wire up handlers

---

### Phase 4: Include Customer Images in Collapse

#### Current Issue:
Customer Images section is outside the Create/Edit collapse content area.

#### Fix:
Move Customer Images `<Border>` inside the `CreateEditContent` Grid that gets hidden/shown.

---

### Phase 5: Generate Metallic Headings

#### Reports & Settings:
Run heading generation script:
```bash
python3 tmp_rovodev_generate_headings.py
```

Then replace plain TextBlock headings with Image elements:
```xml
<Image Source="avares://MetalLink.Desktop/Assets/reports_heading.png"
       Height="40"
       HorizontalAlignment="Left"
       Stretch="Uniform" />
```

---

## Implementation Checklist

### Phase 1: Panel Separation
- [ ] Customers - Create 3 separate Border panels with spacing
  - [ ] Search Criteria panel
  - [ ] Search Results panel  
  - [ ] Customer Details panel
- [ ] Buyers - Create 3 separate Border panels with spacing
  - [ ] Search Criteria panel
  - [ ] Search Results panel
  - [ ] Buyer Details panel (side-by-side with images)

### Phase 2: DataGrid Overlaps
- [ ] Products & Prices - Fix DataGrid width
- [ ] Receiving - Fix DataGrid width
- [ ] Stock Levels - Fix DataGrid width
- [ ] Stock Movements - Fix DataGrid width

### Phase 3: Functional Collapse
- [ ] Buyers - Wire up existing arrows to handlers
- [ ] Companies & Sites - Add handlers and wire up
- [ ] Products & Prices - Add arrows and handlers
- [ ] Receiving - Fix and add arrows

### Phase 4: Collapse Content
- [ ] Customers Create/Edit - Include Images in collapse

### Phase 5: Headings
- [ ] Reports - Generate and add metallic heading
- [ ] Settings - Generate and add metallic heading

---

## Progress Tracking

### Session 1 (Current)
- [x] Created UI_FIXES_PLAN.md
- [ ] Phase 1: Panel Separation
- [ ] Phase 2: DataGrid Overlaps
- [ ] Phase 3: Functional Collapse
- [ ] Phase 4: Collapse Content
- [ ] Phase 5: Headings

**Iterations Used:** 2 of 30  
**Iterations Remaining:** 28  
**Estimated Completion:** ~22 more iterations

---

## Key Code Patterns

### Separate Panel Pattern:
```xml
<!-- Panel 1 -->
<Border Background="{DynamicResource Brush.PanelBackground}" Padding="12" CornerRadius="4">
  <StackPanel Spacing="8">
    <Grid PointerPressed="TogglePanelName" Cursor="Hand">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
      </Grid.ColumnDefinitions>
      <TextBlock Grid.Column="0" x:Name="PanelNameArrow" Text="▼" ... />
      <TextBlock Grid.Column="1" Text="Panel Title" ... />
    </Grid>
    <Grid x:Name="PanelNameContent">
      <!-- Content here -->
    </Grid>
  </StackPanel>
</Border>

<!-- 16px spacing -->

<!-- Panel 2 -->
<Border Background="{DynamicResource Brush.PanelBackground}" ... >
  <!-- Same pattern -->
</Border>
```

### DataGrid Fix Pattern:
```xml
<!-- WRONG -->
<DataGrid Width="1200" ... />

<!-- CORRECT -->
<DataGrid HorizontalAlignment="Stretch" ... />
<!-- OR -->
<DataGrid Width="1000" MaxWidth="1100" ... />
```

### Code-Behind Handler Pattern:
```csharp
private void TogglePanelName(object? sender, PointerPressedEventArgs e)
{
    var arrow = this.FindControl<TextBlock>("PanelNameArrow");
    var content = this.FindControl<Grid>("PanelNameContent");
    TogglePanel(arrow, content);
}

private void TogglePanel(TextBlock? arrow, Control? content)
{
    if (arrow == null || content == null) return;
    bool isCollapsed = arrow.Text == "▶";
    arrow.Text = isCollapsed ? "▼" : "▶";
    content.IsVisible = isCollapsed;
}
```

---

## Resume Instructions

If interrupted, resume with:
1. Check "Progress Tracking" section for current phase
2. Review unchecked items in "Implementation Checklist"
3. Continue with next unchecked item
4. Update progress as you go
5. Test build after each major change
6. Commit at end of each phase

---

## Success Criteria

- [ ] All panels visually separated with spacing
- [ ] All DataGrids fit within viewport (no overlap)
- [ ] All collapse arrows functional
- [ ] All content properly included in collapse areas
- [ ] All views have metallic headings
- [ ] Build succeeds without errors
- [ ] All changes committed to git

---

**END OF PLAN**
