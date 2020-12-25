using couch_backend.Models;
using couch_backend.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace couch_backend.DbInitializers
{
    public class DbInitializer
    {
        public static void SeedDatabase(
            IConfiguration configuration,
            ILogger<DbInitializer> logger,
            RoleManager<Role> roleManager,
            UserManager<User> userManager)
        {
            SeedRolesAndUsers(configuration, logger, roleManager, userManager);
        }

        private static void SeedRolesAndUsers(
            IConfiguration configuration,
            ILogger<DbInitializer> logger,
            RoleManager<Role> roleManager,
            UserManager<User> userManager)
        {
            if (roleManager.FindByNameAsync(Constants.ADMIN_ROLE).Result == null)
                roleManager.CreateAsync(new Role(Constants.ADMIN_ROLE)).Wait();

            if (roleManager.FindByNameAsync(Constants.SUPER_ADMIN_ROLE).Result == null)
                roleManager.CreateAsync(new Role(Constants.SUPER_ADMIN_ROLE)).Wait();

            if (roleManager.FindByNameAsync(Constants.USER_ROLE).Result == null)
                roleManager.CreateAsync(new Role(Constants.USER_ROLE)).Wait();

            string superAdminEmail = configuration["DefaultSuperAdminEmail"];
            string superAdminPassword = configuration["DefaultSuperAdminPassword"];

            // Setup super admin user only if it is setup in appsettings
            if (!string.IsNullOrEmpty(superAdminEmail) && !string.IsNullOrEmpty(superAdminPassword))
            {
                if (userManager.FindByEmailAsync(superAdminEmail).Result == null)
                {
                    User superAdmin = new User
                    {
                        UserName = superAdminEmail,
                        NormalizedUserName = superAdminEmail.ToUpper(),
                        Email = superAdminEmail,
                        NormalizedEmail = superAdminEmail.ToUpper(),
                        EmailConfirmed = true
                    };

                    try
                    {
                        var result = userManager.CreateAsync(superAdmin, superAdminPassword).Result;

                        if (result.Succeeded)
                        {
                            userManager.AddToRoleAsync(superAdmin, Constants.SUPER_ADMIN_ROLE).Wait();
                            userManager.AddToRoleAsync(superAdmin, Constants.ADMIN_ROLE).Wait();
                            userManager.AddToRoleAsync(superAdmin, Constants.USER_ROLE).Wait();

                            logger.LogDebug("CREATED THE SUPER ADMIN");
                        }
                        else
                        {
                            logger.LogError("There was an error creating the super admin user");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating the super admin user");
                    }
                }
            }
        }
    }
}
