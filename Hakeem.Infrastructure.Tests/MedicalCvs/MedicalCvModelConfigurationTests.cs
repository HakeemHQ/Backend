using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class MedicalCvModelConfigurationTests
{
    [Fact]
    public void Model_StoresLogicalIdentityOnMedicalCv()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ModelInspectionOnly")
            .Options;
        using var context = new ApplicationDbContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;

        var medicalCv = model.FindEntityType(typeof(MedicalCv));
        var version = model.FindEntityType(typeof(MedicalCvVersion));

        Assert.NotNull(medicalCv);
        Assert.NotNull(version);
        Assert.NotNull(medicalCv.FindProperty(nameof(MedicalCv.ScopeType)));
        Assert.NotNull(medicalCv.FindProperty(nameof(MedicalCv.Focus)));
        Assert.Null(version.FindProperty("ScopeType"));
        Assert.Null(version.FindProperty("Focus"));

        var logicalIdentityIndex = Assert.Single(
            medicalCv.GetIndexes(),
            index => index.Properties
                .Select(property => property.Name)
                .SequenceEqual(
                [
                    nameof(MedicalCv.PatientId),
                    nameof(MedicalCv.ScopeType),
                    nameof(MedicalCv.Focus)
                ]));

        Assert.True(logicalIdentityIndex.IsUnique);
        Assert.Null(logicalIdentityIndex.GetFilter());
        Assert.Contains(
            medicalCv.GetCheckConstraints(),
            constraint => constraint.Name == "CK_MedicalCvs_ScopeType_Focus");
    }
}
