# StockIn DataGridView Column Configuration Guide

## Current Grid Structure vs. New Structure

### dgvStockIn (Stock In Tab)

| Index | Column Name | Header Text | Content | Type | Notes |
|-------|-------------|-------------|---------|------|-------|
| 0 | Column1 | "No" | Sequential number | TextBox | Auto-filled |
| 1 | Column9 | "Id" | tbStockIn.id | TextBox | **HIDDEN** |
| 2 | Column10 | "Reference#" | si.refno | TextBox | Reference number |
| 3 | Column2 | "Pcode" | si.pcode | TextBox | Product code |
| 4 | Column4 | "Description" | p.pdesc | TextBox | Product description |
| 5 | Column5 | "Qty" | si.qty | TextBox | Stock in quantity |
| 6 | **Column6** | **"Cost Price"** | **p.CostPrice** | **TextBox** | **NEW - Read-only display** |
| 7 | **Column7** | **"Selling Price"** | **p.price** | **TextBox** | **NEW - Read-only display** |
| 8 | Column8 | "Supplier" | s.supplier | TextBox | Supplier name |
| 9 | Delete | "" | Delete button icon | ImageColumn | Delete action |

### dgvInStockHistory (Stock In Record Tab)

| Index | Column Name | Header Text | Content | Type | Notes |
|-------|-------------|-------------|---------|------|-------|
| 0 | dataGridViewTextBoxColumn1 | "No" | Sequential number | TextBox | Auto-filled |
| 1 | dataGridViewTextBoxColumn2 | "Id" | tbStockIn.id | TextBox | **HIDDEN** |
| 2 | dataGridViewTextBoxColumn3 | "Reference#" | si.refno | TextBox | Reference number |
| 3 | dataGridViewTextBoxColumn4 | "Pcode" | si.pcode | TextBox | Product code |
| 4 | dataGridViewTextBoxColumn5 | "Description" | p.pdesc | TextBox | Product description |
| 5 | dataGridViewTextBoxColumn6 | "Qty" | si.qty | TextBox | Stock in quantity |
| 6 | **dataGridViewTextBoxColumn7** | **"Cost Price"** | **p.CostPrice** | **TextBox** | **NEW** |
| 7 | **NEW** | **"Selling Price"** | **p.price** | **TextBox** | **NEW** |
| 8 | dataGridViewTextBoxColumn8 | "Stock In Date" | si.sdate | TextBox | Date formatted |
| 9 | dataGridViewTextBoxColumn9 | "Supplier" | s.supplier | TextBox | Supplier name |

## Code Mapping

### LoadStockIn() Method
```csharp
dgvStockIn.Rows.Add(
    i,                                           // Index 0: Row number
    dr["id"].ToString(),                        // Index 1: Stock In ID (hidden)
    dr["refno"].ToString(),                     // Index 2: Reference No
    dr["pcode"].ToString(),                     // Index 3: Product Code
    dr["pdesc"].ToString(),                     // Index 4: Description
    dr["qty"].ToString(),                       // Index 5: Quantity
    dr["CostPrice"].ToString(),                 // Index 6: COST PRICE (NEW)
    dr["price"].ToString(),                     // Index 7: SELLING PRICE (NEW)
    dr["supplier"].ToString()                   // Index 8: Supplier
);
```

### btnLoad_Click() Method
```csharp
dgvInStockHistory.Rows.Add(
    i,                                           // Index 0: Row number
    dr["id"].ToString(),                        // Index 1: Stock In ID (hidden)
    dr["refno"].ToString(),                     // Index 2: Reference No
    dr["pcode"].ToString(),                     // Index 3: Product Code
    dr["pdesc"].ToString(),                     // Index 4: Description
    dr["qty"].ToString(),                       // Index 5: Quantity
    dr["CostPrice"].ToString(),                 // Index 6: COST PRICE (NEW)
    dr["price"].ToString(),                     // Index 7: SELLING PRICE (NEW)
    DateTime.Parse(dr["sdate"].ToString()).ToShortDateString(),  // Index 8: Date
    dr["supplier"].ToString()                   // Index 9: Supplier
);
```

## Design Time Changes Required

### For dgvStockIn in Designer:

1. **Update existing columns:**
   - Column5: Change header to "Qty"
   - Column8: Ensure visible with header "Supplier"

2. **Add TWO new columns between Column5 and Column8:**

   **Column 6 - Cost Price:**
   ```
   Name: Column6
   HeaderText: "Cost Price"
   AutoSizeMode: AllCells
   ReadOnly: True
   ```

   **Column 7 - Selling Price:**
   ```
   Name: Column7
   HeaderText: "Selling Price"
   AutoSizeMode: AllCells
   ReadOnly: True
   ```

### For dgvInStockHistory in Designer:

1. **Add TWO new columns between dataGridViewTextBoxColumn6 and dataGridViewTextBoxColumn8:**

   **New Column 1 - Cost Price:**
   ```
   Name: dataGridViewTextBoxColumn7
   HeaderText: "Cost Price"
   AutoSizeMode: AllCells
   ReadOnly: True
   ```

   **New Column 2 - Selling Price:**
   ```
   Name: NewCostPriceColumn
   HeaderText: "Selling Price"
   AutoSizeMode: AllCells
   ReadOnly: True
   ```

2. **Rename existing columns if needed to match indices**

## Runtime Behavior

### Stock In Tab (dgvStockIn):
- Double-click or click "Edit" button on a row ? Opens pricing editor
- Editor shows:
  - Current Cost Price
  - Current Markup % (auto-calculated)
  - Current Selling Price (read-only, calculated)
  - "Calculate Price" button to update selling price
- Prices update in database AND grid immediately

### Stock In Record Tab (dgvInStockHistory):
- Read-only display of historical pricing
- Shows what prices were when stock was received
- Filtered by date range

## SQL Joins Reference

All queries use these JOINs:
```sql
FROM tbStockIn si
INNER JOIN tbProduct p ON si.pcode = p.pcode
INNER JOIN tbSupplier s ON si.supplierid = s.id
```

This ensures you can display:
- Stock In data from `tbStockIn`
- Product info (pdesc, CostPrice, price) from `tbProduct`
- Supplier info from `tbSupplier`
