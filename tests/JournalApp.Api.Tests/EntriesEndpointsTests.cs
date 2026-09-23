using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JournalApp.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace JournalApp.Api.Tests;

[Collection(ApiCollection.Name)]
public class EntriesEndpointsTests(JournalApiFactory factory)
{
    [Theory]
    [InlineData("GET", "/api/entries")]
    [InlineData("POST", "/api/entries")]
    [InlineData("GET", "/api/entries/6f1c1a52-2f3e-4f7c-9c55-0d0c5b8c3c11")]
    [InlineData("PUT", "/api/entries/6f1c1a52-2f3e-4f7c-9c55-0d0c5b8c3c11")]
    [InlineData("DELETE", "/api/entries/6f1c1a52-2f3e-4f7c-9c55-0d0c5b8c3c11")]
    public async Task AnyEntriesEndpoint_WithoutToken_Returns401(string method, string url)
    {
        // Arrange
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { title = "Title", content = "Content" });
        }

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationAndServerTimestamps()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var before = DateTime.UtcNow.AddSeconds(-5);

        // Act
        var response = await client.PostAsJsonAsync("/api/entries",
            new { title = "  My day ", content = "It was great.", mood = "Great", createdAt = "2000-01-01T00:00:00Z" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var entry = await response.Content.ReadFromJsonAsync<EntryResponse>(ApiClientExtensions.Json);
        response.Headers.Location!.AbsolutePath.Should().EndWithEquivalentOf($"/api/entries/{entry!.Id}");
        entry.Title.Should().Be("My day");
        entry.Mood.Should().Be("Great");
        entry.CreatedAt.Should().BeAfter(before);
        entry.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Create_EmptyTitle_Returns400ValidationProblem()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/entries", new { title = "", content = "Content" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("title");
    }

    [Fact]
    public async Task Create_ContentTooLong_Returns400()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/entries", new { title = "Title", content = new string('a', 5001) });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_InvalidMood_Returns400()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/entries", new { title = "Title", content = "Content", mood = "Ecstatic" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_SeveralEntries_ReturnsOnlyOwnEntriesNewestFirst()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var (otherClient, _) = await factory.CreateAuthenticatedClientAsync();
        await client.CreateEntryAsync("First");
        await client.CreateEntryAsync("Second");
        await otherClient.CreateEntryAsync("Other user's entry");
        await client.CreateEntryAsync("Third");

        // Act
        var entries = await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);

        // Assert
        entries!.Select(e => e.Title).Should().Equal("Third", "Second", "First");
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/entries/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task EntryOfAnotherUser_GetUpdateDelete_Returns404AndLeavesEntryUntouched()
    {
        // Arrange
        var (owner, _) = await factory.CreateAuthenticatedClientAsync();
        var (intruder, _) = await factory.CreateAuthenticatedClientAsync();
        var entry = await owner.CreateEntryAsync("Private", "Secret thoughts");

        // Act
        var get = await intruder.GetAsync($"/api/entries/{entry.Id}");
        var put = await intruder.PutAsJsonAsync($"/api/entries/{entry.Id}", new { title = "Hacked", content = "Hacked" });
        var delete = await intruder.DeleteAsync($"/api/entries/{entry.Id}");

        // Assert
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        put.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stillThere = await owner.GetFromJsonAsync<EntryResponse>($"/api/entries/{entry.Id}", ApiClientExtensions.Json);
        stillThere!.Title.Should().Be("Private");
        stillThere.Content.Should().Be("Secret thoughts");
    }

    [Fact]
    public async Task Update_InvalidRequest_Returns400()
    {
        // Arrange
        var (client, _) = await factory.CreateAuthenticatedClientAsync();
        var entry = await client.CreateEntryAsync("Title");

        // Act
        var response = await client.PutAsJsonAsync($"/api/entries/{entry.Id}", new { title = new string('a', 101), content = "Content" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullFlow_RegisterLoginCreateReadUpdateDelete_Works()
    {
        // Arrange: register + login
        var (client, _) = await factory.CreateAuthenticatedClientAsync();

        // Create
        var created = await client.CreateEntryAsync("Original", "Original content", "Neutral");

        // Read
        var read = await client.GetFromJsonAsync<EntryResponse>($"/api/entries/{created.Id}", ApiClientExtensions.Json);
        read.Should().BeEquivalentTo(created);

        // Update
        var updateResponse = await client.PutAsJsonAsync($"/api/entries/{created.Id}",
            new { title = "Updated", content = "Updated content", mood = (string?)null });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EntryResponse>(ApiClientExtensions.Json);
        updated!.Title.Should().Be("Updated");
        updated.Mood.Should().BeNull();
        updated.CreatedAt.Should().Be(created.CreatedAt);
        updated.UpdatedAt.Should().NotBeNull();

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/entries/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Gone
        var afterDelete = await client.GetAsync($"/api/entries/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var list = await client.GetFromJsonAsync<List<EntryResponse>>("/api/entries", ApiClientExtensions.Json);
        list.Should().BeEmpty();
    }
}
