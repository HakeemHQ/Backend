using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Hakeem.Infrastructure.Tests.MedicalDocuments;

public sealed class MedicalDocumentCascadeConfigurationTests
{
    [Fact]
    public void DocumentDeletion_CascadesToExtractedItemsAndFields()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HakeemDocumentModelTests")
            .Options;
        using var context = new ApplicationDbContext(options);

        var extractedItemEntity = context.Model.FindEntityType(typeof(ExtractedItem));
        var extractedFieldEntity = context.Model.FindEntityType(typeof(ExtractedField));
        Assert.NotNull(extractedItemEntity);
        Assert.NotNull(extractedFieldEntity);

        var documentForeignKey = Assert.Single(
            extractedItemEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(MedicalDocument));
        var itemForeignKey = Assert.Single(
            extractedFieldEntity.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(ExtractedItem));

        Assert.Equal(DeleteBehavior.Cascade, documentForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, itemForeignKey.DeleteBehavior);
    }
}
