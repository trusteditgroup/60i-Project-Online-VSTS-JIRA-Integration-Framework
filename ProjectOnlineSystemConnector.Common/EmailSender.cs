using System;
using System.Net.Mail;
using NLog;

namespace ProjectOnlineSystemConnector.Common
{
    public class EmailSender
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void SendNotificationEmail(string userEmail, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage
                {
                    Subject = subject,
                    Body = body
                };
                mail.To.Add(userEmail);
                SmtpClient client = new SmtpClient();
                client.Send(mail);
            }
            catch (Exception exception)
            {
                logger.Error(exception);
            }
        }
    }
}