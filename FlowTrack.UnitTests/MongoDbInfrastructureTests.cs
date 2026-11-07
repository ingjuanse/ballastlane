using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FlowTrack.UnitTests;

public sealed class MongoDbInfrastructureTests
{
    [Fact]
    public void Settings_validate_required_values()
    {
        var settings = new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "flowtrack"
        };

        settings.Validate();

        Assert.Equal("users", settings.UsersCollectionName);
        Assert.Equal("tasks", settings.TasksCollectionName);
    }

    [Fact]
    public void Context_exposes_configured_user_and_task_collections()
    {
        var settings = new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "flowtrack",
            UsersCollectionName = "flowtrack-users",
            TasksCollectionName = "flowtrack-tasks"
        };
        var context = new MongoDbContext(
            new MongoClient(settings.ConnectionString),
            Options.Create(settings));

        Assert.Equal("flowtrack-users", context.Users.CollectionNamespace.CollectionName);
        Assert.Equal("flowtrack-tasks", context.Tasks.CollectionNamespace.CollectionName);
        Assert.Equal("flowtrack", context.Users.CollectionNamespace.DatabaseNamespace.DatabaseName);
    }

    [Fact]
    public void Settings_reject_missing_database_name()
    {
        var settings = new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017"
        };

        Assert.Throws<InvalidOperationException>(settings.Validate);
    }

    [Fact]
    public void User_guid_is_serialized_with_standard_representation()
    {
        _ = CreateContext();
        var user = User.Create("test-user", "test@example.com", "password-hash");

        var document = user.ToBsonDocument();

        Assert.Equal(BsonBinarySubType.UuidStandard, document["_id"].AsBsonBinaryData.SubType);
        Assert.True(document.Contains("CreatedUtc"));
    }

    [Fact]
    public void User_bson_round_trip_preserves_id_and_created_timestamp()
    {
        _ = CreateContext();
        var user = User.Create("test-user", "test@example.com", "password-hash");

        var restored = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<User>(user.ToBsonDocument());

        Assert.Equal(user.Id, restored.Id);
        Assert.Equal(user.UserName, restored.UserName);
        Assert.Equal(user.Email, restored.Email);
        Assert.Equal(user.PasswordHash, restored.PasswordHash);
        Assert.Equal(user.CreatedUtc.ToUnixTimeMilliseconds(), restored.CreatedUtc.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void Task_guid_fields_are_serialized_with_standard_representation()
    {
        _ = CreateContext();
        var task = TaskItem.Create("Test task", null, null, Guid.NewGuid());

        var document = task.ToBsonDocument();

        Assert.Equal(BsonBinarySubType.UuidStandard, document["_id"].AsBsonBinaryData.SubType);
        Assert.Equal(BsonBinarySubType.UuidStandard, document["UserId"].AsBsonBinaryData.SubType);
        Assert.True(document.Contains("CreatedUtc"));
    }

    [Fact]
    public void Task_bson_round_trip_preserves_ids_and_created_timestamp()
    {
        _ = CreateContext();
        var task = TaskItem.Create("Test task", "A description", null, Guid.NewGuid());

        var restored = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<TaskItem>(task.ToBsonDocument());

        Assert.Equal(task.Id, restored.Id);
        Assert.Equal(task.UserId, restored.UserId);
        Assert.Equal(task.Title, restored.Title);
        Assert.Equal(task.Description, restored.Description);
        Assert.Equal(task.CreatedUtc.ToUnixTimeMilliseconds(), restored.CreatedUtc.ToUnixTimeMilliseconds());
    }

    [Fact]
    public void Task_query_can_sort_by_created_utc()
    {
        var context = CreateContext();

        var query = context.Tasks.AsQueryable().OrderBy(task => task.CreatedUtc);

        Assert.Contains("CreatedUtc", query.ToString());
    }

    [Fact]
    public void Task_search_filter_is_user_scoped_and_escapes_search_patterns()
    {
        _ = CreateContext();
        var userId = Guid.NewGuid();
        var query = new TaskQuery(Status: FlowTrack.Domain.Enums.TaskStatus.InProgress, Search: " .* ");

        var filter = TaskRepository.BuildSearchFilter(query, userId);
        var rendered = filter.Render(new RenderArgs<TaskItem>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TaskItem>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

        Assert.Equal(userId, FindValue(rendered, "UserId")!.AsBsonBinaryData.ToGuid());
        Assert.Equal((int)FlowTrack.Domain.Enums.TaskStatus.InProgress, FindValue(rendered, "Status")!.AsInt32);
        var regex = FindRegex(rendered)!;
        Assert.Equal(@"\.\*", regex.Pattern);
        Assert.Equal("i", regex.Options);
    }

    [Fact]
    public void Task_owner_filter_includes_both_task_and_user_ids()
    {
        _ = CreateContext();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var filter = TaskRepository.BuildOwnerFilter(taskId, userId);
        var rendered = filter.Render(new RenderArgs<TaskItem>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TaskItem>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

        Assert.Equal(taskId, FindValue(rendered, "_id")!.AsBsonBinaryData.ToGuid());
        Assert.Equal(userId, FindValue(rendered, "UserId")!.AsBsonBinaryData.ToGuid());
    }

    private static MongoDbContext CreateContext()
    {
        var settings = new MongoDbSettings
        {
            ConnectionString = "mongodb://localhost:27017",
            DatabaseName = "flowtrack"
        };

        return new MongoDbContext(
            new MongoClient(settings.ConnectionString),
            Options.Create(settings));
    }

    private static BsonValue? FindValue(BsonDocument document, string key)
    {
        foreach (var element in document)
        {
            if (element.Name == key)
            {
                return element.Value;
            }

            var found = element.Value switch
            {
                BsonDocument nestedDocument => FindValue(nestedDocument, key),
                BsonArray nestedArray => nestedArray
                    .OfType<BsonDocument>()
                    .Select(nestedDocument => FindValue(nestedDocument, key))
                    .FirstOrDefault(value => value is not null),
                _ => null
            };
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static BsonRegularExpression? FindRegex(BsonValue value)
    {
        if (value is BsonRegularExpression regex)
        {
            return regex;
        }

        if (value is BsonDocument document)
        {
            return document.Elements
                .Select(element => FindRegex(element.Value))
                .FirstOrDefault(candidate => candidate is not null);
        }

        if (value is BsonArray array)
        {
            return array
                .Select(FindRegex)
                .FirstOrDefault(candidate => candidate is not null);
        }

        return null;
    }
}
