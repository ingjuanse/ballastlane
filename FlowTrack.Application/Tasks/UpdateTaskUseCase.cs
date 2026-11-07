using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;
using FlowTrack.Domain.Entities;
using FlowTrack.Domain.Exceptions;

namespace FlowTrack.Application.Tasks;

public sealed class UpdateTaskUseCase(ITaskRepository taskRepository)
{
    public async Task<UpdateTaskResult> ExecuteAsync(
        Guid taskId,
        Guid userId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userId == Guid.Empty)
        {
            return UpdateTaskResult.Failure(new[]
            {
                new ValidationError("user_id_required", "User ID is required.")
            });
        }

        var task = await taskRepository.GetByIdAsync(taskId, userId, cancellationToken);
        if (task is null)
        {
            return UpdateTaskResult.Failure(new[]
            {
                new ValidationError("task_not_found", "The requested task was not found.")
            });
        }

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return UpdateTaskResult.Failure(errors);
        }

        try
        {
            task.UpdateDetails(request.Title!.Trim(), request.Description);
            task.Reschedule(request.DueDate);
            task.ChangeStatus(request.Status!.Value);
        }
        catch (DomainRuleViolationException exception)
        {
            return UpdateTaskResult.Failure(new[]
            {
                new ValidationError("task_update_invalid", exception.Message)
            });
        }
        catch (ArgumentException exception)
        {
            return UpdateTaskResult.Failure(new[]
            {
                new ValidationError("task_update_invalid", exception.Message)
            });
        }

        if (!await taskRepository.UpdateAsync(task, userId, cancellationToken))
        {
            return UpdateTaskResult.Failure(new[]
            {
                new ValidationError("task_not_found", "The task could not be updated because it no longer exists.")
            });
        }

        return UpdateTaskResult.Success(ToResponse(task));
    }

    private static List<ValidationError> Validate(UpdateTaskRequest request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ValidationError("title_required", "Task title is required."));
        }
        else if (request.Title.Trim().Length > 200)
        {
            errors.Add(new ValidationError("title_length", "Task title cannot exceed 200 characters."));
        }

        if (request.Description?.Trim().Length > 2_000)
        {
            errors.Add(new ValidationError("description_length", "Task description cannot exceed 2000 characters."));
        }

        if (request.Status is null)
        {
            errors.Add(new ValidationError("status_required", "Task status is required."));
        }
        else if (!Enum.IsDefined(request.Status.Value))
        {
            errors.Add(new ValidationError("status_invalid", "The requested task status is invalid."));
        }

        if (request.DueDate is null)
        {
            errors.Add(new ValidationError("due_date_required", "Due date is required."));
        }
        else if (request.DueDate.Value < DateTimeOffset.UtcNow)
        {
            errors.Add(new ValidationError("due_date_in_past", "Due date cannot be in the past."));
        }

        return errors;
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
