using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using DeviceScanner.Utils;

namespace DeviceScanner.Reporting
{
    /// <summary>Sends the PDF report and the log to the QC address (ScanConfig.QcEmailAddress) over SMTP.</summary>
    internal static class ReportMailer
    {
        /// <summary>Null = sent, otherwise why it was not. sender = the address shown as the sender (display only; replies go to it).</summary>
        public static string Send(ScanConfig cfg, RunReport r, ScanFiles files, string sender, Action<string> log)
        {
            using (L.Use(Lang.En)) return SendEnglish(cfg, r, files, sender, log);   // QC reads English
        }

        private static string SendEnglish(ScanConfig cfg, RunReport r, ScanFiles files, string sender, Action<string> log)
        {
            log("");
            log($"===== {L.T("mail.sending", cfg.QcEmailAddress)} =====");
            if (string.IsNullOrEmpty(cfg.SmtpPassword))
            {
                log("    " + L.T("mail.failed", L.T("mail.noPassword")));
                return L.T("mail.noPassword");
            }
            try
            {
                var modules = ModuleVerdicts.All(r);
                var verdict = ModuleVerdicts.Overall(modules);
                using (var msg = new MailMessage())
                using (var smtp = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort) { EnableSsl = cfg.SmtpSsl, Timeout = cfg.SmtpTimeoutMs })
                {
                    // sent through the config Gmail account (Gmail only relays mail From itself);
                    // the address the operator typed is what the recipients see as the sender, and replies go to it
                    msg.From = new MailAddress(cfg.SmtpUser, sender);
                    msg.ReplyToList.Add(new MailAddress(sender, r.UserName));
                    msg.To.Add(cfg.QcEmailAddress);
                    foreach (string cc in cfg.QcCc) msg.CC.Add(cc);
                    msg.Subject = L.T("mail.subject", r.Profile.Name, verdict.Passed ? L.T("tag.pass") : L.T("tag.fail"),
                                      r.StartedAt.ToString("yyyy-MM-dd HH:mm"));
                    msg.Body = L.T("mail.body", r.Profile.Name, r.UserName, r.StartedAt.ToString("yyyy-MM-dd HH:mm"), Environment.MachineName) +
                               "\n\n" + string.Join("\n", modules.Select(m => $"{PdfReport.TagOf(m.Status),-8} {m.Title}: {m.Summary}"));
                    foreach (string path in new[] { files.PdfPath, files.LogPath }.Where(p => p != null && File.Exists(p)))
                        msg.Attachments.Add(new Attachment(path));
                    smtp.Credentials = new NetworkCredential(cfg.SmtpUser, cfg.SmtpPassword);
                    smtp.Send(msg);
                }
                log("    " + L.T("mail.sent", cfg.QcEmailAddress));
                return null;
            }
            catch (Exception ex)
            {
                string why = ex.GetBaseException().Message;
                log("    " + L.T("mail.failed", why));
                return why;
            }
        }
    }
}
