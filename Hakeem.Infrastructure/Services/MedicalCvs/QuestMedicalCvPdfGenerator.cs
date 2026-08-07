using Hakeem.Application.Features.MedicalCvs.DTOs;
using Hakeem.Application.Interfaces.MedicalCvs;
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
                page.DefaultTextStyle(style => style
                    .FontFamily("Arial")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken3));

                page.Header().Element(header => ComposeHeader(header, document));
                page.Content().PaddingVertical(18).Element(content =>
                    ComposeContent(content, document));
                page.Footer().Element(footer => ComposeFooter(
                    footer,
                    document.GeneratedAtUtc));
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
                ? "Complete Medical CV"
                : $"Focused Medical CV: {document.Focus}";

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
            column.Spacing(10);
            column.Item().Element(patient => ComposePatientInformation(
                patient,
                document.Patient));

            column.Item()
                .Background(LightBackground)
                .Padding(12)
                .Column(summary =>
                {
                    summary.Item().Text("Clinical Summary")
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
                    column.Item().Text("No confirmed entries available.")
                        .Italic()
                        .FontColor(MutedText);
                    continue;
                }

                foreach (var entry in section.Entries)
                {
                    column.Item()
                        .BorderLeft(3)
                        .BorderColor(PrimaryColor)
                        .PaddingLeft(10)
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
        MedicalCvPatientInformation patient)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(12)
            .Column(column =>
            {
                column.Item().Text("Patient Information")
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

                    AddPatientRow(table, "Name", patient.FullName, "Birth date",
                        patient.BirthDate.ToString("yyyy-MM-dd"));
                    AddPatientRow(table, "Gender", patient.Gender, "Phone",
                        patient.PhoneNumber);
                    table.Cell().PaddingVertical(3).Text("Email").SemiBold();
                    table.Cell().ColumnSpan(3).PaddingVertical(3)
                        .Text(ValueOrUnavailable(patient.Email));
                });
            });
    }

    private static void AddPatientRow(
        TableDescriptor table,
        string firstLabel,
        string firstValue,
        string secondLabel,
        string secondValue)
    {
        table.Cell().PaddingVertical(3).Text(firstLabel).SemiBold();
        table.Cell().PaddingVertical(3).Text(ValueOrUnavailable(firstValue));
        table.Cell().PaddingVertical(3).Text(secondLabel).SemiBold();
        table.Cell().PaddingVertical(3).Text(ValueOrUnavailable(secondValue));
    }

    private static void ComposeFooter(
        IContainer container,
        DateTime generatedAtUtc)
    {
        container.Row(row =>
        {
            row.RelativeItem()
                .DefaultTextStyle(style => style
                    .FontSize(8)
                    .FontColor(MutedText))
                .Text(text =>
                {
                    text.Span("Generated by Hakeem | ");
                    text.Span(generatedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'"));
                });

            row.AutoItem()
                .DefaultTextStyle(style => style
                    .FontSize(8)
                    .FontColor(MutedText))
                .Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
        });
    }

    private static string ValueOrUnavailable(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Not available" : value;
    }
}
