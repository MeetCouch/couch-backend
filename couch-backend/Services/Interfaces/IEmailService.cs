namespace couch_backend.Services.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Send an email for a successful user registration
        /// </summary>
        /// <param name="email"></param>
        /// <param name="token"></param>
        void SendSuccessfulRegistrationMessage(string email,
                                               string token);
    }
}
