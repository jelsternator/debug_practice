using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SupportEngineerChallenge.Tests;

public class TaskApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TaskApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    // --- Existing tests ---

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenValid()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "user-001", title = "Test task" };
        client.DefaultRequestHeaders.Add("X-Client-Timestamp", DateTime.UtcNow.ToString("O"));

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ListTasks_ShouldReturnOnlyRequestedUser()
    {
        var client = _factory.CreateClient();

        var user1 = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?userId=user-001&limit=50");

        user1.Should().NotBeNull();
        user1!.Should().OnlyContain(t => t.UserId == "user-001");
    }

    // --- New tests covering the bugs ---

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenTimestampHeaderIsMissing()
    {
        // Regression test: previously threw FormatException -> 500
        var client = _factory.CreateClient();
        var req = new { userId = "user-002", title = "No timestamp task" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenTimestampHeaderIsInvalid()
    {
        // Regression test: invalid timestamp should fall back to server UTC, not crash
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Timestamp", "not-a-date");
        var req = new { userId = "user-003", title = "Bad timestamp task" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn400_WhenUserIdIsMissing()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "", title = "Some task" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn400_WhenTitleIsMissing()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "user-004", title = "" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTasks_ShouldReturnTasksOrderedNewestFirst()
    {
        var client = _factory.CreateClient();

        // Create two tasks with known timestamps
        var older = DateTime.UtcNow.AddMinutes(-10).ToString("O");
        var newer = DateTime.UtcNow.ToString("O");

        var c1 = _factory.CreateClient();
        c1.DefaultRequestHeaders.Add("X-Client-Timestamp", older);
        await c1.PostAsJsonAsync("/api/tasks", new { userId = "user-order-test", title = "Older task" });

        var c2 = _factory.CreateClient();
        c2.DefaultRequestHeaders.Add("X-Client-Timestamp", newer);
        await c2.PostAsJsonAsync("/api/tasks", new { userId = "user-order-test", title = "Newer task" });

        var tasks = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?userId=user-order-test&limit=10");

        tasks.Should().NotBeNull();
        tasks!.Should().HaveCountGreaterOrEqualTo(2);
        tasks.First().CreatedAt.Should().BeOnOrAfter(tasks.Last().CreatedAt);
    }

    [Fact]
    public async Task ListTasks_ShouldNotReturnOtherUserstasks()
    {
        var client = _factory.CreateClient();

        // Create a task for a unique user
        await client.PostAsJsonAsync("/api/tasks", new { userId = "user-isolated", title = "Private task" });

        // Fetch a different user's tasks
        var tasks = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?userId=user-other&limit=50");

        tasks.Should().NotBeNull();
        tasks!.Should().NotContain(t => t.UserId == "user-isolated");
    }

    public record TaskDto(int Id, string UserId, string Title, string Status, DateTime CreatedAt, DateTime UpdatedAt);
}