using Hakeem.Application.Interfaces.Rag;
using MediatR;

namespace Hakeem.Application.Features.MedicalRecords.Queries.SearchMedicalRecords;

public sealed record SearchMedicalRecordsQuery(
    string Query,
    Guid PatientProfileId,
    int Limit = 10)
    : IRequest<IReadOnlyList<MedicalRecordSearchResult>>;
