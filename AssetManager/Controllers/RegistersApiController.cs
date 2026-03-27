using System.Text.Json;
using System.Reflection;
using AssetManager.Data;
using AssetManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace AssetManager.Controllers;

[ApiController]
[Route("api/registers")]
public class RegistersApiController(ApplicationDbContext db) : ControllerBase
{
    private static readonly Dictionary<string, string[]> HeaderConfig = new(StringComparer.OrdinalIgnoreCase)
    {
        ["laptop"] = ["Asset type","Asset Manufacturer","Service Tag","Model","P/N","Asset Owner","Assigned To","Asset Status","Last Owner","Dept","Location","Asset Health","Warranty","Install date","Date Added/Updated","Processor","RAM","HardDisk","O/S","Supt Vendor","Keyboard","Mouse","HeadPhone","USB Extender","Contains PII (Yes/No)"],
        ["desktop"] = ["Asset type","Asset Manufacturer","Service Tag","Model","P/N","Asset Owner","Assigned To","Asset Status","Last Owner","Dept","Location","Asset Health","Warranty","Install date","Date Added/Updated","Processor","O/S","Supt Vendor","Configuration","Contains PII (Yes/No)"],
        ["monitor"] = ["Asset type","Asset Manufacturer","Service Tag","Model","P/N","Asset Owner","Assigned To","Asset Status","Dept","Location","Asset Health","Warranty","INSTALL DATE","Date Added/Updated","Supt Vendor","Contains PII (Yes/No)"],
        ["networking"] = ["Asset type","User","Model","S/N","Warranty","Supt Vendor","Location","Dept","Asset Owner","Contains PII (Yes/No)","Date Added/Updated"],
        ["cloud"] = ["Asset","Asset Type","Asset Value","Asset Owner","Asset Location","Contains PII data?","Asset Region","Date Added/ Updated"],
        ["infodesk"] = ["Asset","Asset Type","Asset Owner","Asset Location","Contains PII data?","Date Added/ Updated"],
        ["thirdparty"] = ["Asset","Asset Type","Asset Value","Asset Owner","Asset Location","Contains PII data?","Date Added/ Updated"],
        ["ups"] = ["Asset type","Device Id","Model","Warranty","INSTALL DATE","Supt Vendor","Location","Dept","Asset Owner","Date Added/Updated"],
        ["mobile"] = ["Asset type","Model","Warranty","Supt Vendor","Location","Dept","Asset Owner","Contains PII (Yes/No)","Date Added/Updated"],
        ["scanners"] = ["Asset type","Model","S/N","Warranty","Supt Vendor","Location","Dept","Asset Owner","Contains PII (Yes/No)","Date Added/Updated"],
        ["admin"] = ["Asset type","Invoice No","Warranty","INSTALL DATE","Supt Vendor","Location","Dept","Asset Owner","Contains PII (Yes/No)","Date Added/Updated"]
    };

    [HttpGet("{registerKey}")]
    public async Task<ActionResult<IEnumerable<RegisterRecordDto>>> GetAll(string registerKey, CancellationToken ct)
    {
        var category = Category(registerKey);
        var rows = await db.FreeAssets
            .AsNoTracking()
            .Where(x => x.Category == category)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return Ok(rows.Select(MapFromEntity));
    }

    [HttpPost("{registerKey}")]
    public async Task<ActionResult<RegisterRecordDto>> Create(string registerKey, [FromBody] RegisterUpsertDto input, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var entity = new FreeAsset
        {
            Category = Category(registerKey),
            Name = ResolveName(input.Fields),
            SerialOrAssetTag = ResolveSerial(input.Fields),
            Notes = JsonSerializer.Serialize(new RegisterStoredPayload { Fields = input.Fields, IsAvailable = input.IsAvailable }),
            UpdatedAt = now
        };
        db.FreeAssets.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { registerKey }, MapFromEntity(entity));
    }

    [HttpPut("{registerKey}/{id:int}")]
    public async Task<ActionResult<RegisterRecordDto>> Update(string registerKey, int id, [FromBody] RegisterUpsertDto input, CancellationToken ct)
    {
        var category = Category(registerKey);
        var entity = await db.FreeAssets.FirstOrDefaultAsync(x => x.Id == id && x.Category == category, ct);
        if (entity is null) return NotFound();

        entity.Name = ResolveName(input.Fields);
        entity.SerialOrAssetTag = ResolveSerial(input.Fields);
        entity.Notes = JsonSerializer.Serialize(new RegisterStoredPayload { Fields = input.Fields, IsAvailable = input.IsAvailable });
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(MapFromEntity(entity));
    }

    [HttpDelete("{registerKey}/{id:int}")]
    public async Task<IActionResult> Delete(string registerKey, int id, CancellationToken ct)
    {
        var category = Category(registerKey);
        var entity = await db.FreeAssets.FirstOrDefaultAsync(x => x.Id == id && x.Category == category, ct);
        if (entity is null) return NotFound();
        db.FreeAssets.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{registerKey}/export.xlsx")]
    public async Task<IActionResult> ExportExcel(string registerKey, CancellationToken ct)
    {
        var headers = ResolveHeaders(registerKey);
        var category = Category(registerKey);
        var entities = await db.FreeAssets.AsNoTracking()
            .Where(x => x.Category == category)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);
        var rows = entities.Select(MapFromEntity).ToList();

        ConfigureEpplusLicense();
        await using var ms = new MemoryStream();
        using (var package = new ExcelPackage())
        {
            var sheet = package.Workbook.Worksheets.Add("Register");
            for (var c = 0; c < headers.Length; c++) sheet.Cells[1, c + 1].Value = headers[c];
            var r = 2;
            foreach (var row in rows)
            {
                for (var c = 0; c < headers.Length; c++)
                {
                    var h = headers[c];
                    object val;
                    if (IsDateHeader(h))
                        val = row.DateAddedUpdated;
                    else
                        val = row.Fields.TryGetValue(h, out var v) ? v : "";
                    sheet.Cells[r, c + 1].Value = val;
                }
                r++;
            }
            await package.SaveAsAsync(ms, ct);
        }
        var fileName = $"{registerKey}-register-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static string Category(string key) => $"register:{key.Trim().ToLowerInvariant()}";
    private static string[] ResolveHeaders(string registerKey) =>
        HeaderConfig.TryGetValue(registerKey.Trim().ToLowerInvariant(), out var h)
            ? h
            : ["Asset", "Date Added/Updated"];
    private static bool IsDateHeader(string h) => h.Contains("Date Added", StringComparison.OrdinalIgnoreCase);

    private static void ConfigureEpplusLicense()
    {
        var licenseProp = typeof(ExcelPackage).GetProperty("License", BindingFlags.Public | BindingFlags.Static);
        var licenseObj = licenseProp?.GetValue(null);
        var setNonCommercial = licenseObj?.GetType().GetMethod("SetNonCommercialPersonal", [typeof(string)])
                               ?? licenseObj?.GetType().GetMethod("SetNonCommercialOrganization", [typeof(string)]);
        if (setNonCommercial is not null)
        {
            setNonCommercial.Invoke(licenseObj, ["Asset Manager"]);
            return;
        }
#pragma warning disable CS0618
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
#pragma warning restore CS0618
    }

    private static string ResolveName(Dictionary<string, string> fields)
    {
        if (fields.TryGetValue("Asset", out var asset) && !string.IsNullOrWhiteSpace(asset)) return asset;
        if (fields.TryGetValue("Asset type", out var type) && !string.IsNullOrWhiteSpace(type)) return type;
        if (fields.TryGetValue("Model", out var model) && !string.IsNullOrWhiteSpace(model)) return model;
        return "Item";
    }

    private static string? ResolveSerial(Dictionary<string, string> fields)
    {
        if (fields.TryGetValue("Service Tag", out var serviceTag) && !string.IsNullOrWhiteSpace(serviceTag)) return serviceTag;
        if (fields.TryGetValue("S/N", out var sn) && !string.IsNullOrWhiteSpace(sn)) return sn;
        if (fields.TryGetValue("Device Id", out var deviceId) && !string.IsNullOrWhiteSpace(deviceId)) return deviceId;
        return null;
    }

    private static RegisterRecordDto MapFromEntity(FreeAsset entity)
    {
        var payload = ParsePayload(entity.Notes);
        var fields = payload?.Fields ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        fields["Date Added/Updated"] = entity.UpdatedAt.ToString("u");

        return new RegisterRecordDto
        {
            Id = entity.Id,
            Fields = fields,
            IsAvailable = payload?.IsAvailable ?? true,
            DateAddedUpdated = entity.UpdatedAt
        };
    }

    private static RegisterStoredPayload? ParsePayload(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        try
        {
            return JsonSerializer.Deserialize<RegisterStoredPayload>(notes);
        }
        catch
        {
            return null;
        }
    }
}

public class RegisterUpsertDto
{
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsAvailable { get; set; } = true;
}

public class RegisterRecordDto
{
    public int Id { get; set; }
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsAvailable { get; set; }
    public DateTime DateAddedUpdated { get; set; }
}

public class RegisterStoredPayload
{
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsAvailable { get; set; } = true;
}
