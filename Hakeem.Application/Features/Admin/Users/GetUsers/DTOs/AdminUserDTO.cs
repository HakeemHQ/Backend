using Hakeem.Domain.Enums.Identity;
using System.Text.Json.Serialization;
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApplicationRole UserType { get; set; } = ApplicationRole.Patient;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AccountStatus Status { get; set; } = AccountStatus.Active;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public IdentityVerificationStatus? IdentityVerificationStatus { get; set; }
    }

    public sealed record AdminUsersResponse(
        IReadOnlyList<AdminUserDto> Items);
}
