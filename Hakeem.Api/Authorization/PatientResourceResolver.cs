using Hakeem.Application.Repositories.MedicalDocuments;
using Hakeem.Application.Repositories.MedicalCvs;
using Hakeem.Application.Repositories.MedicalRecords;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Hakeem.Api.Authorization;

public interface IPatientResourceResolver
{
    Task<Guid?> ResolvePatientIdAsync(object? resource);
}

public sealed class PatientResourceResolver(
    IMedicalDocumentRepository medicalDocumentRepository,
    IMedicalRecordsRepository medicalRecordRepository,
    IMedicalCvReadRepository medicalCvReadRepository,
    IMedicalCvRepository medicalCvRepository)
    : IPatientResourceResolver
{
    private const string DocumentIdRouteValue = "documentId";
    private const string MedicalRecordIdRouteValue = "medicalRecordId";
    private const string ExtractedItemIdRouteValue = "extractedItemId";
    private const string MedicalCvIdRouteValue = "medicalCvId";
    private const string VersionIdRouteValue = "versionId";

    public async Task<Guid?> ResolvePatientIdAsync(object? resource)
    {
        if (resource is DoctorPatientAccessResource accessResource)
        {
            return accessResource.PatientId;
        }

        if (resource is Guid patientId)
        {
            return patientId;
        }

        var httpContext = resource switch
        {
            HttpContext context => context,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => null
        };

        if (httpContext is null)
        {
            return null;
        }

        if (TryGetRouteId(
                httpContext,
                DoctorPatientAccessPolicy.PatientIdRouteValue,
                out patientId) ||
            TryGetRouteId(
                httpContext,
                DoctorPatientAccessPolicy.PatientProfileIdRouteValue,
                out patientId))
        {
            return patientId;
        }

        var cancellationToken = httpContext.RequestAborted;

        if (TryGetRouteId(httpContext, DocumentIdRouteValue, out var documentId))
        {
            var document = await medicalDocumentRepository.GetByIdAsync(
                documentId,
                cancellationToken);
            return document?.PatientProfileId;
        }

        if (TryGetRouteId(
                httpContext,
                MedicalRecordIdRouteValue,
                out var medicalRecordId))
        {
            var medicalRecord = await medicalRecordRepository.GetByIdWithDetailsAsync(
                medicalRecordId,
                cancellationToken);
            return medicalRecord?.PatientProfileId;
        }

        if (TryGetRouteId(
                httpContext,
                ExtractedItemIdRouteValue,
                out var extractedItemId))
        {
            var extractedItem =
                await medicalDocumentRepository.GetExtractedItemForReviewAsync(
                    extractedItemId,
                    cancellationToken);
            return extractedItem?.MedicalDocument.PatientProfileId;
        }

        if (TryGetRouteId(httpContext, MedicalCvIdRouteValue, out var medicalCvId))
        {
            var medicalCv = await medicalCvReadRepository.GetByIdAsync(
                medicalCvId,
                cancellationToken);
            return medicalCv?.PatientId;
        }

        if (TryGetRouteId(httpContext, VersionIdRouteValue, out var versionId))
        {
            var version = await medicalCvRepository.GetVersionByIdAsync(
                versionId,
                cancellationToken);
            if (version is null)
            {
                return null;
            }

            var medicalCv = await medicalCvReadRepository.GetByIdAsync(
                version.MedicalCvId,
                cancellationToken);
            return medicalCv?.PatientId;
        }

        return null;
    }

    private static bool TryGetRouteId(
        HttpContext httpContext,
        string key,
        out Guid id)
    {
        if (httpContext.Request.RouteValues.TryGetValue(key, out var value) &&
            Guid.TryParse(value?.ToString(), out id) &&
            id != Guid.Empty)
        {
            return true;
        }

        id = Guid.Empty;
        return false;
    }
}
