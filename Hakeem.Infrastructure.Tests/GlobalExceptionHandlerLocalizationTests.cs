using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hakeem.Api.Handlers;
using Hakeem.Application.Common.ResponseModel;
using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Resources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Xunit.Abstractions;

namespace Hakeem.Infrastructure.Tests;

public class DummyHostingEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "Hakeem.Api";
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

public class GlobalExceptionHandlerLocalizationTests
{
    private readonly ITestOutputHelper _output;

    public GlobalExceptionHandlerLocalizationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("ar", "البريد الإلكتروني أو كلمة المرور غير صحيحة.")]
    [InlineData("ar-EG", "البريد الإلكتروني أو كلمة المرور غير صحيحة.")]
    [InlineData("ar-SA", "البريد الإلكتروني أو كلمة المرور غير صحيحة.")]
    [InlineData("en", "The email or password is incorrect.")]
    public async Task GlobalExceptionHandler_Returns_Correct_Localized_Message_For_Culture(string cultureName, string expectedMessage)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        var sp = services.BuildServiceProvider();

        var localizer = sp.GetRequiredService<IStringLocalizer<SharedResource>>();
        var env = new DummyHostingEnvironment();

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance,
            env,
            localizer);

        var context = new DefaultHttpContext();
        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var cultureFeature = new RequestCultureFeature(new RequestCulture(culture), new AcceptLanguageHeaderRequestCultureProvider());
        context.Features.Set<IRequestCultureFeature>(cultureFeature);

        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var exception = new UnAuthorizedException(ErrorCodes.AuthInvalidCredentials);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBodyStream);
        var json = await reader.ReadToEndAsync();
        _output.WriteLine($"Culture: {cultureName} -> Response JSON: {json}");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var message = root.GetProperty("message").GetString();
        var globalErrorCode = root.GetProperty("globalErrorCode").GetString();

        Assert.Equal(ErrorCodes.AuthInvalidCredentials, globalErrorCode);
        Assert.Equal(expectedMessage, message);
    }
}
