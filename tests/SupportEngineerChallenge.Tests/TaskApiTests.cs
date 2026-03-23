using Xunit;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SupportEngineerChallenge.Tests;

public class TaskApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TaskApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenValid()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "user-001", title = "Test task" };
        client.DefaultRequestHeaders.Add("X-Client-Timestamp", DateTime.UtcNow.ToString("O"));

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task ListTasks_ShouldReturnOnlyRequestedUser()
    {
        var client = _factory.CreateClient();

        var user1 = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?userId=user-001&limit=50");

        Assert.NotNull(user1);
        Assert.All(user1, t => Assert.Equal("user-001", t.UserId));
    }

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenTimestampHeaderIsMissing()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "user-002", title = "No timestamp task" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn201_WhenTimestampHeaderIsInvalid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Timestamp", "not-a-date");
        var req = new { userId = "user-003", title = "Bad timestamp task" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn400_WhenUserIdIsMissing()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "", title = "Some task" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ShouldReturn400_WhenTitleIsMissing()
    {
        var client = _factory.CreateClient();
        var req = new { userId = "user-004", title = "" };

        var res = await client.PostAsJsonAsync("/api/tasks", req);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ListTasks_ShouldReturnTasksOrderedNewestFirst()
    {
        var c1 = _factory.CreateClient();
        c1.DefaultRequestHeaders.Add("X-Client-Timestamp", DateTime.UtcNow.AddMinutes(-10).ToString("O"));
        await c1.PostAsJsonAsync("/api/tasks", new { userId = "user-order-test", title = "Older task" });

        var c2 = _factory.CreateClient();
        c2.DefaultRequestHeaders.Add("X-Client-Timestamp", DateTime.UtcNow.ToString("O"));
        await c2.PostAsJsonAsync("/api/tasks", new { userId = "user-order-test", title = "Newer task" });

        var client = _factory.CreateClient();
        var tasks = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?userId=user-order-test&limit=10");

        Assert.NotNull(tasks);
        Assert.True(tasks.Count >= 2);
        Assert.True(tasks.First().CreatedAt >= tasks.Last().CreatedAt);
    }

    [Fact]
    public async Task ListTasks_ShouldNotReturnOtherUsersTasks()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/tasks", new { userId = "user-isolated", title = "Private task" });

        var tasks = await client.GetFromJsonAsync<List<TaskDto>>("/api/tasks?userId=user-other&limit=50");

        Assert.NotNull(tasks);
        Assert.DoesNotContain(tasks, t => t.UserId == "user-isolated");
    }

    public record TaskDto(int Id, string UserId, string Title, string Status, DateTime CreatedAt, DateTime UpdatedAt);
}