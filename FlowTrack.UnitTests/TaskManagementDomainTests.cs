using FlowTrack.Domain.Exceptions;
using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.UnitTests;

public sealed class TaskManagementDomainTests
{
    [Fact]
    public void User_create_normalizes_valid_values()
    {
        var user = User.Create("  alex  ", "alex@example.com", "hashed-password");

        user.Id.Should().NotBeEmpty();
        user.UserName.Should().Be("alex");
        user.Email.Should().Be("alex@example.com");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    public void User_create_rejects_invalid_user_names(string userName)
    {
        var action = () => User.Create(userName, "alex@example.com", "hashed-password");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_create_rejects_invalid_email_and_password_hash()
    {
        var invalidEmail = () => User.Create("alex", "not-an-email", "hashed-password");
        var missingPasswordHash = () => User.Create("alex", "alex@example.com", " ");

        invalidEmail.Should().Throw<ArgumentException>();
        missingPasswordHash.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_update_profile_normalizes_values_and_changes_email()
    {
        var user = User.Create("alex", "alex@example.com", "old-hash");

        user.UpdateProfile("  jordan  ", "jordan@example.com");

        user.UserName.Should().Be("jordan");
        user.Email.Should().Be("jordan@example.com");
    }

    [Fact]
    public void User_change_password_hash_replaces_existing_hash()
    {
        var user = User.Create("alex", "alex@example.com", "old-hash");

        user.ChangePasswordHash("new-hash");

        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public void TaskItem_create_starts_in_todo_and_assigns_owner()
    {
        var userId = Guid.NewGuid();
        var dueDate = DateTimeOffset.UtcNow.AddDays(1);

        var task = TaskItem.Create("  Prepare release  ", "  Publish notes  ", dueDate, userId);

        task.Id.Should().NotBeEmpty();
        task.Title.Should().Be("Prepare release");
        task.Description.Should().Be("Publish notes");
        task.Status.Should().Be(TaskStatus.Todo);
        task.DueDate.Should().Be(dueDate);
        task.UserId.Should().Be(userId);
    }

    [Fact]
    public void TaskItem_create_rejects_past_due_dates()
    {
        var action = () => TaskItem.Create(
            "Prepare release",
            null,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            Guid.NewGuid());

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void TaskItem_create_rejects_invalid_title_description_and_owner()
    {
        var emptyTitle = () => TaskItem.Create(" ", null, null, Guid.NewGuid());
        var longDescription = () => TaskItem.Create("Task", new string('x', 2_001), null, Guid.NewGuid());
        var emptyOwner = () => TaskItem.Create("Task", null, null, Guid.Empty);

        emptyTitle.Should().Throw<ArgumentException>();
        longDescription.Should().Throw<ArgumentException>();
        emptyOwner.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TaskItem_allows_valid_forward_status_transitions()
    {
        var task = TaskItem.Create("Prepare release", null, null, Guid.NewGuid());

        task.ChangeStatus(TaskStatus.InProgress);
        task.ChangeStatus(TaskStatus.Completed);

        task.Status.Should().Be(TaskStatus.Completed);
    }

    [Fact]
    public void TaskItem_allows_cancellation_from_todo_or_in_progress()
    {
        var todoTask = TaskItem.Create("Prepare release", null, null, Guid.NewGuid());
        var inProgressTask = TaskItem.Create("Prepare release", null, null, Guid.NewGuid());
        inProgressTask.ChangeStatus(TaskStatus.InProgress);

        todoTask.ChangeStatus(TaskStatus.Cancelled);
        inProgressTask.ChangeStatus(TaskStatus.Cancelled);

        todoTask.Status.Should().Be(TaskStatus.Cancelled);
        inProgressTask.Status.Should().Be(TaskStatus.Cancelled);
    }

    [Fact]
    public void TaskItem_does_not_allow_completed_tasks_to_change()
    {
        var task = TaskItem.Create("Prepare release", null, null, Guid.NewGuid());
        task.ChangeStatus(TaskStatus.Completed);

        var reopen = () => task.ChangeStatus(TaskStatus.Todo);
        var update = () => task.UpdateDetails("Changed", null);
        var reschedule = () => task.Reschedule(DateTimeOffset.UtcNow.AddDays(1));

        reopen.Should().Throw<DomainRuleViolationException>();
        update.Should().Throw<DomainRuleViolationException>();
        reschedule.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void TaskItem_does_not_allow_in_progress_tasks_to_return_to_todo()
    {
        var task = TaskItem.Create("Prepare release", null, null, Guid.NewGuid());
        task.ChangeStatus(TaskStatus.InProgress);

        var action = () => task.ChangeStatus(TaskStatus.Todo);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void TaskItem_updates_details_and_due_date_while_open()
    {
        var task = TaskItem.Create("Prepare release", "Initial notes", null, Guid.NewGuid());
        var dueDate = DateTimeOffset.UtcNow.AddDays(2);

        task.UpdateDetails("  Publish release  ", "  Updated notes  ");
        task.Reschedule(dueDate);

        task.Title.Should().Be("Publish release");
        task.Description.Should().Be("Updated notes");
        task.DueDate.Should().Be(dueDate);
    }
}
