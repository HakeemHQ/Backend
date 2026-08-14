using Hakeem.Application.Constants;
using Hakeem.Application.Exceptions;
using Hakeem.Application.Features.Auth.Commands.Registration;
using Hakeem.Application.Interfaces.Identity;
using Hakeem.Application.Interfaces.Services.Auth;
using Hakeem.Application.Repositories.PatientIdentities;
using Hakeem.Application.Repositories.Users;
using Hakeem.Domain.Entities;
using Hakeem.Domain.Interfaces;

namespace Hakeem.Infrastructure.Tests.Identity;

public sealed class RegisterCommandHandlerTests
{
    private const string NationalId = "30005120112345";

    [Fact]
    public async Task Handle_WhenNationalIdIsAlreadyVerified_ReturnsLocalizedConflictCode()
    {
        var userRepository = new FakeUserRepository();
        var identityRepository = new FakePatientIdentityRepository
        {
            VerifiedNationalIdExists = true
        };
        var passwordHasher = new FakePasswordHasher();
        var patientCodeGenerator = new FakePatientCodeGenerator();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterCommandHandler(
            userRepository,
            identityRepository,
            passwordHasher,
            patientCodeGenerator,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(CreateCommand(), CancellationToken.None));

        Assert.Equal(ErrorCodes.AuthNationalIdAlreadyRegistered, exception.ErrorCode);
        Assert.Equal(NationalId, identityRepository.QueriedNationalId);
        Assert.False(passwordHasher.HashCalled);
        Assert.False(patientCodeGenerator.GenerateCalled);
        Assert.Null(userRepository.AddedUser);
        Assert.False(unitOfWork.SaveCalled);
    }

    [Fact]
    public async Task Handle_WhenNationalIdIsNotVerified_RegistersWithNormalizedNationalId()
    {
        var userRepository = new FakeUserRepository();
        var identityRepository = new FakePatientIdentityRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RegisterCommandHandler(
            userRepository,
            identityRepository,
            new FakePasswordHasher(),
            new FakePatientCodeGenerator(),
            unitOfWork);
        var command = CreateCommand();
        command.NationalId = $"  {NationalId}  ";

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(NationalId, identityRepository.QueriedNationalId);
        Assert.Equal(NationalId, userRepository.AddedUser?.PatientProfile?.NationalId);
        Assert.Equal("Pending", result.Profile.IdentityVerificationStatus);
        Assert.True(unitOfWork.SaveCalled);
    }

    private static RegisterCommand CreateCommand() => new()
    {
        Email = "patient@example.com",
        Password = "Strong!Pass1",
        FirstName = "Mazen",
        LastName = "Mohamed",
        PhoneNumber = "+201001234567",
        Gender = "Male",
        BirthDate = new DateOnly(2000, 5, 12),
        NationalId = NationalId
    };

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? AddedUser { get; private set; }

        public void Add(User user) => AddedUser = user;

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByIdAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<(IEnumerable<Hakeem.Application.Features.Admin.Users.GetUsers.DTOs.AdminUserDto> Items, int TotalCount)> GetUsersAsync(
            string? search,
            Hakeem.Domain.Enums.Identity.AccountStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePatientIdentityRepository : IPatientIdentityRepository
    {
        public bool VerifiedNationalIdExists { get; init; }
        public string? QueriedNationalId { get; private set; }

        public Task<bool> VerifiedNationalIdExistsAsync(
            string verifiedNationalId,
            CancellationToken cancellationToken)
        {
            QueriedNationalId = verifiedNationalId;
            return Task.FromResult(VerifiedNationalIdExists);
        }

        public Task<PatientProfile?> GetByPatientCodeForUpdateAsync(
            string patientCode,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> VerifiedNationalIdBelongsToAnotherPatientAsync(
            string verifiedNationalId,
            Guid patientId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public bool HashCalled { get; private set; }

        public string Hash(string password)
        {
            HashCalled = true;
            return "password-hash";
        }

        public bool Verify(string password, string passwordHash) =>
            throw new NotSupportedException();
    }

    private sealed class FakePatientCodeGenerator : IPatientCodeGenerator
    {
        public bool GenerateCalled { get; private set; }

        public Task<string> GenerateUniqueAsync(CancellationToken cancellationToken)
        {
            GenerateCalled = true;
            return Task.FromResult("H89K-27P");
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveCalled { get; private set; }

        public Task<int> SaveChanges() => SaveChanges(CancellationToken.None);

        public Task<int> SaveChanges(CancellationToken cancellationToken)
        {
            SaveCalled = true;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task CommitTransactionAsync() => throw new NotSupportedException();

        public Task RollBackTransactionAsync() => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
