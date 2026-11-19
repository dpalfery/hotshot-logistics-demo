namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Generic repository contract: the identity-based operations every repository offers for entity
    ///     <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public interface IRepository<T>
        where T : class
    {
        /// <summary>
        ///     Gets an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The entity if found, null otherwise.</returns>
        Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets all entities.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of all entities.</returns>
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Adds a new entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The added entity.</returns>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated entity.</returns>
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Deletes an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the entity was deleted, false otherwise.</returns>
        Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Checks if an entity exists by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the entity exists, false otherwise.</returns>
        Task<bool> ExistsAsync(object id, CancellationToken cancellationToken = default);
    }
}
