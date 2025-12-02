using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Kendo UI services
builder.Services.AddKendo();

// Add DbContext
builder.Services.AddDbContext<DataDictionaryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DataDictionary")));

// Add application services
builder.Services.AddScoped<IDataDictionaryService, DataDictionaryService>();
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();

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
