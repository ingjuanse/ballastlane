using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Abstractions;

public interface IWorkItemRepository
{
    Task<IReadOnlyCollection<WorkItem>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(WorkItem workItem, CancellationToken cancellationToken = default);
}
