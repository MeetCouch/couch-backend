using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace couch_backend_tests.MockRepositories
{
    class MockRefreshTokenRepository : MockGenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        public MockRefreshTokenRepository(List<RefreshToken> refreshTokens) : base(refreshTokens) { }

        public override async Task DeleteAsync(object id)
        {
            Context.Remove(Context.Where(x => x.RefreshTokenId == (string)id).FirstOrDefault());
        }

        public override async Task<RefreshToken> GetByIDAsync(object id)
        {
            return Context.Where(x => x.RefreshTokenId == (string)id).FirstOrDefault();
        }

        public override async Task UpdateAsync(RefreshToken entityToUpdate)
        {
            Context.Remove(Context.Where(x => x.RefreshTokenId == entityToUpdate.RefreshTokenId).FirstOrDefault());
            Context.Add(entityToUpdate);
        }
    }
}
