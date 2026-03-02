using Shared.Database.Data;
using Shared.Core.Exceptions;
using Shared.Core.Exceptions.Application;
using Shared.Core.Exceptions.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackerApi.Repositories;

/// <summary>
/// Base repository implementation providing common CRUD operations for all entities.
/// Wraps infrastructure exceptions (database errors) into application-level exceptions.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to retrieve entity from database", ex);
        }
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Failed to add entity to database", ex);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Database operation failed", ex);
        }
    }

    public virtual async Task UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The entity was modified or deleted by another user", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Failed to update entity in database", ex);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Database operation failed", ex);
        }
    }

    public virtual async Task DeleteAsync(T entity)
    {
        try
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException("Failed to delete entity from database", ex);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Database operation failed", ex);
        }
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        try
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null;
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new DatabaseUnavailableException("Failed to check entity existence in database", ex);
        }
    }
}
