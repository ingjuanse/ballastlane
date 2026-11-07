using MongoDB.Driver;

namespace FlowTrack.Infrastructure.MongoDb;

internal static class MongoRepositoryExceptionHandling
{
    public static MongoRepositoryException Wrap(string operation, MongoException exception) =>
        new($"MongoDB operation '{operation}' failed.", exception);
}
