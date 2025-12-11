# Phase 4: UI Layer

## Objective

Create the Details page accessible from the Data Dictionary grid, showing current column state and audit history.

## Prerequisites

- Phase 1 complete (entity and migration)
- Phase 2 complete (service methods)
- Phase 3 complete (ViewModels)

## Tasks

### 4.1 Add `GetDetailsAsync` Method to Service Interface

**File**: `/src/NetSqlDataDicV2.Web/Services/IDataDictionaryService.cs`

Add method signature:

```csharp
Task<DataElementDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
```

---

### 4.2 Implement `GetDetailsAsync` in Service

**File**: `/src/NetSqlDataDicV2.Web/Services/DataDictionaryService.cs`

```csharp
public async Task<DataElementDetailsViewModel?> GetDetailsAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    var element = await GetByIdAsync(id, cancellationToken);
    if (element == null)
    {
        return null;
    }

    var auditHistory = await GetAuditHistoryAsync(id, cancellationToken);

    return new DataElementDetailsViewModel
    {
        DataElement = element,
        AuditHistory = auditHistory
    };
}
```

---

### 4.3 Add Details Action to Controller

**File**: `/src/NetSqlDataDicV2.Web/Controllers/DataDictionaryController.cs`

```csharp
[HttpGet]
public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
{
    var model = await _service.GetDetailsAsync(id, cancellationToken);

    if (model == null)
    {
        return NotFound();
    }

    return View(model);
}
```

---

### 4.4 Create Details View

**File**: `/src/NetSqlDataDicV2.Web/Views/DataDictionary/Details.cshtml`

```html
@model NetSqlDataDicV2.Web.Models.ViewModels.DataElementDetailsViewModel
@{
    ViewData["Title"] = $"Column Details: {Model.DataElement.TableName}.{Model.DataElement.ColumnName}";
}

<div class="d-flex justify-content-between align-items-center mb-3">
    <div>
        <h2>@Model.DataElement.TableName.@Model.DataElement.ColumnName</h2>
        <small class="text-muted">
            @Model.DataElement.DatabaseServer / @Model.DataElement.DatabaseName / @Model.DataElement.SchemaName
        </small>
    </div>
    <a asp-action="Index" class="btn btn-outline-secondary btn-sm">
        <i class="bi bi-arrow-left"></i> Back to Grid
    </a>
</div>

<!-- Status Badge -->
@if (Model.DataElement.IsDeleted)
{
    <div class="alert alert-warning mb-3">
        <strong>Note:</strong> This column has been removed from the source database.
    </div>
}

<!-- Current State Card -->
<div class="card mb-4">
    <div class="card-header">
        <strong>Current State</strong>
        @if (Model.FirstSeen.HasValue)
        {
            <small class="text-muted float-end">First seen: @Model.FirstSeen.Value.ToString("yyyy-MM-dd")</small>
        }
    </div>
    <div class="card-body">
        <div class="row">
            <div class="col-md-6">
                <table class="table table-sm table-borderless mb-0">
                    <tr>
                        <th style="width:140px">Schema</th>
                        <td>@Model.DataElement.SchemaName</td>
                    </tr>
                    <tr>
                        <th>Table</th>
                        <td>@Model.DataElement.TableName</td>
                    </tr>
                    <tr>
                        <th>Column</th>
                        <td>@Model.DataElement.ColumnName</td>
                    </tr>
                    <tr>
                        <th>Data Type</th>
                        <td><code>@Model.DataElement.DataType</code></td>
                    </tr>
                    <tr>
                        <th>Max Length</th>
                        <td>@(Model.DataElement.MaxLength?.ToString() ?? "-")</td>
                    </tr>
                </table>
            </div>
            <div class="col-md-6">
                <table class="table table-sm table-borderless mb-0">
                    <tr>
                        <th style="width:140px">Nullable</th>
                        <td>
                            @if (Model.DataElement.IsNullable)
                            {
                                <span class="badge bg-secondary">Yes</span>
                            }
                            else
                            {
                                <span class="badge bg-primary">No</span>
                            }
                        </td>
                    </tr>
                    <tr>
                        <th>Primary Key</th>
                        <td>
                            @if (Model.DataElement.IsPrimaryKey)
                            {
                                <span class="badge bg-success">Yes</span>
                            }
                            else
                            {
                                <span>No</span>
                            }
                        </td>
                    </tr>
                    <tr>
                        <th>Foreign Key</th>
                        <td>@(Model.DataElement.ForeignKeyTo ?? "-")</td>
                    </tr>
                    <tr>
                        <th>Last Sync</th>
                        <td>@(Model.DataElement.LastSyncTime?.ToString("yyyy-MM-dd HH:mm") ?? "Never")</td>
                    </tr>
                    <tr>
                        <th>Status</th>
                        <td>
                            @if (Model.DataElement.IsDeleted)
                            {
                                <span class="badge bg-danger">Deleted</span>
                            }
                            else
                            {
                                <span class="badge bg-success">Active</span>
                            }
                        </td>
                    </tr>
                </table>
            </div>
        </div>

        @if (!string.IsNullOrEmpty(Model.DataElement.DataPurpose) || !string.IsNullOrEmpty(Model.DataElement.Notes))
        {
            <hr />
            <div class="row">
                @if (!string.IsNullOrEmpty(Model.DataElement.DataPurpose))
                {
                    <div class="col-md-6">
                        <strong>Purpose:</strong>
                        <p class="mb-0">@Model.DataElement.DataPurpose</p>
                    </div>
                }
                @if (!string.IsNullOrEmpty(Model.DataElement.Notes))
                {
                    <div class="col-md-6">
                        <strong>Notes:</strong>
                        <p class="mb-0">@Model.DataElement.Notes</p>
                    </div>
                }
            </div>
        }
    </div>
</div>

<!-- Audit History Card -->
<div class="card">
    <div class="card-header d-flex justify-content-between align-items-center">
        <strong>Change History</strong>
        <span class="badge bg-secondary">@Model.TotalChanges change(s)</span>
    </div>
    <div class="card-body p-0">
        @if (Model.AuditHistory.Any())
        {
            <div class="table-responsive">
                <table id="auditHistoryGrid" class="table table-striped table-hover table-sm mb-0" style="width:100%">
                    <thead class="table-light">
                        <tr>
                            <th>Date/Time</th>
                            <th>Change Type</th>
                            <th>Property</th>
                            <th>Old Value</th>
                            <th>New Value</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var audit in Model.AuditHistory)
                        {
                            <tr>
                                <td>@audit.ChangeTime.ToString("yyyy-MM-dd HH:mm:ss")</td>
                                <td>
                                    <span class="badge @audit.ChangeTypeBadgeClass">@audit.ChangeType</span>
                                </td>
                                <td>@(audit.PropertyName ?? "-")</td>
                                <td>
                                    @if (audit.OldValue != null)
                                    {
                                        <code>@audit.OldValue</code>
                                    }
                                    else
                                    {
                                        <span class="text-muted">-</span>
                                    }
                                </td>
                                <td>
                                    @if (audit.NewValue != null)
                                    {
                                        <code>@audit.NewValue</code>
                                    }
                                    else
                                    {
                                        <span class="text-muted">-</span>
                                    }
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        }
        else
        {
            <div class="p-4 text-center text-muted">
                <em>No change history recorded for this column.</em>
            </div>
        }
    </div>
</div>

@section Scripts {
    <script>
        $(document).ready(function () {
            if ($('#auditHistoryGrid tbody tr').length > 0) {
                $('#auditHistoryGrid').DataTable({
                    pageLength: 25,
                    lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
                    order: [[0, 'desc']],
                    scrollX: true,
                    language: {
                        emptyTable: "No changes recorded"
                    },
                    columnDefs: [
                        { width: '160px', targets: 0 },
                        { width: '100px', targets: 1 },
                        { width: '120px', targets: 2 }
                    ]
                });
            }
        });
    </script>
}
```

---

### 4.5 Update Index View - Add Details Link

**File**: `/src/NetSqlDataDicV2.Web/Views/DataDictionary/Index.cshtml`

Find the columns definition in the DataTables initialization (around line 139-145) and update the Actions column:

**Current** (approximate):
```javascript
{
    data: 'dataElementId',
    orderable: false,
    className: 'text-center',
    render: function(data, type, row) {
        return '<button class="btn btn-sm btn-link p-0 edit-row-btn" data-id="' + data + '" title="Edit">Edit</button>';
    }
}
```

**Updated**:
```javascript
{
    data: 'dataElementId',
    orderable: false,
    className: 'text-center',
    render: function(data, type, row) {
        return '<a href="@Url.Action("Details", "DataDictionary")/' + data + '" class="btn btn-sm btn-outline-primary me-1" title="View Details">Details</a>' +
               '<button class="btn btn-sm btn-outline-secondary edit-row-btn" data-id="' + data + '" title="Edit">Edit</button>';
    }
}
```

Alternatively, make the column name clickable:

```javascript
{
    data: 'columnName',
    render: function(data, type, row) {
        if (type === 'display') {
            return '<a href="@Url.Action("Details", "DataDictionary")/' + row.dataElementId + '">' + data + '</a>';
        }
        return data;
    }
}
```

---

## Verification

After completing Phase 4:

1. Build succeeds: `dotnet build`
2. Run the application
3. Navigate to Data Dictionary grid
4. Click "Details" on any row
5. Verify Details page shows:
   - Current state card with all properties
   - Audit history grid (may be empty for existing records)
6. Run a sync operation
7. Return to Details page and verify audit records appear
8. Test DataTables functionality (sorting, paging)

## Manual Testing Checklist

- [ ] Details link visible in grid
- [ ] Details page loads without errors
- [ ] Current state displays all properties correctly
- [ ] Audit history grid renders with DataTables
- [ ] Sorting by date works (newest first by default)
- [ ] Badge colors correct (green=Added, red=Deleted, yellow=Modified, blue=Restored)
- [ ] "Back to Grid" button works
- [ ] Empty audit history shows appropriate message
- [ ] Deleted columns show warning banner

## Dependencies

- Phase 1, 2, 3 complete
- Bootstrap 5 (already included)
- DataTables (already included via CDN)

## Future Enhancements (Out of Scope)

1. Filter audit history by change type
2. Export audit history to CSV
3. Link to SyncHistory details from audit records
4. Inline editing from Details page
