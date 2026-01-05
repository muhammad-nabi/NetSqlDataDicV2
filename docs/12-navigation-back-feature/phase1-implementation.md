# Phase 1: Implementation

## Status: Complete

## Objective

Add an optional "Exit" link to the navbar that navigates to the consumer application.

---

## Step 1: Add Configuration Property

### File
`src/DataDictionary.AspNetCore/Configuration/DataDictionaryOptions.cs`

### Change
Add new property after existing properties:

```csharp
/// <summary>
/// Optional URL to navigate back to the consumer application.
/// If configured, an "Exit" link will appear in the navbar.
/// Example: "/" or "/dashboard"
/// </summary>
public string? ConsumerApplicationUrl { get; set; }
```

---

## Step 2: Inject into ViewBag

### File
`src/DataDictionary.AspNetCore/Areas/DataDictionary/Controllers/DataDictionaryControllerBase.cs`

### Change
In `OnActionExecuting` method, add after the RoutePrefix injection:

```csharp
ViewBag.ConsumerApplicationUrl = _options.ConsumerApplicationUrl;
```

### Context
The existing code looks like:

```csharp
public override void OnActionExecuting(ActionExecutingContext context)
{
    var prefix = _options.RoutePrefix.TrimStart('/').TrimEnd('/');
    ViewBag.RoutePrefix = "/" + prefix;
    // ADD HERE: ViewBag.ConsumerApplicationUrl = _options.ConsumerApplicationUrl;
    base.OnActionExecuting(context);
}
```

---

## Step 3: Add Exit Link to Navbar

### File
`src/DataDictionary.AspNetCore/Areas/DataDictionary/Views/Shared/_Layout.cshtml`

### Change
Add conditional "Exit" link after the existing `</ul>` (nav links) and before the closing `</div>` of the navbar:

```html
@if (!string.IsNullOrEmpty(ViewBag.ConsumerApplicationUrl))
{
    <ul class="navbar-nav ms-auto">
        <li class="nav-item">
            <a class="nav-link" href="@ViewBag.ConsumerApplicationUrl">Exit</a>
        </li>
    </ul>
}
```

### Location in File
```html
            <!-- Existing nav links -->
            <ul class="navbar-nav">
                <li class="nav-item">
                    <a class="nav-link" href="@ViewBag.RoutePrefix">Home</a>
                </li>
                <!-- ... other links ... -->
            </ul>

            <!-- ADD EXIT LINK HERE -->
            @if (!string.IsNullOrEmpty(ViewBag.ConsumerApplicationUrl))
            {
                <ul class="navbar-nav ms-auto">
                    <li class="nav-item">
                        <a class="nav-link" href="@ViewBag.ConsumerApplicationUrl">Exit</a>
                    </li>
                </ul>
            }
        </div>
    </div>
</nav>
```

### Notes
- `ms-auto` (Bootstrap margin-start auto) pushes the Exit link to the far right
- Conditional rendering ensures backward compatibility
- Uses standard Bootstrap navbar styling

---

## Step 4: Update Documentation

### File
`docs/INSTALLATION.md`

### Change 1: Add to DataDictionaryOptions Table

Add new row to the options table:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ConsumerApplicationUrl` | string? | `null` | URL for "Exit" link to return to consumer app |

### Change 2: Add Configuration Section

Add under "Optional Configuration" section:

```markdown
### Exit Link to Consumer Application

Add an "Exit" link in the navbar to return to your main application:

```json
{
  "DataDictionary": {
    "ConsumerApplicationUrl": "/"
  }
}
```

> **Note:** The "Exit" link only appears when this property is configured.
```

---

## Verification Checklist

- [ ] `ConsumerApplicationUrl` property added to `DataDictionaryOptions`
- [ ] ViewBag injection added to `DataDictionaryControllerBase`
- [ ] Conditional Exit link added to `_Layout.cshtml`
- [ ] Exit link appears on right side of navbar when configured
- [ ] Exit link is hidden when not configured (backward compatible)
- [ ] Documentation updated in `INSTALLATION.md`
- [ ] Solution builds without errors
- [ ] All existing tests pass

---

## Files Summary

| File | Change |
|------|--------|
| `src/DataDictionary.AspNetCore/Configuration/DataDictionaryOptions.cs` | Add `ConsumerApplicationUrl` property |
| `src/DataDictionary.AspNetCore/Areas/DataDictionary/Controllers/DataDictionaryControllerBase.cs` | Inject `ConsumerApplicationUrl` to ViewBag |
| `src/DataDictionary.AspNetCore/Areas/DataDictionary/Views/Shared/_Layout.cshtml` | Add conditional "Exit" link |
| `docs/INSTALLATION.md` | Document new configuration option |
