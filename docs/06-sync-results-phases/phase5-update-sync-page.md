# Phase 5: Update Sync Index Page

## Status: Complete

## Objective

Add "View Results" buttons to the sync page - both after sync completes and in the history table.

## Files to Modify

| File | Change |
|------|--------|
| `Views/Sync/Index.cshtml` | Add View Results buttons, remove auto-reload |

## Change 1: AJAX Success Handler

**Location:** Lines ~194-215 (JavaScript success handler)

**Current Code:**
```javascript
$details.html(
    '<ul class="mb-0">' +
    '<li>Tables processed: <strong>' + data.tablesProcessed + '</strong></li>' +
    // ... more list items ...
    '</ul>'
);
// ...
// Reload page after 2 seconds to show updated history
setTimeout(function () {
    location.reload();
}, 2000);
```

**New Code:**
```javascript
$details.html(
    '<ul class="mb-2">' +  // Changed from mb-0 to mb-2
    '<li>Tables processed: <strong>' + data.tablesProcessed + '</strong></li>' +
    '<li>Columns processed: <strong>' + data.columnsProcessed + '</strong></li>' +
    '<li>Added: <strong class="text-success">' + data.columnsAdded + '</strong></li>' +
    '<li>Updated: <strong class="text-warning">' + data.columnsUpdated + '</strong></li>' +
    '<li>Removed: <strong class="text-danger">' + data.columnsRemoved + '</strong></li>' +
    '<li>Duration: <strong>' + data.duration.toFixed(1) + ' seconds</strong></li>' +
    '</ul>' +
    '<a href="@Url.Action("Results", "Sync")/' + data.syncHistoryId + '" class="btn btn-sm btn-outline-primary">' +
    'View Results</a>'
);
// Remove auto-reload - user can click View Results or manually refresh
```

**Changes:**
1. Changed `<ul class="mb-0">` to `<ul class="mb-2">` for spacing before button
2. Added "View Results" button using `data.syncHistoryId`
3. Removed `setTimeout` auto-reload (lines 213-215)

## Change 2: History Table Header

**Location:** Lines 85-95

**Current Code:**
```html
<thead class="table-light">
    <tr>
        <th>Started</th>
        <th>Server / Database</th>
        <th>Status</th>
        <th>Duration</th>
        <th>Tables</th>
        <th>Columns</th>
        <th>Added</th>
        <th>Updated</th>
        <th>Removed</th>
    </tr>
</thead>
```

**New Code:**
```html
<thead class="table-light">
    <tr>
        <th>Started</th>
        <th>Server / Database</th>
        <th>Status</th>
        <th>Duration</th>
        <th>Tables</th>
        <th>Columns</th>
        <th>Added</th>
        <th>Updated</th>
        <th>Removed</th>
        <th>Actions</th>  <!-- NEW -->
    </tr>
</thead>
```

## Change 3: History Table Body

**Location:** Lines 108-156 (inside foreach loop)

**Add after the Removed column (line ~155), before `</tr>`:**

```html
<td>
    @if (item.Status == "Completed")
    {
        <a asp-action="Results" asp-route-id="@item.SyncHistoryId"
           class="btn btn-sm btn-outline-primary">
            View
        </a>
    }
    else if (item.Status == "Failed")
    {
        <a asp-action="Results" asp-route-id="@item.SyncHistoryId"
           class="btn btn-sm btn-outline-secondary"
           title="View error details">
            View
        </a>
    }
</td>
```

**Notes:**
- "Completed" syncs get primary outline button
- "Failed" syncs get secondary outline button with tooltip
- "Running" syncs don't get a button (no results yet)

## Change 4: Empty Row Colspan

**Location:** Line 101

**Current Code:**
```html
<td colspan="9" class="text-center text-muted py-4">
```

**New Code:**
```html
<td colspan="10" class="text-center text-muted py-4">
```

## Summary of Changes

| Line(s) | Change |
|---------|--------|
| ~195 | Change `mb-0` to `mb-2` |
| ~202 | Add View Results button after `</ul>` |
| 213-215 | Remove `setTimeout` auto-reload |
| 95 | Add `<th>Actions</th>` |
| 101 | Change colspan from 9 to 10 |
| ~155 | Add Actions column with View button |

## Design Notes

1. **No auto-reload** - User can click "View Results" to see details, or manually refresh
2. **Running syncs** - No View button since sync is still in progress
3. **Failed syncs** - Shows View button (secondary style) to see error details
4. **Button styling** - Uses Bootstrap `btn-outline-*` for consistency

## SyncHistoryViewModel Requirement

The `item.SyncHistoryId` property is already available in `SyncHistoryViewModel`:

```csharp
public class SyncHistoryViewModel
{
    public int SyncHistoryId { get; set; }  // Already exists
    // ... other properties
}
```

## Verification

After this phase:
- [ ] Project builds successfully
- [ ] "View Results" button appears after sync completes
- [ ] History table shows "View" button for Completed rows
- [ ] History table shows "View" button for Failed rows
- [ ] No button shown for Running rows
- [ ] Auto-reload is removed
- [ ] Clicking View button navigates to Results page
- [ ] Empty history row spans all 10 columns
