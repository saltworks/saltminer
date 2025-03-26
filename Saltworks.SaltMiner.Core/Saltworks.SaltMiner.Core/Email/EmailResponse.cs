namespace Saltworks.SaltMiner.Core.Email
{
    public class EmailResponse
    {
        public bool Success;
        public string ErrorMessage { get; set; }

        public EmailResponse(bool success)
        {
            Success = success;
        }

        public EmailResponse(bool success, string message)
        {
            Success = success;
            ErrorMessage = message;
        }
    }
}