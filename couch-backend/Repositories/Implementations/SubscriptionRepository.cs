using couch_backend.DbContexts;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;

namespace couch_backend.Repositories.Implementations
{
    public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(MariaDbContext context) : base(context, context.Subscriptions) { }
    }
}
