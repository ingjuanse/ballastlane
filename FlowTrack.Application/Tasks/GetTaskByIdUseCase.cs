using FlowTrack.Application.Abstractions;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Tasks;

public sealed class GetTaskByIdUseCase(ITaskRepository taskRepository)
{
    public async Task<TaskResponse?> ExecuteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || userId == Guid.Empty)
        {
            return null;
        }

        var task = await taskRepository.GetByIdAsync(id, userId, cancellationToken);
        return task is null ? null : ToResponse(task);
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate ?? default,
            task.UserId,
            task.CreatedUtc);
}
