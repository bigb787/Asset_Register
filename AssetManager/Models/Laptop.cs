namespace AssetManager.Models;

public class Laptop
{
    public int Id { get; set; }

    public string AssetType { get; set; } = string.Empty;

    public string AssetManufacturer { get; set; } = string.Empty;

    public string Processor { get; set; } = string.Empty;

    public string AssetOwner { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string ServiceTag { get; set; } = string.Empty;

    public string PN { get; set; } = string.Empty;

    public string LastOwner { get; set; } = string.Empty;

    public string Warranty { get; set; } = string.Empty;

    public string AssetHealth { get; set; } = string.Empty;

    public DateTime? InstallDate { get; set; }

    public string OS { get; set; } = string.Empty;

    public string SuptVendor { get; set; } = string.Empty;

    public string Dept { get; set; } = string.Empty;

    public string HardDisk { get; set; } = string.Empty;

    public string RAM { get; set; } = string.Empty;

    public string Keyboard { get; set; } = string.Empty;

    public string Mouse { get; set; } = string.Empty;

    public string HeadPhone { get; set; } = string.Empty;

    public string USBExtender { get; set; } = string.Empty;

    public string ContainsPII { get; set; } = string.Empty;

    /// <summary>When true, the asset is unassigned / available to allocate.</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime DateAddedUpdated { get; set; }
}
