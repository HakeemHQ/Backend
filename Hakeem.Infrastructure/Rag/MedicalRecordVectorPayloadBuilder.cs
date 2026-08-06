using Hakeem.Application.Configurations;
using Hakeem.Application.Interfaces.Rag;
using Microsoft.Extensions.Options;

namespace Hakeem.Infrastructure.Rag;

internal static class MedicalRecordVectorPayloadBuilder
{
    public static Dictionary<string, string> BuildValues(
        MedicalRecordVectorDocument document)
    {
        var fieldsText = string.Join(
            "; ",
            document.Fields.Select(field => $"{field.FieldName}: {field.Value}"));

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["medical_record_id"] = document.MedicalRecordId.ToString(),
            ["patient_profile_id"] = document.PatientProfileId.ToString(),
            ["record_type"] = document.RecordType,
            ["display_name"] = document.DisplayName,
            ["status"] = document.Status,
            ["clinical_date"] = document.ClinicalDate.ToString("O"),
            ["content"] = $"{document.DisplayName} | {fieldsText}",
            ["fields"] = fieldsText
        };
    }

    public static IReadOnlyDictionary<string, string> SelectConfiguredFields(
        MedicalRecordVectorDocument document,
        QdrantConfiguration configuration)
    {
        var availableValues = BuildValues(document);
        var selectedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldName in configuration.PayloadFields)
        {
            if (availableValues.TryGetValue(fieldName, out var value))
            {
                selectedValues[fieldName] = value;
            }
        }

        return selectedValues;
    }
}
