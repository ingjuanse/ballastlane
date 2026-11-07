namespace FlowTrack.Domain.Entities;

public sealed class WorkItem
{
    private WorkItem(Guid id, string title)
    {
        Id = id;
        Title = title;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string Title { get; private set; }

    public DateTimeOffset CreatedUtc { get; }

    public static WorkItem Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A work item title is required.", nameof(title));
        }

        return new WorkItem(Guid.NewGuid(), title.Trim());
    }
}
