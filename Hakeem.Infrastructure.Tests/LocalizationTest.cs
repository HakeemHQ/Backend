using System.Globalization;
using Hakeem.Application.Constants;
using Hakeem.Application.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Xunit;
using Xunit.Abstractions;

namespace Hakeem.Infrastructure.Tests;

public class LocalizationTest
{
    private readonly ITestOutputHelper _output;

    public LocalizationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_Arabic_Localization()
    {
        var rm = SharedResource.ResourceManager;
        _output.WriteLine($"RM BaseName: {rm.BaseName}");

        var cultures = new[] { "en", "ar", "ar-EG", "ar-SA" };
        foreach (var c in cultures)
        {
            var ci = new CultureInfo(c);
            var str = rm.GetString(ErrorCodes.AuthInvalidCredentials, ci);
            _output.WriteLine($"[ResourceManager] Culture '{c}': '{str}'");
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        var sp = services.BuildServiceProvider();

        var localizer = sp.GetRequiredService<IStringLocalizer<SharedResource>>();
        var factory = sp.GetRequiredService<IStringLocalizerFactory>();
        var createdLocalizer = factory.Create("Hakeem.Application.Resources.SharedResource", "Hakeem.Application");

        foreach (var c in cultures)
        {
            var ci = new CultureInfo(c);
            CultureInfo.CurrentCulture = ci;
            CultureInfo.CurrentUICulture = ci;

            var valTyped = localizer[ErrorCodes.AuthInvalidCredentials].Value;
            var valCreated = createdLocalizer[ErrorCodes.AuthInvalidCredentials].Value;

            _output.WriteLine($"[IStringLocalizer<SharedResource>] Culture '{c}': '{valTyped}'");
            _output.WriteLine($"[factory.Create] Culture '{c}': '{valCreated}'");

            if (c.StartsWith("ar"))
            {
                Assert.Equal("البريد الإلكتروني أو كلمة المرور غير صحيحة.", valTyped);
                Assert.Equal("البريد الإلكتروني أو كلمة المرور غير صحيحة.", valCreated);
            }
        }
    }
}
