using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Hakeem.Api.Utilities;

public static class EnvironmentsChecker
{
    public static bool IsInDevelopmentMode(IWebHostEnvironment webHostEnvironment)
        => webHostEnvironment.IsDevelopment()
           || webHostEnvironment.IsStaging();
}
