# Navigation Back Feature

Add an optional "Exit" link to the Data Dictionary navbar, allowing users to navigate back to the consumer application.

## Phase Overview

| Phase | Status | Description |
|-------|--------|-------------|
| 1 | Complete | Complete implementation (config, ViewBag, UI, docs) |

## Key Features

- **"Exit" link on right side** of navbar
- **Optional** - only shows when `ConsumerApplicationUrl` is configured
- **Non-breaking** - no changes required for existing consumers
- **Configurable** via appsettings.json

## Problem Statement

When users navigate from a consumer application to the Data Dictionary (e.g., `/tools/datadictionary`), there is no way to navigate back to the main application. The only option is to manually edit the URL or use browser back button.

## Solution

Add an optional `ConsumerApplicationUrl` configuration property. When configured, an "Exit" link appears on the right side of the navbar.

## Configuration Usage

**appsettings.json:**
```json
{
  "DataDictionary": {
    "RoutePrefix": "tools/datadictionary",
    "ConsumerApplicationUrl": "/"
  }
}
```

## UI Behavior

| Configuration | Result |
|--------------|--------|
| `ConsumerApplicationUrl` not set | No "Exit" link shown |
| `ConsumerApplicationUrl: "/"` | "Exit" link navigates to root |
| `ConsumerApplicationUrl: "/dashboard"` | "Exit" link navigates to /dashboard |

## Files Modified

See phase documentation for details.
