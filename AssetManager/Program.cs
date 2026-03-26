using Amazon.S3;
using AssetManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var redirectToHttps = builder.Configuration.GetValue("ReverseProxy:RedirectHttpToHttps", false);

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
if (redirectToHttps)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
