using System.Net.Mail;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Services
{
    public interface IEmailService
    {
        void SendEmail(string[] toEmailAddresses, string subject, string body, MailAddress from = null, bool isHtmlBody = false, string[] ccEmailAddresses = null, string[] bccEmailAddresses = null);
        Task SendEmailAsync(string[] toEmailAddresses, string subject, string body, MailAddress from = null, bool isHtmlBody = false, string[] ccEmailAddresses = null, string[] bccEmailAddresses = null);
        void SendEmail<T>(string[] toEmailAddresses, string subject, T viewModel, string templatePath, MailAddress from = null, bool isHtmlBody = true, string[] ccEmailAddresses = null, string[] bccEmailAddresses = null);
        Task SendEmailAsync<T>(string[] toEmailAddresses, string subject, T viewModel, string templatePath, MailAddress from = null, bool isHtmlBody = true, string[] ccEmailAddresses = null, string[] bccEmailAddresses = null);
    }
}
