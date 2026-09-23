using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    // SQL Server error numbers for unique index / unique constraint violations.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
                                           {
                                               Number: UniqueIndexViolation or UniqueConstraintViolation
                                           })
        {
            // Covers the race where two requests pass the "already exists" check at the same time.
            throw new ConflictException("A record with the same unique value already exists.");
        }
    }
}
