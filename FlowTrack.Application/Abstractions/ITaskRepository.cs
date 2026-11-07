using FlowTrack.Domain.Entities;
using FlowTrack.Application.Common;
using FlowTrack.Application.Tasks;

namespace FlowTrack.Application.Abstractions;

public interface ITaskRepository
{
    Task<PagedResult<TaskItem>> SearchAsync(
        TaskQuery query,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TaskItem>> ListAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
