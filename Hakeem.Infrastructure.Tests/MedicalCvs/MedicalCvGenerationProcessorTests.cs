using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Services.MedicalCvs;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class MedicalCvGenerationProcessorTests
{
    [Fact]
    public async Task ProcessAsync_GeneratesPdfAndCompletesQueuedVersion()
    {
        var version = CreateVersion();
        var contentGenerator = new FakeContentGenerator();
        var storage = new FakeFileStorage();
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            version,
            contentGenerator,
            storage,
            unitOfWork);

        await processor.ProcessAsync(version.Id, CancellationToken.None);

        Assert.Equal(MedicalCvVersionStatus.Draft, version.Status);
        Assert.Equal(
            $"medical-cvs/{version.MedicalCvId}/version-1.pdf",
            version.PdfFileKey);
        Assert.Equal(1, contentGenerator.CallCount);
        Assert.NotNull(contentGenerator.Request);
        Assert.Equal("Test Medical CV", contentGenerator.Request.Title);
        Assert.Equal(
            "Confirmed diagnosis, Diabetes",
            Assert.Single(contentGenerator.Request.Evidence).Content);
        Assert.Equal(1, storage.SaveCallCount);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenGeminiFails_MarksVersionFailedAndRethrows()
    {
        var version = CreateVersion();
        var contentGenerator = new FakeContentGenerator(fail: true);
        var storage = new FakeFileStorage();
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            version,
            contentGenerator,
            storage,
            unitOfWork);

        await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            processor.ProcessAsync(version.Id, CancellationToken.None));

        Assert.Equal(MedicalCvVersionStatus.Failed, version.Status);
        Assert.Empty(version.PdfFileKey);
        Assert.Equal(0, storage.SaveCallCount);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenVersionIsAlreadyComplete_IsIdempotent()
    {
        var version = CreateVersion();
        version.Status = MedicalCvVersionStatus.Draft;
        version.PdfFileKey = "medical-cvs/existing/version-1.pdf";
        var contentGenerator = new FakeContentGenerator();
        var storage = new FakeFileStorage();
        var unitOfWork = new FakeUnitOfWork();
        var processor = CreateProcessor(
            version,
            contentGenerator,
            storage,
            unitOfWork);

        await processor.ProcessAsync(version.Id, CancellationToken.None);

        Assert.Equal(0, contentGenerator.CallCount);
        Assert.Equal(0, storage.SaveCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static MedicalCvGenerationProcessor CreateProcessor(
        MedicalCvVersion version,
        FakeContentGenerator contentGenerator,
        FakeFileStorage storage,
        FakeUnitOfWork unitOfWork)
    {
        return new MedicalCvGenerationProcessor(
            new FakeMedicalCvRepository(version),
            contentGenerator,
            new FakePdfGenerator(),
            storage,
            unitOfWork,
            NullLogger<MedicalCvGenerationProcessor>.Instance);
    }

    private static MedicalCvVersion CreateVersion()
    {
        var patient = new PatientProfile
        {
            Id = Guid.NewGuid(),
            FullName = "Test Patient",
            BirthDate = new DateTime(1990, 3, 12),
            User = new User
            {
                Email = "patient@example.com",
                PhoneNumber = "+201000000000",
                Gender = "Female"
            }
        };
        var medicalCv = new MedicalCv
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            ScopeType = MedicalCvScopeType.Full,
            Title = "Test Medical CV",
            PatientProfile = patient
        };
        var version = new MedicalCvVersion
        {
            Id = Guid.NewGuid(),
            MedicalCvId = medicalCv.Id,
            MedicalCv = medicalCv,
            VersionNumber = 1,
            Status = MedicalCvVersionStatus.Queued
        };
        version.SummarizedRecords.Add(new MedicalRecord
        {
            Id = Guid.NewGuid(),
            DisplayName = "Confirmed diagnosis, Diabetes",
            RecordType = "Diagnosis",
            ClinicalDate = new DateTime(2026, 7, 15)
        });
        return version;
    }

    private sealed class FakeMedicalCvRepository(MedicalCvVersion version)
        : IMedicalCvRepository
    {
        public Task<MedicalCvVersion?> GetVersionForGenerationAsync(
            Guid medicalCvVersionId,
            CancellationToken cancellationToken) =>
            Task.FromResult<MedicalCvVersion?>(
                version.Id == medicalCvVersionId ? version : null);

        public Task<MedicalCv?> GetByLogicalIdentityAsync(
            Guid patientId,
            MedicalCvScopeType scopeType,
            string? focus,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> GetNextVersionNumberAsync(
            Guid medicalCvId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MedicalCvVersion?> GetVersionForPatientAsync(
            Guid medicalCvVersionId,
            Guid patientId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(MedicalCv medicalCv) => throw new NotSupportedException();
        public void AddVersion(MedicalCvVersion version) =>
            throw new NotSupportedException();
    }

    private sealed class FakeContentGenerator(bool fail = false)
        : IMedicalCvContentGenerator
    {
        public int CallCount { get; private set; }
        public MedicalCvContentRequest? Request { get; private set; }

        public Task<MedicalCvContent> GenerateAsync(
            MedicalCvContentRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Request = request;

            return fail
                ? Task.FromException<MedicalCvContent>(
                    new ServiceUnavailableException(
                        ErrorCodes.MedicalCvAiUnavailable))
                : Task.FromResult(new MedicalCvContent
                {
                    Title = "Medical CV",
                    Summary = "Summary"
                });
        }
    }

    private sealed class FakePdfGenerator : IMedicalCvPdfGenerator
    {
        public byte[] Generate(MedicalCvPdfDocument document) =>
            "%PDF-1.7"u8.ToArray();
    }

    private sealed class FakeFileStorage : IMedicalCvFileStorage
    {
        public int SaveCallCount { get; private set; }

        public Task<string> SaveAsync(
            byte[] pdfBytes,
            Guid medicalCvId,
            int versionNumber,
            CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.FromResult(
                $"medical-cvs/{medicalCvId}/version-{versionNumber}.pdf");
        }

        public Task<bool> DeleteAsync(
            string fileKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<Stream> OpenReadAsync(
            string fileKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChanges()
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task CommitTransactionAsync() => Task.CompletedTask;
        public Task RollBackTransactionAsync() => Task.CompletedTask;
        public void Dispose() { }
    }
}
