namespace couch_backend.Utilities
{
    /// <summary>
    /// All the available email templates
    /// </summary>
    public sealed class EmailTemplate
    {
        /// <summary>
        /// The filename for this template 
        /// </summary>
        public string filename { get; }

        private EmailTemplate(string filename)
        {
            this.filename = filename;
        }

        /// <summary>
        /// An email containing instructions to confirm the email
        /// </summary>
        public static readonly EmailTemplate CONFIRM_EMAIL = new EmailTemplate("ConfirmEmail.html");
        /// <summary>
        /// A user successfully confirmed their email
        /// </summary>
        public static readonly EmailTemplate EMAIL_CONFIRMED = new EmailTemplate("EmailConfirmed.html");
        /// <summary>
        /// An email containing instructions to reset the user password
        /// </summary>
        public static readonly EmailTemplate FORGOT_PASSWORD = new EmailTemplate("ForgotPassword.html");
        /// <summary>
        /// A user's password has successfully been changed
        /// </summary>
        public static readonly EmailTemplate PASSWORD_CHANGED_ALERT = new EmailTemplate("PasswordChangedAlert.html");
        /// <summary>
        /// The user has successfully reset their password
        /// </summary>
        public static readonly EmailTemplate PASSWORD_RESET_SUCCESSFUL = new EmailTemplate("PasswordResetSuccessful.html");
    }
}
