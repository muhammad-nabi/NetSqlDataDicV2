using NetSqlDataDicV2.Web.Models.Entities;

namespace NetSqlDataDicV2.Tests.TestHelpers;

/// <summary>
/// Builder for creating DataElement test instances with sensible defaults.
/// </summary>
public class DataElementBuilder
{
    private readonly DataElement _element = new()
    {
        DataElementName = "Test Column",
        DataElementType = "Column",
        DataType = "NVARCHAR(100)",
        DatabaseServer = "TestServer",
        DatabaseName = "TestDb",
        SchemaName = "dbo",
        TableName = "TestTable",
        ColumnName = "TestColumn",
        IsNullable = true,
        IsPrimaryKey = false,
        MaxLength = 100,
        IsDeleted = false,
        CreateTime = DateTime.UtcNow,
        LastUpdateTime = DateTime.UtcNow
    };

    public DataElementBuilder WithId(int id)
    {
        _element.DataElementId = id;
        return this;
    }

    public DataElementBuilder WithName(string name)
    {
        _element.DataElementName = name;
        return this;
    }

    public DataElementBuilder WithServer(string server)
    {
        _element.DatabaseServer = server;
        return this;
    }

    public DataElementBuilder WithDatabase(string database)
    {
        _element.DatabaseName = database;
        return this;
    }

    public DataElementBuilder WithSchema(string schema)
    {
        _element.SchemaName = schema;
        return this;
    }

    public DataElementBuilder WithTable(string table)
    {
        _element.TableName = table;
        return this;
    }

    public DataElementBuilder WithColumn(string column)
    {
        _element.ColumnName = column;
        return this;
    }

    public DataElementBuilder WithLocation(string server, string database, string schema, string table, string column)
    {
        _element.DatabaseServer = server;
        _element.DatabaseName = database;
        _element.SchemaName = schema;
        _element.TableName = table;
        _element.ColumnName = column;
        return this;
    }

    public DataElementBuilder WithDataType(string dataType)
    {
        _element.DataType = dataType;
        return this;
    }

    public DataElementBuilder WithMaxLength(int? maxLength)
    {
        _element.MaxLength = maxLength;
        return this;
    }

    public DataElementBuilder WithPrecisionScale(int? precision, int? scale)
    {
        _element.Precision = precision;
        _element.Scale = scale;
        return this;
    }

    public DataElementBuilder AsNullable(bool isNullable = true)
    {
        _element.IsNullable = isNullable;
        return this;
    }

    public DataElementBuilder AsPrimaryKey(bool isPrimaryKey = true)
    {
        _element.IsPrimaryKey = isPrimaryKey;
        return this;
    }

    public DataElementBuilder AsDeleted(bool isDeleted = true)
    {
        _element.IsDeleted = isDeleted;
        return this;
    }

    public DataElementBuilder WithPurpose(string? dataPurpose, string? entityPurpose = null)
    {
        _element.DataPurpose = dataPurpose;
        _element.EntityPurpose = entityPurpose;
        return this;
    }

    public DataElementBuilder WithNotes(string? notes)
    {
        _element.Notes = notes;
        return this;
    }

    public DataElementBuilder WithForeignKey(string? foreignKeyTo)
    {
        _element.ForeignKeyTo = foreignKeyTo;
        return this;
    }

    public DataElementBuilder WithTimestamps(DateTime? createTime = null, DateTime? updateTime = null, DateTime? syncTime = null)
    {
        if (createTime.HasValue) _element.CreateTime = createTime.Value;
        if (updateTime.HasValue) _element.LastUpdateTime = updateTime.Value;
        _element.LastSyncTime = syncTime;
        return this;
    }

    public DataElement Build() => _element;
}

/// <summary>
/// Builder for creating EfModelSource test instances with sensible defaults.
/// </summary>
public class EfModelSourceBuilder
{
    private readonly EfModelSource _source = new()
    {
        Name = "TestSource",
        ProviderType = "DynamicDll",
        AssemblyPath = "/path/to/test.dll",
        DbContextTypeName = "TestApp.TestDbContext",
        TargetServer = "TestServer",
        TargetDatabase = "TestDb",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public EfModelSourceBuilder WithId(int id)
    {
        _source.Id = id;
        return this;
    }

    public EfModelSourceBuilder WithName(string name)
    {
        _source.Name = name;
        return this;
    }

    public EfModelSourceBuilder WithAssemblyPath(string path)
    {
        _source.AssemblyPath = path;
        return this;
    }

    public EfModelSourceBuilder WithDbContextType(string typeName)
    {
        _source.DbContextTypeName = typeName;
        return this;
    }

    public EfModelSourceBuilder WithConnectionString(string? connectionString)
    {
        _source.ConnectionString = connectionString;
        return this;
    }

    public EfModelSourceBuilder WithTarget(string server, string database)
    {
        _source.TargetServer = server;
        _source.TargetDatabase = database;
        return this;
    }

    public EfModelSourceBuilder AsActive(bool isActive = true)
    {
        _source.IsActive = isActive;
        return this;
    }

    public EfModelSourceBuilder WithDescription(string? description)
    {
        _source.Description = description;
        return this;
    }

    public EfModelSourceBuilder WithLastCompared(DateTime? lastCompared)
    {
        _source.LastComparedAt = lastCompared;
        return this;
    }

    public EfModelSource Build() => _source;
}

/// <summary>
/// Builder for creating SyncHistory test instances with sensible defaults.
/// </summary>
public class SyncHistoryBuilder
{
    private readonly SyncHistory _history = new()
    {
        DatabaseServer = "TestServer",
        DatabaseName = "TestDb",
        SyncStartTime = DateTime.UtcNow,
        Status = "Running"
    };

    public SyncHistoryBuilder WithId(int id)
    {
        _history.SyncHistoryId = id;
        return this;
    }

    public SyncHistoryBuilder WithServer(string server)
    {
        _history.DatabaseServer = server;
        return this;
    }

    public SyncHistoryBuilder WithDatabase(string database)
    {
        _history.DatabaseName = database;
        return this;
    }

    public SyncHistoryBuilder WithStartTime(DateTime startTime)
    {
        _history.SyncStartTime = startTime;
        return this;
    }

    public SyncHistoryBuilder WithEndTime(DateTime? endTime)
    {
        _history.SyncEndTime = endTime;
        return this;
    }

    public SyncHistoryBuilder AsCompleted(int tablesProcessed = 5, int columnsProcessed = 50, int added = 10, int updated = 5, int removed = 2)
    {
        _history.Status = "Completed";
        _history.SyncEndTime = DateTime.UtcNow;
        _history.TablesProcessed = tablesProcessed;
        _history.ColumnsProcessed = columnsProcessed;
        _history.ColumnsAdded = added;
        _history.ColumnsUpdated = updated;
        _history.ColumnsRemoved = removed;
        return this;
    }

    public SyncHistoryBuilder AsFailed(string errorMessage)
    {
        _history.Status = "Failed";
        _history.SyncEndTime = DateTime.UtcNow;
        _history.ErrorMessage = errorMessage;
        return this;
    }

    public SyncHistory Build() => _history;
}

/// <summary>
/// Builder for creating DataElementAudit test instances with sensible defaults.
/// </summary>
public class DataElementAuditBuilder
{
    private readonly DataElementAudit _audit = new()
    {
        ChangeType = "Added",
        ChangeTime = DateTime.UtcNow
    };

    public DataElementAuditBuilder WithId(int id)
    {
        _audit.DataElementAuditId = id;
        return this;
    }

    public DataElementAuditBuilder ForDataElement(int dataElementId)
    {
        _audit.DataElementId = dataElementId;
        return this;
    }

    public DataElementAuditBuilder ForDataElement(DataElement dataElement)
    {
        _audit.DataElement = dataElement;
        _audit.DataElementId = dataElement.DataElementId;
        return this;
    }

    public DataElementAuditBuilder ForSyncHistory(int syncHistoryId)
    {
        _audit.SyncHistoryId = syncHistoryId;
        return this;
    }

    public DataElementAuditBuilder ForSyncHistory(SyncHistory syncHistory)
    {
        _audit.SyncHistory = syncHistory;
        _audit.SyncHistoryId = syncHistory.SyncHistoryId;
        return this;
    }

    public DataElementAuditBuilder AsAdded(string? newValue = null)
    {
        _audit.ChangeType = "Added";
        _audit.NewValue = newValue;
        return this;
    }

    public DataElementAuditBuilder AsModified(string propertyName, string? oldValue, string? newValue)
    {
        _audit.ChangeType = "Modified";
        _audit.PropertyName = propertyName;
        _audit.OldValue = oldValue;
        _audit.NewValue = newValue;
        return this;
    }

    public DataElementAuditBuilder AsDeleted()
    {
        _audit.ChangeType = "Deleted";
        _audit.OldValue = "Active";
        _audit.NewValue = "Deleted";
        return this;
    }

    public DataElementAuditBuilder AsRestored()
    {
        _audit.ChangeType = "Restored";
        _audit.OldValue = "Deleted";
        _audit.NewValue = "Active";
        return this;
    }

    public DataElementAuditBuilder WithChangeTime(DateTime changeTime)
    {
        _audit.ChangeTime = changeTime;
        return this;
    }

    public DataElementAudit Build() => _audit;
}

/// <summary>
/// Builder for creating DataElementNote test instances with sensible defaults.
/// </summary>
public class DataElementNoteBuilder
{
    private readonly DataElementNote _note = new()
    {
        NoteText = "Test note content",
        CreatedAt = DateTime.UtcNow
    };

    public DataElementNoteBuilder WithId(int id)
    {
        _note.DataElementNoteId = id;
        return this;
    }

    public DataElementNoteBuilder ForDataElement(int dataElementId)
    {
        _note.DataElementId = dataElementId;
        return this;
    }

    public DataElementNoteBuilder ForDataElement(DataElement dataElement)
    {
        _note.DataElement = dataElement;
        _note.DataElementId = dataElement.DataElementId;
        return this;
    }

    public DataElementNoteBuilder WithText(string text)
    {
        _note.NoteText = text;
        return this;
    }

    public DataElementNoteBuilder WithCreatedAt(DateTime createdAt)
    {
        _note.CreatedAt = createdAt;
        return this;
    }

    public DataElementNote Build() => _note;
}
