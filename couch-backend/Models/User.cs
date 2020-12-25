using Microsoft.AspNetCore.Identity;
using System;

namespace couch_backend.Models
{
    public class User : IdentityUser<Guid>
    {
        public User() : base()
        {
        }

        public User(string userName) : base(userName)
        {
        }

        public string Name { get; set; }
        public DateTime TimeCreated { get; set; } = DateTime.UtcNow;
    }
}
