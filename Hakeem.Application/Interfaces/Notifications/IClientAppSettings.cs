using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Interfaces.Notifications
{
    public interface IClientAppSettings
    {
        string ClientBaseUrl { get; }
        public string EmailConfirmationPath { get; }
        public string ResetPasswordPath { get; }
    }
}
