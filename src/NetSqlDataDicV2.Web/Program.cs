using DataDictionary.AspNetCore.Extensions;
using NetSqlDataDicV2.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Data Dictionary services (includes MVC, Data Protection, Core services)
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

// Configure pipeline
// Add request logging first to capture all requests including errors
app.UseMiddleware<RequestLoggingMiddleware>();
// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/tools/datadictionary/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// UseDataDictionary handles static files and auto-migration
app.UseDataDictionary();

app.UseRouting();
app.UseAuthorization();

// Map Data Dictionary routes at /tools/datadictionary
app.MapDataDictionary();

// Redirect root to Data Dictionary
app.MapGet("/", context =>
{
    context.Response.Redirect("/tools/datadictionary");
    return Task.CompletedTask;
});

app.Run();
