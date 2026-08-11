using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.Access;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Tests.Access;

public sealed class AccessDomainModelConfigurationTests
{
    [Fact]
    public void StatusEnums_ContainContractValues()
    {
        Assert.Equal(
            ["Pending", "Approved", "Rejected", "Expired", "Redeemed"],
            Enum.GetNames<PatientAccessRequestStatus>());
        Assert.Equal(
            ["Active", "Revoked", "Expired"],
            Enum.GetNames<DoctorPatientAccessStatus>());
    }

    [Fact]
    public void PatientAccessRequest_StoresOnlyHashedCode()
    {
        var properties = typeof(PatientAccessRequest).GetProperties();

        Assert.Contains(properties, property => property.Name == nameof(PatientAccessRequest.CodeHash));
        Assert.DoesNotContain(properties, property => property.Name == "Code");
    }

    [Fact]
    public void AccessModel_ConfiguresCodeAndActiveAccessUniqueness()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HakeemAccessModelTests")
            .Options;
        using var context = new ApplicationDbContext(options);

        var requestEntity = context.Model.FindEntityType(typeof(PatientAccessRequest));
        Assert.NotNull(requestEntity);
        var codeIndex = Assert.Single(
            requestEntity.GetIndexes(),
            index => index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(PatientAccessRequest.CodeHash));
        Assert.True(codeIndex.IsUnique);
        Assert.Equal("[CodeHash] IS NOT NULL", codeIndex.GetFilter());

        var accessEntity = context.Model.FindEntityType(typeof(DoctorPatientAccess));
        Assert.NotNull(accessEntity);
        var requestIndex = Assert.Single(
            accessEntity.GetIndexes(),
            index => index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(DoctorPatientAccess.PatientAccessRequestId));
        Assert.True(requestIndex.IsUnique);

        var activeAccessIndex = Assert.Single(
            accessEntity.GetIndexes(),
            index => index.Properties.Count == 2 &&
                index.Properties.Any(property => property.Name == nameof(DoctorPatientAccess.DoctorProfileId)) &&
                index.Properties.Any(property => property.Name == nameof(DoctorPatientAccess.PatientProfileId)));
        Assert.True(activeAccessIndex.IsUnique);
        Assert.Equal("[Status] = 'Active'", activeAccessIndex.GetFilter());
    }
}
