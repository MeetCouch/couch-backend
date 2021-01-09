using couch_backend.Models;
using couch_backend.Utilities;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace couch_backend_tests.Utilities
{
    class IdentityMocks
    {
        public static Mock<UserManager<User>> MockUserManager(List<User> users)
        {
            var store = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);
            mockUserManager.Object.PasswordHasher = new PasswordHasher<User>();
            mockUserManager.Object.UserValidators.Add(new UserValidator<User>());
            mockUserManager.Object.UserValidators.Add(new UserValidator<User>());
            mockUserManager.Object.PasswordValidators.Add(new PasswordValidator<User>());

            mockUserManager.Setup(x => x.DeleteAsync(It.IsAny<User>())).ReturnsAsync(
                IdentityResult.Success);
            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success).Callback<User, string>((x, y) => users.Add(x));
            mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            foreach (var user in users)
            {
                mockUserManager.Setup(x => x.FindByEmailAsync(user.Email))
                    .Returns(Task.FromResult<User>(user));
                mockUserManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                    .Returns(Task.FromResult<User>(user));
                mockUserManager.Setup(x => x.FindByNameAsync(user.UserName))
                    .Returns(Task.FromResult<User>(user));

                if (user.Email.Contains("user", StringComparison.InvariantCultureIgnoreCase))
                {
                    mockUserManager.Setup(x => x.IsInRoleAsync(user, Constants.USER_ROLE))
                        .Returns(Task.FromResult<bool>(true));
                }
                else if (user.Email.Contains("admin", StringComparison.InvariantCultureIgnoreCase))
                {
                    mockUserManager.Setup(x => x.IsInRoleAsync(user, Constants.ADMIN_ROLE))
                        .Returns(Task.FromResult<bool>(true));
                }
            }

            return mockUserManager;
        }

        public static ClaimsPrincipal GetHttpContextUser(string email, string id, string[] roles)
        {
            var identityOptions = new IdentityOptions();

            var claims = new List<Claim>
            {
                new Claim(identityOptions.ClaimsIdentity.UserNameClaimType, email),
                new Claim(identityOptions.ClaimsIdentity.UserIdClaimType, id)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(identityOptions.ClaimsIdentity.RoleClaimType, role));
            }

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims.ToArray(), "mock"));
            return user;
        }
    }
}
