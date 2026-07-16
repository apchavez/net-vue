using System.ComponentModel.DataAnnotations;

namespace ProductApi.Api.Dtos;

public sealed record ProductRequestDto(
    [Required, StringLength(64, MinimumLength = 1)] string Sku,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(1000)] string? Description,
    [StringLength(100)] string? Category,
    [Required, Range(0, double.MaxValue)] decimal? Price,
    [Required, Range(0, int.MaxValue)] int? Stock,
    [Required] bool? Active);

public sealed record ProductUpdateRequestDto(
    [Required, StringLength(64, MinimumLength = 1)] string Sku,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    [StringLength(1000)] string? Description,
    [StringLength(100)] string? Category,
    [Required, Range(0, double.MaxValue)] decimal? Price,
    [Required, Range(0, int.MaxValue)] int? Stock,
    [Required] bool? Active);

public sealed record ProductResponseDto(
    int Id, string Sku, string Name, string? Description, string? Category,
    decimal Price, int Stock, bool Active)
{
    public static ProductResponseDto From(Domain.Product p) =>
        new(p.Id, p.Sku, p.Name, p.Description, p.Category, p.Price, p.Stock, p.Active);
}

public sealed record ImportRowErrorDto(int Row, string Message);

public sealed record ImportResultDto(int Imported, int Failed, IReadOnlyList<ImportRowErrorDto> Errors);

public sealed record PageResponseDto<T>(IReadOnlyList<T> Content, int Page, int Size, long TotalElements, int TotalPages, bool Last)
{
    public static PageResponseDto<T> Of(IReadOnlyList<T> content, int page, int size, long totalElements)
    {
        var totalPages = size == 0 ? 1 : (int)Math.Ceiling((double)totalElements / size);
        var last = page >= totalPages - 1;
        return new PageResponseDto<T>(content, page, size, totalElements, totalPages, last);
    }
}
