using Hakeem.Application.Interfaces.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Infrastructure.Services.Notifications
{
    public class ClientAppSettings : IClientAppSettings
    {
        public string ClientBaseUrl { get; set; }

        public string EmailConfirmationPath { get; set; }

        public string ResetPasswordPath { get; set; }
    }
}
