using MailKit.Net.Smtp;
using MimeKit;
using System;

namespace Saltworks.SaltMiner.Core.Email
{
    public static class Email
    {
        public static EmailResponse Send(EmailRequest request)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(request.From);
                message.To.Add(request.To);
                message.Subject = request.Subject;
                message.Body = new TextPart("plain") { Text = request.Body };

                using (var client = new SmtpClient())
                {
                    client.Connect(request.Host, request.Port);
                    client.Authenticate(request.UserName, request.Password);
                    var response = client.Send(message);
                    client.Disconnect(true);

                    return new EmailResponse(true, response);
                }
            }
            catch (Exception ex)
            {

                return new EmailResponse(false, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
