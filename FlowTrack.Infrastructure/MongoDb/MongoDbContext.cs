using FlowTrack.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace FlowTrack.Infrastructure.MongoDb;

public sealed class MongoDbContext
{
    static MongoDbContext()
    {
        BsonClassMap.RegisterClassMap<User>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(user => user.Id)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            classMap.MapMember(user => user.CreatedUtc);
        });
        BsonClassMap.RegisterClassMap<TaskItem>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(task => task.Id)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            classMap.MapMember(task => task.UserId)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            classMap.MapMember(task => task.CreatedUtc);
        });
    }

    public MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> settings)
    {
        var mongoDbSettings = settings.Value;
        mongoDbSettings.Validate();

        var database = client.GetDatabase(mongoDbSettings.DatabaseName);
        Users = database.GetCollection<User>(mongoDbSettings.UsersCollectionName);
        Tasks = database.GetCollection<TaskItem>(mongoDbSettings.TasksCollectionName);
    }

    public IMongoCollection<User> Users { get; }

    public IMongoCollection<TaskItem> Tasks { get; }
}
