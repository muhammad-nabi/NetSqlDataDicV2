using Microsoft.AspNetCore.DataProtection;
using DataDictionary.AspNetCore.Core.Extensions;
using NetSqlDataDicV2.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add MVC with JSON options
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Data Protection (required for connection string encryption)
// Keys are persisted to the application's content root by default in Development
// For Production, configure key storage (Azure Key Vault, AWS, or file system)
builder.Services.AddDataProtection()
    .SetApplicationName("NetSqlDataDicV2");

// Add all Data Dictionary Core services (DbContext, business services, security services)
builder.Services.AddDataDictionaryCore(builder.Configuration);

var app = builder.Build();

// Configure pipeline
// Add request logging first to capture all requests including errors
app.UseMiddleware<RequestLoggingMiddleware>();
// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Area routing for the DataDictionary RCL
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Default route redirects to DataDictionary area
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Redirect root to DataDictionary area
app.MapGet("/", context =>
{
    context.Response.Redirect("/DataDictionary");
    return Task.CompletedTask;
});

app.Run();
