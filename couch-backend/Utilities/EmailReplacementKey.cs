namespace couch_backend.Utilities
{
    /// <summary>
    /// A class containing the values that can be replaced within an email template
    /// </summary>
    public sealed class EmailReplacementKey
    {
        /// <summary>
        /// The key string of this EmailReplacementKey
        /// </summary>
        public string KeyString { get; }

        private EmailReplacementKey(string key)
        {
            this.KeyString = key;
        }
        /// <summary>
        /// The user's email
        /// </summary>
        public static readonly EmailReplacementKey EMAIL = new EmailReplacementKey("{{email}}");
        /// <summary>
        /// A URL link that the user uses to create a new password
        /// </summary>
        public static readonly EmailReplacementKey FORGOT_PASSWORD_URL = new EmailReplacementKey("{{forgot-password-url}}");
        /// <summary>
        /// The user's password
        /// </summary>
        public static readonly EmailReplacementKey PASSWORD = new EmailReplacementKey("{{password}}");
        /// <summary>
        /// A URL link that takes the user straight to the homepage
        /// </summary>
        public static readonly EmailReplacementKey SITE_URL = new EmailReplacementKey("{{site-url}}");
        /// <summary>
        /// A URL link that the user uses to verify email
        /// </summary>
        public static readonly EmailReplacementKey VERIFY_EMAIL_URL = new EmailReplacementKey("{{verify-email-url}}");
    }
}
