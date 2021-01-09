using couch_backend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace couch_backend_tests.MockRepositories
{
    abstract class MockGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        public List<TEntity> Context { get; }

        public MockGenericRepository(List<TEntity> context)
        {
            Context = context;
        }

        public virtual Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = Context.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
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

        public virtual TEntity GetOne(Expression<Func<TEntity, bool>> filter)
        {
            return Context.AsQueryable().Where(filter).FirstOrDefault();
        }

        public virtual async Task InsertAsync(TEntity entity)
        {
            Context.Add(entity);
        }

        public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> filter)
        {
            var enquiryToDelete = Context.AsQueryable().Where(filter).FirstOrDefault();
            if (enquiryToDelete != null)
            {
                Context.Remove(enquiryToDelete);
            }
        }

        public virtual async Task DeleteRangeAsync(Expression<Func<TEntity, bool>> filter)
        {
            var enquiriesToDelete = Context.AsQueryable().Where(filter);
            Context.RemoveAll(x => enquiriesToDelete.Contains(x));
        }

        public virtual void Update(Expression<Func<TEntity, bool>> filter, TEntity entityToUpdate)
        {
            var existingEnquiry = Context.AsQueryable().Where(filter).FirstOrDefault();
            if (existingEnquiry != null)
            {
                Context.Remove(existingEnquiry);
                Context.Add(entityToUpdate);
            }
        }

        // first since our TEntity don't have a fixed Id variable
        public virtual async Task<TEntity> GetByIDAsync(object id)
        {
            return Context.FirstOrDefault();
        }

        // abstract since our TEntity don't have a fixed Id variable
        public virtual async Task DeleteAsync(object id)
        {

        }

        public virtual async Task DeleteAsync(TEntity entityToDelete)
        {
            Context.Remove(entityToDelete);
        }

        public async Task DeleteRangeAsync(IEnumerable<TEntity> entitiesToDelete)
        {
            foreach (var entity in entitiesToDelete)
            {
                Context.Remove(entity);
            }
        }

        // abstract since we have to find the entity using Id first
        public virtual async Task UpdateAsync(TEntity entityToUpdate)
        {
        }

        public async Task SaveAsync()
        {
            throw new NotImplementedException();
        }
    }
}
