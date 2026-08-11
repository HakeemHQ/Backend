using Hakeem.Application.Services;
using Hakeem.Infrastructure.Services.Access;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class OneTimeAccessCodeServiceTests
{
    [Fact]
    public void Generate_ReturnsSixDigitsAndStoresOnlyVerifiableHash()
    {
        var service = new OneTimeAccessCodeService(new BcryptPasswordHasher());

        var issuedCode = service.Generate();

        Assert.Matches("^[0-9]{6}$", issuedCode.Code);
        Assert.NotEqual(issuedCode.Code, issuedCode.CodeHash);
        Assert.True(service.Verify(issuedCode.Code, issuedCode.CodeHash));
        Assert.False(service.Verify("000000", issuedCode.CodeHash));
    }
}
