using couch_backend.Services.Interfaces;
using couch_backend.Utilities;
using Mandrill;
using Mandrill.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace couch_backend.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private string _defaultFromEmail;
        private string _defaultFromName;
        private readonly ILogger<EmailService> _logger;
        private MandrillApi _mandrillApi;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _defaultFromEmail = _configuration["DefaultFromEmail"];
            _defaultFromName = _configuration["DefaultFromEmailName"];
            _logger = logger;
            _mandrillApi = new MandrillApi(apiKey: _configuration["MANDRILLAPIKEY"]);
        }

        /// <summary>
        /// Send an email using Mandrill
        /// </summary>
        /// <param name="emailTemplate"></param>
        /// <param name="dictionary"></param>
        /// <param name="subject"></param>
        /// <param name="toEmail"></param>
        internal async Task SendEmail(
            EmailTemplate emailTemplate,
            Dictionary<EmailReplacementKey, string> replacementKeys,
            string subject,
            string toEmail)
        {
            string emailBody = string.Empty;

            using (StreamReader streamReader = new StreamReader(
                $"EmailTemplates/{emailTemplate.filename}"))
            {
                emailBody = streamReader.ReadToEnd();
            }

            foreach (KeyValuePair<EmailReplacementKey, string> pair in replacementKeys)
            {
                emailBody = emailBody.Replace(pair.Key.KeyString, pair.Value);
            }

            var msg = new MandrillMessage()
            {
                FromEmail = _defaultFromEmail,
                Html = emailBody,
                FromName = _defaultFromName,
                Subject = subject,
            };

            msg.AddTo(toEmail);
            var response = await _mandrillApi.Messages.SendAsync(msg);

            _logger.LogInformation($"{emailTemplate.filename} to {toEmail} " +
                $"had a status of {response.FirstOrDefault().Status}\n " +
                $"message: {response.FirstOrDefault().RejectReason}");
        }

        ///// <summary>
        ///// A user account was created by an admin
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        ///// <param name="password"></param>
        ///// <param name="platform"></param>
        ///// <param name="token"></param>
        //public void SendAccountCreatedByAdminEmail(string email,
        //                                           string fullname,
        //                                           string password,
        //                                           int platform,
        //                                           string token)
        //{
        //    var platformUrl = GetPlatformUrl(platform);

        //    var targetUrl = "";

        //    // web
        //    if (platform == OtherConstants.TWO)
        //    {
        //        var encodedEmail = WebUtility.UrlEncode(email);
        //        var encodedToken = WebUtility.UrlEncode(token);
        //        targetUrl = platformUrl + $"/confirm-email/{encodedEmail}/{encodedToken}";
        //    }
        //    else
        //    {
        //        targetUrl = platformUrl + $"/confirm-email/{email}/{token}";
        //    }

        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.EMAIL, email },
        //        {EmailReplacementKey.PASSWORD, password },
        //        {EmailReplacementKey.SITE_URL, OtherConstants.WEB_BASE_URL },
        //        {EmailReplacementKey.VERIFY_EMAIL_URL, targetUrl }
        //    };

        //    SendEmail(email, fullname, "Confirm your Itara.ng Account", EmailTemplate.ADMIN_CREATED_ACCOUNT,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// Send email for a delete account request
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        //public void SendAccountDeletedEmail(string email, string fullname)
        //{
        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.SITE_URL, OtherConstants.WEB_BASE_URL }
        //    };
        //    SendEmail(email, fullname, "Goodbye from Itara.ng", EmailTemplate.DELETE_ACCOUNT_SUCCESSFUL,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// Send an email that the users email has been confirmed
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        //public void SendEmailConfirmedMessage(string email, string fullname)
        //{
        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.SITE_URL, OtherConstants.WEB_BASE_URL }
        //    };
        //    SendEmail(email, fullname, "Welcome to Itara.ng", EmailTemplate.EMAIL_CONFIRMED,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// Request for a new confirmation email
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        ///// <param name="platform"></param>
        ///// <param name="token"></param>
        //public void SendNewEmailConfirmationToken(string email,
        //                                          string fullname,
        //                                          int platform,
        //                                          string token)
        //{
        //    var platformUrl = GetPlatformUrl(platform);

        //    var targetUrl = "";

        //    // web
        //    if (platform == OtherConstants.TWO)
        //    {
        //        var encodedEmail = WebUtility.UrlEncode(email);
        //        var encodedToken = WebUtility.UrlEncode(token);
        //        targetUrl = platformUrl + $"/confirm-email/{encodedEmail}/{encodedToken}";
        //    }
        //    else
        //    {
        //        targetUrl = platformUrl + $"/confirm-email/{email}/{token}";
        //    }

        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.VERIFY_EMAIL_URL, targetUrl }
        //    };

        //    SendEmail(email, fullname, "Confirm your Itara.ng Account", EmailTemplate.CONFIRM_EMAIL,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// User requested for a 'ForgotPassword' link
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        ///// <param name="passwordResetToken"></param>
        ///// <param name="platform"></param>
        //public void SendForgotPasswordEmail(string email,
        //                                    string fullname,
        //                                    string passwordResetToken,
        //                                    int platform)
        //{
        //    var platformUrl = GetPlatformUrl(platform);

        //    var targetUrl = "";

        //    // web
        //    if (platform == OtherConstants.TWO)
        //    {
        //        var encodedEmail = WebUtility.UrlEncode(email);
        //        var encodedToken = WebUtility.UrlEncode(passwordResetToken);
        //        targetUrl = platformUrl + $"/reset-password/{encodedEmail}/{encodedToken}";
        //    }
        //    else
        //    {
        //        targetUrl = platformUrl + $"/reset-password/{email}/{passwordResetToken}";
        //    }

        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.FORGOT_PASSWORD_URL, targetUrl }
        //    };

        //    SendEmail(email, fullname, "Reset your Itara.ng password", EmailTemplate.FORGOT_PASSWORD,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// User's order has been confirmed
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        //public void SendOrderConfirmedEmail(string email, string fullname)
        //{
        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.SITE_URL, OtherConstants.WEB_BASE_URL }
        //    };
        //    SendEmail(email, fullname, "Order Confirmed", EmailTemplate.ORDER_CONFIRMED_EMAIL,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// User has changed their password
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        //public void SendPasswordChangedEmail(string email, string fullname)
        //{
        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.SITE_URL, OtherConstants.WEB_BASE_URL }
        //    };
        //    SendEmail(email, fullname, "Your Itara.ng password has changed", EmailTemplate.PASSWORD_CHANGED_ALERT,
        //        replacementKeys).Wait();
        //}

        ///// <summary>
        ///// User has successfully reset their password
        ///// </summary>
        ///// <param name="email"></param>
        ///// <param name="fullname"></param>
        //public void SendPasswordResetEmail(string email, string fullname)
        //{
        //    var replacementKeys = new Dictionary<EmailReplacementKey, string>
        //    {
        //        {EmailReplacementKey.USERNAME, fullname },
        //        {EmailReplacementKey.SITE_URL, OtherConstants.WEB_BASE_URL }
        //    };
        //    SendEmail(email, fullname, "Your Itara.ng password reset was successful", EmailTemplate.PASSWORD_RESET_SUCCESSFUL,
        //        replacementKeys).Wait();
        //}

        /// <summary>
        /// Send an email on successful registration
        /// </summary>
        /// <param name="email"></param>
        /// <param name="token"></param>
        public void SendSuccessfulRegistrationMessage(
            string email,
            string token)
        {
            var encodedEmail = WebUtility.UrlEncode(email);
            var encodedToken = WebUtility.UrlEncode(token);
            string targetUrl = Constants.SITE_URL + 
                $"/confirm-email/{encodedEmail}/{encodedToken}";

            var replacementKeys = new Dictionary<EmailReplacementKey, string>
            {
                {EmailReplacementKey.VERIFY_EMAIL_URL, targetUrl }
            };

            SendEmail(EmailTemplate.CONFIRM_EMAIL, replacementKeys, 
                "Confirm your Account", email
            ).Wait();
        }
    }
}
