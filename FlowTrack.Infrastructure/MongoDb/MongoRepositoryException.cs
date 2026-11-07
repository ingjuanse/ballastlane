namespace FlowTrack.Infrastructure.MongoDb;

public sealed class MongoRepositoryException(string message, Exception innerException)
    : Exception(message, innerException)
{
}
