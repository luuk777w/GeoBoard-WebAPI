using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GeoBoardWebAPI.Services;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using GeoBoardWebAPI.Models.Account;
using GeoBoardWebAPI.Util.Authorization;
using Microsoft.EntityFrameworkCore;
using GeoBoardWebAPI.Util.Authorization.Claims;
using System.Web;
using GeoBoardWebAPI.Responses;
using Hangfire;
using Microsoft.AspNetCore.Http;
using GeoBoardWebAPI.DAL;

namespace GeoBoardWebAPI.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly AppUserManager _appUserManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IEmailService _emailService;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly UserRepository _userRepository;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public AccountController(
            AppUserManager appUserManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger,
            ApplicationDbContext applicationDbContext,
            UserRepository userRepository,
            TokenValidationParameters tokenValidationParameters,
            IConfiguration config,
            IServiceProvider services,
            IBackgroundJobClient backgroundJobs )
            : base(services)
        {
            _appUserManager = appUserManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _configuration = config;
            _tokenValidationParameters = tokenValidationParameters;
            _emailService = emailService;
            _userRepository = userRepository;
            _backgroundJobs = backgroundJobs;
        }

        /// <summary>
        /// Authorize yourself to the system.
        /// </summary>
        /// <param name="model">The request model containing the request data.</param>
        [AllowAnonymous]
        [HttpPost("Authorize")]
        public async Task<IActionResult> Authorize([FromBody] LoginViewModel model)
        {
            // Validate request
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            // Search the user by username
            var user = await _appUserManager.FindByEmailAsync(model.UserName);
            if (user == null)
            {
                // If the user was not found by email, search by name
                user = await _appUserManager.FindByNameAsync(model.UserName);
            }

            // No user found.
            if (user == null)
                return BadRequest(_localizer["This account is unknown"]);

            // Check email confirmation
            if (user != null && await _appUserManager.IsEmailConfirmedAsync(user))
            {
                // Try to log the user in.
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
                if (result.Succeeded)
                {
                    return Ok(await GenerateAuthenticationResult(user, model.RememberMe));
                }

                // Determine the error message based on what went wrong
                if (result.IsLockedOut)
                {
                    return BadRequest(_localizer["Your account has been locked"]);
                }
                if (result.IsNotAllowed)
                {
                    return BadRequest(_localizer["Your account has been blocked"]);
                }
                else
                {
                    return BadRequest(_localizer["The email and/or password are incorrect"]);
                }
            }

            // Account not yet activated.
            return BadRequest(_localizer["Your account has not yet been activated"]);
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            // Validate request
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if the user is unique
            if (await _appUserManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email already in use.");

                return BadRequest(ModelState);
            }

            if (await _appUserManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError(nameof(model.UserName), "UserName already in use");

                return BadRequest(ModelState);
            }

            // Create a new user.
            var user = new User
            {
                Email = model.Email,
                CreatedAt = DateTime.UtcNow,
                UserName = model.UserName,
                EmailConfirmed = false
            };

            var result = await _appUserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) 
                return BadRequest(result.Errors, ModelState);

            _logger.LogInformation($"{user.Email} has created an account.");


            //sendEmail
            var emailModel = new ActivateAccountEmailViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                Token = HttpUtility.UrlEncode(await _appUserManager.GenerateEmailConfirmationTokenAsync(user)),
                ValidTill = DateTime.Now.AddHours(6)
            };

            await _appUserManager.AddToRoleAsync(await _appUserManager.FindByNameAsync(model.UserName), "User");

            _backgroundJobs.Enqueue(() => SendActivateAccountEmail(emailModel));

            return Ok();
        }

        [NonAction]
        public async Task SendActivateAccountEmail(ActivateAccountEmailViewModel emailModel)
        {
            await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["Confirm your account"], emailModel, "Email/ConfirmUserAccount");
        }

        [AllowAnonymous]
        [HttpPost("resend-activation-email")]
        public async Task<IActionResult> ResendActivationEmail([FromBody] ActivateViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["This account is unknown"]);
            if (user.IsLocked) return BadRequest(_localizer["This account has been locked"]);
            if (user.EmailConfirmed) return BadRequest(_localizer["This account has already been activated"]);

            var emailModel = new ActivateAccountEmailViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                Token = HttpUtility.UrlEncode(await _appUserManager.GenerateEmailConfirmationTokenAsync(user)),
                ValidTill = DateTime.Now.AddHours(6)
            };

            _backgroundJobs.Enqueue(() => SendActivateAccountEmail(emailModel));

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("Activate")]
        public async Task<IActionResult> Activate([FromQuery] ActivateReturnViewModel model)
        {
            if (!ModelState.IsValid) return Ok(ModelState);

            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                if (!await _appUserManager.IsEmailConfirmedAsync(user))
                {
                    await _appUserManager.SetLockoutEnabledAsync(user, false);
                    var confirmResult = await _appUserManager.ConfirmEmailAsync(user, model.Token);
                    if (confirmResult.Succeeded)
                    {
                        return Ok(_localizer["AccountConfirmed"]);
                    }

                    return BadRequest(confirmResult.Errors);
                }

            }
            return Ok("Account does not exist.");
        }

        [AllowAnonymous]
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Email);

            // Return OK when the user is not found or locked to disguise a non-existance user.
            if (user == null) return Ok();
            if (user.IsLocked) return Ok();

            _logger.LogInformation($"{user.Email} has requested a password reset.");

            var emailModel = new ResetPasswordEmailViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                Token = HttpUtility.UrlEncode(await _appUserManager.GeneratePasswordResetTokenAsync(user))
            };

            _backgroundJobs.Enqueue(() => SendPasswordResetInstructionsEmail(emailModel));

            return Ok();
        }

        [NonAction]
        public async Task SendPasswordResetInstructionsEmail(ResetPasswordEmailViewModel emailModel)
        {
            await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["Password reset instructions"], emailModel, "Email/ResetPassword");
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _appUserManager.FindByEmailAsync(model.Email);
            // Return OK when the user is not found or locked to disguise a non-existance user.
            if (user == null) return Ok();
            if (user.IsLocked) return Ok();

            if (!user.EmailConfirmed)
            {
                var res = await _appUserManager.ConfirmEmailAsync(user, await _appUserManager.GenerateEmailConfirmationTokenAsync(user));
                if (!res.Succeeded)
                {
                    return BadRequest(res.Errors);
                }
            }

            var result = await _appUserManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation($"{user.Email} has reset the password.");

                _backgroundJobs.Enqueue(() => SendPasswordHasBeenResetEmail(user));

                return Ok(_localizer["Account password changed"]);
            }

            return BadRequest(result.Errors);
        }

        [NonAction]
        public async Task SendPasswordHasBeenResetEmail(User user)
        {
            await _emailService.SendEmailAsync(new string[] { user.Email }, _localizer["Your password has been reset succesfully"], user, "Email/SuccessfulPasswordReset");
        }

        [AllowAnonymous]
        [HttpPost("Refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenViewModel model)
        {
            var validatedToken = GetPrincipalFromToken(model.AccessToken);
            if (validatedToken == null)
            {
                return BadRequest("Invalid token");
            }

            var expiryDateUnix = long.Parse(validatedToken.Claims.Single(t => t.Type.Equals(JwtRegisteredClaimNames.Exp)).Value);

            DateTime expiryDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
                .AddSeconds(expiryDateUnix)
                .ToLocalTime();

            if (expiryDateTime > DateTime.Now)
            {
                return BadRequest("This token hasn't been expired yet");
            }

            var jti = validatedToken.Claims.Single(t => t.Type == JwtRegisteredClaimNames.Jti).Value;

            var storedRefreshToken = await _applicationDbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token.Equals(model.RefreshToken));

            if (storedRefreshToken == null)
            {
                _logger.LogWarning($"The refresh token ({storedRefreshToken?.Token}) of user '{GetUserId()}' does not exist.");

                return BadRequest("Something went wrong while refreshing the access token.");
            }

            if (DateTime.Now > storedRefreshToken.ExpiryDate)
            {
                return BadRequest("This refresh token has expired");
            }

            if (! storedRefreshToken.JwtId.Equals(jti))
            {
                _logger.LogWarning($"The refresh token ({storedRefreshToken.Token}) of user '{GetUserId()}' does not match this JWT ({jti}).");

                return BadRequest("Something went wrong while refreshing the access token.");
            }

            _applicationDbContext.RefreshTokens.Remove(storedRefreshToken);
            await _applicationDbContext.SaveChangesAsync();

            var user = await _appUserManager.FindByIdAsync(validatedToken.Claims.Single(t => t.Type.Equals(JwtRegisteredClaimNames.NameId)).Value);

            if (user == null)
            {
                _logger.LogWarning($"User '{validatedToken.Claims.Single(t => t.Type.Equals(JwtRegisteredClaimNames.NameId)).Value}' was not found while refreshing the access token.");

                return BadRequest("Something went wrong while refreshing the access token.");
            }

            double rememberMeLifetime = double.Parse(_configuration["JwtSettings:RememberMeRefreshTokenLifetime"].ToString());
            bool rememberMe = (storedRefreshToken.ExpiryDate.Subtract(storedRefreshToken.CreatedAt)).TotalSeconds.Equals(rememberMeLifetime);

            return Ok(
                await GenerateAuthenticationResult(user, rememberMe)
            );
        }

        [NonAction]
        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var tokenValidationParameters = _tokenValidationParameters.Clone();
                tokenValidationParameters.ValidateLifetime = false;

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

                if (!IsJwtWithValidateSecurityAlgorithm(validatedToken))
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }

        [NonAction]
        private bool IsJwtWithValidateSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
        }

        [NonAction]
        private async Task<AuthenticationResultViewModel> GenerateAuthenticationResult(User user, bool rememberMe = false)
        {
            var claims = await GetValidClaims(user);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create a new JWT Access Token.
            var token = new JwtSecurityToken(_configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Issuer"],
                claims,
                expires: DateTime.Now.Add(TimeSpan.Parse(_configuration["JwtSettings:TokenLifetime"].ToString())),
                signingCredentials: creds);

            // Determine the lifetime depending on the rememberMe value.
            double lifeTime = (rememberMe) ? double.Parse(_configuration["JwtSettings:RememberMeRefreshTokenLifetime"].ToString()) : double.Parse(_configuration["JwtSettings:RefreshTokenLifetime"].ToString());

            // Store the current time to calculate the expiry date without extra seconds 
            // caused by re-evaluating the current time and getting extra milliseconds.
            var today = DateTime.Now;

            // Create and store a new refresh token.
            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                UserId = user.Id,
                CreatedAt = today,
                ExpiryDate = today.AddSeconds(lifeTime)
            };

            await _applicationDbContext.RefreshTokens.AddAsync(refreshToken);
            await _applicationDbContext.SaveChangesAsync();

            // Return fresh tokens.
            return new AuthenticationResultViewModel
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken.Token
            };
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutViewModel model)
        {
            var storedRefreshToken = await _applicationDbContext.RefreshTokens.SingleOrDefaultAsync(rt => rt.UserId.Equals(GetUserId()) && rt.JwtId.Equals(model.JwtId));

            if (storedRefreshToken == null) {
                return BadRequest("Something went wrong while logging out.");
            }

            _applicationDbContext.RefreshTokens.Remove(storedRefreshToken);
            await _applicationDbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("Lockout")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Lockout([FromQuery] LockoutViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(model);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["This account is unknown"]);

            if (user.Id == GetUserId()) return BadRequest(_localizer["You cannot lock yourself out"]);

            await _appUserManager.SetLockoutEnabledAsync(user, true);

            return Ok();
        }

        [HttpPost("Remove")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Remove([FromBody] RemoveViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(model);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["This account is unknown"]);
            if (user.Id == GetUserId()) return BadRequest(_localizer["You cannot lock yourself out"]);
            try
            {
                await _appUserManager.DeleteAsync(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return Ok();
        }

        private async Task<List<Claim>> GetValidClaims(User user)
        {
            IdentityOptions _options = new IdentityOptions();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            //var userClaims = await _appUserManager.GetClaimsAsync(user);
            var userRoles = await _appUserManager.GetRolesAsync(user);

            //claims.AddRange(userClaims);

            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
                //var role = await _roleManager.FindByNameAsync(userRole);
                //if (role != null)
                //{
                //    var roleClaims = await _roleManager.GetClaimsAsync(role);
                //    foreach (Claim roleClaim in roleClaims)
                //    {
                //        claims.Add(roleClaim);
                //    }
                //}
            }

            return claims;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAccount()
        {
            var allowedClaimTypes = new List<string>()
            {
                ClaimTypes.Name,
                ClaimTypes.Country,
                JwtRegisteredClaimNames.Email,
                JwtRegisteredClaimNames.NameId,
                ClaimTypes.Role,
                //AppClaimTypes.Permission,
            };

            var user = await _userRepository.GetAll()
                .Include(x => x.Person)
                .ThenInclude(x => x.Country)
                .Include(x => x.Settings)
                .ThenInclude(x => x.Language)
                .SingleOrDefaultAsync(m => m.Id == GetUserId());

            var model = new ProfileAccountViewModel
            {
                User = _mapper.Map<UserViewModel>(user),
                Claims = _mapper.Map<List<ClaimViewModel>>(User.Claims.Where(x => allowedClaimTypes.Contains(x.Type)))
            };

            return Ok(model);
        }

        [HttpGet("roles")]
        [Authorize]
        public IActionResult GetRoles()
        {
            return base.Ok(DAL.Entities.User.Roles);
        }

        [HttpPost("Invite")]
        [Authorize]
        public async Task<IActionResult> Invite([FromBody] InviteViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);
                if (await _appUserManager.FindByEmailAsync(model.Email) != null) return BadRequest(_localizer["AccountEmailInUse"]);

                var user = new User();
                user.Email = model.Email;
                user.CreatedAt = DateTime.UtcNow;
                user.UserName = model.Email;
                user.EmailConfirmed = false;
                user.Person.FirstName = model.Firstname;
                user.Person.LastName = model.Lastname;
                var result = await _appUserManager.CreateAsync(user);
                if (!result.Succeeded) return BadRequest(result.Errors);

                await _appUserManager.AddToRoleAsync(user, "Employee");

                var emailModel = new ActivateAccountEmailViewModel
                {
                    Email = user.Email,
                    Token = await _appUserManager.GeneratePasswordResetTokenAsync(user),
                    ValidTill = DateTime.Now.AddHours(6)
                };
                await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["You have been invited"], emailModel, "Account/InviteUser");
            }
            catch (Exception ex)
            {
                var user = await _appUserManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!await _appUserManager.IsEmailConfirmedAsync(user))
                    {
                        await _appUserManager.DeleteAsync(user);
                    }
                }
                throw ex;
            }

            return Ok();
        }

        [HttpPut("Invite")]
        [AllowAnonymous]
        public async Task<IActionResult> Invite([FromBody] InviteActivateViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["This account is unknown"]);

            var emailConnfirmationToken = await _appUserManager.GenerateEmailConfirmationTokenAsync(user);
            var emailConfirmationResult = await _appUserManager.ConfirmEmailAsync(user, emailConnfirmationToken);

            if (!emailConfirmationResult.Succeeded) { return BadRequest(emailConfirmationResult); }

            var pwValidationResult = await _appUserManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (!pwValidationResult.Succeeded)
            {
                return BadRequest(pwValidationResult);
            }

            return NoContent();
        }

        [HttpPut]
        [Route("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordUserMutateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _appUserManager.FindByEmailAsync(User.FindFirstValue(JwtRegisteredClaimNames.Email));

            var changePasswordResult = await _appUserManager.ChangePasswordAsync(user, model.CurrentPassword, model.Password);

            if (changePasswordResult.Succeeded)
            {
                return Ok();
            }

            return BadRequest(changePasswordResult.Errors);
        }
    }
}