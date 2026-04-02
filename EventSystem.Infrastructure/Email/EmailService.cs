using EventHub.Application.Contracts;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Infrastructure.Email
{
    public class EmailService(IOptions<EmailSettings> options) : IEmailService
    {
        private readonly EmailSettings _options = options.Value;

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Create the email message
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_options.Email);
            email.From.Add(MailboxAddress.Parse(_options.Email));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            // Set the email body
            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            // Send the email using SMTP (Mailkit)
            using var smtp = new SmtpClient();
            smtp.Connect(_options.Host, _options.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_options.Email, _options.Password);

            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }
    }
}
