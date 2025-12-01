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
            _smtpServer = config["Email:SmtpServer"] ?? throw new InvalidOperationException("Email:SmtpServer не задан в appsettings.json");
            _username = config["Email:Username"] ?? throw new InvalidOperationException("Email:Username не задан в appsettings.json");
            _password = config["Email:Password"] ?? throw new InvalidOperationException("Email:Password не задан в appsettings.json");

            var portStr = config["Email:Port"];
            if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out var port))
                throw new InvalidOperationException("Email:Port не задан или не является числом в appsettings.json");

            _port = port;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            Console.WriteLine($"Отправка письма на {to} с темой {subject}, длина тела: {body.Length}"); // <-- Добавьте лог
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
            Console.WriteLine("Письмо успешно отправлено."); // <-- Добавьте лог
        }
    }
}