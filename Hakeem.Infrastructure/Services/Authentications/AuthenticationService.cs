using Hakeem.Application.Exceptions;
using Hakeem.Application.IdentityDTOs;
using Hakeem.Application.Interfaces.Authentications;
using Hakeem.Application.Interfaces.Notifications;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using Hakeem.Infrastructure.Context;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hakeem.Infrastructure.Services.Authentications
{
    public class AuthenticationService(
        UserManager<User> _userManager,
        SignInManager<User> _signInManager,
        IMapper mapper,
        ITokenService tokenService,
        ApplicationDbContext _context,
        IEmailService _emailService,
        IConfiguration configuration,
        IClientAppSettings _clientAppSettings)
        : IAuthentiaction, IScoped
    {
        public async Task<UserDTO> LoginAsync(LoginDTO loginDTO)
        {
            //chcek user exist 
            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user is null) throw new NotFoundException(loginDTO.Email);

            //check pass
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, loginDTO.Password);

            if (isPasswordCorrect)
            {
                //return userdto
                var token = await tokenService.CreateTokenAsync(user);
                var refreshToken = tokenService.GenerateRefreshToken();

                var refreshTokenEntity = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false
                };

                _context.RefreshTokens.Add(refreshTokenEntity);
                await _context.SaveChangesAsync();
                return new UserDTO()
                {
                    FirstName = user.FirstName,
                    Email = user.Email,
                    Token = token,
                };
            }
            else
                throw new UnAuthorizedException("InvalidCredentials");
        }

        public async Task<UserDTO> RegisterAsync(RegisterDTO registerDTO)
        {
            //mapping RegisterDTO -> UserDTO
            var user = new User()
            {
                FirstName = registerDTO.FirstName,
                Email = registerDTO.Email,
                LastName = registerDTO.LastName,
                PhoneNumber = registerDTO.PhoneNumber,
                UserName = registerDTO.Email,
            };
            //create user[applicationuser]
            var res = await _userManager.CreateAsync(user, registerDTO.Password);

            //check if create user successfully
            if (res.Succeeded)
            {
                var token = await tokenService.CreateTokenAsync(user);
                //return userdto
                return new UserDTO()
                {
                    FirstName = user.FirstName,
                    Email = user.Email,
                    Token = token

                };
            }
            else
            {
                var errors = res.Errors.Select(e => e.Description).ToList();
                throw new BadRequestException("UserCredFailed", res.Errors.Select(e => e.Description).ToArray());
            }
        }

        public async Task LogoutAsync(Guid userId)
        {
            var refreshTokens = await _context.RefreshTokens.Where(x => x.UserId == userId && !x.IsRevoked).ToListAsync();
            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
            }
            await _context.SaveChangesAsync();
        }
        public async Task<string> ForgotPasswordAsync(ForgotPasswordDTO forgotPassword)
        {
            var user = await _userManager.FindByEmailAsync(forgotPassword.Email);

            if (user == null) throw new NotFoundException("User not found");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callbackUrl = $"{_clientAppSettings.ClientBaseUrl}" + $"{_clientAppSettings.ResetPasswordPath}" 
               + $"?uid={user.Id}&code={Uri.EscapeDataString(token)}";

            await _emailService.SendEmailAsync( forgotPassword.Email, "Reset Password", callbackUrl, false);

            return token;
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordDTO resetPassword)
        {
            //get user
            var user = await _userManager.FindByEmailAsync(resetPassword.Email);
            //check user exist or not
            if (user is null) throw new NotFoundException("User Not Found");

            var res = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.NewPassword);

            if (!res.Succeeded)
            {
                return string.Join(", ", res.Errors.Select(e => e.Description));
            }

            return ("Password reset successfully.");

        }


    }
}
