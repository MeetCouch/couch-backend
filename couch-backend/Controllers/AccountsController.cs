using AutoMapper;
using couch_backend.ModelDTOs.Requests;
using couch_backend.ModelDTOs.Responses;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using couch_backend.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace couch_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountsController> _logger;
        private IMapper _mapper { get; }
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;

        public AccountsController(
            IConfiguration configuration,
            ILogger<AccountsController> logger,
            IMapper mapper,
            IRefreshTokenRepository refreshTokenRepository,
            UserManager<User> userManager,
            IUserRepository userRepository)
        {
            _configuration = configuration;
            _logger = logger;
            _mapper = mapper;
            _refreshTokenRepository = refreshTokenRepository;
            _userManager = userManager;
            _userRepository = userRepository;
        }

        /// <summary>Sign up endpoint</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("sign-up")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostApplicationUser([FromBody] UserSignUpDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { "The email has already been registered" }));

            user = _mapper.Map<User>(model);

            try
            {
                await _userManager.CreateAsync(user, model.Password);
                await _userManager.AddToRoleAsync(user, Constants.USER_ROLE);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while registering user");

                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { Constants.DEFAULT_ERROR_MESSAGE }));
            }

            return Ok(await GetJWTToken(user));
        }

        /// <summary>Log in endpoint</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("log-in")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LogIn([FromBody] LoginRequestDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByEmailAsync(model.Email);
            
            if (user == null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "The username or password is incorrect" }));

            var validPassword = await _userManager.CheckPasswordAsync(user, model.Password);

            if (!validPassword)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "Email or Password is incorrect" }));

            return Ok(await GetJWTToken(user));
        }

        /// <summary>Social Log in endpoint</summary
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("log-in/social")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SocialLogIn([FromBody] SocialLoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByLoginAsync(model.Issuer, model.Uid);

            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    user = _mapper.Map<User>(model);

                    try
                    {
                        await _userManager.CreateAsync(user);
                        await _userManager.AddToRoleAsync(user, Constants.USER_ROLE);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while creating user");

                        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                            new string[] { Constants.DEFAULT_ERROR_MESSAGE }));
                    }
                }

                var info = new UserLoginInfo(model.Issuer, model.Uid, model.Issuer);

                try
                {
                    await _userManager.AddLoginAsync(user, info);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while adding social login");
                }
            }

            return Ok(await GetJWTToken(user));
        }

        /// <summary>Refresh token endpoint</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("refresh-token")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
                return NotFound(new ErrorResponseDTO(HttpStatusCode.NotFound,
                    new string[] { "The user was not found" }));

            var token = _refreshTokenRepository
                .GetAsync(x => 
                    x.RefreshTokenId == model.RefreshToken &&
                    x.ExpiryTime > DateTime.UtcNow)
                .Result
                .FirstOrDefault();

            if (token == null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { "RefreshToken is invalid" }));

            await _refreshTokenRepository.DeleteAsync(model.RefreshToken);

            return Ok(await GetJWTToken(user));
        }

        /// <summary>Change Password endpoint</summary>
        /// <remarks>Requires Authorization</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("password/change")]
        [Authorize]
        [ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            if (!model.NewPassword.Equals(model.ConfirmPassword))
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { "NewPassword and ConfirmPassword do not match. " +
                        "Enter the same value for both" }));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var user = await _userManager.FindByIdAsync(currentUserId);

            if (user == null)
                return NotFound(new ErrorResponseDTO(HttpStatusCode.NotFound,
                    new string[] { "The user was not found" }));

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result == null || !result.Succeeded)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "CurrentPassword is incorrect. " +
                        "If forgotten, logout and use the forgot password flow" }));

            return Ok(new DataResponseDTO<string>("Password change successful"));
        }

        /// <summary>Log out endpoint</summary>
        /// <remarks>Requires Authorization</remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("log-out")]
        [Authorize]
        [ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public IActionResult Logout([FromBody] LogoutDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var token = _refreshTokenRepository
                .GetAsync(x =>
                    x.RefreshTokenId == model.RefreshToken &&
                    x.ExpiryTime > DateTime.UtcNow)
                .Result
                .FirstOrDefault();

            return Ok(new DataResponseDTO<string>("Log out successful"));
        }

        ///// <summary>Email or phone number confirmation endpoint</summary>
        ///// <remarks>It confirms a user's email or Phone number
        ///// Accepted data are 'email' and 'phone-number' i.e.
        ///// /api/Accounts/Register/Confirm/email
        ///// /api/Accounts/Register/Confirm/phone-number
        ///// </remarks>
        ///// <response code="200">Success</response>
        ///// <response code="400">Bad Request</response>
        ///// <response code="404">Not Found</response>
        ///// <param name="model"></param>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //[HttpPost("confirm/{data}")]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> ConfirmData([FromBody] ConfirmDataDTO model,
        //                                             [FromRoute] string data)
        //{
        //    Logger.LogError("Accounts Controller ConfirmData method called");

        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new ModelStateErrorResponseDTO(HttpStatusCode.BadRequest,
        //            ModelState));
        //    }

        //    if (data != OtherConstants.EMAIL && data != OtherConstants.PHONE_NUMBER)
        //    {
        //        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //            new List<string> { "Invalid data type. (Must be either email or phone-number" }));
        //    }

        //    if (string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(model.PhoneNumber))
        //    {
        //        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //            new string[] { "You are required to provide either email or phone number" }));
        //    }

        //    ApplicationUser user = null;
        //    if (data == OtherConstants.EMAIL && !string.IsNullOrWhiteSpace(model.Email))
        //    {
        //        user = await UserManager.FindByEmailAsync(model.Email);
        //    }
        //    else if (data == OtherConstants.PHONE_NUMBER && !string.IsNullOrWhiteSpace(model.PhoneNumber))
        //    {
        //        user = await UserManager.FindByNameAsync(model.PhoneNumber);
        //    }

        //    if (user == null || user.ShouldDelete)
        //    {
        //        return NotFound(new ErrorResponseDTO(HttpStatusCode.NotFound,
        //            new string[] { "User not found" }));
        //    }

        //    string successMessage = "";
        //    if (data == OtherConstants.EMAIL)
        //    {
        //        IdentityResult result;
        //        try
        //        {
        //            result = await UserManager.ConfirmEmailAsync(user, model.Token);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogError(ex, "Error while confirming user data");
        //            return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //                new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //        }

        //        if (result != null && result.Succeeded)
        //        {
        //            user.EmailConfirmed = true;
        //            Helper.CalculateProfileScore(user);
        //            await UserManager.UpdateAsync(user);

        //            EmailService.SendEmailConfirmedMessage(user.Email,
        //                $"{user.FirstName} {user.LastName}".Trim());

        //            successMessage = "Email Confirmed Successfully";
        //        }
        //        else
        //        {
        //            return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //                new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //        }
        //    }
        //    else if (data == OtherConstants.PHONE_NUMBER)
        //    {
        //        bool isSuccess;
        //        try
        //        {
        //            isSuccess = await UserManager.VerifyChangePhoneNumberTokenAsync(user, model.Token, model.PhoneNumber);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogError(ex, "Error while confirming user data");
        //            return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //                new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //        }

        //        if (isSuccess)
        //        {
        //            user.PhoneNumberConfirmed = true;
        //            Helper.CalculateProfileScore(user);
        //            await UserManager.UpdateAsync(user);

        //            successMessage = "Phone number Confirmed Successfully";
        //        }
        //        else
        //        {
        //            return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //                new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //        }
        //    }

        //    var newActivity = Mapper.Map<Activity>(model);

        //    if (await UserManager.IsInRoleAsync(user, UserRoleConstants.ADMIN))
        //    {
        //        ActivityService.LogActivity(newActivity,
        //                                    adminUser: user,
        //                                    user: user);
        //    }
        //    else
        //    {
        //        ActivityService.LogActivity(newActivity,
        //                                    user: user);
        //    }

        //    return Ok(new DataResponseDTO<string>(successMessage));
        //}

        ///// <summary>Get new confirmation token endpoint</summary>
        ///// <remarks>It generates a new confrimation token and sends to the user
        ///// Accepted data are "email" and "phone-number" i.e.
        ///// /api/Accounts/Register/Confirm/email/new
        ///// /api/Accounts/Register/Confirm/phone-number/new
        ///// </remarks>
        ///// <response code="200">Success</response>
        ///// <response code="400">Bad Request</response>
        ///// <response code="404">Not Found</response>
        ///// <param name="model"></param>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //[HttpPost("confirm/{data}/new")]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> GetNewConfirmationToken([FromBody] ResendConfirmEmailPhoneRequestDTO model,
        //                                                         [FromRoute] string data)
        //{
        //    Logger.LogError("Accounts Controller ConfirmDataNew method called");

        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new ModelStateErrorResponseDTO(HttpStatusCode.BadRequest,
        //            ModelState));
        //    }

        //    if (data != OtherConstants.EMAIL && data != OtherConstants.PHONE_NUMBER)
        //    {
        //        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //            new List<string> { "Invalid data type. (Must be either email or " +
        //                "phone-number" }));
        //    }

        //    if (string.IsNullOrWhiteSpace(model.Email) && string.IsNullOrWhiteSpace(model.PhoneNumber))
        //    {
        //        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //            new string[] { "You are required to provide either email or " +
        //                "phone number" }));
        //    }

        //    ApplicationUser user = null;
        //    if (data == OtherConstants.EMAIL && !string.IsNullOrWhiteSpace(model.Email))
        //    {
        //        user = await UserManager.FindByEmailAsync(model.Email);
        //    }
        //    else if (data == OtherConstants.PHONE_NUMBER && !string.IsNullOrWhiteSpace(model.PhoneNumber))
        //    {
        //        user = await UserManager.FindByNameAsync(model.PhoneNumber);
        //    }

        //    if (user == null || user.ShouldDelete)
        //    {
        //        return NotFound(new ErrorResponseDTO(HttpStatusCode.NotFound,
        //            new string[] { "User not found" }));
        //    }

        //    string successMessage = "";
        //    if (data == OtherConstants.EMAIL)
        //    {
        //        try
        //        {
        //            var emailToken = await UserManager.GenerateEmailConfirmationTokenAsync(user);

        //            var fullName = $"{user.FirstName} {user.LastName}".Trim();

        //            EmailService.SendNewEmailConfirmationToken(user.Email,
        //                                                       fullName,
        //                                                       model.Platform,
        //                                                       emailToken);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogError(ex, "Error while confirming user data");

        //            return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //                new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //        }

        //        successMessage = "Check your mailbox for a confirmation token";
        //    }
        //    else
        //    {
        //        try
        //        {
        //            var phoneNumberToken = await UserManager.GenerateChangePhoneNumberTokenAsync(user, user.PhoneNumber);
        //            // TODO Send sms with token
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogError(ex, "Error while confirming user data");
        //            return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //                new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //        }

        //        successMessage = "Check your phone for a confirmation token";
        //    }

        //    var newActivity = Mapper.Map<Activity>(model);

        //    if (await UserManager.IsInRoleAsync(user, UserRoleConstants.ADMIN))
        //    {
        //        ActivityService.LogActivity(newActivity,
        //                                    adminUser: user,
        //                                    user: user);
        //    }
        //    else
        //    {
        //        ActivityService.LogActivity(newActivity,
        //                                    user: user);
        //    }

        //    return Ok(new DataResponseDTO<string>(successMessage));
        //}

        ///// <summary>Forgot password endpoint</summary>
        ///// <remarks>Accepts either email or phone number as username</remarks>
        ///// <response code="200">Success</response>
        ///// <response code="400">Bad Request</response>
        ///// <response code="404">Not Found</response>
        ///// <param name="model"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("password/forgot")]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ModelStateErrorResponseDTO), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        //{
        //    Logger.LogError("ForgotPassword method called");

        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new ModelStateErrorResponseDTO(HttpStatusCode.BadRequest,
        //            ModelState));
        //    }

        //    ApplicationUser user;
        //    string successMessage;

        //    if (RegexUtilities.IsValidEmail(model.UserName))
        //    {
        //        user = await UserManager.FindByEmailAsync(model.UserName);
        //        successMessage = "An email has been sent to you with details " +
        //            "of how to reset your password";
        //    }
        //    else
        //    {
        //        user = await UserManager.FindByNameAsync(model.UserName);
        //        successMessage = "An SMS has been sent to you with details " +
        //            "of how to reset your password";
        //    }

        //    if (user == null || user.ShouldDelete)
        //    {
        //        return NotFound(new ErrorResponseDTO(HttpStatusCode.NotFound,
        //            new string[] { "User not found" }));
        //    }

        //    var passwordResetToken = await UserManager.GeneratePasswordResetTokenAsync(user);

        //    if (RegexUtilities.IsValidEmail(model.UserName))
        //    {
        //        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        //        EmailService.SendForgotPasswordEmail(user.Email,
        //                                             fullName,
        //                                             passwordResetToken,
        //                                             model.Platform);
        //    }
        //    else
        //    {
        //        // Send SMS with token
        //    }

        //    var newActivity = Mapper.Map<Activity>(model);

        //    if (await UserManager.IsInRoleAsync(user, UserRoleConstants.ADMIN))
        //    {
        //        ActivityService.LogActivity(newActivity, adminUser: user, user: user);
        //    }
        //    else
        //    {
        //        ActivityService.LogActivity(newActivity,
        //                                    user: user);
        //    }

        //    return Ok(new DataResponseDTO<string>(successMessage));
        //}

        ///// <summary>Reset Password endpoint</summary>
        ///// <remarks>Accepts either email or phone number as username</remarks>
        ///// <response code="200">Success</response>
        ///// <response code="400">Bad Request</response>
        ///// <response code="404">Not Found</response>
        ///// <param name="model"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("password/reset")]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        //{
        //    Logger.LogError("ResetPassword method called");

        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new ModelStateErrorResponseDTO(HttpStatusCode.BadRequest,
        //            ModelState));
        //    }

        //    ApplicationUser user;

        //    if (RegexUtilities.IsValidEmail(model.UserName))
        //    {
        //        user = await UserManager.FindByEmailAsync(model.UserName);
        //    }
        //    else
        //    {
        //        user = await UserManager.FindByNameAsync(model.UserName);
        //    }

        //    if (user == null || user.ShouldDelete)
        //    {
        //        return NotFound(new ErrorResponseDTO(HttpStatusCode.NotFound,
        //            new string[] { "User not found" }));
        //    }

        //    IdentityResult result;
        //    try
        //    {
        //        result = await UserManager.ResetPasswordAsync(user, model.PasswordResetToken, model.Password);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex, "Error while reseting password");

        //        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //            new string[] { OtherConstants.DEFAULT_ERROR_MESSAGE }));
        //    }

        //    if (result == null || !result.Succeeded)
        //    {
        //        return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
        //           new string[] { "Invalid Token" }));
        //    }

        //    EmailService.SendPasswordResetEmail(user.Email, $"{user.FirstName} {user.LastName}".Trim());

        //    if (user.HasTemporaryPassword)
        //    {
        //        UserRepository.FlagPermanentPassword(user);
        //        try
        //        {
        //            UserRepository.Update(user);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.LogError(ex, "Error updating user details");
        //        }
        //    }

        //    var newActivity = Mapper.Map<Activity>(model);

        //    if (await UserManager.IsInRoleAsync(user, UserRoleConstants.ADMIN))
        //    {
        //        ActivityService.LogActivity(newActivity, adminUser: user, user: user);
        //    }
        //    else
        //    {
        //        ActivityService.LogActivity(newActivity,
        //                                    user: user);
        //    }

        //    return Ok(new DataResponseDTO<string>("Your password was reset successfully"));
        //}

        private async Task GenerateUserName(User user)
        {
            int length = 2;
            int count = 1;
            string userName = string.Empty;

            do
            {
                if (count % 600 == 0)
                    length++;

                userName = Helper.GetRandomToken(length);
                count++;
            }
            while (await _userManager.FindByNameAsync(userName) != null);

            _mapper.Map(userName, user);

            await _userManager.UpdateAsync(user);
        }

        private async Task<DataResponseDTO<LoginResponseDTO>> GetJWTToken(User user)
        {
            await GenerateUserName(user);

            var currentTime = DateTime.UtcNow;
            var userRolesTask = _userManager.GetRolesAsync(user);
            var identityOptions = new IdentityOptions();

            var claims = new List<Claim>
            {
                new Claim(identityOptions.ClaimsIdentity.UserIdClaimType, user.Id.ToString()),
                new Claim(identityOptions.ClaimsIdentity.EmailClaimType, user.Email)
            };

            var userRoles = await userRolesTask;

            foreach (var role in userRoles.ToList())
                claims.Add(new Claim(identityOptions.ClaimsIdentity.RoleClaimType, role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims.ToArray()),
                Expires = currentTime.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["JWT_Secret"].ToString())
                    ), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(securityToken);

            var refreshToken = _mapper.Map<RefreshToken>(user);

            do
                refreshToken.RefreshTokenId = Helper.GetRandomToken(96);
            while (await _refreshTokenRepository.GetByIDAsync(refreshToken.RefreshTokenId) != null);

            await _refreshTokenRepository.InsertAsync(refreshToken);

            var loginResponseDTO = _mapper.Map<LoginResponseDTO>(user);
            loginResponseDTO.RefreshToken = refreshToken.RefreshTokenId;
            loginResponseDTO.ExpiryTime = tokenDescriptor.Expires.ToString();
            loginResponseDTO.Token = token;
            loginResponseDTO.Roles = userRoles.ToList();

            return new DataResponseDTO<LoginResponseDTO>(loginResponseDTO);
        } 
    }
}
