using System.Net;
using System.Net.Http.Json;
using FinTrack.Application.Features.Categories.Dtos;
using FluentAssertions;

namespace FinTrack.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CategoriesEndpointsTests
{
    private readonly ApiFactory _factory;

    public CategoriesEndpointsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task NewUser_HasNineDefaultCategories()
    {
        var client = await _factory.RegisterClientAsync();

        var categories = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories", ApiTestExtensions.Json);

        categories!.Should().HaveCount(9);
        categories.Should().OnlyContain(c => c.IsDefault);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateNameAndType_ReturnsConflict()
    {
        var client = await _factory.RegisterClientAsync();
        var payload = new { name = "Gym", type = "Expense" };

        var first = await client.PostAsJsonAsync("/api/categories", payload);
        var second = await client.PostAsJsonAsync("/api/categories", payload);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCategory_ArchivesAndExcludesFromDefaultList()
    {
        var client = await _factory.RegisterClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/categories", new { name = "Gym", type = "Expense" }))
            .Content.ReadFromJsonAsync<CategoryDto>(ApiTestExtensions.Json);

        var delete = await client.DeleteAsync($"/api/categories/{created!.Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var active = await client.GetFromJsonAsync<List<CategoryDto>>("/api/categories", ApiTestExtensions.Json);
        active!.Should().NotContain(c => c.Id == created.Id);

        var withArchived = await client.GetFromJsonAsync<List<CategoryDto>>(
            "/api/categories?includeArchived=true", ApiTestExtensions.Json);
        withArchived!.Should().Contain(c => c.Id == created.Id && c.IsArchived);
    }

    [Fact]
    public async Task UpdateCategory_WhenOwnedByAnotherUser_ReturnsNotFound()
    {
        var clientA = await _factory.RegisterClientAsync();
        var created = await (await clientA.PostAsJsonAsync("/api/categories", new { name = "Gym", type = "Expense" }))
            .Content.ReadFromJsonAsync<CategoryDto>(ApiTestExtensions.Json);

        var clientB = await _factory.RegisterClientAsync();
        var response = await clientB.PutAsJsonAsync($"/api/categories/{created!.Id}", new { name = "Hacked" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
