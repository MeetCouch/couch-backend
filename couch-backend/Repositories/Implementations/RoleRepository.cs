using couch_backend.DbContexts;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;

namespace couch_backend.Repositories.Implementations
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(MariaDbContext context) : base(context, context.Roles) { }
    }
}
