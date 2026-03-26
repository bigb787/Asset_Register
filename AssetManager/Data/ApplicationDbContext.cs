using AssetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Laptop> Laptops => Set<Laptop>();

    public DbSet<FreeAsset> FreeAssets => Set<FreeAsset>();
}
