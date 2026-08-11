using Hakeem.Application.Features.DoctorProfile.DTOs;
using MediatR;

namespace Hakeem.Application.Features.DoctorProfile.Queries.GetDoctorProfile;

public sealed record GetDoctorProfileQuery : IRequest<DoctorProfileResponse>;
