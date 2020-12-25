using couch_backend.DbContexts;
using couch_backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;

namespace couch_backend.Repositories.Implementations
{
    public class UserLoginRepository : GenericRepository<IdentityUserLogin<Guid>>, IUserLoginRepository
    {
        public UserLoginRepository(MariaDbContext context) : base(context, context.UserLogins) { }
    }
}
