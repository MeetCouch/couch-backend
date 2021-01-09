using couch_backend.DbContexts;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using couch_backend.Utilities;

namespace couch_backend.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(MariaDbContext context) : base(context, context.Users) { }
    }
}
