using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace couch_backend.Repositories.Interfaces
{
    /// <summary>
    /// Interface for all repositories used to request data from the Db
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IGenericRepository<TEntity>
    {

        /// <summary>
        /// Get all enitities that match 'filter', ordered by 'orderBy', and includes 'includeProperties'
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "");

        /// <summary>
        /// Get an entity using it's primary key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntity> GetByIDAsync(object id);

        /// <summary>
        /// Insert an object into the Db
        /// </summary>
        /// <param name="entity"></param>
        Task InsertAsync(TEntity entity);

        /// <summary>
        /// Delete an entity using it's primary key
        /// </summary>
        /// <param name="id"></param>
        Task DeleteAsync(object id);

        /// <summary>
        /// Delete a single entity
        /// </summary>
        /// <param name="entityToDelete"></param>
        Task DeleteAsync(TEntity entityToDelete);

        /// <summary>
        /// Delete all the passed entities
        /// </summary>
        /// <param name="entitiesToDelete"></param>
        Task DeleteRangeAsync(IEnumerable<TEntity> entitiesToDelete);

        /// <summary>
        /// Update the specified entity
        /// </summary>
        /// <param name="entityToUpdate"></param>
        Task UpdateAsync(TEntity entityToUpdate);

        /// <summary>
        /// Save the cahnges made to the Db
        /// </summary>
        Task SaveAsync();
    }
}
