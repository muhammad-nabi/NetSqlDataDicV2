using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Configuration;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Services;
using NetSqlDataDicV2.Web.Services.DbContextProviders;
using NetSqlDataDicV2.Web.Services.Security;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var dataDictionaryConnectionString = builder.Configuration.GetConnectionString("DataDictionary")
    ?? throw new InvalidOperationException("DataDictionary connection string is required. Configure it in appsettings.json.");

var sourceConnectionString = builder.Configuration.GetConnectionString("SourceDatabase");
if (string.IsNullOrEmpty(sourceConnectionString))
{
    Console.WriteLine("WARNING: SourceDatabase connection string not configured. Direct reference comparison will not work, but dynamic DLL loading will still be available.");
}

// Add MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Kendo UI services
builder.Services.AddKendo();

// Add Data Protection (required for connection string encryption)
// Keys are persisted to the application's content root by default in Development
// For Production, configure key storage (Azure Key Vault, AWS, or file system)
builder.Services.AddDataProtection()
    .SetApplicationName("NetSqlDataDicV2");

// Configure DLL security options
builder.Services.Configure<DllSecurityOptions>(
    builder.Configuration.GetSection(DllSecurityOptions.SectionName));

// Register security services
builder.Services.AddScoped<IDllValidatorService, DllValidatorService>();
builder.Services.AddScoped<IConnectionStringProtector, ConnectionStringProtector>();
builder.Services.AddScoped<ISecurityAuditService, SecurityAuditService>();

// Add DbContext for Data Dictionary
builder.Services.AddDbContext<DataDictionaryDbContext>(options =>
    options.UseSqlServer(dataDictionaryConnectionString));

// Add SourceDbContext for EF model comparison (read-only, for model reflection)
// Optional - only registered if connection string is configured
if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddDbContext<SourceDbContext>(options =>
        options.UseSqlServer(sourceConnectionString));
}

// Add DbContext provider factory (always available - supports both direct and DLL loading)
builder.Services.AddScoped<IDbContextProviderFactory, DbContextProviderFactory>();

// Add application services
builder.Services.AddScoped<IDataDictionaryService, DataDictionaryService>();
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();

// EfModelService - now always registered (can work with DLL loading even without SourceDbContext)
builder.Services.AddScoped<IEfModelService, EfModelService>();

// EfModelSourceService - manages EF model source configurations
builder.Services.AddScoped<IEfModelSourceService, EfModelSourceService>();

// ComparisonService - now always registered
builder.Services.AddScoped<IComparisonService, ComparisonService>();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
