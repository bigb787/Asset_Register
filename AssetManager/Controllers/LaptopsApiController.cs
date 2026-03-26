using AssetManager.Data;
using AssetManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Controllers;

[ApiController]
[Route("api/laptops")]
public class LaptopsApiController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Laptop>>> GetAll(CancellationToken ct) =>
        Ok(await db.Laptops.AsNoTracking().OrderByDescending(x => x.DateAddedUpdated).ToListAsync(ct));

    [HttpGet("free")]
    public async Task<ActionResult<IEnumerable<Laptop>>> GetFree(CancellationToken ct) =>
        Ok(await db.Laptops.AsNoTracking().Where(x => x.IsAvailable).OrderBy(x => x.ServiceTag).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Laptop>> Get(int id, CancellationToken ct)
    {
        var row = await db.Laptops.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<Laptop>> Create([FromBody] Laptop input, CancellationToken ct)
    {
        input.Id = 0;
        input.DateAddedUpdated = DateTime.UtcNow;
        db.Laptops.Add(input);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Laptop>> Update(int id, [FromBody] Laptop input, CancellationToken ct)
    {
        var existing = await db.Laptops.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
            return NotFound();

        existing.AssetType = input.AssetType;
        existing.AssetManufacturer = input.AssetManufacturer;
        existing.Processor = input.Processor;
        existing.AssetOwner = input.AssetOwner;
        existing.Location = input.Location;
        existing.Model = input.Model;
        existing.ServiceTag = input.ServiceTag;
        existing.PN = input.PN;
        existing.LastOwner = input.LastOwner;
        existing.Warranty = input.Warranty;
        existing.AssetHealth = input.AssetHealth;
        existing.InstallDate = input.InstallDate;
        existing.OS = input.OS;
        existing.SuptVendor = input.SuptVendor;
        existing.Dept = input.Dept;
        existing.HardDisk = input.HardDisk;
        existing.RAM = input.RAM;
        existing.Keyboard = input.Keyboard;
        existing.Mouse = input.Mouse;
        existing.HeadPhone = input.HeadPhone;
        existing.USBExtender = input.USBExtender;
        existing.ContainsPII = input.ContainsPII;
        existing.IsAvailable = input.IsAvailable;
        existing.DateAddedUpdated = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var existing = await db.Laptops.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existing is null)
            return NotFound();
        db.Laptops.Remove(existing);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
