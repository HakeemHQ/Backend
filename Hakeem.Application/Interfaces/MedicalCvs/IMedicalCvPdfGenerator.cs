using Hakeem.Application.Features.MedicalCvs.DTOs;

namespace Hakeem.Application.Interfaces.MedicalCvs;

public interface IMedicalCvPdfGenerator
{
    byte[] Generate(MedicalCvPdfDocument document);
}
