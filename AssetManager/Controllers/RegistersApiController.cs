using System.Text.Json;
using AssetManager.Data;
using AssetManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Controllers;

[ApiController]
[Route("api/registers")]
public class RegistersApiController(ApplicationDbContext db) : ControllerBase
{
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

    private static string Category(string key) => $"register:{key.Trim().ToLowerInvariant()}";

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
