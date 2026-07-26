using System.ComponentModel.DataAnnotations;

namespace Hakeem.Application.IdentityDTOs
{
    public class RegisterDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }    
        [Phone]
        public string PhoneNumber { get; set; }
    }
}
