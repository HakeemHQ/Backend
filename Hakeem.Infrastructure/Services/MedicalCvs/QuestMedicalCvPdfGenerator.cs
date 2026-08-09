using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
using Hakeem.Application.Resources;
using Hakeem.Domain.Enums.MedicalCvs;
using Hakeem.Domain.Interfaces.ServiceLifetime;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Hakeem.Infrastructure.Services.MedicalCvs;

public sealed class QuestMedicalCvPdfGenerator : IMedicalCvPdfGenerator, ISingleton
{
    private const string PrimaryColor = "#145B7D";
    private const string LightBackground = "#EFF6F8";
    private const string MutedText = "#52646D";

    static QuestMedicalCvPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(MedicalCvPdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Content);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                if (IsArabic(document))
                {
                    page.ContentFromRightToLeft();
                }
                page.DefaultTextStyle(style => style
                    .FontFamily("Arial")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => ComposeHeader(header, document));
                page.Content().PaddingVertical(18).Element(content =>
                    ComposeContent(content, document));
                page.Footer().Element(footer => ComposeFooter(
                    footer,
                    document.GeneratedAtUtc,
                    IsArabic(document)));
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(
        IContainer container,
        MedicalCvPdfDocument document)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(document.Content.Title)
                .FontSize(22)
                .Bold()
                .FontColor(PrimaryColor);

            var scopeLabel = document.ScopeType == MedicalCvScopeType.Full
                ? LocalizedResourceText.Get(
                    "MedicalCv.Pdf.Scope.Full",
                    document.Language)
                : LocalizedResourceText.Format(
                    "MedicalCv.Pdf.Scope.Focused",
                    document.Language,
                    document.Focus);

            column.Item().Text(scopeLabel)
                .FontSize(11)
                .SemiBold()
                .FontColor(MutedText);
            column.Item().LineHorizontal(1).LineColor(PrimaryColor);
        });
    }

    private static void ComposeContent(
        IContainer container,
        MedicalCvPdfDocument document)
    {
        container.Column(column =>
        {
            var isArabic = IsArabic(document);
            column.Spacing(10);
            column.Item().Element(patient => ComposePatientInformation(
                patient,
                document.Patient,
                isArabic));

            column.Item()
                .Background(LightBackground)
                .Padding(12)
                .Column(summary =>
                {
                    summary.Item().Text(LocalizedResourceText.Get(
                            "MedicalCv.Pdf.ClinicalSummary",
                            document.Language))
                        .FontSize(13)
                        .SemiBold()
                        .FontColor(PrimaryColor);
                    summary.Item().PaddingTop(4).Text(document.Content.Summary);
                });

            foreach (var section in document.Content.Sections)
            {
                column.Item().PaddingTop(8).Text(section.Heading)
                    .FontSize(15)
                    .Bold()
                    .FontColor(PrimaryColor);

                if (section.Entries.Count == 0)
                {
                    column.Item().Text(LocalizedResourceText.Get(
                            "MedicalCv.Pdf.NoConfirmedEntries",
                            document.Language))
                        .Italic()
                        .FontColor(MutedText);
                    continue;
                }

                foreach (var entry in section.Entries)
                {
                    var entryContainer = IsArabic(document)
                        ? column.Item().BorderRight(3).PaddingRight(10)
                        : column.Item().BorderLeft(3).PaddingLeft(10);

                    entryContainer
                        .BorderColor(PrimaryColor)
                        .PaddingVertical(5)
                        .Column(entryColumn =>
                        {
                            entryColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Text(entry.Title).SemiBold();

                                if (!string.IsNullOrWhiteSpace(entry.Date))
                                {
                                    row.AutoItem().Text(entry.Date)
                                        .FontSize(9)
                                        .FontColor(MutedText);
                                }
                            });

                            foreach (var detail in entry.Details)
                            {
                                entryColumn.Item().PaddingTop(2).Text(text =>
                                {
                                    text.Span("- ").FontColor(PrimaryColor);
                                    text.Span(detail);
                                });
                            }
                        });
                }
            }
        });
    }

    private static void ComposePatientInformation(
        IContainer container,
        MedicalCvPatientInformation patient,
        bool isArabic)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Text(Resource(
                        "MedicalCv.Pdf.PatientInformation",
                        isArabic))
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(PrimaryColor);
                column.Item().PaddingTop(7).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(85);
                        columns.RelativeColumn();
                        columns.ConstantColumn(85);
                        columns.RelativeColumn();
                    });

                    AddPatientRow(
                        table,
                        Resource("MedicalCv.Pdf.Name", isArabic),
                        patient.FullName,
                        Resource("MedicalCv.Pdf.BirthDate", isArabic),
                        patient.BirthDate.ToString("yyyy-MM-dd"),
                        isArabic);
                    AddPatientRow(
                        table,
                        Resource("MedicalCv.Pdf.Gender", isArabic),
                        patient.Gender,
                        Resource("MedicalCv.Pdf.Phone", isArabic),
                        patient.PhoneNumber,
                        isArabic);
                    table.Cell().PaddingVertical(3)
                        .Text(Resource("MedicalCv.Pdf.Email", isArabic))
                        .SemiBold();
                    table.Cell().ColumnSpan(3).PaddingVertical(3)
                        .Text(ValueOrUnavailable(patient.Email, isArabic));
                });
            });
    }

    private static void AddPatientRow(
        TableDescriptor table,
        string firstLabel,
        string firstValue,
        string secondLabel,
        string secondValue,
        bool isArabic)
    {
        table.Cell().PaddingVertical(3).Text(firstLabel).SemiBold();
        table.Cell().PaddingVertical(3)
            .Text(ValueOrUnavailable(firstValue, isArabic));
        table.Cell().PaddingVertical(3).Text(secondLabel).SemiBold();
        table.Cell().PaddingVertical(3)
            .Text(ValueOrUnavailable(secondValue, isArabic));
    }

    private static void ComposeFooter(
        IContainer container,
        DateTime generatedAtUtc,
        bool isArabic)
    {
        container.Row(row =>
        {
            row.RelativeItem()
                .DefaultTextStyle(style => style
                    .FontSize(8)
                    .FontColor(MutedText))
                .Text(text =>
                {
                    text.Span(Resource(
                        "MedicalCv.Pdf.GeneratedBy",
                        isArabic) + " | ");
                    text.Span(generatedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'"));
                });

            row.AutoItem()
                .DefaultTextStyle(style => style
                    .FontSize(8)
                    .FontColor(MutedText))
                .Text(text =>
                {
                    text.Span(Resource("MedicalCv.Pdf.Page", isArabic) + " ");
                    text.CurrentPageNumber();
                    text.Span(" " + Resource("MedicalCv.Pdf.Of", isArabic) + " ");
                    text.TotalPages();
                });
        });
    }

    private static string ValueOrUnavailable(string value, bool isArabic)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Resource("MedicalCv.Pdf.NotAvailable", isArabic)
            : value;
    }

    private static string Resource(string key, bool isArabic) =>
        LocalizedResourceText.Get(
            key,
            isArabic
                ? MedicalCvLanguages.Arabic
                : MedicalCvLanguages.English);

    private static bool IsArabic(MedicalCvPdfDocument document) =>
        MedicalCvLanguages.Normalize(document.Language) ==
        MedicalCvLanguages.Arabic;
}
