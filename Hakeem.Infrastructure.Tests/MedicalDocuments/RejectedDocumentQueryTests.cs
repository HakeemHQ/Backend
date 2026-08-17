using Hakeem.Application.Common.Interfaces;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalDocuments.Queries.GetDocumentById;
using Hakeem.Application.Interfaces.Files;
using Hakeem.Application.Repositories.PatientProfiles;
using Hakeem.Domain.Entities;
using Hakeem.Infrastructure.Tests.DocumentExtraction.Fakes;

namespace Hakeem.Infrastructure.Tests.MedicalDocuments;

public sealed class RejectedDocumentQueryTests
{
    [Fact]
    public async Task GetDocumentById_RejectedDocument_ReturnsStableNonMedicalCode()
    {
        var userId = Guid.NewGuid();
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };
        var document = new MedicalDocument
        {
            Id = Guid.NewGuid(),
            PatientProfileId = patient.Id,
            FilePath = "TestDocuments/rejected.pdf"
        };
        document.StartExtraction();
        document.RejectExtraction("Document.NotMedical");
        var handler = new GetDocumentByIdQueryHandler(
            new FakeCurrentUserContext(userId),
            new FakePatientProfileRepository(patient),
            new FakeMedicalDocumentRepository(document),
            new ThrowingFileUrlResolver());

        var exception = await Assert.ThrowsAsync<UnprocessableEntityException>(
            () => handler.Handle(
                new GetDocumentByIdQuery(document.Id),
                CancellationToken.None));

        Assert.Equal("Document.NotMedical", exception.ErrorCode);
        Assert.Equal(422, exception.StatusCode);
    }

    private sealed record FakeCurrentUserContext(Guid UserId)
        : ICurrentUserContext;

    private sealed class FakePatientProfileRepository(PatientProfile patient)
        : IPatientProfileRepository
    {
        public Task<PatientProfile?> GetByIdAsync(
            Guid patientProfileId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.Id == patientProfileId ? patient : null);

        public Task<PatientProfile?> GetByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PatientProfile?>(
                patient.UserId == userId ? patient : null);

        public Task<PatientProfile?> UpdateByUserIdAsync(
            Guid userId,
            string? fullName,
            DateTime? birthDate,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            string? gender,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ThrowingFileUrlResolver : IFileUrlResolver
    {
        public string ResolveFileUrl(string value) =>
            throw new InvalidOperationException(
                "Rejected documents must not resolve a file URL.");

        public string ToAbsoluteUrl(string relativePath) =>
            throw new NotSupportedException();
    }
}
