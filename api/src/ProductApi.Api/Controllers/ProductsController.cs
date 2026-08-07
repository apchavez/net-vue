using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Api.Dtos;
using ProductApi.Api.Reports;
using ProductApi.Application;
using ProductApi.Domain;
using ProductApi.Domain.Exceptions;

namespace ProductApi.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] ProductRequestDto dto, CancellationToken ct)
    {
        var product = new Product(dto.Sku, dto.Name, dto.Description, dto.Category,
            dto.Price!.Value, dto.Stock!.Value, dto.Active!.Value);
        var created = await productService.CreateProductAsync(product, ct);
        return CreatedAtAction(nameof(FindById), new { id = created.Id }, ProductResponseDto.From(created));
    }

    [HttpGet("active")]
    [Authorize(Roles = "ADMIN,USER")]
    public async Task<ActionResult<PageResponseDto<ProductResponseDto>>> ListActive(
        [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var (items, total) = await productService.ListActiveProductsAsync(page, size, ct);
        return Ok(PageResponseDto<ProductResponseDto>.Of(
            items.Select(ProductResponseDto.From).ToList(), page, size, total));
    }

    [HttpGet("inactive")]
    [Authorize(Roles = "ADMIN,USER")]
    public async Task<ActionResult<PageResponseDto<ProductResponseDto>>> ListInactive(
        [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var (items, total) = await productService.ListInactiveProductsAsync(page, size, ct);
        return Ok(PageResponseDto<ProductResponseDto>.Of(
            items.Select(ProductResponseDto.From).ToList(), page, size, total));
    }

    [HttpGet("search")]
    [Authorize(Roles = "ADMIN,USER")]
    public async Task<ActionResult<PageResponseDto<ProductResponseDto>>> Search(
        [FromQuery] string prefix, [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken ct = default)
    {
        var (items, total) = await productService.SearchByNamePrefixAsync(prefix, page, size, ct);
        return Ok(PageResponseDto<ProductResponseDto>.Of(
            items.Select(ProductResponseDto.From).ToList(), page, size, total));
    }

    [HttpGet("sku/{sku}")]
    [Authorize(Roles = "ADMIN,USER")]
    public async Task<IActionResult> FindBySku(string sku, CancellationToken ct)
    {
        var product = await productService.FindBySkuAsync(sku, ct);
        return product is null ? NotFound() : Ok(ProductResponseDto.From(product));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "ADMIN,USER")]
    public async Task<IActionResult> FindById(int id, CancellationToken ct)
    {
        var product = await productService.FindByIdAsync(id, ct);
        return Ok(ProductResponseDto.From(product));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateRequestDto dto, CancellationToken ct)
    {
        var updatedData = new Product(dto.Sku, dto.Name, dto.Description, dto.Category,
            dto.Price!.Value, dto.Stock!.Value, dto.Active!.Value) { Id = id };
        var updated = await productService.UpdateProductAsync(id, updatedData, ct);
        return Ok(ProductResponseDto.From(updated));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await productService.DeleteProductAsync(id, ct);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Roles = "ADMIN")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Ok(new ImportResultDto(0, 0, []));

        var imported = 0;
        var errors = new List<ImportRowErrorDto>();

        using var reader = new StreamReader(file.OpenReadStream());
        await reader.ReadLineAsync(ct); // header row, discarded
        var rowNumber = 1;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var product = ProductCsvParser.ParseRow(line);
                await productService.CreateProductAsync(product, ct);
                imported++;
            }
            catch (ProductDomainException ex)
            {
                errors.Add(new ImportRowErrorDto(rowNumber, ex.Message));
            }
        }

        return Ok(new ImportResultDto(imported, errors.Count, errors));
    }

    [HttpGet("report/pdf")]
    [Authorize(Roles = "ADMIN,USER")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ReportPdf(CancellationToken ct)
    {
        var products = await productService.GetAllProductsAsync(ct);
        var bytes = ProductReportGenerator.GeneratePdf(products);
        return File(bytes, "application/pdf", "products-report.pdf");
    }

    [HttpGet("report/excel")]
    [Authorize(Roles = "ADMIN,USER")]
    public async Task<IActionResult> ReportExcel(CancellationToken ct)
    {
        var products = await productService.GetAllProductsAsync(ct);
        var bytes = ProductReportGenerator.GenerateExcel(products);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "products-report.xlsx");
    }
}
