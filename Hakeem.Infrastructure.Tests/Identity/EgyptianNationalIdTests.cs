using Hakeem.Application.Features.Auth.Commands.Registration;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class EgyptianNationalIdTests
{
    [Fact]
    public void IsStructurallyValid_AcceptsMatchingEgyptianNationalId()
    {
        const string nationalId = "30005120112345";
        var birthDate = new DateOnly(2000, 5, 12);

        Assert.True(EgyptianNationalId.IsStructurallyValid(nationalId, birthDate));
        Assert.True(EgyptianNationalId.IsStructurallyValid(nationalId, birthDate));
    }

    [Theory]
    [InlineData("3000512011234", 2000, 5, 12)]
    [InlineData("30013320112345", 2000, 5, 12)]
    [InlineData("30005129912345", 2000, 5, 12)]
    [InlineData("30005120112345", 2000, 5, 13)]
    public void IsStructurallyValid_RejectsInvalidStructureOrBirthDate(
        string nationalId,
        int year,
        int month,
        int day)
    {
        Assert.False(EgyptianNationalId.IsStructurallyValid(
            nationalId,
            new DateOnly(year, month, day)));
    }
}
