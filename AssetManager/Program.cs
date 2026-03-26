using Amazon.S3;
using AssetManager.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseProvider = builder.Configuration["Database:Provider"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (DbProviderResolver.Resolve(databaseProvider, connectionString))
    {
        case DbProviderKind.Npgsql:
            options.UseNpgsql(connectionString);
            break;
        case DbProviderKind.SqlServer:
            options.UseSqlServer(connectionString);
            break;
        default:
            options.UseSqlite(connectionString ?? "Data Source=assetmanager.db");
            break;
    }
});

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var kind = DbProviderResolver.Resolve(databaseProvider, connectionString);
    if (app.Environment.IsDevelopment() && db.Database.IsSqlite())
    {
        await db.Database.EnsureCreatedAsync();
    }
    else if (kind is DbProviderKind.Npgsql or DbProviderKind.SqlServer)
    {
        await db.Database.EnsureCreatedAsync();
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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

app.MapControllers();

app.Run();
