using FlowTrack.Application.Abstractions;
using FlowTrack.Application.Common;
using FlowTrack.Domain.Entities;

namespace FlowTrack.Application.Tasks;

public sealed class GetTasksUseCase(ITaskRepository taskRepository)
{
    public async Task<GetTasksResult> ExecuteAsync(
        TaskQuery query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = Validate(query, userId);
        if (errors.Count > 0)
        {
            return GetTasksResult.Failure(errors);
        }

        var page = await taskRepository.SearchAsync(query, userId, cancellationToken);
        var response = new PagedResult<TaskResponse>(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount);

        return GetTasksResult.Success(response);
    }

    private static List<ValidationError> Validate(TaskQuery query, Guid userId)
    {
        var errors = new List<ValidationError>();

        if (query.PageNumber < 1)
        {
            errors.Add(new ValidationError("page_number_invalid", "Page number must be greater than zero."));
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors.Add(new ValidationError("page_size_invalid", "Page size must be between 1 and 100."));
        }
        else if (query.PageNumber - 1 > int.MaxValue / query.PageSize)
        {
            errors.Add(new ValidationError("page_number_too_large", "The requested page is outside the supported range."));
        }

        if (!Enum.IsDefined(query.SortBy))
        {
            errors.Add(new ValidationError("sort_field_invalid", "The requested sort field is not supported."));
        }

        if (userId == Guid.Empty)
        {
            errors.Add(new ValidationError("user_id_required", "User ID is required."));
        }

        if (query.Search?.Length > 200)
        {
            errors.Add(new ValidationError("search_length", "Search text cannot exceed 200 characters."));
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
