using System.Collections.Concurrent;
using FlowTrack.Application.Abstractions;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Infrastructure.Persistence;

public sealed class InMemoryWorkItemRepository : IWorkItemRepository
{
    private readonly ConcurrentDictionary<Guid, WorkItem> workItems = new();

    public Task<IReadOnlyCollection<WorkItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<WorkItem> result = workItems.Values.ToArray();
        return Task.FromResult(result);
    }

    public Task AddAsync(WorkItem workItem, CancellationToken cancellationToken = default)
    {
        workItems[workItem.Id] = workItem;
        return Task.CompletedTask;
    }
}
