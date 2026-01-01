using DataDictionary.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Data Dictionary services (includes MVC, Data Protection, Core services)
builder.Services.AddDataDictionary(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/tools/datadictionary/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// UseDataDictionary handles static files, auto-migration, and optional middleware
app.UseDataDictionary(middleware =>
{
    middleware.UseRequestLogging = true;
    middleware.UseExceptionHandling = true;
    middleware.IncludeStackTraceInErrors = app.Environment.IsDevelopment();
});

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
