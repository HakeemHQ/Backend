using Hakeem.Application.IdentityDTOs;

namespace Hakeem.Application.Interfaces.Authentications
{
    public interface IAuthentiaction
    {
        //Login
        Task<UserDTO> LoginAsync(LoginDTO loginDTO);

        //Register
        Task<UserDTO> RegisterAsync(RegisterDTO RegisterDTO);

        //Logout
        Task LogoutAsync(Guid userId);

        //Forget Password
        Task<string> ForgotPasswordAsync(ForgotPasswordDTO forgotPassword);

        //Reset Pasword
        Task<string> ResetPasswordAsync(ResetPasswordDTO resetPassword);
    }
}
