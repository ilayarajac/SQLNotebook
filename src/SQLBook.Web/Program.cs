using SQLBook.Web.Data;
using SQLBook.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// App DB (SQLite)
var appDbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "sqlbook", "sqlbook.db");
Directory.CreateDirectory(Path.GetDirectoryName(appDbPath)!);
builder.Services.AddSingleton(new AppDb($"Data Source={appDbPath}"));

// Services
builder.Services.AddScoped<NotebookService>();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<SchemaService>();

var app = builder.Build();

// Initialise app DB schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    await db.InitialiseAsync();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Library}/{action=Index}/{id?}");

app.Run();
