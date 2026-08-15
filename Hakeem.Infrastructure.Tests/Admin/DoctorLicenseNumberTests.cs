using Hakeem.Application.Features.Admin.Doctors;

namespace Hakeem.Infrastructure.Tests.Admin;

public sealed class DoctorLicenseNumberTests
{
    [Fact]
    public void Generate_UsesDoctorIdAndSystemPrefix()
    {
        var doctorId = Guid.Parse("7c74ddcf-1e92-4662-b909-d1648b374241");

        var licenseNumber = DoctorLicenseNumber.Generate(doctorId);

        Assert.Equal(
            "HKM-DR-7C74DDCF1E924662B909D1648B374241",
            licenseNumber);
    }

    [Fact]
    public void Generate_DifferentDoctorIds_ReturnsDifferentLicenses()
    {
        var first = DoctorLicenseNumber.Generate(Guid.NewGuid());
        var second = DoctorLicenseNumber.Generate(Guid.NewGuid());

        Assert.NotEqual(first, second);
    }
}
