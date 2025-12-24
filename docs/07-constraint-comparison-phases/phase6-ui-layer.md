# Phase 6: UI Layer - Display Constraint Mismatches

## Objective

Update the Comparison Index page to display constraint mismatch status with summary card, filter button, and grid column.

## File to Modify

`src/NetSqlDataDicV2.Web/Views/Comparison/Index.cshtml`

## Changes

### 6a. Add CSS for purple badge (in style section or site.css)

```css
.bg-purple {
    background-color: #6f42c1 !important;
    color: white !important;
}
.text-purple {
    color: #6f42c1 !important;
}
.btn-outline-purple {
    border-color: #6f42c1;
    color: #6f42c1;
}
.btn-outline-purple:hover {
    background-color: #6f42c1;
    color: white;
}
```

### 6b. Add summary card (in summary cards row, around line 83-89)

Find existing cards and add after "Type Mismatches":

```html
<div class="col">
    <h3 id="mismatchCount" class="text-info mb-0">0</h3>
    <small class="text-muted">Type Mismatches</small>
</div>
<!-- NEW: Constraint Mismatch card -->
<div class="col">
    <h3 id="constraintMismatchCount" class="text-purple mb-0">0</h3>
    <small class="text-muted">Constraint Mismatches</small>
</div>
<div class="col">
    <h3 id="skippedCount" class="text-secondary mb-0">0</h3>
    <small class="text-muted">Skipped Tables</small>
</div>
```

### 6c. Add filter button (around line 105)

```html
<button type="button" class="btn btn-outline-info filter-btn" data-filter="TypeMismatch">Type</button>
<!-- NEW -->
<button type="button" class="btn btn-outline-purple filter-btn" data-filter="ConstraintMismatch">Constraint</button>
```

### 6d. Add grid column header (around line 116-119)

```html
<thead>
    <tr>
        <th>Status</th>
        <th>Table</th>
        <th>Column</th>
        <th>DB Type</th>
        <th>EF Property</th>
        <th>CLR Type</th>
        <th>Constraint Details</th>  <!-- NEW -->
    </tr>
</thead>
```

### 6e. Update JavaScript - Status mapping functions

```javascript
function getStatusValue(statusName) {
    var statusMap = {
        "Match": 0,
        "MissingInEfModel": 1,
        "MissingInDatabase": 2,
        "TypeMismatch": 3,
        "ConstraintMismatch": 4  // NEW
    };
    return statusMap[statusName] ?? -1;
}

function getStatusName(statusValue) {
    var statusNames = [
        "Match",
        "Missing in EF",
        "Missing in DB",
        "Type Mismatch",
        "Constraint Mismatch"  // NEW
    ];
    return statusNames[statusValue] || "Unknown";
}

function getStatusBadgeClass(statusValue) {
    var classes = [
        "bg-success",           // 0: Match
        "bg-warning text-dark", // 1: Missing in EF
        "bg-danger",            // 2: Missing in DB
        "bg-info text-dark",    // 3: Type Mismatch
        "bg-purple"             // 4: Constraint Mismatch (NEW)
    ];
    return classes[statusValue] || "bg-secondary";
}
```

### 6f. Update DataTable columns definition

Add new column for constraint details:

```javascript
columns: [
    // ... existing columns ...
    {
        data: 'efClrType',
        render: function(data) { return data || '-'; }
    },
    // NEW: Constraint details column
    {
        data: 'constraintMismatchSummary',
        render: function(data, type, row) {
            if (!data) return '-';
            // Truncate long text with tooltip
            var abbreviated = data.length > 40
                ? data.substring(0, 37) + '...'
                : data;
            return '<span class="text-muted small" title="' +
                escapeHtml(data) + '">' +
                escapeHtml(abbreviated) + '</span>';
        }
    }
],
columnDefs: [
    { width: '100px', targets: 0 },  // Status
    { width: '140px', targets: 1 },  // Table
    { width: '110px', targets: 2 },  // Column
    { width: '100px', targets: 3 },  // DB Type
    { width: '110px', targets: 4 },  // EF Property
    { width: '70px', targets: 5 },   // CLR Type
    { width: '180px', targets: 6 }   // Constraint Details (NEW)
]
```

### 6g. Update summary card values (in AJAX success handler)

```javascript
// Update summary cards
$("#matchCount").text(result.totalMatches);
$("#missingEfCount").text(result.totalMissingInEf);
$("#missingDbCount").text(result.totalMissingInDb);
$("#mismatchCount").text(result.totalTypeMismatches);
$("#constraintMismatchCount").text(result.totalConstraintMismatches);  // NEW
$("#skippedCount").text(result.totalSkippedTables);
```

### 6h. Add escapeHtml helper (if not exists)

```javascript
function escapeHtml(text) {
    if (!text) return '';
    var div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
```

## Visual Design

| Status | Badge Color | Card Color |
|--------|-------------|------------|
| Match | Green (success) | Green |
| Missing in EF | Yellow (warning) | Yellow |
| Missing in DB | Red (danger) | Red |
| Type Mismatch | Cyan (info) | Cyan |
| Constraint Mismatch | Purple | Purple |

## Verification

1. Run the application
2. Navigate to Comparison page
3. Run a comparison
4. Verify:
   - Summary card shows constraint mismatch count
   - Filter button works
   - Grid shows Constraint Details column
   - Purple badges display correctly
