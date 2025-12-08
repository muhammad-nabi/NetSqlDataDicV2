# Phase 2: Replace Kendo UI with DataTables

**Status:** Pending
**Complexity:** High
**Estimated Time:** 4-6 hours

## Overview

Replace Telerik Kendo UI components with DataTables.net and Bootstrap 5. This eliminates the licensing requirement while preserving all grid functionality including server-side paging, filtering, sorting, and inline editing.

## Why Replace Kendo?

1. **Licensing cost** - Telerik requires commercial license for production
2. **Heavy dependency** - Large JavaScript/CSS bundle
3. **Vendor lock-in** - Kendo-specific API throughout codebase
4. **Overkill** - Using only grid components from a full UI suite

## Replacement Strategy

| Kendo Component | Replacement |
|-----------------|-------------|
| kendoGrid | DataTables.net |
| DataSourceRequest | Custom PaginationRequest class |
| ToDataSourceResult | LINQ Skip/Take with custom response |
| kendo.alert() | Bootstrap Modal or SweetAlert2 |
| kendo.confirm() | Bootstrap Modal |
| kendo.ui.progress() | Bootstrap Spinner |
| Kendo CSS Theme | Bootstrap 5 (already included) |

## Files to DELETE

### 1. Kendo License File
**Path:** `src/NetSqlDataDicV2.Web/wwwroot/js/kendo-ui-license.js`

## Files to CREATE

### 1. PaginationRequest.cs
**Path:** `src/NetSqlDataDicV2.Web/Models/PaginationRequest.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models;

/// <summary>
/// Request model for paginated data queries. Replaces Kendo DataSourceRequest.
/// </summary>
public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortField { get; set; }
    public string SortDirection { get; set; } = "asc";
    public string? SearchTerm { get; set; }
    public Dictionary<string, string>? Filters { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
```

### 2. PaginationResponse.cs
**Path:** `src/NetSqlDataDicV2.Web/Models/PaginationResponse.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models;

/// <summary>
/// Response model for paginated data. Replaces Kendo DataSourceResult.
/// </summary>
public class PaginationResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    public static PaginationResponse<T> Create(IEnumerable<T> data, int total, PaginationRequest request)
    {
        return new PaginationResponse<T>
        {
            Data = data,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
```

### 3. DataTables Helper Script
**Path:** `src/NetSqlDataDicV2.Web/wwwroot/js/datatables-helpers.js`

```javascript
// DataTables initialization helpers
const DataTableHelpers = {
    // Standard configuration for server-side processing
    getServerSideConfig: function(ajaxUrl, columns) {
        return {
            processing: true,
            serverSide: true,
            ajax: {
                url: ajaxUrl,
                type: 'POST',
                contentType: 'application/json',
                data: function(d) {
                    return JSON.stringify({
                        page: (d.start / d.length) + 1,
                        pageSize: d.length,
                        sortField: d.columns[d.order[0]?.column]?.data,
                        sortDirection: d.order[0]?.dir || 'asc',
                        searchTerm: d.search?.value
                    });
                },
                dataFilter: function(data) {
                    var json = JSON.parse(data);
                    return JSON.stringify({
                        draw: json.draw,
                        recordsTotal: json.total,
                        recordsFiltered: json.total,
                        data: json.data
                    });
                }
            },
            columns: columns,
            pageLength: 50,
            lengthMenu: [[25, 50, 100, 200], [25, 50, 100, 200]],
            order: [[0, 'asc']],
            responsive: true
        };
    },

    // Show loading spinner
    showProgress: function(show) {
        if (show) {
            $('body').append('<div class="dt-loading-overlay"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>');
        } else {
            $('.dt-loading-overlay').remove();
        }
    },

    // Alert dialog (Bootstrap modal)
    alert: function(message, title = 'Alert') {
        return new Promise((resolve) => {
            const modal = $(`
                <div class="modal fade" tabindex="-1">
                    <div class="modal-dialog">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">${title}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body"><p>${message}</p></div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-primary" data-bs-dismiss="modal">OK</button>
                            </div>
                        </div>
                    </div>
                </div>
            `).appendTo('body');

            modal.on('hidden.bs.modal', function() {
                modal.remove();
                resolve();
            });

            new bootstrap.Modal(modal[0]).show();
        });
    },

    // Confirm dialog (Bootstrap modal)
    confirm: function(message, title = 'Confirm') {
        return new Promise((resolve) => {
            const modal = $(`
                <div class="modal fade" tabindex="-1">
                    <div class="modal-dialog">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">${title}</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body"><p>${message}</p></div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-danger btn-confirm">Confirm</button>
                            </div>
                        </div>
                    </div>
                </div>
            `).appendTo('body');

            modal.find('.btn-confirm').on('click', function() {
                bootstrap.Modal.getInstance(modal[0]).hide();
                resolve(true);
            });

            modal.on('hidden.bs.modal', function() {
                modal.remove();
                resolve(false);
            });

            new bootstrap.Modal(modal[0]).show();
        });
    }
};
```

### 4. DataTables CSS Override
**Path:** `src/NetSqlDataDicV2.Web/wwwroot/css/datatables-custom.css`

```css
/* DataTables Bootstrap 5 customizations */
.dt-loading-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(255,255,255,0.7);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}

/* Status badges */
.badge-match { background-color: #198754; }
.badge-missing-ef { background-color: #dc3545; }
.badge-missing-db { background-color: #ffc107; color: #000; }
.badge-mismatch { background-color: #0dcaf0; color: #000; }

/* Editable cells */
.editable-cell {
    cursor: pointer;
    padding: 4px 8px;
    border-radius: 4px;
}
.editable-cell:hover {
    background-color: #f8f9fa;
}
.editable-cell.editing {
    padding: 0;
}
.editable-cell input,
.editable-cell textarea {
    width: 100%;
    padding: 4px 8px;
    border: 1px solid #0d6efd;
    border-radius: 4px;
}
```

## Files to MODIFY

### 1. NetSqlDataDicV2.Web.csproj

**Remove:**
```xml
<PackageReference Include="Telerik.UI.for.AspNet.Core" Version="2024.1.130" />
```

### 2. Program.cs

**Remove line 32:**
```csharp
builder.Services.AddKendo();
```

### 3. _Layout.cshtml

**Path:** `src/NetSqlDataDicV2.Web/Views/Shared/_Layout.cshtml`

**Remove these lines:**
```html
<!-- Line 15 - Kendo CSS -->
<link href="https://kendo.cdn.telerik.com/themes/8.0.1/default/default-main.css" rel="stylesheet" />

<!-- Line 69 - Kendo JS -->
<script src="https://kendo.cdn.telerik.com/2024.1.130/js/kendo.all.min.js"></script>

<!-- Line 70 - Kendo ASP.NET MVC -->
<script src="https://kendo.cdn.telerik.com/2024.1.130/js/kendo.aspnetmvc.min.js"></script>

<!-- Line 73 - Kendo License -->
<script src="~/js/kendo-ui-license.js"></script>
```

**Add these lines:**
```html
<!-- DataTables CSS (after Bootstrap CSS) -->
<link href="https://cdn.datatables.net/1.13.7/css/dataTables.bootstrap5.min.css" rel="stylesheet" />
<link href="~/css/datatables-custom.css" rel="stylesheet" />

<!-- DataTables JS (after jQuery) -->
<script src="https://cdn.datatables.net/1.13.7/js/jquery.dataTables.min.js"></script>
<script src="https://cdn.datatables.net/1.13.7/js/dataTables.bootstrap5.min.js"></script>
<script src="~/js/datatables-helpers.js"></script>
```

### 4. DataDictionaryController.cs

**Path:** `src/NetSqlDataDicV2.Web/Controllers/DataDictionaryController.cs`

**Remove imports:**
```csharp
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
```

**Add import:**
```csharp
using NetSqlDataDicV2.Web.Models;
```

**Replace Read method:**
```csharp
[HttpPost]
public async Task<IActionResult> Read([FromBody] PaginationRequest request, CancellationToken ct)
{
    var query = _service.GetDataElements();

    // Apply search filter
    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        var term = request.SearchTerm.ToLower();
        query = query.Where(e =>
            e.ColumnName.ToLower().Contains(term) ||
            e.TableName.ToLower().Contains(term) ||
            (e.Purpose != null && e.Purpose.ToLower().Contains(term)));
    }

    // Apply sorting
    query = ApplySorting(query, request.SortField, request.SortDirection);

    var total = await query.CountAsync(ct);
    var data = await query.Skip(request.Skip).Take(request.PageSize).ToListAsync(ct);

    return Json(PaginationResponse<DataElement>.Create(data, total, request));
}

private IQueryable<DataElement> ApplySorting(IQueryable<DataElement> query, string? field, string direction)
{
    var isDesc = direction?.ToLower() == "desc";
    return field?.ToLower() switch
    {
        "columnname" => isDesc ? query.OrderByDescending(e => e.ColumnName) : query.OrderBy(e => e.ColumnName),
        "tablename" => isDesc ? query.OrderByDescending(e => e.TableName) : query.OrderBy(e => e.TableName),
        "datatype" => isDesc ? query.OrderByDescending(e => e.DataType) : query.OrderBy(e => e.DataType),
        _ => query.OrderBy(e => e.TableName).ThenBy(e => e.ColumnName)
    };
}
```

**Replace Update method:**
```csharp
[HttpPost]
public async Task<IActionResult> Update([FromBody] DataElement model, CancellationToken ct)
{
    if (!ModelState.IsValid)
    {
        return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
    }

    var result = await _service.UpdateAsync(model, ct);
    return Json(new { success = true, data = result });
}
```

### 5. ComparisonController.cs

**Path:** `src/NetSqlDataDicV2.Web/Controllers/ComparisonController.cs`

**Remove imports:**
```csharp
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
```

**Replace GetResults method:**
```csharp
[HttpPost]
public async Task<IActionResult> GetResults([FromBody] PaginationRequest request, int? sourceId, CancellationToken ct)
{
    // ... existing comparison logic ...

    var items = result.Items.AsQueryable();

    // Apply filters
    if (request.Filters?.TryGetValue("status", out var statusFilter) == true && !string.IsNullOrEmpty(statusFilter))
    {
        items = items.Where(i => i.Status.ToString() == statusFilter);
    }

    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        var term = request.SearchTerm.ToLower();
        items = items.Where(i =>
            i.TableName.ToLower().Contains(term) ||
            i.ColumnName.ToLower().Contains(term));
    }

    var total = items.Count();
    var data = items.Skip(request.Skip).Take(request.PageSize).ToList();

    return Json(PaginationResponse<ComparisonItem>.Create(data, total, request));
}
```

### 6. Views/DataDictionary/Index.cshtml

**Replace entire kendoGrid with DataTables:**

```html
@{
    ViewData["Title"] = "Data Dictionary";
}

<div class="container-fluid">
    <h2>Data Dictionary</h2>

    <!-- Search and filters -->
    <div class="row mb-3">
        <div class="col-md-4">
            <input type="text" id="globalSearch" class="form-control" placeholder="Search columns, tables, purpose...">
        </div>
        <div class="col-md-2">
            <select id="serverFilter" class="form-select">
                <option value="">All Servers</option>
            </select>
        </div>
        <div class="col-md-2">
            <select id="databaseFilter" class="form-select">
                <option value="">All Databases</option>
            </select>
        </div>
        <div class="col-md-2">
            <button id="clearFilters" class="btn btn-outline-secondary">Clear Filters</button>
        </div>
        <div class="col-md-2 text-end">
            <a href="@Url.Action("Export", "DataDictionary")" class="btn btn-success">
                <i class="bi bi-download"></i> Export CSV
            </a>
        </div>
    </div>

    <!-- Data table -->
    <table id="dataDictionaryGrid" class="table table-striped table-hover" style="width:100%">
        <thead>
            <tr>
                <th>Server</th>
                <th>Database</th>
                <th>Schema</th>
                <th>Table</th>
                <th>Column</th>
                <th>Data Type</th>
                <th>Nullable</th>
                <th>PK</th>
                <th>Purpose</th>
                <th>Notes</th>
                <th>Actions</th>
            </tr>
        </thead>
    </table>
</div>

@section Scripts {
<script>
$(document).ready(function() {
    var table = $('#dataDictionaryGrid').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '@Url.Action("Read", "DataDictionary")',
            type: 'POST',
            contentType: 'application/json',
            data: function(d) {
                return JSON.stringify({
                    page: Math.floor(d.start / d.length) + 1,
                    pageSize: d.length,
                    sortField: d.columns[d.order[0]?.column]?.data,
                    sortDirection: d.order[0]?.dir || 'asc',
                    searchTerm: d.search?.value,
                    filters: {
                        server: $('#serverFilter').val(),
                        database: $('#databaseFilter').val()
                    }
                });
            },
            dataSrc: function(json) {
                json.recordsTotal = json.total;
                json.recordsFiltered = json.total;
                return json.data;
            }
        },
        columns: [
            { data: 'databaseServer' },
            { data: 'databaseName' },
            { data: 'schemaName' },
            { data: 'tableName' },
            { data: 'columnName' },
            { data: 'dataType' },
            { data: 'isNullable', render: function(data) { return data ? 'Yes' : 'No'; } },
            { data: 'isPrimaryKey', render: function(data) { return data ? '<span class="badge bg-primary">PK</span>' : ''; } },
            { data: 'purpose', className: 'editable-cell', render: function(data) { return data || '<em class="text-muted">Click to edit</em>'; } },
            { data: 'notes', className: 'editable-cell', render: function(data) { return data || '<em class="text-muted">Click to edit</em>'; } },
            { data: 'id', orderable: false, render: function(data, type, row) {
                return '<button class="btn btn-sm btn-outline-primary edit-btn" data-id="' + data + '">Edit</button>';
            }}
        ],
        pageLength: 50,
        lengthMenu: [[25, 50, 100, 200], [25, 50, 100, 200]],
        order: [[3, 'asc'], [4, 'asc']]
    });

    // Global search with debounce
    var searchTimeout;
    $('#globalSearch').on('keyup', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            table.search($('#globalSearch').val()).draw();
        }, 300);
    });

    // Filter dropdowns
    $('#serverFilter, #databaseFilter').on('change', function() {
        table.ajax.reload();
    });

    // Clear filters
    $('#clearFilters').on('click', function() {
        $('#globalSearch').val('');
        $('#serverFilter').val('');
        $('#databaseFilter').val('');
        table.search('').draw();
    });

    // Inline editing
    $('#dataDictionaryGrid').on('click', '.editable-cell', function() {
        var cell = $(this);
        if (cell.hasClass('editing')) return;

        var currentValue = cell.text().trim();
        if (currentValue === 'Click to edit') currentValue = '';

        var input = $('<input type="text" class="form-control form-control-sm">').val(currentValue);
        cell.addClass('editing').html(input);
        input.focus().select();

        input.on('blur keydown', function(e) {
            if (e.type === 'blur' || e.key === 'Enter') {
                var newValue = input.val();
                cell.removeClass('editing').text(newValue || 'Click to edit');

                if (newValue !== currentValue) {
                    var rowData = table.row(cell.closest('tr')).data();
                    var field = table.column(cell).dataSrc();
                    rowData[field] = newValue;

                    $.ajax({
                        url: '@Url.Action("Update", "DataDictionary")',
                        type: 'POST',
                        contentType: 'application/json',
                        data: JSON.stringify(rowData),
                        success: function(response) {
                            if (!response.success) {
                                DataTableHelpers.alert('Failed to save: ' + response.errors?.join(', '));
                                cell.text(currentValue || 'Click to edit');
                            }
                        }
                    });
                }
            }
            if (e.key === 'Escape') {
                cell.removeClass('editing').text(currentValue || 'Click to edit');
            }
        });
    });
});
</script>
}
```

### 7. Views/Comparison/Index.cshtml

Replace Kendo grid with DataTables (similar pattern to above).

### 8. Views/EfModelSources/Index.cshtml

Replace Kendo grid with DataTables.

### 9. Views/EfModelSources/Create.cshtml & Edit.cshtml

**Replace:**
```javascript
// OLD: kendo.alert('Message');
// NEW:
DataTableHelpers.alert('Message');

// OLD: kendo.ui.progress($('body'), true);
// NEW:
DataTableHelpers.showProgress(true);

// OLD: kendo.confirm('Are you sure?');
// NEW:
DataTableHelpers.confirm('Are you sure?').then(function(confirmed) {
    if (confirmed) { /* proceed */ }
});
```

## Implementation Steps

### Step 1: Create New Files
1. Create `Models/PaginationRequest.cs`
2. Create `Models/PaginationResponse.cs`
3. Create `wwwroot/js/datatables-helpers.js`
4. Create `wwwroot/css/datatables-custom.css`

### Step 2: Update Dependencies
1. Remove Telerik package from .csproj
2. Remove AddKendo() from Program.cs
3. Update _Layout.cshtml with DataTables CDN

### Step 3: Update Controllers
1. Update DataDictionaryController (Read, Update methods)
2. Update ComparisonController (GetResults method)

### Step 4: Update Views
1. Replace DataDictionary/Index.cshtml grid
2. Replace Comparison/Index.cshtml grid
3. Replace EfModelSources/Index.cshtml grid
4. Update Create/Edit views (alerts, progress)

### Step 5: Delete Old Files
1. Delete wwwroot/js/kendo-ui-license.js

### Step 6: Test
1. Build solution
2. Test each grid (paging, sorting, filtering)
3. Test inline editing
4. Test dialogs (alert, confirm)

## Testing Checklist

- [ ] Solution builds without Kendo references
- [ ] DataDictionary grid loads with server-side paging
- [ ] DataDictionary inline editing works
- [ ] DataDictionary export to CSV works
- [ ] Comparison grid loads and filters by status
- [ ] EfModelSources grid loads with actions
- [ ] Alert dialogs work (validation messages)
- [ ] Confirm dialogs work (delete confirmation)
- [ ] Progress spinners show during AJAX calls
- [ ] All grids are responsive on mobile

## Rollback Plan

If issues arise:
1. Restore Telerik package: `dotnet add package Telerik.UI.for.AspNet.Core`
2. Revert files from git
3. Or implement hybrid: keep Kendo for complex grids, use DataTables for simple ones
