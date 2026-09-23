using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JournalApp.Api.Tests.Fixtures;

namespace JournalApp.Api.Tests;

/// <summary>
/// Reads are served from the cache after the first request; these tests prove writes
/// invalidate it so clients never see stale data.
/// </summary>
[Collection(ApiCollection.Name)]
public class CachingEndpointsTests(JournalApiFactory factory)
{
    [Fact]
    public async Task List_AfterCreate_IncludesNewEntry()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        await client.CreateEntryAsync("First");
        (await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json))!
            .Should().HaveCount(1);

        // Act
        await client.CreateEntryAsync("Second");
        var entries = await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);

        // Assert
        entries!.Select(e => e.Title).Should().Equal("Second", "First");
    }

    [Fact]
    public async Task ListAndGet_AfterUpdate_ReturnUpdatedEntry()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var entry = await client.CreateEntryAsync("Before");
        await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);
        await client.GetFromJsonAsync<EntryResponse>($"/api/entries/{entry.Id}", ApiClientExtensions.Json);

        // Act
        (await client.PutAsJsonAsync($"/api/entries/{entry.Id}", new { title = "After", content = "Changed" }))
            .EnsureSuccessStatusCode();
        var list = await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);
        var single = await client.GetFromJsonAsync<EntryResponse>($"/api/entries/{entry.Id}", ApiClientExtensions.Json);

        // Assert
        list!.Single().Title.Should().Be("After");
        single!.Title.Should().Be("After");
        single.Content.Should().Be("Changed");
    }

    [Fact]
    public async Task ListAndGet_AfterDelete_NoLongerReturnEntry()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var entry = await client.CreateEntryAsync("To delete");
        await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);
        await client.GetFromJsonAsync<EntryResponse>($"/api/entries/{entry.Id}", ApiClientExtensions.Json);

        // Act
        (await client.DeleteAsync($"/api/entries/{entry.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var list = await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);
        var get = await client.GetAsync($"/api/entries/{entry.Id}");

        // Assert
        list.Should().BeEmpty();
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_EntryCachedByOwner_StillReturns404ForAnotherUser()
    {
        // Arrange
        var (owner, _) = await factory.CreateAuthenticatedClientAsync();
        var (intruder, _) = await factory.CreateAuthenticatedClientAsync();
        var entry = await owner.CreateEntryAsync("Private");
        (await owner.GetAsync($"/api/entries/{entry.Id}")).EnsureSuccessStatusCode();

        // Act
        var response = await intruder.GetAsync($"/api/entries/{entry.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
