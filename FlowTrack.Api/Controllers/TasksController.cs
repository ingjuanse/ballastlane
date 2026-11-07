using System.Security.Claims;
using FlowTrack.Api.Contracts.Tasks;
using FlowTrack.Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApplicationTaskSortField = FlowTrack.Application.Tasks.TaskSortField;
using ApiCreateTaskRequest = FlowTrack.Api.Contracts.Tasks.CreateTaskRequest;
using ApiTaskResponse = FlowTrack.Api.Contracts.Tasks.TaskResponse;
using ApiUpdateTaskRequest = FlowTrack.Api.Contracts.Tasks.UpdateTaskRequest;

namespace FlowTrack.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
[Produces("application/json")]
public sealed class TasksController(
    GetTasksUseCase getTasksUseCase,
    GetTaskByIdUseCase getTaskByIdUseCase,
    CreateTaskUseCase createTaskUseCase,
    UpdateTaskUseCase updateTaskUseCase,
    DeleteTaskUseCase deleteTaskUseCase) : ControllerBase
{
    /// <summary>Returns a paginated list of tasks for the authenticated user.</summary>
    /// <response code="200">The requested task page.</response>
    /// <response code="400">The query parameters are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(
        [FromQuery] TaskQueryRequest query,
        CancellationToken cancellationToken)
    {
        var result = await getTasksUseCase.ExecuteAsync(
            new TaskQuery(
                query.PageNumber,
                query.PageSize,
                query.Status,
                (ApplicationTaskSortField)(int)query.SortBy,
                query.SortDescending,
                query.Search),
            GetUserId(),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ToValidationProblem(result.Errors));
        }

        var page = result.Page!;
        return Ok(new PagedTaskResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount));
    }

    /// <summary>Returns a task by ID.</summary>
    /// <response code="200">The requested task.</response>
    /// <response code="404">The task was not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await getTaskByIdUseCase.ExecuteAsync(id, GetUserId(), cancellationToken);
        return task is null ? NotFound() : Ok(ToResponse(task));
    }

    /// <summary>Creates a task for the authenticated user.</summary>
    /// <response code="201">The task was created.</response>
    /// <response code="400">The task request is invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiTaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] ApiCreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createTaskUseCase.ExecuteAsync(
            new Application.Tasks.CreateTaskRequest(
                request.Title,
                request.Description,
                request.DueDate,
                GetUserId()),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(ToValidationProblem(result.Errors));
        }

        var task = ToResponse(result.Task!);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>Updates an existing task.</summary>
    /// <response code="200">The task was updated.</response>
    /// <response code="400">The task request is invalid.</response>
    /// <response code="404">The task was not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ApiUpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateTaskUseCase.ExecuteAsync(
            id,
            GetUserId(),
            new Application.Tasks.UpdateTaskRequest(
                request.Title,
                request.Description,
                request.Status,
                request.DueDate),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.Errors.Any(error => error.Code == "task_not_found")
                ? NotFound()
                : BadRequest(ToValidationProblem(result.Errors));
        }

        return Ok(ToResponse(result.Task!));
    }

    /// <summary>Deletes an existing task.</summary>
    /// <response code="204">The task was deleted.</response>
    /// <response code="404">The task was not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await deleteTaskUseCase.ExecuteAsync(id, GetUserId(), cancellationToken);
        if (!result.Succeeded)
        {
            return result.Errors.Any(error => error.Code == "task_not_found")
                ? NotFound()
                : BadRequest(ToValidationProblem(result.Errors));
        }

        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId) || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("A valid user identity is required.");
        }

        return userId;
    }

    private static ApiTaskResponse ToResponse(Application.Tasks.TaskResponse task) =>
        new(task.Id, task.Title, task.Description, task.Status, task.DueDate, task.UserId, task.CreatedUtc);

    private static ValidationProblemDetails ToValidationProblem(
        IEnumerable<FlowTrack.Application.Common.ValidationError> errors) =>
        new(errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray()));
}
