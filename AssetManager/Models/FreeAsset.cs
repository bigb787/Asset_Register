namespace AssetManager.Models;

/// <summary>
/// Spare / unassigned items tracked separately from assigned laptops (e.g. stock pool).
/// </summary>
public class FreeAsset
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? SerialOrAssetTag { get; set; }

    public string? Notes { get; set; }

    public DateTime UpdatedAt { get; set; }
}
