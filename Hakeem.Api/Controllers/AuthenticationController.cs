using Hakeem.Application.IdentityDTOs;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Interfaces.Authentications;
using Hakeem.Application.Resources;
using Hakeem.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace Hakeem.Api.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/auth")]
    public class AuthenticationController : ApiControllerBase
    {
        private readonly IAuthentiaction _authenticationService;
        public AuthenticationController(IAuthentiaction authenticationService, ApplicationDbContext _context,IStringLocalizer<SharedResource> localizer)
                   : base(localizer)
        {
            _authenticationService = authenticationService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDTO registerDTO)
        {
            var result = await _authenticationService.RegisterAsync(registerDTO);
            return CreatedResponse(result, "Auth.RegisterSuccess");
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var result = await _authenticationService.LoginAsync(loginDTO);
            return SuccessResponse( result, "Auth.LoginSuccess");
        }

        
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _authenticationService.LogoutAsync(userId);

            return Ok("Logged out successfully.");
        }


        [AllowAnonymous]
        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword(ForgotPasswordDTO passwordDTO)
        {
            var res = await _authenticationService.ForgotPasswordAsync(passwordDTO);
            return CreatedResponse(res, "Password reset link sent successfully");
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordDTO resetPassword)
        {
            var res = await _authenticationService.ResetPasswordAsync(resetPassword);
            return SuccessResponse(res);
        }




    }

    }
    

