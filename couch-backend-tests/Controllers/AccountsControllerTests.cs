using AutoMapper;
using couch_backend.AutoMapper;
using couch_backend.Controllers;
using couch_backend.ModelDTOs.Requests;
using couch_backend.ModelDTOs.Responses;
using couch_backend.Models;
using couch_backend.Utilities;
using couch_backend_tests.MockRepositories;
using couch_backend_tests.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace couch_backend_tests.Controllers
{
    public class AccountsControllerTests
    {
        private readonly AccountsController _controller;

        private readonly MapperConfiguration _mockMapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
        });

        private readonly IMapper _mapper;

        private readonly User _testUser = new User
        {
            Id = Guid.Parse("06f18e58-6d0a-4f70-bbc7-2b6a64cf7a2e"),
            UserName = "mc",
            Email = "user@meetcouch.com",
            EmailConfirmed = true
        };

        private readonly User _testUser2 = new User
        {
            Id = Guid.Parse("12f18e58-6d0a-4f70-bbc7-2b6a64cf7a5f"),
            UserName = "pc",
            Email = "user2@meetcouch.com",
            EmailConfirmed = true
        };

        private readonly RefreshToken _testRefreshToken = new RefreshToken
        {
            RefreshTokenId = "aaabbbcccddd",
            GeneratedTime = DateTime.Now,
            ExpiryTime = DateTime.Now.AddDays(1.0)
        };

        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly List<RefreshToken> _refreshTokens;
        private readonly List<User> _users;
        private ClaimsPrincipal _testUserClaims;
        private ClaimsPrincipal _testUser2Claims;

        public AccountsControllerTests()
        {
            _users = new List<User> { _testUser, _testUser2 };
            var mockUserRepository = new MockUserRepository(_users);
            _mockUserManager = IdentityMocks.MockUserManager(_users);
            var testUserTask = _mockUserManager.Object.AddToRoleAsync(
                _testUser, Constants.USER_ROLE);
            var testUser2Task = _mockUserManager.Object.AddToRoleAsync(
                _testUser2, Constants.USER_ROLE);

            _testRefreshToken.User = _testUser;
            _mapper = _mockMapper.CreateMapper();

            var mockConfiguration = new Mock<IConfiguration>();
            var mockLogger = new Mock<ILogger<AccountsController>>();

            _refreshTokens = new List<RefreshToken> { _testRefreshToken };
            var mockRefreshTokenRepository = new MockRefreshTokenRepository(_refreshTokens);

            _controller = new AccountsController(
                mockConfiguration.Object,
                mockLogger.Object,
                _mapper,
                mockRefreshTokenRepository,
                _mockUserManager.Object,
                mockUserRepository);

            _testUserClaims = IdentityMocks.GetHttpContextUser(_testUser.Email,
                _testUser.Id.ToString(), new string[] { Constants.USER_ROLE });
            _testUser2Claims = IdentityMocks.GetHttpContextUser(_testUser2.Email,
                _testUser2.Id.ToString(), new string[] { Constants.USER_ROLE });
        }

        [Fact]
        public async void PostApplicationUser_NoEmail_ReturnsBadRequest()
        {
            // Arrange
            var userSignUpDTO = new UserSignUpDTO
            {
                Password = "12345"
            };

            _controller.ModelState.AddModelError("Email", 
                Constants.EMAIL_REQUIRED_ERROR_MESSAGE);

            // Act
            var result = _controller.PostApplicationUser(userSignUpDTO);

            // Assert
            var badRequestResult = await result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);

            var model = badRequestResult.Value as ModelStateErrorResponseDTO;
            Assert.NotNull(model);
            Assert.Equal(expected: HttpStatusCode.BadRequest, actual: model.Status);
            Assert.Contains(expected: Constants.EMAIL_REQUIRED_ERROR_MESSAGE, 
                collection: model.ErrorMessages);
        }

        [Fact]
        public async void PostApplicationUser_InvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var userSignUpDTO = new UserSignUpDTO
            {
                Email = "invalidEmail",
                Password = "12345"
            };

            _controller.ModelState.AddModelError(
                "Email", Constants.INVALID_EMAIL_REQUIRED_ERROR_MESSAGE);

            // Act
            var result = _controller.PostApplicationUser(userSignUpDTO);

            // Assert
            var badRequestResult = await result as BadRequestObjectResult;
            Assert.Null(badRequestResult);

            var model = badRequestResult.Value as ModelStateErrorResponseDTO;
            Assert.NotNull(model);
            Assert.Equal(expected: HttpStatusCode.BadRequest, actual: model.Status);
            Assert.Contains(expected: Constants.INVALID_EMAIL_REQUIRED_ERROR_MESSAGE,
                collection: model.ErrorMessages);
        }

        [Fact]
        public async void PostApplicationUser_NoPassword_ReturnsBadRequest()
        {
            // Arrange
            var userSignUpDTO = new UserSignUpDTO
            {
                Email = Constants.SAMPLE_EMAIL,
                Password = string.Empty
            };

            _controller.ModelState.AddModelError("Password", 
                Constants.PASSWORD_REQUIRED_ERROR_MESSAGE);

            // Act
            var result = _controller.PostApplicationUser(userSignUpDTO);

            // Assert
            var badRequestResult = await result as BadRequestObjectResult;
            Assert.NotNull(badRequestResult);

            var model = badRequestResult.Value as ModelStateErrorResponseDTO;
            Assert.NotNull(model);
            Assert.Equal(expected: HttpStatusCode.BadRequest, actual: model.Status);
            Assert.Contains(expected: Constants.PASSWORD_REQUIRED_ERROR_MESSAGE,
                collection: model.ErrorMessages);
        }
    }
}
