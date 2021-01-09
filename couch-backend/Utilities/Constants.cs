namespace couch_backend.Utilities
{
    public class Constants
    {
        // Error Messages
        public static readonly string EMAIL_REQUIRED_ERROR_MESSAGE = 
            "The Email field is required.";
        public static readonly string INVALID_EMAIL_REQUIRED_ERROR_MESSAGE =
            "The Email field is not a valid e-mail address.";
        public static readonly string PASSWORD_REQUIRED_ERROR_MESSAGE =
            "The Password field is required.";

        // Others
        public static readonly string SAMPLE_EMAIL = "user@example.com";

        // Roles
        public const string ADMIN_ROLE = "Admin";
        public const string SUPER_ADMIN_ROLE = "Super Admin";
        public const string USER_ROLE = "User";
        public const string DEFAULT_ERROR_MESSAGE = "Could not complete request. " +
            "Please retry later, or contact the support team";
    }
}
