using Hakeem.Application.Features.MedicalDataExtraction.DTOs;
using MediatR;

namespace Hakeem.Application.Features.MedicalDataExtraction.Queries.GetExtractedFields;

public sealed record GetExtractedFieldsQuery(Guid DocumentId)
    : IRequest<ExtractedFieldsResponse>;
