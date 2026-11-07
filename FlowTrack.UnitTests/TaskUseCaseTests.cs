using FlowTrack.Application.Tasks;
using TaskStatus = FlowTrack.Domain.Enums.TaskStatus;

namespace FlowTrack.UnitTests;

public sealed class CreateTaskUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_creates_task_and_persists_it()
    {
        // Arrange
        var repository = new Mock<ITaskRepository>();
        var useCase = new CreateTaskUseCase(repository.Object);
        var userId = Guid.NewGuid();
        var request = new CreateTaskRequest(
            "  Prepare release  ",
            "  Publish notes  ",
            DateTimeOffset.UtcNow.AddDays(1),
            userId);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Task.Should().NotBeNull();
        result.Task!.Title.Should().Be("Prepare release");
        result.Task.Description.Should().Be("Publish notes");
        result.Task.Status.Should().Be(TaskStatus.Todo);
        result.Task.UserId.Should().Be(userId);
        repository.Verify(
            repository => repository.AddAsync(
                It.Is<TaskItem>(task =>
                    task.Title == "Prepare release" &&
                    task.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_errors_without_persisting_invalid_task()
    {
        // Arrange
        var repository = new Mock<ITaskRepository>();
        var useCase = new CreateTaskUseCase(repository.Object);
        var request = new CreateTaskRequest(null, null, null, null);

        // Act
        var result = await useCase.ExecuteAsync(request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain(new[]
        {
            "title_required",
            "due_date_required",
            "user_id_required"
        });
        repository.Verify(
            repository => repository.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public sealed class UpdateTaskUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_updates_task_and_persists_changes()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<ITaskRepository>();
        var task = TaskItem.Create("Old title", "Old description", null, userId);
        repository
            .Setup(repository => repository.GetByIdAsync(taskId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repository
            .Setup(repository => repository.UpdateAsync(task, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var useCase = new UpdateTaskUseCase(repository.Object);
        var request = new UpdateTaskRequest(
            "  New title  ",
            "  New description  ",
            TaskStatus.InProgress,
            DateTimeOffset.UtcNow.AddDays(2));

        // Act
        var result = await useCase.ExecuteAsync(taskId, userId, request);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Task.Should().NotBeNull();
        result.Task!.Title.Should().Be("New title");
        result.Task.Description.Should().Be("New description");
        result.Task.Status.Should().Be(TaskStatus.InProgress);
        task.DueDate.Should().Be(request.DueDate);
        repository.Verify(
            repository => repository.UpdateAsync(task, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_task_does_not_exist()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<ITaskRepository>();
        repository
            .Setup(repository => repository.GetByIdAsync(taskId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var useCase = new UpdateTaskUseCase(repository.Object);
        var request = new UpdateTaskRequest(
            "New title",
            null,
            TaskStatus.Todo,
            DateTimeOffset.UtcNow.AddDays(1));

        // Act
        var result = await useCase.ExecuteAsync(taskId, userId, request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "task_not_found");
        repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<TaskItem>(), userId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_returns_validation_errors_without_updating_invalid_request()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<ITaskRepository>();
        var task = TaskItem.Create("Existing task", null, null, userId);
        repository
            .Setup(repository => repository.GetByIdAsync(taskId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        var useCase = new UpdateTaskUseCase(repository.Object);
        var request = new UpdateTaskRequest(null, null, null, null);

        // Act
        var result = await useCase.ExecuteAsync(taskId, userId, request);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain(new[]
        {
            "title_required",
            "status_required",
            "due_date_required"
        });
        repository.Verify(
            repository => repository.UpdateAsync(It.IsAny<TaskItem>(), userId, It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public sealed class DeleteTaskUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_deletes_existing_task()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<ITaskRepository>();
        var task = TaskItem.Create("Task to delete", null, null, userId);
        repository
            .Setup(repository => repository.GetByIdAsync(taskId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        repository
            .Setup(repository => repository.DeleteAsync(taskId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var useCase = new DeleteTaskUseCase(repository.Object);

        // Act
        var result = await useCase.ExecuteAsync(taskId, userId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        repository.Verify(
            repository => repository.DeleteAsync(taskId, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    public sealed class GetTasksUseCaseTests
    {
        [Fact]
        public async Task ExecuteAsync_passes_search_and_user_scope_to_repository()
        {
            var userId = Guid.NewGuid();
            var query = new TaskQuery(PageNumber: 1, PageSize: 10, Search: "release");
            var repository = new Mock<ITaskRepository>();
            repository
                .Setup(repository => repository.SearchAsync(query, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<TaskItem>(Array.Empty<TaskItem>(), 1, 10, 0));
            var useCase = new GetTasksUseCase(repository.Object);

            var result = await useCase.ExecuteAsync(query, userId);

            result.Succeeded.Should().BeTrue();
            result.Page.Should().NotBeNull();
            repository.Verify(
                repository => repository.SearchAsync(query, userId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(1, 101, "page_size_invalid")]
        [InlineData(214748366, 10, "page_number_too_large")]
        public async Task ExecuteAsync_rejects_unsupported_pagination(int pageNumber, int pageSize, string errorCode)
        {
            var repository = new Mock<ITaskRepository>();
            var useCase = new GetTasksUseCase(repository.Object);

            var result = await useCase.ExecuteAsync(new TaskQuery(pageNumber, pageSize), Guid.NewGuid());

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle(error => error.Code == errorCode);
            repository.VerifyNoOtherCalls();
        }
    }

    [Fact]
    public async Task ExecuteAsync_returns_not_found_when_task_does_not_exist()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new Mock<ITaskRepository>();
        repository
            .Setup(repository => repository.GetByIdAsync(taskId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);
        var useCase = new DeleteTaskUseCase(repository.Object);

        // Act
        var result = await useCase.ExecuteAsync(taskId, userId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "task_not_found");
        repository.Verify(
            repository => repository.DeleteAsync(It.IsAny<Guid>(), userId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_empty_task_id_without_repository_access()
    {
        // Arrange
        var repository = new Mock<ITaskRepository>();
        var useCase = new DeleteTaskUseCase(repository.Object);

        // Act
        var result = await useCase.ExecuteAsync(Guid.Empty, Guid.NewGuid());

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "task_id_required");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_rejects_missing_user_id_without_repository_access()
    {
        var repository = new Mock<ITaskRepository>();
        var useCase = new DeleteTaskUseCase(repository.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), Guid.Empty);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == "user_id_required");
        repository.VerifyNoOtherCalls();
    }
}
