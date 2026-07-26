using System.ComponentModel.DataAnnotations;

namespace Hakeem.Application.IdentityDTOs
{
    public class LoginDTO
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }


    }
}
