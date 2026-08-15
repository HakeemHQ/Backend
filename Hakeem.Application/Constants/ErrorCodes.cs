namespace Hakeem.Application.Constants;

public static class ErrorCodes
{
    // -- General / Operation  --------------------------------------------
    public const string OperationSuccess = "Operation.Success";
    public const string OperationFailure = "Operation.Failure";
    public const string ValidationError = "Validation.Error";
    public const string ServerInternalError = "Server.InternalError";
    public const string ResourceNotFound = "Resource.NotFound";

    // -- Validation  --------------------------------------------
    public const string ValidationRequired = "Validation.Required";
    public const string ValidationMaxLength = "Validation.MaxLength";
    public const string ValidationMinLength = "Validation.MinLength";
    public const string ValidationInvalidFormat = "Validation.InvalidFormat";
    public const string ValidationInvalidEmail = "Validation.InvalidEmail";
    public const string ValidationInvalidRange = "Validation.InvalidRange";
    public const string ValidationInvalid = "Validation.Invalid";

    // -- Auth  -------------------------------------------------
    public const string AuthUnauthorized = "Auth.Unauthorized";
    public const string AuthForbidden = "Auth.Forbidden";
    public const string AuthInvalidCredentials = "Auth.InvalidCredentials";
    public const string AuthAccountInactive = "Auth.AccountInactive";
    public const string AuthPasswordConfigurationRequired = "Auth.PasswordConfigurationRequired";
    public const string AuthPhoneNotConfigured = "Auth.PhoneNotConfigured";
    public const string AuthPasswordAlreadyConfigured = "Auth.PasswordAlreadyConfigured";
    public const string AuthInvalidGeneratedPassword = "Auth.InvalidGeneratedPassword";
    public const string AuthInvalidRefreshToken = "Auth.InvalidRefreshToken";
    public const string AuthInvalidResetToken = "Auth.InvalidResetToken";
    public const string AuthInvalidMfaToken = "Auth.InvalidMfaToken";
    public const string AuthMfaTokenExpired = "Auth.MfaTokenExpired";
    public const string AuthLoggedOut = "Auth.LoggedOut";
    public const string AuthPasswordResetSent = "Auth.PasswordResetSent";
    public const string AuthPasswordResetSuccess = "Auth.PasswordResetSuccess";
    public const string AuthRegistered = "Auth.Registered";
    public const string AuthLoggedIn = "Auth.LoggedIn";
    public const string AuthTokenRefreshed = "Auth.TokenRefreshed";

    // -- User  --------------------------------------------
    public const string UserNotFound = "User.NotFound";
    public const string UserEmailAlreadyExists = "User.EmailAlreadyExists";
    public const string UserRolesRequired = "User.RolesRequired";
    public const string UserUpdated = "User.Updated";
    public const string UserStatusUpdated = "User.StatusUpdated";
    public const string AccountInvalidStatusTransition = "Account.InvalidStatusTransition";

    // -- Doctor --------------------------------------------
    public const string DoctorNotFound = "Doctor.NotFound";
    public const string DoctorLicenseNumberConflict = "Doctor.LicenseNumberConflict";

    // -- Patient identity  --------------------------------------------
    public const string PatientIdentityNotFound = "PatientIdentity.NotFound";
    public const string PatientIdentityInvalidPatientCode = "PatientIdentity.InvalidPatientCode";
    public const string PatientIdentityNationalIdMismatch = "PatientIdentity.NationalIdMismatch";
    public const string PatientIdentityNationalIdAlreadyVerified = "PatientIdentity.NationalIdAlreadyVerified";
    public const string PatientIdentityAlreadyVerifiedWithDifferentNationalId = "PatientIdentity.AlreadyVerifiedWithDifferentNationalId";
    public const string PatientIdentityVerified = "PatientIdentity.Verified";

    // -- Patient access  --------------------------------------------
    public const string PatientAccessPatientNotFound = "PatientAccess.PatientNotFound";
    public const string PatientAccessPatientNotVerified = "PatientAccess.PatientNotVerified";
    public const string PatientAccessPendingRequestExists = "PatientAccess.PendingRequestExists";
    public const string PatientAccessActiveAccessExists = "PatientAccess.ActiveAccessExists";
    public const string PatientAccessRequestCreated = "PatientAccess.RequestCreated";
    public const string PatientAccessRequestNotFound = "PatientAccess.RequestNotFound";
    public const string PatientAccessRequestAlreadyActedOn = "PatientAccess.RequestAlreadyActedOn";
    public const string PatientAccessRequestsRetrieved = "PatientAccess.RequestsRetrieved";
    public const string PatientAccessRequestApproved = "PatientAccess.RequestApproved";
    public const string PatientAccessRequestRejected = "PatientAccess.RequestRejected";
    public const string PatientAccessInvalidCode = "PatientAccess.InvalidCode";
    public const string PatientAccessCodeExpired = "PatientAccess.CodeExpired";
    public const string PatientAccessCodeAlreadyRedeemed = "PatientAccess.CodeAlreadyRedeemed";
    public const string PatientAccessSessionCreated = "PatientAccess.SessionCreated";
    public const string PatientAccessAccessNotFound = "PatientAccess.AccessNotFound";
    public const string PatientAccessDoctorAccessesRetrieved = "PatientAccess.DoctorAccessesRetrieved";
    public const string PatientAccessPatientAccessesRetrieved = "PatientAccess.PatientAccessesRetrieved";

    // -- Role  --------------------------------------------
    public const string RoleNotFound = "Role.NotFound";

    // -- OTP  --------------------------------------------
    public const string OtpRateLimited = "Otp.RateLimited";
    public const string OtpTooManyRequests = "Otp.TooManyRequests";
    public const string OtpInvalidOrExpired = "Otp.InvalidOrExpired";
    public const string OtpExpired = "Otp.Expired";
    public const string OtpInvalidAttemptsRemaining = "Otp.InvalidAttemptsRemaining";

    // --- File Validation --------------------------------------------
    public const string FileValidationNoRuleFound = "FileValidation.NoRuleFound";
    public const string FileValidationMaxSize = "FileValidation.MaxSize";
    public const string FileValidationInvalidExtension = "FileValidation.InvalidExtension";
    public const string FileValidationInvalidContentType = "FileValidation.InvalidContentType";
    public const string FileValidationMinCount = "FileValidation.MinCount";
    public const string FileValidationMaxCount = "FileValidation.MaxCount";

    // --- Medical documents  --------------------------------------------
    public const string DocumentUploaded = "Document.Uploaded";
    public const string DocumentPatientProfileNotFound = "Document.PatientProfileNotFound";
    public const string DocumentFileRequired = "Document.FileRequired";
    public const string DocumentFileTooLarge = "Document.FileTooLarge";
    public const string DocumentUnsupportedMediaType = "Document.UnsupportedMediaType";
    public const string DocumentInvalidFileContent = "Document.InvalidFileContent";
    public const string DocumentTypeTooLong = "Document.DocumentTypeTooLong";
    public const string DocumentTitleTooLong = "Document.TitleTooLong";
    public const string DocumentDateInFuture = "Document.DateInFuture";
    public const string DocumentNotFound = "Document.NotFound";
    public const string DocumentExtractedFieldsRetrieved = "Document.ExtractedFieldsRetrieved";

    // --- Patient Review & Confirmation  --------------------------------------------
    public const string ExtractedItemNotFound = "ExtractedItem.NotFound";
    public const string ExtractionNotCompleted = "ExtractedItem.ExtractionNotCompleted";
    public const string ExtractedItemAlreadyReviewed = "ExtractedItem.AlreadyReviewed";

    public const string ExtractedFieldNotFound = "ExtractedField.NotFound";
    public const string ExtractedFieldDoesNotBelongToItem = "ExtractedField.DoesNotBelongToItem";
    public const string DuplicateExtractedField = "ExtractedField.Duplicate";
    public const string CorrectedValueRequired = "ExtractedField.CorrectedValueRequired";

    public const string ReviewSaved = "Review.Saved";

    // --- Medical Records --------------------------------------------
    public const string MedicalRecordNotFound = "MedicalRecord.NotFound";
    public const string MedicalRecordFieldNotFound = "MedicalRecordField.NotFound";

    // --- Medical CVs --------------------------------------------
    public const string MedicalCvPatientNotFound = "MedicalCv.PatientNotFound";
    public const string MedicalCvNotFound = "MedicalCv.NotFound";
    public const string MedicalCvNoConfirmedInformation = "MedicalCv.NoConfirmedInformation";
    public const string MedicalCvAiUnavailable = "MedicalCv.AiUnavailable";
    public const string MedicalCvGenerated = "MedicalCv.Generated";
    public const string MedicalCvQueued = "MedicalCv.Queued";
    public const string MedicalCvNotReady = "MedicalCv.NotReady";
    public const string MedicalCvGenerationFailed = "MedicalCv.GenerationFailed";
    public const string MedicalCvPreviewInvalidOrExpired = "MedicalCv.PreviewInvalidOrExpired";
    public const string MedicalCvTitleRequired = "MedicalCv.TitleRequired";
    public const string MedicalCvTitleTooLong = "MedicalCv.TitleTooLong";
    public const string MedicalCvVersionNotDraft = "MedicalCv.VersionNotDraft";

    // ---- JSON -------------------------------------------------
    public const string InvalidJson = "Invalid.Json";
}
