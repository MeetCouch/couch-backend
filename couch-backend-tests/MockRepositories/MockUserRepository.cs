using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace couch_backend_tests.MockRepositories
{
    class MockUserRepository : MockGenericRepository<User>, IUserRepository
    {
        public MockUserRepository(List<User> users) : base(users) { }

        public override async Task DeleteAsync(object id)
        {
            Context.Remove(Context.Where(x => x.Id == (Guid)id).FirstOrDefault());
        }

        public override async Task<User> GetByIDAsync(object id)
        {
            return Context.Where(x => x.Id == (Guid)id).FirstOrDefault();
        }

        public override async Task UpdateAsync(User entityToUpdate)
        {
            Context.Remove(Context.Where(x => x.Id == entityToUpdate.Id).FirstOrDefault());
            Context.Add(entityToUpdate);
        }
    }
}
