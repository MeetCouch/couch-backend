using AutoMapper;
using couch_backend.ModelDTOs.Requests;
using couch_backend.ModelDTOs.Responses;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using couch_backend.Utilities;
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

        /// <summary>
        /// Sign up endpoint
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("register")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostApplicationUser([FromBody]UserRegisterDTO model)
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

        /// <summary>
        /// Log in endpoint
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("login")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            User user;
            try
            {
                user = await _userManager.FindByEmailAsync(model.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while logging user in");

                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new string[] { Constants.DEFAULT_ERROR_MESSAGE }));
            }

            if (user == null)
            {
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "The username or password is incorrect" }));
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!validPassword)
            {
                return BadRequest(new ErrorResponseDTO(HttpStatusCode.BadRequest,
                    new List<string> { "The username or password is incorrect" }));
            }

            return Ok(await GetJWTToken(user));
        }

        private async Task<DataResponseDTO<LoginResponseDTO>> GetJWTToken(User user)
        {
            var currentTime = DateTime.UtcNow;
            var userRolesTask = _userManager.GetRolesAsync(user);

            IdentityOptions identityOptions = new IdentityOptions();
            var claims = new List<Claim>
            {
                new Claim(identityOptions.ClaimsIdentity.UserIdClaimType, user.Id.ToString()),
                new Claim(identityOptions.ClaimsIdentity.UserNameClaimType, user.Name ?? "")
            };

            var userRoles = await userRolesTask;
            foreach (var role in userRoles.ToList())
            {
                claims.Add(new Claim(identityOptions.ClaimsIdentity.RoleClaimType, role));
            }

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
            {
                refreshToken.RefreshTokenId = Helper.GetRandomToken(96);
            }
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
