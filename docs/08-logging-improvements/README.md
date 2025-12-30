# Logging Improvements

This folder contains phased implementation plans for improving logging across the NetSqlDataDicV2 application.

## Current State

The codebase has a solid logging foundation:

| Component | Status |
|-----------|--------|
| `ILogger<T>` injection | 17 services/controllers |
| Security audit logging | `ISecurityAuditService` |
| Exception middleware | Correlation IDs |
| Log levels | Info, Warning, Debug, Error |
| Structured logging | Named parameters |

## Identified Gaps

| Gap | Impact | Phase |
|-----|--------|-------|
| HomeController unused logger | Low | 1 |
| No EF SQL logging in Development | Low | 1 |
| Silent shadow copy fallback | Low | 1 |
| No request/response middleware | Medium | 2 |
| Controllers only log errors | Medium | 2 |
| No performance timing | Medium | 2 |
| Hardcoded "system" user | Medium | 3 |

## Phase Summary

| Phase | Description | Status | Files |
|-------|-------------|--------|-------|
| [Phase 1](phase1.md) | Quick Wins | Pending | 3 files |
| [Phase 2](phase2.md) | Operational Visibility | Pending | 8 files |
| [Phase 3](phase3.md) | User Context & Audit | Pending | 1 file |

## Not Recommended

The following were considered but deemed over-engineering for this application:

- **Serilog migration**: Built-in logging is adequate for single-instance deployment
- **Centralized logging**: Not needed unless multi-instance
- **HTTP body logging**: Security and performance concerns outweigh benefits

## Key Files Reference

| Category | File Path |
|----------|-----------|
| Configuration | `src/NetSqlDataDicV2.Web/Program.cs` |
| Settings | `src/NetSqlDataDicV2.Web/appsettings*.json` |
| Exception Handling | `src/NetSqlDataDicV2.Web/Middleware/ExceptionHandlingMiddleware.cs` |
| Security Audit | `src/NetSqlDataDicV2.Web/Services/Security/SecurityAuditService.cs` |
