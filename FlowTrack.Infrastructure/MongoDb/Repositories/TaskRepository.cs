using System.Text.RegularExpressions;
using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;
using FlowTrack.Application.Tasks;
using FlowTrack.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FlowTrack.Infrastructure.MongoDb.Repositories;

public sealed class TaskRepository(MongoDbContext context) : ITaskRepository
{
    public async Task<PagedResult<TaskItem>> SearchAsync(
        TaskQuery query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var filter = BuildSearchFilter(query, userId);
            var totalCount = await context.Tasks.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var items = await context.Tasks
                .Find(filter)
                .Sort(BuildSort(query))
                .Skip(checked((query.PageNumber - 1) * query.PageSize))
                .Limit(query.PageSize)
                .ToListAsync(cancellationToken);
            return new PagedResult<TaskItem>(items, query.PageNumber, query.PageSize, totalCount);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("search tasks", exception);
        }
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Tasks
                .Find(BuildOwnerFilter(id, userId))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("get task by id", exception);
        }
    }

    public async Task<IReadOnlyCollection<TaskItem>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Tasks.Find(task => task.UserId == userId)
                .SortBy(task => task.CreatedUtc)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("list tasks", exception);
        }
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            await context.Tasks.InsertOneAsync(task, cancellationToken: cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("add task", exception);
        }
    }

    public async Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            var result = await context.Tasks.ReplaceOneAsync(
                BuildOwnerFilter(task.Id, userId),
                task,
                cancellationToken: cancellationToken);

            return result.MatchedCount > 0;
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("update task", exception);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await context.Tasks.DeleteOneAsync(
                BuildOwnerFilter(id, userId),
                cancellationToken);

            return result.DeletedCount > 0;
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("delete task", exception);
        }
    }

    private static SortDefinition<TaskItem> BuildSort(TaskQuery query)
    {
        return query.SortBy switch
        {
            TaskSortField.DueDate => query.SortDescending
                ? Builders<TaskItem>.Sort.Descending(task => task.DueDate)
                : Builders<TaskItem>.Sort.Ascending(task => task.DueDate),
            TaskSortField.Title => query.SortDescending
                ? Builders<TaskItem>.Sort.Descending(task => task.Title)
                : Builders<TaskItem>.Sort.Ascending(task => task.Title),
            TaskSortField.Status => query.SortDescending
                ? Builders<TaskItem>.Sort.Descending(task => task.Status)
                : Builders<TaskItem>.Sort.Ascending(task => task.Status),
            _ => query.SortDescending
                ? Builders<TaskItem>.Sort.Descending(task => task.CreatedUtc)
                : Builders<TaskItem>.Sort.Ascending(task => task.CreatedUtc)
        };
    }

    internal static FilterDefinition<TaskItem> BuildSearchFilter(TaskQuery query, Guid userId)
    {
        var filter = Builders<TaskItem>.Filter.Eq(task => task.UserId, userId);
        if (query.Status is not null)
        {
            filter &= Builders<TaskItem>.Filter.Eq(task => task.Status, query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var expression = new BsonRegularExpression(Regex.Escape(query.Search.Trim()), "i");
            filter &= Builders<TaskItem>.Filter.Or(
                Builders<TaskItem>.Filter.Regex(task => task.Title, expression),
                Builders<TaskItem>.Filter.Regex(task => task.Description, expression));
        }

        return filter;
    }

    internal static FilterDefinition<TaskItem> BuildOwnerFilter(Guid id, Guid userId) =>
        Builders<TaskItem>.Filter.Eq(task => task.Id, id) &
        Builders<TaskItem>.Filter.Eq(task => task.UserId, userId);
}
