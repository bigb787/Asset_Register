using AssetManager.Data;
using AssetManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Controllers;

[ApiController]
[Route("api/free-assets")]
public class FreeAssetsApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FreeAsset>>> GetAll(CancellationToken ct) =>
        Ok(await db.FreeAssets.AsNoTracking().OrderByDescending(x => x.UpdatedAt).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FreeAsset>> Get(int id, CancellationToken ct)
    {
        var row = await db.FreeAssets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<FreeAsset>> Create([FromBody] FreeAsset input, CancellationToken ct)
    {
        input.Id = 0;
        input.UpdatedAt = DateTime.UtcNow;
        db.FreeAssets.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FreeAsset>> Update(int id, [FromBody] FreeAsset input, CancellationToken ct)
    {
        var existing = await db.FreeAssets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
            return NotFound();

        existing.Name = input.Name;
        existing.Category = input.Category;
        existing.SerialOrAssetTag = input.SerialOrAssetTag;
        existing.Notes = input.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await db.FreeAssets.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
            return NotFound();
        db.FreeAssets.Remove(existing);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
