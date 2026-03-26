using Amazon.S3;
using Amazon.S3.Model;
using AssetManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace AssetManager.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsApiController(
    ApplicationDbContext db,
    IAmazonS3 s3,
    IConfiguration configuration,
    ILogger<ReportsApiController> logger) : ControllerBase
{
    [HttpGet("export/laptops.xlsx")]
    public async Task<IActionResult> ExportLaptopsExcel(CancellationToken ct)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var rows = await db.Laptops.AsNoTracking().OrderBy(x => x.ServiceTag).ToListAsync(ct);

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Laptops");
                sheet.Cells[1, 1].Value = nameof(Models.Laptop.ServiceTag);
                sheet.Cells[1, 2].Value = nameof(Models.Laptop.AssetType);
                sheet.Cells[1, 3].Value = nameof(Models.Laptop.Model);
                sheet.Cells[1, 4].Value = nameof(Models.Laptop.AssetOwner);
                sheet.Cells[1, 5].Value = nameof(Models.Laptop.Location);
                sheet.Cells[1, 6].Value = nameof(Models.Laptop.IsAvailable);
                sheet.Cells[1, 7].Value = nameof(Models.Laptop.DateAddedUpdated);

                var r = 2;
                foreach (var x in rows)
                {
                    sheet.Cells[r, 1].Value = x.ServiceTag;
                    sheet.Cells[r, 2].Value = x.AssetType;
                    sheet.Cells[r, 3].Value = x.Model;
                    sheet.Cells[r, 4].Value = x.AssetOwner;
                    sheet.Cells[r, 5].Value = x.Location;
                    sheet.Cells[r, 6].Value = x.IsAvailable;
                    sheet.Cells[r, 7].Value = x.DateAddedUpdated;
                    r++;
                }

                await package.SaveAsAsync(ms, ct);
            }

            bytes = ms.ToArray();
        }

        var bucket = configuration["Reports:BucketName"];
        var fileName = $"laptops-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        const string contentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        if (string.IsNullOrEmpty(bucket))
            return File(bytes, contentType, fileName);

        var key = $"reports/{fileName}";
        try
        {
            await using var upload = new MemoryStream(bytes);
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = upload,
                ContentType = contentType,
            }, ct);

            var url = s3.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = bucket,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(1),
            });

            return Ok(new { bucket, key, downloadUrl = url, expiresUtc = DateTime.UtcNow.AddHours(1) });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "S3 upload failed; returning file inline.");
            return File(bytes, contentType, fileName);
        }
    }
}
