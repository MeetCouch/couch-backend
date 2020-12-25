using System;

namespace couch_backend.Models
{
    public class RefreshToken
    {
        public string RefreshTokenId { get; set; }
        public DateTime ExpiryTime { get; set; }
        public DateTime GeneratedTime { get; set; }

        public Guid UserId { get; set; }
        public virtual User User { get; set; }
    }
}
