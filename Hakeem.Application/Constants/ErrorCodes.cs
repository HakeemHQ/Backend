namespace Hakeem.Application.Constants;

public static class ErrorCodes
{
    // ── General / Operation ──────────────────────────────────────────────
    public const string OperationSuccess = "Operation.Success";
    public const string OperationFailure = "Operation.Failure";
    public const string ValidationError = "Validation.Error";
    public const string ServerInternalError = "Server.InternalError";
    public const string ResourceNotFound = "Resource.NotFound";

    // ── Validation ───────────────────────────────────────────────────────
    public const string ValidationRequired = "Validation.Required";
    public const string ValidationMaxLength = "Validation.MaxLength";
    public const string ValidationMinLength = "Validation.MinLength";
    public const string ValidationInvalidFormat = "Validation.InvalidFormat";
    public const string ValidationInvalidEmail = "Validation.InvalidEmail";
    public const string ValidationInvalidRange = "Validation.InvalidRange";
    public const string ValidationInvalid = "Validation.Invalid";

    // ── Auth ─────────────────────────────────────────────────────────────
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

    // ── User ─────────────────────────────────────────────────────────────
    public const string UserNotFound = "User.NotFound";
    public const string UserEmailAlreadyExists = "User.EmailAlreadyExists";
    public const string UserRolesRequired = "User.RolesRequired";
    public const string UserUpdated = "User.Updated";
    public const string UserStatusUpdated = "User.StatusUpdated";

    // ── Role ─────────────────────────────────────────────────────────────
    public const string RoleNotFound = "Role.NotFound";

    // ── OTP ──────────────────────────────────────────────────────────────
    public const string OtpRateLimited = "Otp.RateLimited";
    public const string OtpTooManyRequests = "Otp.TooManyRequests";
    public const string OtpInvalidOrExpired = "Otp.InvalidOrExpired";
    public const string OtpExpired = "Otp.Expired";
    public const string OtpInvalidAttemptsRemaining = "Otp.InvalidAttemptsRemaining";

    // ── File Validation ──────────────────────────────────────────────────
    public const string FileValidationNoRuleFound = "FileValidation.NoRuleFound";
    public const string FileValidationMaxSize = "FileValidation.MaxSize";
    public const string FileValidationInvalidExtension = "FileValidation.InvalidExtension";
    public const string FileValidationInvalidContentType = "FileValidation.InvalidContentType";
    public const string FileValidationMinCount = "FileValidation.MinCount";
    public const string FileValidationMaxCount = "FileValidation.MaxCount";

    // ── JSON ─────────────────────────────────────────────────────────────
    public const string InvalidJson = "Invalid.Json";
}
