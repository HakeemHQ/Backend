using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hakeem.Application.Exceptions
{
    public class BadRequestException(string code, params object[] args)
    : LocalizedHttpException(code, StatusCodes.Status400BadRequest, args)
    {
        public string Code { get; init; } = code;
    }
}

