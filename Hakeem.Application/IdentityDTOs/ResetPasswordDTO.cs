namespace Hakeem.Application.IdentityDTOs
{
    public class ResetPasswordDTO
    {
        public string Email  { get; set; }
        public string Token  { get; set; }
        public string NewPassword  { get; set; }

    }
}
