namespace Hakeem.Application.Services.DocumentExtraction;

internal static class DocumentExtractionSchema
{
    public static readonly IReadOnlySet<string> DocumentTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Prescription",
            "LabReport",
            "DischargeSummary",
            "MedicalVisit",
            "RadiologyReport",
            "ClinicalNote",
            "Other"
        };

    public static readonly IReadOnlySet<string> ItemTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Medication",
            "LabResult",
            "Condition",
            "Allergy",
            "Procedure",
            "Visit",
            "Facility",
            "PatientInformation"
        };

    public static readonly IReadOnlySet<string> FieldNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "MedicationName",
            "Dose",
            "Frequency",
            "Route",
            "LabTestName",
            "LabValue",
            "Unit",
            "ReferenceRange",
            "ConditionName",
            "AllergyName",
            "ProcedureName",
            "Date",
            "DoctorName",
            "FacilityName",
            "PatientName",
            "Quantity"
        };

    public static readonly IReadOnlySet<string> Issues =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "LowConfidence",
            "Unclear",
            "PartiallyReadable",
            "MissingValue",
            "Conflicting",
            "Duplicate",
            "PossibleOcrError"
        };

    public static string CanonicalizeFieldName(
        string itemType,
        string fieldName)
    {
        if (!string.Equals(
                fieldName,
                "Name",
                StringComparison.Ordinal))
        {
            return fieldName;
        }

        return itemType switch
        {
            "PatientInformation" => "PatientName",
            "Medication" => "MedicationName",
            "LabResult" => "LabTestName",
            "Condition" => "ConditionName",
            "Allergy" => "AllergyName",
            "Procedure" => "ProcedureName",
            "Facility" => "FacilityName",
            _ => fieldName
        };
    }
}
