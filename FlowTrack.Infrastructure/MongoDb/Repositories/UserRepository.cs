using FlowTrack.Application.Abstractions;
using FlowTrack.Domain.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace FlowTrack.Infrastructure.MongoDb.Repositories;

public sealed class UserRepository(MongoDbContext context) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await context.Users
                .AsQueryable()
                .Where(user => user.Email.ToLower() == normalizedEmail)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("get user by email", exception);
        }
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await context.Users
                .AsQueryable()
                .AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("check user email uniqueness", exception);
        }
    }

    public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedUserName = userName.Trim().ToLowerInvariant();
            return await context.Users
                .AsQueryable()
                .AnyAsync(user => user.UserName.ToLower() == normalizedUserName, cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("check username uniqueness", exception);
        }
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Users
                .AsQueryable()
                .Where(user => user.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("get user by id", exception);
        }
    }

    public async Task<IReadOnlyCollection<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Users
                .AsQueryable()
                .OrderBy(user => user.UserName)
                .ToListAsync(cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("list users", exception);
        }
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("add user", exception);
        }
    }

    public async Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var result = await context.Users.ReplaceOneAsync(
                existingUser => existingUser.Id == user.Id,
                user,
                cancellationToken: cancellationToken);

            return result.MatchedCount > 0;
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("update user", exception);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await context.Users.DeleteOneAsync(
                user => user.Id == id,
                cancellationToken);

            return result.DeletedCount > 0;
        }
        catch (MongoException exception)
        {
            throw MongoRepositoryExceptionHandling.Wrap("delete user", exception);
        }
    }
}
