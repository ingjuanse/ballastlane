namespace FlowTrack.Infrastructure.MongoDb;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public string UsersCollectionName { get; set; } = "users";

    public string TasksCollectionName { get; set; } = "tasks";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("MongoDb:ConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(DatabaseName))
        {
            throw new InvalidOperationException("MongoDb:DatabaseName is required.");
        }

        if (string.IsNullOrWhiteSpace(UsersCollectionName))
        {
            throw new InvalidOperationException("MongoDb:UsersCollectionName is required.");
        }

        if (string.IsNullOrWhiteSpace(TasksCollectionName))
        {
            throw new InvalidOperationException("MongoDb:TasksCollectionName is required.");
        }
    }
}
