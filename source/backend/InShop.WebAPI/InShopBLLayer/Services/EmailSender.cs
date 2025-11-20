// InShopBLLayer.Services/EmailSender.cs
using InShopBLLayer.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;

namespace InShopBLLayer.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public EmailSender(IConfiguration config)
        {
            _smtpServer = config["Email:SmtpServer"];
            _port = int.Parse(config["Email:Port"]);
            _username = config["Email:Username"];
            _password = config["Email:Password"];
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_smtpServer, _port)
            {
                EnableSsl = true,
                Credentials = new System.Net.NetworkCredential(_username, _password)
            };

            var mail = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mail.From = new MailAddress(_username);
            mail.To.Add(to);

            await client.SendMailAsync(mail);
        }
    }
}