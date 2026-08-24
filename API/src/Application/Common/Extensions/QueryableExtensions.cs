using Microsoft.EntityFrameworkCore;
using ProjectX.Application.Common.Exceptions;

namespace ProjectX.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static async Task<T> SingleOrNotFoundAsync<T>(
        this IQueryable<T> query,
        string resourceName,
        CancellationToken cancellationToken)
        where T : class
    {
        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(resourceName);
    }

    public static async Task<T> FirstOrNotFoundAsync<T>(
        this IQueryable<T> query,
        string resourceName,
        CancellationToken cancellationToken)
        where T : class
    {
        return await query.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(resourceName);
    }
}
