using Hakeem.Application.Features.PatientProfile.DTOs;
using MediatR;

namespace Hakeem.Application.Features.PatientProfile.Queries.GetProfile;

public sealed record GetProfileQuery : IRequest<PatientProfileResponse>;
