using Microsoft.AspNetCore.Identity;
using System;

namespace couch_backend.Models
{
    public class Role : IdentityRole<Guid>
    {
        public Role() : base()
        {
        }

        public Role(string roleName) : base(roleName)
        {
        }
    }
}
