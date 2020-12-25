using couch_backend.DbContexts;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;

namespace couch_backend.Repositories.Implementations
{
    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(MariaDbContext context) : base(context, context.RefreshTokens) { }
    }
}
