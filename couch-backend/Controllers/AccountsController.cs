﻿using AutoMapper;
using couch_backend.ModelDTOs.Requests;
using couch_backend.ModelDTOs.Responses;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using couch_backend.Services.Interfaces;
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
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountsController> _logger;
        private IMapper _mapper { get; }
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;

        public AccountsController(
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<AccountsController> logger,
            IMapper mapper,
            IRefreshTokenRepository refreshTokenRepository,
            UserManager<User> userManager,
            IUserRepository userRepository)
        {
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _mapper = mapper;
            _refreshTokenRepository = refreshTokenRepository;
            _userManager = userManager;
            _userRepository = userRepository;
        }

        /// <summary>Sign up endpoint</summary>
        /// <param name="model"></param>
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

            try
            {
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                _emailService.SendSuccessfulRegistrationMessage(
                    user.Email,
                    emailToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending successful registration email");
            }

            return Ok(await GetJWTToken(user));
        }

        /// <summary>Log in endpoint</summary>
        /// <param name="model"></param>
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
                    new List<string> { "The email or password is incorrect" }));

            var validPassword = await _userManager.CheckPasswordAsync(
                user, model.Password);

            if (!validPassword)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "The email or password is incorrect" }));

            return Ok(await GetJWTToken(user));
        }

        /// <summary>Social Log in endpoint</summary
        /// <param name="model"></param>
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

        /// <summary>Email confirmation endpoint</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("confirm/email")]
        [ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "Email not found" }));

            IdentityResult result;
            try
            {
                result = await _userManager.ConfirmEmailAsync(user, model.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while confirming user email");

                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { Constants.DEFAULT_ERROR_MESSAGE }));
            }

            if (result != null && result.Succeeded)
            {
                user.EmailConfirmed = true;

                await _userManager.UpdateAsync(user);

                //EmailService.SendEmailConfirmedMessage(user.Email);
            }
            else
            {
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { Constants.DEFAULT_ERROR_MESSAGE }));
            }

            return Ok(new DataResponseDTO<string>("Your email was confirmed successfully"));
        }

        /// <summary>Get new email confirmation token endpoint</summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("confirm/email/token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetNewConfirmationToken(
            [FromBody] ResendConfirmEmailRequestDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "Email not found" }));

            string emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            //EmailService.SendNewEmailConfirmationToken(user.Email, emailToken);

            return Ok(new DataResponseDTO<string>("A new confirmation token has " +
                "been sent to your mail"));
        }

        /// <summary>Forgot password endpoint</summary>
        /// <param name="model"></param>
        [HttpPost]
        [Route("password/forgot")]
        [ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModelStateErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "Email not found" }));

            var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            //EmailService.SendForgotPasswordEmail(user.Email, passwordResetToken);

            return Ok(new DataResponseDTO<string>("An email has been sent to you " +
                "with details of how to reset your password"));
        }

        /// <summary>Reset Password endpoint</summary>
        /// <param name="model"></param>
        [HttpPut]
        [Route("password/reset")]
        [ProducesResponseType(typeof(DataResponseDTO<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "Email not found" }));

            IdentityResult result;
            try
            {
                result = await _userManager.ResetPasswordAsync(
                    user, model.PasswordResetToken, model.Password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reseting password");

                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { Constants.DEFAULT_ERROR_MESSAGE }));
            }

            if (result == null || !result.Succeeded)
            {
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                   new string[] { "Invalid Token" }));
            }

            //EmailService.SendPasswordResetEmail(user.Email);

            return Ok(new DataResponseDTO<string>("Your password was reset successfully"));
        }

        /// <summary>Change Password endpoint</summary>
        /// <param name="model"></param>
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

            var result = await _userManager.ChangePasswordAsync(
                user, model.CurrentPassword, model.NewPassword);

            if (result == null || !result.Succeeded)
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "CurrentPassword is incorrect. " +
                        "If forgotten, logout and use the forgot password flow" }));

            return Ok(new DataResponseDTO<string>("Your password was changed successfully"));
        }

        /// <summary>Refresh token endpoint</summary>
        /// <param name="model"></param>
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

        /// <summary>Log out endpoint</summary>
        /// <param name="model"></param>
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
