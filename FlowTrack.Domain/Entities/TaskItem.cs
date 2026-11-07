using FlowTrack.Domain.Enums;
using FlowTrack.Domain.Exceptions;
using FlowTrack.Domain.Validation;
using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.Domain.Entities;

public sealed class TaskItem
{
    private TaskItem(
        Guid id,
        string title,
        string description,
        TaskStatus status,
        DateTimeOffset? dueDate,
        Guid userId)
        : this(id, title, description, status, dueDate, userId, DateTimeOffset.UtcNow)
    {
    }

    internal TaskItem(
        Guid id,
        string title,
        string description,
        TaskStatus status,
        DateTimeOffset? dueDate,
        Guid userId,
        DateTimeOffset createdUtc)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        UserId = userId;
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public TaskStatus Status { get; private set; }

    public DateTimeOffset? DueDate { get; private set; }

    public Guid UserId { get; }

    public DateTimeOffset CreatedUtc { get; }

    public static TaskItem Create(
        string title,
        string? description,
        DateTimeOffset? dueDate,
        Guid userId)
    {
        var normalizedTitle = DomainValidation.Required(title, nameof(title), 200);
        var normalizedDescription = description?.Trim() ?? string.Empty;
        if (normalizedDescription.Length > 2_000)
        {
            throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
        }

        DomainValidation.RequiredId(userId, nameof(userId));
        DomainValidation.FutureOrPresent(dueDate, nameof(dueDate));

        return new TaskItem(
            Guid.NewGuid(),
            normalizedTitle,
            normalizedDescription,
            TaskStatus.Todo,
            dueDate,
            userId);
    }

    public void UpdateDetails(string title, string? description)
    {
        EnsureOpen();

        Title = DomainValidation.Required(title, nameof(title), 200);
        var normalizedDescription = description?.Trim() ?? string.Empty;
        if (normalizedDescription.Length > 2_000)
        {
            throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
        }

        Description = normalizedDescription;
    }

    public void Reschedule(DateTimeOffset? dueDate)
    {
        EnsureOpen();
        DomainValidation.FutureOrPresent(dueDate, nameof(dueDate));
        DueDate = dueDate;
    }

    public void ChangeStatus(TaskStatus status)
    {
        if (status == Status)
        {
            return;
        }

        if (Status is TaskStatus.Completed or TaskStatus.Cancelled)
        {
            throw new DomainRuleViolationException("Completed or cancelled tasks cannot be reopened or changed.");
        }

        if (Status == TaskStatus.Todo && status is not (TaskStatus.InProgress or TaskStatus.Completed or TaskStatus.Cancelled))
        {
            throw new DomainRuleViolationException("A task in Todo status can only move to InProgress, Completed, or Cancelled.");
        }

        if (Status == TaskStatus.InProgress && status is not (TaskStatus.Completed or TaskStatus.Cancelled))
        {
            throw new DomainRuleViolationException("A task in InProgress status can only move to Completed or Cancelled.");
        }

        Status = status;
    }

    private void EnsureOpen()
    {
        if (Status is TaskStatus.Completed or TaskStatus.Cancelled)
        {
            throw new DomainRuleViolationException("Completed or cancelled tasks cannot be modified.");
        }
    }
}
