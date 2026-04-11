// InShopBLLayer.Services/EmailSender.cs
using InShopBLLayer.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
        {
            _logger = logger;
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
            _logger.LogInformation(
                "SMTP: отправка на {Recipient}, сервер {Host}:{Port}, тема длины {SubjectLen}, тело длины {BodyLen}",
                to,
                _smtpServer,
                _port,
                subject?.Length ?? 0,
                body?.Length ?? 0);

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

            try
            {
                await client.SendMailAsync(mail);
                _logger.LogInformation("SMTP: письмо успешно отправлено на {Recipient}", to);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(
                    ex,
                    "SMTP: SmtpException при отправке на {Recipient}. StatusCode={StatusCode}",
                    to,
                    ex.StatusCode);
                throw;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "SMTP: ошибка при отправке на {Recipient}", to);
                throw;
            }
        }
    }
}