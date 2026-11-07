using FlowTrack.Application.Abstractions;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.WorkItems;

public sealed class WorkItemService(IWorkItemRepository repository)
{
    public Task<IReadOnlyCollection<WorkItem>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.ListAsync(cancellationToken);

    public async Task<WorkItem> CreateAsync(string title, CancellationToken cancellationToken = default)
    {
        var workItem = WorkItem.Create(title);
        await repository.AddAsync(workItem, cancellationToken);
        return workItem;
    }
}
