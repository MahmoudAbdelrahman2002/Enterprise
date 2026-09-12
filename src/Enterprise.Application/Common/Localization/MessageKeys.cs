namespace Enterprise.Application.Common.Localization;

/// <summary>
/// Resource keys for every client-facing string. Handlers and controllers pass these
/// (never English literals); <see cref="IAppLocalizer"/> resolves them for the request culture.
/// </summary>
public static class MessageKeys
{
    public const string Success = "Success";
    public const string Created = "Created";

    public static class Error
    {
        public const string NotFound = "Error.NotFound";
        public const string Conflict = "Error.Conflict";
        public const string Unauthorized = "Error.Unauthorized";
        public const string Forbidden = "Error.Forbidden";
        public const string BusinessRule = "Error.BusinessRule";
        public const string EmailDelivery = "Error.EmailDelivery";
        public const string Unexpected = "Error.Unexpected";
        public const string RateLimited = "Error.RateLimited";
    }

    public static class Validation
    {
        public const string Failed = "Validation.Failed";
        public const string CheckRequest = "Validation.CheckRequest";
        public const string Required = "Validation.Required";
        public const string Email = "Validation.Email";
        public const string MaxLength = "Validation.MaxLength";
        public const string MinLength = "Validation.MinLength";
        public const string GreaterThan = "Validation.GreaterThan";
        public const string GreaterThanOrEqual = "Validation.GreaterThanOrEqual";
        public const string OtpLength = "Validation.OtpLength";
        public const string SkuFormat = "Validation.SkuFormat";
        public const string DeltaNonZero = "Validation.DeltaNonZero";
        public const string PasswordMinLength = "Validation.PasswordMinLength";
        public const string PasswordUppercase = "Validation.PasswordUppercase";
        public const string PasswordLowercase = "Validation.PasswordLowercase";
        public const string PasswordDigit = "Validation.PasswordDigit";
        public const string PasswordSpecial = "Validation.PasswordSpecial";
        public const string ProviderUnsupported = "Validation.ProviderUnsupported";
        public const string LocalizedEnRequired = "Validation.LocalizedEnRequired";
    }

    public static class Product
    {
        public const string ListRetrieved = "Product.ListRetrieved";
        public const string Retrieved = "Product.Retrieved";
        public const string Created = "Product.Created";
        public const string Updated = "Product.Updated";
        public const string StockAdjusted = "Product.StockAdjusted";
        public const string Deleted = "Product.Deleted";
        public const string SkuExists = "Product.SkuExists";
        public const string InsufficientStock = "Product.InsufficientStock";
        public const string QuantityMustBePositive = "Product.QuantityMustBePositive";
    }

    public static class Auth
    {
        public const string LoginSuccess = "Auth.LoginSuccess";
        public const string RegistrationVerified = "Auth.RegistrationVerified";
        public const string ExternalLoginSuccess = "Auth.ExternalLoginSuccess";
        public const string TokenRefreshed = "Auth.TokenRefreshed";
        public const string TokenRevoked = "Auth.TokenRevoked";
        public const string PasswordResetSuccess = "Auth.PasswordResetSuccess";
        public const string PasswordChanged = "Auth.PasswordChanged";
        public const string ForgotPasswordSent = "Auth.ForgotPasswordSent";
        public const string InvalidCredentials = "Auth.InvalidCredentials";
        public const string AccountLocked = "Auth.AccountLocked";
        public const string InvalidOrExpiredCode = "Auth.InvalidOrExpiredCode";
        public const string InvalidOrExpiredLoginCode = "Auth.InvalidOrExpiredLoginCode";
        public const string InvalidOrExpiredResetCode = "Auth.InvalidOrExpiredResetCode";
        public const string InvalidOrExpiredVerificationCode = "Auth.InvalidOrExpiredVerificationCode";
        public const string InvalidRefreshToken = "Auth.InvalidRefreshToken";
        public const string RefreshTokenReused = "Auth.RefreshTokenReused";
        public const string RefreshTokenExpired = "Auth.RefreshTokenExpired";
        public const string RefreshTokenNotFound = "Auth.RefreshTokenNotFound";
        public const string UnableToResetPassword = "Auth.UnableToResetPassword";
        public const string UnableToChangePassword = "Auth.UnableToChangePassword";
        public const string SocialEmailRequired = "Auth.SocialEmailRequired";
        public const string SocialLinkFailed = "Auth.SocialLinkFailed";
        public const string SocialSignInFailed = "Auth.SocialSignInFailed";
        public const string SocialAlreadyLinked = "Auth.SocialAlreadyLinked";
        public const string GoogleNotConfigured = "Auth.GoogleNotConfigured";
        public const string FacebookNotConfigured = "Auth.FacebookNotConfigured";
        public const string InvalidGoogleToken = "Auth.InvalidGoogleToken";
        public const string InvalidFacebookToken = "Auth.InvalidFacebookToken";
        public const string FacebookProfileFailed = "Auth.FacebookProfileFailed";
        public const string UnsupportedProvider = "Auth.UnsupportedProvider";
        public const string RegistrationStarted = "Auth.RegistrationStarted";
        public const string OtpSentIfExists = "Auth.OtpSentIfExists";
        public const string AccountNotVerified = "Auth.AccountNotVerified";
    }

    public static class Account
    {
        public const string EmailExists = "Account.EmailExists";
        public const string UnableToCreate = "Account.UnableToCreate";
        public const string UnableToChangeEmail = "Account.UnableToChangeEmail";
        public const string UnableToComplete = "Account.UnableToComplete";
    }

    public static class Profile
    {
        public const string Retrieved = "Profile.Retrieved";
        public const string Updated = "Profile.Updated";
        public const string EmailChangeCodeSent = "Profile.EmailChangeCodeSent";
        public const string EmailChanged = "Profile.EmailChanged";
    }

    public static class Entity
    {
        public const string NotFound = "Entity.NotFound";
    }

    public static class Email
    {
        public const string VerificationSubject = "Email.VerificationSubject";
        public const string VerificationBody = "Email.VerificationBody";
        public const string SignInSubject = "Email.SignInSubject";
        public const string SignInBody = "Email.SignInBody";
        public const string ResetSubject = "Email.ResetSubject";
        public const string ResetBody = "Email.ResetBody";
        public const string ChangeEmailSubject = "Email.ChangeEmailSubject";
        public const string ChangeEmailBody = "Email.ChangeEmailBody";
    }
}
