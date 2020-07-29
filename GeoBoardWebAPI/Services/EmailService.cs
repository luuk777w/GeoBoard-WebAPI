using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using GeoBoardWebAPI.Models.Options;

namespace GeoBoardWebAPI.Services
{
    public class EmailService : IEmailService
    {
        protected readonly IConfiguration _configuration;
        protected readonly ITemplateService _templateService;
        protected readonly EmailSettings _emailSettings;

        public EmailService(
            IConfiguration configuration,
            ITemplateService templateService,
            IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
            _configuration = configuration;
            _templateService = templateService;
        }


        public void SendEmail(string[] toEmailAddresses,
                            string subject,
                            string body,
                            MailAddress from = null,
                            bool isHtmlBody = false,
                            string[] ccEmailAddresses = null,
                            string[] bccEmailAddresses = null)
        {
            var mailMessage = CreateMailMessage(toEmailAddresses, ccEmailAddresses, bccEmailAddresses, from);
            mailMessage.Subject = $"{subject}";
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = isHtmlBody;

            using (var smtpClient = GetClient())
            {
                smtpClient.Send(mailMessage);
            }
        }

        public void SendEmail<T>(string[] toEmailAddresses,
                            string subject, T viewModel,
                            string templatePath,
                            MailAddress from = null,
                            bool isHtmlBody = true,
                            string[] ccEmailAddresses = null,
                            string[] bccEmailAddresses = null)
        {
            var mailMessage = CreateMailMessage(toEmailAddresses, ccEmailAddresses, bccEmailAddresses, from);
            mailMessage.Subject = $"{subject}";
            mailMessage.Body = _templateService.RenderTemplate<T>(templatePath, viewModel);
            mailMessage.IsBodyHtml = isHtmlBody;

            try
            {
                using (var smtpClient = GetClient())
                {
                    smtpClient.Send(mailMessage);
                }
            }
            catch { throw; }
        }

        public async Task SendEmailAsync(string[] toEmailAddresses,
                            string subject,
                            string body,
                            MailAddress from = null,
                            bool isHtmlBody = false,
                            string[] ccEmailAddresses = null,
                            string[] bccEmailAddresses = null)
        {
            var mailMessage = CreateMailMessage(toEmailAddresses, ccEmailAddresses, bccEmailAddresses, from);
            mailMessage.Subject = $"{subject}";
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = isHtmlBody;

            try
            {
                using (var smtpClient = GetClient())
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch { throw; }
        }

        public async Task SendEmailAsync<T>(string[] toEmailAddresses,
                            string subject, T viewModel,
                            string templatePath,
                            MailAddress from = null,
                            bool isHtmlBody = true,
                            string[] ccEmailAddresses = null,
                            string[] bccEmailAddresses = null)
        {
            var mailMessage = CreateMailMessage(toEmailAddresses, ccEmailAddresses, bccEmailAddresses, from);
            mailMessage.Subject = $"{subject}";
            mailMessage.Body = await _templateService.RenderTemplateAsync<T>(templatePath, viewModel);
            mailMessage.IsBodyHtml = isHtmlBody;

            try
            {
                using (var smtpClient = GetClient())
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch { throw; }
        }

        private MailMessage CreateMailMessage(string[] to, string[] cc, string[] bcc, MailAddress from)
        {
            if (!to.Any()) throw new System.Exception("To email address not defined, atleast one needs to be set", new System.NullReferenceException());
            if (string.IsNullOrWhiteSpace(_emailSettings.FromEmail)) throw new System.Exception("From email address not defined", new System.NullReferenceException());

            var message = new MailMessage();
            message.From = from?.Address == null ? new MailAddress(_emailSettings.FromEmail, _emailSettings.FromDisplayName) : from;
            message.ReplyToList.Add(_emailSettings.BounceAddress);
            message.Headers.Add("Errors-To", _emailSettings.BounceAddress);
            message.Headers.Add("Return-Path", _emailSettings.BounceAddress);
            message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;

            foreach (var email in to)
            {
                message.To.Add(email);
            }

            if (cc != null)
            {
                foreach (var email in cc)
                {
                    message.CC.Add(email);
                }
            }

            if (bcc != null)
            {
                foreach (var email in bcc)
                {
                    message.Bcc.Add(email);
                }
            }

            return message;
        }

        private SmtpClient GetClient()
        {
            SmtpClient smtpClient = new SmtpClient(_emailSettings.Host, _emailSettings.Port);
            ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };

            smtpClient.EnableSsl = _emailSettings.EnableSsl;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Credentials = new NetworkCredential(_emailSettings.User, _emailSettings.Password);

            return smtpClient;
        }
    }
}
