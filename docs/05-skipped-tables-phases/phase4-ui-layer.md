# Phase 4: UI Layer

## Status: Complete

## Objective

Add skipped tables section to the comparison UI with its own DataTable grid.

## Files Modified

| File | Change |
|------|--------|
| `Views/Comparison/Index.cshtml` | Add skipped tables section, JavaScript |

## UI Changes

### 1. Summary Card Update

Add "Skipped" count to the summary card (after Type Mismatches):

```html
<div class="col">
    <h3 id="skippedCount" class="text-secondary mb-0">0</h3>
    <small class="text-muted">Skipped Tables</small>
</div>
```

### 2. Skipped Tables Section

Add new section after the main comparison card:

```html
<!-- Skipped Tables Section -->
<div id="skippedTablesSection" class="card mt-4 d-none">
    <div class="card-header d-flex justify-content-between align-items-center py-2">
        <h6 class="mb-0">
            <a class="text-decoration-none text-reset"
               data-bs-toggle="collapse"
               href="#skippedTablesBody"
               role="button"
               aria-expanded="true">
                Tables Not in DbContext
                <span id="skippedTablesCount" class="badge bg-secondary ms-2">0</span>
            </a>
        </h6>
        <small class="text-muted">
            <span id="skippedColumnsCount">0</span> columns total
        </small>
    </div>
    <div class="collapse show" id="skippedTablesBody">
        <div class="card-body p-0">
            <div class="alert alert-info m-3 py-2 small mb-0">
                <i class="bi bi-info-circle me-1"></i>
                These tables exist in the Data Dictionary but have no corresponding entity in the DbContext.
                They may be intentionally excluded for security or other reasons.
            </div>
            <div class="table-responsive">
                <table id="skippedTablesGrid" class="table table-striped table-hover table-sm mb-0" style="width:100%">
                    <thead>
                        <tr>
                            <th>Schema</th>
                            <th>Table Name</th>
                            <th>Columns</th>
                        </tr>
                    </thead>
                    <tbody>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>
```

### 3. JavaScript Variable

Add variable for skipped tables DataTable:

```javascript
var comparisonData = [];
var skippedTablesData = [];  // NEW
var table = null;
var skippedTable = null;     // NEW
```

### 4. AJAX Success Handler Update

Update the success callback in `#runComparison` click handler:

```javascript
success: function (result) {
    if (result.success) {
        // Update summary
        $("#summaryCard").removeClass("d-none");
        $("#matchCount").text(result.totalMatches);
        $("#missingEfCount").text(result.totalMissingInEf);
        $("#missingDbCount").text(result.totalMissingInDb);
        $("#mismatchCount").text(result.totalTypeMismatches);
        $("#skippedCount").text(result.totalSkippedTables);  // NEW

        // Store data and initialize main grid
        comparisonData = result.items;
        initializeGrid(comparisonData);

        // NEW: Handle skipped tables
        skippedTablesData = result.skippedTables || [];
        if (skippedTablesData.length > 0) {
            $("#skippedTablesSection").removeClass("d-none");
            $("#skippedTablesCount").text(result.totalSkippedTables);
            $("#skippedColumnsCount").text(result.totalSkippedColumns);
            initializeSkippedTablesGrid(skippedTablesData);
        } else {
            $("#skippedTablesSection").addClass("d-none");
        }
    } else {
        DataTableHelpers.alert("Comparison failed: " + (result.error || "Unknown error"));
    }
}
```

### 5. Skipped Tables Grid Initialization

Add new function:

```javascript
function initializeSkippedTablesGrid(data) {
    // Destroy existing table if it exists
    if (skippedTable) {
        skippedTable.destroy();
    }

    skippedTable = $('#skippedTablesGrid').DataTable({
        data: data,
        columns: [
            {
                data: 'schemaName',
                render: function(data) {
                    return '<code class="text-muted">' + (data || 'dbo') + '</code>';
                }
            },
            {
                data: 'tableName',
                render: function(data) {
                    return '<code>' + data + '</code>';
                }
            },
            {
                data: 'columnCount',
                className: 'text-center',
                render: function(data) {
                    return '<span class="badge bg-secondary">' + data + '</span>';
                }
            }
        ],
        columnDefs: [
            { width: '100px', targets: 0 },   // Schema
            { width: 'auto', targets: 1 },    // Table
            { width: '100px', targets: 2 }    // Columns
        ],
        autoWidth: false,
        pageLength: 10,
        lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]],
        order: [[0, 'asc'], [1, 'asc']],
        language: {
            emptyTable: "No skipped tables",
            info: "Showing _START_ to _END_ of _TOTAL_ skipped tables",
            infoEmpty: "No skipped tables",
            infoFiltered: "(filtered from _MAX_ total)"
        }
    });
}
```

## Complete Updated HTML Structure

```
+------------------------------------------+
|  Comparison Configuration    | Summary   |
|  [Source selector]          | Matches: N |
|  [Run Comparison]           | -EF: N     |
|                             | -DB: N     |
|                             | Mismatch: N|
|                             | Skipped: N | <-- NEW
+------------------------------------------+
|  Comparison Results                       |
|  [All] [Match] [-EF] [-DB] [Mismatch]    |
|  +--------------------------------------+|
|  | Status | Table | Column | DB | EF    ||
|  +--------------------------------------+|
+------------------------------------------+
|  Tables Not in DbContext (2)         v   | <-- NEW (collapsible)
|  20 columns total                        |
|  +--------------------------------------+|
|  | Schema | Table Name      | Columns   ||
|  | dbo    | AuditLogs       | 12        ||
|  | security| Credentials    | 8         ||
|  +--------------------------------------+|
+------------------------------------------+
```

## Behavior Notes

1. **Hidden by default** - Skipped tables section has `d-none` class, shown only when data exists
2. **Collapsible** - Uses Bootstrap collapse for the table body
3. **Counts update** - Badge shows table count, header shows column count
4. **Sorted** - Default sort by schema, then table name
5. **Info message** - Explains why tables appear here (intentional exclusion)
6. **DataTable features** - Sorting, paging (10/25/50/All), search

## Verification

After this phase:
- [ ] Skipped tables section hidden when no skipped tables
- [ ] Skipped tables section shown with correct counts when tables skipped
- [ ] Summary card shows "Skipped Tables" count
- [ ] Skipped tables grid initializes correctly
- [ ] Sorting and paging work in skipped tables grid
- [ ] Collapse/expand works for skipped tables section
- [ ] Re-running comparison updates both grids correctly
