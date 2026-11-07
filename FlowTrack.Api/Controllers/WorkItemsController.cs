using FlowTrack.Application.WorkItems;
using FlowTrack.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FlowTrack.Api.Controllers;

[ApiController]
[Route("api/work-items")]
public sealed class WorkItemsController(WorkItemService workItemService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<WorkItem>> List(CancellationToken cancellationToken) =>
        workItemService.ListAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<WorkItem>> Create(
        [FromBody] CreateWorkItemRequest request,
        CancellationToken cancellationToken)
    {
        var workItem = await workItemService.CreateAsync(request.Title, cancellationToken);
        return CreatedAtAction(nameof(List), new { id = workItem.Id }, workItem);
    }

    public sealed record CreateWorkItemRequest(string Title);
}
