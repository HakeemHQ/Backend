using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Infrastructure.Services.MedicalCvs;

namespace Hakeem.Infrastructure.Tests.MedicalCvs;

public sealed class QuestMedicalCvPdfGeneratorTests
{
    [Fact]
    public void Generate_CreatesValidPdfBytes()
    {
        var generator = new QuestMedicalCvPdfGenerator();
        var document = new MedicalCvPdfDocument(
            new MedicalCvPatientInformation(
                "Mona Hassan",
                new DateTime(1988, 6, 24),
                "Female",
                "mona@example.com",
                "+201001234567"),
            MedicalCvScopeType.Focused,
            "Diabetes",
            new DateTime(2026, 8, 7, 12, 30, 0, DateTimeKind.Utc),
            new MedicalCvContent
            {
                Title = "Diabetes-Focused Medical CV",
                Summary = "A focused summary based only on confirmed evidence.",
                Sections =
                [
                    new MedicalCvSection
                    {
                        Heading = "Relevant History",
                        Entries =
                        [
                            new MedicalCvEntry
                            {
                                Title = "Laboratory Record",
                                Date = "2026-07-15",
                                Details =
                                [
                                    "HbA1c, 7.2%",
                                    "Record status: confirmed"
                                ]
                            }
                        ]
                    }
                ]
            });

        var pdfBytes = generator.Generate(document);

        Assert.True(pdfBytes.Length > 1_000);
        Assert.Equal("%PDF-"u8.ToArray(), pdfBytes[..5]);

        var previewPath = Environment.GetEnvironmentVariable(
            "HAKEEM_TEST_PDF_OUTPUT");
        if (!string.IsNullOrWhiteSpace(previewPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(previewPath)!);
            File.WriteAllBytes(previewPath, pdfBytes);
        }
    }

    [Fact]
    public void Generate_ArabicDocument_CreatesValidRightToLeftPdf()
    {
        var generator = new QuestMedicalCvPdfGenerator();
        var document = new MedicalCvPdfDocument(
            new MedicalCvPatientInformation(
                "منى حسن",
                new DateTime(1988, 6, 24),
                "أنثى",
                "mona@example.com",
                "+201001234567"),
            MedicalCvScopeType.Full,
            null,
            new DateTime(2026, 8, 7, 12, 30, 0, DateTimeKind.Utc),
            new MedicalCvContent
            {
                Title = "السيرة الطبية",
                Summary = "ملخص طبي مبني على السجلات المؤكدة.",
                Sections =
                [
                    new MedicalCvSection
                    {
                        Heading = "التشخيصات",
                        Entries =
                        [
                            new MedicalCvEntry
                            {
                                Title = "السكري",
                                Details = ["الحالة: مؤكدة"]
                            }
                        ]
                    }
                ]
            },
            "ar");

        var pdfBytes = generator.Generate(document);

        Assert.True(pdfBytes.Length > 1_000);
        Assert.Equal("%PDF-"u8.ToArray(), pdfBytes[..5]);
    }
}
