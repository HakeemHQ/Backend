using Hakeem.Domain.Enums.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Features.Admin.Users.GetUsers.DTOs
{
    public class AdminUserDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public ApplicationRole UserType { get; set; } = ApplicationRole.Patient;
        public AccountStatus Status { get; set; } = AccountStatus.Active;
    }
}
