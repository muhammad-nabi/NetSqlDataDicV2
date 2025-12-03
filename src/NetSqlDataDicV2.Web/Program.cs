using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration
var dataDictionaryConnectionString = builder.Configuration.GetConnectionString("DataDictionary")
    ?? throw new InvalidOperationException("DataDictionary connection string is required. Configure it in appsettings.json.");

var sourceConnectionString = builder.Configuration.GetConnectionString("SourceDatabase");
if (string.IsNullOrEmpty(sourceConnectionString))
{
    Console.WriteLine("WARNING: SourceDatabase connection string not configured. Sync and Comparison features may not work.");
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

// Add DbContext for Data Dictionary
builder.Services.AddDbContext<DataDictionaryDbContext>(options =>
    options.UseSqlServer(dataDictionaryConnectionString));

// Add SourceDbContext for EF model comparison (read-only, for model reflection)
if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddDbContext<SourceDbContext>(options =>
        options.UseSqlServer(sourceConnectionString));
}

// Add application services
builder.Services.AddScoped<IDataDictionaryService, DataDictionaryService>();
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();

if (!string.IsNullOrEmpty(sourceConnectionString))
{
    builder.Services.AddScoped<IEfModelService, EfModelService>();
    builder.Services.AddScoped<IComparisonService, ComparisonService>();
}

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
