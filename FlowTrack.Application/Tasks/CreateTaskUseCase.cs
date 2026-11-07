using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Tasks;

public sealed class CreateTaskUseCase(ITaskRepository taskRepository)
{
    public async Task<CreateTaskResult> ExecuteAsync(
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return CreateTaskResult.Failure(errors);
        }

        var task = TaskItem.Create(
            request.Title!.Trim(),
            request.Description,
            request.DueDate,
            request.UserId!.Value);

        await taskRepository.AddAsync(task, cancellationToken);

        return CreateTaskResult.Success(ToResponse(task));
    }

    private static List<ValidationError> Validate(CreateTaskRequest request)
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

        if (request.DueDate is null)
        {
            errors.Add(new ValidationError("due_date_required", "Due date is required."));
        }
        else if (request.DueDate.Value < DateTimeOffset.UtcNow)
        {
            errors.Add(new ValidationError("due_date_in_past", "Due date cannot be in the past."));
        }

        if (request.UserId is null || request.UserId == Guid.Empty)
        {
            errors.Add(new ValidationError("user_id_required", "User ID is required."));
        }

        return errors;
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.DueDate!.Value,
            task.UserId,
            task.CreatedUtc);
}
