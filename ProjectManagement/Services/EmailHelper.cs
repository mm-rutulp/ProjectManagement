using System.Net.Mail;
using MagnusMinds.Utility.EmailService;
using MimeKit;

namespace ProjectManagement.Services
{
    public class EmailHelper
    {

        private readonly IEmailSender _emailSender;
        public EmailHelper(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }
        public async Task<MimeMessage> SendEmail(string subject, string htmlContent, List<string> to, List<string> cc = null, List<string> bcc = null, string mailFrom = null)
        {
            var mimeMessage = new MimeMessage();
            FillMimeMessage(to, cc, bcc, mimeMessage);
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlContent };

            if (string.IsNullOrEmpty(mailFrom))
            {
                _emailSender.SendEmail(mimeMessage);
            }
            else
            {
                var mailMessage = ConvertToMailMessage(mimeMessage, mailFrom, htmlContent);
                _emailSender.SendEmail(mailMessage);
            }
            return mimeMessage;
        }

        private static void FillMimeMessage(List<string> to, List<string> cc, List<string> bcc, MimeMessage mimeMessage)
        {
            foreach (var item in to)
                mimeMessage.To.Add(new MailboxAddress("TANYO", item));
            if (cc != null)
                foreach (var item in cc)
                    mimeMessage.Cc.Add(new MailboxAddress("TANYO", item));
            if (bcc != null)
                foreach (var item in bcc)
                    mimeMessage.Bcc.Add(new MailboxAddress("TANYO", item));
        }

        private MailMessage ConvertToMailMessage(MimeMessage mimeMessage, string mailFrom, string htmlContent)
        {
            var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(mailFrom);
            mailMessage.Subject = mimeMessage.Subject;
            mailMessage.Body = htmlContent;
            mailMessage.IsBodyHtml = true;

            foreach (var address in mimeMessage.To)
                mailMessage.To.Add(new MailAddress(Convert.ToString(address)));

            foreach (var address in mimeMessage.Cc)
                mailMessage.CC.Add(new MailAddress(Convert.ToString(address)));

            foreach (var address in mimeMessage.Bcc)
                mailMessage.Bcc.Add(new MailAddress(Convert.ToString(address)));

            return mailMessage;
        }

    }
}
