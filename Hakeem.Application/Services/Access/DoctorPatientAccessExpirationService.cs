using Hakeem.Application.Repositories.DoctorPatientAccesses;
using Hakeem.Domain.Interfaces;
using Hakeem.Domain.Interfaces.ServiceLifetime;

namespace Hakeem.Application.Services.Access;

public interface IDoctorPatientAccessExpirationService : IScoped
{
    Task ExpireForDoctorAsync(
        Guid doctorProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task ExpireForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task ExpireForPairAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken);
}

public sealed class DoctorPatientAccessExpirationService(
    IDoctorPatientAccessRepository accessRepository,
    IUnitOfWork unitOfWork)
    : IDoctorPatientAccessExpirationService
{
    public Task ExpireForDoctorAsync(
        Guid doctorProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        ExpireAsync(
            doctorProfileId,
            null,
            utcNow,
            cancellationToken);

    public Task ExpireForPatientAsync(
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        ExpireAsync(
            null,
            patientProfileId,
            utcNow,
            cancellationToken);

    public Task ExpireForPairAsync(
        Guid doctorProfileId,
        Guid patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        ExpireAsync(
            doctorProfileId,
            patientProfileId,
            utcNow,
            cancellationToken);

    private async Task ExpireAsync(
        Guid? doctorProfileId,
        Guid? patientProfileId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if ((!doctorProfileId.HasValue && !patientProfileId.HasValue) ||
            doctorProfileId == Guid.Empty ||
            patientProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "Doctor and patient profile IDs must not be empty.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await accessRepository.ExpireActiveAccessesAsync(
                doctorProfileId,
                patientProfileId,
                utcNow,
                cancellationToken);
            await accessRepository.ExpireRedeemedRequestsForExpiredAccessesAsync(
                doctorProfileId,
                patientProfileId,
                utcNow,
                cancellationToken);
            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await unitOfWork.RollBackTransactionAsync();
            throw;
        }
    }
}
