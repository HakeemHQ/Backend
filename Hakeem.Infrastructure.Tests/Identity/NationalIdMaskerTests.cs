using Hakeem.Application.Features.PatientProfile.DTOs;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class NationalIdMaskerTests
{
    [Fact]
    public void Mask_HidesAllButLastFourDigits()
    {
        var masked = NationalIdMasker.Mask("30005120112345");

        Assert.Equal("**********2345", masked);
        Assert.DoesNotContain("3000512011", masked);
    }
}
