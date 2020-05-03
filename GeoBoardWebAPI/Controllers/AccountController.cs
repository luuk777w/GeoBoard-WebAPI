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
using GeoBoardWebAPI.Extensions.Authorization;
using Microsoft.EntityFrameworkCore;
using GeoBoardWebAPI.Extensions.Authorization.Claims;
using System.Web;
using GeoBoardWebAPI.Responses;
using Hangfire;

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

        public AccountController(
            AppUserManager appUserManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            IEmailService emailService,
            ILogger<AccountController> logger,
            UserRepository userRepository,
            IConfiguration config,
            IServiceProvider services,
            IBackgroundJobClient backgroundJobs )
            : base(services)
        {
            _appUserManager = appUserManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _logger = logger;
            _configuration = config;
            _emailService = emailService;
            _userRepository = userRepository;
            _backgroundJobs = backgroundJobs;
        }

        [AllowAnonymous]
        [HttpPost("authorize")]
        public async Task<IActionResult> Authorize([FromBody] LoginViewModel model)
        {
            // Validate request
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            // Search the user by username
            var user = await _appUserManager.FindByEmailAsync(model.Username);
            if (user == null)
            {
                // If the user was not found by email, search by name
                user = await _appUserManager.FindByNameAsync(model.Username);
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
                    var claims = await GetValidClaims(user);

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(_configuration["Tokens:Issuer"],
                        _configuration["Tokens:Issuer"],
                        claims,
                        expires: model.RememberMe ? DateTime.Now.AddDays(14) : DateTime.Now.AddHours(10),
                        signingCredentials: creds);

                    _logger.LogInformation($"User '{user.Email}' ({user.Email}) has logged in.");

                    // Return the JWT token
                    return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
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

            if (await _appUserManager.FindByNameAsync(model.Username) != null)
            {
                ModelState.AddModelError(nameof(model.Username), "Username already in use");

                return BadRequest(ModelState);
            }

            // Create a new user.
            var user = new User
            {
                Email = model.Email,
                CreatedAt = DateTime.UtcNow,
                UserName = model.Username,
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
                Username = user.UserName,
                Token = HttpUtility.UrlEncode(await _appUserManager.GenerateEmailConfirmationTokenAsync(user)),
            };

            await _appUserManager.AddToRoleAsync(await _appUserManager.FindByNameAsync(model.Username), "User");

            _backgroundJobs.Enqueue(() => SendActivateAccountEmail(emailModel));

            return Ok();
        }

        [NonAction]
        public async Task SendActivateAccountEmail(ActivateAccountEmailViewModel emailModel)
        {
            await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["Confirm your account"], emailModel, "Email/ConfirmUserAccount");
        }

        [HttpPost("ResendActivationEmail")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ResendActivationEmail([FromBody] ActivateViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["This account is unknown"]);
            if (user.IsLocked) return BadRequest(_localizer["This account has been locked"]);
            if (user.EmailConfirmed) return BadRequest(_localizer["This account has already been activted"]);

            var emailModel = new ActivateAccountEmailViewModel
            {
                Email = user.Email,
                Token = await _appUserManager.GeneratePasswordResetTokenAsync(user),
                ValidTill = DateTime.Now.AddHours(6)
            };

            await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["Account confirmation"], emailModel, "Account/ConfirmUserAccount");

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
                        _logger.LogInformation($"{user.Email} has confirmed the account.");

                        return Ok(_localizer["AccountConfirmed"]);
                    }

                    return Ok(confirmResult.Errors);
                }

            }
            return Ok("Account does not exist.");
        }

        [AllowAnonymous]
        [HttpPost("RequestPasswordReset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Username);

            if (user == null)
            {
                user = await _appUserManager.FindByNameAsync(model.Username);
            }

            if (user == null) return BadRequest(_localizer["This account is unknown"]);
            if (user.IsLocked) return BadRequest(_localizer["Your account has been locked"]);

            _logger.LogInformation($"{user.Email} has created requested a password reset.");

            var emailModel = new ResetPasswordEmailViewModel
            {
                Email = user.Email,
                Token = HttpUtility.UrlEncode(await _appUserManager.GeneratePasswordResetTokenAsync(user)),
                ReturnUrl = model.ReturnUrl
            };

            await _emailService.SendEmailAsync(new string[] { emailModel.Email }, _localizer["Password reset"], emailModel, "Email/ResetPassword");

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordReturnViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["This account is unknown"]);
            if (user.IsLocked) return BadRequest(_localizer["Your account has been locked"]);

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
                await _emailService.SendEmailAsync(new string[] { user.Email }, _localizer["Password reset succeeded"], user, "Email/SuccessfullPasswordReset");
                return Ok(_localizer["Account password changed"]);
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("RenewToken")]
        public async Task<IActionResult> RenewToken([FromBody] RenewTokenViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = await _appUserManager.FindByEmailAsync(model.Email);
            if (user == null) return BadRequest(_localizer["AccountEmailUnknown"]);
            if (user.IsLocked == false) return BadRequest(_localizer["AccountIsLocked"]);

            if ((model.CreationDate - DateTime.Now).TotalDays > 1)
            {
                if (await _appUserManager.IsEmailConfirmedAsync(user))
                {
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    var token = new JwtSecurityToken(_configuration["Tokens:Issuer"],
                        _configuration["Tokens:Issuer"],
                        claims,
                        expires: DateTime.Now.AddDays(1),
                        signingCredentials: creds);

                    return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                }
            }

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
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var userClaims = await _appUserManager.GetClaimsAsync(user);
            var userRoles = await _appUserManager.GetRolesAsync(user);

            claims.AddRange(userClaims);

            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
                var role = await _roleManager.FindByNameAsync(userRole);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    foreach (Claim roleClaim in roleClaims)
                    {
                        claims.Add(roleClaim);
                    }
                }
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
                ClaimTypes.Email,
                ClaimTypes.NameIdentifier,
                ClaimTypes.Country,
                JwtRegisteredClaimNames.Email,
                JwtRegisteredClaimNames.NameId,
                ClaimTypes.Role,
                AppClaimTypes.Permission,
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

            var user = await _appUserManager.FindByEmailAsync(User.FindFirstValue(ClaimTypes.Email));

            var changePasswordResult = await _appUserManager.ChangePasswordAsync(user, model.CurrentPassword, model.Password);

            if (changePasswordResult.Succeeded)
            {
                return Ok();
            }

            return BadRequest(changePasswordResult.Errors);
        }
    }
}