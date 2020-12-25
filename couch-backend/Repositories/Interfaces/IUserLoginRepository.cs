using Microsoft.AspNetCore.Identity;
using System;

namespace couch_backend.Repositories.Interfaces
{
    public interface IUserLoginRepository : IGenericRepository<IdentityUserLogin<Guid>>
    {
    }
}
