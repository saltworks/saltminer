using MimeKit;

namespace Saltworks.SaltMiner.Core.Email
{
    public class EmailRequest
    {
        public EmailRequest(string from, string fromDisplay, string to, string toDisplay)
        {
            From = new MailboxAddress(fromDisplay, from);
            To = new MailboxAddress(toDisplay, to);
        }

        public MailboxAddress From { get; set; }
        public MailboxAddress To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
