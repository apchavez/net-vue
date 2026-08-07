using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using ProductApi.Api.Dtos;
using Xunit;

namespace ProductApi.IntegrationTests;

public class ProductsControllerIntegrationTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    private async Task<HttpClient> AuthenticatedClientAsync(string username = "admin", string password = "admin123")
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(username, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_token()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("admin", "admin123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_invalid_credentials_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto("admin", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_without_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto(Guid.NewGuid().ToString("N"), "Widget", null, null, 9.99m, 10, true));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_get_by_id_round_trips()
    {
        var client = await AuthenticatedClientAsync();
        var sku = $"SKU-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto(sku, "Integration Widget", "desc", "cat", 19.99m, 5, true));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        var getResponse = await client.GetAsync($"/api/v1/products/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal(sku, fetched!.Sku);
    }

    [Fact]
    public async Task Create_with_duplicate_sku_returns_409()
    {
        var client = await AuthenticatedClientAsync();
        var sku = $"SKU-{Guid.NewGuid():N}";
        var request = new ProductRequestDto(sku, "Widget", null, null, 9.99m, 10, true);

        await client.PostAsJsonAsync("/api/v1/products", request);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_returns_404_when_missing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FindBySku_returns_404_when_missing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/products/sku/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_by_non_admin_returns_403()
    {
        var client = await AuthenticatedClientAsync("user", "user123");

        var response = await client.DeleteAsync("/api/v1/products/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Full_lifecycle_create_update_delete()
    {
        var client = await AuthenticatedClientAsync();
        var sku = $"SKU-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto(sku, "Widget", null, null, 9.99m, 10, true));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/products/{created!.Id}",
            new ProductUpdateRequestDto(sku, "Updated Widget", null, null, 29.99m, 20, false));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal("Updated Widget", updated!.Name);

        var deleteResponse = await client.DeleteAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/v1/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Get_inactive_products_returns_only_deactivated_items()
    {
        var client = await AuthenticatedClientAsync();
        var sku = $"SKU-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto(sku, "Deactivated Widget", null, null, 9.99m, 10, true));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/products/{created!.Id}",
            new ProductUpdateRequestDto(sku, "Deactivated Widget", null, null, 9.99m, 10, false));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var inactiveResponse = await client.GetAsync("/api/v1/products/inactive?page=0&size=100");
        Assert.Equal(HttpStatusCode.OK, inactiveResponse.StatusCode);
        var inactivePage = await inactiveResponse.Content.ReadFromJsonAsync<PageResponseDto<ProductResponseDto>>();
        Assert.Contains(inactivePage!.Content, p => p.Id == created.Id);

        var activeResponse = await client.GetAsync("/api/v1/products/active?page=0&size=100");
        var activePage = await activeResponse.Content.ReadFromJsonAsync<PageResponseDto<ProductResponseDto>>();
        Assert.DoesNotContain(activePage!.Content, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Inactive_products_without_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products/inactive");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_by_prefix_finds_matching_products()
    {
        var client = await AuthenticatedClientAsync();
        var prefix = $"Findable-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto($"SKU-{Guid.NewGuid():N}", $"{prefix} Product", null, null, 1m, 1, true));

        var response = await client.GetAsync($"/api/v1/products/search?prefix={prefix}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PageResponseDto<ProductResponseDto>>();
        Assert.Equal(1, page!.TotalElements);
    }

    private static HttpContent CsvFile(string csv, string fileName = "import.csv")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Import_without_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/v1/products/import", CsvFile("sku,name,description,category,price,stock,active\n"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_by_non_admin_returns_403()
    {
        var client = await AuthenticatedClientAsync("user", "user123");

        var response = await client.PostAsync("/api/v1/products/import", CsvFile("sku,name,description,category,price,stock,active\n"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Import_valid_csv_creates_all_products()
    {
        var client = await AuthenticatedClientAsync();
        var skuA = $"SKU-{Guid.NewGuid():N}";
        var skuB = $"SKU-{Guid.NewGuid():N}";
        var csv = "sku,name,description,category,price,stock,active\n" +
                  $"{skuA},Widget A,desc,Tools,9.99,10,true\n" +
                  $"{skuB},Widget B,desc,Tools,19.99,5,true\n";

        var response = await client.PostAsync("/api/v1/products/import", CsvFile(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>();
        Assert.Equal(2, result!.Imported);
        Assert.Equal(0, result.Failed);

        var lookup = await client.GetAsync($"/api/v1/products/sku/{skuA}");
        Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
    }

    [Fact]
    public async Task Import_with_invalid_row_reports_error_but_imports_valid_rows()
    {
        var client = await AuthenticatedClientAsync();
        var validSku = $"SKU-{Guid.NewGuid():N}";
        var csv = "sku,name,description,category,price,stock,active\n" +
                  $"{validSku},Good Widget,desc,Tools,9.99,10,true\n" +
                  ",Bad Widget,desc,Tools,9.99,10,true\n";

        var response = await client.PostAsync("/api/v1/products/import", CsvFile(csv));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>();
        Assert.Equal(1, result!.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
        Assert.Equal(3, result.Errors[0].Row);
    }

    [Fact]
    public async Task ReportPdf_without_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/products/report/pdf");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReportPdf_returns_a_pdf_document()
    {
        var client = await AuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto($"SKU-{Guid.NewGuid():N}", "Report Widget", null, null, 9.99m, 10, true));

        var response = await client.GetAsync("/api/v1/products/report/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task ReportExcel_returns_a_workbook_containing_created_products()
    {
        var client = await AuthenticatedClientAsync();
        var sku = $"SKU-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/v1/products",
            new ProductRequestDto(sku, "Excel Widget", null, null, 42.50m, 3, true));

        var response = await client.GetAsync("/api/v1/products/report/excel");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var skus = sheet.RowsUsed().Skip(1).Select(r => r.Cell(1).GetString());
        Assert.Contains(sku, skus);
    }
}
