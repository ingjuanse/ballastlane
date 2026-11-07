using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;
using FlowTrack.Application.Tasks;
using FlowTrack.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace FlowTrack.IntegrationTests;

public sealed class TaskApiIntegrationTests
{
    [Fact]
    public async Task Development_host_seeds_demo_user_and_task()
    {
        using var settings = new IntegrationEnvironment("Development");
        using var factory = new FlowTrackApiFactory("Development");
        using var client = factory.CreateClient();

        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "demo@flowtrack.local",
            password = "FlowTrackDemo1"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = loginJson.RootElement.GetProperty("accessToken").GetString();

        using var tasksRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        tasksRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var tasksResponse = await client.SendAsync(tasksRequest);
        Assert.Equal(HttpStatusCode.OK, tasksResponse.StatusCode);
        using var tasksJson = JsonDocument.Parse(await tasksResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, tasksJson.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Task_endpoints_require_authentication_and_enforce_user_ownership()
    {
        using var settings = new IntegrationEnvironment();
        using var factory = new FlowTrackApiFactory();
        using var client = factory.CreateClient();

        var anonymousResponse = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var invalidTokenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        invalidTokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var invalidTokenResponse = await client.SendAsync(invalidTokenRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenResponse.StatusCode);

        using var expiredTokenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        expiredTokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateExpiredToken());
        using var expiredTokenResponse = await client.SendAsync(expiredTokenRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, expiredTokenResponse.StatusCode);

        var owner = await RegisterAndLoginAsync(client, "owner", "owner@example.com");
        var otherUser = await RegisterAndLoginAsync(client, "other", "other@example.com");

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tasks")
        {
            Content = JsonContent.Create(new
            {
                title = "Launch review",
                description = "Review the release plan",
                dueDate = DateTimeOffset.UtcNow.AddDays(2)
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        using var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var taskId = createdJson.RootElement.GetProperty("id").GetGuid();

        using var searchRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tasks?search=LAUNCH");
        searchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        using var searchResponse = await client.SendAsync(searchRequest);
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        using var searchJson = JsonDocument.Parse(await searchResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, searchJson.RootElement.GetProperty("totalCount").GetInt32());

        using var otherGetRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{taskId}");
        otherGetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherUser.Token);
        using var otherGetResponse = await client.SendAsync(otherGetRequest);
        Assert.Equal(HttpStatusCode.NotFound, otherGetResponse.StatusCode);

        using var otherUpdateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/tasks/{taskId}")
        {
            Content = JsonContent.Create(new
            {
                title = "Unauthorized edit",
                description = "",
                status = "Todo",
                dueDate = DateTimeOffset.UtcNow.AddDays(3)
            })
        };
        otherUpdateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherUser.Token);
        using var otherUpdateResponse = await client.SendAsync(otherUpdateRequest);
        Assert.Equal(HttpStatusCode.NotFound, otherUpdateResponse.StatusCode);

        using var otherDeleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{taskId}");
        otherDeleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", otherUser.Token);
        using var otherDeleteResponse = await client.SendAsync(otherDeleteRequest);
        Assert.Equal(HttpStatusCode.NotFound, otherDeleteResponse.StatusCode);

        using var ownerUpdateRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/tasks/{taskId}")
        {
            Content = JsonContent.Create(new
            {
                title = "Launch review updated",
                description = "Review the final release plan",
                status = "InProgress",
                dueDate = DateTimeOffset.UtcNow.AddDays(4)
            })
        };
        ownerUpdateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        using var ownerUpdateResponse = await client.SendAsync(ownerUpdateRequest);
        Assert.Equal(HttpStatusCode.OK, ownerUpdateResponse.StatusCode);

        using var ownerDeleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{taskId}");
        ownerDeleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
        using var ownerDeleteResponse = await client.SendAsync(ownerDeleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, ownerDeleteResponse.StatusCode);
    }

    private static string CreateExpiredToken()
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(IntegrationEnvironment.TestSecret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "FlowTrack.Api",
            audience: "FlowTrack.Client",
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<(Guid UserId, string Token)> RegisterAndLoginAsync(
        HttpClient client,
        string userName,
        string email)
    {
        var password = "StrongPass1";
        using var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            userName,
            email,
            password
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return (
            loginJson.RootElement.GetProperty("userId").GetGuid(),
            loginJson.RootElement.GetProperty("accessToken").GetString()!);
    }
}

internal sealed class FlowTrackApiFactory : WebApplicationFactory<Program>
{
    private readonly string environment;

    public FlowTrackApiFactory(string environment = "Testing")
    {
        this.environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "integration-tests-signing-secret-with-32-bytes-minimum",
                ["Jwt:Issuer"] = "FlowTrack.Api",
                ["Jwt:Audience"] = "FlowTrack.Client"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ITaskRepository>();
            services.AddSingleton<IntegrationTestDataStore>();
            services.AddSingleton<IUserRepository>(provider => provider.GetRequiredService<IntegrationTestDataStore>());
            services.AddSingleton<ITaskRepository>(provider => provider.GetRequiredService<IntegrationTestDataStore>());
        });
    }
}

internal sealed class IntegrationEnvironment : IDisposable
{
    public const string TestSecret = "integration-tests-signing-secret-with-32-bytes-minimum";

    private readonly string? previousSecret = Environment.GetEnvironmentVariable("Jwt__Secret");
    private readonly string? previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    public IntegrationEnvironment(string environment = "Testing")
    {
        Environment.SetEnvironmentVariable(
            "Jwt__Secret",
            TestSecret);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", previousSecret);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousEnvironment);
    }
}

internal sealed class IntegrationTestDataStore : IUserRepository, ITaskRepository
{
    private readonly ConcurrentDictionary<Guid, User> users = new();
    private readonly ConcurrentDictionary<Guid, TaskItem> tasks = new();

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.Values.FirstOrDefault(user =>
            string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.Values.Any(user =>
            string.Equals(user.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.Values.Any(user =>
            string.Equals(user.UserName, userName.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<User>>(users.Values.ToArray());

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!users.ContainsKey(user.Id))
        {
            return Task.FromResult(false);
        }

        users[user.Id] = user;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(users.TryRemove(id, out _));

    public Task<PagedResult<TaskItem>> SearchAsync(
        TaskQuery query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TaskItem> matching = tasks.Values.Where(task => task.UserId == userId);
        if (query.Status is not null)
        {
            matching = matching.Where(task => task.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            matching = matching.Where(task =>
                task.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                task.Description.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.SortBy switch
        {
            TaskSortField.DueDate => query.SortDescending
                ? matching.OrderByDescending(task => task.DueDate)
                : matching.OrderBy(task => task.DueDate),
            TaskSortField.Title => query.SortDescending
                ? matching.OrderByDescending(task => task.Title)
                : matching.OrderBy(task => task.Title),
            TaskSortField.Status => query.SortDescending
                ? matching.OrderByDescending(task => task.Status)
                : matching.OrderBy(task => task.Status),
            _ => query.SortDescending
                ? matching.OrderByDescending(task => task.CreatedUtc)
                : matching.OrderBy(task => task.CreatedUtc)
        };
        var filtered = ordered.ToArray();
        var page = filtered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArray();
        return Task.FromResult(new PagedResult<TaskItem>(
            page,
            query.PageNumber,
            query.PageSize,
            filtered.LongLength));
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(tasks.TryGetValue(id, out var task) && task.UserId == userId ? task : null);

    public Task<IReadOnlyCollection<TaskItem>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<TaskItem>>(
            tasks.Values.Where(task => task.UserId == userId).ToArray());

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!tasks.TryGetValue(task.Id, out var existing) || existing.UserId != userId)
        {
            return Task.FromResult(false);
        }

        tasks[task.Id] = task;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            tasks.TryGetValue(id, out var task) &&
            task.UserId == userId &&
            tasks.TryRemove(id, out _));
}
