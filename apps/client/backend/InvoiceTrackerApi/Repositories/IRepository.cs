namespace InvoiceTrackerApi.Repositories;

/// <summary>
/// Base repository interface defining common CRUD operations for all entities
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Retrieves an entity by its ID
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Adds a new entity to the database
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <returns>The added entity with generated ID</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity in the database
    /// </summary>
    /// <param name="entity">The entity to update</param>
    Task UpdateAsync(T entity);

    /// <summary>
    /// Deletes an entity from the database
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    Task DeleteAsync(T entity);

    /// <summary>
    /// Checks if an entity with the given ID exists
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <returns>True if the entity exists, false otherwise</returns>
    Task<bool> ExistsAsync(int id);
}
