using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class IdentityModelConfigurationTests
{
    [Fact]
    public void PatientIdentityIndexes_AllowClaimsButUniquelyConstrainVerifiedIds()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HakeemIdentityModelTests")
            .Options;
        using var context = new ApplicationDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PatientProfile));

        Assert.NotNull(entity);
        Assert.DoesNotContain(
            entity.GetIndexes(),
            index => index.Properties.Any(property => property.Name == nameof(PatientProfile.NationalId)));

        var verifiedIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(PatientProfile.VerifiedNationalId));
        Assert.True(verifiedIndex.IsUnique);
        Assert.Equal("[VerifiedNationalId] IS NOT NULL", verifiedIndex.GetFilter());

        var patientCodeIndex = Assert.Single(
            entity.GetIndexes(),
            index => index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(PatientProfile.PatientCode));
        Assert.True(patientCodeIndex.IsUnique);
    }
}
