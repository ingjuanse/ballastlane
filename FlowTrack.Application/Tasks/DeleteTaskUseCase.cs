using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;

namespace FlowTrack.Application.Tasks;

public sealed class DeleteTaskUseCase(ITaskRepository taskRepository)
{
    public async Task<DeleteTaskResult> ExecuteAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (taskId == Guid.Empty || userId == Guid.Empty)
        {
            return DeleteTaskResult.Failure(new[]
            {
                new ValidationError(
                    taskId == Guid.Empty ? "task_id_required" : "user_id_required",
                    taskId == Guid.Empty ? "Task ID is required." : "User ID is required.")
            });
        }

        var task = await taskRepository.GetByIdAsync(taskId, userId, cancellationToken);
        if (task is null)
        {
            return DeleteTaskResult.Failure(new[]
            {
                new ValidationError("task_not_found", "The requested task was not found.")
            });
        }

        if (!await taskRepository.DeleteAsync(taskId, userId, cancellationToken))
        {
            return DeleteTaskResult.Failure(new[]
            {
                new ValidationError("task_not_found", "The task could not be deleted because it no longer exists.")
            });
        }

        return DeleteTaskResult.Success();
    }
}
