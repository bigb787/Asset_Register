using Amazon.S3;
using Amazon.S3.Model;
using AssetManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Reflection;

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
        ConfigureEpplusLicense();

        var rows = await db.Laptops.AsNoTracking().OrderBy(x => x.ServiceTag).ToListAsync(ct);

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Laptops");
                sheet.Cells[1, 1].Value = "Asset Type";
                sheet.Cells[1, 2].Value = "Asset Manufacturer";
                sheet.Cells[1, 3].Value = "Service Tag";
                sheet.Cells[1, 4].Value = "Model";
                sheet.Cells[1, 5].Value = "P/N";
                sheet.Cells[1, 6].Value = "Asset Owner";
                sheet.Cells[1, 7].Value = "Assigned To";
                sheet.Cells[1, 8].Value = "Asset Status";
                sheet.Cells[1, 9].Value = "Last Owner";
                sheet.Cells[1, 10].Value = "Dept";
                sheet.Cells[1, 11].Value = "Location";
                sheet.Cells[1, 12].Value = "Asset Health";
                sheet.Cells[1, 13].Value = "Warranty";
                sheet.Cells[1, 14].Value = "Install date";
                sheet.Cells[1, 15].Value = "Date Added/Updated";
                sheet.Cells[1, 16].Value = "Processor";
                sheet.Cells[1, 17].Value = "RAM";
                sheet.Cells[1, 18].Value = "HardDisk";
                sheet.Cells[1, 19].Value = "O/S";
                sheet.Cells[1, 20].Value = "Supt Vendor";
                sheet.Cells[1, 21].Value = "Keyboard";
                sheet.Cells[1, 22].Value = "Mouse";
                sheet.Cells[1, 23].Value = "HeadPhone";
                sheet.Cells[1, 24].Value = "USB Extender";
                sheet.Cells[1, 25].Value = "Contains PII (Yes/No)";

                var r = 2;
                foreach (var x in rows)
                {
                    sheet.Cells[r, 1].Value = x.AssetType;
                    sheet.Cells[r, 2].Value = x.AssetManufacturer;
                    sheet.Cells[r, 3].Value = x.ServiceTag;
                    sheet.Cells[r, 4].Value = x.Model;
                    sheet.Cells[r, 5].Value = x.PN;
                    sheet.Cells[r, 6].Value = x.AssetOwner;
                    sheet.Cells[r, 7].Value = x.AssetOwner; // Assigned To uses current schema field.
                    sheet.Cells[r, 8].Value = x.IsAvailable ? "Free" : "In Use";
                    sheet.Cells[r, 9].Value = x.LastOwner;
                    sheet.Cells[r, 10].Value = x.Dept;
                    sheet.Cells[r, 11].Value = x.Location;
                    sheet.Cells[r, 12].Value = x.AssetHealth;
                    sheet.Cells[r, 13].Value = x.Warranty;
                    sheet.Cells[r, 14].Value = x.InstallDate;
                    sheet.Cells[r, 15].Value = x.DateAddedUpdated;
                    sheet.Cells[r, 16].Value = x.Processor;
                    sheet.Cells[r, 17].Value = x.RAM;
                    sheet.Cells[r, 18].Value = x.HardDisk;
                    sheet.Cells[r, 19].Value = x.OS;
                    sheet.Cells[r, 20].Value = x.SuptVendor;
                    sheet.Cells[r, 21].Value = x.Keyboard;
                    sheet.Cells[r, 22].Value = x.Mouse;
                    sheet.Cells[r, 23].Value = x.HeadPhone;
                    sheet.Cells[r, 24].Value = x.USBExtender;
                    sheet.Cells[r, 25].Value = x.ContainsPII;
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
}
