# Phase 1: Data Layer

## Objective

Create the database infrastructure for storing audit trail records.

## Tasks

### 1.1 Create `DataElementAudit` Entity

**File**: `/src/NetSqlDataDicV2.Web/Models/Entities/DataElementAudit.cs`

```csharp
namespace NetSqlDataDicV2.Web.Models.Entities;

public class DataElementAudit
{
    public int DataElementAuditId { get; set; }

    // Foreign Keys
    public int DataElementId { get; set; }
    public DataElement DataElement { get; set; } = null!;

    public int SyncHistoryId { get; set; }
    public SyncHistory SyncHistory { get; set; } = null!;

    // Change tracking
    public string ChangeType { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    // Timestamp
    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;
}
```

**Field Descriptions**:

| Field | Type | Description |
|-------|------|-------------|
| DataElementAuditId | int | Primary key |
| DataElementId | int | FK to DataElement being audited |
| SyncHistoryId | int | FK to sync operation that created this record |
| ChangeType | string | "Added", "Modified", "Deleted", or "Restored" |
| PropertyName | string? | Property that changed (null for Add/Delete/Restore) |
| OldValue | string? | Previous value (null for Added) |
| NewValue | string? | New value (null for Deleted) |
| ChangeTime | DateTime | When the change was recorded |

---

### 1.2 Create Entity Configuration

**File**: `/src/NetSqlDataDicV2.Web/Data/Configurations/DataElementAuditConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Web.Data.Configurations;

public class DataElementAuditConfiguration : IEntityTypeConfiguration<DataElementAudit>
{
    public void Configure(EntityTypeBuilder<DataElementAudit> builder)
    {
        builder.ToTable("DataElementAudits");

        builder.HasKey(e => e.DataElementAuditId);

        // String constraints
        builder.Property(e => e.ChangeType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.PropertyName)
            .HasMaxLength(50);

        builder.Property(e => e.OldValue)
            .HasMaxLength(500);

        builder.Property(e => e.NewValue)
            .HasMaxLength(500);

        // Foreign key to DataElement (cascade delete)
        builder.HasOne(e => e.DataElement)
            .WithMany()
            .HasForeignKey(e => e.DataElementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to SyncHistory (cascade delete)
        builder.HasOne(e => e.SyncHistory)
            .WithMany()
            .HasForeignKey(e => e.SyncHistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance indexes
        builder.HasIndex(e => e.DataElementId)
            .HasDatabaseName("IX_DataElementAudits_DataElementId");

        builder.HasIndex(e => e.SyncHistoryId)
            .HasDatabaseName("IX_DataElementAudits_SyncHistoryId");

        builder.HasIndex(e => e.ChangeTime)
            .HasDatabaseName("IX_DataElementAudits_ChangeTime");
    }
}
```

**Design Decisions**:

1. **Cascade Delete on DataElement**: If a DataElement is hard-deleted, its audit history is also removed
2. **Cascade Delete on SyncHistory**: If a SyncHistory is deleted, associated audits are removed
3. **Indexes**: Three indexes for common query patterns (by element, by sync, by time)
4. **MaxLength 500 for values**: Sufficient for most property values; very long values will be truncated

---

### 1.3 Update DbContext

**File**: `/src/NetSqlDataDicV2.Web/Data/DataDictionaryDbContext.cs`

Add the new DbSet:

```csharp
public DbSet<DataElementAudit> DataElementAudits => Set<DataElementAudit>();
```

The configuration will be auto-discovered via `ApplyConfigurationsFromAssembly`.

---

### 1.4 Create and Apply Migration

**Commands**:

```bash
cd src/NetSqlDataDicV2.Web
dotnet ef migrations add AddDataElementAudit
dotnet ef database update
```

**Expected Migration**:

The migration will create:
- Table `DataElementAudits` with all columns
- Primary key `PK_DataElementAudits`
- Foreign key `FK_DataElementAudits_DataElements_DataElementId`
- Foreign key `FK_DataElementAudits_SyncHistory_SyncHistoryId`
- Index `IX_DataElementAudits_DataElementId`
- Index `IX_DataElementAudits_SyncHistoryId`
- Index `IX_DataElementAudits_ChangeTime`

---

## Verification

After completing Phase 1:

1. Build succeeds: `dotnet build`
2. Migration created in `/Migrations/` folder
3. Database updated with new table
4. Can verify table exists:
   ```sql
   SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataElementAudits'
   ```

## Dependencies

- None (this is the foundation phase)

## Next Phase

Phase 2: Service Layer - Modify sync service to create audit records
