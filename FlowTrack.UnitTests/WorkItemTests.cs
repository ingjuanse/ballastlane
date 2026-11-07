namespace FlowTrack.UnitTests;

public sealed class WorkItemTests
{
    [Fact]
    public void Create_rejects_empty_titles()
    {
        Assert.Throws<ArgumentException>(() => WorkItem.Create(" "));
    }

    [Fact]
    public void Create_trims_and_preserves_title()
    {
        var workItem = WorkItem.Create("  Plan release  ");

        Assert.Equal("Plan release", workItem.Title);
    }
}
