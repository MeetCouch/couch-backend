using couch_backend.DbContexts;
using couch_backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace couch_backend.Repositories.Implementations
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        internal MariaDbContext context;
        internal DbSet<TEntity> dbSet;

        /// <summary>
        /// A Generic implementation of the repository used to request data from the database
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dbSet"></param>
        public GenericRepository(MariaDbContext context, DbSet<TEntity> dbSet)
        {
            this.context = context;
            this.dbSet = dbSet;
        }

        /// <summary>
        /// Get all enitities that match 'filter', ordered by 'orderBy', and includes 'includeProperties'
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        public virtual Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return Task.FromResult<IEnumerable<TEntity>>(orderBy(query).ToList());
            }
            else
            {
                return Task.FromResult<IEnumerable<TEntity>>(query.ToList());
            }
        }

        /// <summary>
        /// Get an entity using it's primary key
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<TEntity> GetByIDAsync(object id)
        {
            return await dbSet.FindAsync(id);
        }

        /// <summary>
        /// Insert an object into the DB
        /// </summary>
        /// <param name="entity"></param>
        public virtual async Task InsertAsync(TEntity entity)
        {
            await dbSet.AddAsync(entity);

            await SaveAsync();
        }

        /// <summary>
        /// Delete an entity using it's primary key
        /// </summary>
        /// <param name="id"></param>
        public virtual async Task DeleteAsync(object id)
        {
            TEntity entityToDelete = await dbSet.FindAsync(id);
            await DeleteAsync(entityToDelete);
        }

        /// <summary>
        /// Delete a single entity
        /// </summary>
        /// <param name="entityToDelete"></param>
        public virtual async Task DeleteAsync(TEntity entityToDelete)
        {
            if (context.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);

            await SaveAsync();
        }

        /// <summary>
        /// Delete all the passed entities
        /// </summary>
        /// <param name="entitiesToDelete"></param>
        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entitiesToDelete)
        {
            foreach (var entityToDelete in entitiesToDelete)
            {
                if (context.Entry(entityToDelete).State == EntityState.Detached)
                {
                    dbSet.Attach(entityToDelete);
                }
            }
            dbSet.RemoveRange(entitiesToDelete);

            await SaveAsync();
        }

        /// <summary>
        /// Update the specified entity
        /// </summary>
        /// <param name="entityToUpdate"></param>
        public virtual async Task UpdateAsync(TEntity entityToUpdate)
        {
            dbSet.Attach(entityToUpdate);
            context.Entry(entityToUpdate).State = EntityState.Modified;
            await SaveAsync();
        }

        /// <summary>
        /// Save the changes made to the Db
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
